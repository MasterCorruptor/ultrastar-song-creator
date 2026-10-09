using System.Collections.Immutable;

namespace UltraStar.SongCreator.Core;

public enum AnalysisKind { Waveform, PitchCurve, BeatGrid, VocalTrack, Alignment, Confidence }

public readonly record struct AnalysisPoint(double TimeSeconds, double Value);

public sealed record AnalysisArtifact
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public AnalysisKind Kind { get; init; }
    public Guid? SourceMediaId { get; init; }
    public string Producer { get; init; } = "";
    public string? ModelRevision { get; init; }
    public string? ContentReference { get; init; }
    public ImmutableArray<AnalysisPoint> Points { get; init; } = [];
}

public sealed record AnalysisData
{
    public ImmutableArray<AnalysisArtifact> Artifacts { get; init; } = [];
}
