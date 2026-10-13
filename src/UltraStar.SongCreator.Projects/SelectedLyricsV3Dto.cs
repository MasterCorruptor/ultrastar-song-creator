using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Projects;

internal static partial class ProjectJson
{
    private sealed record EnvelopeV3
    {
        public required string Format { get; init; }
        public required int SchemaVersion { get; init; }
        public required SongDocument Song { get; init; }
        public required SourceDocumentDto? ImportedSource { get; init; }
        public required SelectedLyricsV3Dto? SelectedLyrics { get; init; }
    }

    // Frozen strings deliberately do not serialize evolving Core/Acquisition enums by name.
    private sealed record SelectedLyricsV3Dto
    {
        public required string Kind { get; init; }
        public required LyricsDocumentV3Dto? Document { get; init; }
        public required LyricsProvenanceV3Dto Provenance { get; init; }

        internal static SelectedLyricsV3Dto From(SelectedLyrics selected) => new()
        {
            Kind = selected.Kind switch
            {
                SelectedLyricsKind.Text => "text",
                SelectedLyricsKind.Instrumental => "instrumental",
                _ => throw new JsonException("Unknown v3 selected kind.")
            },
            Document = selected.Document is null ? null : LyricsDocumentV3Dto.From(selected.Document),
            Provenance = LyricsProvenanceV3Dto.From(selected.Provenance)
        };

        internal SelectedLyrics ToSelected() => new(Kind switch
        {
            "text" => SelectedLyricsKind.Text,
            "instrumental" => SelectedLyricsKind.Instrumental,
            _ => throw new JsonException("Unknown v3 selected kind.")
        }, Document?.ToDocument(), Required(Provenance).ToProvenance());
    }

    private sealed record LyricsDocumentV3Dto
    {
        public required string Id { get; init; }
        public required string Text { get; init; }
        public required string SyncLevel { get; init; }
        public required LyricsLineV3Dto[] Lines { get; init; }
        public required string TimeReference { get; init; }

        internal static LyricsDocumentV3Dto From(SelectedLyricsDocument document) => new()
        {
            Id = document.Id,
            Text = document.Text,
            SyncLevel = document.SyncLevel switch
            {
                SelectedLyricsSyncLevel.Plain => "plain",
                SelectedLyricsSyncLevel.Line => "line",
                SelectedLyricsSyncLevel.Word => "word",
                _ => throw new JsonException("Unknown v3 sync level.")
            },
            TimeReference = document.TimeReference == SelectedLyricsTimeReference.LyricsSourceSeconds
                ? "lyricsSourceSeconds" : throw new JsonException("Unknown v3 time reference."),
            Lines = document.Lines.Select(LyricsLineV3Dto.From).ToArray()
        };

        internal SelectedLyricsDocument ToDocument() => new(Required(Id), Required(Text), SyncLevel switch
        {
            "plain" => SelectedLyricsSyncLevel.Plain,
            "line" => SelectedLyricsSyncLevel.Line,
            "word" => SelectedLyricsSyncLevel.Word,
            _ => throw new JsonException("Unknown v3 sync level.")
        }, Required(Lines).Select(line => Required(line).ToLine()).ToImmutableArray(), TimeReference switch
        {
            "lyricsSourceSeconds" => SelectedLyricsTimeReference.LyricsSourceSeconds,
            _ => throw new JsonException("Unknown v3 time reference.")
        });
    }

    private sealed record LyricsLineV3Dto
    {
        public required string Text { get; init; }
        public required double StartSeconds { get; init; }
        public required double? EndSeconds { get; init; }
        public required LyricsWordV3Dto[] Words { get; init; }

        internal static LyricsLineV3Dto From(SelectedLyricsLine line) => new()
        {
            Text = line.Text,
            StartSeconds = line.StartSeconds,
            EndSeconds = line.EndSeconds,
            Words = line.Words.Select(word => new LyricsWordV3Dto
            { Text = word.Text, StartSeconds = word.StartSeconds, EndSeconds = word.EndSeconds }).ToArray()
        };
        internal SelectedLyricsLine ToLine() => new(Required(Text), StartSeconds, EndSeconds,
            Required(Words).Select(word => new SelectedLyricsWord(Required(Required(word).Text),
                word.StartSeconds, word.EndSeconds)).ToImmutableArray());
    }

    private sealed record LyricsWordV3Dto
    {
        public required string Text { get; init; }
        public required double StartSeconds { get; init; }
        public required double? EndSeconds { get; init; }
    }

