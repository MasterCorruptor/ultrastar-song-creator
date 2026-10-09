using System.Globalization;
using UltraStar.SongCreator.Projects;
using UltraStar.SongCreator.UltraStar;

namespace UltraStar.SongCreator.Core.Tests;

public class RelativeImportTests
{
    private const string Headers = "#TITLE:Test\n#ARTIST:Test\n#MP3:missing.wav\n#BPM:120\n#RELATIVE:YES\n";
    private static ImportResult Parse(string body, string extra = "") =>
        new UltraStarImporter().Parse(Headers + extra + body);

    private static Song Success(ImportResult result)
    {
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => $"{d.Code}@{d.Line}: {d.Message}")));
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == ValidationSeverity.Error);
        return Assert.IsType<Song>(result.Song);
    }

    private static void Error(ImportResult result, ImportCode code, int line)
    {
        Assert.False(result.Success);
        Assert.Null(result.Song);
        Assert.Contains(result.Diagnostics, d => d.Severity == ValidationSeverity.Error && d.Code == code && d.Line == line);
    }

    [Fact]
    public async Task HandwrittenFixtureMatchesIndependentAbsoluteBeatVectorAndKeepsGapSeparate()
    {
        var path = Path.Combine(ProjectTestDirectory.RepositoryRoot, "tests", "UltraStar.SongCreator.Core.Tests", "Fixtures", "relative.txt");
        var result = await new UltraStarImporter().LoadAsync(path);
        var song = Success(result);
        var notes = song.Phrases.SelectMany(p => p.Notes).ToArray();
        // Independently worked example: offsets 0,16,28; first source note starts at beat 4.
        Assert.Equal(new[] { .5, 2d, 2.5, 3.5, 3.75, 4d }, notes.Select(n => n.StartSeconds));
        Assert.Equal(new[] { .5, .5, .5, .25, .25, .5 }, notes.Select(n => n.DurationSeconds));
        Assert.Equal(new[] { 60, 58, 62, 60, 60, 60 }, notes.Select(n => n.MidiPitch));
        Assert.Equal(new[] { NoteType.Normal, NoteType.Golden, NoteType.Normal, NoteType.Rap, NoteType.GoldenRap, NoteType.Freestyle }, notes.Select(n => n.Type));
        Assert.Equal(new[] { "Først", "neste del", "Rap gull fri" }, song.Phrases.Select(p => p.Text));
        Assert.Equal(new[] { 1d, 3d, 4.5 }, song.Phrases.Select(p => p.EndSeconds));
        Assert.Equal(1.5, song.AudioOffsetSeconds);
        Assert.Equal(1.25, song.VideoOffsetSeconds);
        Assert.Equal(2, notes[0].StartSeconds + song.AudioOffsetSeconds);
        Assert.Equal(1.75, notes[0].StartSeconds + song.VideoOffsetSeconds);
        Assert.Null(song.BeatsPerMinute);
        Assert.Equal("yEs", song.ImportedSource!.Headers.Single(h => h.Name == "RELATIVE").Value);
        var warning = Assert.Single(result.Diagnostics.Where(d => d.Code == ImportCode.RelativeTimingCompatibility));
        Assert.Equal(8, warning.Line);
        Assert.Equal(ValidationSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void EquivalentExplicitAbsoluteFileHasSameTimingAndLyrics()
    {
        const string relative = ": 4 4 0 A \n- 10 16\n* 0 4 2 B \n: 4 4 4 C\n- 10 12\nF 0 8 0 D\nE";
        const string absolute = ": 4 4 0 A \n- 10\n* 16 4 2 B \n: 20 4 4 C\n- 26\nF 28 8 0 D\nE";
        var rel = Success(Parse(relative, "#GAP:1234.5\n"));
        var abs = Success(new UltraStarImporter().Parse(Headers.Replace("#RELATIVE:YES\n", "") + "#GAP:1234.5\n" + absolute));
        Assert.Equal(abs.AudioOffsetSeconds, rel.AudioOffsetSeconds);
        Assert.Equal(abs.Phrases.Select(p => (p.StartSeconds, p.EndSeconds, p.Text)),
            rel.Phrases.Select(p => (p.StartSeconds, p.EndSeconds, p.Text)));
        Assert.Equal(abs.Phrases.SelectMany(p => p.Notes).Select(n => (n.StartSeconds, n.DurationSeconds, n.MidiPitch, n.Type, n.Text)),
            rel.Phrases.SelectMany(p => p.Notes).Select(n => (n.StartSeconds, n.DurationSeconds, n.MidiPitch, n.Type, n.Text)));
    }

    [Fact]
    public void ResettingLocalStartsDoesNotProduceUnsortedWarningButActualBackwardTimeDoes()
    {
        var forward = Parse(": 8 4 0 A\n- 16 20\n: 0 4 0 B\nE");
        Success(forward);
        Assert.DoesNotContain(forward.Diagnostics, d => d.Code == ImportCode.UnsortedNotes);
        var backward = Parse(": 8 4 0 A\n- 16 0\n: 0 4 0 B\nE");
        Success(backward);
        Assert.Contains(backward.Diagnostics, d => d.Code == ImportCode.UnsortedNotes && d.Line == 8);
    }

    [Fact]
    public void ConsecutiveAndEmptyMarkersAdvanceOffsetWithoutCreatingEmptyPhrases()
    {
        var result = Parse("- 0 8\n- 0 12\n: 0 4 0 A\n- 8 4\n- 0 8\n: 0 4 0 B\nE");
        var song = Success(result);
        Assert.Equal(new[] { 2.5, 4d }, song.Phrases.Select(p => p.Notes[0].StartSeconds));
        Assert.Equal(2, song.Phrases.Length);
        Assert.Equal(3, result.Diagnostics.Count(d => d.Code == ImportCode.PhraseMarker));
    }

    [Fact]
    public void RepeatedSoloDeclarationDoesNotResetAccumulatedOffset()
    {
        var song = Success(Parse("P1\n: 0 4 0 A\n- 8 12\nP1\n: 0 4 0 B\n- 8 16\nP1\n: 0 4 0 C\nE"));
        Assert.Equal(new[] { 0d, 1.5, 3.5 }, song.Phrases.Select(p => p.Notes[0].StartSeconds));
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("1.1.0")]
    public void LegacyRelativeModeIsNotReintroducedIntoV1(string version)
    {
        Error(Parse(": 0 4 0 A\n- 8 12\n: 0 4 0 B\nE", $"#VERSION:{version}\n"),
            ImportCode.UnsupportedRelativeTiming, 5);
        var absolute = Headers.Replace("#RELATIVE:YES", "#RELATIVE:NO") + $"#VERSION:{version}\n: 0 4 0 A\nE";
        Success(new UltraStarImporter().Parse(absolute));
    }

    [Theory]
    [InlineData("- 8")]
    [InlineData("- 8 -1")]
    [InlineData("- 8 +1")]
    [InlineData("- 8 1.5")]
    [InlineData("- 8 12 20")]
    [InlineData("- 9007199254740992 0")]
    [InlineData("- 0 9007199254740992")]
    [InlineData("- 0 9223372036854775808")]
    public void MalformedRelativeMarkerDoesNotPublishPartialSong(string marker)
    {
        Error(Parse(": 0 4 0 A\n" + marker + "\n: 0 4 0 B\nE"), ImportCode.InvalidLine, 7);
    }

    [Fact]
    public void AccumulatedMarkerOffsetAndNoteEndLimitsAreCheckedBeforeAddition()
    {
        Error(Parse("- 0 9007199254740991\n- 0 1\nE"), ImportCode.InvalidTiming, 7);
        Error(Parse("- 0 9007199254740991\n- 1 0\nE"), ImportCode.InvalidTiming, 7);
        Error(Parse("- 0 9007199254740991\n: 1 1 0 A\nE"), ImportCode.InvalidTiming, 7);
        Error(Parse("- 0 9007199254740991\n: 0 1 0 A\nE"), ImportCode.InvalidTiming, 7);
        Error(Parse("- 0 16\n: 9007199254740990 1 0 A\nE"), ImportCode.InvalidTiming, 7);
    }

    [Fact]
    public void MarkerTimeOverflowIsAnErrorInBothModes()
    {
        var bpm = "#BPM:0." + new string('0', 299) + "1\n";
        var header = Headers.Replace("#BPM:120\n", bpm);
        Error(new UltraStarImporter().Parse(header + "- 9007199254740991 0\nE"), ImportCode.InvalidTiming, 6);
        Error(new UltraStarImporter().Parse(header.Replace("#RELATIVE:YES\n", "") + "- 9007199254740991\nE"), ImportCode.InvalidTiming, 5);
    }

    [Theory]
    [InlineData("nb-NO", "\n")]
    [InlineData("en-US", "\r\n")]
    [InlineData("nb-NO", "\r")]
    public void RelativeConversionUsesGridUnitsWithFractionalBpmGapAndAllLineEndings(string culture, string ending)
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var input = (Headers.Replace("#BPM:120", "#BPM:10,5") + "#GAP:1234,5\n: 7 7 0 A\n- 21 42\n: 0 7 0 B\nE").Replace("\n", ending);
            var song = Success(new UltraStarImporter().Parse(input));
            Assert.Equal(10, song.Phrases[0].Notes[0].StartSeconds, 10);
            Assert.Equal(10, song.Phrases[0].Notes[0].DurationSeconds, 10);
            Assert.Equal(60, song.Phrases[1].Notes[0].StartSeconds, 10);
            Assert.Equal(1.2345, song.AudioOffsetSeconds, 10);
        }
        finally { CultureInfo.CurrentCulture = before; }
    }

    [Fact]
    public void RelativeTrailerDoesNotChangeOffsetOrDiagnoseIgnoredDuetData()
    {
        var result = Parse(": 0 4 0 A\n- 8 12\n: 0 4 0 B\nE\n- 0 9007199254740991\nP2\n#RELATIVE:NO");
        var song = Success(result);
        Assert.Equal(1.5, song.Phrases[1].Notes[0].StartSeconds);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == ImportCode.UnsupportedDuet || d.Code == ImportCode.InvalidTiming);
    }

    [Fact]
    public void RelativeDuetStillRejectedWithoutFlatteningVoices()
    {
        Error(Parse("P1\n: 0 4 0 A\n- 8 12\nP2\n: 0 4 0 B\nE"), ImportCode.UnsupportedDuet, 9);
    }

    [Fact]
    public async Task RelativeTimingAndOriginalHeadersSurviveEditUndoRedoAndReopenWithoutSource()
    {
        using var files = new ProjectTestDirectory();
        var sourceFile = files.FilePath("relative.txt");
        await File.WriteAllTextAsync(sourceFile, Headers + "#GAP:1500\n#ALBUM:Bevares\n: 0 4 0 A \n: 8 4 0 B\n- 16 20\n: 0 4 0 C\nE");
        var imported = Success(await new UltraStarImporter().LoadAsync(sourceFile));
        var history = new SongHistory(imported);
        history.Execute(new MoveNote(imported.Phrases[0].Notes[0].Id, .25));
        var edited = history.Current;
        history.Undo();
        Assert.Same(imported, history.Current);
        history.Redo();
        Assert.Same(edited, history.Current);
        Assert.Same(imported.ImportedSource, edited.ImportedSource);
        var store = new ProjectStore();
        await store.SaveAsync(files.FilePath(), edited);
        File.Delete(sourceFile); // A reopen must use the saved model, not reparse the source.
        var loaded = await store.LoadAsync(files.FilePath());
        Assert.Equal(2, loaded.SchemaVersion);
        ProjectRoundTripTests.AssertSameSongValues(edited, loaded.Song);
        Assert.Equal(2.5, loaded.Song.Phrases[1].Notes[0].StartSeconds);
        Assert.Equal("YES", loaded.Song.ImportedSource!.Headers.Single(h => h.Name == "RELATIVE").Value);
        Assert.Equal("Bevares", loaded.Song.ImportedSource.Headers.Single(h => h.Name == "ALBUM").Value);
    }
}
