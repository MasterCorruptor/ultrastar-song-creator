using System.Collections.Immutable;
using UltraStar.SongCreator.Acquisition.Lyrics;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class LyricsContractTests
{
    private static readonly DateTimeOffset Captured = new(2026, 10, 13, 0, 0, 0, TimeSpan.Zero);
    private static LyricsDocument Plain(string id = "plain", string text = "Egen tekst") => new(id, text, LyricsSyncLevel.Plain, []);
    private static LyricsCandidate Candidate(string id = "one") => new()
    {
        ProviderId = "fixture",
        Id = id,
        Artist = "Egen artist",
        Title = "Egen tittel",
        SourceUrl = "https://example.test/lyrics/" + id,
        RetrievedAtUtc = Captured,
        TextStatus = LyricsTextStatus.Available,
        Documents = [Plain()]
    };
    private static LyricsPage Page(params LyricsCandidate[] candidates) =>
        new("fixture", new("Egen artist", "Egen tittel"), candidates.ToImmutableArray(), Captured);
    private static LyricsDocument WordDocument() => new("word", "Hei hei\nhei", LyricsSyncLevel.Word,
        [new("Hei hei", 1, null, [new("Hei ", 1, 4), new("hei", 1, null)]),
         new("hei", 2, 5, [new("hei", 2, 5)])]);

    [Fact]
    public void TimedHierarchyRetainsSimultaneousOverlapAndUnknownEndsWithoutNextLineInference()
    {
        var document = WordDocument();
        LyricsValidation.Document(document);
        Assert.Null(document.Lines[0].EndSeconds);
        Assert.Equal(4, document.Lines[0].Words[0].EndSeconds); // beyond next line start is allowed
        Assert.Null(document.Lines[0].Words[1].EndSeconds);
        LyricsValidation.Document(new("line", "Hei\nhei", LyricsSyncLevel.Line,
            [new("Hei", 1, 4, []), new("hei", 1, null, [])]));
        Assert.Equal(LyricsTimeReference.LyricsSourceSeconds, document.TimeReference);
    }
    public static IEnumerable<object[]> InvalidDocuments()
    {
        var d = WordDocument();
        var line = d.Lines[0];
        yield return [d with { SyncLevel = (LyricsSyncLevel)99 }];
        yield return [d with { TimeReference = (LyricsTimeReference)99 }];
        yield return [d with { Lines = default }];
        yield return [d with { Lines = [] }];
        yield return [d with { Text = "" }];
        yield return [d with { Text = "Hei\r\nhei" }];
        yield return [d with { Lines = [null!] }];
        yield return [d with { Lines = [line with { Words = default }] }];
        yield return [d with { Lines = [line with { Words = [null!] }] }];
        yield return [d with { Lines = [line with { Words = [] }] }];
        yield return [d with { Lines = [line with { StartSeconds = double.NaN }] }];
        yield return [d with { Lines = [line with { StartSeconds = -1 }] }];
        yield return [d with { Lines = [line with { EndSeconds = .5 }] }];
        yield return [d with { Lines = [line with { EndSeconds = double.PositiveInfinity }] }];
        yield return [d with { Lines = [line with { StartSeconds = 14401 }] }];
        yield return [d with { Lines = [d.Lines[1], line] }];
        yield return [d with { Lines = [line with { Words = [new("Hei", .5, 2)] }] }];
        yield return [d with { Lines = [line with { Words = [new("Hei", 2, 1)] }] }];
        yield return [d with { Lines = [line with { Words = [new("Hei", 2), new("hei", 1)] }] }];
        yield return [d with { Lines = [line with { EndSeconds = 3, Words = [new("Hei", 2, 4)] }] }];
        yield return [d with { Lines = [line with { EndSeconds = 3, Words = [new("Hei", 4)] }] }];
        yield return [d with { Lines = [line with { Words = [new("Hei", double.NaN)] }] }];
        yield return [d with { Lines = [line with { Words = [new("Hei", 1, 14401)] }] }];
        yield return [d with { Lines = [line with { Words = [new("", 1)] }] }];
        yield return [Plain() with { Lines = [new("Hei", 0, null, [])] }];
        yield return [d with { SyncLevel = LyricsSyncLevel.Line }];
    }
    [Theory, MemberData(nameof(InvalidDocuments))]
    public void InvalidTimingHierarchyAndUnknownLevelsAreRejected(LyricsDocument document) =>
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.Document(document));

    [Fact]
    public void InclusiveGlobalTimeBoundsDoNotClipToMetadataDuration()
    {
        var d = new LyricsDocument("line", "Egen slutt", LyricsSyncLevel.Line, [new("Egen slutt", 14400, 14400, [])]);
        LyricsValidation.Candidate(Candidate() with { DurationSeconds = 1, Documents = [d] });
        LyricsValidation.Document(WordDocument() with { Lines = [new("Hei", 0, null, [new("Hei", 14400, null)])] });
    }
    [Fact]
    public void StatusAndProviderCapabilitiesAreSeparateFromLocallyValidatedSync()
    {
        var available = Candidate() with { Capabilities = new(true, true) { UnsupportedFormats = ["yaml-word"] } };
        var instrumental = Candidate("instrumental") with { TextStatus = LyricsTextStatus.Instrumental, Documents = [] };
        var missing = Candidate("missing") with { TextStatus = LyricsTextStatus.Missing, Documents = [] };
        var unsupported = Candidate("unsupported") with
        { TextStatus = LyricsTextStatus.Unsupported, Documents = [], Capabilities = new(true) { UnsupportedFormats = ["yaml-word"] } };
        var page = Page(available, instrumental, missing, unsupported);
        LyricsValidation.Page(page);
        Assert.Equal(LyricsSyncLevel.Plain, LyricsSelections.Select(page, "fixture", "one", "plain").Document!.SyncLevel);
        Assert.Null(LyricsSelections.Select(page, "fixture", "instrumental", null).Document);
        Assert.Throws<LyricsValidationException>(() => LyricsSelections.Select(page, "fixture", "instrumental", "plain"));
        Assert.Throws<LyricsValidationException>(() => LyricsSelections.Select(page, "fixture", "missing", null));
        Assert.Throws<LyricsValidationException>(() => LyricsSelections.Select(page, "fixture", "unsupported", null));
        Assert.Empty(Page().Candidates); LyricsValidation.Page(Page());
    }
    [Fact]
    public void SelectionRequiresExplicitPresentVariantAndPreservesImmutableProvenance()
    {
        var candidate = Candidate() with { Documents = [Plain(), WordDocument()], Match = new(Title: LyricsMatchIndication.Different, DurationDeltaSeconds: -2) };
        var page = Page(Candidate("other"), candidate);
        var selected = LyricsSelections.Select(page, "fixture", "one", "word");
        Assert.Same(candidate, selected.Candidate); Assert.Equal("word", selected.Document!.Id);
        Assert.Equal(Captured, selected.Candidate.RetrievedAtUtc); Assert.Equal(candidate.SourceUrl, selected.Candidate.SourceUrl);
        var changedPage = page with { Candidates = [candidate with { Documents = [Plain(text: "Annen tekst")] }] };
        Assert.Equal("Hei hei\nhei", selected.Document.Text); Assert.NotSame(changedPage.Candidates[0], selected.Candidate);
        Assert.Throws<LyricsValidationException>(() => LyricsSelections.Select(page, "wrong", "one", "word"));
        Assert.Throws<LyricsValidationException>(() => LyricsSelections.Select(page, "fixture", "absent", "word"));
        Assert.Throws<LyricsValidationException>(() => LyricsSelections.Select(page, "fixture", "one", null));
        Assert.Throws<LyricsValidationException>(() => LyricsSelections.Select(page, "fixture", "one", "absent"));
        Assert.Equal(LyricsValidationError.Ambiguous,
            Assert.Throws<LyricsValidationException>(() => LyricsSelections.Select(Page(candidate, candidate), "fixture", "one", "word")).Error);
        Assert.Throws<LyricsValidationException>(() => LyricsSelections.Select(Page(candidate with { Documents = [Plain(), Plain()] }), "fixture", "one", "plain"));
    }
    public static IEnumerable<object[]> InvalidCandidates()
    {
        var c = Candidate();
        yield return [c with { ProviderId = null! }];
        yield return [c with { Id = "" }];
        yield return [c with { Id = new string('x', 257) }];
        yield return [c with { Artist = "\ud800" }];
        yield return [c with { Title = new string('a', 4097) }];
        yield return [c with { Documents = default }];
        yield return [c with { Documents = [null!] }];
        yield return [c with { Documents = [] }];
        yield return [c with { TextStatus = (LyricsTextStatus)99 }];
        yield return [c with { TextStatus = LyricsTextStatus.Instrumental }];
        yield return [c with { TextStatus = LyricsTextStatus.Unsupported, Documents = [] }];
        yield return [c with { Capabilities = null! }];
        yield return [c with { Capabilities = new() { UnsupportedFormats = default } }];
        yield return [c with { Match = null! }];
        yield return [c with { Match = new(Artist: (LyricsMatchIndication)99) }];
        yield return [c with { Match = new(DurationDeltaSeconds: double.NaN) }];
        yield return [c with { DurationSeconds = 0 }];
        yield return [c with { DurationSeconds = double.PositiveInfinity }];
        yield return [c with { SourceUrl = "http://example.test/lyrics" }];
        yield return [c with { RetrievedAtUtc = default }];
        yield return [c with { RetrievedAtUtc = Captured.ToOffset(TimeSpan.FromHours(1)) }];
    }
    [Theory, MemberData(nameof(InvalidCandidates))]
    public void InvalidCandidatePayloadCannotBeSelected(LyricsCandidate candidate) =>
        Assert.Throws<LyricsValidationException>(() => LyricsSelections.Select(Page(candidate), "fixture", "one", "plain"));

    [Fact]
    public void InvalidQueriesPagesAndCollectionLimitsAreRejected()
    {
        var invalidQueries = new[]
        {
            new LyricsQuery("", "T"), new LyricsQuery("A", null!), new LyricsQuery("A", "T", Limit: 0),
            new LyricsQuery("A", "T", Limit: 26), new LyricsQuery("A", "T", DurationSeconds: -1),
            new LyricsQuery("A", "T", DurationSeconds: double.NaN), new LyricsQuery("A", "T", RecordingId: " "), new LyricsQuery("A", "T", VersionHint: new string('x', 4097))
        };
        foreach (var query in invalidQueries) Assert.Throws<LyricsValidationException>(() => LyricsValidation.Query(query));
        LyricsValidation.Query(new("A", "T", DurationSeconds: 14400, VersionHint: new string('x', 4096)));
        var page = Page(Candidate());
        foreach (var bad in new[] { page with { Query = null! }, page with { Candidates = default }, page with { Candidates = [null!] },
            page with { ProviderId = "wrong" }, page with { RetrievedAtUtc = default }, page with { Query = page.Query with { Limit = 1 }, Candidates = [Candidate(), Candidate("two")] } })
            Assert.Throws<LyricsValidationException>(() => LyricsValidation.Page(bad));
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.Page(page with { Candidates = Enumerable.Range(0, 26).Select(i => Candidate(i.ToString())).ToImmutableArray() }));
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.Candidate(Candidate() with { Documents = [Plain("a"), Plain("b"), Plain("c"), Plain("d")] }));
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.Candidate(Candidate() with { Capabilities = new() { UnsupportedFormats = Enumerable.Repeat("x", 17).ToImmutableArray() } }));
    }
    [Fact]
    public void DocumentAndPageBudgetsCountEveryVariantAndRepeatedHierarchyText()
    {
        var text = new string('é', 32768); // 64 KiB, 32 Ki UTF-16
        var candidates = Enumerable.Range(0, 25).Select(i => Candidate(i.ToString()) with
        { Documents = [Plain("a", text), Plain("b", text), Plain("c", text)] }).ToArray();
        LyricsValidation.Page(Page(candidates.Take(10).ToArray()));
        Assert.Equal(LyricsValidationError.ResponseTooLarge,
            Assert.Throws<LyricsValidationException>(() => LyricsValidation.Page(Page(candidates))).Error);
        var repeated = new string('a', 128 * 1024);
        Assert.Equal(LyricsValidationError.ResponseTooLarge,
            Assert.Throws<LyricsValidationException>(() => LyricsValidation.Document(new("line", repeated, LyricsSyncLevel.Line, [new(repeated, 0, null, [])]))).Error);
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.Document(new("line", "Egen", LyricsSyncLevel.Line,
            Enumerable.Repeat(new LyricsLine("", 0, null, []), 10001).ToImmutableArray())));
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.Document(new("word", "Egen", LyricsSyncLevel.Word,
            [new("Egen", 0, null, Enumerable.Repeat(new LyricsWord("a", 0), 50001).ToImmutableArray())])));
    }
}
