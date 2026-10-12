using System.Security.Cryptography;

namespace UltraStar.SongCreator.Acquisition;

/// <summary>Optional Python-wheel yt-dlp process. No PATH lookup, tool installation, account or automatic first-source selection.</summary>
public sealed class YtDlpMediaSource : IMediaDownloadSource
{
    private readonly Func<IReadOnlyList<string>, TimeSpan, CancellationToken, Task<ProbeOutput>> run;
    private readonly LocalMediaSource local;
    private readonly TimeSpan metadataTimeout;
    private readonly TimeSpan downloadTimeout;
    private readonly long maximumDownloadBytes;
    private readonly string[] common;
    public string SourceId => "youtube";

    public YtDlpMediaSource(string pythonPath, string javascriptPath, IMediaProbe probe, string javascriptRuntime = "node",
        TimeSpan? metadataTimeout = null, TimeSpan? downloadTimeout = null, long maximumDownloadBytes = 1024L * 1024 * 1024)
        : this(probe, (args, timeout, token) => ProbeProcess.RunAsync(LocalMediaSource.LocalPath(pythonPath),
            new[] { "-I", "-X", "utf8", "-m", "yt_dlp" }.Concat(args), timeout, 8 * 1024 * 1024, token),
            LocalMediaSource.LocalPath(javascriptPath), javascriptRuntime,
            metadataTimeout ?? TimeSpan.FromSeconds(45), downloadTimeout ?? TimeSpan.FromMinutes(15), maximumDownloadBytes)
    {
        _ = LocalMediaSource.LocalPath(pythonPath);
    }

    internal YtDlpMediaSource(IMediaProbe probe, Func<IReadOnlyList<string>, TimeSpan, CancellationToken, Task<ProbeOutput>> run,
        string javascriptPath, string javascriptRuntime, TimeSpan metadataTimeout, TimeSpan downloadTimeout, long maximumDownloadBytes)
    {
        local = new(probe);
        this.run = run ?? throw new ArgumentNullException(nameof(run));
        if (javascriptRuntime is not ("node" or "deno")) throw new ArgumentException("Configure node or deno explicitly.", nameof(javascriptRuntime));
        javascriptPath = LocalMediaSource.LocalPath(javascriptPath);
        if (metadataTimeout <= TimeSpan.Zero || metadataTimeout > TimeSpan.FromMinutes(5)) throw new ArgumentOutOfRangeException(nameof(metadataTimeout));
        if (downloadTimeout <= TimeSpan.Zero || downloadTimeout > TimeSpan.FromHours(2)) throw new ArgumentOutOfRangeException(nameof(downloadTimeout));
        if (maximumDownloadBytes <= 0 || maximumDownloadBytes > 16L * 1024 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(maximumDownloadBytes));
        this.metadataTimeout = metadataTimeout;
        this.downloadTimeout = downloadTimeout;
        this.maximumDownloadBytes = maximumDownloadBytes;
        common = ["--ignore-config", "--no-plugin-dirs", "--no-remote-components", "--no-js-runtimes", "--js-runtimes", javascriptRuntime + ":" + javascriptPath,
            "--no-cookies", "--no-cookies-from-browser", "--no-cache-dir", "--no-update",
            "--use-extractors", "youtube,youtube:search", "--no-playlist", "--no-progress", "--color", "never",
            "--socket-timeout", "15", "--retries", "0", "--fragment-retries", "0", "--extractor-retries", "0", "--fixup", "never"];
    }

