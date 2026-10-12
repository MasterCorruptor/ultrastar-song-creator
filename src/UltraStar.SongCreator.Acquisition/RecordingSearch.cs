using System.Collections.Immutable;

namespace UltraStar.SongCreator.Acquisition;

public sealed record RecordingQuery(string? Artist = null, string? Title = null, int Limit = 25, int Offset = 0);
public sealed record RecordingArtist(string Id, string Name, string JoinPhrase);
public sealed record RecordingRelease(string Id, string Title, string? Date, string? Country, string? Disambiguation);
public sealed record RecordingCandidate(
    string ProviderId, string Id, string Title, ImmutableArray<RecordingArtist> Artists,
    double? DurationSeconds, string? Disambiguation, bool? Video,
    ImmutableArray<RecordingRelease> Releases, string SourceUrl)
{
    public string ArtistCredit => string.Concat(Artists.Select(a => a.Name + a.JoinPhrase));
}

public sealed record RecordingPage(
    string ProviderId, RecordingQuery Query, int TotalCount, int Offset,
    ImmutableArray<RecordingCandidate> Candidates, DateTimeOffset RetrievedAtUtc)
{
    public bool HasMore => Candidates.Length > 0 && (long)Offset + Candidates.Length < TotalCount;
}

public interface IRecordingSearchProvider
{
    string ProviderId { get; }
    Task<RecordingPage> SearchAsync(RecordingQuery query, CancellationToken cancellationToken = default);
}

public enum RecordingSearchError { NetworkFailure, TimedOut, HttpFailure, RateLimited, Unavailable, ResponseTooLarge, InvalidResponse }
public sealed class RecordingSearchException(
    RecordingSearchError error, string message, int? statusCode = null, TimeSpan? retryAfter = null, Exception? inner = null)
    : Exception(message, inner)
{
    public RecordingSearchError Error { get; } = error;
    public int? StatusCode { get; } = statusCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
