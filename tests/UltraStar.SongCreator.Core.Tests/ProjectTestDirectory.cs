namespace UltraStar.SongCreator.Core.Tests;

internal sealed class ProjectTestDirectory : IDisposable
{
    internal static string RepositoryRoot
    {
        get
        {
            for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "UltraStar.SongCreator.slnx"))) return d.FullName;
            throw new InvalidOperationException("Tests require a checkout; no scratch outside the project.");
        }
    }

    private static string ScratchRoot => Path.Combine(RepositoryRoot, ".agent-local", "project-storage-tests");
    public string DirectoryPath { get; } = Path.Combine(ScratchRoot, Guid.NewGuid().ToString("N"));

    public ProjectTestDirectory() => Directory.CreateDirectory(DirectoryPath);
    public string FilePath(string name = "song.uscproject") => Path.Combine(DirectoryPath, name);

    public void Dispose()
    {
        var absolute = Path.GetFullPath(DirectoryPath);
        if (!absolute.StartsWith(Path.GetFullPath(ScratchRoot) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing cleanup outside project test scratch.");
        if (Directory.Exists(absolute)) Directory.Delete(absolute, recursive: true);
    }
}
