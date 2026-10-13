using System.Collections.Immutable;

namespace UltraStar.SongCreator.Acquisition.Lyrics;

public static class ManualLyrics
{
    /// <summary>Explicit user text becomes a selected plain document, without invented search metadata or network access.</summary>
    public static LyricsSelection Create(string text)
    {
        var document = new LyricsDocument("plain", LyricsValidation.NormalizeText(text), LyricsSyncLevel.Plain, []);
        var candidate = new LyricsCandidate
        {
            ProviderId = "manual",
            Id = Guid.NewGuid().ToString("D"),
            RetrievedAtUtc = DateTimeOffset.UtcNow,
            TextStatus = LyricsTextStatus.Available,
            Documents = ImmutableArray.Create(document)
        };
        LyricsValidation.Candidate(candidate);
        return new(candidate, document);
    }
}
