using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using UltraStar.SongCreator.Audio;

namespace UltraStar.SongCreator.Desktop.Playback;

public sealed class PlaybackPanel : UserControl, IDisposable
{
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Button play = new() { Name = "PlaybackPlayButton", Content = "Spill" };
    private readonly Button pause = new() { Name = "PlaybackPauseButton", Content = "Pause" };
    private readonly Button stop = new() { Name = "PlaybackStopButton", Content = "Stopp" };
    private readonly Button seek = new() { Name = "PlaybackSeekButton", Content = "Søk til tid" };
    private readonly TextBox input = new() { Name = "PlaybackSeekInput", Width = 180, MaxLength = 128, PlaceholderText = "Sangsekunder, f.eks. 2,5" };
    private readonly TextBlock position = new() { Name = "PlaybackPositionLabel" };
    private readonly TextBlock state = new() { Name = "PlaybackStateLabel" };
    private readonly TextBlock error = new() { Name = "PlaybackErrorLabel", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private bool subscribed;
    private bool disposed;
    public new PlaybackPresenter Presenter { get; }

    public PlaybackPanel(PlaybackPresenter presenter)
    {
        Presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var control in new Control[] { play, pause, stop, input, seek }) actions.Children.Add(control);
        var body = new StackPanel { Spacing = 8 };
        body.Children.Add(actions); body.Children.Add(position); body.Children.Add(state); body.Children.Add(error);
        Content = body;
        play.Click += (_, _) => { if (!disposed) Presenter.Play(); };
        pause.Click += (_, _) => { if (!disposed) Presenter.Pause(); };
        stop.Click += (_, _) => { if (!disposed) Presenter.Stop(); };
        seek.Click += (_, _) => { if (!disposed) Presenter.SeekSongText(input.Text); };
        timer.Tick += Poll;
        Update();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (disposed || subscribed) return;
        Presenter.Changed += PresenterChanged; subscribed = true;
        Presenter.Refresh(); Update(); timer.Start();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Detach(); base.OnDetachedFromVisualTree(e);
    }
    private void Poll(object? sender, EventArgs e) { if (!disposed && subscribed) Presenter.Refresh(); }
    private void PresenterChanged(object? sender, EventArgs e) { if (!disposed && subscribed) Update(); }
    private void Detach()
    {
        timer.Stop();
        if (subscribed) Presenter.Changed -= PresenterChanged;
        subscribed = false;
    }
    private void Update()
    {
        var view = Presenter.ViewState;
        play.IsEnabled = view.CanPlay; pause.IsEnabled = view.CanPause;
        stop.IsEnabled = view.CanStop; seek.IsEnabled = view.CanSeek; input.IsEnabled = view.CanSeek;
        position.Text = view.SongPositionSeconds is { } seconds ? seconds.ToString("G8", CultureInfo.InvariantCulture) + " s" : "Posisjon utilgjengelig";
        state.Text = !view.SongPositionSeconds.HasValue ? "Status utilgjengelig" : view.Snapshot.State switch
        {
            PlaybackState.Stopped => "Stoppet", PlaybackState.Playing => "Spiller", PlaybackState.Paused => "Pause",
            PlaybackState.Ended => "Slutt på klippet", PlaybackState.Faulted => "Avspillingsfeil", PlaybackState.Disposed => "Avsluttet", _ => "Ukjent status"
        };
        error.Text = view.Error ?? "";
    }
    public void Dispose()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (disposed) return;
        disposed = true; Detach(); timer.Tick -= Poll;
        play.IsEnabled = pause.IsEnabled = stop.IsEnabled = seek.IsEnabled = input.IsEnabled = false;
        // Presenter and backend are borrowed; the application disposes them separately.
    }
}
