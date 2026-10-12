using Avalonia;

namespace UltraStar.SongCreator.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<EditorApplication>().UsePlatformDetect();
}
