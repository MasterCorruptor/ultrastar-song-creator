using System.Collections.Immutable;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Projects;

public enum ProjectError { InvalidJson, InvalidEnvelope, UnsupportedVersion, InvalidSong, TooLarge }

public sealed class ProjectFormatException(
    ProjectError error, string message, ImmutableArray<ValidationIssue> issues = default, Exception? inner = null)
    : Exception(message, inner)
{
    public ProjectError Error { get; } = error;
    public ImmutableArray<ValidationIssue> Issues { get; } = issues.IsDefault ? [] : issues;
}

public sealed record LoadedProject(
    string FilePath, int SchemaVersion, Song Song, ImmutableArray<ValidationIssue> Issues);
