using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using UltraStar.SongCreator.Acquisition;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class RecordingSearchTests
{
    internal const string RecordingId = "11111111-1111-1111-1111-111111111111";
    internal const string ReleaseId = "33333333-3333-3333-3333-333333333333";
    internal const string Body = """
        {"count":2,"offset":0,"recordings":[
          {"id":"11111111-1111-1111-1111-111111111111","title":"Own song","length":123456,"video":false,"disambiguation":"studio recording","score":"100",
           "artist-credit":[
             {"name":"Credited name","joinphrase":" feat. ","artist":{"id":"44444444-4444-4444-4444-444444444444","name":"Original name"}},
             {"artist":{"id":"55555555-5555-5555-5555-555555555555","name":"Second artist"}}],
           "releases":[{"id":"33333333-3333-3333-3333-333333333333","title":"Own release","date":"2020-01","country":"NO","disambiguation":"remastered edition"}],
           "tags":[{"name":"ignored supplementary tag"}]},
          {"id":"22222222-2222-2222-2222-222222222222","title":"Own song","video":null,"disambiguation":"live at own venue","artist-credit":[]}
        ]}
        """;

    internal sealed class Clock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => ticks;
        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2026, 10, 12, 0, 0, 0, TimeSpan.Zero).AddTicks(ticks);
        public void Advance(TimeSpan amount) => ticks += amount.Ticks;
    }

    internal sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => response(request, token);
    }

    private sealed class UncountedContent(byte[] bytes) : HttpContent
    {
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(bytes).AsTask();
        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    private sealed class WaitingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            await Task.Delay(Timeout.Infinite, token);
            return 0;
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    internal static RecordingPage Page() => RecordingJson.Parse(Body, new(Title: "Own song"), DateTimeOffset.UtcNow);
    private static HttpResponseMessage Ok(string body = Body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static MusicBrainzRecordingProvider Provider(HttpClient client, Clock? clock = null, RecordingRequestGate? gate = null, int limit = 2 * 1024 * 1024, TimeSpan? timeout = null)
    {
        clock ??= new();
        gate ??= new(clock, (duration, token) => { token.ThrowIfCancellationRequested(); clock.Advance(duration); return Task.CompletedTask; });
        return new(client, gate, clock, timeout ?? TimeSpan.FromSeconds(10), limit);
    }

    [Theory]
    [InlineData("Own artist", null, "artist:")]
    [InlineData(null, "Own title", "recording:")]
    [InlineData("Own artist", "Own title", " AND ")]
    public async Task AllSearchModesHaveLiteralFieldsAndIdentifiedReadOnlyRequests(string? artist, string? title, string expected)
    {
        using var client = new HttpClient(new Handler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https", request.RequestUri!.Scheme);
            Assert.Equal("musicbrainz.org", request.RequestUri.Host);
            Assert.Equal("/ws/2/recording", request.RequestUri.AbsolutePath);
            Assert.Contains(expected, Uri.UnescapeDataString(request.RequestUri.Query));
            Assert.Contains("application/json", request.Headers.Accept.ToString());
            Assert.Contains("UltraStarSongCreator/0.1.0", request.Headers.UserAgent.ToString());
            Assert.Contains("github.com/MasterCorruptor/ultrastar-song-creator", request.Headers.UserAgent.ToString());
            Assert.Null(request.Headers.Authorization);
            return Task.FromResult(Ok());
        }));
        using var provider = Provider(client);
        var page = await provider.SearchAsync(new(artist, title));
        Assert.Equal(2, page.Candidates.Length);
        Assert.False(page.HasMore);
    }

    [Fact]
    public void LuceneAndUrlEscapingKeepSpecialSyntaxAndUnicodeInsideLiteral()
    {
        var query = MusicBrainzRecordingProvider.Normalize(new(" AC/DC ", "æøå \" OR artist:* & [] \\"));
        var uri = MusicBrainzRecordingProvider.QueryUri(query);
        var lucene = Uri.UnescapeDataString(uri.Query.Split('&').Single(p => p.StartsWith("query="))[6..]);
        Assert.Contains("artist:\"AC\\/DC\"", lucene);
        Assert.Contains("recording:\"æøå", lucene);
        Assert.Contains("\\\" OR artist\\:\\*", lucene);
        Assert.Contains("\\&", lucene);
        Assert.Contains("\\[\\]", lucene);
        Assert.Contains("\\\\", lucene);
        Assert.Contains("&limit=25&offset=0", uri.AbsoluteUri);
    }

    [Theory]
    [InlineData(null, null, 25, 0)]
    [InlineData(" ", "", 25, 0)]
    [InlineData("artist", null, 0, 0)]
    [InlineData("artist", null, 101, 0)]
    [InlineData("artist", null, 25, -1)]
    [InlineData("a\0b", null, 25, 0)]
    public void InvalidQueriesFailBeforeNetwork(string? artist, string? title, int limit, int offset) =>
        Assert.ThrowsAny<ArgumentException>(() => MusicBrainzRecordingProvider.Normalize(new(artist, title, limit, offset)));

    [Fact]
    public void UnpairedSurrogatesAndLongFieldsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => MusicBrainzRecordingProvider.Normalize(new(Title: new string((char)0xD800, 1))));
        Assert.Throws<ArgumentException>(() => MusicBrainzRecordingProvider.Normalize(new(Title: new string('x', 513))));
    }

    [Fact]
    public void CandidateNormalizationPreservesVersionCreditsAndUnknownsWithoutTitleDedup()
    {
        var page = Page();
        Assert.Equal(2, page.Candidates.Length);
        Assert.Equal(page.Candidates[0].Title, page.Candidates[1].Title);
        var first = page.Candidates[0];
        Assert.Equal("Credited name feat. Second artist", first.ArtistCredit);
        Assert.Equal(123.456, first.DurationSeconds);
        Assert.Equal("studio recording", first.Disambiguation);
        Assert.False(first.Video);
        Assert.Equal("remastered edition", first.Releases[0].Disambiguation);
        Assert.Equal("2020-01", first.Releases[0].Date);
        Assert.Null(page.Candidates[1].DurationSeconds);
        Assert.Null(page.Candidates[1].Video);
        Assert.Equal("https://musicbrainz.org/recording/" + RecordingId, first.SourceUrl);
    }

    [Fact]
    public void PaginationAndEmptyResultsNeverAutoFetchOrSelect()
    {
        var body = Body.Replace("\"count\":2,\"offset\":0", "\"count\":20,\"offset\":10");
        Assert.True(RecordingJson.Parse(body, new(Title: "Own song", Offset: 10), DateTimeOffset.UtcNow).HasMore);
        var empty = RecordingJson.Parse("""{"count":0,"offset":25,"recordings":[]}""", new(Title: "None", Offset: 25), DateTimeOffset.UtcNow);
        Assert.Empty(empty.Candidates);
        Assert.False(empty.HasMore);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{")]
    [InlineData("""{"count":1,"offset":0,"recordings":null}""")]
    [InlineData("""{"count":-1,"offset":0,"recordings":[]}""")]
    [InlineData("""{"count":0,"offset":1,"recordings":[]}""")]
    [InlineData("""{"count":1,"offset":0,"recordings":[{"id":"invalid","title":"Own"}]}""")]
    [InlineData("""{"count":1,"offset":0,"recordings":[{"id":"00000000-0000-0000-0000-000000000000","title":"Own"}]}""")]
    [InlineData("""{"count":1,"offset":0,"recordings":[{"id":"11111111-1111-1111-1111-111111111111","title":"Own","length":-1}]}""")]
    [InlineData("""{"count":1,"offset":0,"recordings":[{"id":"11111111-1111-1111-1111-111111111111","title":"Own","video":"yes"}]}""")]
    [InlineData("""{"count":1,"count":2,"offset":0,"recordings":[]}""")]
    public void MalformedProviderDataHasStableError(string body) =>
        Assert.Equal(RecordingSearchError.InvalidResponse, Assert.Throws<RecordingSearchException>(() => RecordingJson.Parse(body, new(Title: "Own"), DateTimeOffset.UtcNow)).Error);

    [Fact]
    public void DuplicateIdsAndOverLimitPagesAreRejected()
    {
        var duplicated = Body.Replace("22222222-2222-2222-2222-222222222222", RecordingId);
        Assert.Equal(RecordingSearchError.InvalidResponse, Assert.Throws<RecordingSearchException>(() => RecordingJson.Parse(duplicated, new(Title: "Own"), DateTimeOffset.UtcNow)).Error);
        Assert.Throws<RecordingSearchException>(() => RecordingJson.Parse(Body, new(Title: "Own", Limit: 1), DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(429, RecordingSearchError.RateLimited)]
    [InlineData(503, RecordingSearchError.Unavailable)]
    [InlineData(403, RecordingSearchError.HttpFailure)]
    [InlineData(302, RecordingSearchError.HttpFailure)]
    public async Task StatusErrorsExposeRetryAfterWithoutHiddenRetries(int status, RecordingSearchError error)
    {
        var calls = 0;
        using var client = new HttpClient(new Handler((_, _) =>
        {
            calls++;
            var response = new HttpResponseMessage((HttpStatusCode)status);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(3));
            return Task.FromResult(response);
        }));
        using var provider = Provider(client);
        var exception = await Assert.ThrowsAsync<RecordingSearchException>(() => provider.SearchAsync(new(Title: "Own")));
        Assert.Equal(error, exception.Error);
        Assert.Equal(status, exception.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(3), exception.RetryAfter);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ContentLengthAndStreamedBodyLimitsAreBothEnforced(bool unknownLength)
    {
        using var client = new HttpClient(new Handler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = unknownLength ? new UncountedContent(new byte[4096]) : new ByteArrayContent(new byte[4096]) })));
        using var provider = Provider(client, limit: 1024);
        Assert.Equal(RecordingSearchError.ResponseTooLarge,
            (await Assert.ThrowsAsync<RecordingSearchException>(() => provider.SearchAsync(new(Title: "Own")))).Error);
    }

    [Fact]
    public async Task InvalidUtf8AndJsonDoNotBecomeEmptyResults()
    {
        foreach (var bytes in new[] { new byte[] { 0xFF }, Encoding.UTF8.GetBytes("not-json") })
        {
            using var client = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) })));
            using var provider = Provider(client);
            Assert.Equal(RecordingSearchError.InvalidResponse,
                (await Assert.ThrowsAsync<RecordingSearchException>(() => provider.SearchAsync(new(Title: "Own")))).Error);
        }
    }

    [Fact]
    public async Task NetworkFailureIsDistinctFromNoMatches()
    {
        using var client = new HttpClient(new Handler((_, _) => throw new HttpRequestException("Synthetic offline")));
        using var provider = Provider(client);
        Assert.Equal(RecordingSearchError.NetworkFailure,
            (await Assert.ThrowsAsync<RecordingSearchException>(() => provider.SearchAsync(new(Title: "Own")))).Error);
    }

    [Fact]
    public async Task TimeoutAndCallerCancellationAreDistinct()
    {
        using var client = new HttpClient(new Handler(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return Ok();
        }));
        using var provider = Provider(client, timeout: TimeSpan.FromMilliseconds(100));
        Assert.Equal(RecordingSearchError.TimedOut,
            (await Assert.ThrowsAsync<RecordingSearchException>(() => provider.SearchAsync(new(Title: "Own")))).Error);
        using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(25));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.SearchAsync(new(Title: "Own"), stop.Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeadlineAndCancellationAlsoBoundBodyReadAfterHeaders(bool callerCancellation)
    {
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StreamContent(new WaitingStream()) })));
        using var provider = Provider(client, timeout: TimeSpan.FromMilliseconds(100));
        using var stop = new CancellationTokenSource();
        if (callerCancellation)
        {
            stop.CancelAfter(TimeSpan.FromMilliseconds(25));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.SearchAsync(new(Title: "Own"), stop.Token));
        }
        else
            Assert.Equal(RecordingSearchError.TimedOut,
                (await Assert.ThrowsAsync<RecordingSearchException>(() => provider.SearchAsync(new(Title: "Own")))).Error);
    }

    [Fact]
    public async Task GateSpacesRequestsAndHonorsServerCooldownAcrossInstances()
    {
        var clock = new Clock();
        var delays = new List<TimeSpan>();
        var gate = new RecordingRequestGate(clock, (duration, token) =>
        {
            token.ThrowIfCancellationRequested();
            delays.Add(duration);
            clock.Advance(duration);
            return Task.CompletedTask;
        });
        var calls = 0;
        using var client = new HttpClient(new Handler((_, _) =>
        {
            calls++;
            if (calls == 1)
            {
                var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(clock.GetUtcNow().AddSeconds(3));
                return Task.FromResult(response);
            }
            return Task.FromResult(Ok());
        }));
        using var first = Provider(client, clock, gate);
        using var second = Provider(client, clock, gate);
        await Assert.ThrowsAsync<RecordingSearchException>(() => first.SearchAsync(new(Title: "Own")));
        await second.SearchAsync(new(Title: "Own"));
        await first.SearchAsync(new(Title: "Own"));
        Assert.Equal(new[] { TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1) }, delays);
    }

    [Fact]
    public async Task CancellingQueuedGateDoesNotConsumeRequestOrDeadlock()
    {
        var clock = new Clock();
        var gate = new RecordingRequestGate(clock, (duration, token) => { clock.Advance(duration); return Task.CompletedTask; });
        using (await gate.EnterAsync(default))
        {
            using var stop = new CancellationTokenSource();
            var queued = gate.EnterAsync(stop.Token);
            stop.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
        }
        using var next = await gate.EnterAsync(default);
    }

    [Fact]
    public async Task PublicProviderInstancesShareRealOneSecondThrottle()
    {
        var stamps = new List<long>();
        using var client = new HttpClient(new Handler((_, _) =>
        {
            stamps.Add(Stopwatch.GetTimestamp());
            return Task.FromResult(Ok());
        }));
        using var first = new MusicBrainzRecordingProvider(client);
        using var second = new MusicBrainzRecordingProvider(client);
        await Task.WhenAll(first.SearchAsync(new(Title: "Own")), second.SearchAsync(new(Title: "Own")));
        Assert.Equal(2, stamps.Count);
        Assert.True(Stopwatch.GetElapsedTime(stamps[0], stamps[1]) >= TimeSpan.FromMilliseconds(950));
    }

    [Fact]
    public async Task PrecancelAndDisposedProviderDoNotSend()
    {
        using var client = new HttpClient(new Handler((_, _) => throw new Exception("Must not send")));
        using var provider = Provider(client);
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.SearchAsync(new(Title: "Own"), stop.Token));
        provider.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => provider.SearchAsync(new(Title: "Own")));
    }
}
