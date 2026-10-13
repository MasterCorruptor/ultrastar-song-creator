using System.Collections.Immutable;
using Acq = UltraStar.SongCreator.Acquisition.Lyrics;
using Core = UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class SelectedLyricsProjectionTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
    private static Acq.LyricsDocument Word() => new("word", "Hei hei", Acq.LyricsSyncLevel.Word,
        [new("Hei hei", 1, null, [new("Hei ", 1, 3), new("hei", 2, null)])]);
    private static Acq.LyricsCandidate Candidate() => new()
    {
        ProviderId = "fixture",
        Id = "chosen-source",
        RetrievedAtUtc = At,
        SourceUrl = "https://example.test/chosen",
        Artist = "Kildeforfatter",
        Title = "Kildetittel",
        Album = "Kildealbum",
        Version = "egen",
        DurationSeconds = 8,
        TextStatus = Acq.LyricsTextStatus.Available,
        Documents = [new("plain", "Annen variant", Acq.LyricsSyncLevel.Plain, []),
            new("line", "Hei", Acq.LyricsSyncLevel.Line, [new("Hei", 1, null, [])]), Word()],
        Capabilities = new(true) { UnsupportedFormats = ["yaml"] },
        Match = new(Title: Acq.LyricsMatchIndication.Different)
    };
    [Fact]
    public void ProjectionStoresOnlyChosenDocumentAndDeterministicallyPreservesAvailableProvenance()
    {
        var c = Candidate(); var selection = new Acq.LyricsSelection(c, c.Documents[2]);
        var a = Acq.SelectedLyricsProjection.FromSelection(selection); var b = Acq.SelectedLyricsProjection.FromSelection(selection);
        Assert.True(Core.SelectedLyricsEquality.AreEqual(a, b));
        Assert.Equal(Core.SelectedLyricsSyncLevel.Word, a.Document!.SyncLevel); Assert.Equal("word", a.Document.Id);
        Assert.Equal("Hei hei", a.Document.Text); Assert.Equal("Hei ", a.Document.Lines[0].Words[0].Text);
        Assert.Null(a.Document.Lines[0].EndSeconds); Assert.Null(a.Document.Lines[0].Words[1].EndSeconds);
        Assert.Equal(c.Id, a.Provenance.CandidateId); Assert.Equal(c.ProviderId, a.Provenance.SourceId);
        Assert.Equal(c.SourceUrl, a.Provenance.SourceUrl); Assert.Equal(At, a.Provenance.RetrievedAtUtc); Assert.Null(a.Provenance.CreatedAtUtc);
        Assert.Equal(c.Artist, a.Provenance.Artist); Assert.Equal(c.Title, a.Provenance.Title);
        Assert.Equal(c.Album, a.Provenance.Album); Assert.Equal(c.Version, a.Provenance.Version); Assert.Equal(c.DurationSeconds, a.Provenance.DurationSeconds);
        Assert.Null(typeof(Core.SelectedLyrics).GetProperty("Candidate"));
        Assert.Null(typeof(Core.SelectedLyrics).GetProperty("Documents"));
    }
    [Fact]
    public void ManualCreationTimeAndIdentityAreCopiedNotGeneratedOrReinterpretedAsRetrieval()
    {
        var selection = Acq.ManualLyrics.Create("  æøå 🎵 \r\nHei\r\nHei\r\n");
        var a = Acq.SelectedLyricsProjection.FromSelection(selection);
        Assert.Equal(Core.SelectedLyricsOrigin.Manual, a.Provenance.Origin);
        Assert.Equal(selection.Candidate.Id, a.Provenance.CandidateId);
        Assert.Equal(selection.Candidate.RetrievedAtUtc, a.Provenance.CreatedAtUtc); Assert.Null(a.Provenance.RetrievedAtUtc);
        Assert.Null(a.Provenance.SourceUrl); Assert.Null(a.Provenance.Artist); Assert.Null(a.Provenance.Title);
        Assert.Equal("  æøå 🎵 \nHei\nHei\n", a.Document!.Text);
        Assert.True(Core.SelectedLyricsEquality.AreEqual(a, Acq.SelectedLyricsProjection.FromSelection(selection)));
        Assert.Throws<Core.SelectedLyricsValidationException>(() => Acq.SelectedLyricsProjection.FromSelection(
            selection with { Candidate = selection.Candidate with { Id = "not-a-canonical-manual-guid" } }));
    }
    [Fact]
    public void InstrumentalIsExplicitAndMissingUnsupportedNeverProjectToStoredChoice()
    {
        var c = Candidate() with { Documents = [], TextStatus = Acq.LyricsTextStatus.Instrumental };
        var selected = Acq.SelectedLyricsProjection.FromSelection(new(c, null));
        Assert.Equal(Core.SelectedLyricsKind.Instrumental, selected.Kind); Assert.Null(selected.Document);
        foreach (var bad in new[] { c with { TextStatus = Acq.LyricsTextStatus.Missing },
            c with { TextStatus = Acq.LyricsTextStatus.Unsupported }, Candidate() })
            Assert.Throws<Acq.LyricsValidationException>(() => Acq.SelectedLyricsProjection.FromSelection(new(bad, null)));
        Assert.Throws<Acq.LyricsValidationException>(() => Acq.SelectedLyricsProjection.FromSelection(new(c, Word())));
    }
    [Fact]
    public void CompleteStructuralAssociationRejectsForgedSameIdTextTimeOrWordPayload()
    {
        var c = Candidate(); var d = c.Documents[2]; var line = d.Lines[0];
        var forged = new[] { d with { Id = "absent" }, d with { Text = "Endret" },
            d with { Lines = [line with { EndSeconds = 4 }] },
            d with { Lines = [line with { Words = line.Words.SetItem(0, line.Words[0] with { Text = "ENDRET " }) }] },
            d with { Lines = [line with { Words = line.Words.SetItem(0, line.Words[0] with { EndSeconds = 2.5 }) }] } };
        foreach (var payload in forged)
            Assert.Throws<Acq.LyricsValidationException>(() => Acq.SelectedLyricsProjection.FromSelection(new(c, payload)));
        var separate = d with { Lines = d.Lines.Select(l => l with { Words = l.Words.Select(w => w with { }).ToImmutableArray() }).ToImmutableArray() };
        var result = Acq.SelectedLyricsProjection.FromSelection(new(c, separate));
        Assert.Equal(d.Text, result.Document!.Text);
    }
    [Fact]
    public void WholeCandidateIsValidatedIncludingUnselectedVariantsAndExplicitLevelsAreMapped()
    {
        var c = Candidate();
        foreach (var d in c.Documents)
        {
            var projected = Acq.SelectedLyricsProjection.FromSelection(new(c, d));
            Assert.Equal(d.SyncLevel.ToString(), projected.Document!.SyncLevel.ToString());
            Assert.Equal(d.Text, projected.Document.Text);
        }
        Assert.Throws<Acq.LyricsValidationException>(() => Acq.SelectedLyricsProjection.FromSelection(
            new(c with { Documents = c.Documents.SetItem(0, c.Documents[0] with { Lines = default }) }, c.Documents[2])));
        Assert.Throws<Acq.LyricsValidationException>(() => Acq.SelectedLyricsProjection.FromSelection(
            new(c with { Documents = [c.Documents[2], c.Documents[2]] }, c.Documents[2])));
        Assert.Throws<Acq.LyricsValidationException>(() => Acq.SelectedLyricsProjection.FromSelection(new(c with { Documents = default }, c.Documents[2])));
        Assert.Throws<Acq.LyricsValidationException>(() => Acq.SelectedLyricsProjection.FromSelection(new(null!, c.Documents[2])));
    }
}
