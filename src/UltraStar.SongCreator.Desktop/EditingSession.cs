using System.Globalization;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Desktop;

/// <summary>Single UI-thread owner of domain history; selection is an identity, never a mutable note copy.</summary>
public sealed class EditingSession(Song initial)
{
    private readonly SongHistory history = new(initial);
    public Song Current => history.Current;
    public bool CanUndo => history.CanUndo;
    public bool CanRedo => history.CanRedo;
    public Guid? SelectedNoteId { get; private set; }
    public Note? SelectedNote => Current.Phrases.SelectMany(p => p.Notes).FirstOrDefault(n => n.Id == SelectedNoteId);
    public string Message { get; private set; } = "Velg en note i tidslinjen.";
    public event EventHandler? Changed;

    public void Select(Guid id)
    {
        if (!Current.Phrases.SelectMany(p => p.Notes).Any(n => n.Id == id)) return;
        SelectedNoteId = id;
        Message = "Valgt note. Skriv ny starttid i sekunder.";
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Move(string? input)
    {
        if (SelectedNoteId is not { } id)
            Message = "Velg en note først.";
        else if (!double.TryParse(input?.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var start)
            || !double.IsFinite(start))
            Message = "Starttid må være et endelig tall i sekunder (for eksempel 2,5).";
        else
        {
            try
            {
                Message = history.Execute(new MoveNote(id, start)) ? "Noten er flyttet." : "Noten står allerede på denne tiden.";
            }
            catch (EditRejectedException error)
            {
                Message = error.Reason == EditRejection.OutsidePhrase
                    ? "Hele noten må ligge innenfor frasens tidsgrenser."
                    : "Flyttingen ble avvist. Starttid må være gyldig og ikke negativ.";
            }
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Applies an explicit validated payload to the current song using the existing history.</summary>
    public bool ApplySelectedLyrics(SelectedLyrics value)
    {
        var changed = false;
        if (value is null)
            Message = "Velg en gyldig sangtekst før bruk.";
        else
        {
            try
            {
                changed = history.Execute(new SetSelectedLyrics(value));
                Message = changed ? "Valgt sangtekst er brukt." : "Denne sangteksten er allerede valgt.";
            }
            catch (EditRejectedException)
            {
                Message = "Sangteksten kunne ikke brukes. Kontroller tekst og kildeinformasjon.";
            }
        }
        Changed?.Invoke(this, EventArgs.Empty);
        return changed;
    }

    public void Undo()
    {
        Message = history.Undo() ? "Endringen er angret." : "Ingen endring å angre.";
        Changed?.Invoke(this, EventArgs.Empty);
    }
    public void Redo()
    {
        Message = history.Redo() ? "Endringen er gjort om." : "Ingen endring å gjøre om.";
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
