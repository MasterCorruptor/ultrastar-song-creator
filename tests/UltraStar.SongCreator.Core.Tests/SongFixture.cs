using System.Collections.Immutable;

namespace UltraStar.SongCreator.Core.Tests;

internal static class SongFixture
{
    public static Song Create()
    {
        var media = new MediaReference { Location = "synthetic.wav", Source = "test" };
        var artifact = new AnalysisArtifact
        {
            Kind = AnalysisKind.PitchCurve, Producer = "synthetic", ModelRevision = "fixture-v1",
            SourceMediaId = media.Id, Points = [new(1, 60.25), new(1.1, 60.5)]
        };
        return new Song
        {
            Metadata = new() { Title = "Synthetic song", Artist = "Test", Language = "nb" },
            Media = [media], AudioOffsetSeconds = -0.2, VideoOffsetSeconds = 0.3, BeatsPerMinute = 120,
            Analysis = new() { Artifacts = [artifact] },
            Phrases =
            [
                new() { StartSeconds = 0, EndSeconds = 4, Notes =
                [
                    new() { StartSeconds = 1, DurationSeconds = 0.5, Text = "Hei ", Confidence = 0.8, AnalysisReferences = [artifact.Id] },
                    new() { StartSeconds = 2, DurationSeconds = 0.75, Text = "du", MidiPitch = 62 }
                ] },
                new() { StartSeconds = 4, EndSeconds = 8, Notes =
                [
                    new() { StartSeconds = 5, DurationSeconds = 1, Text = "Ja", Type = NoteType.Golden }
                ] }
            ]
        };
    }

    public static Song WithFirstNote(Song song, Note note) =>
        song with { Phrases = song.Phrases.SetItem(0, song.Phrases[0] with { Notes = song.Phrases[0].Notes.SetItem(0, note) }) };
}
