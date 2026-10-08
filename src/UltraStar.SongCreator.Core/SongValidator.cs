using System.Collections.Immutable;

namespace UltraStar.SongCreator.Core;

public enum ValidationSeverity { Warning, Error }

public enum ValidationCode
{
    MissingObject, InvalidCollection, EmptyIdentity, DuplicateIdentity,
    InvalidTiming, InvalidDuration, InvalidPhraseBounds, InvalidPitch,
    InvalidConfidence, InvalidTempo, InvalidType, BrokenReference,
    NoteOutsidePhrase, NoteOverlap, MissingText, MissingMetadata, MissingMedia,
    MissingProvenance, InvalidAnalysisPoint
}

public sealed record ValidationIssue(
    ValidationSeverity Severity, ValidationCode Code, string Path, Guid? EntityId, string Message);

public static class SongValidator
{
    public static ImmutableArray<ValidationIssue> Validate(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);
        var issues = ImmutableArray.CreateBuilder<ValidationIssue>();
        var identities = new HashSet<Guid>();
        var timedNotes = new List<(Note Note, string Path, int Order)>();

        void Add(ValidationCode code, string path, Guid? id, string message,
            ValidationSeverity severity = ValidationSeverity.Error) =>
            issues.Add(new(severity, code, path, id, message));

        void Identity(Guid id, string path)
        {
            if (id == Guid.Empty)
                Add(ValidationCode.EmptyIdentity, path, id, "Identity must not be empty.");
            else if (!identities.Add(id))
                Add(ValidationCode.DuplicateIdentity, path, id, "Identity must be unique within the song.");
        }

        ImmutableArray<T> Collection<T>(ImmutableArray<T> items, string path)
        {
            if (!items.IsDefault) return items;
            Add(ValidationCode.InvalidCollection, path, null, "Collection is uninitialized.");
            return [];
        }

        bool Time(double seconds, string path, Guid id, bool allowNegative = false)
        {
            if (double.IsFinite(seconds) && (allowNegative || seconds >= 0)) return true;
            Add(ValidationCode.InvalidTiming, path, id, "Time must be finite and within the documented range.");
            return false;
        }

        Identity(song.Id, "Song.Id");
        Time(song.AudioOffsetSeconds, "Song.AudioOffsetSeconds", song.Id, true);
        Time(song.VideoOffsetSeconds, "Song.VideoOffsetSeconds", song.Id, true);
        if (song.BeatsPerMinute is double bpm && (!double.IsFinite(bpm) || bpm <= 0))
            Add(ValidationCode.InvalidTempo, "Song.BeatsPerMinute", song.Id, "BPM must be finite and positive when provided.");

        if (song.Metadata is null)
            Add(ValidationCode.MissingObject, "Song.Metadata", song.Id, "Metadata object is required.");
        else
        {
            if (string.IsNullOrWhiteSpace(song.Metadata.Title))
                Add(ValidationCode.MissingMetadata, "Song.Metadata.Title", song.Id, "Title is missing.", ValidationSeverity.Warning);
            if (string.IsNullOrWhiteSpace(song.Metadata.Artist))
                Add(ValidationCode.MissingMetadata, "Song.Metadata.Artist", song.Id, "Artist is missing.", ValidationSeverity.Warning);
        }

        var media = Collection(song.Media, "Song.Media");
        var mediaIds = media.Where(m => m is not null).GroupBy(m => m.Id).ToDictionary(g => g.Key, g => g.Count());
        if (!media.Any(m => m is not null && m.Kind == MediaKind.Audio))
            Add(ValidationCode.MissingMedia, "Song.Media", song.Id, "No audio reference in this draft.", ValidationSeverity.Warning);
        for (var i = 0; i < media.Length; i++)
        {
            var item = media[i];
            var path = $"Song.Media[{i}]";
            if (item is null) { Add(ValidationCode.MissingObject, path, null, "Media reference is null."); continue; }
            Identity(item.Id, path + ".Id");
            if (!Enum.IsDefined(item.Kind))
                Add(ValidationCode.InvalidType, path + ".Kind", item.Id, "Unknown media kind.");
            if (string.IsNullOrWhiteSpace(item.Location))
                Add(ValidationCode.MissingMedia, path + ".Location", item.Id, "Media location is missing.", ValidationSeverity.Warning);
        }

        if (song.Analysis is null)
            Add(ValidationCode.MissingObject, "Song.Analysis", song.Id, "Analysis container is required.");
        var artifacts = song.Analysis is null ? [] : Collection(song.Analysis.Artifacts, "Song.Analysis.Artifacts");
        var artifactIds = artifacts.Where(a => a is not null).GroupBy(a => a.Id).ToDictionary(g => g.Key, g => g.Count());
        for (var i = 0; i < artifacts.Length; i++)
        {
            var artifact = artifacts[i];
            var path = $"Song.Analysis.Artifacts[{i}]";
            if (artifact is null) { Add(ValidationCode.MissingObject, path, null, "Analysis artifact is null."); continue; }
            Identity(artifact.Id, path + ".Id");
            if (!Enum.IsDefined(artifact.Kind))
                Add(ValidationCode.InvalidType, path + ".Kind", artifact.Id, "Unknown analysis kind.");
            if (string.IsNullOrWhiteSpace(artifact.Producer))
                Add(ValidationCode.MissingProvenance, path + ".Producer", artifact.Id, "Analysis producer is missing.", ValidationSeverity.Warning);
            if (artifact.SourceMediaId is Guid id && (id == Guid.Empty || !mediaIds.TryGetValue(id, out var count) || count != 1))
                Add(ValidationCode.BrokenReference, path + ".SourceMediaId", artifact.Id, "Media reference must resolve uniquely.");
            var points = Collection(artifact.Points, path + ".Points");
            for (var j = 0; j < points.Length; j++)
                if (!double.IsFinite(points[j].TimeSeconds) || points[j].TimeSeconds < 0 || !double.IsFinite(points[j].Value))
                    Add(ValidationCode.InvalidAnalysisPoint, $"{path}.Points[{j}]", artifact.Id, "Analysis time/value must be finite; time must not be negative.");
        }

