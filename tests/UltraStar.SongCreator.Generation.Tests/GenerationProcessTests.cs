using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

namespace UltraStar.SongCreator.Generation.Tests;

public sealed class GenerationProcessTests
{
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(2);
    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static async Task Done(Task task) => await task.WaitAsync(TimeSpan.FromSeconds(15));
    private static async Task<T> Done<T>(Task<T> task) => await task.WaitAsync(TimeSpan.FromSeconds(15));

    private sealed class Clock : TimeProvider
    {
        private readonly object sync = new();
        private readonly List<Timer> timers = [];
        private long ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() { lock (sync) return ticks; }
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(GetTimestamp());
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new Timer(this, callback, state);
            lock (sync) { timers.Add(timer); timer.Set(dueTime, period); }
            return timer;
        }
        internal void Advance(TimeSpan amount)
        {
            List<(TimerCallback Callback, object? State)> due = [];
            lock (sync)
            {
                ticks += amount.Ticks;
                foreach (var timer in timers)
                    if (timer.Due <= ticks)
                    {
                        due.Add((timer.Callback, timer.State));
                        timer.Due = timer.Period > 0 ? ticks + timer.Period : long.MaxValue;
                    }
            }
            foreach (var (callback, state) in due) callback(state);
        }
        private sealed class Timer(Clock clock, TimerCallback callback, object? state) : ITimer
        {
            internal readonly TimerCallback Callback = callback;
            internal readonly object? State = state;
            internal long Due = long.MaxValue, Period;
            private bool disposed;
            internal void Set(TimeSpan due, TimeSpan period)
            {
                Due = due == Timeout.InfiniteTimeSpan ? long.MaxValue : clock.ticks + due.Ticks;
                Period = period == Timeout.InfiniteTimeSpan ? 0 : period.Ticks;
            }
            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (clock.sync) { if (disposed) return false; Set(dueTime, period); return true; }
            }
            public void Dispose() { lock (clock.sync) { disposed = true; Due = long.MaxValue; } }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
    private sealed class Gate(GenerationProcessPoint point) : IDisposable
    {
        internal readonly TaskCompletionSource Entered = Signal(), Release = Signal();
        public void Dispose() => Release.TrySetResult();
        internal async Task Before(GenerationProcessPoint current)
        {
            if (current != point) return;
            Entered.TrySetResult(); await Release.Task;
        }
    }
    private sealed class Control : IAsyncDisposable
    {
        internal readonly string Name = "usc-g1b-" + Guid.NewGuid().ToString("N");
        private readonly NamedPipeServerStream pipe;
        private readonly StreamReader reader;
        private readonly StreamWriter writer;
        internal Process? Process;
        internal Control()
        {
            pipe = new(Name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            reader = new(pipe, Encoding.UTF8, false, 1024, true);
            writer = new(pipe, new UTF8Encoding(false), 1024, true);
        }
        internal async Task Ready()
        {
            await Done(pipe.WaitForConnectionAsync());
            writer.AutoFlush = true;
            var id = await Done(reader.ReadLineAsync());
            Process = System.Diagnostics.Process.GetProcessById(int.Parse(id!));
        }
        internal async Task Send(string message) => await Done(writer.WriteLineAsync(message));
        internal async Task<string?> Read() => await Done(reader.ReadLineAsync());
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (Process is { } process)
                {
                    if (!process.HasExited) process.Kill(entireProcessTree: true);
                    await Done(process.WaitForExitAsync());
                }
            }
            finally
            {
                Process?.Dispose();
                try { writer.Dispose(); } catch (Exception e) when (e is IOException or InvalidOperationException) { }
                reader.Dispose(); await pipe.DisposeAsync();
            }
        }
    }
    private sealed class Terminal : IAsyncDisposable
    {
        internal readonly Control Root = new();
        internal readonly Control Child = new();
        internal readonly string Directory;
        private readonly string parent;
        internal Terminal()
        {
            var repository = new DirectoryInfo(AppContext.BaseDirectory);
            while (!File.Exists(Path.Combine(repository.FullName, "AGENTS.md")))
                repository = repository.Parent ?? throw new InvalidOperationException("Repository not found.");
            parent = Path.GetFullPath(Path.Combine(repository.FullName, ".agent-local", "phase1", "continuation", "generation-process-g1b", "test-workdirs"));
            Directory = Path.Combine(parent, Guid.NewGuid().ToString("N"), "literal [x] $% ; folder");
            System.IO.Directory.CreateDirectory(Directory);
        }
        internal GenerationProcessOptions Options(string mode = "hold", params string[] arguments)
        {
            var dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
            Assert.False(string.IsNullOrEmpty(dotnet));
            var host = Path.Combine(AppContext.BaseDirectory, "GenerationHost", "Generation.TestHost.dll");
            return new(dotnet!, [host, mode, Root.Name, .. arguments], Directory);
        }
        public async ValueTask DisposeAsync()
        {
            try { await Child.DisposeAsync(); }
            finally
            {
                await Root.DisposeAsync();
                var target = Path.GetFullPath(System.IO.Directory.GetParent(Directory)!.FullName);
                Assert.StartsWith(parent + Path.DirectorySeparatorChar, target, StringComparison.Ordinal);
                System.IO.Directory.Delete(target, true);
            }
        }
    }

    [Fact]
    public async Task LiteralArgumentsCwdClosedStdinAndPublishedResultSurviveLateCancellation()
    {
        await using var terminal = new Terminal();
        var runner = new GenerationProcess();
        using var cancel = new CancellationTokenSource();
        var args = new[] { "space value", "\"quoted\"", "$(%literal);&[]", "\u00e6\u00f8\u00e5", "", "trailing\\" };
        var run = runner.RunAsync(terminal.Options("echo", args), cancel.Token);
        await terminal.Root.Ready(); await terminal.Root.Send("go");
        var result = await Done(run);
        Assert.Equal(GenerationProcessReason.Success, result.Reason);
        Assert.Equal(string.Join('\u001f', args) + "|" + terminal.Directory + "|0", Encoding.UTF8.GetString(result.Stdout.AsSpan()));
        Assert.Empty(result.Stderr); Assert.Equal(0, result.ExitCode);
        Assert.Equal(GenerationCleanupState.LocalReleased, result.Cleanup.State);
        await Done(result.Cleanup.WhenLocalReleased);
        cancel.Cancel(); Assert.Equal(GenerationProcessReason.Success, result.Reason);
        var close = runner.CloseAsync(); Assert.Same(close, runner.CloseAsync());
        Assert.Same(result.Cleanup, await Done(close));
        Assert.Equal(GenerationProcessReason.Closed, (await runner.RunAsync(terminal.Options())).Reason);
    }

    [Theory]
    [InlineData(1024, 64, false)]
    [InlineData(1025, 0, true)]
    [InlineData(0, 65, true)]
    [InlineData(2097152, 2097152, true)]
    public async Task ExactOpaqueByteLimitsAndBothFloods(int stdout, int stderr, bool exceeded)
    {
        await using var terminal = new Terminal(); var runner = new GenerationProcess();
        var run = runner.RunAsync(terminal.Options("bytes", stdout.ToString(), stderr.ToString()) with { StdoutLimit = 1024, StderrLimit = 64 });
        await terminal.Root.Ready(); await terminal.Root.Send("go");
        var result = await Done(run);
        Assert.Equal(exceeded ? GenerationProcessReason.OutputLimit : GenerationProcessReason.Success, result.Reason);
        await Done(result.Cleanup.WhenLocalReleased);
        if (exceeded) { Assert.Empty(result.Stdout); Assert.Empty(result.Stderr); }
        else
        {
            Assert.Equal(stdout, result.Stdout.Length); Assert.All(result.Stdout, b => Assert.Equal(0xFF, b));
            Assert.Equal(stderr, result.Stderr.Length); Assert.All(result.Stderr, b => Assert.Equal(0xFE, b));
        }
        await Done(runner.CloseAsync());
    }

    [Fact]
    public async Task NonzeroAndUnavailableAreSanitizedWithoutOutputOrThrownNativeText()
    {
        await using var terminal = new Terminal(); var runner = new GenerationProcess();
        var run = runner.RunAsync(terminal.Options("nonzero"));
        await terminal.Root.Ready(); await terminal.Root.Send("go");
        var result = await Done(run);
        Assert.Equal(GenerationProcessReason.NonZeroExit, result.Reason); Assert.Equal(7, result.ExitCode);
        await Done(result.Cleanup.WhenLocalReleased);
        var unavailable = await Done(runner.RunAsync(terminal.Options() with { Executable = Path.Combine(terminal.Directory, "missing-own-executable.exe") }));
        Assert.Equal(GenerationProcessReason.Unavailable, unavailable.Reason);
        Assert.Null(unavailable.ExitCode); Assert.Empty(unavailable.Stdout); Assert.Empty(unavailable.Stderr);
        await Done(unavailable.Cleanup.WhenLocalReleased); await Done(runner.CloseAsync());
    }

    [Fact]
    public async Task BusyInvalidAndPrecancelDoNotStopAnAdmittedOperation()
    {
        await using var terminal = new Terminal(); var runner = new GenerationProcess();
        var run = runner.RunAsync(terminal.Options());
        await terminal.Root.Ready(); await terminal.Root.Send("go");
        Assert.Equal(GenerationProcessReason.Busy, (await runner.RunAsync(terminal.Options())).Reason);
        Assert.Equal(GenerationProcessReason.InvalidInvocation, (await runner.RunAsync(terminal.Options() with { Executable = "relative.exe" })).Reason);
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.Equal(GenerationProcessReason.Cancelled, (await runner.RunAsync(terminal.Options(), stop.Token)).Reason);
        Assert.False(run.IsCompleted); Assert.False(terminal.Root.Process!.HasExited);
        await terminal.Root.Send("exit");
        Assert.Equal(GenerationProcessReason.Success, (await Done(run)).Reason);
        await using var next = new Terminal();
        var second = runner.RunAsync(next.Options("echo", "next"));
        await next.Root.Ready(); await next.Root.Send("go");
        Assert.Equal(GenerationProcessReason.Success, (await Done(second)).Reason);
        await Done(runner.CloseAsync());
    }

    [Theory]
    [InlineData("cancel", (int)GenerationProcessReason.Cancelled)]
    [InlineData("close", (int)GenerationProcessReason.CloseRequested)]
    [InlineData("deadline", (int)GenerationProcessReason.TimedOut)]
    public async Task HangingLeafStopsAndRepeatedCloseSharesCleanup(string trigger, int reasonValue)
    {
        var reason = (GenerationProcessReason)reasonValue;
        await using var terminal = new Terminal(); var clock = new Clock();
        var runner = new GenerationProcess(clock);
        using var cancel = new CancellationTokenSource();
        var run = runner.RunAsync(terminal.Options(), cancel.Token);
        await terminal.Root.Ready(); await terminal.Root.Send("go");
        Task<GenerationProcessCleanup>? close = null;
        if (trigger == "cancel") cancel.Cancel();
        else if (trigger == "close") close = runner.CloseAsync();
        else clock.Advance(TimeSpan.FromSeconds(30));
        var result = await Done(run);
        Assert.Equal(reason, result.Reason);
        await Done(result.Cleanup.WhenLocalReleased); Assert.True(terminal.Root.Process!.HasExited);
        close ??= runner.CloseAsync(); Assert.Same(close, runner.CloseAsync()); Assert.Same(result.Cleanup, await Done(close));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StalledStartRetainsLeaseWithoutLaunchingAfterStopAndCannotBeBypassed(bool deadline)
    {
        await using var terminal = new Terminal(); var clock = new Clock(); using var gate = new Gate(GenerationProcessPoint.Start);
        var armed = Signal(); var runner = new GenerationProcess(clock, Grace, new() { Before = gate.Before, CleanupBudgetArmed = () => armed.TrySetResult() });
        using var cancel = new CancellationTokenSource();
        var run = runner.RunAsync(terminal.Options(), cancel.Token);
        await Done(gate.Entered.Task);
        if (deadline) clock.Advance(TimeSpan.FromSeconds(30)); else cancel.Cancel();
        await Done(armed.Task);
        clock.Advance(Grace);
        var result = await Done(run);
        Assert.Equal(deadline ? GenerationProcessReason.TimedOut : GenerationProcessReason.Cancelled, result.Reason);
        Assert.Equal(GenerationCleanupState.Pending, result.Cleanup.State);
        Assert.False(result.Cleanup.WhenLocalReleased.IsCompleted);
        Assert.Equal(GenerationProcessReason.Busy, (await runner.RunAsync(terminal.Options())).Reason);
        gate.Release.TrySetResult();
        await Done(result.Cleanup.WhenLocalReleased);
        Assert.Equal(GenerationCleanupState.LocalReleased, result.Cleanup.State);
        await Done(runner.CloseAsync());
    }

    [Theory]
    [InlineData((int)GenerationProcessPoint.Kill)]
    [InlineData((int)GenerationProcessPoint.Dispose)]
    public async Task NativeOperationStallIsOwnedAfterBoundedReturn(int pointValue)
    {
        var point = (GenerationProcessPoint)pointValue;
        await using var terminal = new Terminal(); var clock = new Clock(); using var gate = new Gate(point);
        var armed = Signal(); var runner = new GenerationProcess(clock, Grace, new() { Before = gate.Before, CleanupBudgetArmed = () => armed.TrySetResult() });
        using var cancel = new CancellationTokenSource();
        var run = runner.RunAsync(terminal.Options(point == GenerationProcessPoint.Dispose ? "echo" : "hold"), cancel.Token);
        await terminal.Root.Ready(); await terminal.Root.Send("go");
        if (point == GenerationProcessPoint.Kill) cancel.Cancel();
        await Done(gate.Entered.Task); await Done(armed.Task); clock.Advance(Grace);
        var result = await Done(run);
        Assert.Equal(point == GenerationProcessPoint.Kill ? GenerationProcessReason.Cancelled : GenerationProcessReason.DrainIncomplete, result.Reason);
        Assert.Equal(GenerationCleanupState.Pending, result.Cleanup.State); Assert.False(result.Cleanup.WhenLocalReleased.IsCompleted);
        Assert.Equal(GenerationProcessReason.Busy, (await runner.RunAsync(terminal.Options())).Reason);
        gate.Release.TrySetResult(); await Done(result.Cleanup.WhenLocalReleased);
        Assert.Equal(GenerationCleanupState.LocalReleased, result.Cleanup.State);
        await Done(runner.CloseAsync());
    }

    [Theory]
    [InlineData("cancel", (int)GenerationProcessReason.Cancelled)]
    [InlineData("close", (int)GenerationProcessReason.CloseRequested)]
    [InlineData("limit", (int)GenerationProcessReason.OutputLimit)]
    [InlineData("pipe", (int)GenerationProcessReason.PipeFailure)]
    public async Task ObservedReasonPriorityIsFrozenBeforeRelease(string higher, int reasonValue)
    {
        var reason = (GenerationProcessReason)reasonValue;
        await using var terminal = new Terminal(); var clock = new Clock(); using var gate = new Gate(GenerationProcessPoint.Dispose);
        var armed = Signal(); using var faultReady = new Gate(GenerationProcessPoint.StdoutRead);
        async Task Before(GenerationProcessPoint point)
        {
            if (point == GenerationProcessPoint.StdoutRead && higher == "pipe")
            { await faultReady.Before(point); throw new IOException("private fixture detail"); }
            await gate.Before(point);
        }
        var runner = new GenerationProcess(clock, Grace, new() { Before = Before, CleanupBudgetArmed = () => armed.TrySetResult() });
        using var cancel = new CancellationTokenSource();
        var run = runner.RunAsync(terminal.Options(higher == "limit" ? "bytes" : "hold", "2097152", "0"), cancel.Token);
        await terminal.Root.Ready(); await terminal.Root.Send("go");
        if (higher == "cancel") { cancel.Cancel(); _ = runner.CloseAsync(); }
        else if (higher == "close") _ = runner.CloseAsync();
        else if (higher == "pipe") faultReady.Release.TrySetResult();
        await Done(gate.Entered.Task); await Done(armed.Task);
        clock.Advance(TimeSpan.FromSeconds(30));
        var result = await Done(run);
        Assert.Equal(reason, result.Reason);
        gate.Release.TrySetResult(); await Done(result.Cleanup.WhenLocalReleased); await Done(runner.CloseAsync());
    }

    [Theory]
    [InlineData((int)GenerationProcessPoint.Start, (int)GenerationProcessReason.Unavailable)]
    [InlineData((int)GenerationProcessPoint.StdoutRead, (int)GenerationProcessReason.PipeFailure)]
    [InlineData((int)GenerationProcessPoint.Kill, (int)GenerationProcessReason.PipeFailure)]
    [InlineData((int)GenerationProcessPoint.Dispose, (int)GenerationProcessReason.PipeFailure)]
    public async Task HookFaultsUseSameCleanupPathWithoutLeakingExceptions(int pointValue, int reasonValue)
    {
        var point = (GenerationProcessPoint)pointValue; var reason = (GenerationProcessReason)reasonValue;
        await using var terminal = new Terminal(); using var faultReady = new Gate(GenerationProcessPoint.StdoutRead);
        var runner = new GenerationProcess(null, null, new()
        {
            Before = async p =>
            {
                if (p != point) return;
                if (p == GenerationProcessPoint.StdoutRead) await faultReady.Before(p);
                throw new IOException("private fixture detail");
            }
        });
        var run = runner.RunAsync(terminal.Options(point == GenerationProcessPoint.Kill ? "hold" : "echo"));
        if (point != GenerationProcessPoint.Start)
        {
            await terminal.Root.Ready(); await terminal.Root.Send("go");
            if (point == GenerationProcessPoint.Kill) _ = runner.CloseAsync();
            if (point == GenerationProcessPoint.StdoutRead) faultReady.Release.TrySetResult();
        }
        var result = await Done(run);
        Assert.Equal(point == GenerationProcessPoint.Kill ? GenerationProcessReason.CloseRequested : reason, result.Reason);
        Assert.Empty(result.Stdout); Assert.Empty(result.Stderr);
        await Done(result.Cleanup.WhenLocalReleased); await Done(runner.CloseAsync());
    }

    [Fact]
    public async Task InheritedWriterAfterRootExitQuarantinesPermanentlyEvenAfterLocalRelease()
    {
        await using var terminal = new Terminal(); var clock = new Clock();
        var eof = Signal(); var cleanup = Signal();
        var runner = new GenerationProcess(clock, Grace, new()
        { RootEofBudgetArmed = () => eof.TrySetResult(), CleanupBudgetArmed = () => cleanup.TrySetResult() });
        var run = runner.RunAsync(terminal.Options("spawn", terminal.Child.Name));
        await terminal.Root.Ready(); await terminal.Root.Send("go");
        await terminal.Child.Ready(); await terminal.Child.Send("go"); Assert.Equal("spawned", await terminal.Root.Read());
        await terminal.Root.Send("exit"); await Done(terminal.Root.Process!.WaitForExitAsync());
        await Done(eof.Task); clock.Advance(Grace); await Done(cleanup.Task); clock.Advance(Grace);
        var result = await Done(run);
        Assert.Equal(GenerationProcessReason.DrainIncomplete, result.Reason);
        Assert.Equal(GenerationCleanupState.Quarantined, result.Cleanup.State);
        Assert.Equal(GenerationProcessReason.Closed, (await runner.RunAsync(terminal.Options())).Reason);
        await terminal.Child.Send("exit"); await Done(terminal.Child.Process!.WaitForExitAsync());
        await Done(result.Cleanup.WhenLocalReleased);
        Assert.Equal(GenerationCleanupState.Quarantined, result.Cleanup.State);
        Assert.Equal(GenerationProcessReason.Closed, (await runner.RunAsync(terminal.Options())).Reason);
        var close = runner.CloseAsync(); Assert.Same(close, runner.CloseAsync()); Assert.Same(result.Cleanup, await Done(close));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StalledReaderAndPipeClosureKeepQuarantineUntilBothOwnedTasksSettle(bool failClose)
    {
        await using var terminal = new Terminal(); var clock = new Clock();
        using var read = new Gate(GenerationProcessPoint.StdoutRead);
        using var closing = new Gate(GenerationProcessPoint.PipeClose);
        var eof = Signal(); var budget = Signal();
        var runner = new GenerationProcess(clock, Grace, new()
        {
            Before = async point =>
            {
                await read.Before(point);
                await closing.Before(point);
                if (point == GenerationProcessPoint.PipeClose && failClose) throw new IOException("private fixture detail");
            },
            RootEofBudgetArmed = () => eof.TrySetResult(),
            CleanupBudgetArmed = () => budget.TrySetResult()
        });
        var run = runner.RunAsync(terminal.Options("echo"));
        await terminal.Root.Ready(); await terminal.Root.Send("go");
        await Done(read.Entered.Task); await Done(eof.Task);
        clock.Advance(Grace); await Done(budget.Task); await Done(closing.Entered.Task);
        clock.Advance(Grace);
        var result = await Done(run);
        Assert.Equal(GenerationProcessReason.DrainIncomplete, result.Reason);
        Assert.Equal(GenerationCleanupState.Quarantined, result.Cleanup.State);
        Assert.False(result.Cleanup.WhenLocalReleased.IsCompleted);
        read.Release.TrySetResult();
        Assert.False(result.Cleanup.WhenLocalReleased.IsCompleted);
        closing.Release.TrySetResult();
        await Done(result.Cleanup.WhenLocalReleased);
        Assert.Equal(GenerationCleanupState.Quarantined, result.Cleanup.State);
        Assert.Equal(GenerationProcessReason.Closed, (await runner.RunAsync(terminal.Options())).Reason);
        Assert.Equal(GenerationProcessReason.DrainIncomplete, result.Reason); // Late fault cannot rewrite publication.
        await Done(runner.CloseAsync());
    }

    [Theory]
    [InlineData("relative.exe", true)]
    [InlineData("https://invalid.example/tool", true)]
    [InlineData("", false)]
    [InlineData("bad\0argument", false)]
    [InlineData("unpaired-surrogate", false)]
    public async Task InvalidTextStartsNothingAndLeavesRunnerReusable(string value, bool executable)
    {
        await using var terminal = new Terminal(); var runner = new GenerationProcess();
        if (value == "unpaired-surrogate") value = new string((char)0xd800, 1);
        var options = terminal.Options();
        options = executable ? options with { Executable = value } : options with { Arguments = [value] };
        // Empty arguments are legal; use an invalid working directory for that row.
        if (value.Length == 0) options = options with { WorkingDirectory = value };
        Assert.Equal(GenerationProcessReason.InvalidInvocation, (await runner.RunAsync(options)).Reason);
        var run = runner.RunAsync(terminal.Options("echo"));
        await terminal.Root.Ready(); await terminal.Root.Send("go");
        Assert.Equal(GenerationProcessReason.Success, (await Done(run)).Reason);
        await Done(runner.CloseAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public async Task InvalidLimitsAndArgumentCollectionsStartNothing(int invalid)
    {
        await using var terminal = new Terminal(); var runner = new GenerationProcess();
        var options = terminal.Options();
        options = invalid switch
        {
            0 => options with { Timeout = TimeSpan.Zero },
            1 => options with { Timeout = TimeSpan.FromMinutes(10) + TimeSpan.FromTicks(1) },
            2 => options with { StdoutLimit = 0 },
            3 => options with { StdoutLimit = 32 * 1024 * 1024 + 1 },
            4 => options with { StderrLimit = 64 * 1024 + 1 },
            5 => options with { Arguments = default },
            6 => options with { Arguments = [.. Enumerable.Repeat("x", 257)] },
            _ => options with { Arguments = [new string('x', 65537)] }
        };
        var result = await runner.RunAsync(options);
        Assert.Equal(GenerationProcessReason.InvalidInvocation, result.Reason);
        Assert.Equal(GenerationCleanupState.LocalReleased, result.Cleanup.State);
        await Done(result.Cleanup.WhenLocalReleased); await Done(runner.CloseAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeadlineBeforePublicationOutranksNonzeroOrUnavailable(bool missing)
    {
        await using var terminal = new Terminal(); var clock = new Clock();
        using var gate = new Gate(GenerationProcessPoint.Dispose);
        var armed = Signal();
        var runner = new GenerationProcess(clock, Grace, new() { Before = gate.Before, CleanupBudgetArmed = () => armed.TrySetResult() });
        var options = terminal.Options("nonzero");
        if (missing) options = options with { Executable = Path.Combine(terminal.Directory, "missing.exe") };
        var run = runner.RunAsync(options);
        if (!missing) { await terminal.Root.Ready(); await terminal.Root.Send("go"); }
        await Done(gate.Entered.Task); await Done(armed.Task);
        clock.Advance(TimeSpan.FromSeconds(30));
        var result = await Done(run);
        Assert.Equal(GenerationProcessReason.TimedOut, result.Reason);
        Assert.Equal(GenerationCleanupState.Pending, result.Cleanup.State);
        gate.Release.TrySetResult(); await Done(result.Cleanup.WhenLocalReleased); await Done(runner.CloseAsync());
    }

    [Fact]
    public async Task ChildWithSeparatePipesMayOutliveSuccessfulRootWithoutAnyContainmentClaim()
    {
        await using var terminal = new Terminal(); var runner = new GenerationProcess();
        var run = runner.RunAsync(terminal.Options("detached", terminal.Child.Name));
        await terminal.Root.Ready(); await terminal.Root.Send("go");
        await terminal.Child.Ready(); await terminal.Child.Send("go"); Assert.Equal("spawned", await terminal.Root.Read());
        await terminal.Root.Send("exit");
        var result = await Done(run);
        Assert.Equal(GenerationProcessReason.Success, result.Reason);
        Assert.Equal(GenerationCleanupState.LocalReleased, result.Cleanup.State);
        Assert.False(terminal.Child.Process!.HasExited); // This explicitly demonstrates the no-child precondition.
        await terminal.Child.Send("exit"); await Done(terminal.Child.Process.WaitForExitAsync()); await Done(runner.CloseAsync());
    }
}
