namespace UltraStar.SongCreator.Acquisition;

/// <summary>One process-wide gate is shared by all normal MusicBrainz providers.</summary>
internal sealed class RecordingRequestGate(TimeProvider clock, Func<TimeSpan, CancellationToken, Task> delay)
{
    private readonly SemaphoreSlim mutex = new(1, 1);
    private long? lastStart;
    private long? cooldownStart;
    private TimeSpan cooldown;

    internal static RecordingRequestGate Shared { get; } =
        new(TimeProvider.System, (duration, token) => Task.Delay(duration, token));

    internal async Task<IDisposable> EnterAsync(CancellationToken token)
    {
        await mutex.WaitAsync(token);
        try
        {
            while (true)
            {
                var now = clock.GetTimestamp();
                var spacing = lastStart is { } previous ? TimeSpan.FromSeconds(1) - clock.GetElapsedTime(previous, now) : TimeSpan.Zero;
                var cooling = cooldownStart is { } start ? cooldown - clock.GetElapsedTime(start, now) : TimeSpan.Zero;
                var remaining = spacing > cooling ? spacing : cooling;
                if (remaining <= TimeSpan.Zero) break;
                await delay(remaining > TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : remaining, token);
            }
            token.ThrowIfCancellationRequested();
            lastStart = clock.GetTimestamp();
            return new Lease(mutex);
        }
        catch { mutex.Release(); throw; }
    }

    // Caller holds the gate, including the response body.
    internal void CoolDown(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return;
        cooldownStart = clock.GetTimestamp();
        cooldown = duration;
    }

    private sealed class Lease(SemaphoreSlim mutex) : IDisposable
    {
        private SemaphoreSlim? owner = mutex;
        public void Dispose() => Interlocked.Exchange(ref owner, null)?.Release();
    }
}
