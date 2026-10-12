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
        async Task<ProbeOutput> Run(IReadOnlyList<string> args, TimeSpan timeout, CancellationToken token)
        {
            calls.Add(args.ToArray());
            return await ProbeProcess.RunAsync(Dotnet, new[] { Host, "media-tool", fixture }.Concat(args), timeout, 1024 * 1024, token);
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
            (args, timeout, token) => ProbeProcess.RunAsync(Dotnet, [Host, mode], timeout, 1024, token),
            Dotnet, "node", TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(5), 1024);
        var failure = await Assert.ThrowsAsync<MediaDownloadException>(() => source.SearchAsync(new(Title: "Own")));
        Assert.Equal(expected, failure.Error);
        if (mode == "failure") Assert.Equal(7, failure.ExitCode);
    }

    [Fact]
    public async Task RealChildCallerCancellationIsPreserved()
    {
        var source = new YtDlpMediaSource(new OnlineMediaTests.Probe(),
            (args, timeout, token) => ProbeProcess.RunAsync(Dotnet, [Host, "hang"], timeout, 1024, token),
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
        var source = new YtDlpMediaSource(new OnlineMediaTests.Probe(), async (args, timeout, token) =>
        {
            var result = await ProbeProcess.RunAsync(Dotnet, new[] { Host, "arguments" }.Concat(args), timeout, 1024 * 1024, token);
            observed = JsonSerializer.Deserialize<string[]>(result.StandardOutput);
            return new(0, OnlineMediaTests.Search, "");
        }, Dotnet, "node", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), 1024);
        await source.SearchAsync(new(Title: "æøå \" ; & $(literal)", Limit: 3));
        Assert.Equal("ytsearch3:æøå \" ; & $(literal)", observed![^1]);
    }
}
