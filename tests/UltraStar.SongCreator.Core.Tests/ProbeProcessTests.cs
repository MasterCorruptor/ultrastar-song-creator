using System.Text.Json;
using UltraStar.SongCreator.Acquisition;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class ProbeProcessTests
{
    private static string Dotnet => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!,
        "..", "..", "..", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet"));
    private static string Host => Path.Combine(AppContext.BaseDirectory, "ProbeHost", "MediaProbe.TestHost.dll");

    private static Task<ProbeOutput> Run(string mode, CancellationToken token = default, TimeSpan? timeout = null, int limit = 1024 * 1024) =>
        ProbeProcess.RunAsync(Dotnet, [Host, mode], timeout ?? TimeSpan.FromSeconds(10), limit, token);

    [Fact]
    public async Task ArgumentsWithSpacesQuotesAndShellMetacharactersStayLiteral()
    {
        Assert.True(File.Exists(Host), "The synthetic child-process test fixture must be copied by the build.");
        var input = new[] { "own file.wav", "a\"b", "$(never run)", "; & |", "æøå", "-option" };
        var output = await ProbeProcess.RunAsync(Dotnet, new[] { Host, "arguments" }.Concat(input),
            TimeSpan.FromSeconds(10), 1024 * 1024, default);
        Assert.Equal(0, output.ExitCode);
        Assert.Equal(input, JsonSerializer.Deserialize<string[]>(output.StandardOutput));
        Assert.Equal("", output.StandardError);
    }

    [Fact]
    public async Task FailureRetainsExitCodeAndDrainsStderr()
    {
        var output = await Run("failure");
        Assert.Equal(7, output.ExitCode);
        Assert.Equal("Synthetic failure", output.StandardError);
    }

    [Theory]
    [InlineData("stdout-limit")]
    [InlineData("stderr-limit")]
    public async Task OversizedOutputStopsHangingProcess(string mode) =>
        Assert.Equal(AcquisitionError.ProbeOutputTooLarge,
            (await Assert.ThrowsAsync<MediaAcquisitionException>(() => Run(mode, limit: 1024))).Error);

    [Fact]
    public async Task TimeoutStopsHangingProcessAndHasDistinctError() =>
        Assert.Equal(AcquisitionError.ProbeTimedOut,
            (await Assert.ThrowsAsync<MediaAcquisitionException>(() => Run("hang", timeout: TimeSpan.FromMilliseconds(500)))).Error);

    [Fact]
    public async Task CallerCancellationStopsHangingProcess()
    {
        using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Run("hang", stop.Token));
    }

    [Fact]
    public async Task PrecancelledProbeDoesNotStart()
    {
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ProbeProcess.RunAsync("missing-executable", [], TimeSpan.FromSeconds(10), 1024, stop.Token));
    }

    [Fact]
    public async Task InvalidUtf8IsRejectedRatherThanReplaced() =>
        Assert.Equal(AcquisitionError.InvalidProbeData,
            (await Assert.ThrowsAsync<MediaAcquisitionException>(() => Run("invalid-utf8"))).Error);
}
