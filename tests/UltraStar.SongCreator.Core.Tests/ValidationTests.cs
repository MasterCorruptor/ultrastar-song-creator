using System.Collections.Immutable;

namespace UltraStar.SongCreator.Core.Tests;

public class ValidationTests
{
    [Fact]
    public void ValidDraftHasNoIssuesAndValidationDoesNotMutateIt()
    {
        var song = SongFixture.Create();
        var phrases = song.Phrases;
        var analysis = song.Analysis;
        Assert.Empty(SongValidator.Validate(song));
        Assert.True(song.Phrases == phrases);
        Assert.Same(analysis, song.Analysis);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0.1)]
    public void InvalidNoteStartIsAnError(double start)
    {
        var song = SongFixture.Create();
        var changed = SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { StartSeconds = start });
        AssertError(changed, ValidationCode.InvalidTiming);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidDurationIsAnError(double duration)
    {
        var song = SongFixture.Create();
        AssertError(SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { DurationSeconds = duration }), ValidationCode.InvalidDuration);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidConfidenceIsAnError(double confidence)
    {
        var song = SongFixture.Create();
        AssertError(SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { Confidence = confidence }), ValidationCode.InvalidConfidence);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void ConfidenceBoundaryAndUnknownAreValid(double? confidence)
    {
        var song = SongFixture.Create();
        Assert.Empty(SongValidator.Validate(SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { Confidence = confidence })));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidTempoIsAnError(double bpm) => AssertError(SongFixture.Create() with { BeatsPerMinute = bpm }, ValidationCode.InvalidTempo);

    [Theory]
    [InlineData(-1)]
    [InlineData(128)]
    public void InvalidMidiPitchIsAnError(int pitch)
    {
        var song = SongFixture.Create();
        AssertError(SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { MidiPitch = pitch }), ValidationCode.InvalidPitch);
    }

    [Fact]
    public void IdentityMustBeNonEmptyAndUniqueAcrossKinds()
    {
        var song = SongFixture.Create();
        AssertError(song with { Id = Guid.Empty }, ValidationCode.EmptyIdentity);
        AssertError(SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { Id = song.Media[0].Id }), ValidationCode.DuplicateIdentity);
    }

    [Fact]
    public void AnalysisReferencesMustResolveAndPreserveIssueLocations()
    {
        var song = SongFixture.Create();
        var bad = SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { AnalysisReferences = [Guid.NewGuid()] });
        var issue = Assert.Single(SongValidator.Validate(bad));
        Assert.Equal(ValidationCode.BrokenReference, issue.Code);
        Assert.Equal("Song.Phrases[0].Notes[0].AnalysisReferences[0]", issue.Path);
        Assert.Equal(song.Phrases[0].Notes[0].Id, issue.EntityId);
        Assert.Equal(SongValidator.Validate(bad).ToArray(), SongValidator.Validate(bad).ToArray());
        var artifact = song.Analysis.Artifacts[0];
        AssertError(song with { Analysis = new() { Artifacts = [artifact with { SourceMediaId = Guid.NewGuid() }] } }, ValidationCode.BrokenReference);
        AssertError(song with { Analysis = new() { Artifacts = [artifact, artifact] } }, ValidationCode.BrokenReference);
    }

    [Fact]
    public void BadBoundsAndNoteOutsidePhraseAreErrors()
    {
        var song = SongFixture.Create();
        AssertError(song with { Phrases = song.Phrases.SetItem(0, song.Phrases[0] with { EndSeconds = 0 }) }, ValidationCode.InvalidPhraseBounds);
        AssertError(SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { StartSeconds = 3.75 }), ValidationCode.NoteOutsidePhrase);
    }

    [Fact]
    public void MissingTextAndOverlapWarnWithoutDiscardingNotes()
    {
        var song = SongFixture.Create();
        var changed = SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { DurationSeconds = 2, Text = "" });
        var issues = SongValidator.Validate(changed);
        Assert.Contains(issues, i => i.Code == ValidationCode.MissingText && i.Severity == ValidationSeverity.Warning);
        Assert.Contains(issues, i => i.Code == ValidationCode.NoteOverlap && i.Severity == ValidationSeverity.Warning);
        Assert.DoesNotContain(issues, i => i.Severity == ValidationSeverity.Error);
        Assert.Equal(2, changed.Phrases[0].Notes.Length);
        Assert.Equal("", changed.Phrases[0].Notes[0].Text);
    }

    [Fact]
    public void TouchingNotesDoNotOverlapAndFreestyleMayHaveNoText()
    {
        var song = SongFixture.Create();
        var changed = SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { StartSeconds = 1.5, Type = NoteType.Freestyle, Text = "" });
        Assert.Empty(SongValidator.Validate(changed));
    }

    [Fact]
    public void OverlapAcrossPhrasesIsAWarningForDraftVoiceContext()
    {
        var song = SongFixture.Create();
        var phrase = song.Phrases[1] with { StartSeconds = 0, Notes = [song.Phrases[1].Notes[0] with { StartSeconds = 1.25 }] };
        Assert.Contains(SongValidator.Validate(song with { Phrases = song.Phrases.SetItem(1, phrase) }),
            i => i.Code == ValidationCode.NoteOverlap && i.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void FiniteInputsWithOverflowOrLostDurationAreRejected()
    {
        var song = SongFixture.Create();
        AssertError(SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { StartSeconds = double.MaxValue, DurationSeconds = double.MaxValue }), ValidationCode.InvalidTiming);
        AssertError(SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { StartSeconds = 1e20, DurationSeconds = 1 }), ValidationCode.InvalidDuration);
    }

    [Fact]
    public void UninitializedCollectionsAndNullEntriesReturnErrors()
    {
        var song = SongFixture.Create();
        AssertError(song with { Phrases = default }, ValidationCode.InvalidCollection);
        AssertError(song with { Phrases = [null!] }, ValidationCode.MissingObject);
        AssertError(song with { Metadata = null! }, ValidationCode.MissingObject);
        AssertError(song with { Analysis = null! }, ValidationCode.MissingObject);
    }

    [Fact]
    public void AnalysisDataTypesReferencesAndPointsAreValidated()
    {
        var song = SongFixture.Create();
        var artifact = song.Analysis.Artifacts[0] with { Kind = (AnalysisKind)999, Points = [new(double.NaN, 60)] };
        var changed = song with { Analysis = new() { Artifacts = [artifact] } };
        AssertError(changed, ValidationCode.InvalidType);
        AssertError(changed, ValidationCode.InvalidAnalysisPoint);
    }

    [Fact]
    public void MissingMetadataAndMediaAreDraftWarnings()
    {
        var song = SongFixture.Create() with { Metadata = new(), Media = [], Analysis = new(), Phrases = [] };
        var issues = SongValidator.Validate(song);
        Assert.Equal(3, issues.Length);
        Assert.All(issues, i => Assert.Equal(ValidationSeverity.Warning, i.Severity));
    }

    private static void AssertError(Song song, ValidationCode code) =>
        Assert.Contains(SongValidator.Validate(song), i => i.Code == code && i.Severity == ValidationSeverity.Error);
}
