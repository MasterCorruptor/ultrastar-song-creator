namespace UltraStar.SongCreator.Acquisition.Lyrics;

/// <summary>Serial LRCLIB transport lease with completion spacing and monotonic cooldown.</summary>
internal sealed class LrclibRequestGate(TimeProvider clock)
{
    internal static LrclibRequestGate Shared { get; } = new(TimeProvider.System);
    private readonly TimeProvider clock = clock;
    private readonly SemaphoreSlim mutex = new(1, 1);
    private long? completedAt;
    private long? cooldownAt;
    private TimeSpan cooldown;
    private bool unavailable;

    internal async Task<Lease> EnterAsync(CancellationToken token)
    {
        await mutex.WaitAsync(token).ConfigureAwait(false);
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (unavailable)
                    throw new LyricsSearchException(LyricsSearchError.Unavailable, "LRCLIB is unavailable until this process restarts.");
                var now = clock.GetTimestamp();
                var spacing = Remaining(completedAt, TimeSpan.FromMilliseconds(500), now);
                var cooling = Remaining(cooldownAt, cooldown, now);
                var remaining = spacing > cooling ? spacing : cooling;
                if (remaining == TimeSpan.Zero) return new Lease(this);
                await Task.Delay(remaining > TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : remaining, clock, token).ConfigureAwait(false);
            }
        }
        catch { mutex.Release(); throw; }
    }

    private TimeSpan Remaining(long? anchor, TimeSpan duration, long now)
    {
        if (anchor is null || duration <= TimeSpan.Zero) return TimeSpan.Zero;
        var elapsed = clock.GetElapsedTime(anchor.Value, now);
        if (elapsed >= duration) return TimeSpan.Zero;
        // Monotonic providers must not go backwards; retain the entire delay if a fixture does.
        return elapsed <= TimeSpan.Zero ? duration : duration - elapsed;
    }

    internal sealed class Lease(LrclibRequestGate gate) : IDisposable
    {
        private LrclibRequestGate? owner = gate;
        private bool started;
        internal void MarkRequestStarted()
        {
            ObjectDisposedException.ThrowIf(owner is null, this);
            started = true;
        }
        internal void ExtendCooldown(TimeSpan duration)
        {
            ObjectDisposedException.ThrowIf(owner is null, this);
            var now = owner.clock.GetTimestamp();
            var remaining = owner.Remaining(owner.cooldownAt, owner.cooldown, now);
            owner.cooldown = duration > remaining ? duration : remaining;
            owner.cooldownAt = now;
        }
        internal void Disable()
        {
            ObjectDisposedException.ThrowIf(owner is null, this);
            owner.unavailable = true;
        }
        public void Dispose()
        {
            var current = Interlocked.Exchange(ref owner, null);
            if (current is null) return;
            try { if (started) current.completedAt = current.clock.GetTimestamp(); }
            finally { current.mutex.Release(); }
        }
    }
}
