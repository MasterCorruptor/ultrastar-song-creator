namespace UltraStar.SongCreator.Acquisition;

/// <summary>A separately configured, replaceable FFprobe process; no tool download or PATH search.</summary>
public sealed class FfprobeMediaProbe : IMediaProbe
{
    private readonly string executable;
    private readonly TimeSpan timeout;
    private readonly int maximumOutputBytes;

    public FfprobeMediaProbe(string executablePath, TimeSpan? timeout = null, int maximumOutputBytes = 1024 * 1024)
    {
        executable = LocalMediaSource.LocalPath(executablePath);
        this.timeout = timeout ?? TimeSpan.FromSeconds(30);
        if (this.timeout <= TimeSpan.Zero || this.timeout > TimeSpan.FromMinutes(5))
            throw new ArgumentOutOfRangeException(nameof(timeout));
        if (maximumOutputBytes is <= 0 or > 32 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(maximumOutputBytes));
        this.maximumOutputBytes = maximumOutputBytes;
    }

    public async Task<MediaInspection> InspectAsync(string absoluteFilePath, CancellationToken cancellationToken = default)
    {
        var file = LocalMediaSource.LocalPath(absoluteFilePath);
        var result = await ProbeProcess.RunAsync(executable,
        [
            "-v", "error", "-protocol_whitelist", "file",
            "-format_whitelist", "wav,mp3,flac,ogg,aac,mov",
            "-show_entries", "format=format_name,duration:format_tags:stream=index,codec_type,codec_name,sample_rate,channels,width,height,duration:stream_disposition=attached_pic:stream_tags",
            "-of", "json", "-i", file
        ], timeout, maximumOutputBytes, cancellationToken);
        if (result.ExitCode != 0)
            throw new MediaAcquisitionException(AcquisitionError.ProbeFailed, $"FFprobe exited with status {result.ExitCode}; the selected file was not imported.");
        return ProbeJson.Parse(result.StandardOutput);
    }
}
