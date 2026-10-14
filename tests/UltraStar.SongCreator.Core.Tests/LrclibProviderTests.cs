using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using UltraStar.SongCreator.Acquisition.Lyrics;
using Clock = UltraStar.SongCreator.Core.Tests.LrclibRequestGateTests.Clock;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class LrclibProviderTests
{
    private static readonly LyricsQuery Query = new(" Egen & æ? ", " Tittel/+🎵 ", Limit: 1);
    private static HttpResponseMessage Response(int status = 200, HttpContent? content = null) =>
        new((HttpStatusCode)status) { Content = content ?? new ByteArrayContent(Encoding.UTF8.GetBytes("[]")) };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        internal int Sends;
        internal bool Disposed;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Interlocked.Increment(ref Sends); return send(request, token); }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
    private sealed class Fixture : IDisposable
    {
        internal Clock Clock { get; } = new();
        internal LrclibRequestGate Gate { get; }
        internal Handler Handler { get; }
        internal HttpClient Client { get; }
        internal LrclibProvider Provider { get; }
        internal Fixture(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? send = null)
        {
            Gate = new(Clock);
            Handler = new(send ?? ((_, _) => Task.FromResult(Response())));
            Client = new(Handler) { Timeout = Timeout.InfiniteTimeSpan };
            Provider = new(Client, Gate, Clock);
        }
        public void Dispose() { Provider.Dispose(); Client.Dispose(); }
    }
    private sealed class UnreadContent : HttpContent
    {
        internal bool Disposed;
        internal int Reads;
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        { Reads++; throw new IOException("secret-payload"); }
        protected override Task<Stream> CreateContentReadStreamAsync()
        { Reads++; throw new IOException("secret-payload"); }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
    private sealed class Body(byte[] bytes) : Stream
    {
        internal bool Block, Fail, Disposed;
        internal Action? OnDispose;
        internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int position;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            Started.TrySetResult();
            if (Block) await Task.Delay(Timeout.InfiniteTimeSpan, token);
            token.ThrowIfCancellationRequested();
            if (Fail) throw new IOException("secret-payload");
            var count = Math.Min(buffer.Length, bytes.Length - position);
            bytes.AsMemory(position, count).CopyTo(buffer); position += count;
            return count;
        }
        protected override void Dispose(bool disposing)
        {
            if (!Disposed) { Disposed = true; OnDispose?.Invoke(); }
            base.Dispose(disposing);
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
    private static async Task<LyricsSearchException> Error(Task<LyricsPage> operation, LyricsSearchError expected)
    {
        var error = await Assert.ThrowsAsync<LyricsSearchException>(() => operation.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(expected, error.Error); Assert.Null(error.InnerException);
        Assert.DoesNotContain("secret-payload", error.Message);
        return error;
    }
    private static T Field<T>(object instance, string name) =>
        (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    [Fact]
    public async Task OriginalEncodedQueryIdentityHeadersAndSingleSendArePreserved()
    {
        HttpRequestMessage? seen = null;
        using var f = new Fixture((r, _) => { seen = r; return Task.FromResult(Response()); });
        var page = await f.Provider.SearchAsync(Query);
        Assert.Same(Query, page.Query); Assert.Empty(page.Candidates); Assert.Equal(f.Clock.InitialUtc, page.RetrievedAtUtc);
        Assert.Equal("lrclib", page.ProviderId);
        Assert.Equal("https://lrclib.net/api/search?track_name=" + Uri.EscapeDataString(Query.Title)
            + "&artist_name=" + Uri.EscapeDataString(Query.Artist), seen!.RequestUri!.AbsoluteUri);
        Assert.Equal(HttpMethod.Get, seen.Method); Assert.Null(seen.Content);
        Assert.Equal("application/json", seen.Headers.Accept.Single().MediaType);
        Assert.Equal("UltraStarSongCreator/0.0-prototype (+https://github.com/MasterCorruptor/ultrastar-song-creator)", seen.Headers.UserAgent.ToString());
        Assert.Null(seen.Headers.Authorization); Assert.Empty(seen.Headers.AcceptEncoding);
        Assert.Equal(1, f.Handler.Sends); Assert.Equal(0, f.Clock.ActiveTimers);
    }

    [Fact]
    public async Task ProductionFlagsSharedGateAndOwnedVersusBorrowedDisposalAreExplicit()
    {
        using var a = new LrclibProvider(); using var b = new LrclibProvider();
        var handler = Field<HttpClientHandler>(a, "ownedHandler");
        Assert.False(handler.AllowAutoRedirect); Assert.False(handler.UseCookies);
        Assert.Equal(DecompressionMethods.None, handler.AutomaticDecompression);
        Assert.False(handler.UseDefaultCredentials); Assert.Null(handler.Credentials);
        Assert.Null(handler.ServerCertificateCustomValidationCallback); Assert.Equal(64, handler.MaxResponseHeadersLength);
        Assert.Equal(Timeout.InfiniteTimeSpan, Field<HttpClient>(a, "client").Timeout);
        Assert.Same(Field<LrclibRequestGate>(a, "gate"), Field<LrclibRequestGate>(b, "gate"));
        Assert.Empty(Assert.Single(typeof(LrclibProvider).GetConstructors()).GetParameters());
        a.Dispose(); a.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => a.SearchAsync(Query));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => Field<HttpClient>(a, "client").GetAsync("https://never-send.invalid"));
        using var f = new Fixture();
        f.Provider.Dispose(); f.Provider.Dispose(); Assert.False(f.Handler.Disposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => f.Provider.SearchAsync(Query));
        using var reply = await f.Client.GetAsync("https://terminal-fixture.invalid");
        Assert.Equal(1, f.Handler.Sends); // Borrowed client is still usable; terminal fixture only.
    }

    [Fact]
    public async Task CallerValidationAndPreCancellationSendNothing()
    {
        using var f = new Fixture();
        await Assert.ThrowsAsync<LyricsValidationException>(() => f.Provider.SearchAsync(Query with { Limit = 0 }));
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        var e = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Provider.SearchAsync(Query, cancel.Token));
        Assert.Equal(cancel.Token, e.CancellationToken); Assert.Equal(0, f.Handler.Sends);
        using var wrongClient = new HttpClient(new Handler((_, _) => Task.FromResult(Response())));
        Assert.Throws<ArgumentException>(() => new LrclibProvider(wrongClient, f.Gate, f.Clock));
    }

    [Theory]
    [InlineData(201, LyricsSearchError.HttpFailure)]
    [InlineData(204, LyricsSearchError.HttpFailure)]
    [InlineData(301, LyricsSearchError.HttpFailure)]
    [InlineData(302, LyricsSearchError.HttpFailure)]
    [InlineData(400, LyricsSearchError.HttpFailure)]
    [InlineData(404, LyricsSearchError.HttpFailure)]
    [InlineData(429, LyricsSearchError.RateLimited)]
    [InlineData(500, LyricsSearchError.HttpFailure)]
    [InlineData(503, LyricsSearchError.Unavailable)]
    public async Task Non200IsClassifiedWithoutReadingErrorBodiesOrFollowingRedirects(int status, LyricsSearchError expected)
    {
        var content = new UnreadContent(); content.Headers.TryAddWithoutValidation("Content-Type", "text/html");
        using var f = new Fixture((_, _) =>
        {
            var response = Response(status, content);
            response.Headers.Location = new("https://never-follow.invalid/secret-payload");
            return Task.FromResult(response);
        });
        var e = await Error(f.Provider.SearchAsync(Query), expected);
        Assert.Equal(status, e.StatusCode); Assert.Null(e.RetryAfter);
        Assert.Equal(0, content.Reads); Assert.True(content.Disposed); Assert.Equal(1, f.Handler.Sends);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("application/json", true)]
    [InlineData("Application/Json; charset=utf-8", true)]
    [InlineData("text/plain", false)]
    [InlineData("application/ld+json", false)]
    [InlineData("broken;", false)]
    public async Task OnlyAbsentOrValidJsonMediaTypeIsAccepted(string? media, bool valid)
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes("[]"));
        if (media is not null) content.Headers.TryAddWithoutValidation("Content-Type", media);
        using var f = new Fixture((_, _) => Task.FromResult(Response(content: content)));
        if (valid) Assert.Empty((await f.Provider.SearchAsync(Query)).Candidates);
        else await Error(f.Provider.SearchAsync(Query), LyricsSearchError.InvalidResponse);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("identity", true)]
    [InlineData("IDENTITY", true)]
    [InlineData("gzip", false)]
    [InlineData("identity, gzip", false)]
    [InlineData("", false)]
    public async Task CompressionIsNeverDecoded(string? encoding, bool valid)
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes("[]"));
        if (encoding is not null) content.Headers.TryAddWithoutValidation("Content-Encoding", encoding);
        using var f = new Fixture((_, _) => Task.FromResult(Response(content: content)));
        if (valid) await f.Provider.SearchAsync(Query);
        else await Error(f.Provider.SearchAsync(Query), LyricsSearchError.InvalidResponse);
    }

    [Fact]
    public async Task DeclaredExcessIsRejectedWithoutReadingAndHeaderLimitAlsoAppliesToFixtures()
    {
        var content = new UnreadContent(); content.Headers.ContentLength = LrclibJson.MaxInputBytes + 1;
        using (var f = new Fixture((_, _) => Task.FromResult(Response(content: content))))
        {
            await Error(f.Provider.SearchAsync(Query), LyricsSearchError.ResponseTooLarge);
            Assert.Equal(0, content.Reads); Assert.True(content.Disposed);
        }
        using var headerFixture = new Fixture((_, _) =>
        {
            var response = Response(); response.Headers.TryAddWithoutValidation("X-Synthetic", new string('a', 65536));
            return Task.FromResult(response);
        });
        await Error(headerFixture.Provider.SearchAsync(Query), LyricsSearchError.NetworkFailure);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task ExactStreamByteLimitPassesAndNextByteFails(int excess, bool valid)
    {
        var bytes = Encoding.UTF8.GetBytes("[]" + new string(' ', LrclibJson.MaxInputBytes - 2 + excess));
        var body = new Body(bytes); var content = new StreamContent(body);
        using var f = new Fixture((_, _) => Task.FromResult(Response(content: content)));
        if (valid) Assert.Empty((await f.Provider.SearchAsync(Query)).Candidates);
        else await Error(f.Provider.SearchAsync(Query), LyricsSearchError.ResponseTooLarge);
        Assert.True(body.Disposed);
    }

    [Theory]
    [InlineData("[{}]", LyricsSearchError.InvalidResponse)]
    [InlineData("[{\"id\":1,\"instrumental\":false,\"plainLyrics\":\"Egen tekst\",\"syncedLyrics\":\"[0:1]Egen tekst\"}]", null)]
    public async Task AcceptedDecoderIsReusedWithOriginalQueryAndSingleReceipt(string body, LyricsSearchError? failure)
    {
        using var f = new Fixture((_, _) => Task.FromResult(Response(content: new ByteArrayContent(Encoding.UTF8.GetBytes(body)))));
        if (failure is { } error) await Error(f.Provider.SearchAsync(Query), error);
        else
        {
            var page = await f.Provider.SearchAsync(Query); var candidate = Assert.Single(page.Candidates);
            Assert.Same(Query, page.Query); Assert.Equal(page.RetrievedAtUtc, candidate.RetrievedAtUtc);
            Assert.Equal(2, candidate.Documents.Length); Assert.Equal(1, f.Handler.Sends);
        }
    }

    [Theory]
    [InlineData(null, -1L)]
    [InlineData("invalid secret-payload", -1L)]
    [InlineData("-1", -1L)]
    [InlineData("+1", -1L)]
    [InlineData("1.5", -1L)]
    [InlineData("1e5", -1L)]
    [InlineData("１２", -1L)]
    [InlineData("0", 0L)]
    [InlineData("00000", 0L)]
    [InlineData(" \t0001 \t", 1L)]
    [InlineData("2147483648", 2147483648L)]
    [InlineData("922337203685", 922337203685L)]
    [InlineData("000922337203685", 922337203685L)]
    public async Task RetryAfterDecimalAndFallbackMetadataDoNotDowncapValidLongValues(string? header, long seconds)
    {
        using var f = new Fixture((_, _) =>
        {
            var response = Response(429);
            if (header is not null) response.Headers.TryAddWithoutValidation("Retry-After", header);
            return Task.FromResult(response);
        });
        var e = await Error(f.Provider.SearchAsync(Query), LyricsSearchError.RateLimited);
        Assert.Equal(429, e.StatusCode);
        Assert.Equal(seconds < 0 ? null : TimeSpan.FromTicks(checked(seconds * TimeSpan.TicksPerSecond)), e.RetryAfter);
        if (seconds > 30)
        {
            var next = f.Provider.SearchAsync(Query);
            Assert.False(next.IsCompleted); f.Clock.Advance(TimeSpan.FromSeconds(30));
            await Error(next, LyricsSearchError.TimedOut);
            Assert.Equal(1, f.Handler.Sends);
            Assert.All(f.Clock.Scheduled, d => Assert.InRange(d, TimeSpan.Zero, TimeSpan.FromMinutes(1)));
        }
    }

    [Theory]
    [InlineData(429)]
    [InlineData(503)]
    public async Task ExtremeValidDeltaPreservesOriginalClassAndFailsOnlyThisGateClosed(int status)
    {
        foreach (var header in new[] { "922337203686", "000922337203686", new string('9', 60000) })
        {
            using var f = new Fixture((_, _) =>
            {
                var response = Response(status); response.Headers.TryAddWithoutValidation("Retry-After", header);
                return Task.FromResult(response);
            });
            var first = await Error(f.Provider.SearchAsync(Query), status == 429 ? LyricsSearchError.RateLimited : LyricsSearchError.Unavailable);
            Assert.Equal(status, first.StatusCode); Assert.Null(first.RetryAfter);
            var next = await Error(f.Provider.SearchAsync(Query), LyricsSearchError.Unavailable);
            Assert.Null(next.StatusCode); Assert.Equal(1, f.Handler.Sends);
            using var cancel = new CancellationTokenSource(); cancel.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Provider.SearchAsync(Query, cancel.Token));
            using var other = new Fixture(); await other.Provider.SearchAsync(Query);
        }
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(7)]
    public async Task HttpDatesUseReceiptAndPastDateIsZero(int seconds)
    {
        var clock = new Clock();
        using var f = new Fixture((_, _) =>
        {
            var response = Response(503);
            response.Headers.TryAddWithoutValidation("Retry-After", clock.InitialUtc.AddSeconds(seconds).ToString("r", CultureInfo.InvariantCulture));
            return Task.FromResult(response);
        });
        var e = await Error(f.Provider.SearchAsync(Query), LyricsSearchError.Unavailable);
        Assert.Equal(TimeSpan.FromSeconds(Math.Max(0, seconds)), e.RetryAfter);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrMultipleHeaderFallbackWaitsOneSecond(bool multiple)
    {
        var sends = 0;
        using var f = new Fixture((_, _) =>
        {
            var response = Response(++sends == 1 ? 429 : 200);
            if (multiple) response.Headers.TryAddWithoutValidation("Retry-After", new[] { "0", "99999999999999" });
            return Task.FromResult(response);
        });
        var e = await Error(f.Provider.SearchAsync(Query), LyricsSearchError.RateLimited); Assert.Null(e.RetryAfter);
        var next = f.Provider.SearchAsync(Query); f.Clock.Advance(TimeSpan.FromMilliseconds(999));
        Assert.Equal(1, f.Handler.Sends); f.Clock.Advance(TimeSpan.FromMilliseconds(1));
        await next.WaitAsync(TimeSpan.FromSeconds(5)); Assert.Equal(2, f.Handler.Sends);
    }

    [Theory]
    [InlineData("queue")]
    [InlineData("cooldown")]
    [InlineData("headers")]
    [InlineData("body")]
    public async Task OverallDeadlineStartsBeforeEveryTransportStageAndCleansUp(string stage)
    {
        var body = new Body(Encoding.UTF8.GetBytes("[]")) { Block = stage == "body" };
        using var f = new Fixture(async (_, token) =>
        {
            if (stage == "headers") await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return Response(content: new StreamContent(body));
        });
        LrclibRequestGate.Lease? held = null;
        if (stage == "queue") held = await f.Gate.EnterAsync(default);
        if (stage == "cooldown") { using var permission = await f.Gate.EnterAsync(default); permission.ExtendCooldown(TimeSpan.FromMinutes(2)); }
        var operation = f.Provider.SearchAsync(Query);
        if (stage == "body") await body.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        f.Clock.Advance(TimeSpan.FromSeconds(30));
        await Error(operation, LyricsSearchError.TimedOut);
        held?.Dispose();
        Assert.Equal(stage is "queue" or "cooldown" ? 0 : 1, f.Handler.Sends);
        if (stage == "body") Assert.True(body.Disposed);
        Assert.Equal(0, f.Clock.ActiveTimers);
    }

    [Theory]
    [InlineData("queue")]
    [InlineData("cooldown")]
    [InlineData("headers")]
    [InlineData("body")]
    public async Task CallerCancellationTakesPrecedenceAndRetainsOriginalToken(string stage)
    {
        var body = new Body([]) { Block = stage == "body" };
        using var f = new Fixture(async (_, token) =>
        {
            if (stage == "headers") await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return Response(content: new StreamContent(body));
        });
        using var cancel = new CancellationTokenSource();
        LrclibRequestGate.Lease? held = null;
        if (stage == "queue") held = await f.Gate.EnterAsync(default);
        if (stage == "cooldown") { using var permission = await f.Gate.EnterAsync(default); permission.ExtendCooldown(TimeSpan.MaxValue); }
        var operation = f.Provider.SearchAsync(Query, cancel.Token);
        if (stage == "body") await body.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancel.Cancel(); f.Clock.Advance(TimeSpan.FromSeconds(30));
        var e = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(cancel.Token, e.CancellationToken); held?.Dispose();
        if (stage == "body") Assert.True(body.Disposed);
        Assert.Equal(0, f.Clock.ActiveTimers);
    }

    [Fact]
    public async Task CompletionSpacingAcrossInstancesIncludesBodyDisposalAndLaterReuse()
    {
        var clock = new Clock(); var gate = new LrclibRequestGate(clock); var sends = new List<long>();
        var body = new Body(Encoding.UTF8.GetBytes("[]")) { OnDispose = () => clock.Advance(TimeSpan.FromSeconds(2)) };
        using var handler = new Handler((_, _) =>
        {
            sends.Add(clock.GetTimestamp());
            return Task.FromResult(Response(content: sends.Count == 1 ? new StreamContent(body) : new ByteArrayContent(Encoding.UTF8.GetBytes("[]"))));
        });
        using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        using var a = new LrclibProvider(client, gate, clock); using var b = new LrclibProvider(client, gate, clock);
        await a.SearchAsync(Query); Assert.True(body.Disposed);
        var second = b.SearchAsync(Query); Assert.Single(sends);
        clock.Advance(TimeSpan.FromMilliseconds(499)); Assert.Single(sends);
        clock.Advance(TimeSpan.FromMilliseconds(1)); await second.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(TimeSpan.FromSeconds(2.5).Ticks, sends[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NetworkAndReadFailureAreSanitizedAndGateCanBeReused(bool readFailure)
    {
        var sends = 0; var body = new Body([]) { Fail = true };
        using var f = new Fixture((_, _) =>
        {
            if (++sends > 1) return Task.FromResult(Response());
            if (readFailure) return Task.FromResult(Response(content: new StreamContent(body)));
            throw new HttpRequestException("https://secret-payload.invalid");
        });
        await Error(f.Provider.SearchAsync(Query), LyricsSearchError.NetworkFailure);
        if (readFailure) Assert.True(body.Disposed);
        var next = f.Provider.SearchAsync(Query); Assert.Equal(1, f.Handler.Sends);
        f.Clock.Advance(TimeSpan.FromMilliseconds(500));
        await next.WaitAsync(TimeSpan.FromSeconds(5)); Assert.Equal(2, f.Handler.Sends);
    }
    [Fact]
    public async Task ServerCooldownSurvivesTimedOutCallAndExpiresWithoutEarlySend()
    {
        var sends = 0;
        using var f = new Fixture((_, _) =>
        {
            var response = Response(++sends == 1 ? 429 : 200);
            if (sends == 1) response.Headers.TryAddWithoutValidation("Retry-After", "35");
            return Task.FromResult(response);
        });
        await Error(f.Provider.SearchAsync(Query), LyricsSearchError.RateLimited);
        var expiring = f.Provider.SearchAsync(Query);
        f.Clock.Advance(TimeSpan.FromSeconds(30));
        await Error(expiring, LyricsSearchError.TimedOut);
        var allowedLater = f.Provider.SearchAsync(Query);
        Assert.Equal(1, f.Handler.Sends);
        f.Clock.Advance(TimeSpan.FromSeconds(4)); Assert.Equal(1, f.Handler.Sends);
        f.Clock.Advance(TimeSpan.FromSeconds(1));
        await allowedLater.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, f.Handler.Sends); Assert.Equal(0, f.Clock.ActiveTimers);
    }

    [Fact]
    public async Task ActiveBodyAndQueuedSearchTimeOutThenSharedGateRecoversAfterAbortSpacing()
    {
        var body = new Body([]) { Block = true };
        using var f = new Fixture((_, _) => Task.FromResult(Response(content:
            !body.Disposed ? new StreamContent(body) : new ByteArrayContent(Encoding.UTF8.GetBytes("[]")))));
        using var secondProvider = new LrclibProvider(f.Client, f.Gate, f.Clock);
        var first = f.Provider.SearchAsync(Query);
        await body.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var queued = secondProvider.SearchAsync(Query);
        Assert.Equal(1, f.Handler.Sends);
        f.Clock.Advance(TimeSpan.FromSeconds(30));
        await Error(first, LyricsSearchError.TimedOut);
        await Error(queued, LyricsSearchError.TimedOut);
        Assert.True(body.Disposed); Assert.Equal(1, f.Handler.Sends);
        var recovered = secondProvider.SearchAsync(Query);
        Assert.Equal(1, f.Handler.Sends);
        f.Clock.Advance(TimeSpan.FromMilliseconds(500));
        await recovered.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, f.Handler.Sends); Assert.Equal(0, f.Clock.ActiveTimers);
    }

    [Fact]
    public async Task InvalidUtf8AndContentHeaderFormsAreSanitizedWithoutBodyLeaks()
    {
        using (var f = new Fixture((_, _) => Task.FromResult(Response(content: new ByteArrayContent([0xff])))))
            await Error(f.Provider.SearchAsync(Query), LyricsSearchError.InvalidResponse);
        foreach (var (name, values) in new[]
        {
            ("Content-Type", new[] { "application/json", "text/plain" }),
            ("Content-Length", new[] { "-1" }),
            ("Content-Length", new[] { "broken secret-payload" }),
            ("Content-Length", new[] { "1", "2" })
        })
        {
            var body = new Body(Encoding.UTF8.GetBytes("[]"));
            var content = new StreamContent(body);
            content.Headers.TryAddWithoutValidation(name, values);
            using var f = new Fixture((_, _) => Task.FromResult(Response(content: content)));
            await Error(f.Provider.SearchAsync(Query), LyricsSearchError.InvalidResponse);
            Assert.False(body.Started.Task.IsCompleted);
            Assert.True(body.Disposed);
        }
    }

    [Fact]
    public async Task ZeroRetryStillHonorsCompletionSpacingAndHeaderDisposal()
    {
        var sends = 0;
        var content = new UnreadContent();
        using var f = new Fixture((_, _) =>
        {
            var response = ++sends == 1 ? Response(429, content) : Response();
            if (sends == 1) response.Headers.TryAddWithoutValidation("Retry-After", "0");
            return Task.FromResult(response);
        });
        Assert.Equal(TimeSpan.Zero, (await Error(f.Provider.SearchAsync(Query), LyricsSearchError.RateLimited)).RetryAfter);
        Assert.True(content.Disposed);
        var next = f.Provider.SearchAsync(Query);
        f.Clock.Advance(TimeSpan.FromMilliseconds(499)); Assert.Equal(1, f.Handler.Sends);
        f.Clock.Advance(TimeSpan.FromMilliseconds(1)); await next.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, f.Handler.Sends);
    }

}
