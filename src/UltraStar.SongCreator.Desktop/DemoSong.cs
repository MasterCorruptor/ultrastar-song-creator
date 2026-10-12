using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Desktop;

public static class DemoSong
{
    public static Song Create() => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Metadata = new() { Artist = "UltraStar Song Creator", Title = "Egen testsang", Language = "nb" },
        Phrases = [new Phrase
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            StartSeconds = 0, EndSeconds = 8,
            Notes = [
                Make("33333333-3333-3333-3333-333333333331", 1, 1, 60, "Hei "),
                Make("33333333-3333-3333-3333-333333333332", 3, 0.75, 64, "på "),
                Make("33333333-3333-3333-3333-333333333333", 5, 1.5, 62, "deg!")]
        }]
    };
    private static Note Make(string id, double start, double duration, int pitch, string text) =>
        new() { Id = Guid.Parse(id), StartSeconds = start, DurationSeconds = duration, MidiPitch = pitch, Text = text };
}
