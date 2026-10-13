using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace UltraStar.SongCreator.Acquisition.Lyrics;

/// <summary>Bounded simple line-LRC. No trimming, inferred ends, instrumental heuristics, or file/network access.</summary>
public static class LrcParser
{
    public const int MaxInputBytes = 256 * 1024, MaxPhysicalLines = 20000, MaxMetadata = 128;
    public const int MaxMetadataValueBytes = 4096, MaxDiagnostics = 64, MaxResultBytes = 4 * 1024 * 1024;
    private const string DocumentId = "line-lrc";
    private static readonly UTF8Encoding Utf8 = new(false, true);
    public static LrcParseResult Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > MaxInputBytes * 2) return InitialFailure(LrcParseStatus.TooLarge, LrcDiagnosticCode.LimitExceeded);
        string raw;
        try { raw = LyricsValidation.NormalizeText(text); }
        catch (LyricsValidationException e)
        {
            return e.Error == LyricsValidationError.ResponseTooLarge
                ? InitialFailure(LrcParseStatus.TooLarge, LrcDiagnosticCode.LimitExceeded)
                : InitialFailure(LrcParseStatus.MalformedInput, LrcDiagnosticCode.InvalidUnicode);
        }
        var metadata = new List<LrcMetadata>();
        var diagnostics = new List<LrcDiagnostic>();
        long resultBytes = Utf8.GetByteCount(raw) + 64L;
        LrcParseResult Fail(LrcParseStatus status, LrcDiagnosticCode code, int? line)
        {
            diagnostics.Add(new(code, line));
            return new(status, null, raw, metadata.ToImmutableArray(), diagnostics.ToImmutableArray());
        }
        if (raw.Count(c => c == '\n') + 1 > MaxPhysicalLines)
            return Fail(LrcParseStatus.TooLarge, LrcDiagnosticCode.LimitExceeded, null);
        var projected = new List<string>();
        var timed = new List<LyricsLine>();
        long documentBytes = 64 + DocumentId.Length;
        var offsetSeen = false;
        int? unsupportedOffsetLine = null;
        var hasLyrics = false;
        var physical = raw.Split('\n');
        for (var number = 1; number <= physical.Length; number++)
        {
            var line = physical[number - 1];
            var position = 0;
            var times = new List<double>();
            while (position < line.Length && line[position] == '[' && LooksNumeric(line.AsSpan(position + 1)))
            {
                var close = line.IndexOf(']', position + 1);
                if (close < 0) return Fail(LrcParseStatus.MalformedInput, LrcDiagnosticCode.MalformedTimestamp, number);
                if (!Timestamp(line.AsSpan(position + 1, close - position - 1), out var time, out var status))
                    return Fail(status, status == LrcParseStatus.TimingOutOfRange ? LrcDiagnosticCode.TimeOutOfBounds
                        : status == LrcParseStatus.UnsupportedFormat ? LrcDiagnosticCode.UnsupportedSyntax : LrcDiagnosticCode.MalformedTimestamp, number);
                if (timed.Count + times.Count >= LyricsValidation.MaxLines)
                    return Fail(LrcParseStatus.TooLarge, LrcDiagnosticCode.LimitExceeded, number);
                times.Add(time); position = close + 1;
            }
            if (times.Count == 0 && !string.IsNullOrWhiteSpace(line))
            {
                if (!line.StartsWith('[')) return Fail(LrcParseStatus.UnsupportedFormat, LrcDiagnosticCode.UnsupportedSyntax, number);
                var close = line.IndexOf(']');
                var colon = line.IndexOf(':');
                if (close < 0 || colon <= 1 || colon > close || !string.IsNullOrWhiteSpace(line[(close + 1)..]))
                    return Fail(LrcParseStatus.UnsupportedFormat, LrcDiagnosticCode.UnsupportedSyntax, number);
                var name = line[1..colon]; var value = line[(colon + 1)..close];
                if (name is not ("ar" or "ti" or "al" or "by" or "re" or "ve" or "au" or "length" or "offset"))
                    return Fail(LrcParseStatus.UnsupportedFormat, LrcDiagnosticCode.UnsupportedSyntax, number);
                if (metadata.Count >= MaxMetadata || Utf8.GetByteCount(value) > MaxMetadataValueBytes)
                    return Fail(LrcParseStatus.TooLarge, LrcDiagnosticCode.LimitExceeded, number);
                if (name == "offset")
                {
                    if (offsetSeen) return Fail(LrcParseStatus.MalformedInput, LrcDiagnosticCode.DuplicateOffset, number);
                    offsetSeen = true;
                    var start = value.Length > 0 && value[0] is '+' or '-' ? 1 : 0;
                    if (value.Length <= start || !AllDigits(value.AsSpan(start)) ||
                        !int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var offset))
                        return Fail(LrcParseStatus.MalformedInput, LrcDiagnosticCode.InvalidOffset, number);
                    metadata.Add(new(name, value, number)); resultBytes += 32 + Utf8.GetByteCount(name) + Utf8.GetByteCount(value);
                    if (offset != 0) unsupportedOffsetLine = number;
                    else { diagnostics.Add(new(LrcDiagnosticCode.ExplicitZeroOffset, number)); resultBytes += 32; }
                }
                else { metadata.Add(new(name, value, number)); resultBytes += 32 + Utf8.GetByteCount(name) + Utf8.GetByteCount(value); }
                if (resultBytes > MaxResultBytes || diagnostics.Count >= MaxDiagnostics)
                    return Fail(LrcParseStatus.TooLarge, LrcDiagnosticCode.LimitExceeded, number);
                continue;
            }
            var suffix = line[position..];
            if (times.Count > 0 && Enhanced(suffix))
                return Fail(LrcParseStatus.UnsupportedFormat, LrcDiagnosticCode.UnsupportedSyntax, number);
            var bytes = Utf8.GetByteCount(suffix);
            documentBytes += bytes + (projected.Count > 0 ? 1 : 0) + times.Count * (48L + bytes);
            // Reserve every expansion before allocating its LyricsLine record.
            if (documentBytes > LyricsValidation.MaxDocumentBytes || resultBytes + documentBytes > MaxResultBytes)
                return Fail(LrcParseStatus.TooLarge, LrcDiagnosticCode.LimitExceeded, number);
            projected.Add(suffix);
            if (times.Count > 0 && !string.IsNullOrWhiteSpace(suffix)) hasLyrics = true;
            foreach (var time in times) timed.Add(new(suffix, time, null, []));
        }
        if (unsupportedOffsetLine.HasValue)
            return Fail(LrcParseStatus.UnsupportedFormat, LrcDiagnosticCode.UnsupportedOffset, unsupportedOffsetLine);
        if (!hasLyrics) return Fail(LrcParseStatus.NoLyrics, LrcDiagnosticCode.NoLyrics, null);
        var document = new LyricsDocument(DocumentId, string.Join("\n", projected), LyricsSyncLevel.Line,
            timed.OrderBy(l => l.StartSeconds).ToImmutableArray());
        LyricsValidation.Document(document);
        return new(LrcParseStatus.Success, document, raw, metadata.ToImmutableArray(), diagnostics.ToImmutableArray());
    }
    private static LrcParseResult InitialFailure(LrcParseStatus status, LrcDiagnosticCode code) =>
        new(status, null, null, [], [new(code, null)]);
    private static bool AsciiDigit(char value) => value is >= '0' and <= '9';
    private static bool AllDigits(ReadOnlySpan<char> value)
    {
        foreach (var c in value) if (!AsciiDigit(c)) return false;
        return true;
    }
    private static bool LooksNumeric(ReadOnlySpan<char> token)
    {
        if (token.IsEmpty) return false;
        var close = token.IndexOf(']');
        var contents = close < 0 ? token : token[..close];
        return contents.IndexOf(':') >= 0 &&
            (AsciiDigit(token[0]) || token[0] is '+' or '-' ||
                token.StartsWith("NaN", StringComparison.Ordinal) || token.StartsWith("Infinity", StringComparison.Ordinal));
    }
    private static bool Enhanced(string text)
    {
        for (var i = 0; i < text.Length; i++)
            if (text[i] == '<')
            {
                var next = i + 1;
                while (next < text.Length && AsciiDigit(text[next])) next++;
                if (next > i + 1 && next < text.Length && text[next] == ':') return true;
            }
        return false;
    }
    private static bool Timestamp(ReadOnlySpan<char> token, out double time, out LrcParseStatus status)
    {
        time = 0; status = LrcParseStatus.MalformedInput;
        if (token.Contains('h') || token.Contains('H')) { status = LrcParseStatus.UnsupportedFormat; return false; }
        var position = 0; var minutes = 0;
        try
        {
            while (position < token.Length && AsciiDigit(token[position]))
            {
                minutes = checked(minutes * 10 + token[position++] - '0');
            }
        }
        catch (OverflowException) { status = LrcParseStatus.TimingOutOfRange; return false; }
        if (position == 0 || position >= token.Length || token[position++] != ':') return false;
        var beginSeconds = position; var seconds = 0;
        while (position < token.Length && AsciiDigit(token[position]))
        {
            if (position - beginSeconds >= 2) return false;
            seconds = seconds * 10 + token[position++] - '0';
        }
        if (position == beginSeconds || seconds > 59) return false;
        var fraction = 0; var digits = 0;
        if (position < token.Length)
        {
            if (token[position++] is not ('.' or ':')) return false;
            while (position < token.Length && AsciiDigit(token[position]))
            {
                if (++digits > 3) return false;
                fraction = fraction * 10 + token[position++] - '0';
            }
            if (digits == 0) return false;
            if (position != token.Length)
            {
                if (token[position] is ':' or '.') status = LrcParseStatus.UnsupportedFormat;
                return false;
            }
        }
        if (minutes > 240) { status = LrcParseStatus.TimingOutOfRange; return false; }
        time = checked(minutes * 60 + seconds) + fraction / (digits switch { 1 => 10d, 2 => 100d, 3 => 1000d, _ => 1d });
        if (time > LyricsValidation.MaxTimeSeconds) { status = LrcParseStatus.TimingOutOfRange; return false; }
        return true;
    }
}
