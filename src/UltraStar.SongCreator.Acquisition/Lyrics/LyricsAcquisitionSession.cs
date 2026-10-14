using System.Text;
using Core = UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Acquisition.Lyrics;

public enum LyricsSessionStatus { Idle, Searching, Results, NoResults, Failed, Cancelled, ManualReady, Disposed }
public enum LyricsSessionOutcomeKind { Succeeded, Unchanged, Rejected, Failed, Cancelled, Superseded, Closed }
public enum LyricsSessionErrorKind { Provider, Validation, Unexpected }
public sealed record LyricsSessionError(LyricsSessionErrorKind Kind, LyricsSearchError? ProviderError = null,
    LyricsValidationError? ValidationError = null, int? HttpStatus = null, TimeSpan? RetryAfter = null);
public readonly record struct LyricsSessionOutcome(LyricsSessionOutcomeKind Kind, LyricsSessionError? Error = null);
public sealed record LyricsSessionSnapshot(long Revision, LyricsSessionStatus Status, LyricsQuery? Query = null,
    LyricsPage? Page = null, LyricsSelection? Selection = null, Core.SelectedLyrics? PreparedSelection = null,
    LyricsSessionError? Error = null);

/// <summary>Bounded transient acquisition state; borrows its provider and never mutates a song.</summary>
public sealed class LyricsAcquisitionSession : IAsyncDisposable
{
    private readonly object sync = new();
    private readonly ILyricsSearchProvider provider;
    private readonly string providerId;
    private LyricsSessionSnapshot current = new(0, LyricsSessionStatus.Idle);
    private Request? active, pending, latest;
    private bool running, closed;
    private TaskCompletionSource? drain;
    public event EventHandler? Changed;
    public LyricsSessionSnapshot Current { get { lock (sync) return current; } }

