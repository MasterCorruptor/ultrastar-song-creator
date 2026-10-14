using System.Collections.Concurrent;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UltraStar.SongCreator.Acquisition.Lyrics;
using UltraStar.SongCreator.Audio;
using UltraStar.SongCreator.Desktop.Lyrics;
using UltraStar.SongCreator.Desktop.Playback;

namespace UltraStar.SongCreator.Desktop.Tests;

public sealed class LyricsApplicationLifetimeTests
{
    private sealed class Backend(PcmClip clip, ConcurrentQueue<string> order) : IPcmPlayback
    {
        internal readonly PcmPlayback Engine = new(clip);
        internal int Disposals;
        internal bool ThrowDispose;
        public PcmClip Clip => Engine.Clip;
        public PlaybackSnapshot Snapshot => Engine.Snapshot;
        public void Play() => Engine.Play();
        public void Pause() => Engine.Pause();
        public void Stop() => Engine.Stop();
        public void SeekFrame(long frame) => Engine.SeekFrame(frame);
        public void SeekSeconds(double seconds) => Engine.SeekSeconds(seconds);
        public void Dispose()
        {
            Assert.True(Dispatcher.UIThread.CheckAccess());
            Disposals++; order.Enqueue("audio"); Engine.Dispose();
            if (ThrowDispose) throw new InvalidOperationException("private audio detail");
        }
    }
    private sealed class Call(LyricsQuery query, CancellationToken token)
    {
        internal readonly LyricsQuery Query = query;
        internal readonly CancellationToken Token = token;
        internal readonly TaskCompletionSource<LyricsPage> Reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal void Complete() => Reply.TrySetResult(new("fixture", Query, [], new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero)));
    }
    private sealed class Provider(ConcurrentQueue<string> order) : ILyricsSearchProvider, IDisposable
    {
        internal string Identity = "fixture";
        public string ProviderId => Identity;
        internal readonly ConcurrentBag<Call> All = [];
        internal readonly TaskCompletionSource<Call> First = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Action<Call>? OnSearch;
        internal bool ThrowDispose;
        internal int Disposals, Calls;
        public Task<LyricsPage> SearchAsync(LyricsQuery query, CancellationToken token)
        {
            Calls++;
            var call = new Call(query, token); All.Add(call); OnSearch?.Invoke(call); First.TrySetResult(call);
            return call.Reply.Task;
        }
        public void Dispose()
        {
            Assert.False(Dispatcher.UIThread.CheckAccess());
            Disposals++; order.Enqueue("provider");
            if (ThrowDispose) throw new InvalidOperationException("private provider detail");
        }
    }
    private sealed class Desktop
    {
        internal bool RejectEditorAssignment;
        internal int Shutdowns, ExitCode;
        internal ShutdownMode ShutdownMode;
        internal Window? MainWindow;
        private Action? exit;
        private Func<bool>? request;
        internal EditorDesktopHost Host => new(
            () => ShutdownMode = ShutdownMode.OnExplicitShutdown,
            window =>
            {
                if (RejectEditorAssignment && window is EditorWindow) throw new InvalidOperationException("private assignment detail");
                MainWindow = window;
            },
            code =>
            {
                Assert.True(Dispatcher.UIThread.CheckAccess());
                Shutdowns++; ExitCode = code; exit?.Invoke();
            },
            callback => exit = callback,
            callback => request = callback);
        internal bool TryShutdown() => request?.Invoke() ?? true;
    }
    private sealed class Fixture : IAsyncDisposable
    {
        internal readonly ConcurrentQueue<string> Order = new();
        internal readonly Provider Provider;
        internal Backend? Backend;
        internal readonly EditorFactories Factories;
        internal LyricsPanelLifetime? Lifetime;
        internal LyricsAcquisitionSession? Acquisition;
        internal readonly EditorApplication App = new();
        internal readonly Desktop Desktop = new();
        internal EditorComposition Composition => App.Composition!;
        internal EditorWindow Window => Composition.Window!;
        internal LyricsPanel Panel => Composition.Panel!;
        internal Fixture(string fault = "", bool holdStartupSearch = false)
        {
            Provider = new(Order);
            if (fault == "session") Provider.Identity = "";
            Desktop.RejectEditorAssignment = fault == "assignment";
            Factories = new()
            {
                Playback = offset => fault == "playback" ? throw new InvalidOperationException("private playback detail")
                    : new(c => Backend = new(c, Order), offset),
                Provider = () => fault == "provider" ? throw new InvalidOperationException("private construction detail") : (Provider, Provider),
                Acquisition = p => Acquisition = new(p),
                Panel = (e, l) =>
                {
                    Lifetime = l;
                    if (fault == "panel") throw new InvalidOperationException("private panel detail");
                    return new(e, l);
                },
                Window = (e, p, l, close) =>
                {
                    if (fault == "window")
                    {
                        if (holdStartupSearch) _ = Acquisition!.SearchAsync(new("Egen artist", "Egen tittel"));
                        throw new InvalidOperationException("private window detail");
                    }
                    return new(e, p, l, close);
                }
            };
        }
        internal void Start()
        {
            App.StartDesktop(Desktop.Host, Factories);
            if (!Composition.StartupFailed) Window.Show();
            Dispatcher.UIThread.RunJobs();
        }
        public async ValueTask DisposeAsync()
        {
            foreach (var call in Provider.All) call.Reply.TrySetCanceled();
            if (App.Composition is not null)
                await Composition.CleanupAsync().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            if (Composition.Window is { } w && w.IsVisible) w.FinishClose(); // Fixture teardown after guard assertions.
            App.StartupFailureWindow?.Hide();
            Dispatcher.UIThread.RunJobs();
        }
    }
    private static async Task<LyricsPanelReceipt> Receipt(Fixture f, Action action,
        Func<LyricsPanelReceipt, bool>? match = null)
    {
        var done = new TaskCompletionSource<LyricsPanelReceipt>(TaskCreationOptions.RunContinuationsAsynchronously);
        var early = new ConcurrentQueue<LyricsPanelReceipt>();
        long expected = -1;
        void Changed(LyricsPanelReceipt r)
        {
            if (Volatile.Read(ref expected) < 0) { early.Enqueue(r); return; }
            if (r.Generation != Volatile.Read(ref expected) || match?.Invoke(r) == false) return;
            f.Lifetime!.Completed -= Changed; done.TrySetResult(r);
        }
        f.Lifetime!.Completed += Changed;
        action();
        Volatile.Write(ref expected, f.Lifetime.LatestGeneration);
        while (early.TryDequeue(out var r)) Changed(r);
        var result = await done.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Dispatcher.UIThread.RunJobs();
        return result;
    }
    private static async Task<Call> Search(Fixture f)
    {
        await Receipt(f, () => { f.Panel.ArtistInput.Text = "Egen artist"; f.Panel.TitleInput.Text = "Egen tittel"; },
            r => r.Outcome is not null);
        await Receipt(f, () => f.Panel.SearchButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)),
            r => r.Snapshot.Status == LyricsSessionStatus.Searching);
        return await f.Provider.First.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [AvaloniaFact]
    public async Task FullAppCompositionShowsPanelWithoutStartupSearchAndSharesHistoryAndPlayback()
    {
        await using var f = new Fixture(); f.Start();
        Assert.Equal(ShutdownMode.OnExplicitShutdown, f.Desktop.ShutdownMode);
        Assert.Same(f.Window, f.Desktop.MainWindow);
        Assert.Contains(f.Panel, f.Window.GetVisualDescendants());
        Assert.Equal(0, f.Provider.Calls); Assert.Null(f.Panel.ArtistInput.Text); Assert.Null(f.Panel.TitleInput.Text);
        var song = f.Window.Session.Current;
        var clip = f.Window.Playback!.Clip;
        f.Window.Playback.Presenter!.Play();
        f.Backend!.Engine.Render(new float[48000 * 2]); f.Window.Playback.Presenter.Refresh();
        var position = f.Window.Waveform!.PlayheadSongSeconds;
        var note = f.Window.Session.Current.Phrases[0].Notes[0];
        f.Window.Session.Select(note.Id); f.Window.StartInput.Text = "2.5";
        f.Window.MoveButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var moved = f.Window.Session.Current;
        await Receipt(f, () => f.Panel.ManualInput.Text = "Egen syntetisk tekst", r => r.Outcome is not null);
        await Receipt(f, () => f.Panel.PrepareManualButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)), r => r.Outcome is not null);
        var frozen = f.Acquisition!.Current.PreparedSelection;
        f.Panel.ApplyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var applied = f.Window.Session.Current;
        Assert.Same(frozen, applied.SelectedLyrics); Assert.Same(moved.Phrases[0].Notes[0], applied.Phrases[0].Notes[0]);
        f.Window.UndoButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Assert.Same(moved, f.Window.Session.Current);
        f.Window.UndoButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Assert.Same(song, f.Window.Session.Current);
        f.Window.RedoButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); f.Window.RedoButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Same(applied, f.Window.Session.Current);
        Assert.Same(clip, f.Window.Playback.Clip); Assert.Same(clip, f.Backend.Clip);
        Assert.Equal(position, f.Window.Waveform.PlayheadSongSeconds);
        Assert.True(f.Window.NotePlayhead.IsVisible); Assert.Equal(0, f.Provider.Calls);
        f.Window.Close();
        Assert.True(await f.Window.CloseTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(1, f.Desktop.Shutdowns); Assert.Equal(1, f.Backend.Disposals); Assert.Equal(1, f.Provider.Disposals);
    }

    [AvaloniaFact]
    public async Task PendingIgnoredCancellationKeepsAppAndWindowAliveUntilDrainAndReusesCloseTask()
    {
        await using var f = new Fixture(); f.Start(); var call = await Search(f);
        Assert.False(f.Desktop.TryShutdown());
        var close = f.Window.CloseTask!; f.Window.Close();
        Assert.Same(close, f.Window.CloseTask);
        Assert.True(f.Window.IsVisible); Assert.Equal(0, f.Desktop.Shutdowns);
        Assert.Equal(1, f.Backend!.Disposals); Assert.Equal(0, f.Provider.Disposals);
        Assert.False(f.Panel.ApplyButton.IsEnabled); Assert.False(f.Window.MoveButton.IsEnabled);
        var responsive = false; Dispatcher.UIThread.Post(() => responsive = true); Dispatcher.UIThread.RunJobs(); Assert.True(responsive);
        call.Complete();
        Assert.True(await close.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.False(f.Window.IsVisible); Assert.Equal(1, f.Desktop.Shutdowns);
        Assert.Equal(new[] { "audio", "provider" }, f.Order); Assert.True(call.Token.IsCancellationRequested);
        f.Window.ReleasePlayback(); Assert.Equal(1, f.Backend.Disposals);
    }

    [AvaloniaFact]
    public async Task WindowCloseStartsHeldCancellationOffUiAndDisposesAudioBeforeCallbackAndProviderAfter()
    {
        await using var f = new Fixture(); f.Start(); var call = await Search(f);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var registration = call.Token.Register(() =>
        {
            Assert.False(Dispatcher.UIThread.CheckAccess());
            Assert.Equal(1, f.Backend!.Disposals);
            entered.TrySetResult(); release.Wait(TestContext.Current.CancellationToken);
        });
        f.Window.Close(); var close = f.Window.CloseTask!;
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        try
        {
            f.Window.Close(); Assert.Same(close, f.Window.CloseTask);
            call.Complete();
            var responsive = false; Dispatcher.UIThread.Post(() => responsive = true); Dispatcher.UIThread.RunJobs();
            Assert.True(responsive); Assert.True(f.Window.IsVisible); Assert.False(close.IsCompleted);
            Assert.Equal(0, f.Provider.Disposals); Assert.Equal(0, f.Desktop.Shutdowns);
            release.Set();
            Assert.True(await close.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(new[] { "audio", "provider" }, f.Order); Assert.Equal(1, f.Desktop.Shutdowns);
        }
        finally { release.Set(); }
    }

    [AvaloniaFact]
    public async Task CleanupFailureIsObservedStaticAndKeepsNormalCloseGuardWithoutDuplicateDisposal()
    {
        await using var f = new Fixture(); f.Provider.ThrowDispose = true; f.Start();
        f.Window.Close(); var close = f.Window.CloseTask!;
        Assert.False(await close.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.True(f.Window.IsVisible); Assert.Equal(0, f.Desktop.Shutdowns);
        Assert.Contains("Opprydding feilet", f.Window.CloseErrorLabel.Text);
        Assert.DoesNotContain("private", f.Window.CloseErrorLabel.Text);
        f.Window.Close(); Assert.Same(close, f.Window.CloseTask);
        Assert.True(f.Window.IsVisible); Assert.Equal(1, f.Provider.Disposals); Assert.Equal(1, f.Backend!.Disposals);
    }

    [AvaloniaFact]
    public async Task AudioCleanupFaultStillDrainsLyricsAndRetainsGuardWithStaticMessage()
    {
        await using var f = new Fixture(); f.Start(); var call = await Search(f);
        f.Backend!.ThrowDispose = true;
        f.Window.Close(); var close = f.Window.CloseTask!;
        Assert.True(f.Window.IsVisible); Assert.Equal(0, f.Provider.Disposals);
        call.Complete();
        Assert.False(await close.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(new[] { "audio", "provider" }, f.Order);
        Assert.Equal(0, f.Desktop.Shutdowns); Assert.True(f.Window.IsVisible);
        Assert.Contains("Opprydding feilet", f.Window.CloseErrorLabel.Text);
        Assert.DoesNotContain("private", f.Window.CloseErrorLabel.Text);
    }

    [AvaloniaFact]
    public async Task FaultedCloseTaskIsObservedWithoutRawExceptionAndIsNotRepeated()
    {
        var order = new ConcurrentQueue<string>(); var provider = new Provider(order);
        await using var acquisition = new LyricsAcquisitionSession(provider);
        var editing = new EditingSession(DemoSong.Create());
        Backend? backend = null;
        using var playback = new EditorPlaybackSession(c => backend = new(c, order), 0);
        var calls = 0;
        var panel = new LyricsPanel(editing, acquisition);
        var window = new EditorWindow(editing, playback, panel, () =>
        {
            calls++; Assert.Equal(1, backend!.Disposals);
            return Task.FromException<bool>(new InvalidOperationException("private cleanup detail"));
        });
        window.Show(); Dispatcher.UIThread.RunJobs();
        try
        {
            window.Close(); var close = window.CloseTask!;
            Assert.False(await close.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.True(window.IsVisible); Assert.Contains("Opprydding feilet", window.CloseErrorLabel.Text);
            Assert.DoesNotContain("private", window.CloseErrorLabel.Text);
            window.Close(); Assert.Same(close, window.CloseTask); Assert.Equal(1, calls);
        }
        finally { window.FinishClose(); await Task.Run(provider.Dispose); }
    }

    [AvaloniaTheory]
    [InlineData("playback")]
    [InlineData("provider")]
    [InlineData("session")]
    [InlineData("panel")]
    [InlineData("window")]
    [InlineData("assignment")]
    public async Task StartupFaultsReleaseAllReturnedPartialResourcesBeforeShutdown(string fault)
    {
        await using var f = new Fixture(fault); f.Start();
        Assert.True(f.Composition.StartupFailed);
        Assert.True(await f.App.StartupCleanupTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(1, f.Desktop.Shutdowns); Assert.Equal(1, f.Desktop.ExitCode);
        Assert.DoesNotContain("private", f.App.StartupStatus.Text);
        Assert.Equal(fault == "playback" ? 0 : 1, f.Backend?.Disposals ?? 0);
        Assert.Equal(fault is "playback" or "provider" ? 0 : 1, f.Provider.Disposals);
        if (fault is not ("playback" or "provider")) Assert.Equal(new[] { "audio", "provider" }, f.Order);
        if (f.Acquisition is not null) Assert.Equal(LyricsSessionStatus.Disposed, f.Acquisition.Current.Status);
        if (f.Panel is not null) Assert.False(f.Panel.SearchButton.IsEnabled);
        Assert.False(f.App.StartupFailureWindow!.IsVisible);
    }

    [AvaloniaFact]
    public async Task StartupFailureWaitsHeldDrainWithoutLosingAppLifetimeOrBlockingDispatcher()
    {
        await using var f = new Fixture("window", holdStartupSearch: true);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        CancellationTokenRegistration registration = default;
        f.Provider.OnSearch = call => registration = call.Token.Register(() =>
        {
            Assert.False(Dispatcher.UIThread.CheckAccess()); Assert.Equal(1, f.Backend!.Disposals);
            entered.TrySetResult(); release.Wait(TestContext.Current.CancellationToken);
        });
        f.Start();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        try
        {
            Assert.Equal(ShutdownMode.OnExplicitShutdown, f.Desktop.ShutdownMode);
            Assert.Equal(0, f.Desktop.Shutdowns); Assert.True(f.App.StartupFailureWindow!.IsVisible);
            Assert.False(f.Desktop.TryShutdown()); Assert.True(f.App.StartupFailureWindow.IsVisible);
            Assert.False(f.App.StartupCleanupTask!.IsCompleted); Assert.Equal(0, f.Provider.Disposals);
            var call = await f.Provider.First.Task; call.Complete();
            var responsive = false; Dispatcher.UIThread.Post(() => responsive = true); Dispatcher.UIThread.RunJobs(); Assert.True(responsive);
            release.Set();
            Assert.True(await f.App.StartupCleanupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(1, f.Desktop.Shutdowns); Assert.Equal(new[] { "audio", "provider" }, f.Order);
        }
        finally { release.Set(); registration.Dispose(); }
    }

    [AvaloniaFact]
    public async Task StartupCleanupFailureRetainsStatusWindowAndAppGuard()
    {
        await using var f = new Fixture("panel"); f.Provider.ThrowDispose = true; f.Start();
        Assert.False(await f.App.StartupCleanupTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(0, f.Desktop.Shutdowns); Assert.True(f.App.StartupFailureWindow!.IsVisible);
        Assert.Contains("Opprydding feilet", f.App.StartupStatus.Text); Assert.DoesNotContain("private", f.App.StartupStatus.Text);
        f.App.StartupFailureWindow.Close(); Assert.True(f.App.StartupFailureWindow.IsVisible);
        Assert.Equal(1, f.Provider.Disposals); Assert.Equal(1, f.Backend!.Disposals);
    }

    [AvaloniaFact]
    public async Task BorrowedLyricsOverloadClosesViewAndAudioWithoutDisposingCallerProviderOrSession()
    {
        var order = new ConcurrentQueue<string>(); var provider = new Provider(order);
        await using var acquisition = new LyricsAcquisitionSession(provider);
        var editing = new EditingSession(DemoSong.Create());
        Backend? backend = null;
        using var playback = new EditorPlaybackSession(c => backend = new(c, order), 0);
        var panel = new LyricsPanel(editing, acquisition);
        var window = new EditorWindow(editing, playback, panel);
        window.Show(); Dispatcher.UIThread.RunJobs(); window.Close();
        Assert.True(await window.CloseTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(1, backend!.Disposals); Assert.Equal(0, provider.Disposals);
        Assert.NotEqual(LyricsSessionStatus.Disposed, acquisition.Current.Status);
        await Task.Run(() => acquisition.SetManualText("Egen tekst etter lukking"));
        Assert.Equal("Egen tekst etter lukking", acquisition.Current.PreparedSelection!.Document!.Text);
        await Task.Run(provider.Dispose);
    }

    [AvaloniaFact]
    public async Task DefaultProviderFactoryIsRealLrclibAndDoesNotSearchAtConstruction()
    {
        var pair = new EditorFactories().Provider();
        Assert.IsType<LrclibProvider>(pair.Provider);
        Assert.Same(pair.Provider, pair.Owner);
        await Task.Run(pair.Owner.Dispose);
    }
}