    private sealed record LyricsProvenanceV3Dto
    {
        public required string Origin { get; init; }
        public required string SourceId { get; init; }
        public required string CandidateId { get; init; }
        public required string? SourceUrl { get; init; }
        public required DateTimeOffset? CreatedAtUtc { get; init; }
        public required DateTimeOffset? RetrievedAtUtc { get; init; }
        public required string? Artist { get; init; }
        public required string? Title { get; init; }
        public required string? Album { get; init; }
        public required string? Version { get; init; }
        public required double? DurationSeconds { get; init; }

        internal static LyricsProvenanceV3Dto From(SelectedLyricsProvenance source) => new()
        {
            Origin = source.Origin switch
            {
                SelectedLyricsOrigin.Manual => "manual",
                SelectedLyricsOrigin.Provider => "provider",
                _ => throw new JsonException("Unknown v3 origin.")
            },
            SourceId = source.SourceId,
            CandidateId = source.CandidateId,
            SourceUrl = source.SourceUrl,
            CreatedAtUtc = source.CreatedAtUtc,
            RetrievedAtUtc = source.RetrievedAtUtc,
            Artist = source.Artist,
            Title = source.Title,
            Album = source.Album,
            Version = source.Version,
            DurationSeconds = source.DurationSeconds
        };
        internal SelectedLyricsProvenance ToProvenance() => new(Origin switch
        {
            "manual" => SelectedLyricsOrigin.Manual,
            "provider" => SelectedLyricsOrigin.Provider,
            _ => throw new JsonException("Unknown v3 origin.")
        }, Required(SourceId), Required(CandidateId), SourceUrl, CreatedAtUtc, RetrievedAtUtc,
            Artist, Title, Album, Version, DurationSeconds);
    }

    private static readonly UTF8Encoding SelectedUtf8 = new(false, true);

    private static void PreflightSelectedLyrics(JsonElement root)
    {
        // JsonDocument is bounded by the store's wire limit; inspect before DTO/immutable arrays allocate.
        static JsonElement Field(JsonElement value, string name)
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(name, out var field))
                throw new JsonException("Missing/invalid selected lyrics object property.");
            return field;
        }
        static long StringBytes(JsonElement value, bool nullable = false)
        {
            if (nullable && value.ValueKind == JsonValueKind.Null) return 0;
            if (value.ValueKind != JsonValueKind.String) throw new JsonException("Expected selected lyrics string.");
            try { return SelectedUtf8.GetByteCount(value.GetString()!); }
            catch (Exception e) when (e is InvalidOperationException or EncoderFallbackException)
            { throw new JsonException("Invalid selected lyrics Unicode.", e); }
        }
        static void Budget(long size, long maximum)
        {
            if (size > maximum)
                throw new ProjectFormatException(ProjectError.TooLarge, "Selected lyrics logical payload limit exceeded.");
        }
        static void Array(JsonElement value, int maximum)
        {
            if (value.ValueKind != JsonValueKind.Array) throw new JsonException("Expected selected lyrics array.");
            Budget(value.GetArrayLength(), maximum);
        }

        var selected = Field(root, "selectedLyrics");
        if (selected.ValueKind == JsonValueKind.Null) return;
        _ = StringBytes(Field(selected, "kind"));
        var document = Field(selected, "document");
        var source = Field(selected, "provenance");
        _ = StringBytes(Field(source, "origin"));
        var size = 128L;
        foreach (var name in new[] { "sourceId", "candidateId", "sourceUrl", "artist", "title", "album", "version" })
            size += StringBytes(Field(source, name), name is not ("sourceId" or "candidateId"));
        Budget(size, SelectedLyricsValidation.MaxSelectedBytes);
        if (document.ValueKind != JsonValueKind.Null)
        {
            _ = StringBytes(Field(document, "syncLevel"));
            _ = StringBytes(Field(document, "timeReference"));
            var documentSize = 64L + StringBytes(Field(document, "id")) + StringBytes(Field(document, "text"));
            Budget(documentSize, SelectedLyricsValidation.MaxDocumentBytes);
            var lines = Field(document, "lines");
            Array(lines, SelectedLyricsValidation.MaxLines);
            var wordCount = 0L;
            foreach (var line in lines.EnumerateArray())
            {
                documentSize += 48L + StringBytes(Field(line, "text"));
                Budget(documentSize, SelectedLyricsValidation.MaxDocumentBytes);
                var words = Field(line, "words");
                Array(words, SelectedLyricsValidation.MaxWords);
                wordCount += words.GetArrayLength();
                Budget(wordCount, SelectedLyricsValidation.MaxWords);
                foreach (var word in words.EnumerateArray())
                {
                    documentSize += 32L + StringBytes(Field(word, "text"));
                    Budget(documentSize, SelectedLyricsValidation.MaxDocumentBytes);
                }
            }
            size += documentSize;
        }
        Budget(size, SelectedLyricsValidation.MaxSelectedBytes);
    }
}
