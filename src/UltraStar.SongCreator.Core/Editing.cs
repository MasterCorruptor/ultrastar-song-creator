namespace UltraStar.SongCreator.Core;

/// <summary>A pure operation on an immutable song snapshot.</summary>
public interface ISongCommand
{
    Song Apply(Song song);
}

public enum EditRejection { InvalidStructure, TargetNotFound, AmbiguousTarget, InvalidTiming, OutsidePhrase }

public sealed class EditRejectedException(EditRejection reason, string message) : InvalidOperationException(message)
{
    public EditRejection Reason { get; } = reason;
}

public sealed record MoveNote(Guid NoteId, double NewStartSeconds) : ISongCommand
{
    public Song Apply(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);
        if (NoteId == Guid.Empty)
            throw new EditRejectedException(EditRejection.TargetNotFound, "Note identity must not be empty.");
        if (song.Phrases.IsDefault)
            throw new EditRejectedException(EditRejection.InvalidStructure, "Phrase collection is uninitialized.");

        (int Phrase, int Note)? target = null;
        for (var i = 0; i < song.Phrases.Length; i++)
        {
            var phrase = song.Phrases[i];
            if (phrase is null || phrase.Notes.IsDefault)
                throw new EditRejectedException(EditRejection.InvalidStructure, "Phrase/notes structure is invalid.");
            for (var j = 0; j < phrase.Notes.Length; j++)
            {
                var note = phrase.Notes[j];
                if (note is null)
                    throw new EditRejectedException(EditRejection.InvalidStructure, "Note is null.");
                if (note.Id != NoteId) continue;
                if (target.HasValue)
                    throw new EditRejectedException(EditRejection.AmbiguousTarget, "Note identity is ambiguous.");
                target = (i, j);
            }
        }
        if (target is not { } location)
            throw new EditRejectedException(EditRejection.TargetNotFound, "Note was not found.");

        var parent = song.Phrases[location.Phrase];
        var original = parent.Notes[location.Note];
        var newEnd = NewStartSeconds + original.DurationSeconds;
        if (!double.IsFinite(NewStartSeconds) || NewStartSeconds < 0 ||
            !double.IsFinite(original.DurationSeconds) || original.DurationSeconds <= 0 ||
            !double.IsFinite(newEnd) || newEnd <= NewStartSeconds)
            throw new EditRejectedException(EditRejection.InvalidTiming, "Start/duration must produce a finite positive interval.");
        if (!double.IsFinite(parent.StartSeconds) || !double.IsFinite(parent.EndSeconds) ||
            parent.StartSeconds < 0 || parent.EndSeconds <= parent.StartSeconds)
            throw new EditRejectedException(EditRejection.InvalidStructure, "Phrase boundaries are invalid.");
        if (NewStartSeconds < parent.StartSeconds || newEnd > parent.EndSeconds)
            throw new EditRejectedException(EditRejection.OutsidePhrase, "Move would place the note outside its phrase.");
        if (NewStartSeconds == original.StartSeconds) return song;

        var moved = original with { StartSeconds = NewStartSeconds };
        var changedPhrase = parent with { Notes = parent.Notes.SetItem(location.Note, moved) };
        return song with { Phrases = song.Phrases.SetItem(location.Phrase, changedPhrase) };
    }
}

/// <summary>Single-writer history; redo restores the recorded snapshot without rerunning a command.</summary>
public sealed class SongHistory(Song initial)
{
    private sealed record Entry(Song Before, Song After);
    private readonly Stack<Entry> undo = new();
    private readonly Stack<Entry> redo = new();
    public Song Current { get; private set; } = initial ?? throw new ArgumentNullException(nameof(initial));
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;

    public bool Execute(ISongCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var before = Current;
        // No state or history changes until the command succeeds.
        var after = command.Apply(before) ?? throw new InvalidOperationException("Command returned a null song.");
        if (ReferenceEquals(before, after)) return false;
        undo.Push(new(before, after));
        redo.Clear();
        Current = after;
        return true;
    }

    public bool Undo()
    {
        if (!undo.TryPop(out var entry)) return false;
        redo.Push(entry);
        Current = entry.Before;
        return true;
    }

    public bool Redo()
    {
        if (!redo.TryPop(out var entry)) return false;
        undo.Push(entry);
        Current = entry.After;
        return true;
    }
}
