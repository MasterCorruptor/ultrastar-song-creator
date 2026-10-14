using UltraStar.SongCreator.Acquisition.Lyrics;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class LrclibRequestGateTests
{
    // Fixture-local monotonic clock. Timers advance only when a test explicitly moves time.
    internal sealed class Clock : TimeProvider
    {
        private readonly object sync = new();
        private readonly List<Timer> timers = [];
        private long ticks;
        internal DateTimeOffset InitialUtc { get; } = new(2026, 10, 14, 10, 0, 0, TimeSpan.Zero);
        internal List<TimeSpan> Scheduled { get; } = [];
        internal int ActiveTimers { get { lock (sync) return timers.Count; } }
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() { lock (sync) return ticks; }
        public override DateTimeOffset GetUtcNow() => InitialUtc + TimeSpan.FromTicks(GetTimestamp());
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new Timer(this, callback, state);
            timer.Change(dueTime, period);
            return timer;
        }
        internal void Advance(TimeSpan duration)
        {
            lock (sync) ticks = checked(ticks + duration.Ticks);
            while (true)
            {
                Timer? next;
                lock (sync)
                {
                    next = timers.Where(t => t.Due <= ticks).OrderBy(t => t.Due).FirstOrDefault();
                    if (next is null) return;
                    if (next.Period == Timeout.InfiniteTimeSpan) timers.Remove(next);
                    else next.Due = checked(ticks + next.Period.Ticks);
                }
                next.Callback(next.State);
            }
        }
        private sealed class Timer(Clock owner, TimerCallback callback, object? state) : ITimer
        {
            internal TimerCallback Callback { get; } = callback;
            internal object? State { get; } = state;
            internal long Due { get; set; }
            internal TimeSpan Period { get; private set; }
            private bool disposed;
            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (owner.sync)
                {
                    if (disposed) return false;
                    owner.timers.Remove(this);
                    Period = period;
                    if (dueTime != Timeout.InfiniteTimeSpan)
                    {
                        Due = checked(owner.ticks + dueTime.Ticks);
                        owner.Scheduled.Add(dueTime);
                        owner.timers.Add(this);
                    }
                    return true;
                }
            }
            public void Dispose() { lock (owner.sync) { disposed = true; owner.timers.Remove(this); } }
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }

    [Fact]
    public async Task CompletionSpacingIsMeasuredFromAbortNotRequestStart()
    {
        var clock = new Clock(); var gate = new LrclibRequestGate(clock);
        var first = await gate.EnterAsync(default);
        first.MarkRequestStarted();
        clock.Advance(TimeSpan.FromSeconds(5));
        first.Dispose(); first.Dispose();
        var second = gate.EnterAsync(default);
        Assert.False(second.IsCompleted);
        clock.Advance(TimeSpan.FromMilliseconds(499));
        Assert.False(second.IsCompleted);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        using var lease = await second;
        Assert.Equal(TimeSpan.FromMilliseconds(500), clock.Scheduled.Single());
    }

    [Fact]
    public async Task UnsentLeaseAndCanceledQueuedWaiterDoNotCreateCompletion()
    {
        var clock = new Clock(); var gate = new LrclibRequestGate(clock);
        using (var held = await gate.EnterAsync(default))
        {
            using var canceled = new CancellationTokenSource();
            var queued = gate.EnterAsync(canceled.Token);
            Assert.False(queued.IsCompleted);
            canceled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
        }
        using var next = await gate.EnterAsync(default);
        Assert.Empty(clock.Scheduled);
    }

    [Fact]
    public async Task CanceledCooldownWaitReleasesMutexWithoutErasingOutstandingDelay()
    {
        var clock = new Clock(); var gate = new LrclibRequestGate(clock);
        using (var lease = await gate.EnterAsync(default)) lease.ExtendCooldown(TimeSpan.FromSeconds(10));
        using var cancel = new CancellationTokenSource();
        var wait = gate.EnterAsync(cancel.Token);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
        clock.Advance(TimeSpan.FromSeconds(3));
        var next = gate.EnterAsync(default);
        Assert.False(next.IsCompleted);
        Assert.Equal(TimeSpan.FromSeconds(7), clock.Scheduled.Last());
        clock.Advance(TimeSpan.FromSeconds(7));
        using var allowed = await next;
    }

    [Fact]
    public async Task MaximumOutstandingCooldownIsRetainedAndWaitChunksAreBounded()
    {
        var clock = new Clock(); var gate = new LrclibRequestGate(clock);
        using (var lease = await gate.EnterAsync(default))
        {
            lease.ExtendCooldown(TimeSpan.MaxValue);
            clock.Advance(TimeSpan.FromSeconds(10));
            lease.ExtendCooldown(TimeSpan.FromSeconds(1));
        }
        using var cancel = new CancellationTokenSource();
        var pending = gate.EnterAsync(cancel.Token);
        Assert.Equal(TimeSpan.FromMinutes(1), clock.Scheduled.Single());
        clock.Advance(TimeSpan.FromMinutes(1));
        // Cancel even if the continuation has not yet scheduled another chunk.
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.All(clock.Scheduled, t => Assert.InRange(t, TimeSpan.Zero, TimeSpan.FromMinutes(1)));
        Assert.Equal(0, clock.ActiveTimers);
    }

    [Fact]
    public async Task CooldownExpiryAndExtensionUseElapsedTimeWithoutDateAddition()
    {
        var clock = new Clock(); var gate = new LrclibRequestGate(clock);
        using (var lease = await gate.EnterAsync(default))
        {
            lease.ExtendCooldown(TimeSpan.FromSeconds(5));
            clock.Advance(TimeSpan.FromSeconds(3));
            lease.ExtendCooldown(TimeSpan.FromSeconds(4));
        }
        var next = gate.EnterAsync(default);
        Assert.Equal(TimeSpan.FromSeconds(4), clock.Scheduled.Last());
        clock.Advance(TimeSpan.FromSeconds(4));
        using var allowed = await next;
    }

    [Fact]
    public async Task FailClosedIsGateLocalAndCancellationStillWins()
    {
        var clock = new Clock(); var gate = new LrclibRequestGate(clock);
        using (var lease = await gate.EnterAsync(default)) lease.Disable();
        var error = await Assert.ThrowsAsync<LyricsSearchException>(() => gate.EnterAsync(default));
        Assert.Equal(LyricsSearchError.Unavailable, error.Error);
        Assert.Null(error.StatusCode); Assert.Null(error.RetryAfter); Assert.Null(error.InnerException);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.EnterAsync(cancel.Token));
        using var other = await new LrclibRequestGate(clock).EnterAsync(default);
    }
}
