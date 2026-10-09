// Phase 1 off-screen feasibility probe. No production song model or editor.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Media.Imaging;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Supply an output directory under repository .agent-local/.");
            return 2;
        }
        var repo = FindRepository(AppContext.BaseDirectory);
        var output = Path.GetFullPath(args[0]);
        var localRoot = Path.Combine(repo, ".agent-local") + Path.DirectorySeparatorChar;
        if (!output.StartsWith(localRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Outputs must remain under repository .agent-local/.");
        Directory.CreateDirectory(output);
        AppBuilder.Configure<ProbeApplication>().UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();
        var timeline = new SyntheticTimeline();
        var window = new Window { Width = 1200, Height = 600, Content = timeline };
        window.Show();
        using (var warm = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("Skia returned no frame")) { }
        var rows = new List<object>();
        var hashes = new HashSet<string>();
        foreach (var (start, duration) in new[] { (0.0, 8.0), (100.0, 8.0), (100.0, 20.0) })
        {
            timeline.Offset = start;
            timeline.Duration = duration;
            var samples = new List<double>();
            for (var i = 0; i < 30; i++)
            {
                var timer = Stopwatch.StartNew();
                timeline.InvalidateVisual();
                using var frame = window.CaptureRenderedFrame()
                    ?? throw new InvalidOperationException("Skia returned no frame");
                timer.Stop();
                samples.Add(timer.Elapsed.TotalMilliseconds);
                if (i == 29)
                {
                    frame.Save(Path.Combine(output, $"timeline-{start}-{duration}.png"), new PngBitmapEncoderOptions());
                    using var locked = frame.Lock();
                    var bytes = new byte[locked.RowBytes * locked.Size.Height];
                    Marshal.Copy(locked.Address, bytes, 0, bytes.Length);
                    hashes.Add(Convert.ToHexString(SHA256.HashData(bytes)));
                }
            }
            if (timeline.VisibleNotes <= 0 || timeline.VisibleNotes >= SyntheticTimeline.TotalNotes)
                throw new InvalidOperationException("Viewport culling was not exercised");
            samples.Sort();
            rows.Add(new { offset_seconds = start, viewport_seconds = duration,
                synthetic_note_count = SyntheticTimeline.TotalNotes,
                visible_note_count = timeline.VisibleNotes, repetitions = samples.Count,
                median_frame_ms = (samples[14] + samples[15]) / 2,
                p95_frame_ms = samples[(int)Math.Ceiling(samples.Count * 0.95) - 1] });
        }
        if (hashes.Count != 3)
            throw new InvalidOperationException("Zoom/scroll did not produce distinct pixels");
        var beforeWheel = timeline.Duration;
        window.MouseWheel(new Point(500, 200), new Vector(0, 1), RawInputModifiers.None);
        if (!(timeline.Duration < beforeWheel))
            throw new InvalidOperationException("Simulated wheel did not change viewport");
        var report = new { generated_utc = DateTimeOffset.UtcNow,
            status = "passed", framework = "Avalonia 12.1.3 / Skia headless",
            runtime = Environment.Version.ToString(), platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Other",
            rows, checks = new[] { "actual_pixel_capture", "viewport_culling", "distinct_zoom_scroll_frames", "simulated_pointer_wheel" },
            limitations = new[] { "no native window or compositor", "no playback/device test", "no text layout or production note editing",
                "30 offscreen repetitions per view; not a native 60 FPS guarantee", "Windows only; Linux not executed" } };
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(output, "timeline-report.json"), json + "\n");
        Console.WriteLine(json);
        window.Close();
        return 0;
    }

    private static string FindRepository(string path)
    {
        for (var current = new DirectoryInfo(path); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "docs", "PROJECT_MASTER.md")))
                return current.FullName;
        throw new InvalidOperationException("Cannot locate project repository");
    }
}

public sealed class ProbeApplication : Application { }

internal sealed class SyntheticTimeline : Control
{
    public const int TotalNotes = 10000;
    private readonly (double Start, double Length, int Row)[] _notes;
    private readonly IBrush _background = new SolidColorBrush(Color.Parse("#101B2B"));
    private readonly IBrush _noteBrush = new SolidColorBrush(Color.Parse("#46D9A0"));
    private readonly Pen _grid = new(new SolidColorBrush(Color.Parse("#243B53")), 1);
    public double Offset { get; set; }
    public double Duration { get; set; } = 8;
    public int VisibleNotes { get; private set; }

    public SyntheticTimeline()
    {
        var random = new Random(1947);
        _notes = Enumerable.Range(0, TotalNotes)
            .Select(i => (i * 300.0 / TotalNotes, 0.06 + random.NextDouble() * 0.2, random.Next(48)))
            .ToArray();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(_background, new Rect(Bounds.Size));
        using (context.PushClip(new Rect(Bounds.Size)))
        {
            for (var row = 0; row <= 48; row++)
                context.DrawLine(_grid, new Point(0, row * Bounds.Height / 48),
                    new Point(Bounds.Width, row * Bounds.Height / 48));
            VisibleNotes = 0;
            foreach (var note in _notes)
            {
                if (note.Start + note.Length < Offset || note.Start > Offset + Duration)
                    continue;
                var x = (note.Start - Offset) * Bounds.Width / Duration;
                var width = note.Length * Bounds.Width / Duration;
                context.FillRectangle(_noteBrush, new Rect(x, note.Row * Bounds.Height / 48 + 1,
                    width, Math.Max(1, Bounds.Height / 48 - 2)));
                VisibleNotes++;
            }
            context.DrawLine(new Pen(Brushes.White, 2), new Point(Bounds.Width * 0.4, 0),
                new Point(Bounds.Width * 0.4, Bounds.Height));
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        Duration = Math.Clamp(Duration * (e.Delta.Y > 0 ? 0.8 : 1.25), 1, 300);
        InvalidateVisual();
        e.Handled = true;
    }
}
