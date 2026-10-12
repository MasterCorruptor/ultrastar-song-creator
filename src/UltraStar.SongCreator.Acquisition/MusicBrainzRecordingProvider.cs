using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace UltraStar.SongCreator.Acquisition;

public sealed class MusicBrainzRecordingProvider : IRecordingSearchProvider, IDisposable
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly HttpClient client;
    private readonly bool ownsClient;
    private readonly RecordingRequestGate gate;
    private readonly TimeProvider clock;
    private readonly TimeSpan timeout;
    private readonly int maximumResponseBytes;
    private bool disposed;
    public string ProviderId => "musicbrainz";

    public MusicBrainzRecordingProvider(HttpClient? client = null, TimeSpan? timeout = null, int maximumResponseBytes = 2 * 1024 * 1024)
        : this(client ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }),
            RecordingRequestGate.Shared, TimeProvider.System, timeout ?? TimeSpan.FromSeconds(30), maximumResponseBytes)
    {
        ownsClient = client is null;
    }

    internal MusicBrainzRecordingProvider(HttpClient client, RecordingRequestGate gate, TimeProvider clock,
        TimeSpan timeout, int maximumResponseBytes)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.gate = gate;
        this.clock = clock;
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes(5)) throw new ArgumentOutOfRangeException(nameof(timeout));
        if (maximumResponseBytes is <= 0 or > 16 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(maximumResponseBytes));
        this.timeout = timeout;
        this.maximumResponseBytes = maximumResponseBytes;
    }

    public async Task<RecordingPage> SearchAsync(RecordingQuery query, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = Normalize(query);
        using var permission = await gate.EnterAsync(cancellationToken);
        using var deadline = new CancellationTokenSource(timeout);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, QueryUri(normalized));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd("UltraStarSongCreator/0.1.0 (+https://github.com/MasterCorruptor/ultrastar-song-creator)");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stop.Token);
            if (!response.IsSuccessStatusCode)
            {
                var retry = response.Headers.RetryAfter?.Delta ??
                    (response.Headers.RetryAfter?.Date is { } date ? date - clock.GetUtcNow() : (TimeSpan?)null);
                if (retry < TimeSpan.Zero) retry = TimeSpan.Zero;
                var status = (int)response.StatusCode;
                var error = response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => RecordingSearchError.RateLimited,
                    HttpStatusCode.ServiceUnavailable => RecordingSearchError.Unavailable,
                    _ => RecordingSearchError.HttpFailure
                };
                if (error is RecordingSearchError.RateLimited or RecordingSearchError.Unavailable)
                    gate.CoolDown(retry ?? TimeSpan.FromSeconds(1));
                throw new RecordingSearchException(error, $"MusicBrainz returned HTTP {status}; no candidate is selected.", status, retry);
            }
            if (response.Content.Headers.ContentLength > maximumResponseBytes)
                throw new RecordingSearchException(RecordingSearchError.ResponseTooLarge, "Recording response exceeds the configured byte limit.");
            await using var stream = await response.Content.ReadAsStreamAsync(stop.Token);
            using var body = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(buffer, stop.Token)) != 0)
            {
                if (body.Length + count > maximumResponseBytes)
                    throw new RecordingSearchException(RecordingSearchError.ResponseTooLarge, "Recording response exceeds the configured byte limit.");
                body.Write(buffer, 0, count);
            }
            stop.Token.ThrowIfCancellationRequested();
            var page = RecordingJson.Parse(Utf8.GetString(body.ToArray()), normalized, clock.GetUtcNow());
            stop.Token.ThrowIfCancellationRequested();
            return page;
        }
        catch (OperationCanceledException e)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new RecordingSearchException(RecordingSearchError.TimedOut, "Recording request timed out.", inner: e);
        }
        catch (Exception e) when (e is HttpRequestException or IOException)
        {
            throw new RecordingSearchException(RecordingSearchError.NetworkFailure, "Recording service could not be reached/read.", inner: e);
        }
        catch (DecoderFallbackException e)
        {
            throw new RecordingSearchException(RecordingSearchError.InvalidResponse, "Recording response is not valid UTF-8.", inner: e);
        }
    }

    internal static RecordingQuery Normalize(RecordingQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        string? Field(string? value)
        {
            if (value is null) return null;
            value = value.Trim();
            if (value.Length == 0) return null;
            if (value.Length > 512 || value.Any(char.IsControl)) throw new ArgumentException("Search fields must be at most 512 characters without controls.", nameof(query));
            try { Utf8.GetByteCount(value); }
            catch (EncoderFallbackException e) { throw new ArgumentException("Search field contains invalid Unicode.", nameof(query), e); }
            return value;
        }
        var normalized = query with { Artist = Field(query.Artist), Title = Field(query.Title) };
        if (normalized.Artist is null && normalized.Title is null) throw new ArgumentException("Provide artist, title or both.", nameof(query));
        if (query.Limit is < 1 or > 100 || query.Offset < 0) throw new ArgumentOutOfRangeException(nameof(query));
        return normalized;
    }

    internal static Uri QueryUri(RecordingQuery query)
    {
        // Escape Lucene syntax inside quoted literals first, then encode the whole URL parameter.
        static string Literal(string value) => "\"" + new string(value.SelectMany(c =>
            "+-&|!(){}[]^\"~*?:\\/".Contains(c) ? new[] { '\\', c } : new[] { c }).ToArray()) + "\"";
        var clauses = new List<string>();
        if (query.Artist is { } artist) clauses.Add("artist:" + Literal(artist));
        if (query.Title is { } title) clauses.Add("recording:" + Literal(title));
        return new("https://musicbrainz.org/ws/2/recording?fmt=json&query=" + Uri.EscapeDataString(string.Join(" AND ", clauses)) +
            "&limit=" + query.Limit.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            "&offset=" + query.Offset.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (ownsClient) client.Dispose();
    }
}
