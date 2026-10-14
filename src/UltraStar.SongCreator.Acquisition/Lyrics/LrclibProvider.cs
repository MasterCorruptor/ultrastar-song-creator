using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace UltraStar.SongCreator.Acquisition.Lyrics;

/// <summary>One bounded anonymous LRCLIB search; no retries, redirects, keys or payload logging.</summary>
public sealed class LrclibProvider : ILyricsSearchProvider, IDisposable
{
    private const int MaximumHeaderBytes = 64 * 1024;
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly HttpClient client;
    private readonly HttpClientHandler? ownedHandler;
    private readonly LrclibRequestGate gate;
    private readonly TimeProvider clock;
    private bool disposed;
    public string ProviderId => "lrclib";

    public LrclibProvider()
    {
        ownedHandler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseDefaultCredentials = false,
            Credentials = null,
            MaxResponseHeadersLength = 64
        };
        client = new HttpClient(ownedHandler) { Timeout = Timeout.InfiniteTimeSpan };
        gate = LrclibRequestGate.Shared;
        clock = TimeProvider.System;
    }

    // Borrowed terminal fixtures only. The fixture must not perform redirects or additional requests.
    internal LrclibProvider(HttpClient client, LrclibRequestGate gate, TimeProvider clock)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        if (client.Timeout != Timeout.InfiniteTimeSpan)
            throw new ArgumentException("Fixture client must use the single provider deadline.", nameof(client));
    }

    public async Task<LyricsPage> SearchAsync(LyricsQuery query, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        LyricsValidation.Query(query);
        using var deadline = new CancellationTokenSource(Deadline, clock);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        void CheckStop()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (deadline.IsCancellationRequested) throw Error(LyricsSearchError.TimedOut);
        }
        try
        {
            using var permission = await gate.EnterAsync(stop.Token).ConfigureAwait(false);
            stop.Token.ThrowIfCancellationRequested();
            using var request = new HttpRequestMessage(HttpMethod.Get,
                "https://lrclib.net/api/search?track_name=" + Uri.EscapeDataString(query.Title)
                + "&artist_name=" + Uri.EscapeDataString(query.Artist));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd("UltraStarSongCreator/0.0-prototype (+https://github.com/MasterCorruptor/ultrastar-song-creator)");
            permission.MarkRequestStarted();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stop.Token).ConfigureAwait(false);
            stop.Token.ThrowIfCancellationRequested();
            var receipt = clock.GetUtcNow();
            ValidateHeaderBudget(response);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var status = (int)response.StatusCode;
                var error = status switch
                {
                    429 => LyricsSearchError.RateLimited,
                    503 => LyricsSearchError.Unavailable,
                    _ => LyricsSearchError.HttpFailure
                };
                TimeSpan? retry = null;
                var extreme = false;
                if (status is 429 or 503)
                {
                    (retry, extreme) = RetryAfter(response, receipt);
                    if (extreme) permission.Disable();
                    else permission.ExtendCooldown(retry ?? TimeSpan.FromSeconds(1));
                }
                throw new LyricsSearchException(error, extreme
                    ? "LRCLIB requested an unsupported wait; this process will not send further LRCLIB requests."
                    : "LRCLIB returned an unsuccessful HTTP status.", status, retry);
            }
            ValidateContentHeaders(response.Content);
            if (response.Content.Headers.ContentLength > LrclibJson.MaxInputBytes)
                throw Error(LyricsSearchError.ResponseTooLarge);
            await using var stream = await response.Content.ReadAsStreamAsync(stop.Token).ConfigureAwait(false);
            using var body = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(buffer, stop.Token).ConfigureAwait(false)) != 0)
            {
                if (body.Length + count > LrclibJson.MaxInputBytes) throw Error(LyricsSearchError.ResponseTooLarge);
                body.Write(buffer, 0, count);
            }
            stop.Token.ThrowIfCancellationRequested();
            var page = LrclibJson.Parse(body.GetBuffer().AsSpan(0, checked((int)body.Length)), query, receipt, stop.Token);
            stop.Token.ThrowIfCancellationRequested();
            return page;
        }
        catch (OperationCanceledException) { CheckStop(); throw Error(LyricsSearchError.NetworkFailure); }
        catch (Exception e) when (e is HttpRequestException or IOException) { CheckStop(); throw Error(LyricsSearchError.NetworkFailure); }
        catch (LyricsSearchException) { CheckStop(); throw; }
        catch (Exception e) when (e is FormatException or EncoderFallbackException)
        { CheckStop(); throw Error(LyricsSearchError.InvalidResponse); }
    }

    private static LyricsSearchException Error(LyricsSearchError error) =>
        new(error, error switch
        {
            LyricsSearchError.TimedOut => "LRCLIB operation exceeded its overall deadline.",
            LyricsSearchError.NetworkFailure => "LRCLIB transport could not complete.",
            LyricsSearchError.ResponseTooLarge => "LRCLIB response exceeds a supported resource bound.",
            _ => "LRCLIB response does not match the supported transport profile."
        });

    private static void ValidateHeaderBudget(HttpResponseMessage response)
    {
        // Also enforce the production header bound on borrowed terminal fixtures.
        long size = 64;
        foreach (var header in response.Headers.NonValidated.Concat(response.Content.Headers.NonValidated))
        {
            size += Utf8.GetByteCount(header.Key) + 4;
            foreach (var value in header.Value) size += Utf8.GetByteCount(value) + 2;
            if (size > MaximumHeaderBytes) throw Error(LyricsSearchError.NetworkFailure);
        }
    }

    private static void ValidateContentHeaders(HttpContent content)
    {
        if (content.Headers.NonValidated.TryGetValues("Content-Type", out var types))
        {
            var values = types.ToArray();
            if (values.Length != 1 || !MediaTypeHeaderValue.TryParse(values[0], out var type)
                || !string.Equals(type.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
                throw Error(LyricsSearchError.InvalidResponse);
        }
        if (content.Headers.NonValidated.TryGetValues("Content-Encoding", out var encodings))
            foreach (var value in encodings)
                foreach (var coding in value.Split(','))
                    if (!string.Equals(coding.Trim(' ', '\t'), "identity", StringComparison.OrdinalIgnoreCase))
                        throw Error(LyricsSearchError.InvalidResponse);
        if (content.Headers.NonValidated.TryGetValues("Content-Length", out var lengths))
        {
            var values = lengths.ToArray();
            if (values.Length != 1 || !long.TryParse(values[0].Trim(' ', '\t'), NumberStyles.None, CultureInfo.InvariantCulture, out var length) || length < 0)
                throw Error(LyricsSearchError.InvalidResponse);
        }
    }

    private static (TimeSpan? Delay, bool Extreme) RetryAfter(HttpResponseMessage response, DateTimeOffset receipt)
    {
        if (!response.Headers.NonValidated.TryGetValues("Retry-After", out var headers)) return (null, false);
        var values = headers.ToArray();
        if (values.Length != 1) return (null, false);
        var value = values[0].AsSpan().Trim(" \t");
        var digits = !value.IsEmpty;
        foreach (var c in value) if (c is < '0' or > '9') { digits = false; break; }
        if (digits)
        {
            var significant = value.TrimStart('0');
            const string maximum = "922337203685";
            if (significant.Length > maximum.Length || (significant.Length == maximum.Length && significant.SequenceCompareTo(maximum) > 0))
                return (null, true);
            long seconds = 0;
            foreach (var digit in significant) seconds = checked(seconds * 10 + digit - '0');
            return (TimeSpan.FromTicks(checked(seconds * TimeSpan.TicksPerSecond)), false);
        }
        if (!RetryConditionHeaderValue.TryParse(value.ToString(), out var parsed) || parsed.Date is not { } date)
            return (null, false);
        return (date <= receipt ? TimeSpan.Zero : date - receipt, false);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (ownedHandler is not null) client.Dispose();
    }
}
