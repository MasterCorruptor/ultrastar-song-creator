namespace UltraStar.SongCreator.Acquisition.Lyrics;

public static class LyricsSelections
{
    public static LyricsSelection Select(LyricsPage page, string providerId, string candidateId, string? documentId)
    {
        LyricsValidation.Page(page);
        if (providerId != page.ProviderId)
            throw new LyricsValidationException(LyricsValidationError.InvalidSelection, "Selected provider does not match the page.");
        var candidate = page.Candidates.SingleOrDefault(c => c.Id == candidateId)
            ?? throw new LyricsValidationException(LyricsValidationError.NotFound, "Select a candidate identity present in this page.");
        if (candidate.TextStatus == LyricsTextStatus.Instrumental && documentId is null) return new(candidate, null);
        if (candidate.TextStatus != LyricsTextStatus.Available || documentId is null)
            throw new LyricsValidationException(LyricsValidationError.InvalidSelection, "Select a validated document, or explicitly choose instrumental.");
        var document = candidate.Documents.SingleOrDefault(d => d.Id == documentId)
            ?? throw new LyricsValidationException(LyricsValidationError.NotFound, "Select an existing document variant.");
        return new(candidate, document);
    }
}
