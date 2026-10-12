using System.Collections.Immutable;

namespace UltraStar.SongCreator.Acquisition;

public sealed record MediaSearchQuery(string? Artist = null, string? Title = null, int Limit = 10);
public sealed record OnlineMediaCandidate(string SourceId, string Id, string Title, string? Uploader,
    double? DurationSeconds, bool? IsLive, string SourceUrl);
public sealed record MediaSearchPage(MediaSearchQuery Query, ImmutableArray<OnlineMediaCandidate> Candidates, DateTimeOffset RetrievedAtUtc);
public sealed record MediaDownloadFormat(string Id, string Extension, string? AudioCodec, string? VideoCodec,
    string? Protocol, long? ByteLength, double? AudioBitrateKbps, int? Width, int? Height, bool IsDrm)
{
    public bool SupportsAudio => !IsDrm && Protocol == "https" && Extension == "m4a" && VideoCodec == "none" &&
        (AudioCodec == "aac" || AudioCodec?.StartsWith("mp4a.", StringComparison.Ordinal) == true);
    public bool SupportsVideo => !IsDrm && Protocol == "https" && Extension == "mp4" &&
        AudioCodec == "none" && VideoCodec is not null and not "none";
}
public sealed record MediaSourceDetails(OnlineMediaCandidate Candidate, ImmutableArray<MediaDownloadFormat> Formats, DateTimeOffset RetrievedAtUtc);
public sealed record MediaDownloadSelection(MediaSourceDetails Details, MediaDownloadFormat Audio, MediaDownloadFormat? Video);
public sealed record DownloadedMedia(MediaDownloadSelection Selection, MediaCandidate Audio, MediaCandidate? Video,
    string AudioSha256, string? VideoSha256, DateTimeOffset CompletedAtUtc, string DirectoryPath);

/// <summary>Optional online acquisition; files reuse the independent IMediaSource/IMediaProbe boundary.</summary>
public interface IMediaDownloadSource
{
    string SourceId { get; }
    Task<MediaSearchPage> SearchAsync(MediaSearchQuery query, CancellationToken cancellationToken = default);
    Task<MediaSourceDetails> ResolveAsync(string sourceUrl, CancellationToken cancellationToken = default);
    Task<DownloadedMedia> DownloadAsync(MediaDownloadSelection selection, string newDirectoryPath, CancellationToken cancellationToken = default);
}

public enum MediaDownloadError { ToolUnavailable, ToolFailed, TimedOut, OutputTooLarge, InvalidData, SourceUnavailable, UnsupportedFormat, InvalidSelection, DownloadTooLarge }
public sealed class MediaDownloadException(MediaDownloadError error, string message, int? exitCode = null, Exception? inner = null) : Exception(message, inner)
{
    public MediaDownloadError Error { get; } = error;
    public int? ExitCode { get; } = exitCode;
}

public static class MediaDownloadChoices
{
    public static MediaDownloadSelection Select(MediaSourceDetails details, string audioFormatId, string? videoFormatId = null)
    {
        ArgumentNullException.ThrowIfNull(details);
        YtDlpJson.ValidateCandidate(details.Candidate);
        if (details.Formats.IsDefault || details.Formats.Any(f => f is null) ||
            details.Formats.Select(f => f.Id).Distinct().Count() != details.Formats.Length)
            throw new MediaDownloadException(MediaDownloadError.InvalidSelection, "Format identities must be initialized and unique.");
        if (details.Candidate.IsLive == true)
            throw new MediaDownloadException(MediaDownloadError.SourceUnavailable, "Live/upcoming streams are outside this download profile.");
        var audio = details.Formats.SingleOrDefault(f => f.Id == audioFormatId);
        var video = videoFormatId is null ? null : details.Formats.SingleOrDefault(f => f.Id == videoFormatId);
        if (audio is null || (videoFormatId is not null && video is null))
            throw new MediaDownloadException(MediaDownloadError.InvalidSelection, "Choose explicit available format identities from this source.");
        if (!audio.SupportsAudio || (video is not null && !video.SupportsVideo))
            throw new MediaDownloadException(MediaDownloadError.UnsupportedFormat, "First profile requires separate HTTPS AAC/M4A audio and optional MP4 video.");
        if (!YtDlpJson.FormatId(audio.Id) || (video is not null && !YtDlpJson.FormatId(video.Id)))
            throw new MediaDownloadException(MediaDownloadError.InvalidSelection, "Invalid format identity.");
        return new(details, audio, video);
    }

    internal static void Validate(MediaDownloadSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (selection.Audio is null)
            throw new MediaDownloadException(MediaDownloadError.InvalidSelection, "Explicit audio format is required.");
        var checkedSelection = Select(selection.Details, selection.Audio.Id, selection.Video?.Id);
        if (checkedSelection.Audio != selection.Audio || checkedSelection.Video != selection.Video)
            throw new MediaDownloadException(MediaDownloadError.InvalidSelection, "Selected format does not match the inspected source.");
    }
}
