using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Avalonia.Threading;
using UltraStar.SongCreator.Desktop.Lyrics;
using UltraStar.SongCreator.Desktop.Controls;
using UltraStar.SongCreator.Desktop.Playback;
using UltraStar.SongCreator.Desktop.Timeline;
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
    public EditorPlaybackSession? Playback { get; }
    public WaveformControl? Waveform { get; }
    public Rectangle NotePlayhead { get; } = new() { Width = 2, Height = 360, Fill = Brushes.OrangeRed, IsHitTestVisible = false, IsVisible = false };
    public ScrollViewer TimelineScroll { get; } = new() { HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
    public TextBlock AudioMessageLabel { get; } = new() { TextWrapping = TextWrapping.Wrap };
    internal LyricsPanel? Lyrics { get; }
    internal TextBlock CloseErrorLabel { get; } = new() { TextWrapping = TextWrapping.Wrap };
    internal Task<bool>? CloseTask { get; private set; }
    private readonly Func<Task<bool>>? cleanup;
    private bool finalCloseAllowed;
    private readonly TimelineMapping mapping;
    private bool playbackReleased;
    private Song? rendered;
    private Guid? renderedSelection;
    public const double SecondsScale = 110;
    public const double TimeOrigin = 75;

    public EditorWindow(EditingSession session, EditorPlaybackSession? playback = null)
        : this(session, playback, null, null) { }

    internal EditorWindow(EditingSession session, EditorPlaybackSession? playback, LyricsPanel? lyrics, Func<Task<bool>>? cleanup = null)
    {
        Dispatcher.UIThread.VerifyAccess();
        Lyrics = lyrics;
        this.cleanup = cleanup;
        Session = session;
        Playback = playback;
        mapping = playback?.Mapping ?? TimelineMapping.Demo;
        Title = "UltraStar Song Creator — GUI-02 testflate";
        Width = 1100; Height = 1000; MinWidth = 720; MinHeight = 550;
        var body = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        body.Children.Add(new TextBlock { Text = Session.Current.Metadata.Title, FontSize = 24 });
        body.Children.Add(new TextBlock { Text = "Velg note → skriv starttid → flytt. Tid: sekunder. Pitch: MIDI. Egen testsang og svak syntetisk demotone." });
        var tracks = new StackPanel { Width = 980, HorizontalAlignment = HorizontalAlignment.Left, Spacing = 0 };
        if (playback is not null)
        {
            Waveform = new WaveformControl(playback.Envelope, mapping, Session.Current.AudioOffsetSeconds);
            tracks.Children.Add(Waveform);
            if (playback.Panel is not null) body.Children.Add(playback.Panel);
            AudioMessageLabel.Text = playback.InitializationError ?? "Svak syntetisk demotone, 8 sekunder. Ingen sangfil er lastet.";
            body.Children.Add(AudioMessageLabel);
        }
        tracks.Children.Add(Timeline); TimelineScroll.Content = tracks; body.Children.Add(TimelineScroll);
        body.Children.Add(SelectedLabel);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        actions.Children.Add(StartInput); actions.Children.Add(MoveButton); actions.Children.Add(UndoButton); actions.Children.Add(RedoButton);
        body.Children.Add(actions); body.Children.Add(MessageLabel); body.Children.Add(ValidationLabel);
        if (Lyrics is not null) { body.Children.Add(Lyrics); body.Children.Add(CloseErrorLabel); }
        Content = new ScrollViewer { Content = body };
        MoveButton.Click += (_, _) => Session.Move(StartInput.Text);
        UndoButton.Click += (_, _) => Session.Undo();
        RedoButton.Click += (_, _) => Session.Redo();
        Session.Changed += Refresh;
        if (Playback?.Presenter is { } presenter) presenter.Changed += PlaybackChanged;
        Closed += (_, _) => ReleasePlayback();
        Refresh(this, EventArgs.Empty);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (Lyrics is null || finalCloseAllowed) return;
        e.Cancel = true;
        if (CloseTask is not null) return;
        var audioReleased = true;
        try { Lyrics.Detach(); ReleasePlayback(); }
        catch (Exception) { audioReleased = false; }
        MoveButton.IsEnabled = UndoButton.IsEnabled = RedoButton.IsEnabled = false;
        Task<bool> drain;
        try { drain = cleanup?.Invoke() ?? Lyrics.CloseAsync(); }
        catch (Exception) { drain = Task.FromResult(false); }
        CloseTask = ObserveCloseAsync(drain, audioReleased);
    }

    private async Task<bool> ObserveCloseAsync(Task<bool> drain, bool audioReleased)
    {
        bool success;
        try { success = await drain.ConfigureAwait(false) && audioReleased; }
        catch (Exception) { success = false; }
        // Always post: even an already completed cleanup must not recursively close in Closing.
        var finished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (success) { FinishClose(); success = !IsVisible; }
                if (!success) CloseErrorLabel.Text = "Opprydding feilet. Vinduet holdes åpent.";
                finished.TrySetResult(success);
            }
            catch (Exception)
            {
                finalCloseAllowed = false;
                CloseErrorLabel.Text = "Vinduet kunne ikke avsluttes.";
                finished.TrySetResult(false);
            }
        });
        return await finished.Task.ConfigureAwait(false);
    }

    internal void FinishClose()
    {
        Dispatcher.UIThread.VerifyAccess();
        finalCloseAllowed = true;
        Close();
    }

    private void Refresh(object? sender, EventArgs args)
    {
        if (Waveform is not null) Waveform.AudioOffsetSeconds = Session.Current.AudioOffsetSeconds;
        if (Playback?.Presenter is { } presenter && presenter.AudioOffsetSeconds != Session.Current.AudioOffsetSeconds)
            presenter.SetAudioOffset(Session.Current.AudioOffsetSeconds);
        var songChanged = !ReferenceEquals(rendered, Session.Current);
        if (songChanged || renderedSelection != Session.SelectedNoteId)
        {
            DrawTimeline();
            if (Session.SelectedNote is { } note)
                StartInput.Text = note.StartSeconds.ToString("G17", CultureInfo.InvariantCulture);
            rendered = Session.Current; renderedSelection = Session.SelectedNoteId;
        }
        UpdatePlayhead();
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
            mapping.TrySongToX(s, out var tickX); Canvas.SetLeft(tick, tickX); Canvas.SetTop(tick, 8); Timeline.Children.Add(tick);
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
                Width = note.DurationSeconds * mapping.PixelsPerSecond,
                Height = 30,
                Background = note.Id == Session.SelectedNoteId ? Brushes.Gold : Brushes.LightSkyBlue,
                Foreground = Brushes.Black,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            button.Click += (_, _) => Session.Select(note.Id);
            mapping.TrySongToX(note.StartSeconds, out var noteX); Canvas.SetLeft(button, noteX);
            Canvas.SetTop(button, PitchTop(note.MidiPitch)); Timeline.Children.Add(button);
        }
    }
    // The line is restored after every immutable-song redraw.
    private void PlaybackChanged(object? sender, EventArgs args) => UpdatePlayhead();
    private void UpdatePlayhead()
    {
        var view = Playback?.Presenter?.ViewState;
        if (Waveform is not null) Waveform.PlayheadSongSeconds = view?.SongPositionSeconds;
        if (!Timeline.Children.Contains(NotePlayhead)) Timeline.Children.Add(NotePlayhead);
        NotePlayhead.IsVisible = view?.PlayheadX.HasValue == true;
        if (view?.PlayheadX is { } x) Canvas.SetLeft(NotePlayhead, x);
    }
    public void ReleasePlayback()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (playbackReleased) return;
        playbackReleased = true;
        Session.Changed -= Refresh;
        if (Playback?.Presenter is { } presenter) presenter.Changed -= PlaybackChanged;
        Playback?.Dispose();
        NotePlayhead.IsVisible = false;
        if (Waveform is not null) Waveform.PlayheadSongSeconds = null;
    }
    public static double PitchTop(int midi) => 40 + (66 - midi) * 34;
}
