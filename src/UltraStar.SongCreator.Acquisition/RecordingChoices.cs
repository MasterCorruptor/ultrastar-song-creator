using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Acquisition;

public sealed record RecordingSelection(RecordingCandidate Candidate, string? ReleaseId, DateTimeOffset RetrievedAtUtc);
public enum RecordingSelectionError { NotFound, Ambiguous, InvalidCandidate, UnsupportedSongSource, InvalidStoredChoice }
public sealed class RecordingSelectionException(RecordingSelectionError error, string message, Exception? inner = null) : Exception(message, inner)
{
    public RecordingSelectionError Error { get; } = error;
}

public sealed record BindRecordingChoice(RecordingSelection Selection) : ISongCommand
{
    public Song Apply(Song song) => RecordingChoices.Attach(song, Selection);
}

public static class RecordingChoices
{
    public const string HeaderName = "catalog.recording.choice.v1";
    private const int MaximumBytes = 2 * 1024 * 1024;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 32
    };

    public static RecordingSelection Select(RecordingPage page, string recordingId, string? releaseId = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (page.Candidates.IsDefault) throw new ArgumentException("Candidate collection is uninitialized.", nameof(page));
        var matches = page.Candidates.Where(c => c.Id == recordingId).ToArray();
        if (matches.Length == 0) throw new RecordingSelectionException(RecordingSelectionError.NotFound, "Choose an identity from this result page.");
        if (matches.Length != 1) throw new RecordingSelectionException(RecordingSelectionError.Ambiguous, "Recording identity is ambiguous.");
        var selected = new RecordingSelection(matches[0], releaseId, page.RetrievedAtUtc.ToUniversalTime());
        if (page.ProviderId != selected.Candidate.ProviderId)
            throw new RecordingSelectionException(RecordingSelectionError.InvalidCandidate, "Page/candidate provider mismatch.");
        Validate(selected);
        return selected;
    }

    /// <summary>Only provenance changes. Current metadata, media, notes and analysis stay untouched.</summary>
    public static Song Attach(Song song, RecordingSelection selection)
    {
        ArgumentNullException.ThrowIfNull(song);
        Validate(selection);
        var source = song.ImportedSource;
        if (source?.FormatId != LocalSongDraft.SourceFormatId)
            throw new RecordingSelectionException(RecordingSelectionError.UnsupportedSongSource, "Binding currently requires a local-media-v1 draft.");
        if (SongValidator.Validate(song).Any(i => i.Severity == ValidationSeverity.Error))
            throw new ArgumentException("Cannot bind recording provenance to a structurally invalid song.", nameof(song));
        var existing = source.Headers.Select((header, index) => (header, index)).Where(x => x.header.Name == HeaderName).ToArray();
        if (existing.Length > 1)
            throw new RecordingSelectionException(RecordingSelectionError.InvalidStoredChoice, "Duplicate recording choice headers are ambiguous.");
        var stored = StoredChoice.From(selection);
        var value = JsonSerializer.Serialize(stored, Options);
        if (Utf8.GetByteCount(value) > MaximumBytes)
            throw new RecordingSelectionException(RecordingSelectionError.InvalidCandidate, "Selected recording exceeds the storage limit.");
        if (existing.Length == 1 && existing[0].header.Value == value) return song;
        var header = new SourceHeader(HeaderName, value);
        var headers = existing.Length == 0 ? source.Headers.Add(header) : source.Headers.SetItem(existing[0].index, header);
        return song with { ImportedSource = source with { Headers = headers } };
    }

    public static RecordingSelection? Read(Song song)
    {
        ArgumentNullException.ThrowIfNull(song);
        if (song.ImportedSource is not { } source) return null;
        if (source.Headers.IsDefault)
            throw new RecordingSelectionException(RecordingSelectionError.InvalidStoredChoice, "Source headers are uninitialized.");
        var headers = source.Headers.Where(h => h.Name == HeaderName).ToArray();
        if (headers.Length == 0) return null;
        if (source.FormatId != LocalSongDraft.SourceFormatId || headers.Length != 1)
            throw new RecordingSelectionException(RecordingSelectionError.InvalidStoredChoice, "Source profile/choice header is ambiguous.");
        try
        {
            if (Utf8.GetByteCount(headers[0].Value) > MaximumBytes) throw new JsonException("Stored choice is too large.");
            using var json = JsonDocument.Parse(headers[0].Value, new JsonDocumentOptions { MaxDepth = 32 });
            RecordingJson.Unique(json.RootElement);
            var stored = JsonSerializer.Deserialize<StoredChoice>(headers[0].Value, Options) ?? throw new JsonException("Choice is null.");
            if (stored.Version != 1) throw new JsonException("Unsupported recording choice version.");
            var choice = stored.ToSelection();
            Validate(choice);
            return choice;
        }
        catch (Exception e) when (e is JsonException or EncoderFallbackException or RecordingSelectionException or ArgumentException)
        {
            throw new RecordingSelectionException(RecordingSelectionError.InvalidStoredChoice, "Stored recording choice is invalid; no network fallback is attempted.", e);
        }
    }

    private static void Validate(RecordingSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        var c = selection.Candidate;
        static bool Id(string? id) => Guid.TryParseExact(id, "D", out var parsed) && parsed != Guid.Empty && id == parsed.ToString("D");
        static bool Text(string? text, bool required = false) => text is not null && text.Length <= 4096 && (!required || !string.IsNullOrWhiteSpace(text));
        if (c is null || c.ProviderId != "musicbrainz" || !Id(c.Id) || !Text(c.Title, true) ||
            c.SourceUrl != "https://musicbrainz.org/recording/" + c.Id ||
            c.Artists.IsDefault || c.Releases.IsDefault || c.Artists.Length > 100 || c.Releases.Length > 250 ||
            c.Artists.Any(a => a is null || !Id(a.Id) || !Text(a.Name, true) || !Text(a.JoinPhrase)) ||
            c.Releases.Any(r => r is null || !Id(r.Id) || !Text(r.Title, true)) ||
            c.Releases.Select(r => r.Id).Distinct().Count() != c.Releases.Length ||
            (c.DurationSeconds is { } duration && (!double.IsFinite(duration) || duration < 0)) ||
            (selection.ReleaseId is not null && !c.Releases.Any(r => r.Id == selection.ReleaseId)))
            throw new RecordingSelectionException(RecordingSelectionError.InvalidCandidate, "Selected recording/release does not match the supported provider profile.");
    }

    // Frozen v1 DTOs deliberately avoid binding persistence to future public search model additions.
    private sealed record ArtistDto(string Id, string Name, string JoinPhrase);
    private sealed record ReleaseDto(string Id, string Title, string? Date, string? Country, string? Disambiguation);
    private sealed record StoredChoice(int Version, string ProviderId, string Id, string Title, ArtistDto[] Artists,
        double? DurationSeconds, string? Disambiguation, bool? Video, ReleaseDto[] Releases,
        string SourceUrl, string? ReleaseId, DateTimeOffset RetrievedAtUtc)
    {
        internal static StoredChoice From(RecordingSelection s) => new(1, s.Candidate.ProviderId, s.Candidate.Id,
            s.Candidate.Title, s.Candidate.Artists.Select(a => new ArtistDto(a.Id, a.Name, a.JoinPhrase)).ToArray(),
            s.Candidate.DurationSeconds, s.Candidate.Disambiguation, s.Candidate.Video,
            s.Candidate.Releases.Select(r => new ReleaseDto(r.Id, r.Title, r.Date, r.Country, r.Disambiguation)).ToArray(),
            s.Candidate.SourceUrl, s.ReleaseId, s.RetrievedAtUtc.ToUniversalTime());

        internal RecordingSelection ToSelection()
        {
            if (Artists is null || Releases is null || Artists.Any(a => a is null) || Releases.Any(r => r is null))
                throw new JsonException("Invalid stored credit/release collection.");
            return new(new(ProviderId, Id, Title, Artists.Select(a => new RecordingArtist(a.Id, a.Name, a.JoinPhrase)).ToImmutableArray(),
                DurationSeconds, Disambiguation, Video, Releases.Select(r => new RecordingRelease(r.Id, r.Title, r.Date, r.Country, r.Disambiguation)).ToImmutableArray(),
                SourceUrl), ReleaseId, RetrievedAtUtc.ToUniversalTime());
        }
    }
}
