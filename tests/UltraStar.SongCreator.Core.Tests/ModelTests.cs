namespace UltraStar.SongCreator.Core.Tests;

public class ModelTests
{
    [Fact]
    public void SongRepresentsPhrasesTextMediaAndProvenance()
    {
        var song = SongFixture.Create();
        Assert.Equal(2, song.Phrases.Length);
        Assert.Equal("Hei du", song.Phrases[0].Text);
        Assert.Equal(-0.2, song.AudioOffsetSeconds);
        Assert.Equal(120, song.BeatsPerMinute);
        var note = song.Phrases[0].Notes[0];
        var source = Assert.Single(song.Analysis.Artifacts);
        Assert.Equal(source.Id, Assert.Single(note.AnalysisReferences));
        Assert.Equal(song.Media[0].Id, source.SourceMediaId);
        Assert.Equal("fixture-v1", source.ModelRevision);
        Assert.Equal(60.25, source.Points[0].Value);
        Assert.Null(song.Phrases[0].Notes[1].Confidence);
    }

    [Fact]
    public void NoteSnapshotSharesImmutableAnalysisAndPreservesOriginal()
    {
        var before = SongFixture.Create();
        var original = before.Phrases[0].Notes[0];
        var after = SongFixture.WithFirstNote(before, original with { StartSeconds = 1.5 });
        Assert.Same(before.Analysis, after.Analysis);
        Assert.True(before.Analysis.Artifacts[0].Points == after.Analysis.Artifacts[0].Points);
        Assert.Same(before.Phrases[1], after.Phrases[1]);
        Assert.Equal(1, original.StartSeconds);
        Assert.Equal(original.DurationSeconds, after.Phrases[0].Notes[0].DurationSeconds);
        Assert.Equal(original.AnalysisReferences, after.Phrases[0].Notes[0].AnalysisReferences);
        Assert.Equal(original.Confidence, after.Phrases[0].Notes[0].Confidence);
    }

    [Fact]
    public void ImmutableAnalysisPointsCannotBeChangedThroughNewSnapshot()
    {
        var song = SongFixture.Create();
        var points = song.Analysis.Artifacts[0].Points;
        var changed = points.SetItem(0, new AnalysisPoint(1, 70));
        Assert.Equal(60.25, points[0].Value);
        Assert.Equal(70, changed[0].Value);
        Assert.Equal(60.25, song.Analysis.Artifacts[0].Points[0].Value);
    }
}
