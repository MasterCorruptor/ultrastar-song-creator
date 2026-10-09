namespace UltraStar.SongCreator.Core.Tests;

public class EditingTests
{
    [Fact]
    public void MoveUndoRedoRestoreExactSnapshotsAndShareRawAnalysis()
    {
        var before = SongFixture.Create();
        var original = before.Phrases[0].Notes[0];
        var history = new SongHistory(before);
        Assert.True(history.Execute(new MoveNote(original.Id, 1.25)));
        var after = history.Current;
        Assert.Equal(original with { StartSeconds = 1.25 }, after.Phrases[0].Notes[0]);
        Assert.Equal(original.DurationSeconds, after.Phrases[0].Notes[0].DurationSeconds);
        Assert.Same(before.Phrases[1], after.Phrases[1]);
        Assert.Same(before.Phrases[0].Notes[1], after.Phrases[0].Notes[1]);
        Assert.Same(before.Metadata, after.Metadata);
        Assert.Equal(before.Media, after.Media);
        Assert.Same(before.Analysis, after.Analysis);
        Assert.Equal(before.Analysis.Artifacts[0].Points, after.Analysis.Artifacts[0].Points);
        Assert.Empty(SongValidator.Validate(after));
        Assert.Equal(1, original.StartSeconds);
        Assert.True(history.Undo());
        Assert.Same(before, history.Current);
        Assert.True(history.Redo());
        Assert.Same(after, history.Current);
    }

    [Fact]
    public void MultipleCommandsAreUndoneAndRedoneInOrder()
    {
        var song = SongFixture.Create();
        var note = song.Phrases[0].Notes[0];
        var history = new SongHistory(song);
        Assert.False(history.Undo());
        Assert.False(history.Redo());
        history.Execute(new MoveNote(note.Id, 1.1));
        var first = history.Current;
        history.Execute(new MoveNote(note.Id, 1.2));
        var second = history.Current;
        Assert.True(history.Undo());
        Assert.Same(first, history.Current);
        Assert.True(history.Undo());
        Assert.Same(song, history.Current);
        Assert.False(history.Undo());
        Assert.True(history.Redo());
        Assert.Same(first, history.Current);
        Assert.True(history.Redo());
        Assert.Same(second, history.Current);
        Assert.False(history.Redo());
    }

