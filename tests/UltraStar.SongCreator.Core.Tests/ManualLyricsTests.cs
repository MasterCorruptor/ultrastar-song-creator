using UltraStar.SongCreator.Acquisition.Lyrics;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class ManualLyricsTests
{
    [Fact]
    public void ManualNormalizesOnlyLineEndingsAndPreservesUnicodeSpacesRepeatsAndTrailingLines()
    {
        var result = ManualLyrics.Create("  Egen sang\r\nHei æøå 🎵  \rHei æøå 🎵  \r\n\r\n");
        Assert.Equal("  Egen sang\nHei æøå 🎵  \nHei æøå 🎵  \n\n", result.Document!.Text);
        Assert.Equal("manual", result.Candidate.ProviderId);
        Assert.Null(result.Candidate.Artist); Assert.Null(result.Candidate.Title); Assert.Null(result.Candidate.SourceUrl);
        Assert.Null(result.Candidate.DurationSeconds); Assert.Equal(new LyricsMatchEvidence(), result.Candidate.Match);
        Assert.Equal(LyricsSyncLevel.Plain, result.Document.SyncLevel); Assert.Empty(result.Document.Lines);
        Assert.Same(result.Document, Assert.Single(result.Candidate.Documents));
        Assert.NotEqual(ManualLyrics.Create("Egen tekst").Candidate.Id, result.Candidate.Id);
    }
    [Fact]
    public void CrLfHeavyInputIsBoundedAfterNormalizationAndKeepsTrailingLine()
    {
        var input = string.Concat(Enumerable.Repeat("a\r\n", 100000));
        Assert.Equal(string.Concat(Enumerable.Repeat("a\n", 100000)), ManualLyrics.Create(input).Document!.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  \r\n ")]
    public void EmptyOrMalformedUnicodeCannotBecomeSelectedLyrics(string text) =>
        Assert.Throws<LyricsValidationException>(() => ManualLyrics.Create(text));

    [Fact]
    public void ExactDocumentBoundaryCountsUtf8AndStructuralBytesWithoutTruncation()
    {
        var text = new string('a', LyricsValidation.MaxDocumentBytes - 69); // document64 + id plain5
        Assert.Equal(text, ManualLyrics.Create(text).Document!.Text);
        Assert.Equal(LyricsValidationError.ResponseTooLarge,
            Assert.Throws<LyricsValidationException>(() => ManualLyrics.Create(text + "a")).Error);
        var unicode = new string('é', (LyricsValidation.MaxDocumentBytes - 69) / 2);
        Assert.Equal(unicode, ManualLyrics.Create(unicode).Document!.Text);
        Assert.Throws<LyricsValidationException>(() => ManualLyrics.Create(unicode + "é"));
    }
    [Fact]
    public void ValidSurrogatePairsRemainIntactAndUnpairedSurrogatesAreRejected()
    {
        Assert.Equal("🎵", ManualLyrics.Create("🎵").Document!.Text);
        // Construct invalid surrogates inside the test: xUnit argument serialization replaces malformed Unicode.
        foreach (var code in new[] { 0xd800, 0xdc00 })
        {
            var invalid = new string((char)code, 1);
            Assert.Throws<LyricsValidationException>(() => ManualLyrics.Create(invalid));
            Assert.Throws<LyricsValidationException>(() => LyricsValidation.NormalizeText("a" + invalid + "b"));
        }
    }
}
