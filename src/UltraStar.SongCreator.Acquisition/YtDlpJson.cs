using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UltraStar.SongCreator.Acquisition;

internal static class YtDlpJson
{
    internal static bool VideoId(string? id) => id is not null && Regex.IsMatch(id, @"\A[A-Za-z0-9_-]{11}\z", RegexOptions.CultureInvariant);
    internal static bool FormatId(string? id) => id is not null && Regex.IsMatch(id, @"\A[A-Za-z0-9_-]{1,64}\z", RegexOptions.CultureInvariant);
    internal static string Url(string id) => "https://www.youtube.com/watch?v=" + id;

    internal static string CanonicalUrl(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !uri.IsDefaultPort || uri.UserInfo.Length != 0)
            throw new ArgumentException("Select an HTTPS YouTube video URL without credentials or custom port.", nameof(source));
        string? id = null;
        if (uri.Host == "youtu.be") id = uri.AbsolutePath.TrimStart('/');
        else if (uri.Host is "youtube.com" or "www.youtube.com" or "m.youtube.com")
        {
            if (uri.AbsolutePath == "/watch")
            {
                var values = uri.Query.TrimStart('?').Split('&').Where(p => p.StartsWith("v=", StringComparison.Ordinal))
                    .Select(p => p[2..]).ToArray();
                if (values.Length == 1) id = values[0];
            }
            else if (uri.AbsolutePath.StartsWith("/shorts/", StringComparison.Ordinal))
                id = uri.AbsolutePath[8..];
        }
        if (!VideoId(id)) throw new ArgumentException("Select one concrete YouTube video, not a playlist/channel/search URL.", nameof(source));
        return Url(id!);
    }

    internal static void ValidateCandidate(OnlineMediaCandidate candidate)
    {
        if (candidate is null || candidate.SourceId != "youtube" || !VideoId(candidate.Id) || candidate.SourceUrl != Url(candidate.Id) ||
            string.IsNullOrWhiteSpace(candidate.Title) || candidate.Title.Length > 4096 ||
            (candidate.DurationSeconds is { } duration && (!double.IsFinite(duration) || duration < 0)))
            throw new MediaDownloadException(MediaDownloadError.InvalidSelection, "Invalid YouTube source identity.");
    }

    private static JsonDocument Document(string body)
    {
        var doc = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 32 });
        try { RecordingJson.Unique(doc.RootElement); return doc; }
        catch { doc.Dispose(); throw; }
    }

    internal static MediaSearchPage Search(string body, MediaSearchQuery query, DateTimeOffset retrieved) => Parse(() =>
    {
        using var doc = Document(body);
        var entries = doc.RootElement.GetProperty("entries");
        if (entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() > query.Limit) throw new JsonException("Invalid search page.");
        var result = ImmutableArray.CreateBuilder<OnlineMediaCandidate>();
        var ids = new HashSet<string>();
        foreach (var entry in entries.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Null) continue; // yt-dlp can retain unavailable playlist slots.
            var candidate = Candidate(entry);
            if (!ids.Add(candidate.Id)) throw new JsonException("Duplicate source identity.");
            result.Add(candidate);
        }
        return new MediaSearchPage(query, result.ToImmutable(), retrieved.ToUniversalTime());
    });

    internal static MediaSourceDetails Details(string body, string expectedUrl, DateTimeOffset retrieved) => Parse(() =>
    {
        using var doc = Document(body);
        var root = doc.RootElement;
        var candidate = Candidate(root);
        if (candidate.SourceUrl != expectedUrl) throw new JsonException("Resolved source identity differs from the selected video.");
        if (candidate.IsLive == true || Text(root, "availability") is "private" or "premium_only" or "subscriber_only" or "needs_auth")
            throw new MediaDownloadException(MediaDownloadError.SourceUnavailable, "Selected source is not an anonymous non-live download.");
        var array = root.GetProperty("formats");
        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() > 1000) throw new JsonException("Invalid format list.");
        var formats = ImmutableArray.CreateBuilder<MediaDownloadFormat>();
        var ids = new HashSet<string>();
        foreach (var item in array.EnumerateArray())
        {
            var id = Required(item, "format_id");
            if (!FormatId(id) || !ids.Add(id)) throw new JsonException("Invalid/duplicate format identity.");
            long? bytes = null;
            if (item.TryGetProperty("filesize", out var size) && size.ValueKind != JsonValueKind.Null)
            {
                bytes = size.GetInt64();
                if (bytes < 0) throw new JsonException("Invalid filesize.");
            }
            formats.Add(new(id, Required(item, "ext"), Text(item, "acodec"), Text(item, "vcodec"), Text(item, "protocol"),
                bytes, Number(item, "abr"), Integer(item, "width"), Integer(item, "height"), Boolean(item, "has_drm") ?? false));
        }
        return new MediaSourceDetails(candidate, formats.ToImmutable(), retrieved.ToUniversalTime());
    });

    private static OnlineMediaCandidate Candidate(JsonElement item)
    {
        var id = Required(item, "id");
        if (!VideoId(id)) throw new JsonException("Invalid video identity.");
        var extractor = Text(item, "extractor_key") ?? Text(item, "ie_key");
        if (extractor is not null && !extractor.Equals("Youtube", StringComparison.OrdinalIgnoreCase)) throw new JsonException("Unexpected extractor.");
        var live = Boolean(item, "is_live");
        if (Text(item, "live_status") is "is_live" or "is_upcoming" or "post_live") live = true;
        return new("youtube", id, Required(item, "title"), Text(item, "uploader"), Number(item, "duration"), live, Url(id));
    }

    private static T Parse<T>(Func<T> parse)
    {
        try { return parse(); }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new MediaDownloadException(MediaDownloadError.InvalidData, "Media source returned invalid metadata; no source is selected.", inner: e); }
    }
    private static string? Text(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.String) throw new JsonException("Expected text.");
        var text = value.GetString()!;
        if (text.Length > 4096) throw new JsonException("Text exceeds profile limit.");
        return text;
    }
    private static string Required(JsonElement item, string name) =>
        Text(item, name) is { } text && !string.IsNullOrWhiteSpace(text) ? text : throw new JsonException("Required field missing.");
    private static double? Number(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        var number = value.GetDouble();
        if (!double.IsFinite(number) || number < 0) throw new JsonException("Invalid nonnegative number.");
        return number;
    }
    private static int? Integer(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        var number = value.GetInt32();
        if (number <= 0) throw new JsonException("Invalid dimension.");
        return number;
    }
    private static bool? Boolean(JsonElement item, string name) =>
        !item.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null ? null : value.GetBoolean();
}
