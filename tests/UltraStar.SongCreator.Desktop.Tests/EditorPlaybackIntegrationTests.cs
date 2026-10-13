using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using UltraStar.SongCreator.Audio;
using UltraStar.SongCreator.Desktop.Playback;

namespace UltraStar.SongCreator.Desktop.Tests;

public sealed class EditorPlaybackIntegrationTests
{
    private sealed class Backend(PcmClip clip) : IPcmPlayback
    {
        public PcmPlayback Engine { get; } = new(clip);
        public PcmClip Clip => Engine.Clip;
        public int Reads, Disposals;
        public Action? Disposing;
        public PlaybackSnapshot Snapshot { get { Reads++; return Engine.Snapshot; } }
        public void Play() => Engine.Play();
        public void Pause() => Engine.Pause();
        public void Stop() => Engine.Stop();
        public void SeekFrame(long frame) => Engine.SeekFrame(frame);
        public void SeekSeconds(double seconds) => Engine.SeekSeconds(seconds);
        public void Dispose() { Disposing?.Invoke(); Disposals++; Engine.Dispose(); }
    }
    private static EditorWindow Open(EditorPlaybackSession owner, double offset = 0)
    {
        var song = DemoSong.Create() with { AudioOffsetSeconds = offset };
        var w = new EditorWindow(new EditingSession(song), owner);
        w.Show(); Dispatcher.UIThread.RunJobs(); return w;
    }
    private static T Control<T>(EditorWindow w, string name) where T : Avalonia.Controls.Control =>
        w.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
    private static void Click(EditorWindow w, Avalonia.Controls.Control c)
    {
        Dispatcher.UIThread.RunJobs();
        var point = c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), w)!.Value;
        w.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        w.MouseUp(point, MouseButton.Left, RawInputModifiers.None); Dispatcher.UIThread.RunJobs();
    }
    private static void Transport(EditorWindow w, string role) => Click(w, Control<Button>(w, "Playback" + role + "Button"));
    private static void Seek(EditorWindow w, string text)
    {
        var input = Control<TextBox>(w, "PlaybackSeekInput"); input.Focus(); input.SelectAll(); w.KeyTextInput(text); Transport(w, "Seek");
    }
    private static void Position(EditorWindow w, double seconds)
    {
        var presenter = w.Playback!.Presenter!;
        Assert.Equal(seconds, presenter.ViewState.SongPositionSeconds);
        Assert.Equal(seconds, w.Waveform!.PlayheadSongSeconds);
        Assert.True(w.NotePlayhead.IsVisible);
        Assert.Equal(presenter.ViewState.PlayheadX, Canvas.GetLeft(w.NotePlayhead));
        Assert.True(w.Waveform.Mapping.TrySongToX(seconds, out var x));
        Assert.Equal(x, Canvas.GetLeft(w.NotePlayhead));
    }

    [AvaloniaFact]
    public void TransportRenderPauseSeekStopAndEosDriveBothPlayheads()
    {
        Backend? b = null; using var owner = new EditorPlaybackSession(c => b = new Backend(c), 0);
        var w = Open(owner);
        try
        {
            Transport(w, "Play"); b!.Engine.Render(new float[48000 * 2]); owner.Presenter!.Refresh(); Position(w, 1);
            Transport(w, "Pause"); var paused = b.Engine.Snapshot;
            b.Engine.Render(new float[48000 * 2]); owner.Presenter.Refresh(); Assert.Equal(paused, b.Engine.Snapshot); Position(w, 1);
            Seek(w, "2,5"); Position(w, 2.5); Assert.Equal(PlaybackState.Paused, b.Engine.Snapshot.State);
            Transport(w, "Stop"); Position(w, 0);
            Seek(w, "7.5"); Transport(w, "Play"); b.Engine.Render(new float[48000 * 2]); owner.Presenter.Refresh();
            Position(w, 8); Assert.Equal(PlaybackState.Ended, b.Engine.Snapshot.State);
            Assert.False(Control<Button>(w, "PlaybackPlayButton").IsEnabled);
        }
        finally { w.Close(); }
    }

    [AvaloniaFact]
    public void NoteMoveUndoRedoPreserveCachedAudioAndSharedScrolledCoordinates()
    {
        Backend? b = null; using var owner = new EditorPlaybackSession(c => b = new Backend(c), 0);
        var w = Open(owner);
        try
        {
            var clip = owner.Clip; var envelope = owner.Envelope; var before = w.Session.Current;
            Transport(w, "Play"); b!.Engine.Render(new float[48000 * 2]); owner.Presenter!.Refresh();
            var note = w.Timeline.Children.OfType<Button>().First(); Click(w, note);
            w.StartInput.Focus(); w.StartInput.SelectAll(); w.KeyTextInput("2,5"); Click(w, w.MoveButton);
            var after = w.Session.Current; Assert.Equal(2.5, after.Phrases[0].Notes[0].StartSeconds); Position(w, 1);
            Click(w, w.UndoButton); Assert.Same(before, w.Session.Current); Position(w, 1);
            Click(w, w.RedoButton); Assert.Same(after, w.Session.Current); Position(w, 1);
            Assert.Same(clip, owner.Clip); Assert.Same(clip, b.Clip); Assert.Same(envelope, owner.Envelope); Assert.Same(envelope, w.Waveform!.Envelope);
            Assert.Same(owner.Mapping, w.Waveform.Mapping); Assert.Same(owner.Mapping, owner.Presenter.Mapping);
            var tracks = Assert.IsType<StackPanel>(w.TimelineScroll.Content);
            Assert.Same(tracks, w.Waveform.Parent); Assert.Same(tracks, w.Timeline.Parent);
            w.Width = 720; Dispatcher.UIThread.RunJobs();
            w.TimelineScroll.Offset = new Vector(140, 0); Dispatcher.UIThread.RunJobs();
            Assert.True(w.TimelineScroll.Offset.X > 0);
            var wavePoint = w.Waveform.TranslatePoint(new Point(Canvas.GetLeft(w.NotePlayhead), 0), w)!.Value;
            var notePoint = w.NotePlayhead.TranslatePoint(new Point(0, 0), w)!.Value;
            Assert.Equal(wavePoint.X, notePoint.X, 6);
        }
        finally { w.Close(); }
    }

    [AvaloniaFact]
    public void SignedOffsetHidesPrerollAndKeepsWaveformAndNotePlayheadsAligned()
    {
        Backend? b = null; using var owner = new EditorPlaybackSession(c => b = new Backend(c), .5);
        var w = Open(owner, .5);
        try
        {
            Assert.Equal(.5, w.Waveform!.AudioOffsetSeconds);
            Assert.Equal(-.5, w.Waveform.PlayheadSongSeconds); Assert.False(w.NotePlayhead.IsVisible);
            Transport(w, "Play"); b!.Engine.Render(new float[48000 * 2]); owner.Presenter!.Refresh(); Position(w, .5);
            Seek(w, "1.5"); Assert.Equal(96000, b.Engine.Snapshot.PositionFrames); Position(w, 1.5);
        }
        finally { w.Close(); }
    }

    [AvaloniaFact]
    public async Task CloseStopsPollingAndDisposesInOrderExactlyOnce()
    {
        Backend? b = null; using var owner = new EditorPlaybackSession(c => b = new Backend(c), 0);
        var w = Open(owner); var presenter = owner.Presenter!;
        b!.Disposing = () =>
        {
            Assert.False(Control<Button>(w, "PlaybackPlayButton").IsEnabled);
            Assert.False(presenter.ViewState.CanPlay);
            Assert.Equal("Avspillingsvisningen er avsluttet.", presenter.ViewState.Error);
            Assert.NotEqual(PlaybackState.Disposed, b.Engine.Snapshot.State);
        };
        Transport(w, "Play"); await Task.Delay(60); Assert.True(b.Reads > 0);
        w.Close(); w.ReleasePlayback(); owner.Dispose(); Assert.Equal(1, b.Disposals);
        b.Reads = 0; await Task.Delay(60); Assert.Equal(0, b.Reads);
        presenter.Refresh(); Assert.Equal(0, b.Reads); Assert.Null(w.Waveform!.PlayheadSongSeconds);
    }

    [AvaloniaFact]
    public void InitializationFailureIsVisibleWithoutFallbackAndEditingStillWorks()
    {
        var calls = 0;
        using var owner = new EditorPlaybackSession(_ => { calls++; throw new PlaybackException("fixture device unavailable"); }, 0);
        var w = Open(owner);
        try
        {
            Assert.Equal(1, calls); Assert.Null(owner.Presenter); Assert.Null(owner.Panel);
            Assert.Contains("fixture device unavailable", w.AudioMessageLabel.Text); Assert.False(w.NotePlayhead.IsVisible);
            Click(w, w.Timeline.Children.OfType<Button>().First()); w.StartInput.Focus(); w.StartInput.SelectAll(); w.KeyTextInput("2");
            Click(w, w.MoveButton); Assert.Equal(2, w.Session.Current.Phrases[0].Notes[0].StartSeconds);
            Click(w, w.UndoButton); Assert.Equal(1, w.Session.Current.Phrases[0].Notes[0].StartSeconds);
        }
        finally { w.Close(); }
    }

    [AvaloniaFact]
    public void BackendReturnedBeforeInitializationFailureIsReleased()
    {
        var b = new Backend(new PcmClip(8000, 1, [0f]));
        using var owner = new EditorPlaybackSession(_ => b, 0);
        Assert.NotNull(owner.InitializationError); Assert.Null(owner.Presenter);
        Assert.Equal(1, b.Disposals); owner.Dispose(); Assert.Equal(1, b.Disposals);
    }
}
