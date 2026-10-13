using System.Collections.Immutable;
using System.Text;

namespace UltraStar.SongCreator.Acquisition.Lyrics;

public static class LyricsValidation
{
    public const int MaxQueryLength = 4096, MaxIdLength = 256, MaxUrlLength = 4096;
    public const int MaxCandidates = 25, MaxVariants = 3, MaxUnsupportedFormats = 16;
    public const int MaxDocumentBytes = 256 * 1024, MaxPageBytes = 4 * 1024 * 1024;
    public const int MaxLines = 10000, MaxWords = 50000;
    public const double MaxTimeSeconds = 14400;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    private static void Require(bool condition, LyricsValidationError error, string message)
    {
        if (!condition) throw new LyricsValidationException(error, message);
    }
    private static int Bytes(string? value, int maxLength, bool required, LyricsValidationError error)
    {
        Require(value is not null || !required, error, "Required text is null.");
        if (value is null) return 0;
        Require(value.Length <= maxLength && (!required || !string.IsNullOrWhiteSpace(value)), error, "Text length/content is invalid.");
        try { return Utf8.GetByteCount(value); }
        catch (EncoderFallbackException) { throw new LyricsValidationException(error, "Invalid UTF-16 surrogate sequence."); }
    }
    public static string NormalizeText(string text)
    {
        // CRLF input can be twice the size of its valid normalized LF document.
        Bytes(text, MaxDocumentBytes * 2, false, LyricsValidationError.InvalidDocument);
        ArgumentNullException.ThrowIfNull(text);
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        Require(Utf8.GetByteCount(normalized) <= MaxDocumentBytes, LyricsValidationError.ResponseTooLarge, "Text exceeds the UTF-8 document budget.");
        return normalized;
    }
    private static bool Time(double value) => double.IsFinite(value) && value >= 0 && value <= MaxTimeSeconds;
    private static void Duration(double? value, LyricsValidationError error) =>
        Require(value is null || (Time(value.Value) && value > 0), error, "Duration must be finite positive source seconds.");
    private static void Timestamp(DateTimeOffset value, LyricsValidationError error) =>
        Require(value != default && value.Offset == TimeSpan.Zero, error, "Timestamp must be a non-default UTC instant.");
    private static void Budget(long value, int maximum) =>
        Require(value <= maximum, LyricsValidationError.ResponseTooLarge, "Normalized payload budget exceeded.");
    private static int Text(string? value)
    {
        var bytes = Bytes(value, MaxDocumentBytes, false, LyricsValidationError.InvalidDocument);
        Require(value is not null && !value.Contains('\r'), LyricsValidationError.InvalidDocument, "Document text must be normalized LF.");
        return bytes;
    }
    public static void Query(LyricsQuery query) => QueryBytes(query);
    private static long QueryBytes(LyricsQuery query)
    {
        Require(query is not null, LyricsValidationError.InvalidQuery, "Query is null.");
        var size = 128L + Bytes(query!.Artist, MaxQueryLength, true, LyricsValidationError.InvalidQuery)
            + Bytes(query.Title, MaxQueryLength, true, LyricsValidationError.InvalidQuery)
            + Bytes(query.VersionHint, MaxQueryLength, false, LyricsValidationError.InvalidQuery)
            + Bytes(query.RecordingId, MaxIdLength, false, LyricsValidationError.InvalidQuery);
        Require(query.Limit is >= 1 and <= MaxCandidates, LyricsValidationError.InvalidQuery, "Invalid result limit.");
        Require(query.RecordingId is null || !string.IsNullOrWhiteSpace(query.RecordingId), LyricsValidationError.InvalidQuery, "Optional recording identity is blank.");
        Duration(query.DurationSeconds, LyricsValidationError.InvalidQuery); return size;
    }
    public static void Document(LyricsDocument document) => DocumentBytes(document);
    private static long DocumentBytes(LyricsDocument document)
    {
        const LyricsValidationError error = LyricsValidationError.InvalidDocument;
        Require(document is not null, error, "Document is null.");
        var size = 64L + Bytes(document!.Id, MaxIdLength, true, error) + Text(document.Text);
        Require(!string.IsNullOrWhiteSpace(document.Text), error, "Lyrics document has no text.");
        Require(Enum.IsDefined(document.SyncLevel) && Enum.IsDefined(document.TimeReference), error, "Unknown timing/sync level.");
        Require(!document.Lines.IsDefault && document.Lines.Length <= MaxLines, error, "Invalid line collection.");
        if (document.SyncLevel == LyricsSyncLevel.Plain) Require(document.Lines.IsEmpty, error, "Plain text must have no invented timing.");
        else Require(!document.Lines.IsEmpty, error, "Timed document requires lines.");
        var previousLine = 0d; var wordCount = 0;
        foreach (var line in document.Lines)
        {
            Require(line is not null, error, "Null line.");
            Require(Time(line!.StartSeconds) && line.StartSeconds >= previousLine &&
                (line.EndSeconds is null || (Time(line.EndSeconds.Value) && line.EndSeconds >= line.StartSeconds)), error, "Invalid line times/order.");
            previousLine = line.StartSeconds; size += 48L + Text(line.Text);
            Require(!line.Words.IsDefault && line.Words.Length <= MaxWords, error, "Invalid word collection.");
            wordCount += line.Words.Length; Require(wordCount <= MaxWords, error, "Too many words.");
            if (document.SyncLevel != LyricsSyncLevel.Word) Require(line.Words.IsEmpty, error, "Words require validated Word level.");
            var previousWord = line.StartSeconds;
            foreach (var word in line.Words)
            {
                Require(word is not null, error, "Null word.");
                Require(Time(word!.StartSeconds) && word.StartSeconds >= previousWord &&
                    (word.EndSeconds is null || (Time(word.EndSeconds.Value) && word.EndSeconds >= word.StartSeconds)) &&
                    (line.EndSeconds is null || (word.StartSeconds <= line.EndSeconds &&
                        (word.EndSeconds is null || word.EndSeconds <= line.EndSeconds))), error, "Invalid word membership/times/order.");
                Require(!string.IsNullOrWhiteSpace(word.Text), error, "Timed word has no text.");
                previousWord = word.StartSeconds; size += 32L + Text(word.Text);
                Budget(size, MaxDocumentBytes);
            }
            Budget(size, MaxDocumentBytes);
        }
        if (document.SyncLevel == LyricsSyncLevel.Word) Require(wordCount > 0, error, "Word level requires timed words.");
        Budget(size, MaxDocumentBytes); return size;
    }
    public static void Candidate(LyricsCandidate candidate) => CandidateBytes(candidate);
    private static long CandidateBytes(LyricsCandidate candidate)
    {
        const LyricsValidationError error = LyricsValidationError.InvalidCandidate;
        Require(candidate is not null, error, "Candidate is null.");
        var size = 128L + Bytes(candidate!.ProviderId, MaxIdLength, true, error) + Bytes(candidate.Id, MaxIdLength, true, error)
            + Bytes(candidate.Artist, MaxQueryLength, false, error) + Bytes(candidate.Title, MaxQueryLength, false, error)
            + Bytes(candidate.Album, MaxQueryLength, false, error) + Bytes(candidate.Version, MaxQueryLength, false, error)
            + Bytes(candidate.SourceUrl, MaxUrlLength, false, error);
        Require(candidate.SourceUrl is null || (Uri.TryCreate(candidate.SourceUrl, UriKind.Absolute, out var uri) &&
            uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Fragment)), error, "Source URL must be canonical HTTPS metadata.");
        Duration(candidate.DurationSeconds, error); Timestamp(candidate.RetrievedAtUtc, error);
        Require(Enum.IsDefined(candidate.TextStatus) && !candidate.Documents.IsDefault && candidate.Documents.Length <= MaxVariants, error, "Invalid status/documents.");
        Require(candidate.TextStatus == LyricsTextStatus.Available ? !candidate.Documents.IsEmpty : candidate.Documents.IsEmpty, error, "Text status contradicts validated documents.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var document in candidate.Documents)
        {
            size += DocumentBytes(document); Require(ids.Add(document.Id), error, "Duplicate variant identity."); Budget(size, MaxPageBytes);
        }
        Require(candidate.Capabilities is not null && !candidate.Capabilities.UnsupportedFormats.IsDefault &&
            candidate.Capabilities.UnsupportedFormats.Length <= MaxUnsupportedFormats, error, "Invalid provider capability collection.");
        size += 64;
        foreach (var format in candidate.Capabilities!.UnsupportedFormats) size += Bytes(format, MaxIdLength, true, error);
        Require(candidate.TextStatus != LyricsTextStatus.Unsupported || !candidate.Capabilities.UnsupportedFormats.IsEmpty, error, "Unsupported status requires a reported format.");
        var match = candidate.Match;
        Require(match is not null && Enum.IsDefined(match.Artist) && Enum.IsDefined(match.Title) &&
            Enum.IsDefined(match.Version) && Enum.IsDefined(match.Text) &&
            (match.DurationDeltaSeconds is null || (double.IsFinite(match.DurationDeltaSeconds.Value) && Math.Abs(match.DurationDeltaSeconds.Value) <= MaxTimeSeconds)), error, "Invalid match indication/duration delta.");
        Budget(size, MaxPageBytes); return size;
    }
    public static void Page(LyricsPage page)
    {
        const LyricsValidationError error = LyricsValidationError.InvalidPage;
        Require(page is not null, error, "Page is null.");
        var size = QueryBytes(page!.Query) + Bytes(page.ProviderId, MaxIdLength, true, error);
        Timestamp(page.RetrievedAtUtc, error);
        Require(!page.Candidates.IsDefault && page.Candidates.Length <= page.Query.Limit, error, "Invalid candidate collection/limit.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in page.Candidates)
        {
            size += CandidateBytes(candidate); Budget(size, MaxPageBytes);
            Require(candidate.ProviderId == page.ProviderId, error, "Page/candidate provider mismatch.");
            Require(ids.Add(candidate.Id), LyricsValidationError.Ambiguous, "Duplicate candidate identity.");
        }
    }
}
