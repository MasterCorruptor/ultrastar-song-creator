using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace UltraStar.SongCreator.Generation;

internal enum GenerationProcessReason
{
    Success, Cancelled, CloseRequested, TimedOut, Unavailable, OutputLimit, PipeFailure,
    NonZeroExit, DrainIncomplete, Busy, Closed, InvalidInvocation
}
internal enum GenerationCleanupState { LocalReleased, Pending, Quarantined }
internal sealed record GenerationProcessOptions(string Executable, ImmutableArray<string> Arguments, string WorkingDirectory)
{
    internal TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    internal int StdoutLimit { get; init; } = 1024 * 1024;
    internal int StderrLimit { get; init; } = 64 * 1024;
}
internal sealed class GenerationProcessCleanup
{
    private int released, quarantined;
    private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal GenerationCleanupState State => Volatile.Read(ref quarantined) != 0 ? GenerationCleanupState.Quarantined
        : Volatile.Read(ref released) != 0 ? GenerationCleanupState.LocalReleased : GenerationCleanupState.Pending;
    internal Task WhenLocalReleased => completion.Task;
    internal void Quarantine() => Interlocked.Exchange(ref quarantined, 1);
    internal void Release() { Volatile.Write(ref released, 1); completion.TrySetResult(); }
    internal static GenerationProcessCleanup Empty()
    {
        var result = new GenerationProcessCleanup(); result.Release(); return result;
    }
}
internal sealed record GenerationProcessResult(GenerationProcessReason Reason, int? ExitCode,
    ImmutableArray<byte> Stdout, ImmutableArray<byte> Stderr, GenerationProcessCleanup Cleanup);

// Test-only operation gates/faults, never a replacement executable/process/stream backend.
internal enum GenerationProcessPoint { Start, StdoutRead, StderrRead, Kill, PipeClose, Dispose, StdinClose, StdoutClose, StderrClose }
internal sealed class GenerationProcessHooks
{
    internal Func<GenerationProcessPoint, Task>? Before { get; init; }
    internal Action<StreamWriter, StreamReader, StreamReader>? StreamsOpened { get; init; }
    internal Action<GenerationProcessPoint>? PipeDisposed { get; init; }
    internal Action? CleanupBudgetArmed { get; init; }
    internal Action? RootEofBudgetArmed { get; init; }
}

/// <summary>One-slot trusted-leaf supervisor. Local release is NOT arbitrary descendant containment.</summary>
internal sealed class GenerationProcess
{
    private readonly object sync = new();
    private readonly TimeProvider clock;
    private readonly TimeSpan grace;
    private readonly GenerationProcessHooks? hooks;
    private Operation? active;
    private bool closed, quarantined;
    private Task<GenerationProcessCleanup>? close;
    private GenerationProcessCleanup last = GenerationProcessCleanup.Empty();

