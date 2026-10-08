using System.Collections.Immutable;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.UltraStar;

public enum ImportCode
{
    MissingHeader, DuplicateHeader, InvalidHeader, UnsupportedVersion, UnsupportedRelativeTiming,
    UnsupportedDuet, InvalidLine, InvalidNote, InvalidPitch, InvalidTiming, UnexpectedHeader,
    MissingEndMarker, IgnoredPitch, UnsortedNotes, OverlappingNotes, MissingText, PhraseMarker,
    InvalidSong, UnsupportedEncoding, InvalidEncoding, InputTooLarge, RetainedPlaybackRange
}

public sealed record ImportDiagnostic(ValidationSeverity Severity, ImportCode Code, int? Line, string Message);

public sealed record ImportResult(Song? Song, ImmutableArray<ImportDiagnostic> Diagnostics)
{
    public bool Success => Song is not null;
}
