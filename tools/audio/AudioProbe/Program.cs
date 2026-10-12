using System.Diagnostics;
using System.Text.Json;
using UltraStar.SongCreator.Audio;

if (args.Length != 2 || args[0] is not ("--native" or "--load-only"))
    throw new ArgumentException("Usage: AudioProbe --native|--load-only <repository .agent-local output-directory>");
var repo = new DirectoryInfo(AppContext.BaseDirectory);
while (repo is not null && !File.Exists(Path.Combine(repo.FullName, "AGENTS.md"))) repo = repo.Parent;
if (repo is null) throw new InvalidOperationException("Cannot locate repository.");
var output = Path.GetFullPath(args[1]);
var local = Path.Combine(repo.FullName, ".agent-local") + Path.DirectorySeparatorChar;
if (!output.StartsWith(local, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
    throw new ArgumentException("Output must remain in repository .agent-local.");
Directory.CreateDirectory(output);
var version = MiniAudioPlayback.NativeVersion;
if (args[0] == "--load-only")
{
    Report(new { status = "passed", mode = "native-library-load-only", version, physical_device = false });
    return;
}
Console.WriteLine("Short default-device probe: own low-amplitude tone (peak 0.02), no external media.");
using var playback = new MiniAudioPlayback(DemoPcm.Create());
Check(playback.Snapshot.State == PlaybackState.Stopped, "Initial state");
playback.Play(); Wait(() => playback.Snapshot.PositionFrames >= 4800, "Native render cursor did not advance");
playback.Pause(); var paused = playback.Snapshot; Thread.Sleep(120);
Check(playback.Snapshot == paused, "Paused cursor advanced");
playback.SeekSeconds(.5); Check(playback.Snapshot.PositionFrames == 24000, "Paused seek is not exact");
playback.Play(); Wait(() => playback.Snapshot.PositionFrames > 28800, "Resume failed");
playback.Pause(); playback.SeekFrame(playback.Clip.FrameCount);
Check(playback.Snapshot.State == PlaybackState.Ended, "Endpoint seek failed");
playback.Play(); Check(playback.Snapshot.State == PlaybackState.Ended, "Endpoint play restarted silently");
playback.SeekFrame(playback.Clip.FrameCount - 2400); playback.Play();
Wait(() => playback.Snapshot.State == PlaybackState.Ended, "Native EOS failed");
playback.Stop(); Check(playback.Snapshot.PositionFrames == 0 && playback.Snapshot.State == PlaybackState.Stopped, "Stop failed");
playback.Play();
using var commandStarted = new ManualResetEventSlim();
var commandCount = 0;
var commands = Task.Run(() =>
{
    for (var i = 0; ; i = (i + 1) % 500)
    {
        try
        {
            playback.SeekFrame(i * 100); playback.Pause(); playback.Play();
            Interlocked.Increment(ref commandCount); commandStarted.Set();
        }
        catch (ObjectDisposedException) { return true; }
    }
});
Check(commandStarted.Wait(2000), "Concurrent command loop did not start");
var closing = Stopwatch.StartNew(); playback.Dispose(); closing.Stop();
Check(commands.Wait(2000) && commands.Result && commandCount > 0, "Concurrent native command did not observe disposal");
var disposed = playback.Snapshot; Thread.Sleep(120);
Check(disposed.State == PlaybackState.Disposed && playback.Snapshot == disposed, "Cursor changed after native disposal");
playback.Dispose();
Report(new
{
    status = "passed",
    mode = "native-default-device",
    version,
    backend = playback.DeviceBackend,
    sample_rate = 48000,
    channels = 2,
    pause_frame = paused.PositionFrames,
    pause_stable = true,
    seek_frame = 24000,
    resume = true,
    eos = true,
    stop = true,
    concurrent_commands_disposal = true,
    concurrent_command_count = commandCount,
    disposed_stable = true,
    dispose_ms = closing.Elapsed.TotalMilliseconds,
    limitations = new[] { "source render-position, queued output may lag", "no measured acoustic latency or owner hearing confirmation", "one default device, no hotplug/device switch" }
});
void Report(object value)
{
    var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(Path.Combine(output, "result.json"), json); Console.WriteLine(json);
}
void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
void Wait(Func<bool> predicate, string message)
{
    var t = Stopwatch.StartNew();
    while (!predicate() && t.ElapsedMilliseconds < 3000)
    {
        if (playback.Snapshot.State == PlaybackState.Faulted) throw new InvalidOperationException(playback.Snapshot.Error);
        Thread.Sleep(10);
    }
    Check(predicate(), message);
}
