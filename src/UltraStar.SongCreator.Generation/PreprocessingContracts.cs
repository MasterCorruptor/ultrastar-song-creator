using System.Collections.Immutable;
using UltraStar.SongCreator.Acquisition;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Generation;

public enum PreprocessingError
{
    InvalidIdentity, InvalidSong, InvalidMedia, InvalidStream, InvalidLyrics, InvalidPath,
    InvalidHash, InvalidProvenance, InvalidPcm, InvalidWaveform, LimitExceeded
}
public sealed class PreprocessingValidationException(PreprocessingError error)
    : ArgumentException("Invalid preprocessing data: " + error + ".")
{
    public PreprocessingError Error { get; } = error;
}
public enum PreprocessingTimeReference { DecodedPcmSeconds }
public sealed record PreprocessingProducer(string Name, string Revision);
public sealed record PcmDescription(string RelativePath, int SampleRate, int Channels, int BitsPerSample,
    long FrameCount, long FileBytes, string Sha256);
/// <summary>Exclusive end frame; values are normalized PCM amplitude bounds, never confidence.</summary>
public readonly record struct WaveformBin(long StartFrame, long EndFrame, double Minimum, double Maximum);

/// <summary>Validated in-memory claims. No file/stream/hash inspection or mutation occurs here.</summary>
public sealed class PreprocessingRequest
{
    public Guid JobId { get; }
    public string CallerId { get; }
    public Song OriginalSong { get; }
    public Guid SourceMediaId { get; }
    public MediaCandidate Candidate { get; }
    public int AudioStreamIndex { get; }
    public SelectedLyrics Lyrics { get; }
    public string? ExpectedSourceSha256 { get; }
    private PreprocessingRequest(Guid jobId, string callerId, Song originalSong, Guid sourceMediaId,
        MediaCandidate candidate, int audioStreamIndex, SelectedLyrics lyrics, string? expectedSourceSha256)
    {
        JobId = jobId; CallerId = callerId; OriginalSong = originalSong; SourceMediaId = sourceMediaId;
        Candidate = candidate; AudioStreamIndex = audioStreamIndex; Lyrics = lyrics;
        ExpectedSourceSha256 = expectedSourceSha256;
    }
    public static PreprocessingRequest Create(Guid jobId, string callerId, Song originalSong, Guid sourceMediaId,
        MediaCandidate candidate, int audioStreamIndex, SelectedLyrics lyrics, string? expectedSourceSha256 = null)
    {
        var result = new PreprocessingRequest(jobId, callerId, originalSong, sourceMediaId, candidate,
            audioStreamIndex, lyrics, expectedSourceSha256);
        PreprocessingValidation.Validate(result);
        return result;
    }
}

/// <summary>Transient proposal in decoded-PCM time. It neither maps clocks nor edits/persists a song.</summary>
public sealed class PreprocessingProposal
{
    public PreprocessingRequest Request { get; }
    public Guid JobId => Request.JobId;
    public Guid SourceMediaId => Request.SourceMediaId;
    public int AudioStreamIndex => Request.AudioStreamIndex;
    public string SourceSha256 { get; }
    public PreprocessingProducer Producer { get; }
    public PcmDescription Pcm { get; }
    public ImmutableArray<WaveformBin> Bins { get; }
    public PreprocessingTimeReference TimeReference => PreprocessingTimeReference.DecodedPcmSeconds;
    public double DurationSeconds => (double)Pcm.FrameCount / Pcm.SampleRate;
    /// <summary>Unverified caller claim; null means unknown. No offset/alignment is inferred.</summary>
    public double? OriginInMediaSeconds { get; }
    private PreprocessingProposal(PreprocessingRequest request, string sourceSha256, PreprocessingProducer producer,
        PcmDescription pcm, ImmutableArray<WaveformBin> bins, double? originInMediaSeconds)
    {
        Request = request; SourceSha256 = sourceSha256; Producer = producer; Pcm = pcm; Bins = bins;
        OriginInMediaSeconds = originInMediaSeconds;
    }
    public static PreprocessingProposal Create(PreprocessingRequest request, string sourceSha256,
        PreprocessingProducer producer, PcmDescription pcm, ImmutableArray<WaveformBin> bins,
        double? originInMediaSeconds = null)
    {
        var result = new PreprocessingProposal(request, sourceSha256, producer, pcm, bins, originInMediaSeconds);
        PreprocessingValidation.Validate(result);
        return result;
    }
}
