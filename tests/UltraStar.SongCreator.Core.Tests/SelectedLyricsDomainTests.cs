using System.Collections.Immutable;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class SelectedLyricsDomainTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
    private static SelectedLyricsProvenance Provider() => new(SelectedLyricsOrigin.Provider, "fixture", "one",
        "https://example.test/one", null, At, "Egen artist", "Egen tittel", "Eget album", "egen versjon", 10);
    private static SelectedLyricsDocument Words() => new("word", "Hei hei\nHei",
        SelectedLyricsSyncLevel.Word, [new("Hei hei", 1, 4, [new("Hei ", 1, 3), new("hei", 1, null)]),
        new("Hei", 1, null, [new("Hei", 2, null)])]);
    private static SelectedLyrics Model() => new(SelectedLyricsKind.Text, Words(), Provider());

    [Fact]
    public void IndependentPlainManualLineWordInstrumentalAndNullAreValidatedWithoutSongAttachment()
    {
        SelectedLyricsValidation.Validate(null);
        Assert.True(SelectedLyricsEquality.AreEqual(null, null)); Assert.False(SelectedLyricsEquality.AreEqual(Model(), null));
        var manual = new SelectedLyricsProvenance(SelectedLyricsOrigin.Manual, "manual",
            "11111111-1111-1111-1111-111111111111", null, At, null);
        SelectedLyricsValidation.Validate(new(SelectedLyricsKind.Text,
            new("plain", "  æøå 🎵  \nHei\nHei\n", SelectedLyricsSyncLevel.Plain, []), manual));
        SelectedLyricsValidation.Validate(new(SelectedLyricsKind.Text,
            new("line", "Egen\n", SelectedLyricsSyncLevel.Line, [new("Egen", 0, null, [])]), Provider()));
        SelectedLyricsValidation.Validate(Model());
        SelectedLyricsValidation.Validate(new(SelectedLyricsKind.Instrumental, null, Provider()));
    }
    public static IEnumerable<object[]> InvalidModels()
    {
        var m = Model(); var d = m.Document!; var p = m.Provenance; var line = d.Lines[0];
        yield return [m with { Kind = (SelectedLyricsKind)99 }];
        yield return [m with { Document = null }];
        yield return [m with { Kind = SelectedLyricsKind.Instrumental }];
        yield return [m with { Provenance = null! }];
        yield return [m with { Provenance = p with { Origin = (SelectedLyricsOrigin)99 } }];
        yield return [m with { Provenance = p with { SourceId = "manual" } }];
        yield return [m with { Provenance = p with { SourceId = "" } }];
        yield return [m with { Provenance = p with { CandidateId = new string('a', 257) } }];
        yield return [m with { Provenance = p with { CreatedAtUtc = At } }];
        yield return [m with { Provenance = p with { RetrievedAtUtc = null } }];
        yield return [m with { Provenance = p with { RetrievedAtUtc = default(DateTimeOffset) } }];
        yield return [m with { Provenance = p with { RetrievedAtUtc = At.ToOffset(TimeSpan.FromHours(1)) } }];
        yield return [m with { Provenance = p with { SourceUrl = "http://example.test/one" } }];
        yield return [m with { Provenance = p with { SourceUrl = "https://user:secret@example.test/one" } }];
        yield return [m with { Provenance = p with { SourceUrl = "https://example.test/one#fragment" } }];
        yield return [m with { Provenance = p with { Title = new string('a', 4097) } }];
        yield return [m with { Provenance = p with { DurationSeconds = 0 } }];
        yield return [m with { Provenance = p with { DurationSeconds = 14401 } }];
        yield return [m with { Provenance = p with { DurationSeconds = double.NaN } }];
        yield return [m with { Document = d with { SyncLevel = (SelectedLyricsSyncLevel)99 } }];
        yield return [m with { Document = d with { TimeReference = (SelectedLyricsTimeReference)99 } }];
        yield return [m with { Document = d with { Text = "" } }];
        yield return [m with { Document = d with { Id = "" } }];
        yield return [m with { Document = d with { Text = null! } }];
        yield return [m with { Document = d with { Text = "Hei\r\nhei" } }];
        yield return [m with { Document = d with { Lines = default } }];
        yield return [m with { Document = d with { Lines = [null!] } }];
        yield return [m with { Document = d with { Lines = [line with { Words = default }] } }];
        yield return [m with { Document = d with { Lines = [line with { Words = [null!] }] } }];
        yield return [m with { Document = d with { Lines = [line with { Words = [] }] } }];
        yield return [m with { Document = d with { Lines = [line with { StartSeconds = -1 }] } }];
        yield return [m with { Document = d with { Lines = [line with { StartSeconds = 14401 }] } }];
        yield return [m with { Document = d with { Lines = [line with { EndSeconds = double.NaN }] } }];
        yield return [m with { Document = d with { Lines = [line with { EndSeconds = .5 }] } }];
        yield return [m with { Document = d with { Lines = [line with { Words = [new("Hei", 0)] }] } }];
        yield return [m with { Document = d with { Lines = [line with { Words = [new("Hei", 2, 5)] }] } }];
        yield return [m with { Document = d with { Lines = [line with { Words = [new("Hei", 2), new("Hei", 1)] }] } }];
        yield return [m with { Document = d with { Lines = [line with { Words = [new("", 1)] }] } }];
        yield return [m with { Document = d with { Lines = [line with { Words = [new("Hei", double.PositiveInfinity)] }] } }];
        yield return [m with { Document = d with { SyncLevel = SelectedLyricsSyncLevel.Plain } }];
        yield return [m with { Document = d with { SyncLevel = SelectedLyricsSyncLevel.Line } }];
    }
    [Theory, MemberData(nameof(InvalidModels))]
    public void MalformedOrDefaultModelsFailValidationAndEqualityEvenAgainstThemselves(SelectedLyrics model)
    {
        Assert.Throws<SelectedLyricsValidationException>(() => SelectedLyricsValidation.Validate(model));
        Assert.Throws<SelectedLyricsValidationException>(() => SelectedLyricsEquality.AreEqual(model, model));
    }
    [Fact]
    public void ManualIdentityAndUtcCreationHaveExplicitSemantics()
    {
        var manual = new SelectedLyricsProvenance(SelectedLyricsOrigin.Manual, "manual",
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", null, At, null);
        var m = new SelectedLyrics(SelectedLyricsKind.Instrumental, null, manual);
        SelectedLyricsValidation.Validate(m);
        Assert.False(SelectedLyricsEquality.AreEqual(m, m with { Provenance = manual with { CreatedAtUtc = At.AddTicks(1) } }));
        foreach (var bad in new[] { manual with { CandidateId = Guid.Empty.ToString("D") },
            manual with { CandidateId = manual.CandidateId.ToUpperInvariant() }, manual with { SourceId = "other" },
            manual with { SourceUrl = "https://example.test/one" }, manual with { CreatedAtUtc = null },
            manual with { RetrievedAtUtc = At }, manual with { CreatedAtUtc = default(DateTimeOffset) }, manual with { CreatedAtUtc = At.ToOffset(TimeSpan.FromHours(1)) } })
            Assert.Throws<SelectedLyricsValidationException>(() => SelectedLyricsValidation.Validate(m with { Provenance = bad }));
    }
    [Fact]
    public void SeparateArraysCompareStructurallyAndEveryValidFieldChangeRemainsAnEdit()
    {
        var m = Model();
        var copy = m with
        {
            Document = m.Document! with
            {
                Lines = m.Document.Lines.Select(l =>
            l with { Words = l.Words.Select(w => w with { }).ToImmutableArray() }).ToImmutableArray()
            },
            Provenance = m.Provenance with { }
        };
        Assert.NotEqual(m.Document!.Lines, copy.Document!.Lines); Assert.True(SelectedLyricsEquality.AreEqual(m, copy));
        var d = m.Document; var p = m.Provenance;
        var changes = new[]
        {
            m with { Kind = SelectedLyricsKind.Instrumental, Document = null },
            m with { Document = d with { Id = "other" } }, m with { Document = d with { Text = "Hei hei\nHEI" } },
            m with { Document = d with { Lines = d.Lines.Reverse().ToImmutableArray() } },
            m with { Document = d with { Lines = d.Lines.SetItem(0, d.Lines[0] with { Text = "HEI hei" }) } },
            m with { Document = d with { Lines = d.Lines.SetItem(0, d.Lines[0] with { EndSeconds = null }) } },
            m with { Document = d with { Lines = d.Lines.SetItem(0, d.Lines[0] with { Words = d.Lines[0].Words.Reverse().ToImmutableArray() }) } },
            m with { Document = d with { Lines = d.Lines.SetItem(0, d.Lines[0] with { Words = d.Lines[0].Words.SetItem(1, d.Lines[0].Words[1] with { StartSeconds = 1.01 }) }) } },
            m with { Document = d with { Lines = d.Lines.SetItem(0, d.Lines[0] with { Words = d.Lines[0].Words.SetItem(0, d.Lines[0].Words[0] with { EndSeconds = null }) }) } },
            m with { Provenance = p with { SourceId = "FIXTURE" } }, m with { Provenance = p with { CandidateId = "two" } },
            m with { Provenance = p with { SourceUrl = null } }, m with { Provenance = p with { RetrievedAtUtc = At.AddTicks(1) } },
            m with { Provenance = p with { Artist = "EGEN artist" } }, m with { Provenance = p with { Title = "EGEN tittel" } },
            m with { Provenance = p with { Album = null } }, m with { Provenance = p with { Version = "other" } },
            m with { Provenance = p with { DurationSeconds = 10.001 } },
            m with { Document = d with { Lines = d.Lines.SetItem(1, d.Lines[1] with { StartSeconds = 1.01 }) } },
            m with { Document = d with { Lines = d.Lines.SetItem(0, d.Lines[0] with { Words = d.Lines[0].Words.SetItem(0, d.Lines[0].Words[0] with { Text = "HEI " }) }) } },
            m with { Document = d with { SyncLevel = SelectedLyricsSyncLevel.Line, Lines = d.Lines.Select(l => l with { Words = [] }).ToImmutableArray() } }
        };
        foreach (var changed in changes) Assert.False(SelectedLyricsEquality.AreEqual(m, changed));
    }
    [Fact]
    public void LogicalUtf8BudgetAndHierarchyCountAreEnforcedWithoutDurationClipping()
    {
        var text = new string('é', (SelectedLyricsValidation.MaxDocumentBytes - 70) / 2); // doc64+id chosen6
        var d = new SelectedLyricsDocument("chosen", text, SelectedLyricsSyncLevel.Plain, []);
        SelectedLyricsValidation.Validate(new(SelectedLyricsKind.Text, d, Provider() with { Artist = new string('é', 4096) }));
        Assert.Equal(SelectedLyricsValidationError.ResponseTooLarge,
            Assert.Throws<SelectedLyricsValidationException>(() => SelectedLyricsValidation.ValidateDocument(d with { Text = text + "é" })).Error);
        var repeated = new string('a', 128 * 1024);
        Assert.Throws<SelectedLyricsValidationException>(() => SelectedLyricsValidation.ValidateDocument(
            new("line", repeated, SelectedLyricsSyncLevel.Line, [new(repeated, 0, null, [])])));
        SelectedLyricsValidation.Validate(Model() with
        {
            Document = new("line", "Slutt", SelectedLyricsSyncLevel.Line,
            [new("Slutt", 14400, 14400, [])]),
            Provenance = Provider() with { DurationSeconds = 1 }
        });
        Assert.Throws<SelectedLyricsValidationException>(() => SelectedLyricsValidation.ValidateDocument(
            new("line", "Egen", SelectedLyricsSyncLevel.Line, Enumerable.Repeat(new SelectedLyricsLine("", 0, null, []), 10001).ToImmutableArray())));
        Assert.Throws<SelectedLyricsValidationException>(() => SelectedLyricsValidation.ValidateDocument(
            new("word", "Egen", SelectedLyricsSyncLevel.Word, [new("Egen", 0, null, Enumerable.Repeat(new SelectedLyricsWord("a", 0), 50001).ToImmutableArray())])));
        var bad = new string((char)0xd800, 1);
        Assert.Throws<SelectedLyricsValidationException>(() => SelectedLyricsValidation.Validate(Model() with { Provenance = Provider() with { Artist = bad } }));
        Assert.Throws<SelectedLyricsValidationException>(() => SelectedLyricsValidation.ValidateDocument(d with { Text = bad }));
    }
}
