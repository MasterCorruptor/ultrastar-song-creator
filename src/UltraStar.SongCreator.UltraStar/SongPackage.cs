using System.Collections.Immutable;
using System.Security.Cryptography;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.UltraStar;

public sealed partial class UltraStarExporter
{
    /// <summary>Publishes a new folder with song.txt and verified local asset copies. Never overwrites a folder.</summary>
    public async Task<ExportResult> PackageAsync(string destinationDirectory, Song song, ExportOptions options,
        IProgress<ExportProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);
        cancellationToken.ThrowIfCancellationRequested();
        var initial = Render(song, options);
        if (!initial.Success) return initial;
        var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destinationDirectory));
        var parent = Path.GetDirectoryName(target) ?? throw new ArgumentException("A root directory is not an export destination.", nameof(destinationDirectory));
        if (!Directory.Exists(parent)) throw new DirectoryNotFoundException("The export parent directory must already exist.");
        if (Directory.Exists(target) || File.Exists(target)) throw new IOException("The export destination already exists; no overwrite is performed.");

        var diagnostics = initial.Diagnostics.ToBuilder();
        var references = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var copies = new Dictionary<string, (string Relative, long Length)>(comparer);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var declared = new List<(string Header, string Value)>
        {
            ("MP3", song.Media.Single(m => m.Kind == MediaKind.Audio).Location)
        };
        if (song.Media.SingleOrDefault(m => m.Kind == MediaKind.Video) is { } video) declared.Add(("VIDEO", video.Location));
        foreach (var header in song.ImportedSource?.Headers ?? [])
            if (AssetHeaders.Contains(header.Name, StringComparer.OrdinalIgnoreCase) && header.Value.Trim().Length > 0)
                declared.Add((header.Name, header.Value.Trim()));

        foreach (var (header, value) in declared)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = LocalReference(value, song.ImportedSource?.FileReference, options.ReferenceDirectory);
            if (path is null)
            {
                diagnostics.Add(new(ValidationSeverity.Error,
                    IsRemote(value) ? ExportCode.NonLocalReference : ExportCode.MissingReferenceDirectory,
                    $"Header.{header}", null, "A local file and an explicit/absolute source directory are required; no working-directory or network fallback is used."));
                continue;
            }
            if (!copies.TryGetValue(path, out var copy))
            {
                var file = new FileInfo(path);
                if (!file.Exists) throw new FileNotFoundException($"Referenced #{header} file is missing; no finished export was created.", path);
                var name = PortableName(file.Name);
                var stem = Path.GetFileNameWithoutExtension(name);
                var extension = Path.GetExtension(name);
                var unique = name;
                var suffix = 2;
                while (!names.Add(unique)) unique = stem + "-" + suffix++ + extension;
                copy = ("media/" + unique, file.Length);
                copies.Add(path, copy);
            }
            references[header] = copy.Relative;
        }
        if (diagnostics.Any(d => d.Severity == ValidationSeverity.Error))
            return new(null, diagnostics.ToImmutable(), initial.Notes);

        var result = Render(song, options, references);
        if (!result.Success) return result;
        var staging = Path.Combine(parent, "." + Path.GetFileName(target) + "." + Guid.NewGuid().ToString("N") + ".export-tmp");
        var created = false;
        var assets = ImmutableArray.CreateBuilder<PackagedAsset>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Creation is never a reason to recursively clean an existing directory.
            if (Directory.Exists(staging) || File.Exists(staging)) throw new IOException("Export staging path already exists.");
            Directory.CreateDirectory(staging);
            created = true;
            Directory.CreateDirectory(Path.Combine(staging, "media"));
            foreach (var pair in copies)
            {
                var output = Path.GetFullPath(Path.Combine(staging, pair.Value.Relative.Replace('/', Path.DirectorySeparatorChar)));
                if (!Within(output, staging)) throw new IOException("Generated asset path escapes staging.");
                var checksum = await CopyVerifiedAsync(pair.Key, output, pair.Value.Length, pair.Value.Relative, progress, cancellationToken);
                assets.Add(new(pair.Key, pair.Value.Relative, pair.Value.Length, checksum));
            }
            await using (var text = new FileStream(Path.Combine(staging, "song.txt"), FileMode.CreateNew,
                FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await text.WriteAsync(Utf8.GetBytes(result.Text!), cancellationToken);
                await text.FlushAsync(cancellationToken);
                text.Flush(flushToDisk: true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            Directory.Move(staging, target);
            created = false;
            return result with { DirectoryPath = target, Assets = assets.ToImmutable() };
        }
        finally
        {
            if (created)
            {
                try
                {
                    // Only the unique, created child may be removed; never the target/parent/source.
                    if (!Within(staging, parent) || Path.GetFullPath(staging) == target ||
                        (File.GetAttributes(staging) & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Unsafe staging cleanup path.");
                    Directory.Delete(staging, recursive: true);
                }
                catch (IOException) { /* Preserve the original failure; interrupted cleanup may leave staging. */ }
                catch (UnauthorizedAccessException) { /* Locked files may remain for manual cleanup. */ }
            }
        }
    }

    private static bool Within(string path, string directory) =>
        Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static bool IsRemote(string value) =>
        !Path.IsPathFullyQualified(value.Replace('\\', Path.DirectorySeparatorChar)) &&
        Uri.TryCreate(value, UriKind.Absolute, out _);

    private static string? LocalReference(string value, string? sourceFile, string? referenceDirectory)
    {
        var normalized = value.Replace('\\', Path.DirectorySeparatorChar);
        if (Path.IsPathFullyQualified(normalized)) return Path.GetFullPath(normalized);
        if (Path.IsPathRooted(normalized) || IsRemote(value)) return null;
        var directory = referenceDirectory;
        if (directory is null && sourceFile is not null && Path.IsPathFullyQualified(sourceFile))
            directory = Path.GetDirectoryName(sourceFile);
        if (directory is null || !Path.IsPathFullyQualified(directory)) return null;
        return Path.GetFullPath(Path.Combine(directory, normalized));
    }

    private static string PortableName(string original)
    {
        // Files are renamed independently of lyric/metadata Unicode, for portable Windows/Linux packages.
        var name = new string(original.Select(c =>
            char.IsAsciiLetterOrDigit(c) || c is ' ' or '.' or '_' or '-' ? c : '_').ToArray()).Trim(' ', '.');
        if (name.Length == 0) name = "asset";
        if (name.Length > 120)
        {
            var extension = Path.GetExtension(name);
            if (extension.Length > 20) extension = "";
            name = (name[..(120 - extension.Length)] + extension).TrimEnd(' ', '.');
        }
        var stem = Path.GetFileNameWithoutExtension(name).TrimEnd(' ', '.').ToUpperInvariant();
        if (stem is "CON" or "PRN" or "AUX" or "NUL" ||
            (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) &&
                stem[3] is >= '1' and <= '9'))
            name = "_" + name;
        return name;
    }

    private static async Task<string> CopyVerifiedAsync(string sourcePath, string destinationPath, long expectedLength, string assetReference, IProgress<ExportProgress>? progress, CancellationToken cancellationToken)
    {
        byte[] sourceDigest;
        await using (var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            if (input.Length != expectedLength) throw new IOException("Source asset length changed after preflight.");
            using var checksum = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using var output = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous | FileOptions.WriteThrough);
            var buffer = new byte[81920];
            var remaining = expectedLength;
            while (remaining > 0)
            {
                var read = await input.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellationToken);
                if (read == 0) throw new IOException("Source asset became shorter while copying.");
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                checksum.AppendData(buffer, 0, read);
                remaining -= read;
                progress?.Report(new(assetReference, expectedLength - remaining, expectedLength));
            }
            if (await input.ReadAsync(buffer.AsMemory(0, 1), cancellationToken) != 0 || input.Length != expectedLength)
                throw new IOException("Source asset grew while copying.");
            await output.FlushAsync(cancellationToken);
            output.Flush(flushToDisk: true);
            sourceDigest = checksum.GetHashAndReset();
        }
        await using var verification = new FileStream(destinationPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var copiedDigest = await SHA256.HashDataAsync(verification, cancellationToken);
        if (verification.Length != expectedLength || !CryptographicOperations.FixedTimeEquals(sourceDigest, copiedDigest))
            throw new IOException("Copied asset length/checksum does not match the source snapshot.");
        return Convert.ToHexString(copiedDigest).ToLowerInvariant();
    }
}
