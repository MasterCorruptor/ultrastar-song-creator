using System.Collections.Immutable;
using System.Globalization;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Acquisition;

public static class LocalSongDraft
{
    public const string SourceFormatId = "local-media-v1";

    public static SongDraft Create(MediaCandidate candidate, MetadataOverrides? overrides = null,
        int? audioStreamIndex = null, int? videoStreamIndex = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (candidate.SourceId != "local-file" || candidate.ByteLength <= 0)
            throw new ArgumentException("A nonempty local-file candidate is required.", nameof(candidate));
        var path = LocalMediaSource.LocalPath(candidate.FilePath);
        var inspection = candidate.Inspection ?? throw new ArgumentException("Inspection is required.", nameof(candidate));
        if (inspection.Streams.IsDefault || inspection.Tags.IsDefault ||
            inspection.Streams.Any(s => s is null || s.Tags.IsDefault))
            throw new ArgumentException("Inspection collections must be initialized.", nameof(candidate));
        if (inspection.Streams.Select(s => s.Index).Distinct().Count() != inspection.Streams.Length)
            throw new ArgumentException("Stream indices must be unique.", nameof(candidate));
        overrides ??= new();
        var diagnostics = ImmutableArray.CreateBuilder<AcquisitionDiagnostic>();
        var audio = Select(inspection.Streams.Where(s => s.IsAudio).ToArray(), audioStreamIndex, "audio", required: true)!;
        var video = Select(inspection.Streams.Where(s => s.IsVideo).ToArray(), videoStreamIndex, "video", required: false);
        var levels = new[] { inspection.Tags, audio.Tags, video?.Tags ?? [] };

        string Metadata(string name, string? manual)
        {
            if (manual is not null) return manual.Trim();
            foreach (var level in levels)
            {
                var values = level.Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.Value.Trim()).Where(v => v.Length > 0 &&
                        (!name.Equals("language", StringComparison.OrdinalIgnoreCase) || !v.Equals("und", StringComparison.OrdinalIgnoreCase)))
                    .Distinct(StringComparer.Ordinal).ToArray();
                if (values.Length == 1) return values[0];
                if (values.Length > 1)
                {
                    diagnostics.Add(new("ConflictingMetadata", "Metadata." + name,
                        "Conflicting file values require manual correction; no value is selected implicitly."));
                    return "";
                }
            }
            return "";
        }

        var language = Metadata("language", overrides.Language);
        if (language.Equals("und", StringComparison.OrdinalIgnoreCase)) language = "";
        var media = ImmutableArray.CreateBuilder<MediaReference>();
        media.Add(new() { Kind = MediaKind.Audio, Location = path, Source = candidate.SourceId });
        if (video is not null) media.Add(new() { Kind = MediaKind.Video, Location = path, Source = candidate.SourceId });
        var source = ImmutableArray.CreateBuilder<SourceHeader>();
        void Put(string name, string value) => source.Add(new(name, value));
        void Number(string name, double? value)
        {
            if (value is not null) Put(name, value.Value.ToString("R", CultureInfo.InvariantCulture));
        }
        void Integer(string name, int? value)
        {
            if (value is not null) Put(name, value.Value.ToString(CultureInfo.InvariantCulture));
        }
        Put("source.id", candidate.SourceId);
        Put("source.bytes", candidate.ByteLength.ToString(CultureInfo.InvariantCulture));
        Put("format.name", inspection.Container);
        Number("format.durationSeconds", inspection.DurationSeconds);
        Integer("selection.audioStream", audio.Index);
        Integer("selection.videoStream", video?.Index);
        foreach (var tag in inspection.Tags) Put("format.tag." + tag.Name, tag.Value);
        foreach (var stream in inspection.Streams)
        {
            var prefix = "stream." + stream.Index.ToString(CultureInfo.InvariantCulture) + ".";
            Put(prefix + "kind", stream.Kind);
            if (stream.CodecName is not null) Put(prefix + "codec", stream.CodecName);
            Put(prefix + "attachedPicture", stream.AttachedPicture ? "true" : "false");
            Integer(prefix + "sampleRate", stream.SampleRate);
            Integer(prefix + "channels", stream.Channels);
            Integer(prefix + "width", stream.Width);
            Integer(prefix + "height", stream.Height);
            Number(prefix + "durationSeconds", stream.DurationSeconds);
            foreach (var tag in stream.Tags) Put(prefix + "tag." + tag.Name, tag.Value);
        }
        var song = new Song
        {
            Metadata = new()
            {
                Title = Metadata("title", overrides.Title),
                Artist = Metadata("artist", overrides.Artist),
                Language = language.Length == 0 ? null : language
            },
            Media = media.ToImmutable(),
            ImportedSource = new() { FormatId = SourceFormatId, FileReference = path, Headers = source.ToImmutable() }
        };
        if (inspection.Streams.Count(s => s.IsAudio) > 1 || inspection.Streams.Count(s => s.IsVideo) > 1)
            diagnostics.Add(new("SelectedStreamsRequireExtraction", "Inspection.Streams",
                "Stream selection is retained for later preprocessing; export of the original multi-stream file cannot preserve that choice."));
        if (inspection.DurationSeconds is null)
            diagnostics.Add(new("UnknownDuration", "Inspection.DurationSeconds", "Container duration is unknown; it is not inferred from metadata or file size."));
        if (inspection.Streams.Any(s => s.AttachedPicture))
            diagnostics.Add(new("EmbeddedArtworkRetained", "Inspection.Streams", "Embedded artwork remains in the source file; no cover asset is extracted."));
        foreach (var issue in SongValidator.Validate(song))
        {
            if (issue.Severity == ValidationSeverity.Error)
                throw new MediaAcquisitionException(AcquisitionError.InvalidProbeData, "Inspected media cannot form a valid draft: " + issue.Message);
            diagnostics.Add(new(issue.Code.ToString(), issue.Path, issue.Message));
        }
        return new(song, diagnostics.ToImmutable());
    }

    private static MediaStreamInfo? Select(MediaStreamInfo[] streams, int? index, string kind, bool required)
    {
        if (index is not null)
            return streams.SingleOrDefault(s => s.Index == index)
                ?? throw new MediaAcquisitionException(AcquisitionError.UnsupportedMedia, "Selected index does not identify a usable " + kind + " stream.");
        if (streams.Length > 1)
            throw new MediaAcquisitionException(AcquisitionError.AmbiguousStream, "Select an explicit " + kind + " stream index; the file contains several.");
        if (streams.Length == 1) return streams[0];
        if (required)
            throw new MediaAcquisitionException(AcquisitionError.UnsupportedMedia, "A song draft requires an audio stream; a silent video/image is not used as audio.");
        return null;
    }
}
