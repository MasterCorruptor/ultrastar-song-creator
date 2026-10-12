namespace UltraStar.SongCreator.Audio;

public enum PlaybackState { Stopped, Playing, Paused, Ended, Faulted, Disposed }
public readonly record struct PlaybackSnapshot(PlaybackState State, long PositionFrames, int SampleRate, long FrameCount, string? Error = null)
{
    public double PositionSeconds => PositionFrames / (double)SampleRate;
}

public interface IPcmPlayback : IDisposable
{
    PcmClip Clip { get; }
    PlaybackSnapshot Snapshot { get; }
    void Play();
    void Pause();
    void Stop();
    void SeekFrame(long frame);
    void SeekSeconds(double seconds);
}

/// <summary>The source renderer used by both explicit no-device callers and the native callback.</summary>
public sealed class PcmPlayback(PcmClip clip) : IPcmPlayback
{
    private readonly object gate = new();
    private long position;
    private PlaybackState state = PlaybackState.Stopped;
    private string? error;
    public PcmClip Clip { get; } = clip ?? throw new ArgumentNullException(nameof(clip));
    public PlaybackSnapshot Snapshot { get { lock (gate) return new(state, position, Clip.SampleRate, Clip.FrameCount, error); } }

    private void EnsureActive()
    {
        ObjectDisposedException.ThrowIf(state == PlaybackState.Disposed, this);
        if (state == PlaybackState.Faulted) throw new InvalidOperationException(error);
    }
    public void Play()
    {
        lock (gate) { EnsureActive(); state = position == Clip.FrameCount ? PlaybackState.Ended : PlaybackState.Playing; }
    }
    public void Pause()
    {
        lock (gate) { EnsureActive(); if (state == PlaybackState.Playing) state = PlaybackState.Paused; }
    }
    public void Stop()
    {
        lock (gate) { EnsureActive(); position = 0; state = PlaybackState.Stopped; }
    }
    public void SeekFrame(long frame)
    {
        lock (gate)
        {
            EnsureActive();
            if (frame < 0 || frame > Clip.FrameCount) throw new ArgumentOutOfRangeException(nameof(frame));
            position = frame;
            if (position == Clip.FrameCount) state = PlaybackState.Ended;
            else if (state == PlaybackState.Ended) state = PlaybackState.Paused;
        }
    }
    public void SeekSeconds(double seconds)
    {
        lock (gate)
        {
            EnsureActive();
            if (!double.IsFinite(seconds) || seconds < 0 || seconds > Clip.DurationSeconds)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            SeekFrame((long)Math.Round(seconds * Clip.SampleRate, MidpointRounding.AwayFromZero));
        }
    }
    public void Render(Span<float> output)
    {
        if (output.Length % Clip.Channels != 0) throw new ArgumentException("Output must contain whole PCM frames.", nameof(output));
        lock (gate)
        {
            output.Clear();
            if (state != PlaybackState.Playing) return;
            var frames = Math.Min(output.Length / Clip.Channels, Clip.FrameCount - position);
            Clip.CopyFrames(position, output[..checked((int)frames * Clip.Channels)]);
            position += frames;
            if (position == Clip.FrameCount) state = PlaybackState.Ended;
        }
    }
    internal void FailCallback()
    {
        lock (gate)
        {
            if (state == PlaybackState.Disposed) return;
            state = PlaybackState.Faulted; error = "Audio callback rendering failed. Dispose and recreate playback.";
        }
    }
    public void Dispose() { lock (gate) state = PlaybackState.Disposed; }
}