    internal GenerationProcess(TimeProvider? clock = null, TimeSpan? drainGrace = null) : this(clock, drainGrace, null) { }
    internal GenerationProcess(TimeProvider? clock, TimeSpan? drainGrace, GenerationProcessHooks? hooks)
    {
        this.clock = clock ?? TimeProvider.System; grace = drainGrace ?? TimeSpan.FromSeconds(2);
        if (grace <= TimeSpan.Zero || grace > TimeSpan.FromSeconds(10)) throw new ArgumentOutOfRangeException(nameof(drainGrace));
        this.hooks = hooks;
    }
    internal Task<GenerationProcessResult> RunAsync(GenerationProcessOptions options, CancellationToken caller = default)
    {
        var valid = Valid(options);
        var admitted = clock.GetTimestamp();
        Operation operation;
        lock (sync)
        {
            if (closed || quarantined) return Rejected(GenerationProcessReason.Closed);
            if (caller.IsCancellationRequested) return Rejected(GenerationProcessReason.Cancelled);
            if (!valid) return Rejected(GenerationProcessReason.InvalidInvocation);
            if (active is not null) return Rejected(GenerationProcessReason.Busy);
            operation = new(this, options, caller, admitted);
            active = operation; last = operation.Cleanup;
        }
        operation.Begin();
        return operation.Result.Task;
    }
    internal Task<GenerationProcessCleanup> CloseAsync()
    {
        lock (sync)
        {
            if (close is not null) return close;
            closed = true;
            active?.Record(Operation.Closing);
            close = active is { } operation ? CloseCore(operation.Result.Task) : Task.FromResult(last);
            return close;
        }
    }
    private static async Task<GenerationProcessCleanup> CloseCore(Task<GenerationProcessResult> result) =>
        (await result.ConfigureAwait(false)).Cleanup;
    private static Task<GenerationProcessResult> Rejected(GenerationProcessReason reason) =>
        Task.FromResult(new GenerationProcessResult(reason, null, [], [], GenerationProcessCleanup.Empty()));
    private static bool Valid(GenerationProcessOptions? o)
    {
        if (o is null || o.Arguments.IsDefault || o.Arguments.Length > 256 || o.Timeout <= TimeSpan.Zero ||
            o.Timeout > TimeSpan.FromMinutes(10) || o.StdoutLimit is < 1 or > 32 * 1024 * 1024 ||
            o.StderrLimit is < 1 or > 64 * 1024) return false;
        try
        {
            var utf8 = new UTF8Encoding(false, true);
            foreach (var path in new[] { o.Executable, o.WorkingDirectory })
            {
                if (string.IsNullOrWhiteSpace(path) || path.Length > 4096 || path.Contains('\0') ||
                    !Path.IsPathFullyQualified(path) ||
                    (Uri.TryCreate(path, UriKind.Absolute, out var uri) && !uri.IsFile)) return false;
                _ = Path.GetFullPath(path); _ = utf8.GetByteCount(path);
            }
            long bytes = 0;
            foreach (var arg in o.Arguments)
            {
                if (arg is null || arg.Contains('\0')) return false;
                bytes += utf8.GetByteCount(arg);
                if (bytes > 64 * 1024) return false;
            }
            return true;
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException) { return false; }
    }
    private void Poison(Operation operation)
    {
        lock (sync) { operation.Cleanup.Quarantine(); quarantined = true; }
    }
    private void Released(Operation operation)
    {
        lock (sync)
        {
            if (ReferenceEquals(active, operation) && operation.Result.Task.IsCompleted && operation.Cleanup.State != GenerationCleanupState.Quarantined)
                active = null;
            operation.Cleanup.Release();
        }
    }
    private void Publish(Operation operation)
    {
        lock (sync)
        {
            if (operation.Result.Task.IsCompleted) return;
            if (operation.Caller.IsCancellationRequested) operation.Record(Operation.Cancel);
            var facts = operation.Freeze();
            var reason = Operation.Reason(facts);
            if (reason == GenerationProcessReason.Success &&
                (operation.Cleanup.State != GenerationCleanupState.LocalReleased || !operation.SuccessfulEof))
                reason = GenerationProcessReason.DrainIncomplete;
            operation.Result.TrySetResult(new(reason, operation.ExitCode,
                reason == GenerationProcessReason.Success ? ImmutableArray.CreateRange(operation.Out) : [],
                reason == GenerationProcessReason.Success ? ImmutableArray.CreateRange(operation.Err) : [],
                operation.Cleanup));
            if (ReferenceEquals(active, operation) && operation.Cleanup.State == GenerationCleanupState.LocalReleased)
                active = null;
        }
    }

