using System.Collections.Immutable;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Acquisition;

public sealed record MediaTag(string Name, string Value);

public sealed record MediaStreamInfo(
    int Index, string Kind, string? CodecName, bool AttachedPicture,
    int? SampleRate, int? Channels, int? Width, int? Height,
    double? DurationSeconds, ImmutableArray<MediaTag> Tags)
{
    public bool IsAudio => Kind == "audio";
    public bool IsVideo => Kind == "video" && !AttachedPicture;
}

public sealed record MediaInspection(
    string Container, double? DurationSeconds,
    ImmutableArray<MediaStreamInfo> Streams, ImmutableArray<MediaTag> Tags);

public sealed record MediaCandidate(
    string SourceId, string FilePath, long ByteLength, MediaInspection Inspection);

public interface IMediaProbe
{
    Task<MediaInspection> InspectAsync(string absoluteFilePath, CancellationToken cancellationToken = default);
}

public interface IMediaSource
{
    string SourceId { get; }
    Task<MediaCandidate> InspectAsync(string absoluteFilePath, CancellationToken cancellationToken = default);
}

/// <summary>Null retains the file value; empty string deliberately clears it.</summary>
public sealed record MetadataOverrides(string? Title = null, string? Artist = null, string? Language = null);

public sealed record AcquisitionDiagnostic(string Code, string Path, string Message);
public sealed record SongDraft(Song Song, ImmutableArray<AcquisitionDiagnostic> Diagnostics);

public enum AcquisitionError
{
    ProbeUnavailable, ProbeFailed, ProbeTimedOut, ProbeOutputTooLarge, InvalidProbeData,
    UnsupportedMedia, AmbiguousStream
}

public sealed class MediaAcquisitionException(
    AcquisitionError error, string message, Exception? innerException = null) : Exception(message, innerException)
{
    public AcquisitionError Error { get; } = error;
}
