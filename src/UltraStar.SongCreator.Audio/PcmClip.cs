namespace UltraStar.SongCreator.Audio;

/// <summary>Owned immutable normalized interleaved float32 PCM. No file/device/model dependency.</summary>
public sealed class PcmClip
{
    public const int MaxSamples = 16_000_000;
    private readonly float[] samples;
    public int SampleRate { get; }
    public int Channels { get; }
    public long FrameCount => samples.Length / Channels;
    public double DurationSeconds => FrameCount / (double)SampleRate;

    public PcmClip(int sampleRate, int channels, ReadOnlySpan<float> samples)
    {
        if (sampleRate is < 8000 or > 192000) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (channels is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(channels));
        if (samples.Length > MaxSamples || samples.Length % channels != 0)
            throw new ArgumentException("PCM must contain bounded whole frames.", nameof(samples));
        var owned = samples.ToArray();
        foreach (var value in owned)
            if (!float.IsFinite(value) || value is < -1 or > 1)
                throw new ArgumentException("PCM samples must be finite and normalized to [-1, 1].", nameof(samples));
        SampleRate = sampleRate; Channels = channels; this.samples = owned;
    }
    public float GetSample(long frame, int channel)
    {
        if (frame < 0 || frame >= FrameCount) throw new ArgumentOutOfRangeException(nameof(frame));
        if (channel < 0 || channel >= Channels) throw new ArgumentOutOfRangeException(nameof(channel));
        return samples[checked((int)frame * Channels + channel)];
    }
    internal void CopyFrames(long start, Span<float> destination) =>
        samples.AsSpan(checked((int)start * Channels), destination.Length).CopyTo(destination);
}