    [Fact]
    public void NewEditAfterUndoDiscardsRedoBranch()
    {
        var song = SongFixture.Create();
        var note = song.Phrases[0].Notes[0];
        var history = new SongHistory(song);
        history.Execute(new MoveNote(note.Id, 1.25));
        history.Undo();
        Assert.True(history.CanRedo);
        history.Execute(new MoveNote(note.Id, 1.4));
        Assert.False(history.CanRedo);
        Assert.False(history.Redo());
        Assert.Equal(1.4, history.Current.Phrases[0].Notes[0].StartSeconds);
        history.Undo();
        Assert.Same(song, history.Current);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0.1)]
    [InlineData(3.75)]
    [InlineData(double.MaxValue)]
    public void RejectedMoveLeavesCurrentAndUndoRedoHistoryUnchanged(double start)
    {
        var song = SongFixture.Create();
        var id = song.Phrases[0].Notes[0].Id;
        var history = new SongHistory(song);
        history.Execute(new MoveNote(id, 1.2));
        var recorded = history.Current;
        history.Undo();
        Assert.Throws<EditRejectedException>(() => history.Execute(new MoveNote(id, start)));
        Assert.Same(song, history.Current);
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
        history.Redo();
        Assert.Same(recorded, history.Current);
    }

    [Fact]
    public void NoOpDoesNotConsumeHistoryOrClearRedo()
    {
        var song = SongFixture.Create();
        var id = song.Phrases[0].Notes[0].Id;
        var history = new SongHistory(song);
        history.Execute(new MoveNote(id, 1.25));
        var changed = history.Current;
        history.Undo();
        Assert.False(history.Execute(new MoveNote(id, 1)));
        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
        history.Redo();
        Assert.Same(changed, history.Current);
    }

    [Fact]
    public void MissingEmptyAndDuplicateTargetsAreRejected()
    {
        var song = SongFixture.Create();
        Assert.Equal(EditRejection.TargetNotFound, Assert.Throws<EditRejectedException>(() => new MoveNote(Guid.NewGuid(), 1).Apply(song)).Reason);
        Assert.Equal(EditRejection.TargetNotFound, Assert.Throws<EditRejectedException>(() => new MoveNote(Guid.Empty, 1).Apply(song)).Reason);
        var first = song.Phrases[0].Notes[0];
        var duplicate = song with { Phrases = song.Phrases.SetItem(1, song.Phrases[1] with { Notes = [first] }) };
        Assert.Equal(EditRejection.AmbiguousTarget, Assert.Throws<EditRejectedException>(() => new MoveNote(first.Id, 1.25).Apply(duplicate)).Reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidExistingDurationCannotBeMoved(double duration)
    {
        var song = SongFixture.Create();
        var first = song.Phrases[0].Notes[0] with { DurationSeconds = duration };
        var changed = SongFixture.WithFirstNote(song, first);
        Assert.Equal(EditRejection.InvalidTiming, Assert.Throws<EditRejectedException>(() => new MoveNote(first.Id, 1).Apply(changed)).Reason);
    }

    [Fact]
    public void ExactPhraseEdgesAreAllowedButCrossPhraseMovesAreRejected()
    {
        var song = SongFixture.Create();
        var first = song.Phrases[0].Notes[0];
        Assert.Equal(0, new MoveNote(first.Id, 0).Apply(song).Phrases[0].Notes[0].StartSeconds);
        Assert.Equal(4, new MoveNote(first.Id, 3.5).Apply(song).Phrases[0].Notes[0].EndSeconds);
        Assert.Equal(EditRejection.OutsidePhrase, Assert.Throws<EditRejectedException>(() => new MoveNote(first.Id, 4).Apply(song)).Reason);
        Assert.Equal(0, song.Phrases[0].StartSeconds);
    }

    [Fact]
    public void MoveMayCreateOverlapWarningWithoutDeletingAnything()
    {
        var song = SongFixture.Create();
        var after = new MoveNote(song.Phrases[0].Notes[0].Id, 2.25).Apply(song);
        Assert.Equal(2, after.Phrases[0].Notes.Length);
        Assert.Contains(SongValidator.Validate(after), i => i.Code == ValidationCode.NoteOverlap && i.Severity == ValidationSeverity.Warning);
        Assert.DoesNotContain(SongValidator.Validate(after), i => i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void MoveCanRepairBadStartWithoutBlockingOnUnrelatedDraftErrors()
    {
        var song = SongFixture.Create() with { BeatsPerMinute = -1 };
        var first = song.Phrases[0].Notes[0] with { StartSeconds = double.NaN };
        var before = SongFixture.WithFirstNote(song, first);
        var after = new MoveNote(first.Id, 1).Apply(before);
        Assert.DoesNotContain(SongValidator.Validate(after), i => i.Code == ValidationCode.InvalidTiming);
        Assert.Contains(SongValidator.Validate(after), i => i.Code == ValidationCode.InvalidTempo);
    }

    [Fact]
    public void MalformedGraphAndPrecisionLossFailAtomically()
    {
        var song = SongFixture.Create();
        var id = song.Phrases[0].Notes[0].Id;
        Assert.Equal(EditRejection.InvalidStructure, Assert.Throws<EditRejectedException>(() => new MoveNote(id, 1).Apply(song with { Phrases = default })).Reason);
        Assert.Throws<EditRejectedException>(() => new MoveNote(id, 1).Apply(song with { Phrases = [null!] }));
        Assert.Equal(EditRejection.InvalidTiming, Assert.Throws<EditRejectedException>(() => new MoveNote(id, 1e20).Apply(song)).Reason);
    }

    [Fact]
    public void FailingCustomCommandsCannotChangeHistory()
    {
        var song = SongFixture.Create();
        var history = new SongHistory(song);
        Assert.Throws<InvalidOperationException>(() => history.Execute(new FailingCommand()));
        Assert.Throws<InvalidOperationException>(() => history.Execute(new NullCommand()));
        Assert.Same(song, history.Current);
        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    private sealed class FailingCommand : ISongCommand
    {
        public Song Apply(Song song) => throw new InvalidOperationException("test failure");
    }

    private sealed class NullCommand : ISongCommand
    {
        public Song Apply(Song song) => null!;
    }
}
