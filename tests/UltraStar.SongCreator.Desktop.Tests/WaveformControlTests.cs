using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Skia.Helpers;
using Avalonia.Threading;
using SkiaSharp;
using UltraStar.SongCreator.Audio;
using UltraStar.SongCreator.Desktop.Controls;
using UltraStar.SongCreator.Desktop.Timeline;

namespace UltraStar.SongCreator.Desktop.Tests;

public sealed class WaveformControlTests
{
    private static WaveformEnvelope CreateEnvelope()
    {
        var samples = new float[18000];
        for (var i = 0; i < 8000; i++) samples[i] = i % 2 == 0 ? -.5f : .75f;
        for (var i = 8000; i < 16000; i++) samples[i] = i % 2 == 0 ? -.25f : .5f;
        for (var i = 16000; i < samples.Length; i++) samples[i] = 1;
        return WaveformEnvelope.Create(new PcmClip(8000, 1, samples), 8000);
    }

    // Local canvas rendering leaves the shared headless application unchanged and never opens audio.
    private static async Task<SKBitmap> Draw(WaveformControl control)
    {
        control.Measure(new Size(980, 120)); control.Arrange(new Rect(0, 0, 980, 120));
        var bitmap = new SKBitmap(980, 120);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        await DrawingContextHelper.RenderAsync(canvas, control);
        return bitmap;
    }
    private static void Wave(SKBitmap image, int x, int y) => Assert.Equal(new SKColor(0x49, 0xB8, 0xAD), image.GetPixel(x, y));
    private static void Empty(SKBitmap image, int x, int y) => Assert.Equal((byte)0, image.GetPixel(x, y).Alpha);

