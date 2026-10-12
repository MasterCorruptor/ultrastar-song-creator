using System.Globalization;
using Avalonia.Threading;
using UltraStar.SongCreator.Audio;
using UltraStar.SongCreator.Desktop.Timeline;

namespace UltraStar.SongCreator.Desktop.Playback;

public readonly record struct PlaybackViewState(PlaybackSnapshot Snapshot,
    double? SongPositionSeconds, double? PlayheadX,
    bool CanPlay, bool CanPause, bool CanStop, bool CanSeek, string? Error);

/// <summary>UI-thread presentation of a borrowed backend. Never creates a device or invents elapsed audio time.</summary>
public sealed class PlaybackPresenter : IDisposable
{
    private readonly IPcmPlayback playback;
    private readonly PcmClip clip;
    private bool disposed;
    private string? commandError;
    public TimelineMapping Mapping { get; }
    public double AudioOffsetSeconds { get; private set; }
    public PlaybackViewState ViewState { get; private set; }
    public event EventHandler? Changed;

    public PlaybackPresenter(IPcmPlayback playback, TimelineMapping mapping, double audioOffsetSeconds = 0)
    {
        Dispatcher.UIThread.VerifyAccess();
        this.playback = playback ?? throw new ArgumentNullException(nameof(playback));
        clip = playback.Clip ?? throw new ArgumentException("Playback must have an immutable PCM clip.", nameof(playback));
        Mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
        if (!double.IsFinite(audioOffsetSeconds)) throw new ArgumentOutOfRangeException(nameof(audioOffsetSeconds));
        AudioOffsetSeconds = audioOffsetSeconds;
        Refresh();
    }
    public void Refresh()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (disposed) return;
        var snapshot = ViewState.Snapshot;
        string? readError = null;
        try { snapshot = playback.Snapshot; }
        catch (Exception e) { readError = "Kunne ikke lese avspillingsstatus: " + e.Message; }
        var song = readError is null && snapshot.SampleRate == clip.SampleRate && snapshot.FrameCount == clip.FrameCount
            ? Mapping.SnapshotToSongSeconds(snapshot, AudioOffsetSeconds) : null;
        var valid = song.HasValue;
        double? x = song is { } seconds && Mapping.TrySongToX(seconds, out var value) ? value : null;
        var usable = valid && clip.FrameCount > 0;
        var canPlay = usable && (snapshot.State is PlaybackState.Stopped or PlaybackState.Paused) && snapshot.PositionFrames < snapshot.FrameCount;
        var canPause = usable && snapshot.State == PlaybackState.Playing;
        var canStop = usable && (snapshot.State is PlaybackState.Playing or PlaybackState.Paused or PlaybackState.Ended || snapshot.PositionFrames > 0);
        var error = valid ? commandError : readError ?? snapshot.Error ?? "Avspillingen er utilgjengelig eller har ugyldig status.";
        ViewState = new(snapshot, song, x, canPlay, canPause, canStop, usable, error);
        Changed?.Invoke(this, EventArgs.Empty);
    }
    public void SetAudioOffset(double seconds)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (!double.IsFinite(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (disposed) return;
        AudioOffsetSeconds = seconds; commandError = null; Refresh();
    }
    private bool Execute(Action action, Func<PlaybackViewState, bool> allowed)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (disposed) return false;
        Refresh();
        if (!allowed(ViewState)) return false;
        try { action(); commandError = null; }
        catch (Exception e) { commandError = "Avspillingshandlingen feilet: " + e.Message; Refresh(); return false; }
        Refresh(); return true;
    }
    public void Play() => Execute(playback.Play, static v => v.CanPlay);
    public void Pause() => Execute(playback.Pause, static v => v.CanPause);
    public void Stop() => Execute(playback.Stop, static v => v.CanStop);
    public bool SeekSongSeconds(double seconds)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (disposed) return false;
        if (!Mapping.TrySongToSourceFrame(seconds, AudioOffsetSeconds, clip.SampleRate, clip.FrameCount, out var frame))
        {
            commandError = "Starttid må være et endelig tall innenfor sangområdet og lydklippet.";
            Refresh(); return false;
        }
        return Execute(() => playback.SeekFrame(frame), static v => v.CanSeek);
    }
    public bool SeekSongText(string? text)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (disposed) return false;
        if (text is null || text.Length > 128 || !double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            commandError = "Skriv starttid i sangsekunder, for eksempel 2,5.";
            Refresh(); return false;
        }
        return SeekSongSeconds(seconds);
    }
    public void Dispose()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (disposed) return;
        disposed = true;
        ViewState = new(ViewState.Snapshot, null, null, false, false, false, false, "Avspillingsvisningen er avsluttet.");
        Changed?.Invoke(this, EventArgs.Empty);
        Changed = null;
        // The application owns the backend and stops/disposes it after borrowers are detached.
    }
}
