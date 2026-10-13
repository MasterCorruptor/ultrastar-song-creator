using System.Collections.Immutable;
using Core = UltraStar.SongCreator.Core;
using Acq = UltraStar.SongCreator.Acquisition.Lyrics;

namespace UltraStar.SongCreator.Acquisition.Lyrics;

public static class SelectedLyricsProjection
{
    public static Core.SelectedLyrics FromSelection(Acq.LyricsSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        Acq.LyricsValidation.Candidate(selection.Candidate);
        var candidate = selection.Candidate;
        Core.SelectedLyricsDocument? document = null;
        Core.SelectedLyricsKind kind;
        if (candidate.TextStatus == Acq.LyricsTextStatus.Instrumental && selection.Document is null)
            kind = Core.SelectedLyricsKind.Instrumental;
        else if (candidate.TextStatus == Acq.LyricsTextStatus.Available && selection.Document is not null)
        {
            Acq.LyricsValidation.Document(selection.Document);
            var member = candidate.Documents.SingleOrDefault(d => d.Id == selection.Document.Id);
            if (member is null || !Core.SelectedLyricsEquality.DocumentAreEqual(Map(member), Map(selection.Document)))
                throw Invalid("Selected document is not an exact candidate member.");
            document = Map(member); kind = Core.SelectedLyricsKind.Text;
        }
        else throw Invalid("Only explicit available text or instrumental/null is selectable.");
        var manual = candidate.ProviderId == "manual";
        var provenance = new Core.SelectedLyricsProvenance(
            manual ? Core.SelectedLyricsOrigin.Manual : Core.SelectedLyricsOrigin.Provider,
            candidate.ProviderId, candidate.Id, candidate.SourceUrl,
            manual ? candidate.RetrievedAtUtc : null, manual ? null : candidate.RetrievedAtUtc,
            candidate.Artist, candidate.Title, candidate.Album, candidate.Version, candidate.DurationSeconds);
        var result = new Core.SelectedLyrics(kind, document, provenance);
        Core.SelectedLyricsValidation.Validate(result);
        return result;
    }
    private static Acq.LyricsValidationException Invalid(string message) => new(Acq.LyricsValidationError.InvalidSelection, message);
    private static Core.SelectedLyricsDocument Map(Acq.LyricsDocument document) => new(document.Id, document.Text,
        document.SyncLevel switch
        {
            Acq.LyricsSyncLevel.Plain => Core.SelectedLyricsSyncLevel.Plain,
            Acq.LyricsSyncLevel.Line => Core.SelectedLyricsSyncLevel.Line,
            Acq.LyricsSyncLevel.Word => Core.SelectedLyricsSyncLevel.Word,
            _ => throw Invalid("Unknown document sync level.")
        },
        document.Lines.Select(l => new Core.SelectedLyricsLine(l.Text, l.StartSeconds, l.EndSeconds,
            l.Words.Select(w => new Core.SelectedLyricsWord(w.Text, w.StartSeconds, w.EndSeconds)).ToImmutableArray())).ToImmutableArray(),
        document.TimeReference switch
        {
            Acq.LyricsTimeReference.LyricsSourceSeconds => Core.SelectedLyricsTimeReference.LyricsSourceSeconds,
            _ => throw Invalid("Unknown document time reference.")
        });
}
