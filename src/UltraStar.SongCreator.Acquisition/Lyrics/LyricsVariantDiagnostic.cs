namespace UltraStar.SongCreator.Acquisition.Lyrics;

public enum LyricsVariantDiagnosticCode { InvalidSyntax, TimingOutOfRange, UnsupportedFormat, NoLyrics, ExplicitZeroOffset }
public enum LyricsVariantDiagnosticSeverity { Info, Warning }
/// <summary>Transient format-level information. No raw text, messages, URLs, or confidence payload.</summary>
public sealed record LyricsVariantDiagnostic(string FormatId, string? VariantId, LyricsVariantDiagnosticCode Code,
    LyricsVariantDiagnosticSeverity Severity, int? PhysicalLineNumber = null);
