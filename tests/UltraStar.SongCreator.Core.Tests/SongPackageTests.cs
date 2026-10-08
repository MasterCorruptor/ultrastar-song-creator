using System.Security.Cryptography;
using System.Text;
using UltraStar.SongCreator.Projects;
using UltraStar.SongCreator.UltraStar;

namespace UltraStar.SongCreator.Core.Tests;

public class SongPackageTests
{
    private sealed class ImmediateProgress(Action<ExportProgress> action) : IProgress<ExportProgress>
    {
        public void Report(ExportProgress value) => action(value);
    }

    private static async Task<string> Asset(ProjectTestDirectory files, string name, byte[]? bytes = null)
    {
        var path = files.FilePath(Path.Combine("inputs", name));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, bytes ?? [1, 2, 3, 4, 5]);
        return path;
    }

    private static Song WithSource(ProjectTestDirectory files, params SourceHeader[] headers) => UltraStarExportTests.Song() with
    {
        ImportedSource = new()
        {
            FormatId = "ultrastar-v1",
            FileReference = files.FilePath(Path.Combine("inputs", "original.txt")),
            Headers = [new("BPM", "120"), new("TITLE", "Old"), new("ARTIST", "Old"), new("MP3", "audio.wav"), .. headers]
        }
    };

    private static string TargetAsset(ExportResult result, PackagedAsset asset) =>
        Path.Combine(result.DirectoryPath!, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));

    private static void NoStaging(ProjectTestDirectory files) =>
        Assert.Empty(Directory.EnumerateDirectories(files.DirectoryPath, "*.export-tmp"));

    [Theory]
    [InlineData(ExportFormat.Unversioned)]
    [InlineData(ExportFormat.V1)]
    public async Task CompleteFolderIncludesByteIdenticalMediaAndRewrittenImageStemReferences(ExportFormat format)
    {
        using var files = new ProjectTestDirectory();
        var audio = await Asset(files, "audio.wav", [0, 1, 2, 3]);
        await Asset(files, "video.mp4", [4, 5, 6]);
        await Asset(files, "ø cover.png", [7, 8, 9]);
        await Asset(files, "background.jpg", [10, 11]);
        await Asset(files, "vocals.wav", [12, 13]);
        await Asset(files, "instrumental.flac", [14, 15]);
        var song = WithSource(files, new("COVER", "ø cover.png"), new("BACKGROUND", "background.jpg"),
            new("VOCALS", "vocals.wav"), new("INSTRUMENTAL", "instrumental.flac"), new("ALBUM", "Bevares"), new("X-CUSTOM", "a:b")) with
        {
            Media = [new() { Kind = MediaKind.Audio, Location = "audio.wav" }, new() { Kind = MediaKind.Video, Location = "video.mp4" }],
            AudioOffsetSeconds = .25,
            VideoOffsetSeconds = -.5
        };
        var result = UltraStarExportTests.Success(await new UltraStarExporter().PackageAsync(files.FilePath("export"), song, new(format)));
        Assert.Equal(6, result.Assets.Length);
        Assert.Equal(files.FilePath("export"), result.DirectoryPath);
        foreach (var asset in result.Assets)
        {
            var source = await File.ReadAllBytesAsync(asset.SourcePath);
            var copy = await File.ReadAllBytesAsync(TargetAsset(result, asset));
            Assert.Equal(source, copy);
            Assert.Equal(source.Length, asset.ByteLength);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant(), asset.Sha256);
            Assert.StartsWith("media/", asset.RelativePath);
        }
        Assert.Equal(new byte[] { 0, 1, 2, 3 }, await File.ReadAllBytesAsync(audio));
        var textBytes = await File.ReadAllBytesAsync(Path.Combine(result.DirectoryPath!, "song.txt"));
        Assert.False(textBytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
        Assert.Equal(result.Text, new UTF8Encoding(false, true).GetString(textBytes));
        Assert.DoesNotContain('\r', result.Text!);
        var parsed = (await new UltraStarImporter().LoadAsync(Path.Combine(result.DirectoryPath!, "song.txt"))).Song!;
        Assert.Equal(song.Metadata, parsed.Metadata);
        Assert.Equal(song.Phrases.Select(p => p.Text), parsed.Phrases.Select(p => p.Text));
        Assert.Equal(.25, parsed.AudioOffsetSeconds);
        Assert.Equal(-.5, parsed.VideoOffsetSeconds);
        Assert.Equal("Bevares", parsed.ImportedSource!.Headers.Single(h => h.Name == "ALBUM").Value);
        Assert.Equal("a:b", parsed.ImportedSource.Headers.Single(h => h.Name == "X-CUSTOM").Value);
        foreach (var header in parsed.ImportedSource.Headers.Where(h => h.Name is "MP3" or "VIDEO" or "COVER" or "BACKGROUND" or "VOCALS" or "INSTRUMENTAL"))
        {
            Assert.StartsWith("media/", header.Value);
            Assert.True(File.Exists(Path.Combine(result.DirectoryPath!, header.Value)));
        }
        NoStaging(files);
    }

    [Fact]
    public async Task CanonicallyIdenticalSourceReferencesShareOneCopy()
    {
        using var files = new ProjectTestDirectory();
        await Asset(files, "audio.wav");
        var song = WithSource(files, new("VOCALS", "./audio.wav"), new("INSTRUMENTAL", "folder/../audio.wav"), new("AUDIO", "stale.flac"));
        var result = UltraStarExportTests.Success(await new UltraStarExporter().PackageAsync(files.FilePath("export"), song, new(ExportFormat.V1)));
        Assert.Single(result.Assets);
        var parsed = UltraStarExportTests.Imported(result);
        var refs = parsed.ImportedSource!.Headers.Where(h => h.Name is "MP3" or "AUDIO" or "VOCALS" or "INSTRUMENTAL").Select(h => h.Value);
        Assert.Single(refs.Distinct());
        Assert.DoesNotContain("stale.flac", result.Text!);
    }

    [Fact]
    public async Task CaseInsensitiveNameCollisionsAcrossFoldersGetDistinctPortableNames()
    {
        using var files = new ProjectTestDirectory();
        await Asset(files, "audio.wav");
        await Asset(files, Path.Combine("a", "cover.png"), [1]);
        await Asset(files, Path.Combine("b", "COVER.png"), [2]);
        var song = WithSource(files, new("COVER", "a/cover.png"), new("BACKGROUND", "b/COVER.png"));
        var writer = new UltraStarExporter();
        var first = UltraStarExportTests.Success(await writer.PackageAsync(files.FilePath("export-one"), song, new(ExportFormat.V1)));
        var second = UltraStarExportTests.Success(await writer.PackageAsync(files.FilePath("export-two"), song, new(ExportFormat.V1)));
        Assert.Equal(first.Text, second.Text);
        Assert.Equal(first.Assets.Select(a => a.RelativePath), second.Assets.Select(a => a.RelativePath));
        Assert.Equal(3, first.Assets.Select(a => a.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        var parsed = UltraStarExportTests.Imported(first);
        var cover = parsed.ImportedSource!.Headers.Single(h => h.Name == "COVER").Value;
        var background = parsed.ImportedSource.Headers.Single(h => h.Name == "BACKGROUND").Value;
        Assert.NotEqual(cover.ToUpperInvariant(), background.ToUpperInvariant());
        Assert.Equal(new byte[] { 1 }, await File.ReadAllBytesAsync(Path.Combine(first.DirectoryPath!, cover)));
        Assert.Equal(new byte[] { 2 }, await File.ReadAllBytesAsync(Path.Combine(first.DirectoryPath!, background)));
    }

    [Fact]
    public async Task MissingReferenceDirectoryIsNotGuessedFromWorkingDirectory()
    {
        using var files = new ProjectTestDirectory();
        await Asset(files, "audio.wav");
        var writer = new UltraStarExporter();
        UltraStarExportTests.Error(await writer.PackageAsync(files.FilePath("export"), UltraStarExportTests.Song(), new(ExportFormat.V1, 120)),
            ExportCode.MissingReferenceDirectory);
        Assert.False(Directory.Exists(files.FilePath("export")));
        var result = UltraStarExportTests.Success(await writer.PackageAsync(files.FilePath("export"), UltraStarExportTests.Song(),
            new(ExportFormat.V1, 120, files.FilePath("inputs"))));
        Assert.Single(result.Assets);
    }

    [Fact]
    public async Task AbsoluteAssetNeedsNoReferenceDirectoryAndOutputContainsNoAbsolutePath()
    {
        using var files = new ProjectTestDirectory();
        var audio = await Asset(files, "audio.wav");
        var song = UltraStarExportTests.Song() with { Media = [new() { Location = audio }] };
        var result = UltraStarExportTests.Success(await new UltraStarExporter().PackageAsync(files.FilePath("export"), song, new(ExportFormat.V1, 120)));
        Assert.DoesNotContain(audio, result.Text!);
        Assert.StartsWith("media/", UltraStarExportTests.Imported(result).Media[0].Location);
    }

    [Fact]
    public async Task WindowsStyleRelativeSeparatorsWorkOnBothPlatforms()
    {
        using var files = new ProjectTestDirectory();
        await Asset(files, Path.Combine("sub", "audio.wav"));
        var song = WithSource(files) with { Media = [new() { Location = "sub\\audio.wav" }] };
        var result = UltraStarExportTests.Success(await new UltraStarExporter().PackageAsync(files.FilePath("export"), song, new(ExportFormat.Unversioned)));
        Assert.Single(result.Assets);
        Assert.True(File.Exists(TargetAsset(result, result.Assets[0])));
    }

    [Theory]
    [InlineData("https://example.invalid/audio.wav")]
    [InlineData("file:///nonexistent/audio.wav")]
    public async Task UrlReferencesAreNotDownloadedOrTreatedAsLocalFiles(string value)
    {
        using var files = new ProjectTestDirectory();
        var song = UltraStarExportTests.Song() with { Media = [new() { Location = value }] };
        var result = await new UltraStarExporter().PackageAsync(files.FilePath("export"), song, new(ExportFormat.V1, 120));
        UltraStarExportTests.Error(result, ExportCode.NonLocalReference);
        Assert.False(Directory.Exists(files.FilePath("export")));
        NoStaging(files);
    }

    [Theory]
    [InlineData("COVER")]
    [InlineData("BACKGROUND")]
    [InlineData("VOCALS")]
    [InlineData("INSTRUMENTAL")]
    public async Task MissingDeclaredAssetLeavesNoFinishedFolder(string header)
    {
        using var files = new ProjectTestDirectory();
        var audio = await Asset(files, "audio.wav");
        var before = await File.ReadAllBytesAsync(audio);
        var song = WithSource(files, new SourceHeader(header, "missing.dat"));
        await Assert.ThrowsAsync<FileNotFoundException>(() => new UltraStarExporter().PackageAsync(files.FilePath("export"), song, new(ExportFormat.V1)));
        Assert.False(Directory.Exists(files.FilePath("export")));
        Assert.Equal(before, await File.ReadAllBytesAsync(audio));
        NoStaging(files);
    }

    [Fact]
    public async Task ExistingFolderOrFileIsNeverOverwritten()
    {
        using var files = new ProjectTestDirectory();
        await Asset(files, "audio.wav");
        Directory.CreateDirectory(files.FilePath("existing"));
        await File.WriteAllTextAsync(Path.Combine(files.FilePath("existing"), "keep.txt"), "unchanged");
        var writer = new UltraStarExporter();
        await Assert.ThrowsAsync<IOException>(() => writer.PackageAsync(files.FilePath("existing"), WithSource(files), new(ExportFormat.V1)));
        Assert.Equal("unchanged", await File.ReadAllTextAsync(Path.Combine(files.FilePath("existing"), "keep.txt")));
        await File.WriteAllTextAsync(files.FilePath("file-target"), "unchanged");
        await Assert.ThrowsAsync<IOException>(() => writer.PackageAsync(files.FilePath("file-target"), WithSource(files), new(ExportFormat.V1)));
        Assert.Equal("unchanged", await File.ReadAllTextAsync(files.FilePath("file-target")));
        NoStaging(files);
    }

    [Fact]
    public async Task MidCopyCancellationRemovesStagingAndPreservesSource()
    {
        using var files = new ProjectTestDirectory();
        var bytes = Enumerable.Range(0, 200000).Select(i => (byte)(i % 251)).ToArray();
        var source = await Asset(files, "audio.wav", bytes);
        using var cancellation = new CancellationTokenSource();
        var progress = new ImmediateProgress(p =>
        {
            Assert.StartsWith("media/", p.AssetReference);
            Assert.True(p.BytesCopied < p.TotalBytes);
            cancellation.Cancel();
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new UltraStarExporter().PackageAsync(files.FilePath("export"),
            WithSource(files), new(ExportFormat.V1), progress, cancellation.Token));
        Assert.False(Directory.Exists(files.FilePath("export")));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(source));
        NoStaging(files);
    }

    [Fact]
    public async Task CopyStageFailureCleansUpAndDestinationRacePreservesNewExistingFolder()
    {
        using var files = new ProjectTestDirectory();
        await Asset(files, "audio.wav");
        var writer = new UltraStarExporter();
        var throwing = new ImmediateProgress(_ => throw new IOException("Injected copy-stage failure"));
        await Assert.ThrowsAsync<IOException>(() => writer.PackageAsync(files.FilePath("export"), WithSource(files), new(ExportFormat.V1), throwing));
        Assert.False(Directory.Exists(files.FilePath("export")));
        NoStaging(files);
        var race = new ImmediateProgress(_ =>
        {
            Directory.CreateDirectory(files.FilePath("export"));
            File.WriteAllText(Path.Combine(files.FilePath("export"), "keep.txt"), "another writer");
        });
        await Assert.ThrowsAsync<IOException>(() => writer.PackageAsync(files.FilePath("export"), WithSource(files), new(ExportFormat.V1), race));
        Assert.Equal("another writer", await File.ReadAllTextAsync(Path.Combine(files.FilePath("export"), "keep.txt")));
        NoStaging(files);
    }

    [Fact]
    public async Task PreCancellationAndValidationFailureCreateNoDirectories()
    {
        using var files = new ProjectTestDirectory();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var writer = new UltraStarExporter();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            writer.PackageAsync(files.FilePath("export"), UltraStarExportTests.Song(), new(ExportFormat.V1, 120), cancellationToken: cancellation.Token));
        var song = SongFixture.WithFirstNote(UltraStarExportTests.Song(), new() { StartSeconds = .01, DurationSeconds = .01, Text = "tiny" });
        UltraStarExportTests.Error(await writer.PackageAsync(files.FilePath("export"), song, new(ExportFormat.V1, 120)), ExportCode.CollapsedNote);
        Assert.Empty(Directory.EnumerateFileSystemEntries(files.DirectoryPath));
    }

    [Fact]
    public async Task CompletedPackageIsIndependentOfSourcesAndCanBeRelocated()
    {
        using var files = new ProjectTestDirectory();
        var source = await Asset(files, "audio.wav");
        var song = WithSource(files);
        var result = UltraStarExportTests.Success(await new UltraStarExporter().PackageAsync(files.FilePath("export"), song, new(ExportFormat.V1)));
        File.Delete(source);
        var relocated = files.FilePath("relocated");
        foreach (var path in new[] { result.DirectoryPath!, relocated })
            Assert.StartsWith(Path.GetFullPath(files.DirectoryPath) + Path.DirectorySeparatorChar, Path.GetFullPath(path), StringComparison.Ordinal);
        Directory.Move(result.DirectoryPath!, relocated);
        var parsed = await new UltraStarImporter().LoadAsync(Path.Combine(relocated, "song.txt"));
        Assert.True(parsed.Success);
        Assert.Equal(song.Phrases.Select(p => p.Text), parsed.Song!.Phrases.Select(p => p.Text));
        Assert.True(File.Exists(Path.Combine(relocated, parsed.Song.Media[0].Location)));
    }

    [Fact]
    public async Task ProjectSaveLoadEditExportDoesNotMutateProjectMetadataOrAnalysis()
    {
        using var files = new ProjectTestDirectory();
        await Asset(files, "audio.wav");
        var song = WithSource(files);
        var store = new ProjectStore();
        await store.SaveAsync(files.FilePath("project.uscproject"), song);
        var beforeBytes = await File.ReadAllBytesAsync(files.FilePath("project.uscproject"));
        var loaded = await store.LoadAsync(files.FilePath("project.uscproject"));
        var edited = new MoveNote(loaded.Song.Phrases[0].Notes[0].Id, .07).Apply(loaded.Song);
        var result = UltraStarExportTests.Success(await new UltraStarExporter().PackageAsync(files.FilePath("export"), edited, new(ExportFormat.V1)));
        Assert.Same(loaded.Song.ImportedSource, edited.ImportedSource);
        Assert.Same(loaded.Song.Analysis, edited.Analysis);
        Assert.Equal(beforeBytes, await File.ReadAllBytesAsync(files.FilePath("project.uscproject")));
        Assert.Equal(.07, edited.Phrases[0].Notes[0].StartSeconds);
        Assert.Equal(.125, UltraStarExportTests.Imported(result).Phrases[0].Notes[0].StartSeconds);
        Assert.Contains(result.Diagnostics, d => d.Code == ExportCode.TimingRounded);
    }

    [Fact]
    public async Task EmptyDuplicateImageMetadataRemainsEmptyAndNonemptyDuplicateIsRejected()
    {
        using var files = new ProjectTestDirectory();
        await Asset(files, "audio.wav");
        await Asset(files, "cover.png");
        var song = WithSource(files, new("COVER", ""), new("COVER", "cover.png"));
        var result = UltraStarExportTests.Success(await new UltraStarExporter().PackageAsync(files.FilePath("export"), song, new(ExportFormat.V1)));
        Assert.Contains(new SourceHeader("COVER", ""), UltraStarExportTests.Imported(result).ImportedSource!.Headers);
        Assert.Equal(2, result.Assets.Length);
        song = WithSource(files, new("COVER", "cover.png"), new("COVER", "cover.png"));
        UltraStarExportTests.Error(await new UltraStarExporter().PackageAsync(files.FilePath("rejected"), song, new(ExportFormat.V1)), ExportCode.InvalidHeader);
        Assert.False(Directory.Exists(files.FilePath("rejected")));
    }

    [Fact]
    public async Task ReservedWindowsAssetNameGetsPortablePrefixOnLinux()
    {
        if (OperatingSystem.IsWindows()) return; // Windows cannot create this source filename.
        using var files = new ProjectTestDirectory();
        await Asset(files, "CON.wav");
        var song = WithSource(files) with { Media = [new() { Location = "CON.wav" }] };
        var result = UltraStarExportTests.Success(await new UltraStarExporter().PackageAsync(files.FilePath("export"), song, new(ExportFormat.V1)));
        Assert.Equal("media/_CON.wav", Assert.Single(result.Assets).RelativePath);
    }

    [Fact]
    public async Task ParentDirectoryAndRenderedHeaderValidationFailBeforeAnyStaging()
    {
        using var files = new ProjectTestDirectory();
        await Asset(files, "audio.wav");
        var writer = new UltraStarExporter();
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => writer.PackageAsync(files.FilePath(Path.Combine("missing-parent", "export")),
            WithSource(files), new(ExportFormat.V1)));
        var invalid = WithSource(files, new("P1", "First"), new("P1", "Second"));
        UltraStarExportTests.Error(await writer.PackageAsync(files.FilePath("export"), invalid, new(ExportFormat.V1)), ExportCode.InvalidHeader);
        Assert.False(Directory.Exists(files.FilePath("export")));
        Assert.False(Directory.Exists(files.FilePath("missing-parent")));
        NoStaging(files);
    }
}
