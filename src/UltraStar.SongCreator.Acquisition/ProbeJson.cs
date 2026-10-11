using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;

namespace UltraStar.SongCreator.Acquisition;

internal static class ProbeJson
{
    internal static MediaInspection Parse(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 32 });
            UniqueProperties(document.RootElement);
            var root = document.RootElement;
            var format = root.GetProperty("format");
            var container = Text(format, "format_name") ?? throw new JsonException("Container is missing.");
            if (string.IsNullOrWhiteSpace(container)) throw new JsonException("Container is empty.");
            var streams = root.GetProperty("streams");
            if (streams.ValueKind != JsonValueKind.Array || streams.GetArrayLength() > 256)
                throw new JsonException("Invalid stream collection.");
            var ids = new HashSet<int>();
            var parsed = ImmutableArray.CreateBuilder<MediaStreamInfo>();
            foreach (var stream in streams.EnumerateArray())
            {
                var index = Integer(stream, "index") ?? throw new JsonException("Stream index is missing.");
                if (index < 0 || !ids.Add(index)) throw new JsonException("Stream indices must be unique and nonnegative.");
                var kind = Text(stream, "codec_type") ?? throw new JsonException("Stream kind is missing.");
                var attached = false;
                if (stream.TryGetProperty("disposition", out var disposition))
                {
                    var flag = Integer(disposition, "attached_pic") ?? 0;
                    if (flag is not (0 or 1)) throw new JsonException("Invalid attached-picture flag.");
                    attached = flag == 1;
                }
                parsed.Add(new(index, kind, Text(stream, "codec_name"), attached,
                    PositiveInteger(stream, "sample_rate"), PositiveInteger(stream, "channels"),
                    PositiveInteger(stream, "width"), PositiveInteger(stream, "height"),
                    Duration(stream), Tags(stream)));
            }
            return new(container, Duration(format), parsed.ToImmutable(), Tags(format));
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            throw new MediaAcquisitionException(AcquisitionError.InvalidProbeData, "Probe returned an invalid metadata document.", e);
        }
    }

    private static string? Text(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind != JsonValueKind.String) throw new JsonException("Expected text field.");
        return value.GetString();
    }

    private static int? Integer(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out number)) return number;
        throw new JsonException("Expected integer field.");
    }

    private static int? PositiveInteger(JsonElement parent, string name)
    {
        var value = Integer(parent, name);
        if (value <= 0) throw new JsonException("Stream dimensions/rate/channels must be positive.");
        return value;
    }

    private static double? Duration(JsonElement parent)
    {
        if (!parent.TryGetProperty("duration", out var value)) return null;
        if (value.ValueKind == JsonValueKind.String && value.GetString() == "N/A") return null;
        double number;
        if (value.ValueKind == JsonValueKind.Number) number = value.GetDouble();
        else if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(),
            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number)) { }
        else throw new JsonException("Expected decimal duration.");
        if (!double.IsFinite(number) || number < 0) throw new JsonException("Invalid duration.");
        return number;
    }

    private static ImmutableArray<MediaTag> Tags(JsonElement parent)
    {
        if (!parent.TryGetProperty("tags", out var tags)) return [];
        if (tags.ValueKind != JsonValueKind.Object || tags.EnumerateObject().Count() > 1024)
            throw new JsonException("Invalid tags.");
        return tags.EnumerateObject().Select(t => new MediaTag(t.Name,
            t.Value.ValueKind == JsonValueKind.String ? t.Value.GetString()! : throw new JsonException("Expected tag text."))).ToImmutableArray();
    }

    private static void UniqueProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new JsonException("Duplicate JSON property.");
                UniqueProperties(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var child in value.EnumerateArray()) UniqueProperties(child);
    }
}
