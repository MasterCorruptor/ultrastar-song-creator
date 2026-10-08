using System.Collections.Immutable;
using System.Text.Json.Nodes;
using UltraStar.SongCreator.Projects;

namespace UltraStar.SongCreator.Core.Tests;

public class SourceMetadataTests
{
    [Fact]
    public async Task V1LoadsWithoutInventingMetadataAndSavesExplicitlyAsV2()
    {
        using var files = new ProjectTestDirectory();
        var fixture = Path.Combine(ProjectTestDirectory.RepositoryRoot, "tests", "UltraStar.SongCreator.Core.Tests", "Fixtures", "v1.uscproject");
        File.Copy(fixture, files.FilePath());
        var before = await File.ReadAllBytesAsync(files.FilePath());
        var store = new ProjectStore();
        var loaded = await store.LoadAsync(files.FilePath());
        Assert.Equal(1, loaded.SchemaVersion);
        Assert.Null(loaded.Song.ImportedSource);
        Assert.Equal(before, await File.ReadAllBytesAsync(files.FilePath()));
        await store.SaveAsync(files.FilePath("v2"), loaded.Song);
        var migrated = await store.LoadAsync(files.FilePath("v2"));
        Assert.Equal(2, migrated.SchemaVersion);
        Assert.Equal(before, await File.ReadAllBytesAsync(files.FilePath()));
        ProjectRoundTripTests.AssertSameSongValues(loaded.Song, migrated.Song);
    }

    [Fact]
    public async Task EveryOriginalHeaderAndItsOrderSurvivesEditingAndReopening()
    {
        using var files = new ProjectTestDirectory();
        var source = new SourceDocument
        {
            FormatId = "ultrastar-v1",
            FileReference = "sources/song.txt",
            Headers = [new("COVER", "cover.png"), new("ALBUM", "Æ Album"), new("CREATOR", "Test"),
                new("X-CUSTOM", "a:b"), new("X-CUSTOM", "second"), new("COMMENT", "")]
        };
        var song = SongFixture.Create() with { ImportedSource = source };
        var edited = new MoveNote(song.Phrases[0].Notes[0].Id, 1.25).Apply(song);
        Assert.Same(source, edited.ImportedSource);
        var store = new ProjectStore();
        await store.SaveAsync(files.FilePath(), edited);
        var reopened = await store.LoadAsync(files.FilePath());
        Assert.Equal(2, reopened.SchemaVersion);
        ProjectRoundTripTests.AssertSameSongValues(edited, reopened.Song);
    }

    [Theory]
    [InlineData("missingSource")]
    [InlineData("nullHeaders")]
    [InlineData("nullHeader")]
    [InlineData("missingValue")]
    [InlineData("extraField")]
    public async Task V2SourceSchemaDoesNotSilentlyLoseMetadata(string mutation)
    {
        using var files = new ProjectTestDirectory();
        var store = new ProjectStore();
        await store.SaveAsync(files.FilePath(), SongFixture.Create() with
        {
            ImportedSource = new() { FormatId = "test", Headers = [new("COVER", "image.png")] }
        });
        var root = JsonNode.Parse(await File.ReadAllTextAsync(files.FilePath()))!;
        switch (mutation)
        {
            case "missingSource": root.AsObject().Remove("importedSource"); break;
            case "nullHeaders": root["importedSource"]!["headers"] = null; break;
            case "nullHeader": root["importedSource"]!["headers"]![0] = null; break;
            case "missingValue": root["importedSource"]!["headers"]![0]!.AsObject().Remove("value"); break;
            case "extraField": root["importedSource"]!["future"] = 1; break;
        }
        await File.WriteAllTextAsync(files.FilePath(), root.ToJsonString());
        Assert.Equal(ProjectError.InvalidJson,
            (await Assert.ThrowsAsync<ProjectFormatException>(() => store.LoadAsync(files.FilePath()))).Error);
    }

    [Fact]
    public void InvalidSourceMetadataReturnsDomainErrors()
    {
        var song = SongFixture.Create() with { ImportedSource = new() { FormatId = "" } };
        Assert.Contains(SongValidator.Validate(song), i => i.Code == ValidationCode.InvalidSourceMetadata);
        song = song with { ImportedSource = new() { FormatId = "test", Headers = [new("", null!)] } };
        Assert.Contains(SongValidator.Validate(song), i => i.Code == ValidationCode.InvalidSourceMetadata);
    }
}
