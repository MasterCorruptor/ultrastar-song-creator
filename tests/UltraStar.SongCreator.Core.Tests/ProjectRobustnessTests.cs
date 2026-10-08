using System.Text;
using System.Text.Json.Nodes;
using UltraStar.SongCreator.Projects;

namespace UltraStar.SongCreator.Core.Tests;

public class ProjectRobustnessTests
{
    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"format\":\"other\",\"schemaVersion\":1}")]
    public async Task InvalidEnvelopeOrJsonIsRejectedWithoutChangingFile(string json) =>
        await Reject(json);

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    public async Task UnsupportedVersionIsExplicitAndDoesNotRewriteTheFile(int version)
    {
        var root = JsonNode.Parse(await Fixture())!;
        root["schemaVersion"] = version;
        Assert.Equal(ProjectError.UnsupportedVersion, (await Reject(root.ToJsonString())).Error);
    }

    [Theory]
    [InlineData("\"1\"")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("1.5")]
    [InlineData("2147483648")]
    public async Task VersionMustBeAnInteger(string value)
    {
        var root = JsonNode.Parse(await Fixture())!;
        root["schemaVersion"] = JsonNode.Parse(value);
        Assert.Equal(ProjectError.InvalidEnvelope, (await Reject(root.ToJsonString())).Error);
    }

    [Theory]
    [InlineData("unknownRoot")]
    [InlineData("unknownNote")]
    [InlineData("missingSongId")]
    [InlineData("missingNullable")]
    [InlineData("nullSong")]
    [InlineData("nullMetadata")]
    [InlineData("nullMediaItem")]
    [InlineData("nullNotes")]
    [InlineData("nullNoteItem")]
    [InlineData("nullText")]
    [InlineData("nullPoint")]
    [InlineData("badEnum")]
    [InlineData("numericEnum")]
    [InlineData("compositeEnum")]
    [InlineData("wrongEnumCase")]
    [InlineData("numericStringEnum")]
    [InlineData("badGuid")]
    [InlineData("wrongCase")]
    public async Task SchemaFailuresDoNotBecomeDefaultValuesOrSilentDataLoss(string mutation)
    {
        var root = JsonNode.Parse(await Fixture())!;
        var song = root["song"]!;
        var note = song["phrases"]![0]!["notes"]![0]!;
        switch (mutation)
        {
            case "unknownRoot": root["futureFeature"] = 1; break;
            case "unknownNote": note["futureFlag"] = true; break;
            case "missingSongId": song.AsObject().Remove("id"); break;
            case "missingNullable": song["metadata"]!.AsObject().Remove("language"); break;
            case "nullSong": root["song"] = null; break;
            case "nullMetadata": song["metadata"] = null; break;
            case "nullMediaItem": song["media"]![0] = null; break;
            case "nullNotes": song["phrases"]![0]!["notes"] = null; break;
            case "nullNoteItem": song["phrases"]![0]!["notes"]![0] = null; break;
            case "nullText": note["text"] = null; break;
            case "nullPoint": song["analysis"]!["artifacts"]![0]!["points"]![0] = null; break;
            case "badEnum": note["type"] = "mystery"; break;
            case "numericEnum": note["type"] = 0; break;
            case "compositeEnum": note["type"] = "golden, normal"; break;
            case "wrongEnumCase": note["type"] = "GOLDEN"; break;
            case "numericStringEnum": note["type"] = "1"; break;
            case "badGuid": note["id"] = "not-a-guid"; break;
            case "wrongCase": note.AsObject().Remove("startSeconds"); note["StartSeconds"] = 1; break;
        }
        Assert.Equal(ProjectError.InvalidJson, (await Reject(root.ToJsonString())).Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DuplicatePropertiesAreRejectedAtAnyLevel(bool nested)
    {
        var json = await Fixture();
        json = nested
            ? json.Replace("\"title\":", "\"title\": \"first\", \"title\":", StringComparison.Ordinal)
            : json.Replace("\"schemaVersion\":", "\"schemaVersion\": 2, \"schemaVersion\":", StringComparison.Ordinal);
        Assert.Equal(ProjectError.InvalidJson, (await Reject(json)).Error);
    }

    [Fact]
    public async Task StructuralSongErrorsReturnDomainDiagnostics()
    {
        var root = JsonNode.Parse(await Fixture())!;
        root["song"]!["phrases"]![0]!["notes"]![0]!["startSeconds"] = -1;
        var error = await Reject(root.ToJsonString());
        Assert.Equal(ProjectError.InvalidSong, error.Error);
        Assert.Contains(error.Issues, i => i.Code == ValidationCode.InvalidTiming && i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public async Task Utf8BomIsAcceptedButInvalidUtf8IsRejected()
    {
        using var files = new ProjectTestDirectory();
        await File.WriteAllBytesAsync(files.FilePath(), [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(await Fixture())]);
        var loaded = await new ProjectStore().LoadAsync(files.FilePath());
        Assert.Equal("Høyt 🎵", loaded.Song.Metadata.Title);
        await File.WriteAllBytesAsync(files.FilePath(), [0xFF, 0xFE, 0x80]);
        Assert.Equal(ProjectError.InvalidJson, (await Assert.ThrowsAsync<ProjectFormatException>(() => new ProjectStore().LoadAsync(files.FilePath()))).Error);
    }

    [Fact]
    public async Task ExcessiveJsonDepthIsRejected()
    {
        await Reject(new string('[', 70) + "0" + new string(']', 70));
    }

    [Fact]
    public async Task FailedValidationAndSchemaEncodingPreserveExistingProjectBytes()
    {
        using var files = new ProjectTestDirectory();
        var store = new ProjectStore();
        var song = SongFixture.Create();
        await store.SaveAsync(files.FilePath(), song);
        var old = await File.ReadAllBytesAsync(files.FilePath());
        var invalid = SongFixture.WithFirstNote(song, song.Phrases[0].Notes[0] with { Confidence = double.NaN });
        var error = await Assert.ThrowsAsync<ProjectFormatException>(() => store.SaveAsync(files.FilePath(), invalid));
        Assert.Equal(ProjectError.InvalidSong, error.Error);
        Assert.Contains(error.Issues, i => i.Code == ValidationCode.InvalidConfidence);
        Assert.Equal(old, await File.ReadAllBytesAsync(files.FilePath()));
        var nullTitle = song with { Metadata = song.Metadata with { Title = null! } };
        Assert.Equal(ProjectError.InvalidSong, (await Assert.ThrowsAsync<ProjectFormatException>(() => store.SaveAsync(files.FilePath(), nullTitle))).Error);
        Assert.Equal(old, await File.ReadAllBytesAsync(files.FilePath()));
        Assert.Empty(Directory.GetFiles(files.DirectoryPath, "*.tmp"));
    }

    [Fact]
    public async Task SizeLimitAppliesBeforeReplacingAFileAndAcceptsExactBoundary()
    {
        using var files = new ProjectTestDirectory();
        var song = SongFixture.Create();
        await new ProjectStore().SaveAsync(files.FilePath(), song);
        var bytes = await File.ReadAllBytesAsync(files.FilePath());
        var limited = new ProjectStore(bytes.Length - 1);
        Assert.Equal(ProjectError.TooLarge, (await Assert.ThrowsAsync<ProjectFormatException>(() => limited.LoadAsync(files.FilePath()))).Error);
        Assert.Equal(ProjectError.TooLarge, (await Assert.ThrowsAsync<ProjectFormatException>(() => limited.SaveAsync(files.FilePath(), song))).Error);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(files.FilePath()));
        await new ProjectStore(bytes.Length).SaveAsync(files.FilePath(), song);
        Assert.Empty((await new ProjectStore(bytes.Length).LoadAsync(files.FilePath())).Issues);
        Assert.Empty(Directory.GetFiles(files.DirectoryPath, "*.tmp"));
    }

    [Fact]
    public async Task PreCancelledSaveAndLoadNeverChangeOrCreateFiles()
    {
        using var files = new ProjectTestDirectory();
        var store = new ProjectStore();
        var song = SongFixture.Create();
        await store.SaveAsync(files.FilePath(), song);
        var old = await File.ReadAllBytesAsync(files.FilePath());
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(files.FilePath(), song, cancel.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.LoadAsync(files.FilePath(), cancel.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(files.FilePath("new"), song, cancel.Token));
        Assert.Equal(old, await File.ReadAllBytesAsync(files.FilePath()));
        Assert.False(File.Exists(files.FilePath("new")));
        Assert.Empty(Directory.GetFiles(files.DirectoryPath, "*.tmp"));
    }

    [Fact]
    public async Task ReplacementFailureCleansTempAndPreservesDestinationDirectory()
    {
        using var files = new ProjectTestDirectory();
        Directory.CreateDirectory(files.FilePath());
        var sentinel = Path.Combine(files.FilePath(), "keep.txt");
        await File.WriteAllTextAsync(sentinel, "keep");
        var error = await Record.ExceptionAsync(() => new ProjectStore().SaveAsync(files.FilePath(), SongFixture.Create()));
        Assert.NotNull(error);
        Assert.True(error is IOException or UnauthorizedAccessException);
        Assert.Equal("keep", await File.ReadAllTextAsync(sentinel));
        Assert.Empty(Directory.GetFiles(files.DirectoryPath, "*.tmp"));
    }

    [Fact]
    public async Task SuccessReplacesOnlyTheRequestedFileAndLeavesNoTemp()
    {
        using var files = new ProjectTestDirectory();
        var store = new ProjectStore();
        var song = SongFixture.Create();
        await store.SaveAsync(files.FilePath(), song);
        await File.WriteAllTextAsync(files.FilePath("unrelated"), "keep");
        var changed = new MoveNote(song.Phrases[0].Notes[0].Id, 1.5).Apply(song);
        await store.SaveAsync(files.FilePath(), changed);
        Assert.Equal(1.5, (await store.LoadAsync(files.FilePath())).Song.Phrases[0].Notes[0].StartSeconds);
        Assert.Equal("keep", await File.ReadAllTextAsync(files.FilePath("unrelated")));
        Assert.Empty(Directory.GetFiles(files.DirectoryPath, "*.tmp"));
    }

    [Fact]
    public async Task MissingFileAndParentDirectoryRemainIoErrors()
    {
        using var files = new ProjectTestDirectory();
        await Assert.ThrowsAsync<FileNotFoundException>(() => new ProjectStore().LoadAsync(files.FilePath()));
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => new ProjectStore().SaveAsync(files.FilePath("missing/song"), SongFixture.Create()));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(2147483648L)]
    public void InvalidConfiguredLimitsAreRejected(long limit) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProjectStore(limit));

    private static Task<string> Fixture() => File.ReadAllTextAsync(Path.Combine(
        ProjectTestDirectory.RepositoryRoot, "tests", "UltraStar.SongCreator.Core.Tests", "Fixtures", "v1.uscproject"));

    private static async Task<ProjectFormatException> Reject(string json)
    {
        using var files = new ProjectTestDirectory();
        await File.WriteAllTextAsync(files.FilePath(), json);
        var before = await File.ReadAllBytesAsync(files.FilePath());
        var error = await Assert.ThrowsAsync<ProjectFormatException>(() => new ProjectStore().LoadAsync(files.FilePath()));
        Assert.Equal(before, await File.ReadAllBytesAsync(files.FilePath()));
        return error;
    }
}
