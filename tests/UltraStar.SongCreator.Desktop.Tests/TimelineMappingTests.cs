using UltraStar.SongCreator.Audio;
using UltraStar.SongCreator.Desktop.Timeline;

namespace UltraStar.SongCreator.Desktop.Tests;

public sealed class TimelineMappingTests
{
    [Fact]
    public void DefaultProjectionAgreesWithExistingDemoAndRoundTripsEndpoints()
    {
        var m = TimelineMapping.Demo;
        Assert.Equal(EditorWindow.TimeOrigin, m.OriginX); Assert.Equal(EditorWindow.SecondsScale, m.PixelsPerSecond);
        foreach (var time in new[] { 0, 1, 2.5, 8 })
        {
            Assert.True(m.TrySongToX(time, out var x)); Assert.Equal(75 + time * 110, x);
            Assert.True(m.TryXToSong(x, out var back)); Assert.Equal(time, back);
        }
        Assert.Equal(955, m.RightX);
    }
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-1)]
    [InlineData(9)]
    public void InvalidSongAndPixelRequestsAreRejected(double value)
    {
        var m = TimelineMapping.Demo; Assert.False(m.TrySongToX(value, out _)); Assert.False(m.TrySongToSourceFrame(value, 0, 48000, 384000, out _));
        Assert.False(m.TryXToSong(double.IsFinite(value) ? value * 110 + 75 : value, out _));
    }
    [Theory]
    [InlineData(1, 0, 48000)]
    [InlineData(-1, 1, 0)]
    [InlineData(0, 2.5, 120000)]
    public void SignedOffsetUsesOneSourceToSongToFrameMapping(double offset, double song, long sourceFrame)
    {
        var m = TimelineMapping.Demo; var s = new PlaybackSnapshot(PlaybackState.Paused, sourceFrame, 48000, 384000);
        Assert.Equal(song, m.SnapshotToSongSeconds(s, offset)); Assert.True(m.TrySongToSourceFrame(song, offset, 48000, 384000, out var frame)); Assert.Equal(sourceFrame, frame);
    }
    [Fact]
    public void SeekHonorsClipBoundsAndEndpointAndRoundAwayFromZero()
    {
        var m = TimelineMapping.Demo;
        Assert.True(m.TrySongToSourceFrame(8, 0, 48000, 384000, out var end)); Assert.Equal(384000, end);
        Assert.True(m.TrySongToSourceFrame(.5 / 8000, 0, 8000, 8, out var rounded)); Assert.Equal(1, rounded);
        Assert.False(m.TrySongToSourceFrame(0, -1, 48000, 384000, out _)); Assert.False(m.TrySongToSourceFrame(8, 1, 48000, 384000, out _));
        Assert.True(m.TrySongToSourceFrame(0, 0, 8000, 0, out var empty)); Assert.Equal(0, empty);
        Assert.False(m.TrySongToSourceFrame(1, 0, 8000, 0, out _));
        foreach (var offset in new[] { double.NaN, double.PositiveInfinity, double.MaxValue }) Assert.False(m.TrySongToSourceFrame(1, offset, 48000, 384000, out _));
    }
    [Fact]
    public void ValidOutOfViewportClockIsPreservedButPlayheadIsHiddenAndFaultHasNoPosition()
    {
        var m = TimelineMapping.Demo; var s = new PlaybackSnapshot(PlaybackState.Playing, 0, 48000, 384000);
        Assert.Equal(-1, m.SnapshotToSongSeconds(s, 1)); Assert.False(m.TrySongToX(m.SnapshotToSongSeconds(s, 1)!.Value, out _));
        foreach (var state in new[] { PlaybackState.Stopped, PlaybackState.Playing, PlaybackState.Paused, PlaybackState.Ended }) Assert.Equal(8, m.SnapshotToSongSeconds(s with { State = state, PositionFrames = 384000 }, 0));
        foreach (var bad in new[] { s with { State = PlaybackState.Faulted }, s with { State = PlaybackState.Disposed }, s with { State = (PlaybackState)99 }, s with { SampleRate = 0 }, s with { FrameCount = -1 }, s with { PositionFrames = -1 }, s with { PositionFrames = 384001 }, s with { FrameCount = long.MaxValue } }) Assert.Null(m.SnapshotToSongSeconds(bad, 0));
        Assert.Null(m.SnapshotToSongSeconds(s, double.NaN));
    }
    [Fact]
    public void WaveformBucketsUseSameSignedOffsetAndClipAtViewportEdges()
    {
        var m = TimelineMapping.Demo;
        Assert.True(m.TrySourceIntervalToX(0, 96000, 48000, 384000, 1, out var left)); Assert.Equal(new TimelineSpan(75, 185), left);
        Assert.True(m.TrySourceIntervalToX(288000, 384000, 48000, 384000, -1, out var right)); Assert.Equal(new TimelineSpan(845, 955), right);
        Assert.False(m.TrySourceIntervalToX(0, 48000, 48000, 384000, 1, out _));
        Assert.True(m.TrySourceIntervalToX(7, 8, 8000, 8, 0, out var final)); Assert.True(final.Width > 0);
        Assert.False(m.TrySourceIntervalToX(-1, 1, 48000, 384000, 0, out _)); Assert.False(m.TrySourceIntervalToX(0, 0, 48000, 0, 0, out _));
        Assert.False(m.TrySourceIntervalToX(0, 384001, 48000, 384000, 0, out _)); Assert.False(m.TrySourceIntervalToX(0, 10, 0, 384000, 0, out _)); Assert.False(m.TrySourceIntervalToX(0, 10, 48000, 384000, double.NaN, out _));
    }
    [Fact]
    public void ViewportRejectsNonFiniteOrUnrepresentableBoundsAndSupportsTranslatedView()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineMapping(double.NaN)); Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineMapping(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineMapping(1, 1)); Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineMapping(0, 8, -1)); Assert.Throws<ArgumentOutOfRangeException>(() => new TimelineMapping(0, 8, 75, 0));
        Assert.Throws<ArgumentException>(() => new TimelineMapping(0, double.MaxValue, 75, double.MaxValue));
        var m = new TimelineMapping(2, 4, 10, 5); Assert.True(m.TrySongToX(3, out var x)); Assert.Equal(15, x); Assert.True(m.TryXToSong(x, out var song)); Assert.Equal(3, song);
    }
}