        var phrases = Collection(song.Phrases, "Song.Phrases");
        for (var i = 0; i < phrases.Length; i++)
        {
            var phrase = phrases[i];
            var path = $"Song.Phrases[{i}]";
            if (phrase is null) { Add(ValidationCode.MissingObject, path, null, "Phrase is null."); continue; }
            Identity(phrase.Id, path + ".Id");
            var validStart = Time(phrase.StartSeconds, path + ".StartSeconds", phrase.Id);
            var validEnd = Time(phrase.EndSeconds, path + ".EndSeconds", phrase.Id);
            var validBounds = validStart && validEnd && phrase.EndSeconds > phrase.StartSeconds;
            if (validStart && validEnd && !validBounds)
                Add(ValidationCode.InvalidPhraseBounds, path, phrase.Id, "Phrase end must be after its start.");
            var notes = Collection(phrase.Notes, path + ".Notes");
            for (var j = 0; j < notes.Length; j++)
            {
                var note = notes[j];
                var notePath = $"{path}.Notes[{j}]";
                if (note is null) { Add(ValidationCode.MissingObject, notePath, null, "Note is null."); continue; }
                Identity(note.Id, notePath + ".Id");
                var validNoteStart = Time(note.StartSeconds, notePath + ".StartSeconds", note.Id);
                var validDuration = double.IsFinite(note.DurationSeconds) && note.DurationSeconds > 0;
                if (!validDuration)
                    Add(ValidationCode.InvalidDuration, notePath + ".DurationSeconds", note.Id, "Duration must be finite and positive.");
                var validNoteEnd = false;
                if (validNoteStart && validDuration)
                {
                    validNoteEnd = Time(note.EndSeconds, notePath + ".EndSeconds", note.Id) && note.EndSeconds > note.StartSeconds;
                    if (double.IsFinite(note.EndSeconds) && note.EndSeconds <= note.StartSeconds)
                        Add(ValidationCode.InvalidDuration, notePath + ".DurationSeconds", note.Id, "Duration is lost at this start time's numeric precision.");
                }
                if (validNoteStart && validNoteEnd)
                {
                    timedNotes.Add((note, notePath, timedNotes.Count));
                    if (validBounds && (note.StartSeconds < phrase.StartSeconds || note.EndSeconds > phrase.EndSeconds))
                        Add(ValidationCode.NoteOutsidePhrase, notePath, note.Id, "Note must lie within its phrase.");
                }
                if (note.MidiPitch is < 0 or > 127)
                    Add(ValidationCode.InvalidPitch, notePath + ".MidiPitch", note.Id, "MIDI pitch must be between 0 and 127.");
                if (!Enum.IsDefined(note.Type))
                    Add(ValidationCode.InvalidType, notePath + ".Type", note.Id, "Unknown note type.");
                if (note.Confidence is double confidence && (!double.IsFinite(confidence) || confidence is < 0 or > 1))
                    Add(ValidationCode.InvalidConfidence, notePath + ".Confidence", note.Id, "Confidence must be finite and between 0 and 1 when provided.");
                if (note.Type != NoteType.Freestyle && string.IsNullOrWhiteSpace(note.Text))
                    Add(ValidationCode.MissingText, notePath + ".Text", note.Id, "Pitched/rap note has no text in this draft.", ValidationSeverity.Warning);
                var refs = Collection(note.AnalysisReferences, notePath + ".AnalysisReferences");
                for (var k = 0; k < refs.Length; k++)
                    if (refs[k] == Guid.Empty || !artifactIds.TryGetValue(refs[k], out var count) || count != 1)
                        Add(ValidationCode.BrokenReference, $"{notePath}.AnalysisReferences[{k}]", note.Id, "Analysis reference must resolve uniquely.");
            }
        }

        // One warning per overlapping note, rather than a quadratic number of pair warnings.
        var maximumEnd = double.NegativeInfinity;
        foreach (var (note, path, _) in timedNotes.OrderBy(n => n.Note.StartSeconds).ThenBy(n => n.Order))
        {
            if (note.StartSeconds < maximumEnd)
                Add(ValidationCode.NoteOverlap, path, note.Id, "Note overlaps an earlier note; check voice/context.", ValidationSeverity.Warning);
            maximumEnd = Math.Max(maximumEnd, note.EndSeconds);
        }
        return issues.ToImmutable();
    }
}
