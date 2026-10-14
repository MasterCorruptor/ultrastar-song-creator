using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using UltraStar.SongCreator.Acquisition.Lyrics;
using Core = UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class LyricsAcquisitionSessionTests
{
    private static readonly LyricsQuery Query = new("Egen artist", "Egen tittel");
    private static readonly DateTimeOffset At = new(2026, 10, 14, 10, 0, 0, TimeSpan.Zero);
    private static LyricsCandidate Candidate(LyricsTextStatus status = LyricsTextStatus.Available) => new()
    {
        ProviderId = "fixture",
        Id = "candidate",
        RetrievedAtUtc = At,
        TextStatus = status,
        Documents = status == LyricsTextStatus.Available
            ? [new("plain", " Egen æøå \n", LyricsSyncLevel.Plain, []),
               new("line", " Egen æøå \n", LyricsSyncLevel.Line, [new(" Egen æøå ", 1, null, [])])] : [],
        Capabilities = new(true, true) { UnsupportedFormats = status == LyricsTextStatus.Unsupported ? ["opaque"] : [] }
    };
    private static LyricsPage Page(LyricsQuery query, params LyricsCandidate[] candidates) => new("fixture", query, candidates.ToImmutableArray(), At);
    private static async Task<T> Done<T>(Task<T> task) => await task.WaitAsync(TimeSpan.FromSeconds(5));

    private sealed class Call(LyricsQuery query, CancellationToken token)
    {
        internal LyricsQuery Query { get; } = query;
        internal CancellationToken Token { get; } = token;
        internal TaskCompletionSource<LyricsPage> Reply { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal void Complete() => Reply.TrySetResult(Page(Query, Candidate()));
    }
    private sealed class Controlled : ILyricsSearchProvider, IDisposable
    {
        public string ProviderId => "fixture";
        internal readonly ConcurrentQueue<Call> Waiting = new();
        internal readonly ConcurrentBag<Call> All = [];
        private readonly SemaphoreSlim arrived = new(0);
        internal int Count, InFlight, Maximum;
        internal bool Disposed;
        public async Task<LyricsPage> SearchAsync(LyricsQuery query, CancellationToken token)
        {
            var count = Interlocked.Increment(ref InFlight);
            int previous;
            do { previous = Maximum; if (previous >= count) break; }
            while (Interlocked.CompareExchange(ref Maximum, count, previous) != previous);
            Interlocked.Increment(ref Count);
            var call = new Call(query, token); All.Add(call); Waiting.Enqueue(call); arrived.Release();
            try { return await call.Reply.Task.ConfigureAwait(false); }
            finally { Interlocked.Decrement(ref InFlight); }
        }
        internal async Task<Call> Next()
        {
            await arrived.WaitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(Waiting.TryDequeue(out var call)); return call!;
        }
        public void Dispose() { Disposed = true; arrived.Dispose(); }
    }
    private sealed class Fixture : IAsyncDisposable
    {
        internal Controlled Provider { get; } = new();
        internal LyricsAcquisitionSession Session { get; }
        internal Fixture() => Session = new(Provider);
        public async ValueTask DisposeAsync()
        {
            var drain = Session.DisposeAsync().AsTask();
            foreach (var call in Provider.All) call.Reply.TrySetCanceled();
            await drain.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(Provider.Disposed);
            Provider.Dispose();
        }
    }
    private sealed class Immediate(Func<LyricsQuery, CancellationToken, Task<LyricsPage>> reply, string id = "fixture") : ILyricsSearchProvider
    {
        public string ProviderId => id;
        internal int Calls;
        public Task<LyricsPage> SearchAsync(LyricsQuery q, CancellationToken t) { Calls++; return reply(q, t); }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SuccessAndEmptyResultsKeepOriginalQueryAndNeverPickAutomatically(bool empty)
    {
        var provider = new Immediate((q, _) => Task.FromResult(empty ? Page(q) : Page(q, Candidate())));
        await using var session = new LyricsAcquisitionSession(provider);
        Assert.Equal(LyricsSessionStatus.Idle, session.Current.Status);
        var result = await Done(session.SearchAsync(Query));
        Assert.Equal(LyricsSessionOutcomeKind.Succeeded, result.Kind);
        Assert.Equal(empty ? LyricsSessionStatus.NoResults : LyricsSessionStatus.Results, session.Current.Status);
        Assert.Same(Query, session.Current.Query); Assert.Null(session.Current.Selection); Assert.Null(session.Current.PreparedSelection);
        Assert.Null(session.Current.Error); Assert.Equal(1, provider.Calls);
    }

    [Theory]
    [InlineData(LyricsSearchError.NetworkFailure)]
    [InlineData(LyricsSearchError.TimedOut)]
    [InlineData(LyricsSearchError.HttpFailure)]
    [InlineData(LyricsSearchError.RateLimited)]
    [InlineData(LyricsSearchError.Unavailable)]
    [InlineData(LyricsSearchError.ResponseTooLarge)]
    [InlineData(LyricsSearchError.InvalidResponse)]
    public async Task ProviderFailuresAreTypedAndNeverExposeMessagesOrInnerExceptions(LyricsSearchError error)
    {
        var provider = new Immediate((_, _) => throw new LyricsSearchException(error, "secret-payload", 429, TimeSpan.FromSeconds(7), new Exception("secret-inner")));
        await using var session = new LyricsAcquisitionSession(provider);
        var outcome = await Done(session.SearchAsync(Query));
        Assert.Equal(LyricsSessionOutcomeKind.Failed, outcome.Kind); Assert.Equal(LyricsSessionStatus.Failed, session.Current.Status);
        Assert.Equal(error, session.Current.Error!.ProviderError); Assert.Null(session.Current.Page);
        Assert.Equal(error is LyricsSearchError.RateLimited or LyricsSearchError.Unavailable ? TimeSpan.FromSeconds(7) : null, outcome.Error!.RetryAfter);
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(session.Current));
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(outcome));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidFaultMetadataAndUnexpectedFaultsAreSanitized(bool undefined)
    {
        var provider = new Immediate((_, _) => throw new LyricsSearchException(undefined ? (LyricsSearchError)999 : LyricsSearchError.RateLimited,
            "secret-payload", -123, TimeSpan.FromSeconds(-1)));
        await using var session = new LyricsAcquisitionSession(provider);
        var error = (await Done(session.SearchAsync(Query))).Error!;
        Assert.Equal(undefined ? LyricsSessionErrorKind.Unexpected : LyricsSessionErrorKind.Provider, error.Kind);
        Assert.Null(error.HttpStatus); Assert.Null(error.RetryAfter); Assert.DoesNotContain("secret", JsonSerializer.Serialize(error));
        var unexpected = new Immediate((_, _) => throw new InvalidOperationException("secret-inner"));
        await using var other = new LyricsAcquisitionSession(unexpected);
        Assert.Equal(LyricsSessionErrorKind.Unexpected, (await Done(other.SearchAsync(Query))).Error!.Kind);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("query")]
    [InlineData("provider")]
    [InlineData("candidate")]
    [InlineData("oversize")]
    public async Task BadReturnedPagesAreRejectedBeforeSelection(string defect)
    {
        var provider = new Immediate((q, _) =>
        {
            LyricsPage? page = Page(q, Candidate());
            page = defect switch
            {
                "null" => null,
                "query" => page with { Query = q with { Title = "Other" } },
                "provider" => page with { ProviderId = "other", Candidates = [Candidate() with { ProviderId = "other" }] },
                "candidate" => page with { Candidates = [Candidate() with { RetrievedAtUtc = default }] },
                "oversize" => page with { Candidates = [Candidate() with { Documents = [new("plain", new string('a', LyricsValidation.MaxDocumentBytes), LyricsSyncLevel.Plain, [])] }] },
                _ => page
            };
            return Task.FromResult(page!);
        });
        await using var session = new LyricsAcquisitionSession(provider);
        var outcome = await Done(session.SearchAsync(Query));
        Assert.Equal(defect == "oversize" ? LyricsSearchError.ResponseTooLarge : LyricsSearchError.InvalidResponse, outcome.Error!.ProviderError);
        Assert.Null(session.Current.Page); Assert.Null(session.Current.PreparedSelection);
    }

    [Fact]
    public async Task InvalidInputSupersedesActiveWorkWithoutRetainingUnboundedQuery()
    {
        await using var f = new Fixture();
        var old = f.Session.SearchAsync(Query); var call = await f.Provider.Next();
        var invalid = await Done(f.Session.SearchAsync(Query with { Title = new string('a', 4097) }));
        Assert.Equal(LyricsSessionErrorKind.Validation, invalid.Error!.Kind);
        Assert.Equal(LyricsValidationError.InvalidQuery, invalid.Error.ValidationError);
        Assert.Null(f.Session.Current.Query); Assert.Null(f.Session.Current.Page);
        Assert.Equal(LyricsSessionOutcomeKind.Superseded, (await Done(old)).Kind);
        call.Complete();
        await f.Session.DisposeAsync();
        Assert.Equal(1, f.Provider.Count);
    }

    [Fact]
    public async Task ExplicitSelectionFreezesSourceTimesAndInvalidChoiceRetainsValidatedPage()
    {
        var provider = new Immediate((q, _) => Task.FromResult(Page(q, Candidate())));
        await using var session = new LyricsAcquisitionSession(provider);
        await session.SearchAsync(Query);
        var page = session.Current.Page;
        var valid = session.Select(session.Current.Revision, "fixture", "candidate", "line");
        Assert.Equal(LyricsSessionOutcomeKind.Succeeded, valid.Kind);
        var prepared = session.Current.PreparedSelection!;
        Assert.Equal(Core.SelectedLyricsSyncLevel.Line, prepared.Document!.SyncLevel);
        Assert.Equal(Core.SelectedLyricsTimeReference.LyricsSourceSeconds, prepared.Document.TimeReference);
        Assert.Equal(1, prepared.Document.Lines[0].StartSeconds); Assert.Null(prepared.Document.Lines[0].EndSeconds);
        Assert.Equal(At, prepared.Provenance.RetrievedAtUtc);
        var invalid = session.Select(session.Current.Revision, "fixture", "candidate", "word-not-present");
        Assert.Equal(LyricsSessionOutcomeKind.Rejected, invalid.Kind);
        Assert.Same(page, session.Current.Page); Assert.Null(session.Current.Selection); Assert.Null(session.Current.PreparedSelection);
        Assert.Equal(LyricsSessionStatus.Results, session.Current.Status); Assert.NotNull(session.Current.Error);
        Assert.Equal(LyricsSessionOutcomeKind.Succeeded, session.Select(session.Current.Revision, "fixture", "candidate", "plain").Kind);
        Assert.Null(session.Current.Error);
    }

    [Theory]
    [InlineData(LyricsTextStatus.Instrumental, true)]
    [InlineData(LyricsTextStatus.Missing, false)]
    [InlineData(LyricsTextStatus.Unsupported, false)]
    public async Task InstrumentalMustBeExplicitAndUnavailableCandidatesCannotBeApplied(LyricsTextStatus status, bool valid)
    {
        var provider = new Immediate((q, _) => Task.FromResult(Page(q, Candidate(status))));
        await using var session = new LyricsAcquisitionSession(provider); await session.SearchAsync(Query);
        Assert.Null(session.Current.PreparedSelection);
        var outcome = session.Select(session.Current.Revision, "fixture", "candidate", null);
        Assert.Equal(valid ? LyricsSessionOutcomeKind.Succeeded : LyricsSessionOutcomeKind.Rejected, outcome.Kind);
        if (valid) { Assert.Equal(Core.SelectedLyricsKind.Instrumental, session.Current.PreparedSelection!.Kind); Assert.Null(session.Current.PreparedSelection.Document); }
        else Assert.Null(session.Current.PreparedSelection);
    }

    [Fact]
    public async Task StaleSelectPreservesNewPreviewAndDoesNotNotify()
    {
        var provider = new Immediate((q, _) => Task.FromResult(Page(q, Candidate())));
        await using var session = new LyricsAcquisitionSession(provider); await session.SearchAsync(Query);
        var stale = session.Current.Revision;
        session.Select(stale, "fixture", "candidate", "plain");
        var snapshot = session.Current; var notifications = 0;
        session.Changed += (_, _) => notifications++;
        Assert.Equal(LyricsSessionOutcomeKind.Rejected, session.Select(stale, "wrong", "wrong", null).Kind);
        Assert.Same(snapshot, session.Current); Assert.Equal(0, notifications);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidManualInputIsTypedAndHasNoApplicablePreview(string? text)
    {
        await using var f = new Fixture();
        f.Session.SetManualText("Egen tekst");
        var result = f.Session.SetManualText(text!);
        Assert.Equal(LyricsSessionOutcomeKind.Failed, result.Kind);
        Assert.Equal(LyricsSessionErrorKind.Validation, result.Error!.Kind);
        Assert.Equal(LyricsSessionStatus.Failed, f.Session.Current.Status);
        Assert.Null(f.Session.Current.PreparedSelection); Assert.Null(f.Session.Current.Selection); Assert.Equal(0, f.Provider.Count);
    }

    [Fact]
    public async Task ManualPreviewIsOneStablePayloadAndRemainsUsableWhileProviderIgnoresCancel()
    {
        await using var f = new Fixture();
        var first = f.Session.SearchAsync(Query); var call = await f.Provider.Next();
        var queued = f.Session.SearchAsync(Query with { Title = "Pending" });
        Assert.Equal(LyricsSessionOutcomeKind.Succeeded, f.Session.SetManualText("  Egen æøå\r\n \n").Kind);
        var frozen = f.Session.Current.PreparedSelection!;
        Assert.Equal(Core.SelectedLyricsOrigin.Manual, frozen.Provenance.Origin);
        Assert.Equal("  Egen æøå\n \n", frozen.Document!.Text);
        Assert.Same(frozen, f.Session.Current.PreparedSelection); Assert.Null(f.Session.Current.Query); Assert.Null(f.Session.Current.Page);
        Assert.Equal(LyricsSessionOutcomeKind.Superseded, (await Done(first)).Kind);
        Assert.Equal(LyricsSessionOutcomeKind.Superseded, (await Done(queued)).Kind);
        Assert.True(call.Token.IsCancellationRequested); Assert.Equal(1, f.Provider.Count);
        var drained = f.Session.DisposeAsync().AsTask();
        Assert.False(drained.IsCompleted); call.Complete(); await drained.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, f.Provider.Count); Assert.False(f.Provider.Disposed);
    }

    [Fact]
    public async Task RapidSupersessionRetainsOnlyNewestIntentAndSettlesObsoleteTasksImmediately()
    {
        await using var f = new Fixture();
        var first = f.Session.SearchAsync(Query); var active = await f.Provider.Next();
        var requests = Enumerable.Range(1, 30).Select(i => f.Session.SearchAsync(Query with { Title = i.ToString() })).ToArray();
        Assert.Equal(1, f.Provider.Count); Assert.Equal(1, f.Provider.Maximum);
        Assert.Equal(LyricsSessionOutcomeKind.Superseded, (await Done(first)).Kind);
        foreach (var superseded in requests[..^1]) Assert.Equal(LyricsSessionOutcomeKind.Superseded, (await Done(superseded)).Kind);
        Assert.False(requests[^1].IsCompleted);
        active.Complete(); var newest = await f.Provider.Next();
        Assert.Equal("30", newest.Query.Title); Assert.Equal(2, f.Provider.Count); Assert.Equal(1, f.Provider.Maximum);
        newest.Complete(); Assert.Equal(LyricsSessionOutcomeKind.Succeeded, (await Done(requests[^1])).Kind);
        Assert.Equal("30", f.Session.Current.Query!.Title);
    }

    [Theory]
    [InlineData("cancel", false)]
    [InlineData("cancel", true)]
    [InlineData("reset", false)]
    [InlineData("reset", true)]
    [InlineData("manual", false)]
    [InlineData("manual", true)]
    public async Task LateSuccessOrFailureCannotOverwriteNewState(string action, bool fault)
    {
        await using var f = new Fixture(); var result = f.Session.SearchAsync(Query); var call = await f.Provider.Next();
        if (action == "cancel") f.Session.Cancel();
        if (action == "reset") f.Session.Reset();
        if (action == "manual") f.Session.SetManualText(" Egen ");
        var snapshot = f.Session.Current;
        Assert.True(result.IsCompleted);
        if (fault) call.Reply.SetException(new Exception("secret-payload")); else call.Complete();
        // Drain after checking no late mutation using a provider-side continuation barrier.
        var probe = f.Session.SearchAsync(Query with { Title = "Probe" });
        var next = await f.Provider.Next();
        Assert.Equal("Probe", f.Session.Current.Query!.Title);
        next.Complete(); await Done(probe);
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(f.Session.Current));
        Assert.True(snapshot.Status is LyricsSessionStatus.Cancelled or LyricsSessionStatus.Idle or LyricsSessionStatus.ManualReady);
    }

    [Theory]
    [InlineData("admission")]
    [InlineData("active")]
    [InlineData("pending")]
    public async Task CallerCancellationIsTypedAndDoesNotInvokeCancelledPending(string stage)
    {
        await using var f = new Fixture(); using var cancel = new CancellationTokenSource();
        Call? old = null;
        if (stage == "pending") { _ = f.Session.SearchAsync(Query); old = await f.Provider.Next(); }
        if (stage == "admission") cancel.Cancel();
        var operation = f.Session.SearchAsync(Query with { Title = stage }, cancel.Token);
        Call? call = stage == "active" ? await f.Provider.Next() : null;
        cancel.Cancel();
        Assert.Equal(LyricsSessionOutcomeKind.Cancelled, (await Done(operation)).Kind);
        Assert.Equal(LyricsSessionStatus.Cancelled, f.Session.Current.Status);
        Assert.Null(f.Session.Current.Page); Assert.Null(f.Session.Current.PreparedSelection);
        if (call is not null) { Assert.True(call.Token.IsCancellationRequested); call.Complete(); }
        if (old is not null) old.Complete();
        await f.Session.DisposeAsync();
        Assert.Equal(stage == "admission" ? 0 : 1, f.Provider.Count);
    }

    [Fact]
    public async Task ObserverReentrancyCanReplacePendingSearchWithoutProviderCallOrDeadlock()
    {
        await using var f = new Fixture(); var other = 0;
        f.Session.Changed += (_, _) => throw new Exception("secret-observer");
        f.Session.Changed += (_, _) =>
        {
            if (f.Session.Current.Status == LyricsSessionStatus.Searching)
                Task.Run(() => f.Session.SetManualText("Observer manual")).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        };
        f.Session.Changed += (_, _) => other++;
        Assert.Equal(LyricsSessionOutcomeKind.Superseded, (await Done(f.Session.SearchAsync(Query))).Kind);
        Assert.Equal(LyricsSessionStatus.ManualReady, f.Session.Current.Status); Assert.True(other >= 1);
        Assert.Equal(0, f.Provider.Count); Assert.Null(f.Session.Current.Error);
    }

    [Fact]
    public async Task CancellationCallbackReentrancyDoesNotRunUnderStateLock()
    {
        await using var f = new Fixture(); var first = f.Session.SearchAsync(Query); var call = await f.Provider.Next();
        using var callback = call.Token.Register(() =>
            Task.Run(() => f.Session.SetManualText("Cancellation manual")).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult());
        var newer = f.Session.SearchAsync(Query with { Title = "Newer" });
        Assert.Equal(LyricsSessionOutcomeKind.Superseded, (await Done(first)).Kind);
        Assert.Equal(LyricsSessionOutcomeKind.Superseded, (await Done(newer)).Kind);
        Assert.Equal(LyricsSessionStatus.ManualReady, f.Session.Current.Status);
        call.Complete(); await f.Session.DisposeAsync(); Assert.Equal(1, f.Provider.Count);
    }

    [Fact]
    public async Task ProviderInvocationIsOutsideLockAndSynchronousCompletionIsSupported()
    {
        LyricsAcquisitionSession? session = null;
        var provider = new Immediate((q, _) =>
        {
            Task.Run(() => session!.Reset()).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            return Task.FromResult(Page(q, Candidate()));
        });
        await using (session = new LyricsAcquisitionSession(provider))
        {
            Assert.Equal(LyricsSessionOutcomeKind.Superseded, (await Done(session.SearchAsync(Query))).Kind);
            Assert.Equal(LyricsSessionStatus.Idle, session.Current.Status);
        }
    }

    [Fact]
    public async Task ConcurrentRepeatedDisposeSharesDrainAndClearsStateWithOneTerminalNotification()
    {
        await using var f = new Fixture(); var first = f.Session.SearchAsync(Query); var call = await f.Provider.Next();
        var pending = f.Session.SearchAsync(Query with { Title = "Pending" }); var terminal = 0; var notifications = 0;
        f.Session.Changed += (_, _) => { notifications++; if (f.Session.Current.Status == LyricsSessionStatus.Disposed) terminal++; };
        var disposals = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => new[] { f.Session.DisposeAsync().AsTask() })));
        var drains = disposals.Select(d => d[0]).ToArray();
        Assert.All(drains, d => Assert.Same(drains[0], d));
        Assert.False(drains[0].IsCompleted);
        call.Complete();
        await drains[0].WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(LyricsSessionStatus.Disposed, f.Session.Current.Status); Assert.Equal(1, terminal);
        Assert.Null(f.Session.Current.Query); Assert.Null(f.Session.Current.Page); Assert.Null(f.Session.Current.Selection);
        Assert.Null(f.Session.Current.PreparedSelection); Assert.Null(f.Session.Current.Error);
        Assert.Equal(LyricsSessionOutcomeKind.Closed, (await Done(pending)).Kind);
        Assert.True((await Done(first)).Kind is LyricsSessionOutcomeKind.Superseded or LyricsSessionOutcomeKind.Closed);
        Assert.Equal(1, f.Provider.Count); Assert.False(f.Provider.Disposed); Assert.Equal(1, notifications);
    }
    [Fact]
    public async Task InvalidUtf16ManualTextIsValidatedBeforeSnapshotRetention()
    {
        await using var f = new Fixture();
        var bad = new string((char)0xd800, 1);
        var result = f.Session.SetManualText(bad);
        Assert.Equal(LyricsSessionOutcomeKind.Failed, result.Kind);
        Assert.Equal(LyricsValidationError.InvalidDocument, result.Error!.ValidationError);
        Assert.Null(f.Session.Current.Selection); Assert.Null(f.Session.Current.PreparedSelection);
        var oversized = f.Session.SetManualText(new string('a', LyricsValidation.MaxDocumentBytes + 1));
        Assert.Equal(LyricsSessionOutcomeKind.Failed, oversized.Kind);
        Assert.Null(f.Session.Current.PreparedSelection);
    }

    [Fact]
    public async Task OlderCallerCancellationCannotInvalidateLatestPendingSearch()
    {
        await using var f = new Fixture(); using var oldCaller = new CancellationTokenSource();
        var older = f.Session.SearchAsync(Query, oldCaller.Token); var first = await f.Provider.Next();
        var newer = f.Session.SearchAsync(Query with { Title = "Latest" });
        var snapshot = f.Session.Current; oldCaller.Cancel();
        Assert.Same(snapshot, f.Session.Current);
        Assert.Equal(LyricsSessionOutcomeKind.Superseded, (await Done(older)).Kind);
        first.Complete(); var last = await f.Provider.Next(); last.Complete();
        Assert.Equal(LyricsSessionOutcomeKind.Succeeded, (await Done(newer)).Kind);
        Assert.Equal("Latest", f.Session.Current.Query!.Title);
    }

    [Fact]
    public async Task FaultingCancellationObserversCannotCorruptNewIntentOrPreventCleanup()
    {
        await using var f = new Fixture(); _ = f.Session.SearchAsync(Query); var first = await f.Provider.Next();
        var otherObserver = 0;
        using var one = first.Token.Register(() => throw new Exception("secret-cancellation"));
        using var two = first.Token.Register(() => otherObserver++);
        var newer = f.Session.SearchAsync(Query with { Title = "Latest" });
        Assert.Equal(1, otherObserver); Assert.Null(f.Session.Current.Error);
        first.Complete(); var last = await f.Provider.Next(); last.Complete();
        Assert.Equal(LyricsSessionOutcomeKind.Succeeded, (await Done(newer)).Kind);
        Assert.Equal(1, f.Provider.Maximum);
    }

    [Fact]
    public async Task ClosedSessionRejectsAllActionsAndDrainNeverOwnsProvider()
    {
        await using var f = new Fixture();
        var drain = f.Session.DisposeAsync().AsTask();
        Assert.Same(drain, f.Session.DisposeAsync().AsTask()); await drain;
        await Assert.ThrowsAsync<ObjectDisposedException>(() => f.Session.SearchAsync(Query));
        Assert.Throws<ObjectDisposedException>(() => f.Session.Cancel());
        Assert.Throws<ObjectDisposedException>(() => f.Session.Reset());
        Assert.Throws<ObjectDisposedException>(() => f.Session.SetManualText("Egen"));
        Assert.Throws<ObjectDisposedException>(() => f.Session.Select(0, "fixture", "candidate", "plain"));
        Assert.Equal(0, f.Provider.Count); Assert.False(f.Provider.Disposed);
    }

    [Fact]
    public async Task IneligibleSelectionAndNoOpCancelDoNotMutateState()
    {
        var provider = new Immediate((q, _) => Task.FromResult(Page(q)));
        await using var session = new LyricsAcquisitionSession(provider); await session.SearchAsync(Query);
        var current = session.Current;
        Assert.Equal(LyricsSessionOutcomeKind.Rejected, session.Select(current.Revision, "fixture", "candidate", "plain").Kind);
        Assert.Equal(LyricsSessionOutcomeKind.Unchanged, session.Cancel().Kind);
        Assert.Same(current, session.Current);
        session.Reset(); Assert.Equal(LyricsSessionStatus.Idle, session.Current.Status);
        Assert.Null(session.Current.Query); Assert.Null(session.Current.Page); Assert.Null(session.Current.Error);
    }

    [Fact]
    public async Task NullProviderTaskIsObservedAsUnexpectedAndIdentityMisuseIsSanitized()
    {
        var provider = new Immediate((_, _) => null!);
        await using var session = new LyricsAcquisitionSession(provider);
        Assert.Equal(LyricsSessionErrorKind.Unexpected, (await Done(session.SearchAsync(Query))).Error!.Kind);
        var error = Assert.Throws<ArgumentException>(() => new LyricsAcquisitionSession(new Immediate((q, _) => Task.FromResult(Page(q)), new string('a', 257))));
        Assert.DoesNotContain(new string('a', 257), error.Message);
    }

    [Fact]
    public async Task DrainWaitsForCancellationCallbacksEvenWhenProviderHasAlreadySettled()
    {
        await using var f = new Fixture(); _ = f.Session.SearchAsync(Query); var call = await f.Provider.Next();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = call.Token.Register(() =>
        {
            call.Complete();
            entered.TrySetResult();
            release.Task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        });
        var reset = Task.Run(() => f.Session.Reset());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var drain = f.Session.DisposeAsync().AsTask();
        Assert.False(drain.IsCompleted);
        release.TrySetResult();
        await reset.WaitAsync(TimeSpan.FromSeconds(5)); await drain.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(LyricsSessionStatus.Disposed, f.Session.Current.Status); Assert.False(f.Provider.Disposed);
    }

}
