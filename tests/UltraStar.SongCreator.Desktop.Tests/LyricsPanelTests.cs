using System.Collections.Concurrent;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using UltraStar.SongCreator.Acquisition.Lyrics;
using UltraStar.SongCreator.Core;
using UltraStar.SongCreator.Desktop.Lyrics;

namespace UltraStar.SongCreator.Desktop.Tests;

public sealed class LyricsPanelTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);
    private static LyricsCandidate Candidate(LyricsTextStatus status = LyricsTextStatus.Available) => new()
    {
        ProviderId = "fixture",
        Id = "c",
        Artist = "Egen artist",
        Title = "Egen tittel",
        RetrievedAtUtc = At,
        TextStatus = status,
        Documents = status == LyricsTextStatus.Available
            ? [new("plain", " Egen æøå \n", LyricsSyncLevel.Plain, []),
               new("line", " Egen æøå \n", LyricsSyncLevel.Line, [new(" Egen æøå ", 12.5, null, [])])] : [],
        VariantDiagnostics = [new("lrc", "bad", LyricsVariantDiagnosticCode.InvalidSyntax, LyricsVariantDiagnosticSeverity.Warning, 3)],
        Capabilities = new(true, true) { UnsupportedFormats = ["opaque"] }
    };
    private static LyricsPage Page(LyricsQuery query, params LyricsCandidate[] candidates) => new("fixture", query, [.. candidates], At);
    private sealed class Call(LyricsQuery query, CancellationToken token)
    {
        internal readonly LyricsQuery Query = query;
        internal readonly CancellationToken Token = token;
        internal readonly TaskCompletionSource<LyricsPage> Reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    private sealed class Provider : ILyricsSearchProvider, IDisposable
    {
        public string ProviderId => "fixture";
        private readonly SemaphoreSlim arrived = new(0);
        private readonly ConcurrentQueue<Call> waiting = new();
        internal readonly ConcurrentBag<Call> All = [];
        internal int Count, Disposes;
        internal bool ThrowDispose;
        public Task<LyricsPage> SearchAsync(LyricsQuery query, CancellationToken token)
        {
            var call = new Call(query, token);
            Interlocked.Increment(ref Count); All.Add(call); waiting.Enqueue(call); arrived.Release();
            return call.Reply.Task;
        }
        internal async Task<Call> Next()
        {
            await arrived.WaitAsync().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(waiting.TryDequeue(out var call)); return call!;
        }
        public void Dispose()
        {
            Interlocked.Increment(ref Disposes);
            if (ThrowDispose) throw new InvalidOperationException("private fixture detail");
        }
    }
    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly Provider Provider = new();
        internal readonly LyricsAcquisitionSession Acquisition;
        internal readonly LyricsPanelLifetime Lifetime;
        internal readonly EditingSession Editing = new(DemoSong.Create());
        internal readonly LyricsPanel Panel;
        internal Fixture(bool owned = false)
        {
            Acquisition = new(Provider); Lifetime = new(Acquisition, owned ? Provider : null);
            Panel = new(Editing, Lifetime);
        }
        public async ValueTask DisposeAsync()
        {
            Panel.Detach();
            foreach (var call in Provider.All) call.Reply.TrySetCanceled();
            var close = Lifetime.CloseAsync();
            await Acquisition.DisposeAsync();
            await close.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Dispatcher.UIThread.RunJobs();
        }
    }
    private static Task<LyricsPanelReceipt> Receipt(LyricsPanelLifetime lifetime, Action action,
        Func<LyricsPanelReceipt, bool>? predicate = null)
    {
        var done = new TaskCompletionSource<LyricsPanelReceipt>(TaskCreationOptions.RunContinuationsAsynchronously);
        long expected = -1;
        var early = new ConcurrentQueue<LyricsPanelReceipt>();
        void Handler(LyricsPanelReceipt value)
        {
            if (Volatile.Read(ref expected) == -1) { early.Enqueue(value); return; }
            if (value.Generation != Volatile.Read(ref expected)) return;
            if (predicate is not null && !predicate(value)) return;
            lifetime.Completed -= Handler; done.TrySetResult(value);
        }
        lifetime.Completed += Handler;
        action();
        Volatile.Write(ref expected, lifetime.LatestGeneration);
        while (early.TryDequeue(out var value)) Handler(value);
        return done.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }
    private static async Task SetQuery(Fixture f, string artist = "Egen artist", string title = "Egen tittel")
    {
        await Receipt(f.Lifetime, () =>
        {
            f.Panel.ArtistInput.Text = artist; f.Panel.TitleInput.Text = title;
            Dispatcher.UIThread.RunJobs();
        }, r => r.Outcome is not null && r.Snapshot.Status == LyricsSessionStatus.Idle);
        Dispatcher.UIThread.RunJobs();
    }
    private static async Task<Call> Search(Fixture f)
    {
        await Receipt(f.Lifetime, f.Panel.Search, r => r.Snapshot.Status == LyricsSessionStatus.Searching);
        Dispatcher.UIThread.RunJobs();
        return await f.Provider.Next();
    }
    private static async Task Results(Fixture f, Call call, params LyricsCandidate[] candidates)
    {
        await Receipt(f.Lifetime, () => call.Reply.SetResult(Page(call.Query, candidates)),
            r => r.Outcome?.Kind == LyricsSessionOutcomeKind.Succeeded);
        Dispatcher.UIThread.RunJobs();
    }
    private static async Task Choose(Fixture f, int candidate = 0, int variant = 0)
    {
        await Receipt(f.Lifetime, () => f.Panel.Candidates.SelectedIndex = candidate, r => r.Outcome is not null);
        Dispatcher.UIThread.RunJobs();
        Assert.False(f.Panel.ApplyButton.IsEnabled);
        Assert.Equal(-1, f.Panel.Variants.SelectedIndex);
        await Receipt(f.Lifetime, () => f.Panel.Variants.SelectedIndex = variant, r => r.Outcome is not null);
        Dispatcher.UIThread.RunJobs();
    }
    private static async Task Manual(Fixture f, string text)
    {
        await Receipt(f.Lifetime, () => { f.Panel.ManualInput.Text = text; Dispatcher.UIThread.RunJobs(); },
            r => r.Outcome is not null);
        Dispatcher.UIThread.RunJobs();
        await Receipt(f.Lifetime, f.Panel.PrepareManual, r => r.Outcome is not null);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public async Task BlankStartupHasNoQueryOrChoiceAndTargetIsSeparate()
    {
        await using var f = new Fixture();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, f.Provider.Count);
        Assert.Null(f.Panel.ArtistInput.Text); Assert.Null(f.Panel.TitleInput.Text);
        Assert.False(f.Panel.SearchButton.IsEnabled); Assert.False(f.Panel.ApplyButton.IsEnabled);
        Assert.Contains(f.Editing.Current.Metadata.Title, f.Panel.TargetLabel.Text);
        Assert.Empty(f.Panel.StoredLyrics.Text!);
        await SetQuery(f, "Egen & æ artist", "Tittel + ?");
        var call = await Search(f);
        Assert.Equal(new LyricsQuery("Egen & æ artist", "Tittel + ?"), call.Query);
        Assert.False(f.Panel.ApplyButton.IsEnabled);
        await Results(f, call, Candidate());
        Assert.Equal(-1, f.Panel.Candidates.SelectedIndex);
        Assert.Equal(-1, f.Panel.Variants.SelectedIndex);
        Assert.False(f.Panel.ApplyButton.IsEnabled); Assert.Null(f.Editing.Current.SelectedLyrics);
        Assert.Equal(1, f.Provider.Count);
    }

    [AvaloniaFact]
    public async Task ExplicitVariantPreservesExactPreviewProvenanceAndSourceTimeAndHistory()
    {
        await using var f = new Fixture();
        await SetQuery(f); var call = await Search(f);
        f.Editing.Select(f.Editing.Current.Phrases[0].Notes[0].Id); f.Editing.Move("2.5");
        var before = f.Editing.Current;
        await Results(f, call, Candidate()); await Choose(f, variant: 1);
        var prepared = f.Acquisition.Current.PreparedSelection!;
        Assert.True(f.Panel.ApplyButton.IsEnabled);
        Assert.Equal(" Egen æøå \n", f.Panel.Preview.Text);
        Assert.Contains("Warning", f.Panel.DiagnosticsLabel.Text);
        Assert.Contains("InvalidSyntax", f.Panel.DiagnosticsLabel.Text);
        Assert.Contains("linje 3", f.Panel.DiagnosticsLabel.Text);
        Assert.Contains("opaque", f.Panel.DiagnosticsLabel.Text);
        Assert.Contains("LyricsSourceSeconds", f.Panel.ProvenanceLabel.Text);
        Assert.Contains("12.5", f.Panel.TimingPreview.Text);
        Assert.True(f.Panel.Apply());
        Assert.Same(prepared, f.Editing.Current.SelectedLyrics);
        Assert.Same(before.Phrases[0].Notes[0], f.Editing.Current.Phrases[0].Notes[0]);
        var after = f.Editing.Current;
        f.Editing.Undo();
        Assert.Same(before, f.Editing.Current); Assert.Equal("", f.Panel.StoredLyrics.Text);
        f.Editing.Redo(); Assert.Same(after, f.Editing.Current);
        Assert.False(f.Panel.Apply());
        f.Editing.Undo(); Assert.True(f.Panel.Apply());
        Assert.Same(prepared, f.Editing.Current.SelectedLyrics);
    }

    [AvaloniaFact]
    public async Task ManualFrozenPayloadSharesHistoryAndNoOpPreservesRedo()
    {
        await using var f = new Fixture();
        await Manual(f, " Egen linje\n");
        Assert.True(f.Panel.ApplyButton.IsEnabled);
        var frozen = f.Acquisition.Current.PreparedSelection!;
        Assert.True(f.Panel.Apply()); Assert.Same(frozen, f.Editing.Current.SelectedLyrics);
        f.Editing.Select(f.Editing.Current.Phrases[0].Notes[0].Id); f.Editing.Move("2.5");
        f.Editing.Undo(); Assert.True(f.Editing.CanRedo);
        Assert.False(f.Panel.Apply()); Assert.True(f.Editing.CanRedo);
        f.Editing.Redo(); var moved = f.Editing.Current;
        f.Editing.Undo(); f.Editing.Undo();
        Assert.Null(f.Editing.Current.SelectedLyrics);
        Assert.Equal("", f.Panel.StoredLyrics.Text);
        Assert.Equal(" Egen linje\n", f.Panel.Preview.Text);
        f.Editing.Redo(); f.Editing.Redo();
        Assert.Same(moved, f.Editing.Current); Assert.Equal(" Egen linje\n", f.Panel.StoredLyrics.Text);
        Assert.Equal(0, f.Provider.Count);
    }

    [AvaloniaTheory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidManualNeverClearsStoredSong(string text)
    {
        await using var f = new Fixture();
        await Manual(f, "Egen tekst"); f.Panel.Apply();
        var before = f.Editing.Current;
        await Manual(f, text);
        Assert.False(f.Panel.ApplyButton.IsEnabled);
        Assert.False(f.Panel.Apply()); Assert.Same(before, f.Editing.Current);
        Assert.Contains("ugyldig", f.Panel.StatusLabel.Text);
    }

    [AvaloniaTheory]
    [InlineData(LyricsTextStatus.Instrumental, true)]
    [InlineData(LyricsTextStatus.Missing, false)]
    [InlineData(LyricsTextStatus.Unsupported, false)]
    public async Task InstrumentalRequiresExplicitVariantAndUnavailableNeverApplies(LyricsTextStatus status, bool expected)
    {
        await using var f = new Fixture();
        await SetQuery(f); var call = await Search(f); await Results(f, call, Candidate(status));
        await Receipt(f.Lifetime, () => f.Panel.Candidates.SelectedIndex = 0, r => r.Outcome is not null);
        Dispatcher.UIThread.RunJobs();
        Assert.False(f.Panel.ApplyButton.IsEnabled);
        if (expected)
        {
            await Receipt(f.Lifetime, () => f.Panel.Variants.SelectedIndex = 0, r => r.Outcome is not null);
            Dispatcher.UIThread.RunJobs();
            Assert.True(f.Panel.Apply()); Assert.Equal(SelectedLyricsKind.Instrumental, f.Editing.Current.SelectedLyrics!.Kind);
        }
        else { Assert.Empty(f.Panel.Variants.Items); Assert.False(f.Panel.Apply()); }
    }

    [AvaloniaFact]
    public async Task NoResultsAndCancelObserveIgnoredCancellationWithoutHiddenFallback()
    {
        await using var f = new Fixture();
        await SetQuery(f); var call = await Search(f);
        await Receipt(f.Lifetime, f.Panel.Cancel, r => r.Outcome?.Kind == LyricsSessionOutcomeKind.Cancelled);
        Dispatcher.UIThread.RunJobs();
        Assert.True(call.Token.IsCancellationRequested); Assert.Contains("avbrutt", f.Panel.StatusLabel.Text);
        call.Reply.SetResult(Page(call.Query, Candidate())); // late reply cannot restore preview
        var next = await Search(f); await Results(f, next);
        Assert.Contains("Ingen sangtekst", f.Panel.StatusLabel.Text);
        Assert.False(f.Panel.ApplyButton.IsEnabled); Assert.Equal(2, f.Provider.Count);
    }

    [AvaloniaTheory]
    [InlineData(LyricsSearchError.RateLimited, true)]
    [InlineData(LyricsSearchError.InvalidResponse, false)]
    public async Task ProviderErrorsAreTypedStaticAndNeverDisplayExceptionText(LyricsSearchError error, bool retry)
    {
        await using var f = new Fixture();
        await SetQuery(f); var call = await Search(f);
        await Receipt(f.Lifetime, () => call.Reply.SetException(new LyricsSearchException(error, "private raw lyrics",
            429, retry ? TimeSpan.FromSeconds(7) : null)), r => r.Outcome?.Kind == LyricsSessionOutcomeKind.Failed);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(error.ToString(), f.Panel.StatusLabel.Text);
        Assert.DoesNotContain("private", f.Panel.StatusLabel.Text);
        if (retry) Assert.Contains("7 sekunder", f.Panel.StatusLabel.Text);
        Assert.False(f.Panel.ApplyButton.IsEnabled); Assert.Equal(1, f.Provider.Count);
    }

    [AvaloniaFact]
    public async Task EditedInputAndExternalRevisionDisableApplyBeforeResetOrQueuedRender()
    {
        await using var f = new Fixture();
        await Manual(f, "Egen tekst");
        Assert.True(f.Panel.ApplyButton.IsEnabled);
        var rendered = f.Acquisition.Current;
        f.Panel.ArtistInput.Text = "Ny artist";
        Assert.False(f.Panel.ApplyButton.IsEnabled); // Property invalidation is synchronous, before queued TextChanged/render work.
        Assert.False(f.Panel.Apply());
        Dispatcher.UIThread.RunJobs();
        Assert.False(f.Panel.ApplyButton.IsEnabled); Assert.False(f.Panel.Apply());
        await Manual(f, "Ny tekst");
        Assert.True(f.Panel.ApplyButton.IsEnabled);
        await Task.Run(() => f.Acquisition.SetManualText("Utenfra"));
        // Apply checks current revision even without processing a queued Changed.
        Assert.False(f.Panel.Apply());
        Dispatcher.UIThread.RunJobs();
        Assert.False(f.Panel.ApplyButton.IsEnabled); Assert.Null(f.Editing.Current.SelectedLyrics);
        Assert.True(f.Acquisition.Current.Revision > rendered.Revision);
    }

    [AvaloniaFact]
    public async Task StaleCandidateChoiceIsRejectedAndNeverRetriedAgainstCurrentRevision()
    {
        await using var f = new Fixture();
        await SetQuery(f); var call = await Search(f); await Results(f, call, Candidate());
        Assert.True(f.Panel.Candidates.IsEnabled);
        await Task.Run(() => f.Acquisition.Reset());
        // Do not pump the already queued Changed; the control still shows the old page.
        await Receipt(f.Lifetime, () => f.Panel.Candidates.SelectedIndex = 0, r => r.Outcome?.Kind == LyricsSessionOutcomeKind.Rejected);
        Dispatcher.UIThread.RunJobs();
        Assert.False(f.Panel.ApplyButton.IsEnabled);
        Assert.Null(f.Acquisition.Current.PreparedSelection);
        Assert.Contains("ikke lenger gyldig", f.Panel.StatusLabel.Text);
        Assert.Equal(1, f.Provider.Count);
    }

    [AvaloniaFact]
    public async Task OldPostedPreviewCannotReenableApplyAfterImmediateEditAndDetach()
    {
        await using var f = new Fixture();
        await Manual(f, "Første tekst");
        f.Panel.ManualInput.Text = "Andre tekst";
        Assert.False(f.Panel.ApplyButton.IsEnabled);
        await Receipt(f.Lifetime, f.Panel.PrepareManual, r => r.Outcome is not null);
        // Its receipt is posted but no explicit dispatcher drain is needed to invalidate it.
        f.Panel.ManualInput.Text = "Tredje tekst";
        Assert.False(f.Panel.ApplyButton.IsEnabled); Assert.False(f.Panel.Apply());
        f.Panel.Detach();
        Dispatcher.UIThread.RunJobs();
        Assert.False(f.Panel.ApplyButton.IsEnabled); Assert.Null(f.Editing.Current.SelectedLyrics);
    }

    [AvaloniaFact]
    public async Task DetachDropsLateReceiptsAndDoesNotDisposeBorrowedResources()
    {
        await using var f = new Fixture();
        await SetQuery(f); var call = await Search(f);
        f.Panel.Detach(); var status = f.Panel.StatusLabel.Text;
        call.Reply.SetResult(Page(call.Query, Candidate()));
        await f.Acquisition.DisposeAsync();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(status, f.Panel.StatusLabel.Text);
        Assert.False(f.Panel.ApplyButton.IsEnabled); Assert.False(f.Panel.PrepareManualButton.IsEnabled);
        Assert.Equal(0, f.Provider.Disposes); Assert.False(f.Panel.Apply());
    }

    [AvaloniaFact]
    public async Task ManualAndCancelAreAdmittedWhileProviderSearchIsPending()
    {
        await using var f = new Fixture();
        await SetQuery(f); var call = await Search(f);
        await Manual(f, "Egen manuell tekst");
        Assert.True(call.Token.IsCancellationRequested);
        Assert.True(f.Panel.ApplyButton.IsEnabled); Assert.True(f.Panel.Apply());
        Assert.Equal(1, f.Provider.Count);
        call.Reply.SetResult(Page(call.Query, Candidate()));
        await f.Acquisition.DisposeAsync(); Dispatcher.UIThread.RunJobs();
        Assert.False(f.Panel.Apply());
    }

    [AvaloniaFact]
    public async Task OwnedCloseReturnsSameTaskAndWaitsForIgnoredProvider()
    {
        await using var f = new Fixture(owned: true);
        await SetQuery(f); var call = await Search(f);
        var close = f.Panel.CloseAsync();
        Assert.Same(close, f.Panel.CloseAsync());
        var responsive = false; Dispatcher.UIThread.Post(() => responsive = true); Dispatcher.UIThread.RunJobs();
        Assert.True(responsive); Assert.False(close.IsCompleted); Assert.Equal(0, f.Provider.Disposes);
        call.Reply.SetResult(Page(call.Query, Candidate()));
        Assert.True(await close.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(1, f.Provider.Disposes); Assert.Equal(LyricsSessionStatus.Disposed, f.Acquisition.Current.Status);
        Assert.False(f.Panel.SearchButton.IsEnabled);
    }

    [AvaloniaFact]
    public async Task HeldCancellationCoalescesPendingEditsAndKeepsUiResponsive()
    {
        await using var f = new Fixture(owned: true);
        await SetQuery(f); var call = await Search(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var registration = call.Token.Register(() => { entered.TrySetResult(); release.Wait(TestContext.Current.CancellationToken); });
        f.Panel.Cancel();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        try
        {
            f.Panel.ManualInput.Text = "Premier";
            f.Panel.ManualInput.Text = "Dernier";
            Dispatcher.UIThread.RunJobs();
            f.Panel.PrepareManual();
            Assert.False(f.Panel.ApplyButton.IsEnabled);
            var posted = false; Dispatcher.UIThread.Post(() => posted = true); Dispatcher.UIThread.RunJobs();
            Assert.True(posted); Assert.Equal(1, f.Provider.Count);
            var receipt = Receipt(f.Lifetime, release.Set, r => r.Snapshot.Status == LyricsSessionStatus.ManualReady && r.Outcome is not null);
            await receipt; Dispatcher.UIThread.RunJobs();
            Assert.True(f.Panel.ApplyButton.IsEnabled); Assert.Equal("Dernier", f.Panel.Preview.Text);
        }
        finally { release.Set(); }
    }

    [AvaloniaFact]
    public async Task CloseWithHeldCallbackWaitsForAdmittedActionAndNeverDisposesProviderEarly()
    {
        await using var f = new Fixture(owned: true);
        await SetQuery(f); var call = await Search(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var registration = call.Token.Register(() => { entered.TrySetResult(); release.Wait(TestContext.Current.CancellationToken); });
        f.Panel.Cancel(); await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        try
        {
            f.Panel.ManualInput.Text = "Forkastet"; Dispatcher.UIThread.RunJobs(); f.Panel.PrepareManual();
            var close = f.Panel.CloseAsync();
            call.Reply.SetResult(Page(call.Query, Candidate()));
            var alive = false; Dispatcher.UIThread.Post(() => alive = true); Dispatcher.UIThread.RunJobs();
            Assert.True(alive); Assert.False(close.IsCompleted); Assert.Equal(0, f.Provider.Disposes);
            release.Set(); Assert.True(await close.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(1, f.Provider.Disposes);
            Assert.Null(f.Acquisition.Current.PreparedSelection); Assert.False(f.Panel.ApplyButton.IsEnabled);
        }
        finally { release.Set(); }
    }

    [AvaloniaFact]
    public async Task CloseItselfStartsCancellationOffUiAndWaitsForCallbackLease()
    {
        await using var f = new Fixture(owned: true);
        await SetQuery(f); var call = await Search(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var registration = call.Token.Register(() => { entered.TrySetResult(); release.Wait(TestContext.Current.CancellationToken); });
        var close = f.Panel.CloseAsync(); // Must return before the callback is allowed to return.
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        try
        {
            Assert.Same(close, f.Panel.CloseAsync());
            call.Reply.SetResult(Page(call.Query, Candidate()));
            var responsive = false; Dispatcher.UIThread.Post(() => responsive = true); Dispatcher.UIThread.RunJobs();
            Assert.True(responsive); Assert.False(close.IsCompleted); Assert.Equal(0, f.Provider.Disposes);
            release.Set();
            Assert.True(await close.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(1, f.Provider.Disposes); Assert.Null(f.Acquisition.Current.PreparedSelection);
        }
        finally { release.Set(); }
    }

    [AvaloniaFact]
    public async Task BorrowedPanelCloseAllowsCallerToReuseSessionAndNeverDisposesProvider()
    {
        await using var f = new Fixture();
        await Manual(f, "Egen før lukking");
        Assert.True(await f.Panel.CloseAsync());
        Assert.Equal(0, f.Provider.Disposes);
        Assert.Equal(LyricsSessionStatus.ManualReady, f.Acquisition.Current.Status);
        var text = f.Panel.StatusLabel.Text;
        await Task.Run(() => f.Acquisition.SetManualText("Egen etter lukking"));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(text, f.Panel.StatusLabel.Text); Assert.False(f.Panel.ApplyButton.IsEnabled);
        Assert.Equal("Egen etter lukking", f.Acquisition.Current.PreparedSelection!.Document!.Text);
    }

    [AvaloniaFact]
    public async Task CleanupFaultIsObservedAsFalseAndSingleTaskWithoutRawUiPayload()
    {
        await using var f = new Fixture(owned: true);
        f.Provider.ThrowDispose = true;
        var close = f.Panel.CloseAsync();
        Assert.False(await close.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Same(close, f.Panel.CloseAsync()); Assert.True(f.Lifetime.CleanupFailed);
        Assert.Equal(1, f.Provider.Disposes);
        Assert.DoesNotContain("private", f.Panel.StatusLabel.Text);
    }

    [Fact]
    public async Task AdmissionIsBoundedLatestWinsObserversCannotBreakDrainAndBorrowedCloseDoesNotDispose()
    {
        var provider = new Provider(); var session = new LyricsAcquisitionSession(provider);
        var life = new LyricsPanelLifetime(session);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var calls = new ConcurrentQueue<int>();
        life.Completed += _ => throw new InvalidOperationException("private observer");
        life.Enqueue(1, () => { entered.SetResult(); release.Wait(TestContext.Current.CancellationToken); calls.Enqueue(1); return Task.FromResult(session.Reset()); });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        for (var index = 2; index < 100; index++)
        {
            var captured = index;
            life.Enqueue(index, () => { calls.Enqueue(captured); return Task.FromResult(session.Reset()); });
        }
        await Receipt(life, release.Set, r => r.Generation == 99 && r.Outcome is not null);
        var close = life.CloseAsync();
        Assert.True(await close.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(new[] { 1, 99 }, calls);
        Assert.Equal(0, provider.Disposes); Assert.NotEqual(LyricsSessionStatus.Disposed, session.Current.Status);
        Assert.False(life.Enqueue(100, () => throw new InvalidOperationException()));
        await session.DisposeAsync(); provider.Dispose();
    }

    [AvaloniaFact]
    public async Task SynchronousAndAsyncActionFaultsUseStaticMessageAndRemainClosable()
    {
        await using var f = new Fixture();
        // Generation zero matches the initial panel, without synthetic UI input.
        await Receipt(f.Lifetime, () => f.Lifetime.Enqueue(0, () => throw new InvalidOperationException("private")));
        Dispatcher.UIThread.RunJobs(); Assert.Contains("kunne ikke", f.Panel.StatusLabel.Text);
        await Receipt(f.Lifetime, () => f.Lifetime.Enqueue(0, () => Task.FromException<LyricsSessionOutcome>(new Exception("private"))));
        Dispatcher.UIThread.RunJobs(); Assert.DoesNotContain("private", f.Panel.StatusLabel.Text);
        Assert.False(f.Panel.Apply()); Assert.True(await f.Panel.CloseAsync());
    }
}
