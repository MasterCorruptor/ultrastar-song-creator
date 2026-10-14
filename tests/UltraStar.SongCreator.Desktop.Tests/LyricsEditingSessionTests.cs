using System.Collections.Immutable;
using UltraStar.SongCreator.Core;
using UltraStar.SongCreator.Desktop;

namespace UltraStar.SongCreator.Desktop.Tests;

public sealed class LyricsEditingSessionTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 14, 10, 0, 0, TimeSpan.Zero);
    private static SelectedLyrics Payload(string candidate = "one") => new(
        SelectedLyricsKind.Text,
        new("line", " Egen æøå \n", SelectedLyricsSyncLevel.Line,
            [new(" Egen æøå ", 12.5, null, [])]),
        new(SelectedLyricsOrigin.Provider, "fixture", candidate, "https://fixture.invalid/" + candidate,
            null, At, "Kildeartist", "Kildetittel", "Album", "Versjon", 45));
    private static Song Song() => DemoSong.Create() with
    {
        AudioOffsetSeconds = 2.25,
        VideoOffsetSeconds = -1.5,
        BeatsPerMinute = 120,
        Media = [new() { Kind = MediaKind.Audio, Location = "synthetic.wav", Source = "fixture" }],
        Metadata = new() { Artist = "Egen artist", Title = "Egen sang", Language = "nb" }
    };
    private static void OtherFieldsUnchanged(Song before, Song after)
    {
        Assert.Equal(before.Id, after.Id); Assert.Same(before.Metadata, after.Metadata);
        Assert.Equal(before.Media, after.Media); Assert.Equal(before.Phrases, after.Phrases);
        Assert.Same(before.Analysis, after.Analysis); Assert.Same(before.ImportedSource, after.ImportedSource);
        Assert.Equal(before.AudioOffsetSeconds, after.AudioOffsetSeconds);
        Assert.Equal(before.VideoOffsetSeconds, after.VideoOffsetSeconds); Assert.Equal(before.BeatsPerMinute, after.BeatsPerMinute);
    }

    [Fact]
    public void ApplyUsesCurrentSongAndPreservesNoteEditsMadeBeforeApply()
    {
        var session = new EditingSession(Song()); var id = session.Current.Phrases[0].Notes[0].Id;
        var payload = Payload(); session.Select(id); session.Move("2.5");
        var moved = session.Current; Assert.Equal(2.5, moved.Phrases[0].Notes[0].StartSeconds);
        Assert.True(session.ApplySelectedLyrics(payload));
        OtherFieldsUnchanged(moved, session.Current); Assert.Equal(id, session.SelectedNoteId);
        Assert.Same(payload, session.Current.SelectedLyrics);
        Assert.Equal(12.5, session.Current.SelectedLyrics!.Document!.Lines[0].StartSeconds);
        Assert.Null(session.Current.SelectedLyrics.Document.Lines[0].EndSeconds);
        Assert.Equal(SelectedLyricsTimeReference.LyricsSourceSeconds, session.Current.SelectedLyrics.Document.TimeReference);
        Assert.Equal(At, session.Current.SelectedLyrics.Provenance.RetrievedAtUtc);
        Assert.Contains("æøå", session.Current.SelectedLyrics.Document.Text);
    }

    [Fact]
    public void MixedNoteAndLyricsHistoryRestoresExactSnapshotsInOrder()
    {
        var session = new EditingSession(Song()); var initial = session.Current;
        session.Select(initial.Phrases[0].Notes[0].Id); session.Move("2"); var moved = session.Current;
        Assert.True(session.ApplySelectedLyrics(Payload())); var lyrics = session.Current;
        session.Move("3"); var movedAgain = session.Current;
        session.Undo(); Assert.Same(lyrics, session.Current); Assert.Equal("Endringen er angret.", session.Message);
        session.Undo(); Assert.Same(moved, session.Current); Assert.Null(session.Current.SelectedLyrics);
        session.Undo(); Assert.Same(initial, session.Current); Assert.False(session.CanUndo);
        session.Redo(); Assert.Same(moved, session.Current); Assert.Equal("Endringen er gjort om.", session.Message);
        session.Redo(); Assert.Same(lyrics, session.Current);
        session.Redo(); Assert.Same(movedAgain, session.Current); Assert.False(session.CanRedo);
    }

    [Fact]
    public void ValidEqualDistinctPayloadIsNoOpWithoutClearingRedo()
    {
        var payload = Payload(); var session = new EditingSession(Song() with { SelectedLyrics = payload });
        session.Select(session.Current.Phrases[0].Notes[0].Id); session.Move("2"); var redo = session.Current;
        session.Undo(); var before = session.Current;
        var clone = payload with
        {
            Document = payload.Document! with { Lines = payload.Document.Lines.Select(l => l with { Words = l.Words.ToImmutableArray() }).ToImmutableArray() },
            Provenance = payload.Provenance with { }
        };
        Assert.NotSame(payload, clone); Assert.NotSame(payload.Document, clone.Document);
        Assert.False(session.ApplySelectedLyrics(clone));
        Assert.Same(before, session.Current); Assert.True(session.CanRedo);
        Assert.Equal("Denne sangteksten er allerede valgt.", session.Message);
        session.Redo(); Assert.Same(redo, session.Current);
    }

    [Fact]
    public void NewLyricsChangeClearsRedoAndIsUndoableToTheCurrentSnapshot()
    {
        var session = new EditingSession(Song()); session.Select(session.Current.Phrases[0].Notes[0].Id);
        session.Move("2"); session.Undo(); var before = session.Current;
        Assert.True(session.CanRedo); Assert.True(session.ApplySelectedLyrics(Payload("two")));
        Assert.False(session.CanRedo); session.Undo(); Assert.Same(before, session.Current);
    }

    public static IEnumerable<object[]> InvalidPayloads()
    {
        yield return [null!];
        var good = Payload();
        yield return [good with { Document = null }];
        yield return [good with { Kind = (SelectedLyricsKind)999 }];
        yield return [good with { Document = good.Document! with { Text = "secret-payload\r" } }];
        yield return [good with { Document = good.Document! with { Lines = [new("Egen", -1, null, [])] } }];
        yield return [good with { Provenance = good.Provenance with { SourceUrl = "http://secret-payload.invalid" } }];
        yield return [good with { Provenance = good.Provenance with { RetrievedAtUtc = default(DateTimeOffset) } }];
        yield return [good with { Document = good.Document! with { Text = new string('a', SelectedLyricsValidation.MaxDocumentBytes + 1) } }];
    }

    [Theory, MemberData(nameof(InvalidPayloads))]
    public void RejectedPayloadAndNullPreserveCurrentLyricsHistoryRedoAndSelection(SelectedLyrics? invalid)
    {
        var stored = Payload(); var session = new EditingSession(Song() with { SelectedLyrics = stored });
        var id = session.Current.Phrases[0].Notes[0].Id; session.Select(id); session.Move("2"); var redo = session.Current;
        session.Undo(); var before = session.Current; var notified = 0;
        session.Changed += (_, _) => notified++;
        Assert.False(session.ApplySelectedLyrics(invalid!));
        Assert.Same(before, session.Current); Assert.Same(stored, session.Current.SelectedLyrics);
        Assert.Equal(id, session.SelectedNoteId); Assert.True(session.CanRedo); Assert.False(session.CanUndo);
        Assert.Equal(1, notified); Assert.DoesNotContain("secret", session.Message);
        Assert.Equal(invalid is null ? "Velg en gyldig sangtekst før bruk." : "Sangteksten kunne ikke brukes. Kontroller tekst og kildeinformasjon.", session.Message);
        session.Redo(); Assert.Same(redo, session.Current);
    }

    [Fact]
    public void ChangedAndStaticStatusAreTruthfulForSuccessNoOpAndRejection()
    {
        var session = new EditingSession(Song()); var payload = Payload(); var notified = new List<Song>();
        session.Changed += (_, _) => notified.Add(session.Current);
        Assert.True(session.ApplySelectedLyrics(payload)); var applied = session.Current;
        Assert.Equal("Valgt sangtekst er brukt.", session.Message);
        Assert.False(session.ApplySelectedLyrics(payload)); Assert.False(session.ApplySelectedLyrics(null!));
        Assert.Equal(3, notified.Count); Assert.All(notified, s => Assert.Same(applied, s));
        session.Undo(); Assert.Null(session.Current.SelectedLyrics); Assert.Equal("Endringen er angret.", session.Message);
        session.Redo(); Assert.Same(applied, session.Current); Assert.Equal("Endringen er gjort om.", session.Message);
    }

    [Fact]
    public void ExplicitInstrumentalPayloadAppliesWithoutInventedTextOrTiming()
    {
        var text = Payload();
        var instrumental = text with { Kind = SelectedLyricsKind.Instrumental, Document = null };
        var session = new EditingSession(Song()); var before = session.Current;
        Assert.True(session.ApplySelectedLyrics(instrumental));
        OtherFieldsUnchanged(before, session.Current); Assert.Null(session.Current.SelectedLyrics!.Document);
        Assert.Equal(SelectedLyricsKind.Instrumental, session.Current.SelectedLyrics.Kind);
    }
}
