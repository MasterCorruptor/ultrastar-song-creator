using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using UltraStar.SongCreator.Audio;
using UltraStar.SongCreator.Desktop.Timeline;

namespace UltraStar.SongCreator.Desktop.Controls;

/// <summary>Displays borrowed immutable source envelopes using the shared song timeline.</summary>
public sealed class WaveformControl : Control
{
    private static readonly IBrush WaveformBrush = new SolidColorBrush(Color.Parse("#49B8AD"));
    private static readonly Pen PlayheadPen = new(Brushes.OrangeRed, 2);
    private double audioOffsetSeconds;
    private double? playheadSongSeconds;

    public WaveformControl(WaveformEnvelope envelope, TimelineMapping mapping, double audioOffsetSeconds = 0)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(mapping);
        ValidateEnvelope(envelope);
        if (!double.IsFinite(audioOffsetSeconds)) throw new ArgumentOutOfRangeException(nameof(audioOffsetSeconds));
        Envelope = envelope; Mapping = mapping; this.audioOffsetSeconds = audioOffsetSeconds;
        Width = 980; Height = 120; ClipToBounds = true;
    }

    public WaveformEnvelope Envelope { get; }
    public TimelineMapping Mapping { get; }
    public double AudioOffsetSeconds
    {
        get => audioOffsetSeconds;
        set
        {
            if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (audioOffsetSeconds == value) return;
            audioOffsetSeconds = value; InvalidateVisual();
        }
    }
    public double? PlayheadSongSeconds
    {
        get => playheadSongSeconds;
        set
        {
            if (value.HasValue && !double.IsFinite(value.Value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (playheadSongSeconds == value) return;
            playheadSongSeconds = value; InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var height = Bounds.Height;
        if (height <= 0 || Bounds.Width <= 0) return;
        using (context.PushClip(new Rect(Bounds.Size)))
        {
            foreach (var bucket in Envelope.Buckets)
            {
                if (!Mapping.TrySourceIntervalToX(bucket.StartFrame, bucket.EndFrame, Envelope.SampleRate,
                    Envelope.FrameCount, AudioOffsetSeconds, out var span)) continue;
                var top = Math.Min((1 - bucket.Maximum) * height / 2, Math.Max(0, height - 1));
                var bottom = (1 - bucket.Minimum) * height / 2;
                // Keep silence/constant samples visible without changing their source-time extent.
                context.DrawRectangle(WaveformBrush, null,
                    new Rect(span.LeftX, top, span.Width, Math.Max(1, bottom - top)));
            }
            if (PlayheadSongSeconds is double seconds && Mapping.TrySongToX(seconds, out var x))
                context.DrawLine(PlayheadPen, new Point(x, 0), new Point(x, height));
        }
    }

    private static void ValidateEnvelope(WaveformEnvelope envelope)
    {
        if (envelope.SampleRate is < 8000 or > 192000 || envelope.FrameCount < 0
            || envelope.FrameCount > PcmClip.MaxSamples || envelope.FramesPerBucket <= 0 || envelope.Buckets.IsDefault)
            throw new ArgumentException("Waveform metadata must match the bounded PCM profile.", nameof(envelope));
        var expectedCount = (envelope.FrameCount + envelope.FramesPerBucket - 1) / envelope.FramesPerBucket;
        if (expectedCount > WaveformEnvelope.MaxBuckets || envelope.Buckets.Length != expectedCount)
            throw new ArgumentException("Waveform buckets must cover the source clip exactly.", nameof(envelope));
        long start = 0;
        foreach (var bucket in envelope.Buckets)
        {
            var end = Math.Min(start + envelope.FramesPerBucket, envelope.FrameCount);
            if (bucket.StartFrame != start || bucket.EndFrame != end || bucket.EndFrame <= bucket.StartFrame
                || !float.IsFinite(bucket.Minimum) || !float.IsFinite(bucket.Maximum)
                || bucket.Minimum < -1 || bucket.Maximum > 1 || bucket.Minimum > bucket.Maximum)
                throw new ArgumentException("Waveform boundaries and normalized amplitudes must match the factory profile.", nameof(envelope));
            start = end;
        }
    }
}
