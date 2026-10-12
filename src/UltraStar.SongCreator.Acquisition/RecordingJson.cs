using System.Collections.Immutable;
using System.Text.Json;

namespace UltraStar.SongCreator.Acquisition;

internal static class RecordingJson
{
    internal static RecordingPage Parse(string text, RecordingQuery query, DateTimeOffset retrieved)
    {
        try
        {
            using var doc = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 32 });
            Unique(doc.RootElement);
            var root = doc.RootElement;
            var total = root.GetProperty("count").GetInt32();
            var offset = root.GetProperty("offset").GetInt32();
            if (total < 0 || offset != query.Offset) throw new JsonException("Invalid count/offset.");
            var recordings = root.GetProperty("recordings");
            if (recordings.ValueKind != JsonValueKind.Array || recordings.GetArrayLength() > query.Limit)
                throw new JsonException("Invalid recording array.");
            var candidates = ImmutableArray.CreateBuilder<RecordingCandidate>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in recordings.EnumerateArray())
            {
                var id = Id(item);
                if (!ids.Add(id)) throw new JsonException("Duplicate recording identity.");
                var credits = ImmutableArray.CreateBuilder<RecordingArtist>();
                if (item.TryGetProperty("artist-credit", out var artists))
                {
                    if (artists.ValueKind != JsonValueKind.Array || artists.GetArrayLength() > 100)
                        throw new JsonException("Invalid artist-credit.");
                    foreach (var credit in artists.EnumerateArray())
                    {
                        var artist = credit.GetProperty("artist");
                        credits.Add(new(Id(artist), Text(credit, "name") ?? RequiredText(artist, "name"),
                            Text(credit, "joinphrase") ?? ""));
                    }
                }
                var releases = ImmutableArray.CreateBuilder<RecordingRelease>();
                var releaseIds = new HashSet<string>(StringComparer.Ordinal);
                if (item.TryGetProperty("releases", out var releaseArray))
                {
                    if (releaseArray.ValueKind != JsonValueKind.Array || releaseArray.GetArrayLength() > 250)
                        throw new JsonException("Invalid release array.");
                    foreach (var release in releaseArray.EnumerateArray())
                    {
                        var releaseId = Id(release);
                        if (!releaseIds.Add(releaseId)) throw new JsonException("Duplicate release identity.");
                        releases.Add(new(releaseId, RequiredText(release, "title"), Text(release, "date"),
                            Text(release, "country"), Text(release, "disambiguation")));
                    }
                }
                double? duration = null;
                if (item.TryGetProperty("length", out var length) && length.ValueKind != JsonValueKind.Null)
                {
                    if (!length.TryGetInt64(out var milliseconds) || milliseconds < 0 || milliseconds > 9007199254740991)
                        throw new JsonException("Invalid recording duration.");
                    duration = milliseconds / 1000.0;
                }
                bool? video = null;
                if (item.TryGetProperty("video", out var isVideo) && isVideo.ValueKind != JsonValueKind.Null)
                    video = isVideo.GetBoolean();
                candidates.Add(new("musicbrainz", id, RequiredText(item, "title"), credits.ToImmutable(),
                    duration, Text(item, "disambiguation"), video, releases.ToImmutable(),
                    "https://musicbrainz.org/recording/" + id));
            }
            if (candidates.Count > total) throw new JsonException("Result count is inconsistent.");
            return new("musicbrainz", query, total, offset, candidates.ToImmutable(), retrieved.ToUniversalTime());
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {
            throw new RecordingSearchException(RecordingSearchError.InvalidResponse, "Recording service returned invalid candidate data.", inner: e);
        }
    }

    internal static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new JsonException("Duplicate JSON property.");
                Unique(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var child in value.EnumerateArray()) Unique(child);
    }

    private static string Id(JsonElement value)
    {
        var text = RequiredText(value, "id");
        if (!Guid.TryParseExact(text, "D", out var id) || id == Guid.Empty) throw new JsonException("Invalid MusicBrainz identity.");
        return id.ToString("D");
    }

    private static string RequiredText(JsonElement value, string name) =>
        Text(value, name) is { } text && !string.IsNullOrWhiteSpace(text) ? text : throw new JsonException("Missing required text.");

    private static string? Text(JsonElement value, string name)
    {
        if (!value.TryGetProperty(name, out var property) || property.ValueKind == JsonValueKind.Null) return null;
        if (property.ValueKind != JsonValueKind.String) throw new JsonException("Expected text.");
        return property.GetString();
    }
}
