using System.Collections.Immutable;

namespace UltraStar.SongCreator.Acquisition.Lyrics;

public enum LrcParseStatus { Success, MalformedInput, TimingOutOfRange, TooLarge, UnsupportedFormat, NoLyrics }
public enum LrcDiagnosticCode { InvalidUnicode, MalformedTimestamp, TimeOutOfBounds, UnsupportedSyntax, UnsupportedOffset, DuplicateOffset, InvalidOffset, LimitExceeded, NoLyrics, ExplicitZeroOffset }
public sealed record LrcMetadata(string Name, string Value, int LineNumber);
public sealed record LrcDiagnostic(LrcDiagnosticCode Code, int? LineNumber);
public sealed record LrcParseResult(LrcParseStatus Status, LyricsDocument? Document, string? NormalizedLrc,
    ImmutableArray<LrcMetadata> Metadata, ImmutableArray<LrcDiagnostic> Diagnostics);
