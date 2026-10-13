namespace UltraStar.SongCreator.Core;

public static class SelectedLyricsEquality
{
    /// <summary>Validates both inputs, including reference-equal inputs. Invalid data throws; null means no selection.</summary>
    public static bool AreEqual(SelectedLyrics? left, SelectedLyrics? right)
    {
        SelectedLyricsValidation.Validate(left); SelectedLyricsValidation.Validate(right);
        if (left is null || right is null) return left is null && right is null;
        // Provenance is a sealed scalar-only record: ordinal strings and exact nullable values, no arrays.
        if (left.Kind != right.Kind || left.Provenance != right.Provenance) return false;
        if (left.Document is null || right.Document is null) return left.Document is null && right.Document is null;
        return DocumentAreEqual(left.Document, right.Document);
    }
    public static bool DocumentAreEqual(SelectedLyricsDocument left, SelectedLyricsDocument right)
    {
        SelectedLyricsValidation.ValidateDocument(left); SelectedLyricsValidation.ValidateDocument(right);
        if (left.Id != right.Id || left.Text != right.Text || left.SyncLevel != right.SyncLevel ||
            left.TimeReference != right.TimeReference || left.Lines.Length != right.Lines.Length) return false;
        for (var i = 0; i < left.Lines.Length; i++)
        {
            var a = left.Lines[i]; var b = right.Lines[i];
            if (a.Text != b.Text || a.StartSeconds != b.StartSeconds || a.EndSeconds != b.EndSeconds || a.Words.Length != b.Words.Length) return false;
            for (var j = 0; j < a.Words.Length; j++)
            {
                var x = a.Words[j]; var y = b.Words[j];
                if (x.Text != y.Text || x.StartSeconds != y.StartSeconds || x.EndSeconds != y.EndSeconds) return false;
            }
        }
        return true;
    }
}