    public async Task<MediaSearchPage> SearchAsync(MediaSearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Limit is < 1 or > 25) throw new ArgumentOutOfRangeException(nameof(query));
        var fields = MusicBrainzRecordingProvider.Normalize(new(query.Artist, query.Title));
        var normalized = query with { Artist = fields.Artist, Title = fields.Title };
        var term = string.Join(" ", new[] { fields.Artist, fields.Title }.Where(f => f is not null));
        var output = await RunAsync(["--flat-playlist", "--dump-single-json", "--", "ytsearch" + query.Limit + ":" + term], metadataTimeout, cancellationToken);
        return YtDlpJson.Search(output.StandardOutput, normalized, DateTimeOffset.UtcNow);
    }

    public async Task<MediaSourceDetails> ResolveAsync(string sourceUrl, CancellationToken cancellationToken = default)
    {
        var url = YtDlpJson.CanonicalUrl(sourceUrl);
        var output = await RunAsync(["--dump-single-json", "--", url], metadataTimeout, cancellationToken);
        return YtDlpJson.Details(output.StandardOutput, url, DateTimeOffset.UtcNow);
    }

    private async Task<ProbeOutput> RunAsync(string[] arguments, TimeSpan timeout, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        ProbeOutput output;
        try { output = await run(common.Concat(arguments).ToArray(), timeout, token); }
        catch (MediaAcquisitionException e)
        {
            var error = e.Error switch
            {
                AcquisitionError.ProbeUnavailable => MediaDownloadError.ToolUnavailable,
                AcquisitionError.ProbeTimedOut => MediaDownloadError.TimedOut,
                AcquisitionError.ProbeOutputTooLarge => MediaDownloadError.OutputTooLarge,
                AcquisitionError.InvalidProbeData => MediaDownloadError.InvalidData,
                _ => MediaDownloadError.ToolFailed
            };
            throw new MediaDownloadException(error, "Configured media tool could not complete the request.", inner: e);
        }
        token.ThrowIfCancellationRequested();
        if (output.ExitCode != 0) throw new MediaDownloadException(MediaDownloadError.ToolFailed,
            "yt-dlp could not complete this request. Source/network/runtime availability must be checked; no account/cookie fallback is attempted.", output.ExitCode);
        return output;
    }

    public async Task<DownloadedMedia> DownloadAsync(MediaDownloadSelection selection, string newDirectoryPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MediaDownloadChoices.Validate(selection);
        var destination = Path.TrimEndingDirectorySeparator(LocalMediaSource.LocalPath(newDirectoryPath));
        var parent = Path.GetDirectoryName(destination);
        if (parent is null || !Directory.Exists(parent)) throw new DirectoryNotFoundException("Select a new directory under an existing parent.");
        if (Path.Exists(destination)) throw new IOException("Download destination already exists; no files are overwritten.");
        using var deadline = new CancellationTokenSource(downloadTimeout);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        using var watcherStop = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
        string? stage = null;
        Task? watching = null;
        var tooLarge = 0;
        try
        {
            var fresh = await ResolveAsync(selection.Details.Candidate.SourceUrl, stop.Token);
            var chosen = MediaDownloadChoices.Select(fresh, selection.Audio.Id, selection.Video?.Id);
            bool SameProfile(MediaDownloadFormat a, MediaDownloadFormat b) =>
                a.Extension == b.Extension && a.AudioCodec == b.AudioCodec && a.VideoCodec == b.VideoCodec && a.Width == b.Width && a.Height == b.Height;
            if (!SameProfile(chosen.Audio, selection.Audio) || (chosen.Video is { } video && !SameProfile(video, selection.Video!)))
                throw new MediaDownloadException(MediaDownloadError.InvalidSelection, "Selected format changed; inspect and choose again.");
            var declared = (decimal)(chosen.Audio.ByteLength ?? 0) + (chosen.Video?.ByteLength ?? 0);
            if (declared > maximumDownloadBytes) throw new MediaDownloadException(MediaDownloadError.DownloadTooLarge, "Declared media exceeds the combined byte limit.");
            var newStage = Path.Combine(parent, ".usc-download-" + Guid.NewGuid().ToString("N"));
            if (Path.Exists(newStage)) throw new IOException("Staging path already exists.");
            Directory.CreateDirectory(newStage);
            stage = newStage;

            async Task WatchAsync()
            {
                try
                {
                    while (true)
                    {
                        await Task.Delay(100, watcherStop.Token);
                        decimal bytes = 0;
                        foreach (var file in Directory.EnumerateFiles(stage))
                        {
                            var info = new FileInfo(file);
                            info.Refresh();
                            if (!info.Exists) continue; // A regular .part rename is allowed between enumeration and observation.
                            if ((info.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Unexpected link in staging.");
                            bytes += info.Length;
                        }
                        if (bytes > maximumDownloadBytes)
                        {
                            Interlocked.Exchange(ref tooLarge, 1);
                            stop.Cancel();
                            return;
                        }
                    }
                }
                catch (OperationCanceledException) when (watcherStop.IsCancellationRequested) { }
                catch { stop.Cancel(); throw; }
            }
            watching = WatchAsync();
            async Task<MediaCandidate> FetchAsync(MediaDownloadFormat format, string name, bool audio)
            {
                var path = Path.Combine(stage, name + "." + format.Extension);
                await RunAsync(["--no-simulate", "--no-overwrites", "--no-continue", "--no-mtime",
                    "--max-filesize", maximumDownloadBytes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "--format", format.Id, "--output", path, "--paths", "home:" + stage, "--paths", "temp:" + stage,
                    "--", fresh.Candidate.SourceUrl], downloadTimeout, stop.Token);
                if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new MediaDownloadException(MediaDownloadError.InvalidData, "Tool did not produce the selected regular media file.");
                var media = await local.InspectAsync(path, stop.Token);
                var streams = media.Inspection.Streams;
                if (audio ? streams.Count(s => s.IsAudio) != 1 || streams.Any(s => s.IsVideo) :
                    streams.Count(s => s.IsVideo) != 1 || streams.Any(s => s.IsAudio))
                    throw new MediaDownloadException(MediaDownloadError.InvalidData, "Downloaded streams differ from the selected separate audio/video profile.");
                return media;
            }
            var audio = await FetchAsync(chosen.Audio, "audio", true);
            var videoFile = chosen.Video is null ? null : await FetchAsync(chosen.Video, "video", false);
            if ((decimal)audio.ByteLength + (videoFile?.ByteLength ?? 0) > maximumDownloadBytes)
                throw new MediaDownloadException(MediaDownloadError.DownloadTooLarge, "Downloaded media exceeds the combined byte limit.");
            var expected = new[] { audio.FilePath, videoFile?.FilePath }.Where(p => p is not null).ToHashSet(StringComparer.Ordinal);
            if (Directory.EnumerateDirectories(stage).Any() || Directory.EnumerateFiles(stage).Any(p => !expected.Contains(p)))
                throw new MediaDownloadException(MediaDownloadError.InvalidData, "Unexpected residual output; incomplete jobs are not published.");
            async Task<string> HashAsync(string path)
            {
                await using var file = File.OpenRead(path);
                return Convert.ToHexStringLower(await SHA256.HashDataAsync(file, stop.Token));
            }
            var result = new DownloadedMedia(chosen,
                audio with { FilePath = Path.Combine(destination, Path.GetFileName(audio.FilePath)) },
                videoFile is null ? null : videoFile with { FilePath = Path.Combine(destination, Path.GetFileName(videoFile.FilePath)) },
                await HashAsync(audio.FilePath), videoFile is null ? null : await HashAsync(videoFile.FilePath),
                DateTimeOffset.UtcNow, destination);
            watcherStop.Cancel();
            await watching;
            stop.Token.ThrowIfCancellationRequested();
            await File.WriteAllTextAsync(Path.Combine(stage, "source.json"), DownloadedMediaDraft.Serialize(result), stop.Token);
            stop.Token.ThrowIfCancellationRequested();
            Directory.Move(stage, destination); // Commit point: late cancellation must not report a published result as failed.
            stage = null;
            return result;
        }
        catch (OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Volatile.Read(ref tooLarge) != 0) throw new MediaDownloadException(MediaDownloadError.DownloadTooLarge, "Staging exceeded the byte limit.");
            if (deadline.IsCancellationRequested) throw new MediaDownloadException(MediaDownloadError.TimedOut, "Download exceeded the whole-job deadline.");
            throw;
        }
        finally
        {
            stop.Cancel();
            watcherStop.Cancel();
            try { if (watching is not null) await watching; }
            finally
            {
                if (stage is not null && Directory.Exists(stage))
                {
                    var absolute = Path.GetFullPath(stage);
                    if (Path.GetDirectoryName(absolute) != parent || !Path.GetFileName(absolute).StartsWith(".usc-download-", StringComparison.Ordinal))
                        throw new IOException("Refusing cleanup outside this download's staging directory.");
                    Directory.Delete(absolute, recursive: true);
                }
            }
        }
    }
}
