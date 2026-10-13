using System.Collections.Immutable;
using UltraStar.SongCreator.Acquisition.Lyrics;
using Core = UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class LyricsVariantDiagnosticTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 14, 0, 0, 0, TimeSpan.Zero);
    private static LyricsCandidate Candidate(string id = "one") => new()
    {
        ProviderId = "fixture",
        Id = id,
        RetrievedAtUtc = At,
        TextStatus = LyricsTextStatus.Available,
        Documents = [new("plain", "Egen tekst", LyricsSyncLevel.Plain, [])]
    };
    private static LyricsVariantDiagnostic Diagnostic() => new("lrc-simple-line-v1", "line-lrc",
        LyricsVariantDiagnosticCode.InvalidSyntax, LyricsVariantDiagnosticSeverity.Warning, 1);

    [Fact]
    public void OldDefaultAndFailedVariantDiagnosticsPreserveSelectionWithoutClaimingSyncOrPersisting()
    {
        var original = Candidate(); Assert.False(original.VariantDiagnostics.IsDefault); Assert.Empty(original.VariantDiagnostics);
        LyricsValidation.Candidate(original);
        var diagnostic = Diagnostic();
        var enriched = original with { VariantDiagnostics = [diagnostic] };
        var query = new LyricsQuery("Egen artist", "Egen tittel", Limit: 1);
        var selected = LyricsSelections.Select(new("fixture", query, [enriched], At), "fixture", "one", "plain");
        Assert.Same(diagnostic, Assert.Single(selected.Candidate.VariantDiagnostics));
        Assert.Equal(LyricsTextStatus.Available, selected.Candidate.TextStatus);
        Assert.Equal(LyricsSyncLevel.Plain, selected.Document!.SyncLevel); Assert.Empty(enriched.Capabilities.UnsupportedFormats);
        Assert.Throws<LyricsValidationException>(() => LyricsSelections.Select(
            new("fixture", query, [enriched], At), "fixture", "one", "line-lrc"));
        Assert.True(Core.SelectedLyricsEquality.AreEqual(
            SelectedLyricsProjection.FromSelection(new(original, original.Documents[0])),
            SelectedLyricsProjection.FromSelection(selected)));
        Assert.Null(typeof(Core.SelectedLyrics).GetProperty("VariantDiagnostics"));
    }

    public static IEnumerable<object[]> InvalidDiagnostics()
    {
        var d = Diagnostic();
        yield return [d with { FormatId = null! }];
        yield return [d with { FormatId = "" }];
        yield return [d with { FormatId = " " }];
        yield return [d with { FormatId = new string('a', 257) }];
        yield return [d with { VariantId = "" }];
        yield return [d with { VariantId = " " }];
        yield return [d with { VariantId = new string('a', 257) }];
        yield return [d with { Code = (LyricsVariantDiagnosticCode)99 }];
        yield return [d with { Severity = (LyricsVariantDiagnosticSeverity)99 }];
        yield return [d with { PhysicalLineNumber = 0 }];
        yield return [d with { PhysicalLineNumber = -1 }];
        yield return [d with { PhysicalLineNumber = 20001 }];
    }
    [Theory, MemberData(nameof(InvalidDiagnostics))]
    public void InvalidShapeFailsEvenWhenChosenPlainDocumentIsValid(LyricsVariantDiagnostic diagnostic) =>
        Assert.Throws<LyricsValidationException>(() => SelectedLyricsProjection.FromSelection(
            new(Candidate() with { VariantDiagnostics = [diagnostic] }, Candidate().Documents[0])));

    [Fact]
    public void NullDefaultEntriesAndCountBoundsAreChecked()
    {
        var c = Candidate();
        foreach (var diagnostics in new[] { default(ImmutableArray<LyricsVariantDiagnostic>),
            ImmutableArray.Create<LyricsVariantDiagnostic>((LyricsVariantDiagnostic)null!),
            Enumerable.Repeat(Diagnostic(), 65).ToImmutableArray() })
            Assert.Throws<LyricsValidationException>(() => LyricsValidation.Candidate(c with { VariantDiagnostics = diagnostics }));
        LyricsValidation.Candidate(c with { VariantDiagnostics = Enumerable.Repeat(Diagnostic(), 64).ToImmutableArray() });
    }

    [Fact]
    public void UnicodeIdsAndBoundaryLineNumbersAreValidButMalformedSurrogatesAreRejected()
    {
        var c = Candidate();
        var longest = new string('漢', 256);
        foreach (var number in new int?[] { null, 1, 20000 })
            LyricsValidation.Candidate(c with
            {
                VariantDiagnostics = [new(longest, longest,
                LyricsVariantDiagnosticCode.ExplicitZeroOffset, LyricsVariantDiagnosticSeverity.Info, number)]
            });
        LyricsValidation.Candidate(c with { VariantDiagnostics = [Diagnostic() with { VariantId = null }] });
        foreach (var code in new[] { 0xd800, 0xdc00 })
        {
            var invalid = new string((char)code, 1);
            Assert.Throws<LyricsValidationException>(() => LyricsValidation.Candidate(c with { VariantDiagnostics = [Diagnostic() with { FormatId = invalid }] }));
            Assert.Throws<LyricsValidationException>(() => LyricsValidation.Candidate(c with { VariantDiagnostics = [Diagnostic() with { VariantId = invalid }] }));
        }
    }

    [Fact]
    public void DiagnosticsWithoutDocumentsAreCountedWithMetadataAndUtf8InWholePageBudget()
    {
        var metadata = new string('漢', 4096);
        var format = new string('漢', 256);
        const string urlPrefix = "https://example.test/";
        var url = urlPrefix + new string('漢', 4096 - urlPrefix.Length);
        var diagnostics = Enumerable.Repeat(new LyricsVariantDiagnostic(format, format,
            LyricsVariantDiagnosticCode.NoLyrics, LyricsVariantDiagnosticSeverity.Warning), 64).ToImmutableArray();
        var candidates = Enumerable.Range(0, 25).Select(i => Candidate(i.ToString()) with
        {
            TextStatus = LyricsTextStatus.Missing,
            Documents = [],
            Artist = metadata,
            Title = metadata,
            Album = metadata,
            Version = metadata,
            SourceUrl = url,
            Capabilities = new() { UnsupportedFormats = Enumerable.Repeat(format, 16).ToImmutableArray() }
        }).ToImmutableArray();
        var query = new LyricsQuery("Egen", "Tekst");
        LyricsValidation.Page(new("fixture", query, candidates, At));
        var enriched = candidates.Select(c => c with { VariantDiagnostics = diagnostics }).ToImmutableArray();
        LyricsValidation.Candidate(enriched[0]);
        Assert.Equal(LyricsValidationError.ResponseTooLarge,
            Assert.Throws<LyricsValidationException>(() => LyricsValidation.Page(new("fixture", query, enriched, At))).Error);
    }
}
