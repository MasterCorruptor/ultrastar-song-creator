using System.Collections.Immutable;

namespace UltraStar.SongCreator.Core.Tests;

public class SelectedLyricsEditingTests
{
    internal static readonly DateTimeOffset At = new(2026, 10, 13, 12, 34, 56, TimeSpan.Zero);
    internal static SelectedLyrics Plain() => new(SelectedLyricsKind.Text,
        new("chosen", "  Hei 🎵\n  Hei 🎵\n", SelectedLyricsSyncLevel.Plain, []),
        new(SelectedLyricsOrigin.Manual, "manual", "11111111-1111-1111-1111-111111111111", null, At, null));
    internal static SelectedLyrics Word() => Plain() with
    {
        Document = new("chosen-word", " Hei 🎵\n Hei 🎵\n", SelectedLyricsSyncLevel.Word,
        [new(" Hei 🎵", 1, 3, [new(" Hei ", 1, 2), new("🎵", 1, null)]),
            new(" Hei 🎵", 1, null, [new(" Hei 🎵", 1.5, 4)])]),
        Provenance = new(SelectedLyricsOrigin.Provider, "fixture", "chosen-candidate",
            "https://example.invalid/lyrics", null, At.AddTicks(1234567), "Source claim", null, "Æ", "Live", 0.25)
    };

    [Fact]
    public void SelectionReplacementClearAndUndoRedoRestoreExactSnapshots()
    {
        var original = SongFixture.Create() with
        {
            ImportedSource = new() { FormatId = "local-media-v1", Headers = [new("AU", "instrumental")] }
        };
        var history = new SongHistory(original);
        var first = Plain();
        Assert.True(history.Execute(new SetSelectedLyrics(first)));
        var selected = history.Current;
        Assert.Same(first, selected.SelectedLyrics);
        Assert.Same(original.Metadata, selected.Metadata);
        Assert.Same(original.ImportedSource, selected.ImportedSource);
        Assert.Same(original.Analysis, selected.Analysis);
        Assert.True(original.Media == selected.Media);
        Assert.True(original.Phrases == selected.Phrases);
        Assert.Equal(original.AudioOffsetSeconds, selected.AudioOffsetSeconds);
        Assert.Equal(original.VideoOffsetSeconds, selected.VideoOffsetSeconds);
        Assert.Equal(original.BeatsPerMinute, selected.BeatsPerMinute);
        Assert.Equal(original.Id, selected.Id);
        var second = Word();
        Assert.True(history.Execute(new SetSelectedLyrics(second)));
        var replaced = history.Current;
        Assert.True(history.Execute(new SetSelectedLyrics(null)));
        var cleared = history.Current;
        Assert.Null(cleared.SelectedLyrics);
        Assert.True(history.Undo()); Assert.Same(replaced, history.Current);
        Assert.True(history.Undo()); Assert.Same(selected, history.Current);
        Assert.True(history.Undo()); Assert.Same(original, history.Current);
        Assert.True(history.Redo()); Assert.Same(selected, history.Current);
        Assert.True(history.Redo()); Assert.Same(replaced, history.Current);
        Assert.Same(second, history.Current.SelectedLyrics);
        Assert.True(history.Redo()); Assert.Same(cleared, history.Current);
    }

    [Fact]
    public void SeparatelyAllocatedEqualArraysAndNullNoOpsPreserveRedo()
    {
        var value = Word();
        var original = SongFixture.Create() with { SelectedLyrics = value };
        var history = new SongHistory(original);
        history.Execute(new SetSelectedLyrics(null));
        var cleared = history.Current;
        history.Undo();
        var equal = value with
        {
            Provenance = value.Provenance with { },
            Document = value.Document! with
            {
                Lines = value.Document.Lines.Select(line => line with
                { Words = line.Words.Select(word => word with { }).ToImmutableArray() }).ToImmutableArray()
            }
        };
        Assert.False(history.Execute(new SetSelectedLyrics(equal)));
        Assert.Same(original, history.Current);
        Assert.False(history.CanUndo); Assert.True(history.CanRedo);
        Assert.True(history.Redo()); Assert.Same(cleared, history.Current);
        Assert.False(history.Execute(new SetSelectedLyrics(null)));
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void NullToNullAfterUndoPreservesTheRecordedSelectionForRedo()
    {
        var original = SongFixture.Create();
        var history = new SongHistory(original);
        history.Execute(new SetSelectedLyrics(Word())); var selected = history.Current;
        history.Undo();
        Assert.False(history.Execute(new SetSelectedLyrics(null)));
        Assert.Same(original, history.Current); Assert.True(history.CanRedo);
        Assert.True(history.Redo()); Assert.Same(selected, history.Current);
    }

    [Theory]
    [InlineData("date")]
    [InlineData("candidate")]
    [InlineData("source")]
    public void ProvenanceChangesAreEditsAndDiscardOldRedo(string field)
    {
        var value = Word();
        var original = SongFixture.Create() with { SelectedLyrics = value };
        var history = new SongHistory(original);
        history.Execute(new SetSelectedLyrics(null)); history.Undo();
        var source = field switch
        {
            "date" => value.Provenance with { RetrievedAtUtc = At.AddTicks(1234568) },
            "candidate" => value.Provenance with { CandidateId = "another" },
            _ => value.Provenance with { SourceId = "another" }
        };
        Assert.True(history.Execute(new SetSelectedLyrics(value with { Provenance = source })));
        Assert.False(history.CanRedo);
        history.Undo(); Assert.Same(original, history.Current);
    }

    [Fact]
    public void RejectionLeavesCurrentUndoAndRedoIntactAndValidatorReportsTypedPath()
    {
        var original = SongFixture.Create() with { SelectedLyrics = Plain() };
        var history = new SongHistory(original);
        history.Execute(new SetSelectedLyrics(null)); var cleared = history.Current; history.Undo();
        var invalid = Plain() with { Document = null };
        Assert.Equal(EditRejection.InvalidStructure,
            Assert.Throws<EditRejectedException>(() => history.Execute(new SetSelectedLyrics(invalid))).Reason);
        Assert.Same(original, history.Current); Assert.False(history.CanUndo); Assert.True(history.CanRedo);
        Assert.True(history.Redo()); Assert.Same(cleared, history.Current);
        var issue = Assert.Single(SongValidator.Validate(original with { SelectedLyrics = invalid }),
            i => i.Code == ValidationCode.InvalidSelectedLyrics);
        Assert.Equal("selectedLyrics", issue.Path);
        Assert.Equal(ValidationSeverity.Error, issue.Severity);
        Assert.Contains("InvalidSelection", issue.Message);
    }

    [Fact]
    public void EmptyTextAndSourceTagsNeverInferInstrumental()
    {
        var original = SongFixture.Create() with { ImportedSource = new() { FormatId = "test", Headers = [new("AU", "instrumental")] } };
        Assert.Same(original, new SetSelectedLyrics(null).Apply(original));
        Assert.Throws<EditRejectedException>(() => new SetSelectedLyrics(Plain() with
        { Document = Plain().Document! with { Text = "" } }).Apply(original));
        var explicitClaim = Plain() with { Kind = SelectedLyricsKind.Instrumental, Document = null };
        Assert.Same(explicitClaim, new SetSelectedLyrics(explicitClaim).Apply(original).SelectedLyrics);
    }
}
