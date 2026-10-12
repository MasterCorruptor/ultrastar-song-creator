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
using UltraStar.SongCreator.Desktop.Timeline;

namespace UltraStar.SongCreator.Desktop.Tests;

public sealed class PlaybackPanelTests
{
    private static T Control<T>(PlaybackPanel p, string name) where T : Avalonia.Controls.Control =>
        p.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
    private static Button Button(PlaybackPanel p, string role) => Control<Button>(p, "Playback" + role + "Button");
    private static Window Open(PlaybackPanel panel)
    {
        var w = new Window { Width = 800, Height = 250, Content = panel }; w.Show(); Dispatcher.UIThread.RunJobs(); return w;
    }
    private static void Click(Window w, Avalonia.Controls.Control c)
    {
        Dispatcher.UIThread.RunJobs();
        var point = c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), w)!.Value;
        w.MouseDown(point, MouseButton.Left, RawInputModifiers.None); w.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }
    private static void Seek(Window w, PlaybackPanel p, string text)
    {
        var input = Control<TextBox>(p, "PlaybackSeekInput"); input.Focus(); input.SelectAll(); w.KeyTextInput(text); Click(w, Button(p, "Seek"));
    }
    private static void Disabled(PlaybackViewState s)
    {
        Assert.Null(s.SongPositionSeconds); Assert.Null(s.PlayheadX);
        Assert.False(s.CanPlay); Assert.False(s.CanPause); Assert.False(s.CanStop); Assert.False(s.CanSeek); Assert.NotNull(s.Error);
    }
    private sealed class Backend : IPcmPlayback
    {
        public PcmPlayback Engine { get; }
        public PcmClip Clip => Engine.Clip;
        public int Reads, Commands, Seeks, DisposeCalls;
        public Exception? ReadError, PlayError;
        public PlaybackSnapshot? Override;
        public Backend(bool empty = false) { Engine = new(new PcmClip(8000, 1, empty ? [] : Enumerable.Repeat(.1f, 64000).ToArray())); }
        public PlaybackSnapshot Snapshot { get { Reads++; if (ReadError is not null) throw ReadError; return Override ?? Engine.Snapshot; } }
        public void Play() { Commands++; if (PlayError is not null) throw PlayError; Engine.Play(); }
        public void Pause() { Commands++; Engine.Pause(); }
        public void Stop() { Commands++; Engine.Stop(); }
        public void SeekFrame(long f) { Commands++; Seeks++; Engine.SeekFrame(f); }
        public void SeekSeconds(double s) { Commands++; Seeks++; Engine.SeekSeconds(s); }
        public void Dispose() { DisposeCalls++; Engine.Dispose(); }
    }

    [AvaloniaFact]
    public void ClicksDriveRealPcmRenderAndPauseSeekStopEosButtonStates()
    {
        using var b = new Backend(); using var presenter = new PlaybackPresenter(b, TimelineMapping.Demo);
        using var panel = new PlaybackPanel(presenter); var w = Open(panel);
        try
        {
            Assert.True(Button(panel, "Play").IsEnabled); Assert.False(Button(panel, "Pause").IsEnabled); Assert.False(Button(panel, "Stop").IsEnabled);
            Click(w, Button(panel, "Play")); Assert.Equal(PlaybackState.Playing, b.Engine.Snapshot.State);
            b.Engine.Render(new float[8000]); presenter.Refresh();
            Assert.Equal(1, presenter.ViewState.SongPositionSeconds); Assert.Equal(185, presenter.ViewState.PlayheadX);
            Assert.Contains("1 s", Control<TextBlock>(panel, "PlaybackPositionLabel").Text);
            Click(w, Button(panel, "Pause")); var paused = b.Engine.Snapshot; b.Engine.Render(new float[8000]); presenter.Refresh(); Assert.Equal(paused, b.Engine.Snapshot);
            Assert.True(Button(panel, "Play").IsEnabled); Assert.False(Button(panel, "Pause").IsEnabled);
            Seek(w, panel, "2,5"); Assert.Equal(20000, b.Engine.Snapshot.PositionFrames); Assert.Equal(PlaybackState.Paused, b.Engine.Snapshot.State);
            Assert.Equal(350, presenter.ViewState.PlayheadX); Assert.Contains("2.5 s", Control<TextBlock>(panel, "PlaybackPositionLabel").Text);
            Click(w, Button(panel, "Play")); Seek(w, panel, "3.5"); Assert.Equal(PlaybackState.Playing, b.Engine.Snapshot.State); Assert.Equal(28000, b.Engine.Snapshot.PositionFrames);
            Click(w, Button(panel, "Stop")); Assert.Equal(0, presenter.ViewState.SongPositionSeconds); Assert.Equal(PlaybackState.Stopped, b.Engine.Snapshot.State);
            Seek(w, panel, "8"); Assert.Equal(PlaybackState.Ended, presenter.ViewState.Snapshot.State); Assert.False(Button(panel, "Play").IsEnabled); Assert.True(Button(panel, "Stop").IsEnabled);
            Seek(w, panel, "7.5"); Assert.Equal(PlaybackState.Paused, b.Engine.Snapshot.State); Click(w, Button(panel, "Play")); b.Engine.Render(new float[8000]); presenter.Refresh();
            Assert.Equal(PlaybackState.Ended, presenter.ViewState.Snapshot.State); Assert.Equal(8, presenter.ViewState.SongPositionSeconds); Assert.False(Button(panel, "Play").IsEnabled);
        }
        finally { w.Close(); }
    }
    [AvaloniaTheory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-1")]
    [InlineData("8.1")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1e999")]
    public void InvalidTypedSeekMakesNoCommandAndPreservesRealClock(string input)
    {
        using var b = new Backend(); using var p = new PlaybackPresenter(b, TimelineMapping.Demo); using var panel = new PlaybackPanel(p); var w = Open(panel);
        try
        {
            var before = b.Engine.Snapshot; var commands = b.Commands; Seek(w, panel, input);
            Assert.Equal(before, b.Engine.Snapshot); Assert.Equal(commands, b.Commands); Assert.NotEmpty(Control<TextBlock>(panel, "PlaybackErrorLabel").Text!);
            Seek(w, panel, "1.5"); Assert.Equal(12000, b.Engine.Snapshot.PositionFrames); Assert.Null(p.ViewState.Error);
        }
        finally { w.Close(); }
    }
    [AvaloniaFact]
    public void OffsetChangesOnlyProjectionAndSeekUsesSourceFramesWithBounds()
    {
        using var b = new Backend(); using var p = new PlaybackPresenter(b, TimelineMapping.Demo);
        b.Engine.SeekFrame(8000); var before = b.Engine.Snapshot;
        p.SetAudioOffset(.5); Assert.Equal(before, b.Engine.Snapshot); Assert.Equal(0, b.Seeks); Assert.Equal(.5, p.ViewState.SongPositionSeconds); Assert.Equal(130, p.ViewState.PlayheadX);
        Assert.True(p.SeekSongSeconds(1)); Assert.Equal(12000, b.Engine.Snapshot.PositionFrames); Assert.Equal(1, p.ViewState.SongPositionSeconds);
        p.SetAudioOffset(1); var commands = b.Commands; before = b.Engine.Snapshot;
        Assert.False(p.SeekSongSeconds(8)); Assert.Equal(commands, b.Commands); Assert.Equal(before, b.Engine.Snapshot);
        p.SetAudioOffset(-1); Assert.False(p.SeekSongSeconds(0)); Assert.True(p.SeekSongSeconds(1)); Assert.Equal(0, b.Engine.Snapshot.PositionFrames);
        p.SetAudioOffset(1); Assert.Equal(-1, p.ViewState.SongPositionSeconds); Assert.Null(p.ViewState.PlayheadX);
        Assert.Throws<ArgumentOutOfRangeException>(() => p.SetAudioOffset(double.NaN)); Assert.Equal(1, p.AudioOffsetSeconds);
    }
    [AvaloniaFact]
    public void RefreshReadsOneSnapshotAndRejectsMalformedFaultedDisposedOrUnreachableBackend()
    {
        using var b = new Backend(); using var p = new PlaybackPresenter(b, TimelineMapping.Demo);
        b.Reads = 0; p.Refresh(); Assert.Equal(1, b.Reads);
        var s = b.Engine.Snapshot;
        foreach (var invalid in new[] { s with { SampleRate = 48000 }, s with { FrameCount = 1 }, s with { PositionFrames = -1 }, s with { State = (PlaybackState)99 }, s with { State = PlaybackState.Faulted, Error = "fixture fault" }, s with { State = PlaybackState.Disposed } })
        {
            b.Override = invalid; p.Refresh(); Disabled(p.ViewState); var commands = b.Commands;
            p.Play(); p.Pause(); p.Stop(); Assert.False(p.SeekSongSeconds(1)); Assert.Equal(commands, b.Commands);
        }
        b.Override = null; b.ReadError = new InvalidOperationException("fixture read failure"); p.Refresh(); Disabled(p.ViewState); Assert.Contains("fixture read failure", p.ViewState.Error);
        b.ReadError = null; p.Refresh(); Assert.True(p.ViewState.CanPlay);
        b.Dispose(); p.Refresh(); Disabled(p.ViewState);
    }
    [AvaloniaFact]
    public void CommandFailuresAreVisibleAndEmptyClipDisablesControls()
    {
        using var b = new Backend(); using var p = new PlaybackPresenter(b, TimelineMapping.Demo); using var panel = new PlaybackPanel(p); var w = Open(panel);
        try
        {
            b.PlayError = new PlaybackException("fixture device error"); var before = b.Engine.Snapshot;
            Click(w, Button(panel, "Play")); Assert.Equal(before, b.Engine.Snapshot); Assert.Contains("fixture device error", Control<TextBlock>(panel, "PlaybackErrorLabel").Text);
            b.PlayError = null; Click(w, Button(panel, "Play")); Assert.Null(p.ViewState.Error);
        }
        finally { w.Close(); }
        using var empty = new Backend(true); using var e = new PlaybackPresenter(empty, TimelineMapping.Demo);
        Assert.False(e.ViewState.CanPlay); Assert.False(e.ViewState.CanPause); Assert.False(e.ViewState.CanStop); Assert.False(e.ViewState.CanSeek); Assert.Equal(0, e.ViewState.SongPositionSeconds);
    }
    [AvaloniaFact]
    public async Task AttachedPollOnlyReadsAndDetachAndDisposeStopPollingWithoutDisposingBorrowers()
    {
        using var b = new Backend(); using var p = new PlaybackPresenter(b, TimelineMapping.Demo); using var panel = new PlaybackPanel(p); var w = Open(panel);
        try
        {
            p.Play(); var before = b.Engine.Snapshot; b.Reads = 0; await Task.Delay(80);
            Assert.True(b.Reads > 0); Assert.Equal(before, b.Engine.Snapshot);
            w.Content = null; Dispatcher.UIThread.RunJobs(); b.Reads = 0; await Task.Delay(60); Assert.Equal(0, b.Reads);
            w.Content = panel; Dispatcher.UIThread.RunJobs(); b.Reads = 0; await Task.Delay(60); Assert.True(b.Reads > 0);
            panel.Dispose(); b.Reads = 0; await Task.Delay(60); Assert.Equal(0, b.Reads); Assert.Equal(0, b.DisposeCalls);
            Assert.False(Button(panel, "Play").IsEnabled);
            p.Dispose(); p.Dispose(); p.Refresh(); p.Play(); Assert.False(p.SeekSongSeconds(1)); Assert.Equal(0, b.DisposeCalls); Disabled(p.ViewState);
            b.Engine.Stop(); b.Engine.Play(); Assert.Equal(PlaybackState.Playing, b.Engine.Snapshot.State);
        }
        finally { w.Close(); }
    }
    [AvaloniaFact]
    public async Task PresenterPublicationIsOnUiThreadAndDisposePublishesOnlyFinalUnavailableState()
    {
        using var b = new Backend(); using var p = new PlaybackPresenter(b, TimelineMapping.Demo); var events = 0;
        p.Changed += (_, _) => { Assert.True(Dispatcher.UIThread.CheckAccess()); events++; };
        p.Refresh(); Assert.Equal(1, events);
        var error = await Task.Run(() => Record.Exception(p.Refresh)); Assert.IsType<InvalidOperationException>(error);
        p.Dispose(); Assert.Equal(2, events); p.Refresh(); p.SetAudioOffset(0); p.Stop(); Assert.Equal(2, events); Assert.Equal(0, b.DisposeCalls);
    }

    [AvaloniaFact]
    public void NorwegianLabelsRemainCorrectThroughInvalidSeekAndEndOfClip()
    {
        using var b = new Backend(); using var p = new PlaybackPresenter(b, TimelineMapping.Demo);
        using var panel = new PlaybackPanel(p); var w = Open(panel);
        try
        {
            Assert.Equal("Søk til tid", Button(panel, "Seek").Content);
            Seek(w, panel, "-1");
            Assert.Equal("Starttid må være et endelig tall innenfor sangområdet og lydklippet.",
                Control<TextBlock>(panel, "PlaybackErrorLabel").Text);
            Seek(w, panel, "8");
            Assert.Equal("Slutt på klippet", Control<TextBlock>(panel, "PlaybackStateLabel").Text);
        }
        finally { w.Close(); }
    }
}
