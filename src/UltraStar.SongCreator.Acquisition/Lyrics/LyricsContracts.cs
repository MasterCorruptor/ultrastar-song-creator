using System.Collections.Immutable;

namespace UltraStar.SongCreator.Acquisition.Lyrics;

public enum LyricsSyncLevel { Plain, Line, Word }
public enum LyricsTextStatus { Available, Instrumental, Missing, Unsupported }
public enum LyricsTimeReference { LyricsSourceSeconds }
public enum LyricsMatchIndication { Unknown, ExactNormalized, Different }
public sealed record LyricsQuery(string Artist, string Title, double? DurationSeconds = null,
    string? VersionHint = null, string? RecordingId = null, int Limit = 25);
public sealed record LyricsWord(string Text, double StartSeconds, double? EndSeconds = null);
public sealed record LyricsLine(string Text, double StartSeconds, double? EndSeconds,
    ImmutableArray<LyricsWord> Words);
public sealed record LyricsDocument(string Id, string Text, LyricsSyncLevel SyncLevel,
    ImmutableArray<LyricsLine> Lines, LyricsTimeReference TimeReference = LyricsTimeReference.LyricsSourceSeconds);
public sealed record LyricsCapabilities(bool? ReportedWordSync = null, bool? ReportedLineSync = null)
{
    public ImmutableArray<string> UnsupportedFormats { get; init; } = [];
}
public sealed record LyricsMatchEvidence(
    LyricsMatchIndication Artist = LyricsMatchIndication.Unknown,
    LyricsMatchIndication Title = LyricsMatchIndication.Unknown,
    LyricsMatchIndication Version = LyricsMatchIndication.Unknown,
    LyricsMatchIndication Text = LyricsMatchIndication.Unknown,
    double? DurationDeltaSeconds = null);
public sealed record LyricsCandidate
{
    public required string ProviderId { get; init; }
    public required string Id { get; init; }
    public string? Artist { get; init; }
    public string? Title { get; init; }
    public string? Album { get; init; }
    public string? Version { get; init; }
    public double? DurationSeconds { get; init; }
    public string? SourceUrl { get; init; }
    public DateTimeOffset RetrievedAtUtc { get; init; }
    public LyricsTextStatus TextStatus { get; init; }
    public ImmutableArray<LyricsDocument> Documents { get; init; } = [];
    public LyricsCapabilities Capabilities { get; init; } = new();
    public LyricsMatchEvidence Match { get; init; } = new();
    public ImmutableArray<LyricsVariantDiagnostic> VariantDiagnostics { get; init; } = [];
}
public sealed record LyricsPage(string ProviderId, LyricsQuery Query,
    ImmutableArray<LyricsCandidate> Candidates, DateTimeOffset RetrievedAtUtc);
/// <summary>Frozen immutable selection. Null document is reserved for explicit instrumental choice.</summary>
public sealed record LyricsSelection(LyricsCandidate Candidate, LyricsDocument? Document);
public interface ILyricsSearchProvider
{
    string ProviderId { get; }
    Task<LyricsPage> SearchAsync(LyricsQuery query, CancellationToken cancellationToken = default);
}
public enum LyricsSearchError { NetworkFailure, TimedOut, HttpFailure, RateLimited, Unavailable, ResponseTooLarge, InvalidResponse }
public sealed class LyricsSearchException(LyricsSearchError error, string message,
    int? statusCode = null, TimeSpan? retryAfter = null, Exception? inner = null) : Exception(message, inner)
{
    public LyricsSearchError Error { get; } = error;
    public int? StatusCode { get; } = statusCode;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
public enum LyricsValidationError { InvalidQuery, InvalidPage, InvalidCandidate, InvalidDocument, ResponseTooLarge, InvalidSelection, NotFound, Ambiguous }
public sealed class LyricsValidationException(LyricsValidationError error, string message) : ArgumentException(message)
{
    public LyricsValidationError Error { get; } = error;
}
