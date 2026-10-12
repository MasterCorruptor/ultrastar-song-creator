using UltraStar.SongCreator.Audio;

namespace UltraStar.SongCreator.Desktop.Timeline;

public readonly record struct TimelineSpan(double LeftX, double RightX)
{
    public double Width => RightX - LeftX;
}

/// <summary>Pure shared projection. Source PCM frames are authoritative; song offsets never mutate PCM/history.</summary>
public sealed class TimelineMapping
{
    public double SongStartSeconds { get; }
    public double SongEndSeconds { get; }
    public double OriginX { get; }
    public double PixelsPerSecond { get; }
    public double RightX { get; }
    public static TimelineMapping Demo { get; } = new();

    public TimelineMapping(double songStartSeconds = 0, double songEndSeconds = 8, double originX = 75, double pixelsPerSecond = 110)
    {
        if (!double.IsFinite(songStartSeconds) || songStartSeconds < 0) throw new ArgumentOutOfRangeException(nameof(songStartSeconds));
        if (!double.IsFinite(songEndSeconds) || songEndSeconds <= songStartSeconds) throw new ArgumentOutOfRangeException(nameof(songEndSeconds));
        if (!double.IsFinite(originX) || originX < 0) throw new ArgumentOutOfRangeException(nameof(originX));
        if (!double.IsFinite(pixelsPerSecond) || pixelsPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(pixelsPerSecond));
        var right = originX + (songEndSeconds - songStartSeconds) * pixelsPerSecond;
        if (!double.IsFinite(right) || right <= originX) throw new ArgumentException("Viewport must have a finite, representable positive pixel extent.");
        SongStartSeconds = songStartSeconds; SongEndSeconds = songEndSeconds;
        OriginX = originX; PixelsPerSecond = pixelsPerSecond; RightX = right;
    }
    public bool TrySongToX(double songSeconds, out double x)
    {
        x = default;
        if (!double.IsFinite(songSeconds) || songSeconds < SongStartSeconds || songSeconds > SongEndSeconds) return false;
        x = OriginX + (songSeconds - SongStartSeconds) * PixelsPerSecond;
        return double.IsFinite(x);
    }
    public bool TryXToSong(double x, out double songSeconds)
    {
        songSeconds = default;
        if (!double.IsFinite(x) || x < OriginX || x > RightX) return false;
        songSeconds = SongStartSeconds + (x - OriginX) / PixelsPerSecond;
        return double.IsFinite(songSeconds) && songSeconds >= SongStartSeconds && songSeconds <= SongEndSeconds;
    }
    private static bool ValidClip(int sampleRate, long frames) =>
        sampleRate is >= 8000 and <= 192000 && frames >= 0 && frames <= PcmClip.MaxSamples;

    public bool TrySongToSourceFrame(double songSeconds, double audioOffsetSeconds, int sampleRate, long frameCount, out long frame)
    {
        frame = default;
        if (!TrySongToX(songSeconds, out _) || !double.IsFinite(audioOffsetSeconds) || !ValidClip(sampleRate, frameCount)) return false;
        var sourceSeconds = songSeconds + audioOffsetSeconds;
        if (!double.IsFinite(sourceSeconds) || sourceSeconds < 0 || sourceSeconds > frameCount / (double)sampleRate) return false;
        frame = (long)Math.Round(sourceSeconds * sampleRate, MidpointRounding.AwayFromZero);
        return frame >= 0 && frame <= frameCount;
    }
    public double? SnapshotToSongSeconds(PlaybackSnapshot snapshot, double audioOffsetSeconds)
    {
        if (snapshot.State is not (PlaybackState.Stopped or PlaybackState.Playing or PlaybackState.Paused or PlaybackState.Ended)
            || !double.IsFinite(audioOffsetSeconds) || !ValidClip(snapshot.SampleRate, snapshot.FrameCount)
            || snapshot.PositionFrames < 0 || snapshot.PositionFrames > snapshot.FrameCount) return null;
        var seconds = snapshot.PositionFrames / (double)snapshot.SampleRate - audioOffsetSeconds;
        return double.IsFinite(seconds) ? seconds : null;
    }
    public bool TrySourceIntervalToX(long startFrame, long endFrame, int sampleRate, long frameCount, double audioOffsetSeconds, out TimelineSpan span)
    {
        span = default;
        if (!ValidClip(sampleRate, frameCount) || !double.IsFinite(audioOffsetSeconds)
            || startFrame < 0 || endFrame <= startFrame || endFrame > frameCount) return false;
        var start = startFrame / (double)sampleRate - audioOffsetSeconds;
        var end = endFrame / (double)sampleRate - audioOffsetSeconds;
        if (!double.IsFinite(start) || !double.IsFinite(end)) return false;
        start = Math.Max(start, SongStartSeconds); end = Math.Min(end, SongEndSeconds);
        if (end <= start || !TrySongToX(start, out var left) || !TrySongToX(end, out var right)) return false;
        span = new(left, right); return span.Width > 0;
    }
}
