using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Projects;

public sealed class ProjectStore
{
    public const long DefaultMaximumFileBytes = 64 * 1024 * 1024;
    private readonly long maximumFileBytes;

    public ProjectStore(long maximumFileBytes = DefaultMaximumFileBytes)
    {
        if (maximumFileBytes is <= 0 or > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(maximumFileBytes));
        this.maximumFileBytes = maximumFileBytes;
    }

    public async Task SaveAsync(string filePath, Song song, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var target = Path.GetFullPath(filePath);
        var bytes = ProjectJson.Encode(song);
        CheckLength(bytes.LongLength);
        var directory = Path.GetDirectoryName(target)!;
        var temporary = Path.Combine(directory, "." + Path.GetFileName(target) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        var created = false;
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 65536, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                created = true;
                await stream.WriteAsync(bytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, target, overwrite: true);
            created = false;
        }
        finally
        {
            if (created)
            {
                try { File.Delete(temporary); }
                catch (IOException) { /* Best effort; preserve the original operation's exception. */ }
                catch (UnauthorizedAccessException) { /* Locked temp files may remain for later cleanup. */ }
            }
        }
    }

    public async Task<LoadedProject> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.GetFullPath(filePath);
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete,
            65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        CheckLength(stream.Length);
        var bytes = new byte[(int)stream.Length];
        await stream.ReadExactlyAsync(bytes, cancellationToken);
        if (await stream.ReadAsync(new byte[1], cancellationToken) != 0)
            throw new IOException("Project file changed while being read.");
        cancellationToken.ThrowIfCancellationRequested();
        var (song, issues, version) = ProjectJson.Decode(bytes);
        return new(path, version, song, issues);
    }

    private void CheckLength(long length)
    {
        if (length > maximumFileBytes)
            throw new ProjectFormatException(ProjectError.TooLarge, "Project exceeds the configured file-size limit.");
    }
}
