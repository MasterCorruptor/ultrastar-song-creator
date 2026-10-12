using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;

namespace UltraStar.SongCreator.Desktop;

public sealed class EditorApplication : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new EditorWindow(new EditingSession(DemoSong.Create()));
        base.OnFrameworkInitializationCompleted();
    }
}
