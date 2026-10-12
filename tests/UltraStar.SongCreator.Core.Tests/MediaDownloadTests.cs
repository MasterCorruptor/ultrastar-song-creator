using System.Security.Cryptography;
using System.Text.Json.Nodes;
using UltraStar.SongCreator.Acquisition;
using UltraStar.SongCreator.Core;
using UltraStar.SongCreator.Projects;
using UltraStar.SongCreator.UltraStar;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class MediaDownloadTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublishesCompleteFolderWithExplicitFormatsAndVerifiedHashes(bool withVideo)
    {
        using var directory = new ProjectTestDirectory();
        var rig = new OnlineMediaTests.Rig();
        var selection = MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140", withVideo ? "133" : null);
        var destination = directory.FilePath("download");
        var result = await rig.Source().DownloadAsync(selection, destination);
        Assert.Equal(destination, result.DirectoryPath);
        Assert.True(File.Exists(result.Audio.FilePath));
        Assert.Equal(withVideo, result.Video is not null);
        Assert.Equal(withVideo ? 3 : 2, Directory.GetFiles(destination).Length);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(result.Audio.FilePath))), result.AudioSha256);
        Assert.Empty(Directory.GetDirectories(directory.DirectoryPath, ".usc-download-*"));
        var calls = rig.Calls.Where(c => c.Contains("--output")).ToArray();
        Assert.Equal(withVideo ? 2 : 1, calls.Length);
        foreach (var args in calls)
        {
            Assert.Equal(OnlineMediaTests.Url, args[^1]);
            Assert.Contains("--no-overwrites", args);
            Assert.DoesNotContain("--extract-audio", args);
            Assert.DoesNotContain("--merge-output-format", args);
            Assert.DoesNotContain("--recode-video", args);
        }
        Assert.DoesNotContain("expiring-secret", await File.ReadAllTextAsync(Path.Combine(destination, "source.json")));
    }

    [Fact]
    public async Task ExistingFolderIsPreservedWithoutToolCall()
    {
        using var directory = new ProjectTestDirectory();
        var destination = directory.FilePath("download");
        Directory.CreateDirectory(destination);
        var existing = Path.Combine(destination, "keep.txt");
        await File.WriteAllTextAsync(existing, "Owner file");
        var rig = new OnlineMediaTests.Rig();
        await Assert.ThrowsAsync<IOException>(() => rig.Source().DownloadAsync(MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140"), destination));
        Assert.Equal("Owner file", await File.ReadAllTextAsync(existing));
        Assert.Empty(rig.Calls);
    }

    [Theory]
    [InlineData("fail-video")]
    [InlineData("residual")]
    public async Task FailedOrIncompleteJobsPublishNothingAndCleanStage(string mode)
    {
        using var directory = new ProjectTestDirectory();
        var rig = new OnlineMediaTests.Rig { Mode = mode };
        await Assert.ThrowsAsync<MediaDownloadException>(() => rig.Source().DownloadAsync(
            MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140", "133"), directory.FilePath("download")));
        Assert.Empty(Directory.GetFileSystemEntries(directory.DirectoryPath));
    }

    [Theory]
    [InlineData("large")]
    [InlineData("large-hang")]
    public async Task ActualAndMonitoredByteLimitsStopAndCleanJobs(string mode)
    {
        using var directory = new ProjectTestDirectory();
        var rig = new OnlineMediaTests.Rig { Mode = mode };
        var error = await Assert.ThrowsAsync<MediaDownloadException>(() => rig.Source(32).DownloadAsync(
            MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140"), directory.FilePath("download")));
        Assert.Equal(MediaDownloadError.DownloadTooLarge, error.Error);
        Assert.Empty(Directory.GetFileSystemEntries(directory.DirectoryPath));
    }

    [Fact]
    public async Task DeclaredLimitAndStaleFormatFailBeforeCreatingStage()
    {
        using var directory = new ProjectTestDirectory();
        var selection = MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140");
        var rig = new OnlineMediaTests.Rig { Body = OnlineMediaTests.Details.Replace("\"filesize\":null", "\"filesize\":2048") };
        Assert.Equal(MediaDownloadError.DownloadTooLarge, (await Assert.ThrowsAsync<MediaDownloadException>(() =>
            rig.Source(32).DownloadAsync(selection, directory.FilePath("download")))).Error);
        rig.Body = OnlineMediaTests.Details.Replace("\"format_id\":\"140\"", "\"format_id\":\"141\"");
        Assert.Equal(MediaDownloadError.InvalidSelection, (await Assert.ThrowsAsync<MediaDownloadException>(() =>
            rig.Source().DownloadAsync(selection, directory.FilePath("download")))).Error);
        Assert.Empty(Directory.GetFileSystemEntries(directory.DirectoryPath));
        Assert.All(rig.Calls, args => Assert.DoesNotContain("--output", args));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WholeJobDeadlineAndCallerCancellationAreDistinctAndCleanStage(bool cancelCaller)
    {
        using var directory = new ProjectTestDirectory();
        var rig = new OnlineMediaTests.Rig { Mode = "hang" };
        using var stop = new CancellationTokenSource();
        if (cancelCaller) stop.CancelAfter(TimeSpan.FromMilliseconds(100));
        var work = rig.Source(timeout: TimeSpan.FromMilliseconds(250)).DownloadAsync(
            MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140"), directory.FilePath("download"), stop.Token);
        if (cancelCaller) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => work);
        else Assert.Equal(MediaDownloadError.TimedOut, (await Assert.ThrowsAsync<MediaDownloadException>(() => work)).Error);
        Assert.Empty(Directory.GetFileSystemEntries(directory.DirectoryPath));
    }

    [Fact]
    public async Task ProbeAndStreamMismatchCleanBeforePublish()
    {
        using var directory = new ProjectTestDirectory();
        var rig = new OnlineMediaTests.Rig();
        rig.Probe.WrongStreams = true;
        await Assert.ThrowsAsync<MediaDownloadException>(() => rig.Source().DownloadAsync(
            MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140"), directory.FilePath("download")));
        Assert.Empty(Directory.GetFileSystemEntries(directory.DirectoryPath));
        rig.Probe.WrongStreams = false;
        rig.Probe.Fail = true;
        await Assert.ThrowsAsync<MediaAcquisitionException>(() => rig.Source().DownloadAsync(
            MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140"), directory.FilePath("download")));
        Assert.Empty(Directory.GetFileSystemEntries(directory.DirectoryPath));
    }

    [Fact]
    public async Task DestinationRacePreservesOtherDirectoryAndCleansOnlyOwnedStage()
    {
        using var directory = new ProjectTestDirectory();
        var rig = new OnlineMediaTests.Rig { Mode = "race-destination" };
        await Assert.ThrowsAsync<IOException>(() => rig.Source().DownloadAsync(
            MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140"), directory.FilePath("download")));
        Assert.True(Directory.Exists(directory.FilePath("download")));
        Assert.Single(Directory.GetDirectories(directory.DirectoryPath));
    }

    [Fact]
    public async Task DraftProvenanceAndExplicitCatalogueChoiceRoundTripOfflineAndExportCurrentMetadata()
    {
        using var directory = new ProjectTestDirectory();
        var rig = new OnlineMediaTests.Rig();
        var media = await rig.Source().DownloadAsync(MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140", "133"), directory.FilePath("download"));
        var recording = RecordingChoices.Select(RecordingSearchTests.Page(), RecordingSearchTests.RecordingId);
        var draft = DownloadedMediaDraft.Create(media, new("Owner title", "Owner artist"), recording);
        Assert.Equal("Owner title", draft.Song.Metadata.Title);
        Assert.Equal("Owner artist", draft.Song.Metadata.Artist);
        Assert.Equal(2, draft.Song.Media.Length);
        Assert.Contains(draft.Diagnostics, d => d.Code == "SourceNeedsManualVerification");
        var song = draft.Song with { Phrases = [new() { StartSeconds = 0, EndSeconds = 2, Notes = [new() { StartSeconds = 0, DurationSeconds = 1, Text = "Own text" }] }] };
        await new ProjectStore().SaveAsync(directory.FilePath(), song);
        var loaded = await new ProjectStore().LoadAsync(directory.FilePath());
        Assert.Equal(song.Metadata, loaded.Song.Metadata);
        var provenance = DownloadedMediaDraft.Read(loaded.Song)!;
        Assert.Equal(OnlineMediaTests.Id, provenance.Id);
        Assert.Equal(media.AudioSha256, provenance.Audio.Sha256);
        Assert.Equal(media.VideoSha256, provenance.Video!.Sha256);
        Assert.Equal(recording.Candidate.Id, RecordingChoices.Read(loaded.Song)!.Candidate.Id);
        var exported = await new UltraStarExporter().PackageAsync(directory.FilePath("export"), loaded.Song, new(ExportFormat.V1, GridBpm: 120));
        Assert.True(exported.Success, string.Join("\n", exported.Diagnostics));
        Assert.Equal(2, exported.Assets.Length);
        Assert.Contains("#TITLE:Owner title", exported.Text);
        Assert.DoesNotContain(DownloadedMediaDraft.HeaderName, exported.Text);
        Assert.DoesNotContain(OnlineMediaTests.Id, exported.Text);
        Assert.DoesNotContain("Own uploader", exported.Text);
        File.Delete(media.Audio.FilePath);
        File.Delete(media.Video!.FilePath);
        Assert.Equal(provenance, DownloadedMediaDraft.Read((await new ProjectStore().LoadAsync(directory.FilePath())).Song));
        Assert.Equal(3, rig.Calls.Count); // No search/probe/network occurs during subsequent storage/export/source reads.
    }

    [Fact]
    public async Task OfflineReferencesAndTrailingDestinationSeparatorRemainUsable()
    {
        using var directory = new ProjectTestDirectory();
        var rig = new OnlineMediaTests.Rig();
        var media = await rig.Source().DownloadAsync(MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140"),
            directory.FilePath("download") + Path.DirectorySeparatorChar);
        var song = DownloadedMediaDraft.Create(media).Song;
        var header = song.ImportedSource!.Headers.Single(h => h.Name == DownloadedMediaDraft.HeaderName);
        var body = JsonNode.Parse(header.Value)!;
        body["audio"]!["location"] = @"Z:\portable\audio.m4a";
        song = song with { ImportedSource = song.ImportedSource with { Headers = [header with { Value = body.ToJsonString() }] } };
        Assert.Equal(@"Z:\portable\audio.m4a", DownloadedMediaDraft.Read(song)!.Audio.Location);
        Assert.True(Directory.Exists(media.DirectoryPath));
    }

    [Theory]
    [InlineData("version")]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("duplicate")]
    [InlineData("null-audio")]
    public async Task FrozenSourceRejectsUnsupportedOrCorruptPayloadWithoutFallback(string mode)
    {
        using var directory = new ProjectTestDirectory();
        var rig = new OnlineMediaTests.Rig();
        var media = await rig.Source().DownloadAsync(MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140"), directory.FilePath("download"));
        var song = DownloadedMediaDraft.Create(media).Song;
        var header = song.ImportedSource!.Headers.Single(h => h.Name == DownloadedMediaDraft.HeaderName);
        var value = header.Value;
        value = mode switch
        {
            "version" => value.Replace("\"version\":1", "\"version\":2"),
            "missing" => value.Replace("\"sourceId\":\"youtube\",", ""),
            "extra" => value.Insert(1, "\"newField\":true,"),
            "duplicate" => value.Insert(1, "\"version\":1,"),
            "null-audio" => value[..value.IndexOf("\"audio\":", StringComparison.Ordinal)] + "\"audio\":null,\"video\":null}",
            _ => value
        };
        var invalid = song with { ImportedSource = song.ImportedSource with { Headers = [header with { Value = value }] } };
        Assert.Equal(MediaDownloadError.InvalidData, Assert.Throws<MediaDownloadException>(() => DownloadedMediaDraft.Read(invalid)).Error);
        Assert.Equal(2, rig.Calls.Count);
    }
}
