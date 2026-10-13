using System.Collections.Immutable;
using UltraStar.SongCreator.Acquisition.Lyrics;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class LyricsProviderResultValidationTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 14, 0, 0, 0, TimeSpan.Zero);
    private static LyricsCandidate Candidate(string id) => new()
    {
        ProviderId = "fixture",
        Id = id,
        RetrievedAtUtc = At,
        TextStatus = LyricsTextStatus.Available,
        Documents = [new("plain", "Egen tekst", LyricsSyncLevel.Plain, [])]
    };
    private static ImmutableArray<LyricsCandidate> Twenty() =>
        Enumerable.Range(0, 20).Select(i => Candidate(i.ToString())).ToImmutableArray();

    [Fact]
    public void EntireBatchIsCheckedBeforeLocalLimitWithoutChangingQueryAndReturnedSubset()
    {
        var query = new LyricsQuery("Egen", "Tekst", Limit: 1); var all = Twenty();
        LyricsValidation.ValidateProviderResults("fixture", query, all);
        Assert.Equal(1, query.Limit); Assert.Equal(20, all.Length);
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.Page(new("fixture", query, all, At)));
        var page = new LyricsPage("fixture", query, [all[0]], At);
        LyricsValidation.Page(page); Assert.Same(query, page.Query);
        Assert.Equal("0", LyricsSelections.Select(page, "fixture", "0", "plain").Candidate.Id);
        Assert.Throws<LyricsValidationException>(() => LyricsSelections.Select(page, "fixture", "19", "plain"));
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.Page(page with { RetrievedAtUtc = default }));
    }
    [Fact]
    public void BadTailDuplicateWrongProviderAndOversizeDiagnosticsCannotHideAfterLimitOne()
    {
        var q = new LyricsQuery("Egen", "Tekst", Limit: 1); var all = Twenty();
        foreach (var tail in new[] { all[19] with { Documents = default },
            all[19] with { ProviderId = "OTHER" }, all[19] with { VariantDiagnostics = default },
            all[19] with { VariantDiagnostics = [new("lrc", null, (LyricsVariantDiagnosticCode)99, LyricsVariantDiagnosticSeverity.Warning)] } })
            Assert.Throws<LyricsValidationException>(() => LyricsValidation.ValidateProviderResults("fixture", q, all.SetItem(19, tail)));
        Assert.Equal(LyricsValidationError.Ambiguous, Assert.Throws<LyricsValidationException>(() =>
            LyricsValidation.ValidateProviderResults("fixture", q, all.SetItem(19, all[0]))).Error);
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.ValidateProviderResults("fixture", q, all.Add(Candidate("extra"))));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(26)]
    [InlineData(int.MaxValue)]
    public void InvalidProviderMaximumIsRejected(int maximum) =>
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.ValidateProviderResults(
            "fixture", new("Egen", "Tekst"), [], maximum));

    [Fact]
    public void BoundaryMaximumQueryProviderAndDefaultCollectionsShareNormalValidation()
    {
        var q = new LyricsQuery("Egen", "Tekst", Limit: 1);
        LyricsValidation.ValidateProviderResults("fixture", q, [Candidate("one")], 1);
        var all25 = Enumerable.Range(0, 25).Select(i => Candidate(i.ToString())).ToImmutableArray();
        LyricsValidation.ValidateProviderResults("fixture", q, all25, 25);
        LyricsValidation.ValidateProviderResults("fixture", q, [], 20);
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.ValidateProviderResults("fixture", q, [Candidate("one"), Candidate("two")], 1));
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.ValidateProviderResults("fixture", q, default));
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.ValidateProviderResults("fixture", q, [null!]));
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.ValidateProviderResults("", q, []));
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.ValidateProviderResults("fixture", null!, []));
        Assert.Throws<LyricsValidationException>(() => LyricsValidation.ValidateProviderResults("fixture", q with { Limit = 0 }, []));
    }
    [Fact]
    public void AllVariantsAndDiagnosticsUseSharedPageBudgetBeforeRequestedLimit()
    {
        var q = new LyricsQuery("Egen", "Tekst", Limit: 1);
        var text = new string('a', 64 * 1024);
        var all = Twenty().Select(c => c with
        { Documents = [new("a", text, LyricsSyncLevel.Plain, []), new("b", text, LyricsSyncLevel.Plain, []), new("c", text, LyricsSyncLevel.Plain, [])] }).ToImmutableArray();
        LyricsValidation.ValidateProviderResults("fixture", q, all);
        var id = new string('a', 256);
        var diagnostics = Enumerable.Repeat(new LyricsVariantDiagnostic(id, id,
            LyricsVariantDiagnosticCode.UnsupportedFormat, LyricsVariantDiagnosticSeverity.Warning), 64).ToImmutableArray();
        var enriched = all.Select(c => c with { VariantDiagnostics = diagnostics }).ToImmutableArray();
        LyricsValidation.Page(new("fixture", q, [enriched[0]], At));
        Assert.Equal(LyricsValidationError.ResponseTooLarge,
            Assert.Throws<LyricsValidationException>(() => LyricsValidation.ValidateProviderResults("fixture", q, enriched)).Error);
    }
}
