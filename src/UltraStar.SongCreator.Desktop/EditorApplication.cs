using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using UltraStar.SongCreator.Audio;
using UltraStar.SongCreator.Desktop.Playback;

namespace UltraStar.SongCreator.Desktop;

public sealed class EditorApplication : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var session = new EditingSession(DemoSong.Create());
            var playback = new EditorPlaybackSession(static clip => new MiniAudioPlayback(clip), session.Current.AudioOffsetSeconds);
            try
            {
                var window = new EditorWindow(session, playback);
                desktop.MainWindow = window;
                desktop.Exit += (_, _) => window.ReleasePlayback();
            }
            catch { playback.Dispose(); throw; }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
