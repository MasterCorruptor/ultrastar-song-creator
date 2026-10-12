using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.MiniAudio;

namespace UltraStar.SongCreator.Audio;

public sealed class PlaybackException(string message, Exception? inner = null) : InvalidOperationException(message, inner);

/// <summary>Default native playback device. Keeps silence flowing while paused/stopped; never falls back to a mock.</summary>
public sealed unsafe class MiniAudioPlayback : IPcmPlayback
{
    private readonly object lifecycle = new();
    private readonly PcmPlayback renderer;
    private MaDevice* device;
    private GCHandle rendererHandle;
    private bool initialized;
    private bool disposed;
    public PcmClip Clip => renderer.Clip;
    public PlaybackSnapshot Snapshot => renderer.Snapshot;
    public string DeviceBackend { get; } = "Uninitialized";
    public static string NativeVersion => MiniAudio.VersionStringS();

    public MiniAudioPlayback(PcmClip clip)
    {
        renderer = new(clip);
        try
        {
            rendererHandle = GCHandle.Alloc(renderer);
            device = (MaDevice*)NativeMemory.AllocZeroed((nuint)sizeof(MaDevice));
            var config = MiniAudio.DeviceConfigInit(MaDeviceType.Playback);
            config.Playback.Format = MaFormat.F32;
            config.Playback.Channels = (uint)clip.Channels;
            config.SampleRate = (uint)clip.SampleRate;
            config.DataCallback = (void*)(delegate* unmanaged[Cdecl]<MaDevice*, void*, void*, uint, void>)&Callback;
            config.PUserData = (void*)GCHandle.ToIntPtr(rendererHandle);
            var result = MiniAudio.DeviceInit(default(MaContextPtr), in config, new MaDevicePtr(device));
            if (result != MaResult.Success) throw new PlaybackException($"Cannot initialize default audio playback device ({result}). Check audio device availability.");
            initialized = true;
            if (device->PContext == null || device->PContext->Backend == MaBackend.Null)
                throw new PlaybackException("No real default audio backend is available; the null backend is not a playback substitute.");
            if (device->SampleRate != clip.SampleRate || device->Playback.Channels != clip.Channels || device->Playback.Format != MaFormat.F32)
                throw new PlaybackException("Audio device did not preserve the requested PCM callback format.");
            DeviceBackend = device->PContext->Backend.ToString();
            result = MiniAudio.DeviceStart(new MaDevicePtr(device));
            if (result != MaResult.Success) throw new PlaybackException($"Cannot start audio playback device ({result}).");
        }
        catch (Exception e)
        {
            Release();
            if (e is PlaybackException) throw;
            throw new PlaybackException("Native miniaudio playback could not be initialized. Check runtime/library compatibility and the default audio device.", e);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Callback(MaDevice* device, void* output, void* input, uint frames)
    {
        // Native device-uninit joins the callback before the handle is released. No native lifecycle calls here.
        if (output == null || frames == 0) return;
        PcmPlayback? renderer = null;
        Span<float> samples = default;
        try
        {
            renderer = (PcmPlayback)GCHandle.FromIntPtr((nint)device->PUserData).Target!;
            samples = new Span<float>(output, checked((int)frames * renderer.Clip.Channels));
            renderer.Render(samples);
        }
        catch
        {
            samples.Clear();
            renderer?.FailCallback();
        }
    }
    private void Command(Action<PcmPlayback> action)
    {
        lock (lifecycle)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            action(renderer);
        }
    }
    public void Play() => Command(static p => p.Play());
    public void Pause() => Command(static p => p.Pause());
    public void Stop() => Command(static p => p.Stop());
    public void SeekFrame(long frame) => Command(p => p.SeekFrame(frame));
    public void SeekSeconds(double seconds) => Command(p => p.SeekSeconds(seconds));
    private void Release()
    {
        // Uninit blocks until the native device thread has stopped. Never hold renderer.gate across this join.
        if (initialized) { MiniAudio.DeviceUninit(new MaDevicePtr(device)); initialized = false; }
        renderer.Dispose();
        if (rendererHandle.IsAllocated) rendererHandle.Free();
        if (device != null) { NativeMemory.Free(device); device = null; }
        disposed = true;
    }
    public void Dispose()
    {
        lock (lifecycle) { if (!disposed) Release(); }
    }
}
