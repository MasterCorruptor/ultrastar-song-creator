using System.Collections.Immutable;

namespace UltraStar.SongCreator.Core;

public enum SelectedLyricsKind { Text, Instrumental }
public enum SelectedLyricsOrigin { Manual, Provider }
public enum SelectedLyricsSyncLevel { Plain, Line, Word }
public enum SelectedLyricsTimeReference { LyricsSourceSeconds }
public sealed record SelectedLyricsWord(string Text, double StartSeconds, double? EndSeconds = null);
public sealed record SelectedLyricsLine(string Text, double StartSeconds, double? EndSeconds,
    ImmutableArray<SelectedLyricsWord> Words);
public sealed record SelectedLyricsDocument(string Id, string Text, SelectedLyricsSyncLevel SyncLevel,
    ImmutableArray<SelectedLyricsLine> Lines,
    SelectedLyricsTimeReference TimeReference = SelectedLyricsTimeReference.LyricsSourceSeconds);
public sealed record SelectedLyricsProvenance(SelectedLyricsOrigin Origin, string SourceId, string CandidateId,
    string? SourceUrl, DateTimeOffset? CreatedAtUtc, DateTimeOffset? RetrievedAtUtc,
    string? Artist = null, string? Title = null, string? Album = null, string? Version = null, double? DurationSeconds = null);
/// <summary>Immutable selected payload supported by Song and versioned project persistence.</summary>
public sealed record SelectedLyrics(SelectedLyricsKind Kind, SelectedLyricsDocument? Document, SelectedLyricsProvenance Provenance);
public enum SelectedLyricsValidationError { InvalidSelection, InvalidDocument, InvalidProvenance, ResponseTooLarge }
public sealed class SelectedLyricsValidationException(SelectedLyricsValidationError error, string message) : ArgumentException(message)
{
    public SelectedLyricsValidationError Error { get; } = error;
}
