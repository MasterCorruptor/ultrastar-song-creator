using System.Collections.Immutable;

namespace UltraStar.SongCreator.Core;

/// <summary>Original import metadata, distinct from the user's current editable song state.</summary>
public sealed record SourceDocument
{
    public string FormatId { get; init; } = "";
    public string? FileReference { get; init; }
    public ImmutableArray<SourceHeader> Headers { get; init; } = [];
}

public sealed record SourceHeader(string Name, string Value);