    [AvaloniaFact]
    public async Task HeadlessLayoutBorrowsObjectsAndRendersFactoryMinMaxAndExactPartialEnd()
    {
        var envelope = CreateEnvelope(); var mapping = TimelineMapping.Demo;
        var control = new WaveformControl(envelope, mapping);
        var window = new Window { Width = 1000, Height = 140, Content = control };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(980, control.Bounds.Width); Assert.Equal(120, control.Bounds.Height);
            Assert.Same(envelope, control.Envelope); Assert.Same(mapping, control.Mapping);
            Assert.Null(control.PlayheadSongSeconds);
            using var image = await Draw(control);
            Empty(image, 74, 40); Wave(image, 100, 16); Wave(image, 100, 89); Empty(image, 100, 14); Empty(image, 100, 91);
            Wave(image, 200, 31); Empty(image, 200, 29); Wave(image, 200, 74); Empty(image, 200, 76);
            Wave(image, 310, 0); Empty(image, 323, 0);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task OffsetUpdatesReprojectSourceAndBothViewportEdgesClipWithoutRescaling()
    {
        var control = new WaveformControl(CreateEnvelope(), new TimelineMapping(.5, 1.5, 20, 100));
        using (var image = await Draw(control))
        {
            Empty(image, 19, 40); Wave(image, 21, 20); Wave(image, 69, 20);
            Empty(image, 71, 20); Wave(image, 71, 40); Wave(image, 119, 40); Empty(image, 121, 40);
        }
        control.AudioOffsetSeconds = .5;
        using (var image = await Draw(control)) { Empty(image, 21, 20); Wave(image, 21, 40); Wave(image, 119, 40); Empty(image, 121, 40); }
        control.AudioOffsetSeconds = -.5;
        using (var image = await Draw(control)) { Wave(image, 21, 20); Wave(image, 119, 20); Empty(image, 121, 20); }
        control.AudioOffsetSeconds = double.MaxValue;
        using (var image = await Draw(control)) Empty(image, 50, 40);
    }

    [AvaloniaFact]
    public async Task EmptyEnvelopeAndExternallySuppliedPlayheadRenderAndHideAtBounds()
    {
        var envelope = WaveformEnvelope.Create(new PcmClip(8000, 1, []), 100);
        var control = new WaveformControl(envelope, TimelineMapping.Demo);
        using (var image = await Draw(control)) Empty(image, 185, 40);
        foreach (var song in new[] { 0d, 1, 8 })
        {
            control.PlayheadSongSeconds = song;
            using var image = await Draw(control);
            Assert.Equal(SKColors.OrangeRed, image.GetPixel((int)(75 + song * 110), 40));
        }
        foreach (var song in new double?[] { -1, 9, null })
        {
            control.PlayheadSongSeconds = song;
            using var image = await Draw(control); Empty(image, 185, 40);
        }
        control.PlayheadSongSeconds = 1; control.AudioOffsetSeconds = 2;
        using (var image = await Draw(control)) Assert.Equal(SKColors.OrangeRed, image.GetPixel(185, 40));
    }

    [AvaloniaFact]
    public async Task StereoExtremaSilenceAndNegativeFullScaleRemainVisible()
    {
        var samples = new float[48000];
        for (var frame = 0; frame < 8000; frame++)
        {
            samples[frame * 2] = -.75f;
            samples[frame * 2 + 1] = .5f;
        }
        for (var i = 32000; i < samples.Length; i++) samples[i] = -1;
        var envelope = WaveformEnvelope.Create(new PcmClip(8000, 2, samples), 8000);
        var control = new WaveformControl(envelope, TimelineMapping.Demo);
        using var image = await Draw(control);
        Wave(image, 100, 31); Wave(image, 100, 104); Empty(image, 100, 106);
        Wave(image, 200, 60); Empty(image, 200, 59); Empty(image, 200, 62);
        Wave(image, 310, 119); Empty(image, 310, 117);
        Assert.Same(envelope, control.Envelope);
    }
    [AvaloniaTheory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void InvalidDisplayValuesAreRejectedAndPreserveExistingState(double value)
    {
        var control = new WaveformControl(CreateEnvelope(), TimelineMapping.Demo, .5) { PlayheadSongSeconds = 1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => new WaveformControl(control.Envelope, control.Mapping, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => control.AudioOffsetSeconds = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => control.PlayheadSongSeconds = value);
        Assert.Equal(.5, control.AudioOffsetSeconds); Assert.Equal(1, control.PlayheadSongSeconds);
    }

    [AvaloniaFact]
    public void MalformedMetadataAndBucketsAreRejectedBeforeAnyRendering()
    {
        var valid = CreateEnvelope();
        var malformed = new[]
        {
            valid with { SampleRate = 7999 }, valid with { SampleRate = 192001 },
            valid with { FrameCount = -1 }, valid with { FrameCount = PcmClip.MaxSamples + 1L },
            valid with { FramesPerBucket = 0 }, valid with { FramesPerBucket = -1 },
            valid with { Buckets = default }, valid with { Buckets = ImmutableArray<WaveformBucket>.Empty },
            valid with { FrameCount = 0 }, valid with { FramesPerBucket = 1 },
            valid with { Buckets = valid.Buckets.SetItem(0, valid.Buckets[0] with { StartFrame = -1 }) },
            valid with { Buckets = valid.Buckets.SetItem(0, valid.Buckets[0] with { EndFrame = 0 }) },
            valid with { Buckets = valid.Buckets.SetItem(1, valid.Buckets[1] with { StartFrame = 7999 }) },
            valid with { Buckets = valid.Buckets.SetItem(1, valid.Buckets[1] with { StartFrame = 8001 }) },
            valid with { Buckets = valid.Buckets.SetItem(2, valid.Buckets[2] with { EndFrame = 18001 }) },
            valid with { Buckets = valid.Buckets.SetItem(2, valid.Buckets[2] with { EndFrame = 17999 }) }
        };
        foreach (var envelope in malformed) Assert.Throws<ArgumentException>(() => new WaveformControl(envelope, TimelineMapping.Demo));
        foreach (var (minimum, maximum) in new[] { (float.NaN, 1f), (-1f, float.NaN), (float.NegativeInfinity, 1f), (-1f, float.PositiveInfinity), (-1.01f, 1f), (-1f, 1.01f), (.5f, -.5f) })
        {
            var envelope = valid with { Buckets = valid.Buckets.SetItem(0, valid.Buckets[0] with { Minimum = minimum, Maximum = maximum }) };
            Assert.Throws<ArgumentException>(() => new WaveformControl(envelope, TimelineMapping.Demo));
        }
        Assert.Throws<ArgumentNullException>(() => new WaveformControl(null!, TimelineMapping.Demo));
        Assert.Throws<ArgumentNullException>(() => new WaveformControl(valid, null!));
    }
}
