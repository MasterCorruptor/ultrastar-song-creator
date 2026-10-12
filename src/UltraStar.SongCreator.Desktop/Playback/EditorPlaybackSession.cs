using Avalonia.Threading;
using UltraStar.SongCreator.Audio;
using UltraStar.SongCreator.Desktop.Timeline;

namespace UltraStar.SongCreator.Desktop.Playback;

/// <summary>One UI-session owner. Backend factories transfer ownership; failed factories clean their own partial native init.</summary>
public sealed class EditorPlaybackSession : IDisposable
{
    private IPcmPlayback? backend;
    private bool disposed;
    public PcmClip Clip { get; }
    public WaveformEnvelope Envelope { get; }
    public TimelineMapping Mapping { get; } = TimelineMapping.Demo;
    public PlaybackPresenter? Presenter { get; private set; }
    public PlaybackPanel? Panel { get; private set; }
    public string? InitializationError { get; private set; }

    public EditorPlaybackSession(Func<PcmClip, IPcmPlayback> factory, double audioOffsetSeconds)
    {
        Dispatcher.UIThread.VerifyAccess();
        ArgumentNullException.ThrowIfNull(factory);
        if (!double.IsFinite(audioOffsetSeconds)) throw new ArgumentOutOfRangeException(nameof(audioOffsetSeconds));
        Clip = DemoPcm.Create();
        Envelope = WaveformEnvelope.Create(Clip, 480);
        try
        {
            backend = factory(Clip) ?? throw new PlaybackException("Playback factory returned no backend.");
            if (!ReferenceEquals(Clip, backend.Clip)) throw new PlaybackException("Playback must use the cached demo clip.");
            Presenter = new PlaybackPresenter(backend, Mapping, audioOffsetSeconds);
            Panel = new PlaybackPanel(Presenter);
        }
        catch (Exception error)
        {
            InitializationError = "Lyd kunne ikke startes. Kontroller lydenheten og runtime-bibliotekene. Redigering er fortsatt tilgjengelig. " + error.Message;
            Release();
            Presenter = null; Panel = null;
        }
    }

    private void Release()
    {
        try { Panel?.Dispose(); }
        finally
        {
            try { Presenter?.Dispose(); }
            finally { var owned = backend; backend = null; owned?.Dispose(); }
        }
    }

    public void Dispose()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (disposed) return;
        disposed = true; Release();
    }
}
