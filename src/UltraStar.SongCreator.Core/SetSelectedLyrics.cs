namespace UltraStar.SongCreator.Core;

/// <summary>Replaces or explicitly clears the selected payload without changing other song data.</summary>
public sealed record SetSelectedLyrics(SelectedLyrics? Value) : ISongCommand
{
    public Song Apply(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);
        try
        {
            // Equality validates both payloads, including before a same-reference/no-op result.
            if (SelectedLyricsEquality.AreEqual(song.SelectedLyrics, Value)) return song;
        }
        catch (SelectedLyricsValidationException e)
        {
            throw new EditRejectedException(EditRejection.InvalidStructure, e.Message);
        }
        return song with { SelectedLyrics = Value };
    }
}
