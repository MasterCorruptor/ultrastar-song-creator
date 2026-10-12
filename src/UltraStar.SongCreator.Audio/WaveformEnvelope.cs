using System.Collections.Immutable;

namespace UltraStar.SongCreator.Audio;

public readonly record struct WaveformBucket(long StartFrame, long EndFrame, float Minimum, float Maximum);
public sealed record WaveformEnvelope(int SampleRate, long FrameCount, int FramesPerBucket, ImmutableArray<WaveformBucket> Buckets)
{
    public const int MaxBuckets = 100_000;
    public static WaveformEnvelope Create(PcmClip clip, int framesPerBucket)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (framesPerBucket <= 0) throw new ArgumentOutOfRangeException(nameof(framesPerBucket));
        var count = (clip.FrameCount + framesPerBucket - 1) / framesPerBucket;
        if (count > MaxBuckets) throw new ArgumentException("Waveform bucket count exceeds the bounded display profile.", nameof(framesPerBucket));
        var buckets = ImmutableArray.CreateBuilder<WaveformBucket>((int)count);
        for (long start = 0; start < clip.FrameCount; start += framesPerBucket)
        {
            var end = Math.Min(start + framesPerBucket, clip.FrameCount);
            var min = clip.GetSample(start, 0); var max = min;
            for (var frame = start; frame < end; frame++)
                for (var c = 0; c < clip.Channels; c++)
                {
                    var sample = clip.GetSample(frame, c);
                    min = Math.Min(min, sample); max = Math.Max(max, sample);
                }
            buckets.Add(new(start, end, min, max));
        }
        return new(clip.SampleRate, clip.FrameCount, framesPerBucket, buckets.MoveToImmutable());
    }
}
