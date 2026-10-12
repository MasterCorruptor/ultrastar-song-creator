using UltraStar.SongCreator.Audio;

namespace UltraStar.SongCreator.Audio.Tests;

public sealed class PcmTests
{
    [Fact]
    public void RenderingPauseResumeSeekStopAndEosUseExactSameFrames()
    {
        var samples = new float[] { .1f, .2f, .3f, .4f, .5f, .6f };
        var clip = new PcmClip(8000, 2, samples);
        samples[0] = -1;
        using var p = new PcmPlayback(clip);
        var output = new float[4];
        p.Render(output); Assert.All(output, v => Assert.Equal(0, v));
        p.Play(); p.Render(output); Assert.Equal(new float[] { .1f, .2f, .3f, .4f }, output); Assert.Equal(2, p.Snapshot.PositionFrames);
        p.Pause(); var paused = p.Snapshot; p.Render(output); Assert.Equal(paused, p.Snapshot); Assert.All(output, v => Assert.Equal(0, v));
        p.Play(); p.Render(output); Assert.Equal(new float[] { .5f, .6f, 0, 0 }, output); Assert.Equal(PlaybackState.Ended, p.Snapshot.State); Assert.Equal(3, p.Snapshot.PositionFrames);
        p.Play(); p.Render(output); Assert.Equal(PlaybackState.Ended, p.Snapshot.State); Assert.All(output, v => Assert.Equal(0, v));
        p.SeekFrame(1); Assert.Equal(PlaybackState.Paused, p.Snapshot.State); p.Play(); p.Render(output); Assert.Equal(new float[] { .3f, .4f, .5f, .6f }, output);
        p.Stop(); Assert.Equal(0, p.Snapshot.PositionFrames); Assert.Equal(PlaybackState.Stopped, p.Snapshot.State);
        p.Play(); p.Render(output); Assert.Equal(.1f, output[0]); Assert.Same(clip, p.Clip);
    }
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-.1)]
    [InlineData(9)]
    public void InvalidSecondsDoNotChangeClockOrState(double seconds)
    {
        using var p = new PcmPlayback(DemoPcm.Create()); p.Play(); p.Render(new float[20]); var before = p.Snapshot;
        Assert.Throws<ArgumentOutOfRangeException>(() => p.SeekSeconds(seconds)); Assert.Equal(before, p.Snapshot);
    }
    [Fact]
    public void SeekEndpointsRoundingAndPausedSeekAreExplicit()
    {
        using var p = new PcmPlayback(new PcmClip(8000, 1, new float[8]));
        p.SeekSeconds(.5 / 8000); Assert.Equal(1, p.Snapshot.PositionFrames); Assert.Equal(PlaybackState.Stopped, p.Snapshot.State);
        p.Play(); p.Pause(); p.SeekFrame(4); Assert.Equal(PlaybackState.Paused, p.Snapshot.State);
        p.SeekSeconds(.001); Assert.Equal(8, p.Snapshot.PositionFrames); Assert.Equal(PlaybackState.Ended, p.Snapshot.State);
        var before = p.Snapshot; Assert.Throws<ArgumentOutOfRangeException>(() => p.SeekFrame(-1)); Assert.Throws<ArgumentOutOfRangeException>(() => p.SeekFrame(9)); Assert.Equal(before, p.Snapshot);
        p.SeekFrame(0); Assert.Equal(PlaybackState.Paused, p.Snapshot.State);
    }
    [Fact]
    public void EmptyClipAndLateRenderAreSilentAndDisposalIsIdempotent()
    {
        var p = new PcmPlayback(new PcmClip(8000, 1, [])); p.Play(); Assert.Equal(PlaybackState.Ended, p.Snapshot.State);
        var output = new float[] { 1, 1 }; p.Render(output); Assert.All(output, v => Assert.Equal(0, v));
        p.Dispose(); p.Dispose(); p.Render(output); Assert.Equal(PlaybackState.Disposed, p.Snapshot.State);
        Assert.Throws<ObjectDisposedException>(p.Play); Assert.Throws<ObjectDisposedException>(p.Pause); Assert.Throws<ObjectDisposedException>(p.Stop); Assert.Throws<ObjectDisposedException>(() => p.SeekFrame(0)); Assert.Throws<ObjectDisposedException>(() => p.SeekSeconds(0));
    }
    [Fact]
    public void PartialFrameAndInvalidClipAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PcmClip(0, 1, [])); Assert.Throws<ArgumentOutOfRangeException>(() => new PcmClip(8000, 3, []));
        Assert.Throws<ArgumentException>(() => new PcmClip(8000, 2, new float[3]));
        foreach (var value in new[] { float.NaN, float.PositiveInfinity, 1.1f, -1.1f }) Assert.Throws<ArgumentException>(() => new PcmClip(8000, 1, [value]));
        Assert.Throws<ArgumentException>(() => new PcmClip(8000, 1, new float[PcmClip.MaxSamples + 1]));
        using var p = new PcmPlayback(new PcmClip(8000, 2, [.1f, .2f])); p.Play(); var before = p.Snapshot;
        Assert.Throws<ArgumentException>(() => p.Render(new float[3])); Assert.Equal(before, p.Snapshot);
    }
    [Fact]
    public void WaveformIncludesChannelsAndFinalBucketWithoutFalseZeroBaseline()
    {
        var clip = new PcmClip(8000, 2, [.1f, .6f, .3f, .4f, -.2f, -.5f]); var e = WaveformEnvelope.Create(clip, 2);
        Assert.Equal(8000, e.SampleRate); Assert.Equal(3, e.FrameCount);
        Assert.Equal(new WaveformBucket(0, 2, .1f, .6f), e.Buckets[0]); Assert.Equal(new WaveformBucket(2, 3, -.5f, -.2f), e.Buckets[1]);
        Assert.Empty(WaveformEnvelope.Create(new PcmClip(8000, 1, []), 3).Buckets);
        Assert.Throws<ArgumentOutOfRangeException>(() => WaveformEnvelope.Create(clip, 0));
        Assert.Throws<ArgumentException>(() => WaveformEnvelope.Create(new PcmClip(8000, 1, new float[100001]), 1));
    }
    [Fact]
    public void DemoIsBoundedAndEnvelopeMatchesEverySample()
    {
        var clip = DemoPcm.Create(); var e = WaveformEnvelope.Create(clip, 4800); Assert.Equal(8, clip.DurationSeconds); Assert.Equal(80, e.Buckets.Length);
        foreach (var b in e.Buckets)
            for (var f = b.StartFrame; f < b.EndFrame; f++) for (var c = 0; c < clip.Channels; c++) { var s = clip.GetSample(f, c); Assert.InRange(s, b.Minimum, b.Maximum); Assert.InRange(Math.Abs(s), 0, .020001f); }
        Assert.Equal(0, clip.GetSample(0, 0)); Assert.Equal(0, clip.GetSample(clip.FrameCount - 1, 1));
    }
    [Fact]
    public async Task RenderCommandsAndDisposeCanRaceWithoutInvalidCursorOrLeakingSamples()
    {
        using var p = new PcmPlayback(new PcmClip(8000, 2, Enumerable.Repeat(.1f, 16000).ToArray()));
        var render = Task.Run(() => { var b = new float[128]; for (var i = 0; i < 4000; i++) { p.Render(b); var s = p.Snapshot; Assert.InRange(s.PositionFrames, 0, s.FrameCount); } });
        var commands = Task.Run(() => { for (var i = 0; i < 1000; i++) { p.SeekFrame(i % 7000); p.Play(); p.Pause(); p.Stop(); } p.Dispose(); });
        await Task.WhenAll(render, commands); var output = new float[] { 1, 1 }; p.Render(output); Assert.All(output, v => Assert.Equal(0, v)); Assert.Equal(PlaybackState.Disposed, p.Snapshot.State);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 127)]
    [InlineData(2, 13)]
    [InlineData(2, 512)]
    public void RenderChunksMatchReferenceSamplesAndTailExactly(int channels, int chunkFrames)
    {
        var input = Enumerable.Range(0, 1503 * channels).Select(i => (i % 101 - 50) / 50f).ToArray();
        using var p = new PcmPlayback(new PcmClip(48000, channels, input)); p.SeekFrame(11); p.Play();
        var output = new float[chunkFrames * channels]; long position = 11;
        while (position < 1503)
        {
            p.Render(output); var consumed = Math.Min(chunkFrames, 1503 - position);
            for (var f = 0; f < chunkFrames; f++) for (var c = 0; c < channels; c++)
                Assert.Equal(f < consumed ? input[(position + f) * channels + c] : 0, output[f * channels + c]);
            position += consumed; Assert.Equal(position, p.Snapshot.PositionFrames);
        }
        Assert.Equal(PlaybackState.Ended, p.Snapshot.State);
    }
    [Fact]
    public void NativeLibraryIsLoadableAtThePinnedAbiVersion()
    {
        Assert.Equal("0.11.25", MiniAudioPlayback.NativeVersion);
    }
}
