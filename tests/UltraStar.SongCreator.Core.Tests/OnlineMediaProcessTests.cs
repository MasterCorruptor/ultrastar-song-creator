using System.Text.Json;
using UltraStar.SongCreator.Acquisition;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class OnlineMediaProcessTests
{
    private static string Dotnet => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "..", "..", "..",
        OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));
    private static string Host => Path.Combine(AppContext.BaseDirectory, "ProbeHost", "MediaProbe.TestHost.dll");

    [Fact]
    public async Task RealChildWritesSelectedFilesThroughLiteralArgumentBoundary()
    {
        using var directory = new ProjectTestDirectory();
        var fixture = directory.FilePath("metadata with spaces.json");
        await File.WriteAllTextAsync(fixture, OnlineMediaTests.Details);
        var calls = new List<string[]>();
        async Task<ProbeOutput> Run(IReadOnlyList<string> args, TimeSpan timeout, CancellationToken token, string? workingDirectory)
        {
            calls.Add(args.ToArray());
            return await ProbeProcess.RunAsync(Dotnet, new[] { Host, "media-tool", fixture }.Concat(args), timeout, 1024 * 1024, token, workingDirectory);
        }
        var source = new YtDlpMediaSource(new OnlineMediaTests.Probe(), Run, Dotnet, "node", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), 1024);
        var result = await source.DownloadAsync(MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140", "133"), directory.FilePath("download"));
        Assert.Equal("Own synthetic media", await File.ReadAllTextAsync(result.Audio.FilePath));
        Assert.Equal("Own synthetic media", await File.ReadAllTextAsync(result.Video!.FilePath));
        Assert.Equal(3, calls.Count);
        Assert.All(calls, args => Assert.Equal(OnlineMediaTests.Url, args[^1]));
        Assert.Empty(Directory.GetDirectories(directory.DirectoryPath, ".usc-download-*"));
    }

    [Theory]
    [InlineData("stdout-limit", MediaDownloadError.OutputTooLarge)]
    [InlineData("stderr-limit", MediaDownloadError.OutputTooLarge)]
    [InlineData("invalid-utf8", MediaDownloadError.InvalidData)]
    [InlineData("hang", MediaDownloadError.TimedOut)]
    [InlineData("failure", MediaDownloadError.ToolFailed)]
    public async Task RealChildFailureAndOutputBoundsMapToMediaErrors(string mode, MediaDownloadError expected)
    {
        var source = new YtDlpMediaSource(new OnlineMediaTests.Probe(),
            (args, timeout, token, workingDirectory) => ProbeProcess.RunAsync(Dotnet, [Host, mode], timeout, 1024, token, workingDirectory),
            Dotnet, "node", TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(5), 1024);
        var failure = await Assert.ThrowsAsync<MediaDownloadException>(() => source.SearchAsync(new(Title: "Own")));
        Assert.Equal(expected, failure.Error);
        if (mode == "failure") Assert.Equal(7, failure.ExitCode);
    }

    [Fact]
    public async Task RealChildCallerCancellationIsPreserved()
    {
        var source = new YtDlpMediaSource(new OnlineMediaTests.Probe(),
            (args, timeout, token, workingDirectory) => ProbeProcess.RunAsync(Dotnet, [Host, "hang"], timeout, 1024, token, workingDirectory),
            Dotnet, "node", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), 1024);
        using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.SearchAsync(new(Title: "Own"), stop.Token));
    }

    [Fact]
    public async Task MissingPublicConfiguredPythonIsToolUnavailable()
    {
        using var directory = new ProjectTestDirectory();
        var source = new YtDlpMediaSource(directory.FilePath("missing-python"), Dotnet, new OnlineMediaTests.Probe());
        Assert.Equal(MediaDownloadError.ToolUnavailable, (await Assert.ThrowsAsync<MediaDownloadException>(() => source.SearchAsync(new(Title: "Own")))).Error);
        Assert.Throws<ArgumentException>(() => new YtDlpMediaSource("relative-python", Dotnet, new OnlineMediaTests.Probe()));
    }

    [Fact]
    public async Task UnicodeAndShellSyntaxReachChildAsLiteralSearchArgument()
    {
        string[]? observed = null;
        var source = new YtDlpMediaSource(new OnlineMediaTests.Probe(), async (args, timeout, token, workingDirectory) =>
        {
            var result = await ProbeProcess.RunAsync(Dotnet, new[] { Host, "arguments" }.Concat(args), timeout, 1024 * 1024, token, workingDirectory);
            observed = JsonSerializer.Deserialize<string[]>(result.StandardOutput);
            return new(0, OnlineMediaTests.Search, "");
        }, Dotnet, "node", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), 1024);
        await source.SearchAsync(new(Title: "æøå \" ; & $(literal)", Limit: 3));
        Assert.Equal("ytsearch3:æøå \" ; & $(literal)", observed![^1]);
    }

    [Theory]
    [InlineData("%(title)s")]
    [InlineData("%(id)s")]
    [InlineData("%PATH%")]
    [InlineData("$PATH")]
    [InlineData("${PATH}")]
    [InlineData("ordinary%percent$dollar")]
    [InlineData("double%%percent$$dollar")]
    [InlineData("spaces æøå")]
    public async Task NativeDownloadCwdKeepsLiteralParentsAndCleansAfterFirstFile(string parentName)
    {
        foreach (var outcome in new[] { "success", "failure", "cancel" })
        {
            using var directory = new ProjectTestDirectory();
            var parent = directory.FilePath(parentName);
            Directory.CreateDirectory(parent);
            var destination = Path.Combine(parent, "download");
            var fixture = directory.FilePath("metadata.json");
            await File.WriteAllTextAsync(fixture, OnlineMediaTests.Details);
            using var stop = new CancellationTokenSource();
            async Task<ProbeOutput> Run(IReadOnlyList<string> args, TimeSpan timeout, CancellationToken token, string? workingDirectory)
            {
                var outputPosition = args.ToList().IndexOf("--output");
                if (outputPosition >= 0)
                {
                    Assert.DoesNotContain("--paths", args);
                    Assert.Equal(parent, Path.GetDirectoryName(workingDirectory));
                    Assert.Contains(args[outputPosition + 1], new[] { "audio.m4a", "video.mp4" });
                    if (args[outputPosition + 1] == "video.mp4" && outcome != "success")
                    {
                        Assert.True(File.Exists(Path.Combine(workingDirectory!, "audio.m4a")));
                        if (outcome == "failure") return new(7, "", "Own controlled failure");
                        stop.Cancel();
                        token.ThrowIfCancellationRequested();
                    }
                }
                return await ProbeProcess.RunAsync(Dotnet, new[] { Host, "media-tool", fixture }.Concat(args),
                    timeout, 1024 * 1024, token, workingDirectory);
            }
            var source = new YtDlpMediaSource(new OnlineMediaTests.Probe(), Run, Dotnet, "node",
                TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), 1024);
            var work = source.DownloadAsync(MediaDownloadChoices.Select(OnlineMediaTests.Page(), "140", "133"), destination, stop.Token);
            if (outcome == "success")
            {
                var result = await work;
                Assert.Equal(Path.Combine(destination, "audio.m4a"), result.Audio.FilePath);
                Assert.Equal("Own synthetic media", await File.ReadAllTextAsync(result.Audio.FilePath));
                Assert.Equal("Own synthetic media", await File.ReadAllTextAsync(result.Video!.FilePath));
                Assert.Equal(new[] { destination }, Directory.GetDirectories(parent));
            }
            else
            {
                if (outcome == "failure") await Assert.ThrowsAsync<MediaDownloadException>(() => work);
                else await Assert.ThrowsAnyAsync<OperationCanceledException>(() => work);
                Assert.Empty(Directory.GetFileSystemEntries(parent));
            }
            Assert.Equal(new[] { parent }, Directory.GetDirectories(directory.DirectoryPath));
        }
    }
}