    private sealed class Operation(GenerationProcess owner, GenerationProcessOptions options, CancellationToken caller, long admitted)
    {
        internal const int Cancel = 1, Closing = 2, Limit = 4, Pipe = 8, Timeout = 16, Unavailable = 32, Nonzero = 64, Drain = 128;
        private const int Published = 1 << 30;
        private int facts, abortReads;
        private readonly TaskCompletionSource stop = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource unknown = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource<GenerationProcessResult> Result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly GenerationProcessCleanup Cleanup = new();
        internal CancellationToken Caller => caller;
        private Process? process;
        private StreamWriter? inputWriter;
        private StreamReader? outputReader, errorReader;
        private Stream? inputPipe, outputPipe, errorPipe;
        private Task<bool>? starting;
        private Task? root, input, output, errors, allIo;
        private bool started, outEof, errEof;
        private volatile bool rootExited;
        private Task? observation;
        private int? exitCode;
        private byte[] stdout = [], stderr = [];
        internal int? ExitCode => rootExited ? exitCode : null;
        internal byte[] Out => stdout;
        internal byte[] Err => stderr;
        internal bool SuccessfulEof => rootExited && exitCode == 0 && outEof && errEof && allIo?.IsCompletedSuccessfully == true;
        private bool Stopping => (Volatile.Read(ref facts) & (Cancel | Closing | Limit | Pipe | Timeout | Unavailable | Drain)) != 0;
        internal void Record(int cause)
        {
            while (true)
            {
                var old = Volatile.Read(ref facts);
                if ((old & Published) != 0) return;
                if (Interlocked.CompareExchange(ref facts, old | cause, old) != old) continue;
                if (cause != Nonzero) stop.TrySetResult();
                return;
            }
        }
        internal int Freeze() => Interlocked.Or(ref facts, Published);
        internal static GenerationProcessReason Reason(int value) =>
            (value & Cancel) != 0 ? GenerationProcessReason.Cancelled :
            (value & Closing) != 0 ? GenerationProcessReason.CloseRequested :
            (value & Limit) != 0 ? GenerationProcessReason.OutputLimit :
            (value & Pipe) != 0 ? GenerationProcessReason.PipeFailure :
            (value & Timeout) != 0 ? GenerationProcessReason.TimedOut :
            (value & Unavailable) != 0 ? GenerationProcessReason.Unavailable :
            (value & Nonzero) != 0 ? GenerationProcessReason.NonZeroExit :
            (value & Drain) != 0 ? GenerationProcessReason.DrainIncomplete : GenerationProcessReason.Success;

