using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using UltraStar.SongCreator.Acquisition.Lyrics;
using UltraStar.SongCreator.Audio;
using UltraStar.SongCreator.Desktop.Lyrics;
using UltraStar.SongCreator.Desktop.Playback;

namespace UltraStar.SongCreator.Desktop;

/// <summary>Factories own incomplete acquisitions until they successfully return; returned resources transfer immediately.</summary>
internal sealed class EditorFactories
{
    internal Func<double, EditorPlaybackSession> Playback { get; init; } =
        offset => new EditorPlaybackSession(static clip => new MiniAudioPlayback(clip), offset);
    internal Func<(ILyricsSearchProvider Provider, IDisposable Owner)> Provider { get; init; } = () =>
    {
        var provider = new LrclibProvider();
        return (provider, provider);
    };
    internal Func<ILyricsSearchProvider, LyricsAcquisitionSession> Acquisition { get; init; } = p => new(p);
    internal Func<EditingSession, LyricsPanelLifetime, LyricsPanel> Panel { get; init; } = (e, l) => new(e, l);
    internal Func<EditingSession, EditorPlaybackSession, LyricsPanel, Func<Task<bool>>, EditorWindow> Window { get; init; } =
        (e, p, l, close) => new(e, p, l, close);
}

/// <summary>UI-owned composition; one cleanup path covers all returned partial acquisitions and normal closure.</summary>
internal sealed class EditorComposition(EditorFactories factories)
{
    private IDisposable? providerOwner;
    private LyricsPanelLifetime? lyricsLifetime;
    private Task<bool>? cleanup;
    internal EditingSession Editing { get; } = new(DemoSong.Create());
    internal EditorPlaybackSession? Playback { get; private set; }
    internal LyricsAcquisitionSession? Acquisition { get; private set; }
    internal LyricsPanel? Panel { get; private set; }
    internal EditorWindow? Window { get; private set; }
    internal bool StartupFailed { get; private set; }

    internal bool TryStart(Action<EditorWindow> attach)
    {
        Dispatcher.UIThread.VerifyAccess();
        try
        {
            Playback = factories.Playback(Editing.Current.AudioOffsetSeconds);
            var provider = factories.Provider();
            providerOwner = provider.Owner;
            Acquisition = factories.Acquisition(provider.Provider);
            lyricsLifetime = new(Acquisition, providerOwner);
            Panel = factories.Panel(Editing, lyricsLifetime);
            Window = factories.Window(Editing, Playback, Panel, CleanupAsync);
            attach(Window);
            return true;
        }
        catch (Exception) { StartupFailed = true; return false; }
    }

    internal void ReleaseAudio()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (Window is not null) Window.ReleasePlayback();
        else Playback?.Dispose();
    }

    internal Task<bool> CleanupAsync()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (cleanup is not null) return cleanup;
        var uiReleased = true;
        try { Panel?.Detach(); }
        catch (Exception) { uiReleased = false; }
        try { ReleaseAudio(); }
        catch (Exception) { uiReleased = false; }
        Task<bool> drain;
        try
        {
            drain = Panel?.CloseAsync() ?? lyricsLifetime?.CloseAsync() ?? Task.Run(() =>
            {
                try { providerOwner?.Dispose(); return true; }
                catch (Exception) { return false; }
            });
        }
        catch (Exception) { drain = Task.FromResult(false); }
        cleanup = CombineAsync(drain, uiReleased);
        return cleanup;
    }

    private static async Task<bool> CombineAsync(Task<bool> drain, bool uiReleased)
    {
        try { return await drain.ConfigureAwait(false) && uiReleased; }
        catch (Exception) { return false; }
    }
}

// Avalonia's lifetime interfaces intentionally cannot be implemented by clients.
// These app-specific callbacks keep tests on the same shutdown/composition path.
internal sealed record EditorDesktopHost(Action KeepAlive, Action<Window> AssignMainWindow, Action<int> Shutdown,
    Action<Action> ObserveExit, Action<Func<bool>> ObserveShutdownRequest);

public sealed class EditorApplication : Application
{
    private bool shutdownAllowed;
    internal EditorComposition? Composition { get; private set; }
    internal Task<bool>? StartupCleanupTask { get; private set; }
    internal Window? StartupFailureWindow { get; private set; }
    internal TextBlock StartupStatus { get; } = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };

    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) StartDesktop(desktop, new());
        base.OnFrameworkInitializationCompleted();
    }

    private void StartDesktop(IClassicDesktopStyleApplicationLifetime desktop, EditorFactories factories) =>
        StartDesktop(new EditorDesktopHost(
            () => desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown,
            window => desktop.MainWindow = window,
            code => desktop.Shutdown(code),
            callback => desktop.Exit += (_, _) => callback(),
            callback => desktop.ShutdownRequested += (_, e) => { if (!callback()) e.Cancel = true; }), factories);

    internal void StartDesktop(EditorDesktopHost desktop, EditorFactories factories)
    {
        Dispatcher.UIThread.VerifyAccess();
        // Keep the app alive when startup or the last window is waiting on asynchronous cleanup.
        desktop.KeepAlive();
        Composition = new(factories);
        desktop.ObserveExit(() =>
        {
            try { Composition.ReleaseAudio(); }
            catch (Exception) { /* Hard process/OS exit cannot promise orderly cleanup. */ }
        });
        desktop.ObserveShutdownRequest(() =>
        {
            if (shutdownAllowed) return true;
            if (!Composition.StartupFailed) Composition.Window?.Close();
            else StartupFailureWindow?.Close();
            return false;
        });
        if (Composition.TryStart(window => desktop.AssignMainWindow(window)))
        {
            Composition.Window!.Closed += (_, _) =>
            {
                shutdownAllowed = true;
                desktop.Shutdown(0);
            };
            return;
        }

        var drain = Composition.CleanupAsync(); // UI audio is released before any owned lyrics teardown.
        StartupStatus.Text = "Programmet kunne ikke startes. Rydder opp før avslutning …";
        StartupFailureWindow = new Window
        {
            Title = "UltraStar Song Creator — oppstart",
            Width = 560,
            Height = 180,
            Content = new StackPanel { Margin = new Thickness(20), Children = { StartupStatus } }
        };
        StartupFailureWindow.Closing += (_, e) => { if (!shutdownAllowed) e.Cancel = true; };
        // A failed assignment of the editor does not leave an unowned editor or a fake fallback.
        // The small status window is independent of the failing editor composition.
        try { desktop.AssignMainWindow(StartupFailureWindow); StartupFailureWindow.Show(); }
        catch (Exception) { StartupStatus.Text = "Oppstart feilet. Opprydding pågår."; }
        StartupCleanupTask = ObserveStartupCleanupAsync(desktop, drain);
    }

    private async Task<bool> ObserveStartupCleanupAsync(EditorDesktopHost desktop, Task<bool> drain)
    {
        bool success;
        try { success = await drain.ConfigureAwait(false); }
        catch (Exception) { success = false; }
        var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (success)
                {
                    Composition?.Window?.FinishClose();
                    shutdownAllowed = true;
                    StartupFailureWindow?.Close();
                    desktop.Shutdown(1);
                }
                else StartupStatus.Text = "Opprydding feilet. Programmet holdes åpent.";
                completed.TrySetResult(success);
            }
            catch (Exception)
            {
                StartupStatus.Text = "Programmet kunne ikke avsluttes.";
                completed.TrySetResult(false);
            }
        });
        return await completed.Task.ConfigureAwait(false);
    }
}
