using System.Collections.Immutable;
using UltraStar.SongCreator.Acquisition;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Generation.Tests;

public sealed class PreprocessingContractTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 15, 1, 0, 0, TimeSpan.Zero);
    private static readonly string SourceHash = new('a', 64), PcmHash = new('b', 64);
    private static readonly Guid Job = Guid.Parse("b363fbd4-2133-4c56-bafd-0ea344fcd6b5");
    private sealed record Input(Song Song, MediaCandidate Candidate, SelectedLyrics Lyrics, Guid MediaId);
    private static SelectedLyrics Text() => new(SelectedLyricsKind.Text,
        new("line", " Egen tekst \n", SelectedLyricsSyncLevel.Line,
            [new(" Egen tekst ", 12.5, null, [])]),
        new(SelectedLyricsOrigin.Provider, "fixture", "explicit", "https://example.invalid/own", null, At));
    private static Input Fixture()
    {
        var path = Path.GetFullPath(Path.Combine("g1a-memory-only", "own [x] $% clip.wav"));
        var candidate = new MediaCandidate("local-file", path, 128,
            new("wav", 3, [
                new(2, "audio", "pcm_s16le", false, 48000, 2, null, null, 3, []),
                new(5, "audio", "pcm_s16le", false, 16000, 1, null, null, 3, []),
                new(9, "video", "own", false, null, null, 8, 8, 3, [])
            ], [new("comment", "Own\nmetadata")]));
        var song = LocalSongDraft.Create(candidate, audioStreamIndex: 5).Song;
        var lyrics = Text();
        song = song with
        {
            SelectedLyrics = lyrics,
            AudioOffsetSeconds = 3.25,
            VideoOffsetSeconds = -2,
            Phrases = [new() { StartSeconds = 0, EndSeconds = 8, Notes = [new() { StartSeconds = 1, DurationSeconds = 1, Text = "own" }] }]
        };
        return new(song, candidate, lyrics, song.Media.Single(m => m.Kind == MediaKind.Audio).Id);
    }
    private static PreprocessingRequest Request(Input f, string? expected = null) =>
        PreprocessingRequest.Create(Job, "own-caller", f.Song, f.MediaId, f.Candidate, 5, f.Lyrics, expected);
    private static PcmDescription Pcm(long frames = 16001) => new("analysis.wav", 16000, 1, 16, frames, 44 + frames * 2, PcmHash);
    private static PreprocessingProposal Proposal(PreprocessingRequest r, PcmDescription? pcm = null,
        ImmutableArray<WaveformBin>? bins = null, double? origin = null) =>
        PreprocessingProposal.Create(r, SourceHash, new("own-producer", "reviewed-revision"), pcm ?? Pcm(),
            bins ?? [new(0, 16000, -.5, .25), new(16000, 16001, 0, 0)], origin);
    private static void Error(PreprocessingError error, Action action)
    {
        var exception = Assert.Throws<PreprocessingValidationException>(action);
        Assert.Equal(error, exception.Error);
        Assert.Equal("Invalid preprocessing data: " + error + ".", exception.Message);
    }

    [Fact]
    public void LocalSelectionUsesRetainedStreamAndPreservesExactSnapshotLyricsAndCandidate()
    {
        var f = Fixture(); var request = Request(f);
        Assert.Same(f.Song, request.OriginalSong); Assert.Same(f.Candidate, request.Candidate); Assert.Same(f.Lyrics, request.Lyrics);
        Assert.Equal(5, request.AudioStreamIndex); Assert.Equal(Job, request.JobId);
        Assert.Equal(f.Song.ImportedSource, request.OriginalSong.ImportedSource);
        Assert.Equal("Own\nmetadata", request.Candidate.Inspection.Tags[0].Value);
        // A path/size assertion is deliberately not a file existence or content assertion.
        Assert.EndsWith("own [x] $% clip.wav", request.Candidate.FilePath);
        PreprocessingValidation.Validate(request);
    }

    [Fact]
    public void ActualDownloadedDraftAcceptsLocalCandidateLabelAndRetainsKnownHashClaim()
    {
        var f = Fixture();
        var format = new MediaDownloadFormat("140", "m4a", "aac", "none", "https", 128, 64, null, null, false);
        var online = new OnlineMediaCandidate("youtube", "abcdefghijk", "Own title", "Own source", 3, false,
            "https://www.youtube.com/watch?v=abcdefghijk");
        var selected = MediaDownloadChoices.Select(new(online, [format], At), "140");
        var candidate = f.Candidate with
        {
            FilePath = Path.GetFullPath(Path.Combine("g1a-memory-only", "audio.m4a")),
            Inspection = new("mov", 3, [new(5, "audio", "aac", false, 48000, 2, null, null, 3, [])], [])
        };
        var downloaded = new DownloadedMedia(selected, candidate, null, SourceHash, null, At,
            Path.GetDirectoryName(candidate.FilePath)!);
        var draft = DownloadedMediaDraft.Create(downloaded).Song with { SelectedLyrics = f.Lyrics };
        var stored = DownloadedMediaDraft.Read(draft)!;
        var request = PreprocessingRequest.Create(Job, "own-caller", draft, draft.Media.Single().Id, candidate, 5,
            f.Lyrics, stored.Audio.Sha256);
        Assert.Equal("youtube", draft.Media[0].Source);
        Assert.Equal("local-file", request.Candidate.SourceId);
        Assert.Same(candidate, request.Candidate); Assert.Equal(stored.Audio.Sha256, request.ExpectedSourceSha256);
        var result = Proposal(request);
        Assert.Same(request, result.Request); Assert.Equal(request.SourceMediaId, result.SourceMediaId);
    }

    [Fact]
    public void CanonicalNativePathAliasesMatchWithoutRewritingCapturedObjects()
    {
        var f = Fixture(); var path = f.Candidate.FilePath;
        var lexical = Path.Combine(Path.GetDirectoryName(path)!, ".", Path.GetFileName(path));
        var reference = f.Song.Media.Single(m => m.Id == f.MediaId) with { Location = lexical };
        var song = f.Song with { Media = f.Song.Media.Select(m => m.Id == f.MediaId ? reference : m).ToImmutableArray() };
        var request = Request(f with { Song = song });
        Assert.Same(reference, request.OriginalSong.Media.Single(m => m.Id == f.MediaId));
        Assert.Equal(lexical, reference.Location);
        var changedCase = f.Candidate with { FilePath = path.ToUpperInvariant() };
        if (OperatingSystem.IsWindows()) Assert.Same(changedCase, Request(f with { Candidate = changedCase }).Candidate);
        else Error(PreprocessingError.InvalidPath, () => Request(f with { Candidate = changedCase }));
    }

    [Theory]
    [InlineData("manual")]
    [InlineData("instrumental")]
    [InlineData("equal-copy")]
    public void ExplicitValidatedLyricsAreRetainedWithoutNewIdentityOrTime(string kind)
    {
        var f = Fixture();
        var lyrics = kind switch
        {
            "manual" => new SelectedLyrics(SelectedLyricsKind.Text, new("manual", " Egen tekst\n", SelectedLyricsSyncLevel.Plain, []),
                new(SelectedLyricsOrigin.Manual, "manual", Job.ToString("D"), null, At, null)),
            "instrumental" => f.Lyrics with { Kind = SelectedLyricsKind.Instrumental, Document = null },
            _ => f.Lyrics with { Document = f.Lyrics.Document! with { Lines = f.Lyrics.Document.Lines.ToImmutableArray() } }
        };
        var song = f.Song with { SelectedLyrics = kind == "equal-copy" ? f.Lyrics : lyrics };
        var request = Request(f with { Song = song, Lyrics = lyrics });
        Assert.Same(lyrics, request.Lyrics);
        Assert.True(SelectedLyricsEquality.AreEqual(song.SelectedLyrics, request.Lyrics));
        Assert.Equal(lyrics.Provenance, request.Lyrics.Provenance);
    }

    [Theory]
    [InlineData("null-song", PreprocessingError.InvalidSong)]
    [InlineData("empty-song-id", PreprocessingError.InvalidSong)]
    [InlineData("nonfinite-offset", PreprocessingError.InvalidSong)]
    [InlineData("null-candidate", PreprocessingError.InvalidMedia)]
    [InlineData("missing-id", PreprocessingError.InvalidMedia)]
    [InlineData("duplicate-id", PreprocessingError.InvalidMedia)]
    [InlineData("default-media", PreprocessingError.InvalidMedia)]
    [InlineData("null-media", PreprocessingError.InvalidMedia)]
    [InlineData("video-reference", PreprocessingError.InvalidMedia)]
    [InlineData("zero-bytes", PreprocessingError.InvalidMedia)]
    [InlineData("oversize", PreprocessingError.LimitExceeded)]
    [InlineData("null-inspection", PreprocessingError.InvalidMedia)]
    [InlineData("bad-duration", PreprocessingError.InvalidMedia)]
    [InlineData("default-tags", PreprocessingError.InvalidMedia)]
    [InlineData("null-tag", PreprocessingError.InvalidMedia)]
    [InlineData("null-lyrics", PreprocessingError.InvalidLyrics)]
    [InlineData("missing-current-lyrics", PreprocessingError.InvalidLyrics)]
    [InlineData("different-lyrics", PreprocessingError.InvalidLyrics)]
    [InlineData("malformed-lyrics", PreprocessingError.InvalidLyrics)]
    public void InconsistentRequestDataFailsWithTypedStaticError(string change, PreprocessingError error)
    {
        var f = Fixture();
        f = change switch
        {
            "null-song" => f with { Song = null! },
            "empty-song-id" => f with { Song = f.Song with { Id = Guid.Empty } },
            "nonfinite-offset" => f with { Song = f.Song with { AudioOffsetSeconds = double.NaN } },
            "null-candidate" => f with { Candidate = null! },
            "missing-id" => f with { MediaId = Guid.NewGuid() },
            "duplicate-id" => f with { Song = f.Song with { Media = f.Song.Media.Add(f.Song.Media[0]) } },
            "default-media" => f with { Song = f.Song with { Media = default } },
            "null-media" => f with { Song = f.Song with { Media = [null!] } },
            "video-reference" => f with { MediaId = f.Song.Media.Single(m => m.Kind == MediaKind.Video).Id },
            "zero-bytes" => f with { Candidate = f.Candidate with { ByteLength = 0 } },
            "oversize" => f with { Candidate = f.Candidate with { ByteLength = PreprocessingValidation.MaxSourceBytes + 1 } },
            "null-inspection" => f with { Candidate = f.Candidate with { Inspection = null! } },
            "bad-duration" => f with { Candidate = f.Candidate with { Inspection = f.Candidate.Inspection with { DurationSeconds = double.PositiveInfinity } } },
            "default-tags" => f with { Candidate = f.Candidate with { Inspection = f.Candidate.Inspection with { Tags = default } } },
            "null-tag" => f with { Candidate = f.Candidate with { Inspection = f.Candidate.Inspection with { Tags = [null!] } } },
            "null-lyrics" => f with { Lyrics = null! },
            "missing-current-lyrics" => f with { Song = f.Song with { SelectedLyrics = null } },
            "different-lyrics" => f with { Lyrics = f.Lyrics with { Document = f.Lyrics.Document! with { Text = "Different" } } },
            _ => f with { Lyrics = f.Lyrics with { Document = f.Lyrics.Document! with { Lines = default } } }
        };
        Error(error, () => Request(f));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("non-audio")]
    [InlineData("picture")]
    [InlineData("duplicate")]
    [InlineData("default")]
    [InlineData("null")]
    [InlineData("rate")]
    [InlineData("retained-mismatch")]
    [InlineData("retained-duplicate")]
    [InlineData("retained-missing")]
    public void ExplicitStreamMustBeActualUniqueAudioAndMatchRetainedChoice(string change)
    {
        var f = Fixture(); var streams = f.Candidate.Inspection.Streams;
        if (change == "retained-mismatch")
            f = f with
            {
                Song = f.Song with
                {
                    ImportedSource = f.Song.ImportedSource! with
                    { Headers = f.Song.ImportedSource.Headers.Select(h => h.Name == "selection.audioStream" ? h with { Value = "2" } : h).ToImmutableArray() }
                }
            };
        else if (change == "retained-duplicate")
            f = f with
            {
                Song = f.Song with
                {
                    ImportedSource = f.Song.ImportedSource! with
                    { Headers = f.Song.ImportedSource.Headers.Add(new("selection.audioStream", "5")) }
                }
            };
        else if (change == "retained-missing")
            f = f with
            {
                Song = f.Song with
                {
                    ImportedSource = f.Song.ImportedSource! with
                    { Headers = f.Song.ImportedSource.Headers.Where(h => h.Name != "selection.audioStream").ToImmutableArray() }
                }
            };
        else
        {
            streams = change switch
            {
                "unknown" => streams.Where(s => s.Index != 5).ToImmutableArray(),
                "non-audio" => streams.SetItem(1, streams[1] with { Kind = "video" }),
                "picture" => streams.SetItem(1, streams[1] with { AttachedPicture = true }),
                "duplicate" => streams.Add(streams[1]),
                "default" => default,
                "null" => [null!],
                _ => streams.SetItem(1, streams[1] with { SampleRate = 0 })
            };
            f = f with { Candidate = f.Candidate with { Inspection = f.Candidate.Inspection with { Streams = streams } } };
        }
        Error(PreprocessingError.InvalidStream, () => Request(f));
    }

    [Theory]
    [InlineData("relative.wav")]
    [InlineData("https://example.invalid/clip.wav")]
    [InlineData("file:///own/clip.wav")]
    [InlineData("empty")]
    [InlineData("nul")]
    [InlineData("mismatch")]
    [InlineData("retained-path")]
    public void LexicallyInvalidOrInconsistentPathsFailWithoutOpeningFiles(string value)
    {
        var f = Fixture();
        if (value == "retained-path")
            f = f with
            {
                Song = f.Song with
                {
                    ImportedSource = f.Song.ImportedSource! with
                    { FileReference = Path.GetFullPath("other-memory-only.wav") }
                }
            };
        else f = f with
        {
            Candidate = f.Candidate with
            {
                FilePath = value switch
                {
                    "empty" => "",
                    "nul" => f.Candidate.FilePath + '\0',
                    "mismatch" => Path.GetFullPath("other-memory-only.wav"),
                    _ => value
                }
            }
        };
        Error(PreprocessingError.InvalidPath, () => Request(f));
    }

    [Fact]
    public void IdentityAndHashClaimsAreExplicitStaticAndNotAutomaticallyGenerated()
    {
        var f = Fixture();
        Error(PreprocessingError.InvalidIdentity, () => PreprocessingRequest.Create(Guid.Empty, "caller", f.Song, f.MediaId, f.Candidate, 5, f.Lyrics));
        Error(PreprocessingError.InvalidIdentity, () => PreprocessingRequest.Create(Job, "private\ncaller", f.Song, f.MediaId, f.Candidate, 5, f.Lyrics));
        Error(PreprocessingError.InvalidIdentity, () => PreprocessingRequest.Create(Job, new string((char)0xD800, 1), f.Song, f.MediaId, f.Candidate, 5, f.Lyrics));
        Error(PreprocessingError.InvalidHash, () => Request(f, "sha256:private"));
        var r = Request(f, SourceHash.ToUpperInvariant());
        var p = Proposal(r);
        Assert.Equal(SourceHash.ToUpperInvariant(), r.ExpectedSourceSha256);
        Assert.Equal(Job, p.JobId);
        Error(PreprocessingError.InvalidHash, () => PreprocessingProposal.Create(r, PcmHash, p.Producer, p.Pcm, p.Bins));
    }

    [Fact]
    public void DecodedFrameClockAndUnknownOriginNeverApplySongOrLyricsOffsets()
    {
        var f = Fixture(); var request = Request(f); var proposal = Proposal(request);
        Assert.Equal(PreprocessingTimeReference.DecodedPcmSeconds, proposal.TimeReference);
        Assert.Equal(16001d / 16000, proposal.DurationSeconds);
        Assert.Equal(16001, proposal.Bins[^1].EndFrame); Assert.Equal(16000, proposal.Bins[^1].StartFrame);
        Assert.Null(proposal.OriginInMediaSeconds);
        Assert.Equal(12.5, request.Lyrics.Document!.Lines[0].StartSeconds);
        Assert.Null(request.Lyrics.Document.Lines[0].EndSeconds);
        Assert.Equal(SelectedLyricsTimeReference.LyricsSourceSeconds, request.Lyrics.Document.TimeReference);
        Assert.Equal(3.25, request.OriginalSong.AudioOffsetSeconds); Assert.Equal(-2, request.OriginalSong.VideoOffsetSeconds);
        var claimed = Proposal(request, proposal.Pcm, proposal.Bins, origin: 99);
        Assert.Equal(99, claimed.OriginInMediaSeconds);
        Assert.Equal(proposal.Bins, claimed.Bins); Assert.Same(f.Song, request.OriginalSong);
        Error(PreprocessingError.InvalidPcm, () => Proposal(request, origin: double.NaN));
        Error(PreprocessingError.InvalidPcm, () => Proposal(request, origin: -1));
    }

    [Fact]
    public void NoteEditWhileRequestLivesCannotMakeOriginalSnapshotCurrentBySongId()
    {
        var f = Fixture(); var request = Request(f);
        var current = new MoveNote(f.Song.Phrases[0].Notes[0].Id, 2.5).Apply(f.Song);
        Assert.Equal(current.Id, request.OriginalSong.Id); Assert.NotSame(current, request.OriginalSong);
        Assert.Equal(1, request.OriginalSong.Phrases[0].Notes[0].StartSeconds);
        Assert.Equal(2.5, current.Phrases[0].Notes[0].StartSeconds);
        var proposal = Proposal(request);
        Assert.Same(request, proposal.Request); Assert.Same(f.Song, proposal.Request.OriginalSong);
        Assert.Same(f.Lyrics, proposal.Request.Lyrics); Assert.Empty(current.Analysis.Artifacts);
    }

    [Theory]
    [InlineData("path", PreprocessingError.InvalidPcm)]
    [InlineData("relative-escape", PreprocessingError.InvalidPcm)]
    [InlineData("rate", PreprocessingError.InvalidPcm)]
    [InlineData("channels", PreprocessingError.InvalidPcm)]
    [InlineData("bits", PreprocessingError.InvalidPcm)]
    [InlineData("zero-frames", PreprocessingError.InvalidPcm)]
    [InlineData("negative-frames", PreprocessingError.InvalidPcm)]
    [InlineData("too-many-frames", PreprocessingError.LimitExceeded)]
    [InlineData("file-short", PreprocessingError.InvalidPcm)]
    [InlineData("file-large", PreprocessingError.LimitExceeded)]
    [InlineData("hash", PreprocessingError.InvalidHash)]
    public void PcmShapeAndHashClaimsMustMeetStageProfile(string change, PreprocessingError error)
    {
        var p = Pcm();
        p = change switch
        {
            "path" => p with { RelativePath = Path.GetFullPath("analysis.wav") },
            "relative-escape" => p with { RelativePath = "../analysis.wav" },
            "rate" => p with { SampleRate = 48000 },
            "channels" => p with { Channels = 2 },
            "bits" => p with { BitsPerSample = 32 },
            "zero-frames" => p with { FrameCount = 0 },
            "negative-frames" => p with { FrameCount = -1 },
            "too-many-frames" => p with { FrameCount = long.MaxValue },
            "file-short" => p with { FileBytes = p.FileBytes - 1 },
            "file-large" => p with { FileBytes = PreprocessingValidation.MaxPcmFileBytes + 1 },
            _ => p with { Sha256 = new string('g', 64) }
        };
        Error(error, () => Proposal(Request(Fixture()), p));
    }

    [Theory]
    [InlineData("default")]
    [InlineData("empty")]
    [InlineData("gap")]
    [InlineData("overlap")]
    [InlineData("reversed")]
    [InlineData("past-end")]
    [InlineData("incomplete")]
    [InlineData("nan")]
    [InlineData("infinite")]
    [InlineData("outside-amplitude")]
    [InlineData("min-max")]
    public void WaveformBinsMustCoverExactFramesWithFiniteAmplitudeBounds(string change)
    {
        ImmutableArray<WaveformBin> bins = change switch
        {
            "default" => default,
            "empty" => [],
            "gap" => [new(1, 16001, 0, .5)],
            "overlap" => [new(0, 16000, 0, .5), new(15999, 16001, 0, .5)],
            "reversed" => [new(0, 0, 0, .5)],
            "past-end" => [new(0, 16002, 0, .5)],
            "incomplete" => [new(0, 16000, 0, .5)],
            "nan" => [new(0, 16001, double.NaN, .5)],
            "infinite" => [new(0, 16001, -.5, double.PositiveInfinity)],
            "outside-amplitude" => [new(0, 16001, -1.1, .5)],
            _ => [new(0, 16001, .5, -.5)]
        };
        Error(PreprocessingError.InvalidWaveform, () => Proposal(Request(Fixture()), bins: bins));
    }

    [Fact]
    public void LimitsAcceptExactBoundsButRejectExcessAndInvalidProducerWithoutMutableLeakage()
    {
        var f = Fixture(); var request = Request(f with { Candidate = f.Candidate with { ByteLength = PreprocessingValidation.MaxSourceBytes } });
        var bins = ImmutableArray.CreateBuilder<WaveformBin>();
        for (int i = 0; i < PreprocessingValidation.MaxBins; i++) bins.Add(new(i, i + 1, -1, 1));
        var frozen = bins.ToImmutable();
        var p = Proposal(request, Pcm(4096), frozen);
        bins[0] = new(0, 2, 0, 0);
        Assert.Equal(new WaveformBin(0, 1, -1, 1), p.Bins[0]);
        var changed = p.Bins.SetItem(0, new(0, 2, 0, 0));
        Assert.NotEqual(changed[0], p.Bins[0]);
        var excess = frozen.Add(new(4096, 4097, 0, 0));
        Error(PreprocessingError.LimitExceeded, () => Proposal(request, Pcm(4097), excess));
        var max = Proposal(request, Pcm(PreprocessingValidation.MaxFrames), [new(0, PreprocessingValidation.MaxFrames, -1, 1)]);
        Assert.Equal(1800, max.DurationSeconds);
        Error(PreprocessingError.InvalidProvenance, () => PreprocessingProposal.Create(request, SourceHash,
            new("private\nproducer", "revision"), p.Pcm, p.Bins));
        Error(PreprocessingError.InvalidProvenance, () => PreprocessingProposal.Create(request, SourceHash,
            new("producer", new string((char)0xD800, 1)), p.Pcm, p.Bins));
    }

    [Fact]
    public void NullContractsAndMalformedNestedClaimsUseTypedErrors()
    {
        Error(PreprocessingError.InvalidIdentity, () => PreprocessingValidation.Validate((PreprocessingRequest?)null));
        Error(PreprocessingError.InvalidIdentity, () => PreprocessingValidation.Validate((PreprocessingProposal?)null));
        var r = Request(Fixture()); var p = Proposal(r);
        Error(PreprocessingError.InvalidIdentity, () => PreprocessingProposal.Create(null!, SourceHash, p.Producer, p.Pcm, p.Bins));
        Error(PreprocessingError.InvalidProvenance, () => PreprocessingProposal.Create(r, SourceHash, null!, p.Pcm, p.Bins));
        Error(PreprocessingError.InvalidPcm, () => PreprocessingProposal.Create(r, SourceHash, p.Producer, null!, p.Bins));
        Error(PreprocessingError.InvalidHash, () => PreprocessingProposal.Create(r, null!, p.Producer, p.Pcm, p.Bins));
    }
}