        // The admitted operation retains its coordinator, which observes every native/I/O task.
        internal void Begin() => observation = Task.Run(ObserveAsync);
        private async Task Hook(GenerationProcessPoint point)
        {
            if (owner.hooks?.Before is { } before) await before(point).ConfigureAwait(false);
        }
        private async Task<bool> StartAsync()
        {
            try
            {
                process = new Process
                {
                    StartInfo = new()
                    {
                        FileName = options.Executable,
                        WorkingDirectory = options.WorkingDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                foreach (var arg in options.Arguments) process.StartInfo.ArgumentList.Add(arg);
                await Hook(GenerationProcessPoint.Start).ConfigureAwait(false);
                if (Stopping) return false;
                started = process.Start();
                if (!started) Record(Unavailable);
                else CaptureStreams();
                return started;
            }
            catch (Exception) { Record(Unavailable); return false; }
        }
        private void CaptureStreams()
        {
            // Accessed stdout/stderr select SyncMode; accessed stdin is also caller-owned.
            // Capture each wrapper/base stream once, retaining partial acquisition on failure.
            try { inputWriter = process!.StandardInput; inputPipe = inputWriter.BaseStream; }
            catch (Exception) { Record(Pipe); }
            try { outputReader = process!.StandardOutput; outputPipe = outputReader.BaseStream; }
            catch (Exception) { Record(Pipe); }
            try { errorReader = process!.StandardError; errorPipe = errorReader.BaseStream; }
            catch (Exception) { Record(Pipe); }
            try
            {
                if (inputWriter is not null && outputReader is not null && errorReader is not null)
                    owner.hooks?.StreamsOpened?.Invoke(inputWriter, outputReader, errorReader);
            }
            catch (Exception) { Record(Pipe); }
        }
        private void StartIo()
        {
            if (allIo is not null || !started) return;
            root = Task.Run(async () =>
            {
                try
                {
                    await process!.WaitForExitAsync().ConfigureAwait(false);
                    exitCode = process.ExitCode; rootExited = true;
                    if (exitCode != 0) Record(Nonzero);
                }
                catch (Exception)
                {
                    Record(Pipe);
                    try { if (process!.HasExited) { exitCode = process.ExitCode; rootExited = true; } }
                    catch (Exception) { /* Unknown root identity remains owned/quarantined. */ }
                }
            });
            input = Task.Run(() => CloseOwnedPipeAsync(inputWriter, inputPipe, GenerationProcessPoint.StdinClose));
            output = Task.Run(() => ReadAsync(true));
            errors = Task.Run(() => ReadAsync(false));
            allIo = Task.WhenAll(root, input, output, errors);
        }
        private async Task ReadAsync(bool standardOutput)
        {
            try
            {
                await Hook(standardOutput ? GenerationProcessPoint.StdoutRead : GenerationProcessPoint.StderrRead).ConfigureAwait(false);
                if (Volatile.Read(ref abortReads) != 0) return;
                var stream = standardOutput ? outputPipe! : errorPipe!;
                var maximum = standardOutput ? options.StdoutLimit : options.StderrLimit;
                using var buffer = new MemoryStream();
                var chunk = new byte[8192];
                while (true)
                {
                    var count = await stream.ReadAsync(chunk).ConfigureAwait(false);
                    if (count == 0)
                    {
                        if (Volatile.Read(ref abortReads) == 0)
                        { if (standardOutput) outEof = true; else errEof = true; }
                        if (standardOutput) stdout = buffer.ToArray(); else stderr = buffer.ToArray();
                        return;
                    }
                    if (buffer.Length + count > maximum) { Record(Limit); return; }
                    buffer.Write(chunk, 0, count);
                }
            }
            catch (Exception) { Record(Pipe); }
        }
        private async Task KillAsync()
        {
            try { await Hook(GenerationProcessPoint.Kill).ConfigureAwait(false); }
            catch (Exception) { Record(Pipe); }
            try { if (!process!.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { /* Root can exit concurrently; wait still determines release. */ }
            catch (Exception) { Record(Pipe); }
        }
        private async Task CloseOwnedPipeAsync(IDisposable? wrapper, Stream? pipe, GenerationProcessPoint point)
        {
            try { await Hook(point).ConfigureAwait(false); }
            catch (Exception) { Record(Pipe); owner.Poison(this); }
            var uncertain = false;
            try { wrapper?.Dispose(); }
            catch (Exception) { uncertain = true; Record(Pipe); owner.Poison(this); }
            // Independent fallback even if wrapper disposal failed; retain the actual base-stream handle.
            try { pipe?.Dispose(); }
            catch (Exception) { uncertain = true; Record(Pipe); owner.Poison(this); }
            if (uncertain) await unknown.Task.ConfigureAwait(false);
            else Notify(() => owner.hooks?.PipeDisposed?.Invoke(point));
        }
        private async Task ClosePipesAsync()
        {
            Interlocked.Exchange(ref abortReads, 1);
            try { await Hook(GenerationProcessPoint.PipeClose).ConfigureAwait(false); }
            catch (Exception) { Record(Pipe); owner.Poison(this); }
            // Separate owned tasks ensure one blocked/faulted close cannot suppress the other attempt.
            var closeOutput = Task.Run(() => CloseOwnedPipeAsync(outputReader, outputPipe, GenerationProcessPoint.StdoutClose));
            var closeError = Task.Run(() => CloseOwnedPipeAsync(errorReader, errorPipe, GenerationProcessPoint.StderrClose));
            await Task.WhenAll(closeOutput, closeError).ConfigureAwait(false);
        }
        private async Task DisposeAsync()
        {
            try { await Hook(GenerationProcessPoint.Dispose).ConfigureAwait(false); }
            catch (Exception) { Record(Pipe); }
            try { process?.Dispose(); }
            catch (Exception) { Record(Pipe); owner.Poison(this); await unknown.Task.ConfigureAwait(false); }
        }
        private async Task FinalizeAsync()
        {
            await starting!.ConfigureAwait(false);
            if (started)
            {
                StartIo();
                if (Stopping && !rootExited) await Task.Run(KillAsync).ConfigureAwait(false);
                await root!.ConfigureAwait(false);
                if (!rootExited) { owner.Poison(this); await unknown.Task.ConfigureAwait(false); }
                var readers = Task.WhenAll(output!, errors!);
                if (!readers.IsCompleted)
                {
                    if (Cleanup.State != GenerationCleanupState.Quarantined)
                        await Task.WhenAny(readers, Delay(owner.grace)).ConfigureAwait(false);
                    if (!readers.IsCompleted) owner.Poison(this);
                }
                // Completed reads do not dispose their SyncMode wrappers/pipes. Always close them,
                // including EOF, nonzero and limit paths, before releasing or disposing Process.
                var closing = Task.Run(ClosePipesAsync);
                await Task.WhenAll(allIo!, closing).ConfigureAwait(false);
            }
            await Task.Run(DisposeAsync).ConfigureAwait(false);
        }
        private Task Delay(TimeSpan duration) => Task.Delay(duration, owner.clock);
        private void Notify(Action? observer)
        {
            try { observer?.Invoke(); }
            catch (Exception) { Record(Pipe); }
        }
        private async Task ObserveAsync()
        {
            try
            {
                using var registration = caller.UnsafeRegister(static state => ((Operation)state!).Record(Cancel), this);
                var remaining = options.Timeout - owner.clock.GetElapsedTime(admitted);
                using var timer = owner.clock.CreateTimer(_ => Record(Timeout), null,
                    remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, System.Threading.Timeout.InfiniteTimeSpan);
                if (remaining <= TimeSpan.Zero) Record(Timeout);
                starting = Task.Run(StartAsync);
                await Task.WhenAny(starting, stop.Task).ConfigureAwait(false);
                if (starting.IsCompletedSuccessfully && starting.Result)
                {
                    StartIo();
                    while (!Stopping && !allIo!.IsCompleted)
                    {
                        await Task.WhenAny(root!, allIo, stop.Task).ConfigureAwait(false);
                        if (Stopping || allIo.IsCompleted) break;
                        if (root!.IsCompleted)
                        {
                            var eofGrace = Delay(owner.grace);
                            Notify(owner.hooks?.RootEofBudgetArmed);
                            await Task.WhenAny(allIo, stop.Task, eofGrace).ConfigureAwait(false);
                            if (!allIo.IsCompleted && !Stopping)
                            {
                                if (!output!.IsCompleted || !errors!.IsCompleted) owner.Poison(this);
                                Record(Drain);
                            }
                        }
                    }
                }
                var finalizing = Task.Run(FinalizeAsync);
                var budget = Delay(owner.grace);
                Notify(owner.hooks?.CleanupBudgetArmed);
                await Task.WhenAny(finalizing, budget).ConfigureAwait(false);
                if (!finalizing.IsCompleted)
                {
                    owner.Publish(this);
                }
                await finalizing.ConfigureAwait(false);
                owner.Released(this);
                owner.Publish(this);
            }
            catch (Exception)
            {
                // Unknown native/coordination failure never grants a released lease or loses ownership.
                Record(Pipe); owner.Poison(this); owner.Publish(this);
                await unknown.Task.ConfigureAwait(false);
            }
        }
    }
}
