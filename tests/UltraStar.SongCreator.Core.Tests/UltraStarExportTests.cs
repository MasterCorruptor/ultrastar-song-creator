using System.Globalization;
using UltraStar.SongCreator.UltraStar;

namespace UltraStar.SongCreator.Core.Tests;

public class UltraStarExportTests
{
    internal static Song Song() => new()
    {
        Metadata = new() { Title = "Syntetisk øvelse 🎵", Artist = "Test" },
        Media = [new() { Kind = MediaKind.Audio, Location = "audio.wav" }],
        Phrases = [new() { StartSeconds = 0, EndSeconds = 1.5, Notes =
            [new() { StartSeconds = 0, DurationSeconds = .5, Text = "La " },
             new() { StartSeconds = 1, DurationSeconds = .5, MidiPitch = 62, Text = "la" }] }]
    };
    internal static ExportResult Success(ExportResult result)
    {
        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => $"{d.Code}: {d.Message}")));
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == ValidationSeverity.Error);
        return result;
    }
    internal static void Error(ExportResult result, ExportCode code)
    {
        Assert.False(result.Success);
        Assert.Null(result.Text);
        Assert.Contains(result.Diagnostics, d => d.Code == code && d.Severity == ValidationSeverity.Error);
    }
    internal static Song Imported(ExportResult result)
    {
        var parsed = new UltraStarImporter().Parse(Success(result).Text!);
        Assert.True(parsed.Success, string.Join("; ", parsed.Diagnostics.Select(d => d.Message)));
        return parsed.Song!;
    }

    [Theory]
    [InlineData(ExportFormat.Unversioned)]
    [InlineData(ExportFormat.V1)]
    public void HandwrittenExpectedFileAndStableOutput(ExportFormat format)
    {
        var song = Song();
        var result = Success(new UltraStarExporter().Render(song, new(format, 120)));
        var expected = (format == ExportFormat.V1 ? "#VERSION:1.0.0\n" : "") +
            "#TITLE:Syntetisk øvelse 🎵\n#ARTIST:Test\n#BPM:120\n#GAP:0\n#MP3:audio.wav\n: 0 4 0 La \n: 8 4 2 la\nE\n";
        Assert.Equal(expected, result.Text);
        Assert.Equal(result.Text, Success(new UltraStarExporter().Render(song, new(format, 120))).Text);
        Assert.All(result.Notes, n => { Assert.Equal(0, n.StartDeltaSeconds); Assert.Equal(0, n.EndDeltaSeconds); Assert.Equal(0, n.DurationDeltaSeconds); });
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain('\r', result.Text!);
    }

    [Theory]
    [InlineData("unversioned.txt", ExportFormat.Unversioned)]
    [InlineData("unversioned.txt", ExportFormat.V1)]
    [InlineData("v1.txt", ExportFormat.Unversioned)]
    [InlineData("v1.txt", ExportFormat.V1)]
    [InlineData("relative.txt", ExportFormat.Unversioned)]
    [InlineData("relative.txt", ExportFormat.V1)]
    public async Task ImportExportImportPreservesSemanticNotesAndMetadata(string fixture, ExportFormat format)
    {
        var path = Path.Combine(ProjectTestDirectory.RepositoryRoot, "tests", "UltraStar.SongCreator.Core.Tests", "Fixtures", fixture);
        var original = (await new UltraStarImporter().LoadAsync(path)).Song!;
        var output = Success(new UltraStarExporter().Render(original, new(format)));
        var reopened = Imported(output);
        Assert.Equal(original.Metadata, reopened.Metadata);
        Assert.Equal(original.AudioOffsetSeconds, reopened.AudioOffsetSeconds, 10);
        Assert.Equal(original.VideoOffsetSeconds, reopened.VideoOffsetSeconds, 10);
        Assert.Equal(original.Media.Select(m => (m.Kind, m.Location)), reopened.Media.Select(m => (m.Kind, m.Location)));
        Assert.Equal(original.Phrases.Select(p => p.Text), reopened.Phrases.Select(p => p.Text));
        var before = original.Phrases.SelectMany(p => p.Notes).ToArray();
        var after = reopened.Phrases.SelectMany(p => p.Notes).ToArray();
        Assert.Equal(before.Length, after.Length);
        for (var i = 0; i < before.Length; i++)
        {
            Assert.Equal(before[i].StartSeconds, after[i].StartSeconds, 10);
            Assert.Equal(before[i].DurationSeconds, after[i].DurationSeconds, 10);
            Assert.Equal(before[i].MidiPitch, after[i].MidiPitch);
            Assert.Equal(before[i].Type, after[i].Type);
            Assert.Equal(before[i].Text, after[i].Text);
        }
        Assert.Equal(format == ExportFormat.V1 ? "ultrastar-v1" : "ultrastar-unversioned", reopened.ImportedSource!.FormatId);
        foreach (var header in original.ImportedSource!.Headers.Where(h => h.Name is "ALBUM" or "COVER" or "TEST-CUSTOM" or "CREATOR" or "COMMENT"))
            Assert.Contains(header, reopened.ImportedSource.Headers);
        Assert.DoesNotContain(reopened.ImportedSource.Headers, h => h.Name == "RELATIVE" && h.Value == "YES");
    }

    [Fact]
    public void RoundingUsesEndpointsAndReportsActualStartEndAndDurationDeltas()
    {
        var song = Song();
        var first = song.Phrases[0].Notes[0] with { StartSeconds = .07, DurationSeconds = .22 };
        song = SongFixture.WithFirstNote(song, first);
        var result = Success(new UltraStarExporter().Render(song, new(ExportFormat.V1, 120)));
        var q = result.Notes[0];
        Assert.Equal(1, q.StartBeat);
        Assert.Equal(2, q.EndBeat);
        Assert.Equal(.125, q.ExportedStartSeconds);
        Assert.Equal(.25, q.ExportedEndSeconds);
        Assert.Equal(.125, q.ExportedDurationSeconds);
        Assert.Equal(.055, q.StartDeltaSeconds, 12);
        Assert.Equal(-.04, q.EndDeltaSeconds, 12);
        Assert.Equal(-.095, q.DurationDeltaSeconds, 12);
        Assert.Contains(result.Diagnostics, d => d.Code == ExportCode.TimingRounded && d.EntityId == first.Id);
        var reread = Imported(result).Phrases[0].Notes[0];
        Assert.Equal(q.ExportedStartSeconds, reread.StartSeconds);
        Assert.Equal(q.ExportedEndSeconds, reread.EndSeconds);
        Assert.Equal(q.ExportedDurationSeconds, reread.DurationSeconds);
        Assert.Equal(.07, first.StartSeconds);
        Assert.Equal(.22, first.DurationSeconds);
    }

    [Fact]
    public void MidpointRoundsAwayFromZeroAndAdjacentNotesRemainAdjacent()
    {
        var song = Song() with { Phrases = [new() { StartSeconds = 0, EndSeconds = 1, Notes =
            [new() { StartSeconds = .0625, DurationSeconds = .125, Text = "A" },
             new() { StartSeconds = .1875, DurationSeconds = .125, Text = "B" }] }] };
        var result = Success(new UltraStarExporter().Render(song, new(ExportFormat.Unversioned, 120)));
        Assert.Equal((1L, 2L), (result.Notes[0].StartBeat, result.Notes[0].EndBeat));
        Assert.Equal((2L, 3L), (result.Notes[1].StartBeat, result.Notes[1].EndBeat));
        var reread = Imported(result);
        Assert.Equal(reread.Phrases[0].Notes[0].EndSeconds, reread.Phrases[0].Notes[1].StartSeconds);
    }

    [Fact]
    public void CollapsedNoteIsNotDeletedOrArtificiallyLengthened()
    {
        var song = SongFixture.WithFirstNote(Song(), new() { StartSeconds = .01, DurationSeconds = .01, Text = "tiny" });
        Error(new UltraStarExporter().Render(song, new(ExportFormat.V1, 120)), ExportCode.CollapsedNote);
    }

    [Fact]
    public void ExportUsesCurrentEditsAndNeverMutatesRawAnalysisOrSourceHeaders()
    {
        var song = SongFixture.Create() with
        {
            AudioOffsetSeconds = .25,
            ImportedSource = new() { FormatId = "ultrastar-v1", Headers =
                [new("VERSION", "1.1.0"), new("TITLE", "Old"), new("ARTIST", "Old"), new("BPM", "120"),
                 new("MP3", "stale.wav"), new("AUDIO", "stale.flac"), new("COVER", "cover.png"),
                 new("LANGUAGE", "Old"), new("X", "one"), new("X", "two")] }
        };
        var source = song.ImportedSource;
        var analysis = song.Analysis;
        var edited = new MoveNote(song.Phrases[0].Notes[0].Id, 1.125).Apply(song) with
        {
            Metadata = new() { Title = "New", Artist = "New artist", Language = null },
            Media = [song.Media[0] with { Location = "new.wav" }]
        };
        var result = Success(new UltraStarExporter().Render(edited, new(ExportFormat.V1)));
        var reopened = Imported(result);
        Assert.Equal("New", reopened.Metadata.Title);
        Assert.Equal("New artist", reopened.Metadata.Artist);
        Assert.Null(reopened.Metadata.Language);
        Assert.Equal("new.wav", reopened.Media[0].Location);
        Assert.Equal(1.125, reopened.Phrases[0].Notes[0].StartSeconds);
        Assert.Same(source, edited.ImportedSource);
        Assert.Same(analysis, edited.Analysis);
        Assert.Equal("Old", source!.Headers.Single(h => h.Name == "TITLE").Value);
        Assert.Equal(.8, edited.Phrases[0].Notes[0].Confidence);
        Assert.Contains(result.Diagnostics, d => d.Code == ExportCode.ProjectOnlyData);
        Assert.Contains(result.Diagnostics, d => d.Code == ExportCode.PhraseBoundsDerived);
        Assert.Equal(new[] { "one", "two" }, reopened.ImportedSource!.Headers.Where(h => h.Name == "X").Select(h => h.Value));
    }

    [Fact]
    public void NewSongDoesNotGuessQuantizationGridFromMusicalTempo()
    {
        var song = Song() with { BeatsPerMinute = 120 };
        Error(new UltraStarExporter().Render(song, new(ExportFormat.V1)), ExportCode.MissingGrid);
        Success(new UltraStarExporter().Render(song, new(ExportFormat.V1, 120)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidGridIsExplicit(double grid)
    {
        Error(new UltraStarExporter().Render(Song(), new(ExportFormat.V1, grid)), ExportCode.InvalidOptions);
    }

    [Fact]
    public void GridChangesRebaseMedleyBeatMetadata()
    {
        var song = Song() with { ImportedSource = new() { FormatId = "ultrastar-v1", Headers =
            [new("BPM", "120"), new("MEDLEYSTARTBEAT", "8"), new("MEDLEYENDBEAT", "16")] } };
        var result = Success(new UltraStarExporter().Render(song, new(ExportFormat.V1, 240)));
        Assert.Contains("#MEDLEYSTARTBEAT:16\n", result.Text!);
        Assert.Contains("#MEDLEYENDBEAT:32\n", result.Text!);
        Assert.Equal((0L, 8L), (result.Notes[0].StartBeat, result.Notes[0].EndBeat));
        Assert.Equal(1, Imported(result).Phrases[0].Notes[1].StartSeconds);
    }

    [Theory]
    [InlineData("bogus", false)]
    [InlineData("8", true)]
    public void AmbiguousMedleyIsNotSilentlyReactivatedOrGuessed(string beat, bool relative)
    {
        var song = Song() with { ImportedSource = new() { FormatId = "ultrastar-unversioned", Headers =
            [new("BPM", "120"), new("RELATIVE", relative ? "YES" : "NO"), new("MEDLEYSTARTBEAT", beat)] } };
        Error(new UltraStarExporter().Render(song, new(ExportFormat.V1)), ExportCode.InvalidMedley);
    }

    [Theory]
    [InlineData("nb-NO")]
    [InlineData("en-US")]
    public void DecimalHeadersAreInvariantWithoutExponentNotation(string culture)
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var baseline = Song();
            var song = baseline with { AudioOffsetSeconds = 1e-10, VideoOffsetSeconds = -.25,
                Media = baseline.Media.Add(new() { Kind = MediaKind.Video, Location = "video.mp4" }) };
            var result = Success(new UltraStarExporter().Render(song, new(ExportFormat.V1, 120)));
            Assert.Contains("#GAP:0.", result.Text!);
            Assert.DoesNotContain("E-", result.Text!);
            var parsed = Imported(result);
            Assert.Equal(1e-10, parsed.AudioOffsetSeconds, 15);
            Assert.Equal(-.25, parsed.VideoOffsetSeconds, 12);
        }
        finally { CultureInfo.CurrentCulture = before; }
    }

    [Fact]
    public void NegativeZeroIsEmittedAsUnsignedZero()
    {
        var result = Success(new UltraStarExporter().Render(Song() with { AudioOffsetSeconds = -0d }, new(ExportFormat.V1, 120)));
        Assert.Contains("#GAP:0\n", result.Text!);
        Imported(result);
    }

    [Fact]
    public void LargeGridBpmUsesPlainDecimalAndReparses()
    {
        var song = Song() with { Phrases = [new() { StartSeconds = 0, EndSeconds = 1e-18, Notes =
            [new() { StartSeconds = 0, DurationSeconds = 1e-18, Text = "A" }] }] };
        var result = Success(new UltraStarExporter().Render(song, new(ExportFormat.V1, 1e20)));
        Assert.Contains("#BPM:100000000000000000000\n", result.Text!);
        Imported(result);
    }

    [Fact]
    public void EmptyPhraseAndNonrepresentableTimeAreExplicitErrors()
    {
        var song = Song() with { Phrases = [new() { StartSeconds = 0, EndSeconds = 1, Notes = [] }, Song().Phrases[0]] };
        Error(new UltraStarExporter().Render(song, new(ExportFormat.V1, 120)), ExportCode.EmptyPhrase);
        Error(new UltraStarExporter().Render(Song(), new(ExportFormat.V1, 1e20)), ExportCode.UnrepresentableTiming);
    }

    [Fact]
    public void MissingMetadataNotesMediaAndAmbiguousMediaBlockExport()
    {
        var writer = new UltraStarExporter();
        Error(writer.Render(Song() with { Metadata = new() }, new(ExportFormat.V1, 120)), ExportCode.MissingMetadata);
        Error(writer.Render(Song() with { Phrases = [] }, new(ExportFormat.V1, 120)), ExportCode.MissingNotes);
        Error(writer.Render(Song() with { Media = [] }, new(ExportFormat.V1, 120)), ExportCode.MissingMedia);
        var song = Song();
        Error(writer.Render(song with { Media = song.Media.Add(new() { Location = "other.wav" }) }, new(ExportFormat.V1, 120)), ExportCode.AmbiguousMedia);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("A\n#BPM:999")]
    [InlineData("A\tB")]
    public void InvalidNoteTextCannotInjectLinesOrProduceInvalidUtf8(string? text)
    {
        var song = SongFixture.WithFirstNote(Song(), Song().Phrases[0].Notes[0] with { Text = text! });
        Error(new UltraStarExporter().Render(song, new(ExportFormat.V1, 120)), ExportCode.InvalidText);
    }

    [Theory]
    [InlineData("TITLE", "A\n#MP3:other.wav")]
    [InlineData("X:Y", "value")]
    [InlineData(" TITLE ", "value")]
    [InlineData("COVER", "a.png\nE")]
    public void MalformedSourceHeadersCannotCreateNewLinesOrConflictingKeys(string name, string value)
    {
        var song = Song() with { ImportedSource = new() { FormatId = "ultrastar-v1", Headers = [new(name, value)] } };
        Error(new UltraStarExporter().Render(song, new(ExportFormat.V1, 120)), ExportCode.InvalidHeader);
    }

    [Fact]
    public void CoreErrorsNegativeGapAndDuetHeadersRemainErrors()
    {
        var writer = new UltraStarExporter();
        Error(writer.Render(Song() with { AudioOffsetSeconds = -.25 }, new(ExportFormat.V1, 120)), ExportCode.InvalidOffset);
        Error(writer.Render(Song() with { AudioOffsetSeconds = double.MaxValue }, new(ExportFormat.V1, 120)), ExportCode.InvalidOffset);
        var song = SongFixture.WithFirstNote(Song(), Song().Phrases[0].Notes[0] with { MidiPitch = 128 });
        Error(writer.Render(song, new(ExportFormat.V1, 120)), ExportCode.InvalidSong);
        song = Song() with { ImportedSource = new() { FormatId = "ultrastar-v1", Headers = [new("P2", "Other")] } };
        Error(writer.Render(song, new(ExportFormat.V1, 120)), ExportCode.UnsupportedDuet);
    }

    [Fact]
    public void LimitsUnsupportedSourcesAndHeaderRangesAreChecked()
    {
        Error(new UltraStarExporter(10).Render(Song(), new(ExportFormat.V1, 120)), ExportCode.OutputTooLarge);
        Error(new UltraStarExporter().Render(Song(), new((ExportFormat)99, 120)), ExportCode.InvalidOptions);
        var song = Song() with { ImportedSource = new() { FormatId = "other", Headers = [] } };
        Error(new UltraStarExporter().Render(song, new(ExportFormat.V1, 120)), ExportCode.UnsupportedSource);
        song = song with { ImportedSource = new() { FormatId = "ultrastar-v1", Headers = [new("START", "-1")] } };
        Error(new UltraStarExporter().Render(song, new(ExportFormat.V1, 120)), ExportCode.InvalidHeader);
        Assert.Throws<ArgumentOutOfRangeException>(() => new UltraStarExporter(0));
    }

    [Fact]
    public void SourceEncodingAndRelativeFlagsFollowOutputProfile()
    {
        var song = Song() with { ImportedSource = new() { FormatId = "ultrastar-unversioned", Headers =
            [new("BPM", "120"), new("ENCODING", "CP1252"), new("RELATIVE", "YES"), new("VERSION", "1.1.0")] } };
        var writer = new UltraStarExporter();
        var legacy = Success(writer.Render(song, new(ExportFormat.Unversioned)));
        Assert.Contains("#ENCODING:UTF-8\n", legacy.Text!);
        Assert.Contains("#RELATIVE:NO\n", legacy.Text!);
        Assert.DoesNotContain("#VERSION", legacy.Text!);
        var v1 = Success(writer.Render(song, new(ExportFormat.V1)));
        Assert.StartsWith("#VERSION:1.0.0\n", v1.Text!);
        Assert.DoesNotContain("#ENCODING", v1.Text!);
        Assert.DoesNotContain("#RELATIVE", v1.Text!);
        Imported(legacy);
        Imported(v1);
    }

    [Fact]
    public void InvalidUnicodeIsConstructedAtRuntimeWithoutAttributeMetadataReplacement()
    {
        var invalid = new string((char)0xD800, 1);
        var writer = new UltraStarExporter();
        var song = SongFixture.WithFirstNote(Song(), Song().Phrases[0].Notes[0] with { Text = invalid });
        Error(writer.Render(song, new(ExportFormat.V1, 120)), ExportCode.InvalidText);
        song = Song() with { ImportedSource = new() { FormatId = "ultrastar-v1", Headers = [new("X", invalid)] } };
        Error(writer.Render(song, new(ExportFormat.V1, 120)), ExportCode.InvalidHeader);
    }
}
