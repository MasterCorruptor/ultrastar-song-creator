using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Projects;

internal static class ProjectJson
{
    internal const string Format = "ultrastar-song-creator";
    internal const int Version = 2;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        WriteIndented = true,
        MaxDepth = 64,
        Converters =
        {
            new NamedEnumConverter<MediaKind>(("audio", MediaKind.Audio), ("video", MediaKind.Video)),
            new NamedEnumConverter<NoteType>(
                ("normal", NoteType.Normal), ("golden", NoteType.Golden), ("freestyle", NoteType.Freestyle),
                ("rap", NoteType.Rap), ("goldenRap", NoteType.GoldenRap)),
            new NamedEnumConverter<AnalysisKind>(
                ("waveform", AnalysisKind.Waveform), ("pitchCurve", AnalysisKind.PitchCurve), ("beatGrid", AnalysisKind.BeatGrid),
                ("vocalTrack", AnalysisKind.VocalTrack), ("alignment", AnalysisKind.Alignment), ("confidence", AnalysisKind.Confidence))
        }
    };

    internal static byte[] Encode(Song song)
    {
        Validate(song);
        try
        {
            var document = new EnvelopeV2
            {
                Format = Format, SchemaVersion = Version, Song = SongDocument.From(song),
                ImportedSource = song.ImportedSource is null ? null : SourceDocumentDto.From(song.ImportedSource)
            };
            return JsonSerializer.SerializeToUtf8Bytes(document, Options);
        }
        catch (JsonException e)
        {
            throw new ProjectFormatException(ProjectError.InvalidSong, "Song does not match the project storage schema.", inner: e);
        }
    }

    internal static (Song Song, ImmutableArray<ValidationIssue> Issues, int SchemaVersion) Decode(byte[] bytes)
    {
        try
        {
            ReadOnlyMemory<byte> input = bytes;
            if (bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) input = input[3..];
            using var json = JsonDocument.Parse(input, new JsonDocumentOptions { MaxDepth = 64 });
            CheckDuplicateProperties(json.RootElement);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String || format.GetString() != Format ||
                !root.TryGetProperty("schemaVersion", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number))
                throw new ProjectFormatException(ProjectError.InvalidEnvelope, "Missing/invalid project format or schemaVersion.");
            Song song;
            if (number == 1)
            {
                var legacy = JsonSerializer.Deserialize<Envelope>(input.Span, Options)
                    ?? throw new JsonException("Project document is null.");
                song = legacy.Song.ToSong(); // v1 migration adds no invented source metadata.
            }
            else if (number == Version)
            {
                var current = JsonSerializer.Deserialize<EnvelopeV2>(input.Span, Options)
                    ?? throw new JsonException("Project document is null.");
                song = current.Song.ToSong() with { ImportedSource = current.ImportedSource?.ToSource() };
            }
            else
                throw new ProjectFormatException(ProjectError.UnsupportedVersion, $"Unsupported project schema version: {number}.");
            return (song, Validate(song), number);
        }
        catch (JsonException e)
        {
            throw new ProjectFormatException(ProjectError.InvalidJson, "Project JSON is malformed or does not match the declared project schema.", inner: e);
        }
    }

    private static ImmutableArray<ValidationIssue> Validate(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);
        var issues = SongValidator.Validate(song);
        if (issues.Any(i => i.Severity == ValidationSeverity.Error))
            throw new ProjectFormatException(ProjectError.InvalidSong, "Project contains structural song errors.", issues);
        return issues;
    }

    private static void CheckDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new JsonException("Duplicate JSON property.");
                CheckDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) CheckDuplicateProperties(item);
    }

    private static T Required<T>(T? value) where T : class =>
        value ?? throw new JsonException("Null is not permitted for this v1 field.");

    // Freeze v1 names independently of future domain enum additions.
    private sealed class NamedEnumConverter<T>(params (string Name, T Value)[] entries) : JsonConverter<T> where T : struct, Enum
    {
        private readonly Dictionary<string, T> values = entries.ToDictionary(e => e.Name, e => e.Value, StringComparer.Ordinal);
        private readonly Dictionary<T, string> names = entries.ToDictionary(e => e.Value, e => e.Name);

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && values.TryGetValue(reader.GetString()!, out var value))
                return value;
            throw new JsonException("Expected one defined v1 enum name.");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (!names.TryGetValue(value, out var name)) throw new JsonException("Enum value is not defined in v1.");
            writer.WriteStringValue(name);
        }
    }

    private sealed record EnvelopeV2
    {
        public required string Format { get; init; }
        public required int SchemaVersion { get; init; }
        public required SongDocument Song { get; init; }
        public required SourceDocumentDto? ImportedSource { get; init; }
    }

    private sealed record SourceDocumentDto
    {
        public required string FormatId { get; init; }
        public required string? FileReference { get; init; }
        public required HeaderDocument[] Headers { get; init; }

        internal static SourceDocumentDto From(SourceDocument source) => new()
        {
            FormatId = source.FormatId, FileReference = source.FileReference,
            Headers = source.Headers.Select(h => new HeaderDocument { Name = h.Name, Value = h.Value }).ToArray()
        };

        internal SourceDocument ToSource() => new()
        {
            FormatId = Required(FormatId), FileReference = FileReference,
            Headers = Required(Headers).Select(h => new SourceHeader(Required(h).Name, Required(h.Value))).ToImmutableArray()
        };
    }

    private sealed record HeaderDocument
    {
        public required string Name { get; init; }
        public required string Value { get; init; }
    }

    private sealed record Envelope
    {
        public required string Format { get; init; }
        public required int SchemaVersion { get; init; }
        public required SongDocument Song { get; init; }
    }

    private sealed record SongDocument
    {
        public required Guid Id { get; init; }
        public required MetadataDocument Metadata { get; init; }
        public required MediaDocument[] Media { get; init; }
        public required double AudioOffsetSeconds { get; init; }
        public required double VideoOffsetSeconds { get; init; }
        public required double? BeatsPerMinute { get; init; }
        public required PhraseDocument[] Phrases { get; init; }
        public required AnalysisDocument Analysis { get; init; }

        internal static SongDocument From(Song song) => new()
        {
            Id = song.Id,
            Metadata = new()
            {
                Title = Required(song.Metadata.Title),
                Artist = Required(song.Metadata.Artist),
                Language = song.Metadata.Language
            },
            Media = song.Media.Select(m => new MediaDocument
            {
                Id = m.Id,
                Kind = m.Kind,
                Location = Required(m.Location),
                Source = m.Source
            }).ToArray(),
            AudioOffsetSeconds = song.AudioOffsetSeconds,
            VideoOffsetSeconds = song.VideoOffsetSeconds,
            BeatsPerMinute = song.BeatsPerMinute,
            Phrases = song.Phrases.Select(p => new PhraseDocument
            {
                Id = p.Id,
                StartSeconds = p.StartSeconds,
                EndSeconds = p.EndSeconds,
                Notes = p.Notes.Select(n => new NoteDocument
                {
                    Id = n.Id,
                    StartSeconds = n.StartSeconds,
                    DurationSeconds = n.DurationSeconds,
                    MidiPitch = n.MidiPitch,
                    Text = Required(n.Text),
                    Type = n.Type,
                    Confidence = n.Confidence,
                    AnalysisReferences = n.AnalysisReferences.ToArray()
                }).ToArray()
            }).ToArray(),
            Analysis = new()
            {
                Artifacts = song.Analysis.Artifacts.Select(a => new ArtifactDocument
                {
                    Id = a.Id,
                    Kind = a.Kind,
                    SourceMediaId = a.SourceMediaId,
                    Producer = Required(a.Producer),
                    ModelRevision = a.ModelRevision,
                    ContentReference = a.ContentReference,
                    Points = a.Points.Select(p => new PointDocument { TimeSeconds = p.TimeSeconds, Value = p.Value }).ToArray()
                }).ToArray()
            }
        };

        internal Song ToSong() => new()
        {
            Id = Id,
            Metadata = new()
            {
                Title = Required(Required(Metadata).Title),
                Artist = Required(Metadata.Artist),
                Language = Metadata.Language
            },
            Media = Required(Media).Select(m => new MediaReference
            {
                Id = Required(m).Id,
                Kind = m.Kind,
                Location = Required(m.Location),
                Source = m.Source
            }).ToImmutableArray(),
            AudioOffsetSeconds = AudioOffsetSeconds,
            VideoOffsetSeconds = VideoOffsetSeconds,
            BeatsPerMinute = BeatsPerMinute,
            Phrases = Required(Phrases).Select(p => new Phrase
            {
                Id = Required(p).Id,
                StartSeconds = p.StartSeconds,
                EndSeconds = p.EndSeconds,
                Notes = Required(p.Notes).Select(n => new Note
                {
                    Id = Required(n).Id,
                    StartSeconds = n.StartSeconds,
                    DurationSeconds = n.DurationSeconds,
                    MidiPitch = n.MidiPitch,
                    Text = Required(n.Text),
                    Type = n.Type,
                    Confidence = n.Confidence,
                    AnalysisReferences = Required(n.AnalysisReferences).ToImmutableArray()
                }).ToImmutableArray()
            }).ToImmutableArray(),
            Analysis = new()
            {
                Artifacts = Required(Required(Analysis).Artifacts).Select(a => new AnalysisArtifact
                {
                    Id = Required(a).Id,
                    Kind = a.Kind,
                    SourceMediaId = a.SourceMediaId,
                    Producer = Required(a.Producer),
                    ModelRevision = a.ModelRevision,
                    ContentReference = a.ContentReference,
                    Points = Required(a.Points).Select(p => new AnalysisPoint(Required(p).TimeSeconds, p.Value)).ToImmutableArray()
                }).ToImmutableArray()
            }
        };
    }

    private sealed record MetadataDocument
    {
        public required string Title { get; init; }
        public required string Artist { get; init; }
        public required string? Language { get; init; }
    }

    private sealed record MediaDocument
    {
        public required Guid Id { get; init; }
        public required MediaKind Kind { get; init; }
        public required string Location { get; init; }
        public required string? Source { get; init; }
    }

    private sealed record PhraseDocument
    {
        public required Guid Id { get; init; }
        public required double StartSeconds { get; init; }
        public required double EndSeconds { get; init; }
        public required NoteDocument[] Notes { get; init; }
    }

    private sealed record NoteDocument
    {
        public required Guid Id { get; init; }
        public required double StartSeconds { get; init; }
        public required double DurationSeconds { get; init; }
        public required int MidiPitch { get; init; }
        public required string Text { get; init; }
        public required NoteType Type { get; init; }
        public required double? Confidence { get; init; }
        public required Guid[] AnalysisReferences { get; init; }
    }

    private sealed record AnalysisDocument
    {
        public required ArtifactDocument[] Artifacts { get; init; }
    }

    private sealed record ArtifactDocument
    {
        public required Guid Id { get; init; }
        public required AnalysisKind Kind { get; init; }
        public required Guid? SourceMediaId { get; init; }
        public required string Producer { get; init; }
        public required string? ModelRevision { get; init; }
        public required string? ContentReference { get; init; }
        public required PointDocument[] Points { get; init; }
    }

    private sealed record PointDocument
    {
        public required double TimeSeconds { get; init; }
        public required double Value { get; init; }
    }
}
