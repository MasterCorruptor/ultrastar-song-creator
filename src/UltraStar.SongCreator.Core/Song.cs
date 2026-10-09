using System.Collections.Immutable;

namespace UltraStar.SongCreator.Core;

public sealed record Song
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public SongMetadata Metadata { get; init; } = new();
    public ImmutableArray<MediaReference> Media { get; init; } = [];
    public double AudioOffsetSeconds { get; init; }
    public double VideoOffsetSeconds { get; init; }
    public double? BeatsPerMinute { get; init; }
    public ImmutableArray<Phrase> Phrases { get; init; } = [];
    public AnalysisData Analysis { get; init; } = new();
}

public sealed record SongMetadata
{
    public string Title { get; init; } = "";
    public string Artist { get; init; } = "";
    public string? Language { get; init; }
}

public enum MediaKind { Audio, Video }

public sealed record MediaReference
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public MediaKind Kind { get; init; }
    public string Location { get; init; } = "";
    public string? Source { get; init; }
}

public sealed record Phrase
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public double StartSeconds { get; init; }
    public double EndSeconds { get; init; }
    public ImmutableArray<Note> Notes { get; init; } = [];
    public string Text => string.Concat(Notes.IsDefault ? [] : Notes.Select(n => n.Text));
}

public enum NoteType { Normal, Golden, Freestyle, Rap, GoldenRap }

public sealed record Note
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public double StartSeconds { get; init; }
    public double DurationSeconds { get; init; }
    public double EndSeconds => StartSeconds + DurationSeconds;
    public int MidiPitch { get; init; } = 60;
    public string Text { get; init; } = "";
    public NoteType Type { get; init; }
    public double? Confidence { get; init; }
    public ImmutableArray<Guid> AnalysisReferences { get; init; } = [];
}
