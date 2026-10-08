using System.Collections.Immutable;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.UltraStar;

public enum ExportFormat { Unversioned, V1 }

public sealed record ExportOptions(ExportFormat Format, double? GridBpm = null, string? ReferenceDirectory = null);

public enum ExportCode
{
    InvalidOptions, InvalidSong, MissingMetadata, MissingGrid, MissingNotes, MissingMedia, AmbiguousMedia,
    InvalidOffset, InvalidText, InvalidHeader, UnsupportedSource, UnsupportedDuet, EmptyPhrase,
    UnrepresentableTiming, CollapsedNote, TimingRounded, HeaderRegenerated, InvalidMedley,
    ProjectOnlyData, PhraseBoundsDerived, NonScoringPitchNormalized, DraftWarning, OutputTooLarge,
    MissingReferenceDirectory, NonLocalReference
}

public sealed record ExportDiagnostic(ValidationSeverity Severity, ExportCode Code, string Path, Guid? EntityId, string Message);

public sealed record QuantizedNote(Guid NoteId, long StartBeat, long EndBeat, double OriginalStartSeconds,
    double OriginalEndSeconds, double ExportedStartSeconds, double ExportedEndSeconds, double OriginalDurationSeconds, double ExportedDurationSeconds)
{
    public double StartDeltaSeconds => ExportedStartSeconds - OriginalStartSeconds;
    public double EndDeltaSeconds => ExportedEndSeconds - OriginalEndSeconds;
    public double DurationDeltaSeconds => ExportedDurationSeconds - OriginalDurationSeconds;
}

public sealed record ExportProgress(string AssetReference, long BytesCopied, long TotalBytes);

public sealed record PackagedAsset(string SourcePath, string RelativePath, long ByteLength, string Sha256);

public sealed record ExportResult(string? Text, ImmutableArray<ExportDiagnostic> Diagnostics, ImmutableArray<QuantizedNote> Notes)
{
    public bool Success => Text is not null;
    public string? DirectoryPath { get; init; }
    public ImmutableArray<PackagedAsset> Assets { get; init; } = [];
}
