using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Acquisition;

public sealed record DownloadedFileSnapshot(string Location, string FormatId, string Extension, string? AudioCodec, string? VideoCodec, long Bytes, string Sha256);
public sealed record DownloadedSourceSnapshot(string SourceId, string Id, string SourceUrl, string Title, string? Uploader,
    double? DurationSeconds, DateTimeOffset CompletedAtUtc, DownloadedFileSnapshot Audio, DownloadedFileSnapshot? Video);

public static class DownloadedMediaDraft
{
    public const string HeaderName = "media.download.source.v1";
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 16
    };

    public static SongDraft Create(DownloadedMedia media, MetadataOverrides? overrides = null, RecordingSelection? recording = null)
    {
        var value = Serialize(media);
        var draft = LocalSongDraft.Create(media.Audio, overrides);
        var song = draft.Song;
        if (media.Video is { } video)
            song = song with { Media = song.Media.Add(new() { Kind = MediaKind.Video, Location = video.FilePath, Source = "youtube" }) };
        song = song with
        {
            Media = song.Media.Select(m => m with { Source = "youtube" }).ToImmutableArray(),
            ImportedSource = song.ImportedSource! with { Headers = song.ImportedSource.Headers.Add(new(HeaderName, value)) }
        };
        if (recording is not null) song = RecordingChoices.Attach(song, recording);
        return new(song, draft.Diagnostics.Add(new("SourceNeedsManualVerification", "ImportedSource",
            "YouTube identity and recording catalogue are separate; title/duration do not prove a matching recording or rights.")));
    }

    internal static string Serialize(DownloadedMedia media)
    {
        ArgumentNullException.ThrowIfNull(media);
        MediaDownloadChoices.Validate(media.Selection);
        var c = media.Selection.Details.Candidate;
        static FileV1 File(MediaCandidate file, MediaDownloadFormat format, string hash) =>
            new(file.FilePath, format.Id, format.Extension, format.AudioCodec, format.VideoCodec, file.ByteLength, hash);
        var stored = new SourceV1(1, c.SourceId, c.Id, c.SourceUrl, c.Title, c.Uploader, c.DurationSeconds,
            media.CompletedAtUtc.ToUniversalTime(), File(media.Audio, media.Selection.Audio, media.AudioSha256),
            media.Video is null ? null : File(media.Video, media.Selection.Video ??
                throw new MediaDownloadException(MediaDownloadError.InvalidData, "Missing video selection."), media.VideoSha256!));
        if ((media.Selection.Video is null) != (media.Video is null))
            throw new MediaDownloadException(MediaDownloadError.InvalidData, "Video selection/file mismatch.");
        Validate(stored);
        var text = JsonSerializer.Serialize(stored, Options);
        if (Utf8.GetByteCount(text) > 65536) throw new MediaDownloadException(MediaDownloadError.InvalidData, "Source snapshot exceeds 64 KiB.");
        return text;
    }

    /// <summary>Only persisted data is read. No network, probe, hash recomputation or file opening.</summary>
    public static DownloadedSourceSnapshot? Read(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);
        if (song.ImportedSource is not { } source) return null;
        if (source.Headers.IsDefault) throw new MediaDownloadException(MediaDownloadError.InvalidData, "Uninitialized source headers.");
        var matches = source.Headers.Where(h => h.Name == HeaderName).ToArray();
        if (matches.Length == 0) return null;
        if (source.FormatId != LocalSongDraft.SourceFormatId || matches.Length != 1)
            throw new MediaDownloadException(MediaDownloadError.InvalidData, "Ambiguous source snapshot.");
        try
        {
            var text = matches[0].Value;
            if (Utf8.GetByteCount(text) > 65536) throw new JsonException("Source snapshot too large.");
            using var json = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 16 });
            RecordingJson.Unique(json.RootElement);
            var value = JsonSerializer.Deserialize<SourceV1>(text, Options) ?? throw new JsonException("Missing source snapshot.");
            Validate(value);
            static DownloadedFileSnapshot File(FileV1 f) => new(f.Location, f.FormatId, f.Extension, f.AudioCodec, f.VideoCodec, f.Bytes, f.Sha256);
            return new(value.SourceId, value.Id, value.SourceUrl, value.Title, value.Uploader, value.DurationSeconds,
                value.CompletedAtUtc, File(value.Audio), value.Video is null ? null : File(value.Video));
        }
        catch (Exception e) when (e is JsonException or ArgumentException or MediaDownloadException)
        { throw new MediaDownloadException(MediaDownloadError.InvalidData, "Stored download source is invalid; no network fallback is attempted.", inner: e); }
    }

    private static void Validate(SourceV1 s)
    {
        if (s.Version != 1) throw new JsonException("Unsupported source version.");
        YtDlpJson.ValidateCandidate(new(s.SourceId, s.Id, s.Title, s.Uploader, s.DurationSeconds, false, s.SourceUrl));
        static void File(FileV1? f, bool audio)
        {
            if (f is null || f.Bytes <= 0 || !YtDlpJson.FormatId(f.FormatId) ||
                !Regex.IsMatch(f.Sha256 ?? "", @"\A[0-9a-f]{64}\z") || f.Extension != (audio ? "m4a" : "mp4"))
                throw new JsonException("Invalid file snapshot.");
            // Offline provenance remains readable when moved between Windows and Linux.
            // The stored location is a reference, never opened or resolved by Read.
            var path = f.Location;
            if (string.IsNullOrWhiteSpace(path) || path.Length > 4096 ||
                (!Path.IsPathFullyQualified(path) && !Regex.IsMatch(path, @"\A[A-Za-z]:[\\/].+") && !path.StartsWith(@"\\", StringComparison.Ordinal)) ||
                !path.EndsWith("." + f.Extension, StringComparison.Ordinal))
                throw new JsonException("Invalid absolute file reference/extension.");
            var format = new MediaDownloadFormat(f.FormatId, f.Extension, f.AudioCodec, f.VideoCodec, "https", f.Bytes, null, null, null, false);
            if (audio ? !format.SupportsAudio : !format.SupportsVideo) throw new JsonException("Unsupported file profile.");
        }
        File(s.Audio, true);
        if (s.Video is not null)
        {
            File(s.Video, false);
            if (s.Video.Location == s.Audio.Location) throw new JsonException("Separate media require separate paths.");
        }
    }

    // Frozen v1 storage is independent of additions to public network/format models.
    private sealed record FileV1(string Location, string FormatId, string Extension, string? AudioCodec, string? VideoCodec, long Bytes, string Sha256);
    private sealed record SourceV1(int Version, string SourceId, string Id, string SourceUrl, string Title, string? Uploader,
        double? DurationSeconds, DateTimeOffset CompletedAtUtc, FileV1 Audio, FileV1? Video);
}
