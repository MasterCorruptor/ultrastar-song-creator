using System.Text;

namespace UltraStar.SongCreator.Core;

public static class SelectedLyricsValidation
{
    public const int MaxIdLength = 256, MaxMetadataLength = 4096, MaxUrlLength = 4096;
    public const int MaxDocumentBytes = 256 * 1024, MaxSelectedBytes = 512 * 1024, MaxLines = 10000, MaxWords = 50000;
    public const double MaxTimeSeconds = 14400;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static void Require(bool condition, SelectedLyricsValidationError error, string message)
    {
        if (!condition) throw new SelectedLyricsValidationException(error, message);
    }
    private static int Bytes(string? text, int maxLength, bool required, SelectedLyricsValidationError error)
    {
        Require(text is not null || !required, error, "Required text is null.");
        if (text is null) return 0;
        Require(text.Length <= maxLength && (!required || !string.IsNullOrWhiteSpace(text)), error, "Invalid text length/content.");
        try { return Utf8.GetByteCount(text); }
        catch (EncoderFallbackException) { throw new SelectedLyricsValidationException(error, "Invalid UTF-16 surrogate sequence."); }
    }
    private static int Text(string? text)
    {
        var bytes = Bytes(text, MaxDocumentBytes, false, SelectedLyricsValidationError.InvalidDocument);
        Require(text is not null && !text.Contains('\r'), SelectedLyricsValidationError.InvalidDocument, "Document must contain normalized LF text.");
        return bytes;
    }
    private static bool Time(double value) => double.IsFinite(value) && value >= 0 && value <= MaxTimeSeconds;
    private static bool Instant(DateTimeOffset? value) => value.HasValue && value.Value != default && value.Value.Offset == TimeSpan.Zero;
    private static void Budget(long size, int maximum) =>
        Require(size <= maximum, SelectedLyricsValidationError.ResponseTooLarge, "Logical payload budget exceeded.");
    public static void Validate(SelectedLyrics? selected)
    {
        if (selected is null) return;
        Require(Enum.IsDefined(selected.Kind), SelectedLyricsValidationError.InvalidSelection, "Unknown selected kind.");
        Require(selected.Kind == SelectedLyricsKind.Text ? selected.Document is not null : selected.Document is null,
            SelectedLyricsValidationError.InvalidSelection, "Text requires one document; instrumental has none.");
        var size = 128L + (selected.Document is null ? 0 : DocumentBytes(selected.Document));
        var source = selected.Provenance;
        const SelectedLyricsValidationError error = SelectedLyricsValidationError.InvalidProvenance;
        Require(source is not null, error, "Provenance is null.");
        size += Bytes(source!.SourceId, MaxIdLength, true, error) + Bytes(source.CandidateId, MaxIdLength, true, error)
            + Bytes(source.SourceUrl, MaxUrlLength, false, error) + Bytes(source.Artist, MaxMetadataLength, false, error)
            + Bytes(source.Title, MaxMetadataLength, false, error) + Bytes(source.Album, MaxMetadataLength, false, error)
            + Bytes(source.Version, MaxMetadataLength, false, error);
        Require(Enum.IsDefined(source.Origin), error, "Unknown origin.");
        if (source.Origin == SelectedLyricsOrigin.Manual)
        {
            Require(source.SourceId == "manual" && Guid.TryParseExact(source.CandidateId, "D", out var id) &&
                id != Guid.Empty && source.CandidateId == id.ToString("D") && source.SourceUrl is null &&
                Instant(source.CreatedAtUtc) && source.RetrievedAtUtc is null, error, "Invalid manual identity/date/source semantics.");
        }
        else
        {
            Require(source.SourceId != "manual" && source.CreatedAtUtc is null && Instant(source.RetrievedAtUtc), error, "Invalid provider identity/date semantics.");
            Require(source.SourceUrl is null || (Uri.TryCreate(source.SourceUrl, UriKind.Absolute, out var uri) &&
                uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Fragment)), error, "Source URL must be canonical HTTPS data.");
        }
        Require(source.DurationSeconds is null || (Time(source.DurationSeconds.Value) && source.DurationSeconds > 0), error, "Invalid source duration.");
        Budget(size, MaxSelectedBytes);
    }
    public static void ValidateDocument(SelectedLyricsDocument document) => DocumentBytes(document);
    private static long DocumentBytes(SelectedLyricsDocument document)
    {
        const SelectedLyricsValidationError error = SelectedLyricsValidationError.InvalidDocument;
        Require(document is not null, error, "Document is null.");
        var size = 64L + Bytes(document!.Id, MaxIdLength, true, error) + Text(document.Text);
        Require(!string.IsNullOrWhiteSpace(document.Text), error, "Lyrics document has no text.");
        Require(Enum.IsDefined(document.SyncLevel) && Enum.IsDefined(document.TimeReference), error, "Unknown timing/sync level.");
        Require(!document.Lines.IsDefault && document.Lines.Length <= MaxLines, error, "Invalid line collection.");
        if (document.SyncLevel == SelectedLyricsSyncLevel.Plain) Require(document.Lines.IsEmpty, error, "Plain text must have no invented timing.");
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
            if (document.SyncLevel != SelectedLyricsSyncLevel.Word) Require(line.Words.IsEmpty, error, "Words require validated Word level.");
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
        if (document.SyncLevel == SelectedLyricsSyncLevel.Word) Require(wordCount > 0, error, "Word level requires timed words.");
        Budget(size, MaxDocumentBytes); return size;
    }
}
