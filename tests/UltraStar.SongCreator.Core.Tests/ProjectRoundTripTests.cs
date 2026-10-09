using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json.Nodes;
using UltraStar.SongCreator.Projects;

namespace UltraStar.SongCreator.Core.Tests;

public class ProjectRoundTripTests
{
    [Fact]
    public async Task HandwrittenV1FixtureLoadsIndependentOfWriter()
    {
        using var files = new ProjectTestDirectory();
        var fixture = Path.Combine(ProjectTestDirectory.RepositoryRoot, "tests", "UltraStar.SongCreator.Core.Tests", "Fixtures", "v1.uscproject");
        File.Copy(fixture, files.FilePath());
        var loaded = await new ProjectStore().LoadAsync(files.FilePath());
        Assert.Equal(1, loaded.SchemaVersion);
        Assert.Equal(Path.GetFullPath(files.FilePath()), loaded.FilePath);
        Assert.Empty(loaded.Issues);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), loaded.Song.Id);
        Assert.Equal("Høyt 🎵", loaded.Song.Metadata.Title);
        var note = Assert.Single(Assert.Single(loaded.Song.Phrases).Notes);
        Assert.Equal(1.25, note.StartSeconds);
        Assert.Equal(0.5, note.DurationSeconds);
        Assert.Equal(NoteType.Golden, note.Type);
        Assert.Equal(0.75, note.Confidence);
        var artifact = Assert.Single(loaded.Song.Analysis.Artifacts);
        Assert.Equal(AnalysisKind.PitchCurve, artifact.Kind);
        Assert.Equal("fixture-v1", artifact.ModelRevision);
        Assert.Equal("cache/pitch.json", artifact.ContentReference);
        Assert.Equal([new AnalysisPoint(1, 61.25), new AnalysisPoint(1.1, 62.5)], artifact.Points.ToArray());
        Assert.Equal(artifact.Id, Assert.Single(note.AnalysisReferences));
    }

    [Fact]
    public async Task EditedSongReopensWithoutAnalysisAndPreservesEveryStoredField()
    {
        using var files = new ProjectTestDirectory();
        var original = SongFixture.Create() with
        {
            Metadata = new() { Title = "Æ Ø Å 🎵\nTittel", Artist = "Test", Language = null }
        };
        var history = new SongHistory(original);
        history.Execute(new MoveNote(original.Phrases[0].Notes[0].Id, 1.25));
        var edited = history.Current;
        var store = new ProjectStore();
        await store.SaveAsync(files.FilePath(), edited);
        var loaded = await store.LoadAsync(files.FilePath());
        AssertSameSongValues(edited, loaded.Song);
        Assert.Equal(1.25, loaded.Song.Phrases[0].Notes[0].StartSeconds);
        Assert.Equal(1, original.Phrases[0].Notes[0].StartSeconds);
        Assert.False(new SongHistory(loaded.Song).CanUndo);
        Assert.Empty(loaded.Issues);
    }

    [Fact]
    public async Task AllNoteAndAnalysisKindsAndNullableFieldsRoundTrip()
    {
        using var files = new ProjectTestDirectory();
        var song = SongFixture.Create();
        var notes = Enum.GetValues<NoteType>().Select((kind, i) => new Note
        {
            StartSeconds = i,
            DurationSeconds = 0.25,
            Type = kind,
            Text = "x",
            Confidence = i % 2 == 0 ? null : 0,
            MidiPitch = i == 0 ? 0 : 127
        }).ToImmutableArray();
        song = song with
        {
            BeatsPerMinute = null,
            Phrases = [new() { StartSeconds = 0, EndSeconds = 10, Notes = notes }],
            Analysis = new()
            {
                Artifacts = Enum.GetValues<AnalysisKind>().Select(kind => new AnalysisArtifact
                {
                    Kind = kind,
                    Producer = "test",
                    ModelRevision = null,
                    ContentReference = null,
                    SourceMediaId = null,
                    Points = []
                }).ToImmutableArray()
            },
            Media = [song.Media[0], new() { Kind = MediaKind.Video, Location = "video.mp4", Source = null }]
        };
        var store = new ProjectStore();
        await store.SaveAsync(files.FilePath(), song);
        AssertSameSongValues(song, (await store.LoadAsync(files.FilePath())).Song);
    }

    [Fact]
    public async Task SaveIsCultureIndependentDeterministicAndOmitsComputedProperties()
    {
        using var files = new ProjectTestDirectory();
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
            var song = SongFixture.Create();
            var store = new ProjectStore();
            await store.SaveAsync(files.FilePath("first"), song);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            await store.SaveAsync(files.FilePath("second"), song);
            Assert.Equal(await File.ReadAllBytesAsync(files.FilePath("first")), await File.ReadAllBytesAsync(files.FilePath("second")));
            var root = JsonNode.Parse(await File.ReadAllTextAsync(files.FilePath("first")))!;
            var phrase = root["song"]!["phrases"]![0]!;
            Assert.Null(phrase["text"]);
            Assert.Null(phrase["notes"]![0]!["endSeconds"]);
            Assert.Equal("normal", phrase["notes"]![0]!["type"]!.GetValue<string>());
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    [Fact]
    public async Task DraftWarningsAndRelativeReferencesSurviveRoundTrip()
    {
        using var files = new ProjectTestDirectory();
        var song = SongFixture.Create();
        song = SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { Text = "", DurationSeconds = 2 });
        song = song with
        {
            Metadata = new(),
            Media = [song.Media[0] with { Location = "media/æ.wav" }],
            Analysis = song.Analysis with { Artifacts = [song.Analysis.Artifacts[0] with { ContentReference = "../cache/curve.json" }] }
        };
        var store = new ProjectStore();
        await store.SaveAsync(files.FilePath(), song);
        var loaded = await store.LoadAsync(files.FilePath());
        Assert.All(loaded.Issues, i => Assert.Equal(ValidationSeverity.Warning, i.Severity));
        Assert.Contains(loaded.Issues, i => i.Code == ValidationCode.NoteOverlap);
        Assert.Equal("media/æ.wav", loaded.Song.Media[0].Location);
        Assert.Equal("../cache/curve.json", loaded.Song.Analysis.Artifacts[0].ContentReference);
        Assert.Equal(2, loaded.Song.Phrases[0].Notes.Length);
        AssertSameSongValues(song, loaded.Song);
    }

    internal static void AssertSameSongValues(Song before, Song after)
    {
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.Metadata, after.Metadata);
        if (before.ImportedSource is null) Assert.Null(after.ImportedSource);
        else
        {
            Assert.NotNull(after.ImportedSource);
            Assert.Equal(before.ImportedSource.FormatId, after.ImportedSource.FormatId);
            Assert.Equal(before.ImportedSource.FileReference, after.ImportedSource.FileReference);
            Assert.Equal(before.ImportedSource.Headers.ToArray(), after.ImportedSource.Headers.ToArray());
        }
        Assert.Equal(before.Media.ToArray(), after.Media.ToArray());
        Assert.Equal(before.AudioOffsetSeconds, after.AudioOffsetSeconds);
        Assert.Equal(before.VideoOffsetSeconds, after.VideoOffsetSeconds);
        Assert.Equal(before.BeatsPerMinute, after.BeatsPerMinute);
        Assert.Equal(before.Phrases.Length, after.Phrases.Length);
        for (var i = 0; i < before.Phrases.Length; i++)
        {
            var p = before.Phrases[i]; var q = after.Phrases[i];
            Assert.Equal(p.Id, q.Id); Assert.Equal(p.StartSeconds, q.StartSeconds); Assert.Equal(p.EndSeconds, q.EndSeconds);
            Assert.Equal(p.Text, q.Text); Assert.Equal(p.Notes.Length, q.Notes.Length);
            for (var j = 0; j < p.Notes.Length; j++)
            {
                var n = p.Notes[j]; var m = q.Notes[j];
                Assert.Equal(n.Id, m.Id); Assert.Equal(n.StartSeconds, m.StartSeconds); Assert.Equal(n.DurationSeconds, m.DurationSeconds);
                Assert.Equal(n.MidiPitch, m.MidiPitch); Assert.Equal(n.Text, m.Text); Assert.Equal(n.Type, m.Type);
                Assert.Equal(n.Confidence, m.Confidence); Assert.Equal(n.AnalysisReferences.ToArray(), m.AnalysisReferences.ToArray());
            }
        }
        Assert.Equal(before.Analysis.Artifacts.Length, after.Analysis.Artifacts.Length);
        for (var i = 0; i < before.Analysis.Artifacts.Length; i++)
        {
            var a = before.Analysis.Artifacts[i]; var b = after.Analysis.Artifacts[i];
            Assert.Equal(a.Id, b.Id); Assert.Equal(a.Kind, b.Kind); Assert.Equal(a.SourceMediaId, b.SourceMediaId);
            Assert.Equal(a.Producer, b.Producer); Assert.Equal(a.ModelRevision, b.ModelRevision); Assert.Equal(a.ContentReference, b.ContentReference);
            Assert.Equal(a.Points.ToArray(), b.Points.ToArray());
        }
    }
}
