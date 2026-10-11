namespace UltraStar.SongCreator.Acquisition;

/// <summary>Reads a selected file; neither copies nor modifies it or downloads media.</summary>
public sealed class LocalMediaSource(IMediaProbe probe) : IMediaSource
{
    private readonly IMediaProbe probe = probe ?? throw new ArgumentNullException(nameof(probe));
    public string SourceId => "local-file";

    public async Task<MediaCandidate> InspectAsync(string absoluteFilePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = LocalPath(absoluteFilePath);
        var file = new FileInfo(path);
        if (!file.Exists) throw new FileNotFoundException("Selected media file does not exist.", path);
        var length = file.Length;
        var modified = file.LastWriteTimeUtc;
        if (length == 0) throw new MediaAcquisitionException(AcquisitionError.UnsupportedMedia, "Selected media file is empty.");
        var inspection = await probe.InspectAsync(path, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        file.Refresh();
        if (!file.Exists || file.Length != length || file.LastWriteTimeUtc != modified)
            throw new IOException("Media file changed while it was being inspected.");
        return new(SourceId, path, length, inspection);
    }

    internal static string LocalPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Path.IsPathFullyQualified(path) || (Uri.TryCreate(path, UriKind.Absolute, out var uri) && !uri.IsFile))
            throw new ArgumentException("Select a fully qualified filesystem path; URLs and working-directory guesses are not supported.", nameof(path));
        return Path.GetFullPath(path);
    }
}
