using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace UltraStar.SongCreator.Acquisition.Lyrics;

/// <summary>Pure bounded response decoder. Never sends requests, follows URLs, or retains raw provider payload.</summary>
internal static class LrclibJson
{
    internal const int MaxInputBytes = 4 * 1024 * 1024;
    private const string Provider = "lrclib";
    private static readonly UTF8Encoding Utf8 = new(false, true);

    internal static LyricsPage Parse(ReadOnlySpan<byte> utf8, LyricsQuery originalQuery, DateTimeOffset retrievedAtUtc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LyricsValidation.Query(originalQuery);
        if (retrievedAtUtc == default || retrievedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(retrievedAtUtc), "Receipt must be a non-default UTC instant.");
        if (utf8.Length > MaxInputBytes) throw Error(LyricsSearchError.ResponseTooLarge);
        try
        {
            Utf8.GetCharCount(utf8); cancellationToken.ThrowIfCancellationRequested();
            using var json = JsonDocument.Parse(utf8.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            ValidateJson(json.RootElement, cancellationToken);
            if (json.RootElement.ValueKind != JsonValueKind.Array) throw Error();
            if (json.RootElement.GetArrayLength() > 20) throw Error(LyricsSearchError.ResponseTooLarge);
            var all = ImmutableArray.CreateBuilder<LyricsCandidate>();
            foreach (var entry in json.RootElement.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                all.Add(Candidate(entry, originalQuery, retrievedAtUtc, cancellationToken));
            }
            var candidates = all.ToImmutable();
            LyricsValidation.ValidateProviderResults(Provider, originalQuery, candidates, 20);
            cancellationToken.ThrowIfCancellationRequested();
            var page = new LyricsPage(Provider, originalQuery, candidates.Take(Math.Min(originalQuery.Limit, 20)).ToImmutableArray(), retrievedAtUtc);
            LyricsValidation.Page(page); cancellationToken.ThrowIfCancellationRequested(); return page;
        }
        catch (LyricsValidationException e)
        {
            throw Error(e.Error == LyricsValidationError.ResponseTooLarge ? LyricsSearchError.ResponseTooLarge : LyricsSearchError.InvalidResponse);
        }
        catch (Exception e) when (e is JsonException or DecoderFallbackException or EncoderFallbackException
            or InvalidOperationException or FormatException or OverflowException)
        { throw Error(); }
    }
    private static LyricsSearchException Error(LyricsSearchError error = LyricsSearchError.InvalidResponse) =>
        new(error, error == LyricsSearchError.ResponseTooLarge ? "LRCLIB response exceeds a supported resource bound." : "LRCLIB response does not match the supported profile.");

    private static void ValidateJson(JsonElement value, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                token.ThrowIfCancellationRequested(); Utf8.GetByteCount(property.Name);
                if (!names.Add(property.Name)) throw Error();
                ValidateJson(property.Value, token);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var child in value.EnumerateArray()) ValidateJson(child, token);
        else if (value.ValueKind == JsonValueKind.String) Utf8.GetByteCount(value.GetString()!);
    }
    private static string? String(JsonElement record, string name, int? maximumCharacters = null)
    {
        if (!record.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.String) throw Error();
        var text = value.GetString()!;
        if (maximumCharacters is { } maximum && text.Length > maximum) throw Error(LyricsSearchError.ResponseTooLarge);
        return text;
    }
    private static bool? Boolean(JsonElement record, string name)
    {
        if (!record.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        return value.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, _ => throw Error() };
    }
    private static string? LegacyText(JsonElement entry, string name)
    {
        var text = String(entry, name, LyricsValidation.MaxDocumentBytes * 2);
        return text is null ? null : LyricsValidation.NormalizeText(text);
    }
    private static LyricsCandidate Candidate(JsonElement entry, LyricsQuery query, DateTimeOffset at, CancellationToken token)
    {
        if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("id", out var idElement)
            || idElement.ValueKind != JsonValueKind.Number || !idElement.TryGetInt64(out var id) || id <= 0) throw Error();
        var instrumental = Boolean(entry, "instrumental") ?? throw Error();
        var reportedWord = Boolean(entry, "hasWordSync");
        var name = String(entry, "name", LyricsValidation.MaxQueryLength);
        var track = String(entry, "trackName", LyricsValidation.MaxQueryLength);
        if (name is not null && track is not null && name != track) throw Error();
        var artist = String(entry, "artistName", LyricsValidation.MaxQueryLength);
        var album = String(entry, "albumName", LyricsValidation.MaxQueryLength);
        double? duration = null;
        if (entry.TryGetProperty("duration", out var durationElement) && durationElement.ValueKind != JsonValueKind.Null)
        {
            if (durationElement.ValueKind != JsonValueKind.Number || !durationElement.TryGetDouble(out var parsed)
                || !double.IsFinite(parsed) || parsed <= 0 || parsed > LyricsValidation.MaxTimeSeconds) throw Error();
            duration = parsed;
        }
        var plain = LegacyText(entry, "plainLyrics");
        var synced = LegacyText(entry, "syncedLyrics");
        var yaml = String(entry, "lyricsfile");
        var hasPlain = !string.IsNullOrWhiteSpace(plain); var hasSynced = !string.IsNullOrWhiteSpace(synced);
        var hasYaml = !string.IsNullOrEmpty(yaml);
        if (instrumental && (hasPlain || hasSynced || reportedWord == true)) throw Error();
        var documents = ImmutableArray.CreateBuilder<LyricsDocument>();
        var diagnostics = ImmutableArray.CreateBuilder<LyricsVariantDiagnostic>();
        var unsupported = ImmutableArray.CreateBuilder<string>();
        if (hasYaml)
        {
            unsupported.Add("lyricsfile-yaml");
            diagnostics.Add(new("lyricsfile-yaml", null, LyricsVariantDiagnosticCode.UnsupportedFormat, LyricsVariantDiagnosticSeverity.Warning));
        }
        LrcParseStatus? lrcStatus = null;
        if (hasPlain) documents.Add(new("plain", plain!, LyricsSyncLevel.Plain, []));
        if (hasSynced)
        {
            token.ThrowIfCancellationRequested();
            var lrc = LrcParser.Parse(synced!); token.ThrowIfCancellationRequested(); lrcStatus = lrc.Status;
            if (lrc.Status == LrcParseStatus.TooLarge) throw Error(LyricsSearchError.ResponseTooLarge);
            if (lrc.Status == LrcParseStatus.Success) documents.Add(lrc.Document!);
            if (lrc.Status == LrcParseStatus.UnsupportedFormat) unsupported.Add("lrc-simple-line-v1");
            foreach (var diagnostic in lrc.Diagnostics)
            {
                var code = diagnostic.Code switch
                {
                    LrcDiagnosticCode.ExplicitZeroOffset => LyricsVariantDiagnosticCode.ExplicitZeroOffset,
                    LrcDiagnosticCode.NoLyrics => LyricsVariantDiagnosticCode.NoLyrics,
                    LrcDiagnosticCode.TimeOutOfBounds => LyricsVariantDiagnosticCode.TimingOutOfRange,
                    LrcDiagnosticCode.UnsupportedSyntax or LrcDiagnosticCode.UnsupportedOffset => LyricsVariantDiagnosticCode.UnsupportedFormat,
                    LrcDiagnosticCode.InvalidUnicode or LrcDiagnosticCode.MalformedTimestamp or LrcDiagnosticCode.DuplicateOffset or LrcDiagnosticCode.InvalidOffset => LyricsVariantDiagnosticCode.InvalidSyntax,
                    _ => throw Error()
                };
                diagnostics.Add(new("lrc-simple-line-v1", "line-lrc", code,
                    code == LyricsVariantDiagnosticCode.ExplicitZeroOffset ? LyricsVariantDiagnosticSeverity.Info : LyricsVariantDiagnosticSeverity.Warning,
                    diagnostic.LineNumber));
            }
        }
        if (documents.Count == 0 && lrcStatus is LrcParseStatus.MalformedInput or LrcParseStatus.TimingOutOfRange) throw Error();
        var status = instrumental ? LyricsTextStatus.Instrumental : documents.Count > 0 ? LyricsTextStatus.Available
            : unsupported.Count > 0 ? LyricsTextStatus.Unsupported : LyricsTextStatus.Missing;
        var candidate = new LyricsCandidate
        {
            ProviderId = Provider,
            Id = id.ToString(CultureInfo.InvariantCulture),
            Artist = artist,
            Title = track ?? name,
            Album = album,
            DurationSeconds = duration,
            SourceUrl = "https://lrclib.net/api/get/" + id.ToString(CultureInfo.InvariantCulture),
            RetrievedAtUtc = at,
            TextStatus = status,
            Documents = documents.ToImmutable(),
            VariantDiagnostics = diagnostics.ToImmutable(),
            Capabilities = new(reportedWord, hasSynced ? true : null) { UnsupportedFormats = unsupported.ToImmutable() },
            Match = new(DurationDeltaSeconds: duration.HasValue && query.DurationSeconds.HasValue ? duration.Value - query.DurationSeconds.Value : null)
        };
        LyricsValidation.Candidate(candidate); return candidate;
    }
}
