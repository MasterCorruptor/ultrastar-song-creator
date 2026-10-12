using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Desktop;

public sealed class EditorWindow : Window
{
    public EditingSession Session { get; }
    public Canvas Timeline { get; } = new() { Width = 980, Height = 360, Background = Brushes.WhiteSmoke };
    public TextBox StartInput { get; } = new() { Width = 150, PlaceholderText = "Sekunder", Name = "StartInput" };
    public Button MoveButton { get; } = new() { Content = "Flytt note", Name = "MoveButton" };
    public Button UndoButton { get; } = new() { Content = "Angre", Name = "UndoButton" };
    public Button RedoButton { get; } = new() { Content = "Gjør om", Name = "RedoButton" };
    public TextBlock SelectedLabel { get; } = new();
    public TextBlock MessageLabel { get; } = new() { TextWrapping = TextWrapping.Wrap };
    public TextBlock ValidationLabel { get; } = new() { TextWrapping = TextWrapping.Wrap };
    private Song? rendered;
    private Guid? renderedSelection;
    public const double SecondsScale = 110;
    public const double TimeOrigin = 75;

    public EditorWindow(EditingSession session)
    {
        Session = session;
        Title = "UltraStar Song Creator — GUI-01 testflate";
        Width = 1100; Height = 650; MinWidth = 720; MinHeight = 550;
        var body = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        body.Children.Add(new TextBlock { Text = Session.Current.Metadata.Title, FontSize = 24 });
        body.Children.Add(new TextBlock { Text = "Velg note → skriv starttid → flytt. Tid: sekunder. Pitch: MIDI. Egen testsang uten lyd." });
        body.Children.Add(new ScrollViewer { Content = Timeline, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto });
        body.Children.Add(SelectedLabel);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        actions.Children.Add(StartInput); actions.Children.Add(MoveButton); actions.Children.Add(UndoButton); actions.Children.Add(RedoButton);
        body.Children.Add(actions); body.Children.Add(MessageLabel); body.Children.Add(ValidationLabel);
        Content = new ScrollViewer { Content = body };
        MoveButton.Click += (_, _) => Session.Move(StartInput.Text);
        UndoButton.Click += (_, _) => Session.Undo();
        RedoButton.Click += (_, _) => Session.Redo();
        Session.Changed += Refresh;
        Closed += (_, _) => Session.Changed -= Refresh;
        Refresh(this, EventArgs.Empty);
    }

    private void Refresh(object? sender, EventArgs args)
    {
        var songChanged = !ReferenceEquals(rendered, Session.Current);
        if (songChanged || renderedSelection != Session.SelectedNoteId)
        {
            DrawTimeline();
            if (Session.SelectedNote is { } note)
                StartInput.Text = note.StartSeconds.ToString("G17", CultureInfo.InvariantCulture);
            rendered = Session.Current; renderedSelection = Session.SelectedNoteId;
        }
        SelectedLabel.Text = Session.SelectedNote is { } selected
            ? $"Valgt: {selected.Text.Trim()} · MIDI {selected.MidiPitch} · start {selected.StartSeconds:G} s · varighet {selected.DurationSeconds:G} s"
            : "Ingen note valgt.";
        MoveButton.IsEnabled = Session.SelectedNoteId.HasValue;
        UndoButton.IsEnabled = Session.CanUndo; RedoButton.IsEnabled = Session.CanRedo;
        MessageLabel.Text = Session.Message;
        var issues = SongValidator.Validate(Session.Current);
        ValidationLabel.Text = issues.IsEmpty ? "Validering: ingen funn." :
            "Validering: " + string.Join("; ", issues.Select(i => i.Code switch
            {
                ValidationCode.MissingMedia => "Testsangen har ingen lydfil.",
                ValidationCode.NoteOverlap => "Noter overlapper (tillatt i utkast).",
                _ => $"{i.Severity}: {i.Code}"
            }));
    }

    private void DrawTimeline()
    {
        Timeline.Children.Clear();
        for (var s = 0; s <= 8; s++)
        {
            var tick = new TextBlock { Text = $"{s} s", Foreground = Brushes.Black };
            Canvas.SetLeft(tick, TimeOrigin + s * SecondsScale); Canvas.SetTop(tick, 8); Timeline.Children.Add(tick);
        }
        for (var pitch = 58; pitch <= 66; pitch++)
        {
            var label = new TextBlock { Text = $"MIDI {pitch}", Foreground = Brushes.Black };
            Canvas.SetLeft(label, 5); Canvas.SetTop(label, PitchTop(pitch)); Timeline.Children.Add(label);
        }
        foreach (var note in Session.Current.Phrases.SelectMany(p => p.Notes))
        {
            var button = new Button
            {
                Name = "Note_" + note.Id.ToString("N"),
                Tag = note.Id,
                Content = note.Text.Trim(),
                Width = note.DurationSeconds * SecondsScale,
                Height = 30,
                Background = note.Id == Session.SelectedNoteId ? Brushes.Gold : Brushes.LightSkyBlue,
                Foreground = Brushes.Black,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            button.Click += (_, _) => Session.Select(note.Id);
            Canvas.SetLeft(button, TimeOrigin + note.StartSeconds * SecondsScale);
            Canvas.SetTop(button, PitchTop(note.MidiPitch)); Timeline.Children.Add(button);
        }
    }
    public static double PitchTop(int midi) => 40 + (66 - midi) * 34;
}
