using System.Globalization;
using System.Text;
using UltraStar.SongCreator.Projects;
using UltraStar.SongCreator.UltraStar;

namespace UltraStar.SongCreator.Core.Tests;

public class UltraStarImportTests
{
    private const string Headers = "#TITLE:Test\n#ARTIST:Test\n#MP3:missing.wav\n#BPM:120\n";
    private static string Fixture(string name) => Path.Combine(ProjectTestDirectory.RepositoryRoot,
        "tests", "UltraStar.SongCreator.Core.Tests", "Fixtures", name);
    private static ImportResult Parse(string body, string extra = "") =>
        new UltraStarImporter().Parse(Headers + extra + body);
    private static Song Success(ImportResult result)
    {
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => $"{d.Code}@{d.Line}: {d.Message}")));
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == ValidationSeverity.Error);
        return Assert.IsType<Song>(result.Song);
    }
    private static ImportDiagnostic Error(ImportResult result, ImportCode code)
    {
        Assert.False(result.Success);
        Assert.Null(result.Song);
        return Assert.Single(result.Diagnostics.Where(d => d.Code == code && d.Severity == ValidationSeverity.Error));
    }

    [Fact]
    public async Task HandwrittenLegacyFixtureMapsUnitsTypesPhrasesAndEveryHeader()
    {
        var song = Success(await new UltraStarImporter().LoadAsync(Fixture("unversioned.txt")));
        Assert.Equal("Syntetisk prøve", song.Metadata.Title);
        Assert.Equal("Test", song.Metadata.Artist);
        Assert.Equal(.25, song.AudioOffsetSeconds);
        Assert.Equal(.75, song.VideoOffsetSeconds);
        Assert.Null(song.BeatsPerMinute);
        Assert.Equal(new[] { "media/test.wav", "media/test.mp4" }, song.Media.Select(m => m.Location));
        Assert.Equal(2, song.Phrases.Length);
        var notes = song.Phrases.SelectMany(p => p.Notes).ToArray();
        Assert.Equal(new[] { NoteType.Normal, NoteType.Golden, NoteType.Rap, NoteType.GoldenRap, NoteType.Freestyle }, notes.Select(n => n.Type));
        Assert.Equal(new[] { 60, 58, 60, 60, 60 }, notes.Select(n => n.MidiPitch));
        Assert.Equal(new[] { 0d, 1d, 2d, 2.5, 3d }, notes.Select(n => n.StartSeconds));
        Assert.All(notes, n => Assert.Equal(.5, n.DurationSeconds));
        Assert.All(notes, n => { Assert.Null(n.Confidence); Assert.Empty(n.AnalysisReferences); });
        Assert.Equal("La la", song.Phrases[0].Text);
        Assert.Equal(1.5, song.Phrases[0].EndSeconds); // Marker at beat 13 is not a note boundary.
        Assert.Equal(13, song.ImportedSource!.Headers.Length);
        Assert.Equal(new[] { "a:b", "second" }, song.ImportedSource.Headers.Where(h => h.Name == "TEST-CUSTOM").Select(h => h.Value));
        Assert.Equal("", song.ImportedSource.Headers.Single(h => h.Name == "COMMENT").Value);
        Assert.Equal(Path.GetFullPath(Fixture("unversioned.txt")), song.ImportedSource.FileReference);
        Assert.DoesNotContain(SongValidator.Validate(song), i => i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public async Task V1FractionalCommaGridAudioOverrideUnicodeAndTrailer()
    {
        var song = Success(await new UltraStarImporter().LoadAsync(Fixture("v1.txt")));
        Assert.Equal("ultrastar-v1", song.ImportedSource!.FormatId);
        Assert.Equal("V1 øvelse 🎵", song.Metadata.Title);
        Assert.Equal("Norwegian", song.Metadata.Language);
        Assert.Equal("media/preferred.flac", Assert.Single(song.Media).Location);
        Assert.Equal(1.2345, song.AudioOffsetSeconds, 12);
        Assert.Equal(60, song.Phrases[0].Notes[0].StartSeconds, 10);
        Assert.Equal(30, song.Phrases[0].Notes[0].DurationSeconds, 10);
        Assert.Equal(65, song.Phrases[0].Notes[0].MidiPitch);
        Assert.Equal(" Hei ", song.Phrases[0].Text);
        Assert.Equal("keep:colon", song.ImportedSource.Headers.Single(h => h.Name == "TEST-CUSTOM").Value);
        Assert.DoesNotContain(song.ImportedSource.Headers, h => h.Name == "P2");
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void AllLineEndingsBomEmptyLinesAndEofAreAccepted(string ending)
    {
        var input = "\uFEFF\n \t\n" + Headers + ": 0 4 0 La \nE";
        var song = Success(new UltraStarImporter().Parse(input.Replace("\n", ending)));
        Assert.Equal("La ", Assert.Single(Assert.Single(song.Phrases).Notes).Text);
    }

    [Fact]
    public void EofWithoutEndMarkerWarnsButPreservesLastPhrase()
    {
        var result = Parse(": 0 4 0 La");
        Assert.Single(Success(result).Phrases);
        Assert.Contains(result.Diagnostics, d => d.Code == ImportCode.MissingEndMarker && d.Line is null);
    }

    [Theory]
    [InlineData("-60", 0)]
    [InlineData("67", 127)]
    public void MidiBoundaryMappingIsExact(string sourcePitch, int midi)
    {
        var song = Success(Parse($": 0 4 {sourcePitch} La\nE"));
        Assert.Equal(midi, song.Phrases[0].Notes[0].MidiPitch);
    }

    [Theory]
    [InlineData("-61")]
    [InlineData("68")]
    public void ScoredPitchIsNotSilentlyClamped(string pitch)
    {
        Assert.Equal(5, Error(Parse($": 0 4 {pitch} La\nE"), ImportCode.InvalidPitch).Line);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1e2")]
    [InlineData("120,5.5")]
    public void BadGridBaseDoesNotProduceSong(string bpm)
    {
        var result = new UltraStarImporter().Parse(Headers.Replace("120", bpm) + ": 0 4 0 La\nE");
        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Line == 4 && d.Severity == ValidationSeverity.Error);
    }

    [Theory]
    [InlineData(": -1 4 0 La")]
    [InlineData(": 0 0 0 La")]
    [InlineData(": 0 -1 0 La")]
    [InlineData(": 0 4 +1 La")]
    [InlineData(": 0 4 1.5 La")]
    [InlineData(": 9223372036854775808 4 0 La")]
    [InlineData(": 9007199254740991 1 0 La")]
    [InlineData(": 0 9007199254740992 0 La")]
    [InlineData(": 0 4")]
    [InlineData("X 0 4 0 La")]
    [InlineData(": 0 4 0 La\tla")]
    public void MalformedOrUnrepresentableNoteGivesLineError(string line)
    {
        Assert.Equal(5, Error(Parse(line + "\nE"), ImportCode.InvalidNote).Line);
    }

    [Theory]
    [InlineData("2.0.0")]
    [InlineData("0.0.0")]
    [InlineData("1.0")]
    [InlineData("1.0.0-alpha")]
    [InlineData("future")]
    public void UnsupportedVersionIsNotGuessed(string version)
    {
        Error(Parse(": 0 4 0 La\nE", $"#VERSION:{version}\n"), ImportCode.UnsupportedVersion);
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("1.1.0")]
    [InlineData("1.9.12")]
    public void SameMajorVersionAcceptedWithOriginalVersionPreserved(string version)
    {
        var song = Success(Parse(": 0 4 0 La\nE", $"#VERSION:{version}\n"));
        Assert.Equal(version, song.ImportedSource!.Headers.Single(h => h.Name == "VERSION").Value);
    }

    [Theory]
    [InlineData("TITLE")]
    [InlineData("ARTIST")]
    [InlineData("BPM")]
    [InlineData("MP3")]
    public void RequiredHeaderMissingOrEmptyIsExplicit(string name)
    {
        var input = string.Join('\n', Headers.Split('\n').Where(l => !l.StartsWith("#" + name + ":"))) + "\nE";
        Assert.Contains("#" + name, Error(new UltraStarImporter().Parse(input), ImportCode.MissingHeader).Message);
    }

    [Theory]
    [InlineData("#P2:Other\n")]
    [InlineData("#P9:Other\n")]
    [InlineData("#DUETSINGERP2:Other\n")]
    public void DuetMetadataNotFlattened(string extra)
    {
        Error(Parse(": 0 4 0 La\nE", extra), ImportCode.UnsupportedDuet);
    }

    [Theory]
    [InlineData("P2")]
    [InlineData("P0")]
    [InlineData("P9")]
    public void OtherVoiceNotFlattenedEvenAfterSoloNotes(string voice)
    {
        Assert.Equal(6, Error(Parse($": 0 4 0 La\n{voice}\n: 8 4 0 La\nE"), ImportCode.UnsupportedDuet).Line);
    }

    [Fact]
    public void ExplicitSoloVoiceDoesNotSplitPhrase()
    {
        var song = Success(Parse("P1\n: 0 4 0 La \nP1\n: 8 4 0 la\nE", "#VERSION:1.0.0\n#P1:Solo\n"));
        Assert.Equal("La la", Assert.Single(song.Phrases).Text);
        Error(Parse("P1\n: 0 4 0 La\nE", "#VERSION:1.0.0\n"), ImportCode.MissingHeader);
    }

    [Fact]
    public void RelativeModeNeverSilentlyInterpretedAsAbsolute()
    {
        Error(Parse(": 0 4 0 La\nE", "#RELATIVE:yEs\n"), ImportCode.UnsupportedRelativeTiming);
        Error(Parse(": 0 4 0 La\n- 8 16\nE"), ImportCode.InvalidLine);
        Success(Parse(": 0 4 0 La\nE", "#RELATIVE:no\n"));
    }

    [Fact]
    public void OperationalDuplicatesAreErrorsUnknownDuplicatesArePreserved()
    {
        Error(Parse(": 0 4 0 La\nE", "#bpm:120\n"), ImportCode.DuplicateHeader);
        var song = Success(Parse(": 0 4 0 La\nE", "#BPM:\n#X:a\n#X:b\n"));
        Assert.Equal(2, song.ImportedSource!.Headers.Count(h => h.Name == "X"));
    }

    [Fact]
    public void PhraseGroupingDoesNotMoveNotesToMarkerAndWarningsRetainSourceOrder()
    {
        var result = Parse(": 8 4 0 late \n: 0 12 0 early\n- 3\n- 4\n* 16 4 0 next\nE");
        var song = Success(result);
        Assert.Equal(2, song.Phrases.Length);
        Assert.Equal("late early", song.Phrases[0].Text);
        Assert.Equal(0, song.Phrases[0].StartSeconds);
        Assert.Equal(1.5, song.Phrases[0].EndSeconds);
        Assert.Contains(result.Diagnostics, d => d.Code == ImportCode.UnsortedNotes && d.Line == 6);
        Assert.Contains(result.Diagnostics, d => d.Code == ImportCode.OverlappingNotes && d.Line is not null);
        Assert.Equal(2, result.Diagnostics.Count(d => d.Code == ImportCode.PhraseMarker));
    }

    [Fact]
    public void SingleTextDelimiterPreservesLeadingTrailingSpacesAndUnicodeWhitespaceInV1()
    {
        var song = Success(Parse(":\u00A00\u00A04\u00A00\u00A0 Hei \nE", "#VERSION:1.0.0\n"));
        Assert.Equal(" Hei ", song.Phrases[0].Text);
        Error(Parse(":\u00A00\u00A04\u00A00\u00A0La\nE"), ImportCode.InvalidNote);
        Assert.Equal(" La ", Success(Parse(":\t0\t4\t0\t La \nE")).Phrases[0].Text);
    }

    [Theory]
    [InlineData("nb-NO")]
    [InlineData("en-US")]
    public void NumericMappingIndependentOfProcessCulture(string culture)
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var song = Success(new UltraStarImporter().Parse(Headers.Replace("120", "10,5") + "#GAP:1.5\n: 42 21 0 La\nE"));
            Assert.Equal(60, song.Phrases[0].StartSeconds, 10);
            Assert.Equal(.0015, song.AudioOffsetSeconds);
        }
        finally { CultureInfo.CurrentCulture = before; }
    }

    [Fact]
    public void EmptyTextWarningsAndPlaybackRangeStayAvailable()
    {
        var result = Parse(": 0 4 0\nF 8 4 0\nE", "#START:3\n#END:10000\n");
        Assert.Equal(2, Success(result).Phrases[0].Notes.Length);
        Assert.Single(result.Diagnostics.Where(d => d.Code == ImportCode.MissingText));
        Assert.Equal(2, result.Diagnostics.Count(d => d.Code == ImportCode.RetainedPlaybackRange));
    }

    [Fact]
    public void HeaderAfterBodyAndMalformedHeaderAreErrors()
    {
        Error(Parse(": 0 4 0 La\n#COVER:test.png\nE"), ImportCode.UnexpectedHeader);
        Error(Parse(": 0 4 0 La\nE", "#:value\n"), ImportCode.InvalidHeader);
    }

    [Theory]
    [InlineData("#GAP:-1\n")]
    [InlineData("#GAP:1e2\n")]
    [InlineData("#VIDEOGAP:NaN\n")]
    [InlineData("#RELATIVE:maybe\n")]
    public void InvalidOptionalTimingNotIgnored(string extra)
    {
        Error(Parse(": 0 4 0 La\nE", extra), ImportCode.InvalidHeader);
    }

    [Theory]
    [InlineData("CP1252", 1252, "Blå øvelse")]
    [InlineData("cp1250", 1250, "Łódź")]
    public async Task ExplicitLegacyEncodingAppliesEvenToEarlierHeaders(string name, int page, string title)
    {
        using var files = new ProjectTestDirectory();
        var input = Headers.Replace("#TITLE:Test", "#TITLE:" + title) + $"#ENCODING:{name}\n: 0 4 0 {title}\nE";
        var encoding = CodePagesEncodingProvider.Instance.GetEncoding(page)!;
        await File.WriteAllBytesAsync(files.FilePath("encoded.txt"), encoding.GetBytes(input));
        var song = Success(await new UltraStarImporter().LoadAsync(files.FilePath("encoded.txt")));
        Assert.Equal(title, song.Metadata.Title);
        Assert.Equal(title, song.Phrases[0].Text);
    }

    [Fact]
    public async Task UndeclaredInvalidUtf8IsNotGuessedAndV1EncodingHeaderDoesNotChangeUtf8()
    {
        using var files = new ProjectTestDirectory();
        var cp = CodePagesEncodingProvider.Instance.GetEncoding(1252)!;
        var input = Headers.Replace("#TITLE:Test", "#TITLE:Blå") + ": 0 4 0 La\nE";
        await File.WriteAllBytesAsync(files.FilePath("invalid.txt"), cp.GetBytes(input));
        Error(await new UltraStarImporter().LoadAsync(files.FilePath("invalid.txt")), ImportCode.InvalidEncoding);
        await File.WriteAllBytesAsync(files.FilePath("v1.txt"), cp.GetBytes("#VERSION:1.0.0\n#ENCODING:CP1252\n" + input));
        Error(await new UltraStarImporter().LoadAsync(files.FilePath("v1.txt")), ImportCode.InvalidEncoding);
        Success(Parse(": 0 4 0 Æ\nE", "#VERSION:1.0.0\n#ENCODING:CP1252\n"));
    }

    [Fact]
    public async Task Utf8BomFilesAndEncodingErrorsAreExplicit()
    {
        using var files = new ProjectTestDirectory();
        var input = Headers + ": 0 4 0 Æ\nE";
        await File.WriteAllTextAsync(files.FilePath("bom.txt"), input, new UTF8Encoding(true));
        Success(await new UltraStarImporter().LoadAsync(files.FilePath("bom.txt")));
        Error(Parse(": 0 4 0 La\nE", "#ENCODING:CP866\n"), ImportCode.UnsupportedEncoding);
        await File.WriteAllTextAsync(files.FilePath("bad-encoding.txt"), Headers + "#ENCODING:CP866\nE");
        Error(await new UltraStarImporter().LoadAsync(files.FilePath("bad-encoding.txt")), ImportCode.UnsupportedEncoding);
    }

    [Fact]
    public async Task ByteLimitsCancellationAndIoErrorsDoNotCreatePartialProjects()
    {
        using var files = new ProjectTestDirectory();
        var importer = new UltraStarImporter(100);
        var input = Headers + ": 0 4 0 " + new string('Æ', 100) + "\nE";
        Error(importer.Parse(input), ImportCode.InputTooLarge);
        await File.WriteAllTextAsync(files.FilePath("large.txt"), input);
        Error(await importer.LoadAsync(files.FilePath("large.txt")), ImportCode.InputTooLarge);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => importer.LoadAsync(files.FilePath("large.txt"), cancellation.Token));
        await Assert.ThrowsAsync<FileNotFoundException>(() => importer.LoadAsync(files.FilePath("missing.txt")));
        Error(new UltraStarImporter().Parse(Headers + "\ud800"), ImportCode.InvalidEncoding);
        Assert.Throws<ArgumentOutOfRangeException>(() => new UltraStarImporter(0));
    }

    [Fact]
    public async Task ImportEditUndoRedoSaveAndReopenPreservesSourceMetadataWithoutReadingMedia()
    {
        using var files = new ProjectTestDirectory();
        var importer = new UltraStarImporter();
        var song = Success(await importer.LoadAsync(Fixture("unversioned.txt")));
        var history = new SongHistory(song);
        history.Execute(new MoveNote(song.Phrases[0].Notes[0].Id, .25));
        Assert.Same(song.ImportedSource, history.Current.ImportedSource);
        var edited = history.Current;
        history.Undo();
        Assert.Same(song, history.Current);
        history.Redo();
        Assert.Same(edited, history.Current);
        var store = new ProjectStore();
        await store.SaveAsync(files.FilePath(), edited);
        var reopened = await store.LoadAsync(files.FilePath());
        Assert.Equal(2, reopened.SchemaVersion);
        ProjectRoundTripTests.AssertSameSongValues(edited, reopened.Song);
        Assert.Equal("Eksempel", reopened.Song.ImportedSource!.Headers.Single(h => h.Name == "ALBUM").Value);
        Assert.False(File.Exists(Path.Combine(files.DirectoryPath, "media", "test.wav")));
    }

    [Fact]
    public async Task TrailerIsIgnoredBeforeDecodingAndBomEncodingConflictIsRejected()
    {
        using var files = new ProjectTestDirectory();
        var input = Encoding.UTF8.GetBytes(Headers + ": 0 4 0 La\nE\r\n");
        await File.WriteAllBytesAsync(files.FilePath("trailer.txt"), input.Concat(new byte[] { 0xFF, 0xFE }).ToArray());
        Success(await new UltraStarImporter().LoadAsync(files.FilePath("trailer.txt")));
        await File.WriteAllTextAsync(files.FilePath("conflict.txt"), Headers + "#ENCODING:CP1252\nE", new UTF8Encoding(true));
        Error(await new UltraStarImporter().LoadAsync(files.FilePath("conflict.txt")), ImportCode.InvalidEncoding);
    }

    [Fact]
    public async Task V1UnicodeHeaderWhitespaceDoesNotAccidentallyEnableLegacyEncoding()
    {
        using var files = new ProjectTestDirectory();
        var input = "#\u00A0VERSION\u00A0:\u00A01.0.0\n#ENCODING:CP1252\n" + Headers +
            ": 0 4 0 Æ ø 🎵\nE";
        await File.WriteAllTextAsync(files.FilePath("unicode-header.txt"), input, new UTF8Encoding(false, true));
        var song = Success(await new UltraStarImporter().LoadAsync(files.FilePath("unicode-header.txt")));
        Assert.Equal("ultrastar-v1", song.ImportedSource!.FormatId);
        Assert.Equal("Æ ø 🎵", song.Phrases[0].Text);
    }
}