    private sealed class Request(LyricsQuery query, CancellationToken caller)
    {
        internal readonly LyricsQuery Query = query;
        internal readonly CancellationToken Caller = caller;
        internal readonly CancellationTokenSource Stop = new();
        internal readonly TaskCompletionSource<LyricsSessionOutcome> Result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal CancellationTokenRegistration Registration;
        internal bool Admitted, Cancelled;
        private const int Closing = 1 << 30;
        private int lifetime, disposed;
        private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal void Cancel()
        {
            // Atomic leases allow callbacks outside every lock, without racing CTS disposal.
            while (true)
            {
                var state = Volatile.Read(ref lifetime);
                if ((state & Closing) != 0) return;
                if (Interlocked.CompareExchange(ref lifetime, state + 1, state) == state) break;
            }
            try { Stop.Cancel(); }
            catch (AggregateException) { /* Cancellation observers cannot corrupt coordinator state. */ }
            finally { if (Interlocked.Decrement(ref lifetime) == Closing) Release(); }
        }
        internal Task Dispose()
        {
            var previous = Interlocked.Or(ref lifetime, Closing);
            if ((previous & (Closing - 1)) == 0) Release();
            return released.Task;
        }
        private void Release()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            Registration.Unregister();
            Stop.Dispose();
            released.TrySetResult();
        }
    }

    public LyricsAcquisitionSession(ILyricsSearchProvider provider)
    {
        this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        try
        {
            providerId = provider.ProviderId;
            if (string.IsNullOrWhiteSpace(providerId) || providerId.Length > LyricsValidation.MaxIdLength)
                throw new ArgumentException();
            new UTF8Encoding(false, true).GetByteCount(providerId);
        }
        catch (Exception) { throw new ArgumentException("Provider identity must be valid bounded text.", nameof(provider)); }
    }

    private void Alive() => ObjectDisposedException.ThrowIf(closed, this);
    private static LyricsSessionError Validation(LyricsValidationError error) =>
        new(LyricsSessionErrorKind.Validation, ValidationError: Enum.IsDefined(error) ? error : LyricsValidationError.InvalidSelection);
    private static LyricsSessionError Unexpected() => new(LyricsSessionErrorKind.Unexpected);
    private static LyricsSessionError ProviderError(LyricsSearchException error)
    {
        if (!Enum.IsDefined(error.Error)) return Unexpected();
        var status = error.StatusCode is >= 100 and <= 599 ? error.StatusCode : null;
        var retry = error.Error is LyricsSearchError.RateLimited or LyricsSearchError.Unavailable
            && error.RetryAfter >= TimeSpan.Zero ? error.RetryAfter : null;
        return new(LyricsSessionErrorKind.Provider, error.Error, HttpStatus: status, RetryAfter: retry);
    }
    private long State(LyricsSessionStatus status, LyricsQuery? query = null, LyricsPage? page = null,
        LyricsSelection? selection = null, Core.SelectedLyrics? prepared = null, LyricsSessionError? error = null)
    {
        current = new(checked(current.Revision + 1), status, query, page, selection, prepared, error);
        return current.Revision;
    }

    // Called under sync; Task continuations are always asynchronous. External effects happen afterward.
    private Request? Invalidate(LyricsSessionOutcomeKind outcome, out Request? abandoned)
    {
        abandoned = pending;
        pending = null;
        latest = null;
        active?.Result.TrySetResult(new(outcome));
        abandoned?.Result.TrySetResult(new(outcome));
        return active;
    }
    private static void Effects(Request? cancel, Request? abandoned)
    {
        cancel?.Cancel();
        abandoned?.Dispose();
    }
    private void Notify(long revision)
    {
        EventHandler? handlers;
        lock (sync)
        {
            if (current.Revision != revision) return;
            handlers = Changed;
        }
        if (handlers is null) return;
        foreach (EventHandler observer in handlers.GetInvocationList())
        {
            lock (sync) { if (closed && current.Revision != revision) return; }
            try { observer(this, EventArgs.Empty); }
            catch (Exception) { /* Isolate ordinary subscriber faults without exposing or logging data. */ }
        }
    }

    public Task<LyricsSessionOutcome> SearchAsync(LyricsQuery query, CancellationToken cancellationToken = default)
    {
        lock (sync) Alive();
        LyricsSessionError? invalid = null;
        try { LyricsValidation.Query(query); }
        catch (LyricsValidationException e) { invalid = Validation(e.Error); }
        if (invalid is not null)
        {
            Request? cancel, abandoned; long revision;
            lock (sync)
            {
                Alive();
                cancel = Invalidate(LyricsSessionOutcomeKind.Superseded, out abandoned);
                revision = State(LyricsSessionStatus.Failed, error: invalid);
            }
            Effects(cancel, abandoned); Notify(revision);
            return Task.FromResult(new LyricsSessionOutcome(LyricsSessionOutcomeKind.Failed, invalid));
        }
        var request = new Request(query, cancellationToken);
        request.Registration = cancellationToken.Register(() => CancelIntent(request));
        Request? old, discarded; long changed; bool start = false, cancelledAdmission = false;
        try
        {
            lock (sync)
            {
                Alive();
                old = Invalidate(LyricsSessionOutcomeKind.Superseded, out discarded);
                request.Admitted = true;
                if (request.Cancelled || cancellationToken.IsCancellationRequested)
                {
                    cancelledAdmission = true;
                    request.Result.TrySetResult(new(LyricsSessionOutcomeKind.Cancelled));
                    changed = State(LyricsSessionStatus.Cancelled, query);
                }
                else
                {
                    pending = latest = request;
                    changed = State(LyricsSessionStatus.Searching, query);
                    if (!running) { running = true; start = true; }
                }
            }
        }
        catch { request.Dispose(); throw; }
        Effects(old, discarded);
        if (cancelledAdmission) request.Dispose();
        Notify(changed);
        if (start) _ = PumpAsync();
        return request.Result.Task;
    }

    private void CancelIntent(Request request)
    {
        Request? discarded = null; long? revision = null;
        lock (sync)
        {
            request.Cancelled = true;
            if (!request.Admitted || closed) return;
            request.Result.TrySetResult(new(LyricsSessionOutcomeKind.Cancelled));
            if (ReferenceEquals(latest, request))
            {
                latest = null;
                if (ReferenceEquals(pending, request)) { pending = null; discarded = request; }
                revision = State(LyricsSessionStatus.Cancelled, request.Query);
            }
        }
        request.Cancel();
        discarded?.Dispose();
        if (revision is { } value) Notify(value);
    }

    private async Task PumpAsync()
    {
        while (true)
        {
            Request request;
            lock (sync)
            {
                if (pending is null)
                {
                    running = false;
                    drain?.TrySetResult();
                    return;
                }
                request = pending; pending = null; active = request;
            }
            LyricsPage? page = null; LyricsSessionError? error = null; bool cancelled = false;
            try
            {
                request.Stop.Token.ThrowIfCancellationRequested();
                page = await provider.SearchAsync(request.Query, request.Stop.Token).ConfigureAwait(false);
                LyricsValidation.Page(page);
                if (page.ProviderId != providerId || page.Query != request.Query)
                    throw new LyricsValidationException(LyricsValidationError.InvalidPage, "Provider result identity mismatch.");
            }
            catch (OperationCanceledException) { cancelled = true; }
            catch (LyricsSearchException e) { error = ProviderError(e); }
            catch (LyricsValidationException e)
            {
                error = new(LyricsSessionErrorKind.Provider, e.Error == LyricsValidationError.ResponseTooLarge
                    ? LyricsSearchError.ResponseTooLarge : LyricsSearchError.InvalidResponse);
            }
            catch (Exception) { error = Unexpected(); }
            finally { await request.Dispose().ConfigureAwait(false); }
            long? changed = null;
            lock (sync)
            {
                active = null;
                if (!closed && ReferenceEquals(latest, request))
                {
                    latest = null;
                    if (cancelled || request.Cancelled || request.Caller.IsCancellationRequested)
                    {
                        changed = State(LyricsSessionStatus.Cancelled, request.Query);
                        request.Result.TrySetResult(new(LyricsSessionOutcomeKind.Cancelled));
                    }
                    else if (error is not null)
                    {
                        changed = State(LyricsSessionStatus.Failed, request.Query, error: error);
                        request.Result.TrySetResult(new(LyricsSessionOutcomeKind.Failed, error));
                    }
                    else
                    {
                        changed = State(page!.Candidates.IsEmpty ? LyricsSessionStatus.NoResults : LyricsSessionStatus.Results, request.Query, page);
                        request.Result.TrySetResult(new(LyricsSessionOutcomeKind.Succeeded));
                    }
                }
            }
            if (changed is { } revision) Notify(revision);
        }
    }

    public LyricsSessionOutcome Select(long revision, string providerId, string candidateId, string? documentId)
    {
        LyricsSessionSnapshot snapshot;
        lock (sync)
        {
            Alive(); snapshot = current;
            if (snapshot.Revision != revision) return new(LyricsSessionOutcomeKind.Rejected, Validation(LyricsValidationError.InvalidSelection));
            if (snapshot.Status != LyricsSessionStatus.Results || snapshot.Page is null)
                return new(LyricsSessionOutcomeKind.Rejected, Validation(LyricsValidationError.InvalidSelection));
        }
        LyricsSelection? selection = null; Core.SelectedLyrics? prepared = null; LyricsSessionError? error = null;
        try
        {
            selection = LyricsSelections.Select(snapshot.Page, providerId, candidateId, documentId);
            prepared = SelectedLyricsProjection.FromSelection(selection);
        }
        catch (LyricsValidationException e) { error = Validation(e.Error); selection = null; prepared = null; }
        catch (Core.SelectedLyricsValidationException) { error = Validation(LyricsValidationError.InvalidSelection); selection = null; prepared = null; }
        long changed;
        lock (sync)
        {
            Alive();
            if (current.Revision != revision) return new(LyricsSessionOutcomeKind.Rejected, Validation(LyricsValidationError.InvalidSelection));
            changed = State(LyricsSessionStatus.Results, snapshot.Query, snapshot.Page, selection, prepared, error);
        }
        Notify(changed);
        return new(error is null ? LyricsSessionOutcomeKind.Succeeded : LyricsSessionOutcomeKind.Rejected, error);
    }

    public LyricsSessionOutcome SetManualText(string text)
    {
        lock (sync) Alive();
        LyricsSelection? selection = null; Core.SelectedLyrics? prepared = null; LyricsSessionError? error = null;
        try { selection = ManualLyrics.Create(text); prepared = SelectedLyricsProjection.FromSelection(selection); }
        catch (LyricsValidationException e) { error = Validation(e.Error); }
        catch (ArgumentNullException) { error = Validation(LyricsValidationError.InvalidDocument); }
        Request? cancel, abandoned; long changed;
        lock (sync)
        {
            Alive();
            cancel = Invalidate(LyricsSessionOutcomeKind.Superseded, out abandoned);
            changed = State(error is null ? LyricsSessionStatus.ManualReady : LyricsSessionStatus.Failed,
                selection: selection, prepared: prepared, error: error);
        }
        Effects(cancel, abandoned); Notify(changed);
        return new(error is null ? LyricsSessionOutcomeKind.Succeeded : LyricsSessionOutcomeKind.Failed, error);
    }

    public LyricsSessionOutcome Reset()
    {
        Request? cancel, abandoned; long changed;
        lock (sync)
        {
            Alive();
            cancel = Invalidate(LyricsSessionOutcomeKind.Superseded, out abandoned);
            changed = State(LyricsSessionStatus.Idle);
        }
        Effects(cancel, abandoned); Notify(changed);
        return new(LyricsSessionOutcomeKind.Succeeded);
    }
    public LyricsSessionOutcome Cancel()
    {
        Request? cancel, abandoned; long changed;
        lock (sync)
        {
            Alive();
            if (current.Status != LyricsSessionStatus.Searching) return new(LyricsSessionOutcomeKind.Unchanged);
            var query = current.Query;
            cancel = Invalidate(LyricsSessionOutcomeKind.Cancelled, out abandoned);
            changed = State(LyricsSessionStatus.Cancelled, query);
        }
        Effects(cancel, abandoned); Notify(changed);
        return new(LyricsSessionOutcomeKind.Cancelled);
    }

    public ValueTask DisposeAsync()
    {
        Request? cancel, abandoned; long changed; Task completion;
        lock (sync)
        {
            if (drain is not null) return new(drain.Task);
            closed = true;
            drain = new(TaskCreationOptions.RunContinuationsAsynchronously);
            completion = drain.Task;
            cancel = Invalidate(LyricsSessionOutcomeKind.Closed, out abandoned);
            changed = State(LyricsSessionStatus.Disposed);
            if (!running) drain.TrySetResult();
        }
        Effects(cancel, abandoned); Notify(changed);
        return new(completion);
    }
}
