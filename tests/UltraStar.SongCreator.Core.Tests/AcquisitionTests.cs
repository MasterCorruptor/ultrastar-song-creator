using System.Collections.Immutable;
using System.Text.Json;
using UltraStar.SongCreator.Acquisition;
using UltraStar.SongCreator.Core;
using UltraStar.SongCreator.Projects;
using UltraStar.SongCreator.UltraStar;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class AcquisitionTests
{
    private const string Json = """
        {
          "streams": [
            {"index":0,"codec_type":"audio","codec_name":"pcm_s16le","sample_rate":"8000","channels":1,"duration":"3.500000","tags":{"title":"Stream title","language":"eng"}},
            {"index":1,"codec_type":"video","codec_name":"mpeg4","width":64,"height":64,"disposition":{"attached_pic":0}}
          ],
          "format":{"format_name":"wav","duration":"3.500000","tags":{"TITLE":"  Own title  ","artist":" Own artist ","ALBUM":"Own album","custom":"raw  "}}
        }
        """;

    private static MediaCandidate Candidate(string? text = null)
    {
        var path = Path.GetFullPath(Path.Combine(".agent-local", "phase3-local-media", "synthetic.wav"));
        return new("local-file", path, 100, ProbeJson.Parse(text ?? Json));
    }

    private sealed class Probe(Func<string, CancellationToken, Task<MediaInspection>> inspect) : IMediaProbe
    {
        public Task<MediaInspection> InspectAsync(string path, CancellationToken cancellationToken = default) => inspect(path, cancellationToken);
    }

    [Fact]
    public void ParserAndDraftNormalizeSelectedMetadataAndKeepRawProvenance()
    {
        var candidate = Candidate();
        var draft = LocalSongDraft.Create(candidate);
        var song = draft.Song;
        Assert.Equal("Own title", song.Metadata.Title);
        Assert.Equal("Own artist", song.Metadata.Artist);
        Assert.Equal("eng", song.Metadata.Language);
        Assert.Null(song.BeatsPerMinute);
        Assert.Empty(song.Phrases);
        Assert.Empty(song.Analysis.Artifacts);
        Assert.Equal(2, song.Media.Length);
        Assert.All(song.Media, m => Assert.Equal(candidate.FilePath, m.Location));
        Assert.Equal(song.Media.Length, song.Media.Select(m => m.Id).Distinct().Count());
        Assert.Equal(0, song.AudioOffsetSeconds);
        Assert.Equal(LocalSongDraft.SourceFormatId, song.ImportedSource!.FormatId);
        Assert.Equal(candidate.FilePath, song.ImportedSource.FileReference);
        Assert.Contains(new SourceHeader("format.tag.TITLE", "  Own title  "), song.ImportedSource.Headers);
        Assert.Contains(new SourceHeader("format.tag.custom", "raw  "), song.ImportedSource.Headers);
        Assert.Contains(new SourceHeader("stream.0.sampleRate", "8000"), song.ImportedSource.Headers);
        Assert.Contains(new SourceHeader("selection.audioStream", "0"), song.ImportedSource.Headers);
        Assert.Contains(new SourceHeader("selection.videoStream", "1"), song.ImportedSource.Headers);
        Assert.DoesNotContain(SongValidator.Validate(song), i => i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void ManualOverridesIncludingEmptyValuesHavePriorityAndDoNotModifyTags()
    {
        var candidate = Candidate();
        var draft = LocalSongDraft.Create(candidate, new(" Changed ", "", "nob"));
        Assert.Equal("Changed", draft.Song.Metadata.Title);
        Assert.Equal("", draft.Song.Metadata.Artist);
        Assert.Equal("nob", draft.Song.Metadata.Language);
        Assert.Contains(draft.Diagnostics, d => d.Code == "MissingMetadata");
        Assert.Contains(new MediaTag("TITLE", "  Own title  "), candidate.Inspection.Tags);
        Assert.Contains(new SourceHeader("format.tag.artist", " Own artist "), draft.Song.ImportedSource!.Headers);
    }

    [Fact]
    public void MissingMetadataDoesNotInventTitleFromFilenameOrRejectEditableDraft()
    {
        var candidate = Candidate() with { Inspection = Candidate().Inspection with { Tags = [], Streams = [Candidate().Inspection.Streams[0] with { Tags = [] }] } };
        var draft = LocalSongDraft.Create(candidate);
        Assert.Equal("", draft.Song.Metadata.Title);
        Assert.Equal("", draft.Song.Metadata.Artist);
        Assert.Null(draft.Song.Metadata.Language);
        Assert.Equal(2, draft.Diagnostics.Count(d => d.Code == "MissingMetadata"));
    }

    [Fact]
    public void ConflictingCaseVariantsRequireCorrectionRatherThanArbitrarySelection()
    {
        var candidate = Candidate() with { Inspection = Candidate().Inspection with { Tags = [new("title", "One"), new("TITLE", "Two")] } };
        var draft = LocalSongDraft.Create(candidate);
        Assert.Equal("", draft.Song.Metadata.Title);
        Assert.Contains(draft.Diagnostics, d => d.Code == "ConflictingMetadata");
        Assert.Equal("Manual", LocalSongDraft.Create(candidate, new(Title: "Manual")).Song.Metadata.Title);
        Assert.Equal("Own title", LocalSongDraft.Create(Candidate()).Song.Metadata.Title); // Container precedes stream title.
    }

    [Fact]
    public void UnknownLanguageAndUnknownDurationStayUnknown()
    {
        var candidate = Candidate() with { Inspection = Candidate().Inspection with { DurationSeconds = null, Tags = [new("language", "und")], Streams = [Candidate().Inspection.Streams[0] with { Tags = [] }] } };
        var draft = LocalSongDraft.Create(candidate);
        Assert.Null(draft.Song.Metadata.Language);
        Assert.Contains(draft.Diagnostics, d => d.Code == "UnknownDuration");
        Assert.DoesNotContain(draft.Song.ImportedSource!.Headers, h => h.Name == "format.durationSeconds");
    }

    [Fact]
    public void UndefinedContainerLanguageDoesNotHideKnownAudioLanguage()
    {
        var candidate = Candidate() with { Inspection = Candidate().Inspection with { Tags = [new("language", "und")] } };
        Assert.Equal("eng", LocalSongDraft.Create(candidate).Song.Metadata.Language);
        Assert.Null(LocalSongDraft.Create(candidate, new(Language: "und")).Song.Metadata.Language);
    }

    [Fact]
    public void MultipleAudioOrVideoStreamsRequireExplicitSelection()
    {
        var original = Candidate();
        var candidate = original with { Inspection = original.Inspection with { Streams = original.Inspection.Streams.Add(original.Inspection.Streams[0] with { Index = 2 }) } };
        Assert.Equal(AcquisitionError.AmbiguousStream, Assert.Throws<MediaAcquisitionException>(() => LocalSongDraft.Create(candidate)).Error);
        var draft = LocalSongDraft.Create(candidate, audioStreamIndex: 2);
        Assert.Contains(new SourceHeader("selection.audioStream", "2"), draft.Song.ImportedSource!.Headers);
        Assert.Equal(AcquisitionError.UnsupportedMedia, Assert.Throws<MediaAcquisitionException>(() => LocalSongDraft.Create(candidate, audioStreamIndex: 99)).Error);
        candidate = original with { Inspection = original.Inspection with { Streams = original.Inspection.Streams.Add(original.Inspection.Streams[1] with { Index = 3 }) } };
        Assert.Equal(AcquisitionError.AmbiguousStream, Assert.Throws<MediaAcquisitionException>(() => LocalSongDraft.Create(candidate)).Error);
        Assert.Contains(new SourceHeader("selection.videoStream", "3"), LocalSongDraft.Create(candidate, videoStreamIndex: 3).Song.ImportedSource!.Headers);
    }

    [Fact]
    public void EmbeddedCoverIsNotSelectedAsVideoAndSilentVideoIsNotSongAudio()
    {
        var original = Candidate();
        var candidate = original with { Inspection = original.Inspection with { Streams = [original.Inspection.Streams[0], original.Inspection.Streams[1] with { AttachedPicture = true }] } };
        var draft = LocalSongDraft.Create(candidate);
        Assert.Equal(MediaKind.Audio, Assert.Single(draft.Song.Media).Kind);
        Assert.Contains(draft.Diagnostics, d => d.Code == "EmbeddedArtworkRetained");
        candidate = original with { Inspection = original.Inspection with { Streams = [original.Inspection.Streams[1]] } };
        Assert.Equal(AcquisitionError.UnsupportedMedia, Assert.Throws<MediaAcquisitionException>(() => LocalSongDraft.Create(candidate)).Error);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{")]
    [InlineData("""{"format":{},"streams":[]}""")]
    [InlineData("""{"format":{"format_name":"wav","duration":"NaN"},"streams":[]}""")]
    [InlineData("""{"format":{"format_name":"wav","duration":1e999},"streams":[]}""")]
    [InlineData("""{"format":{"format_name":"wav","duration":"-1"},"streams":[]}""")]
    [InlineData("""{"format":{"format_name":"wav"},"streams":[{"index":0,"codec_type":"audio","sample_rate":"0"}]}""")]
    [InlineData("""{"format":{"format_name":"wav"},"streams":[{"index":0,"codec_type":"audio","channels":-1}]}""")]
    [InlineData("""{"format":{"format_name":"wav"},"streams":[{"index":0,"codec_type":"audio"},{"index":0,"codec_type":"audio"}]}""")]
    [InlineData("""{"format":{"format_name":"wav"},"streams":[{"index":-1,"codec_type":"audio"}]}""")]
    [InlineData("""{"format":{"format_name":"wav"},"streams":[{"index":0,"codec_type":"video","disposition":{"attached_pic":2}}]}""")]
    [InlineData("""{"format":{"format_name":"wav","tags":{"title":5}},"streams":[]}""")]
    [InlineData("""{"format":{"format_name":"wav","format_name":"mp3"},"streams":[]}""")]
    [InlineData("""{"format":{"format_name":"wav"},"streams":null}""")]
    public void InvalidProbeDocumentsAreRejected(string text) =>
        Assert.Equal(AcquisitionError.InvalidProbeData, Assert.Throws<MediaAcquisitionException>(() => ProbeJson.Parse(text)).Error);

    [Fact]
    public void ParserAcceptsUnknownDurationAndIgnoresUnselectedTechnicalSections()
    {
        var parsed = ProbeJson.Parse("""{"programs":[],"format":{"format_name":"mp3","duration":"N/A"},"streams":[{"index":0,"codec_type":"audio"}]}""");
        Assert.Null(parsed.DurationSeconds);
        Assert.Null(parsed.Streams[0].SampleRate);
        Assert.Empty(parsed.Tags);
    }

    [Fact]
    public async Task LocalSourceReturnsAbsoluteReferenceWithoutChangingOrCopyingFile()
    {
        using var files = new ProjectTestDirectory();
        var path = files.FilePath("own audio.wav");
        await File.WriteAllBytesAsync(path, [1, 2, 3]);
        var calls = 0;
        var source = new LocalMediaSource(new Probe((actual, _) =>
        {
            Assert.Equal(path, actual);
            calls++;
            return Task.FromResult(Candidate().Inspection);
        }));
        var candidate = await source.InspectAsync(path);
        Assert.Equal(1, calls);
        Assert.Equal(path, candidate.FilePath);
        Assert.Equal(3, candidate.ByteLength);
        Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(path));
        Assert.Single(Directory.GetFiles(files.DirectoryPath));
    }

    [Theory]
    [InlineData("https://example.test/audio.wav")]
    [InlineData("relative.wav")]
    [InlineData("file:///fake/audio.wav")]
    public async Task LocalSourceRejectsUrlsAndRelativePathsBeforeCallingProbe(string path)
    {
        var source = new LocalMediaSource(new Probe((_, _) => throw new InvalidOperationException("Should not probe")));
        await Assert.ThrowsAsync<ArgumentException>(() => source.InspectAsync(path));
    }

    [Fact]
    public async Task MissingEmptyChangingAndCancelledFilesDoNotProduceCandidates()
    {
        using var files = new ProjectTestDirectory();
        var path = files.FilePath("audio.wav");
        var source = new LocalMediaSource(new Probe((_, _) => Task.FromResult(Candidate().Inspection)));
        await Assert.ThrowsAsync<FileNotFoundException>(() => source.InspectAsync(path));
        await File.WriteAllBytesAsync(path, []);
        Assert.Equal(AcquisitionError.UnsupportedMedia, (await Assert.ThrowsAsync<MediaAcquisitionException>(() => source.InspectAsync(path))).Error);
        await File.WriteAllBytesAsync(path, [1]);
        source = new(new Probe(async (_, _) =>
        {
            await File.WriteAllBytesAsync(path, [1, 2]);
            return Candidate().Inspection;
        }));
        await Assert.ThrowsAsync<IOException>(() => source.InspectAsync(path));
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.InspectAsync(path, stop.Token));
    }

    [Fact]
    public async Task SaveLoadPreservesOriginalAndEditedMetadataWithoutNewProbe()
    {
        using var files = new ProjectTestDirectory();
        var original = LocalSongDraft.Create(Candidate(), new(Title: "Owner title")).Song;
        var store = new ProjectStore();
        var path = files.FilePath("own.uscproject");
        await store.SaveAsync(path, original);
        var saved = await File.ReadAllBytesAsync(path);
        var loaded = await store.LoadAsync(path);
        Assert.Equal(2, loaded.SchemaVersion);
        Assert.Equal(original.Metadata, loaded.Song.Metadata);
        Assert.Equal(original.Media.ToArray(), loaded.Song.Media.ToArray());
        Assert.Equal(original.ImportedSource!.Headers.ToArray(), loaded.Song.ImportedSource!.Headers.ToArray());
        Assert.Equal(original.ImportedSource.FileReference, loaded.Song.ImportedSource.FileReference);
        Assert.Equal(saved, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task LocalDraftWithLaterNotesCanExportPackageWithoutLeakingProbeHeaders()
    {
        using var files = new ProjectTestDirectory();
        var source = files.FilePath("audio.wav");
        await File.WriteAllBytesAsync(source, [1, 2, 3]); // Copy contract, not a codec test.
        var candidate = Candidate() with { FilePath = source, Inspection = Candidate().Inspection with { Streams = [Candidate().Inspection.Streams[0]] } };
        var draft = LocalSongDraft.Create(candidate).Song;
        var phrase = new Phrase { StartSeconds = 0, EndSeconds = 1, Notes = [new() { StartSeconds = 0, DurationSeconds = 1, Text = "La" }] };
        var edited = draft with { Phrases = [phrase] };
        var writer = new UltraStarExporter();
        Assert.Contains(writer.Render(edited, new(ExportFormat.V1)).Diagnostics, d => d.Code == ExportCode.MissingGrid);
        foreach (var format in new[] { ExportFormat.Unversioned, ExportFormat.V1 })
        {
            var result = await writer.PackageAsync(files.FilePath(format.ToString()), edited, new(format, GridBpm: 120));
            Assert.True(result.Success);
            Assert.DoesNotContain("#format.", result.Text!);
            Assert.DoesNotContain("#stream.", result.Text!);
            Assert.Contains(result.Diagnostics, d => d.Code == ExportCode.ProjectOnlyData);
            var imported = new UltraStarImporter().Parse(result.Text!);
            Assert.True(imported.Success);
            Assert.Equal(edited.Metadata.Title, imported.Song!.Metadata.Title);
            Assert.Equal("La", imported.Song.Phrases[0].Text);
            Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(Path.Combine(result.DirectoryPath!, imported.Song.Media[0].Location)));
        }
        Assert.Same(draft.ImportedSource, edited.ImportedSource);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExportDoesNotSilentlyLoseMultiStreamSelection(bool multipleVideo)
    {
        var candidate = Candidate();
        var extra = candidate.Inspection.Streams[multipleVideo ? 1 : 0] with { Index = 2 };
        candidate = candidate with { Inspection = candidate.Inspection with { Streams = candidate.Inspection.Streams.Add(extra) } };
        var draft = LocalSongDraft.Create(candidate, audioStreamIndex: multipleVideo ? 0 : 2, videoStreamIndex: multipleVideo ? 2 : 1);
        Assert.Contains(draft.Diagnostics, d => d.Code == "SelectedStreamsRequireExtraction");
        var song = draft.Song with { Phrases = [new() { StartSeconds = 0, EndSeconds = 1, Notes = [new() { DurationSeconds = 1, Text = "La" }] }] };
        var result = new UltraStarExporter().Render(song, new(ExportFormat.V1, GridBpm: 120));
        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Code == ExportCode.AmbiguousMedia);
        Assert.Same(draft.Song.ImportedSource, song.ImportedSource);
    }

    [Fact]
    public void StandaloneAudioReferenceCanExportAfterMultiStreamPreprocessing()
    {
        var candidate = Candidate();
        candidate = candidate with { Inspection = candidate.Inspection with { Streams = [candidate.Inspection.Streams[0], candidate.Inspection.Streams[0] with { Index = 2 }] } };
        var draft = LocalSongDraft.Create(candidate, audioStreamIndex: 2).Song;
        var song = draft with
        {
            Media = [draft.Media[0] with { Location = Path.GetFullPath(".agent-local/phase3-local-media/extracted.wav") }],
            Phrases = [new() { StartSeconds = 0, EndSeconds = 1, Notes = [new() { DurationSeconds = 1, Text = "La" }] }]
        };
        Assert.True(new UltraStarExporter().Render(song, new(ExportFormat.V1, GridBpm: 120)).Success);
    }

    [Fact]
    public void ProbeOptionsRejectImplicitExecutableOrInvalidLimits()
    {
        Assert.Throws<ArgumentException>(() => new FfprobeMediaProbe("ffprobe"));
        var exe = Candidate().FilePath;
        Assert.Throws<ArgumentOutOfRangeException>(() => new FfprobeMediaProbe(exe, TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FfprobeMediaProbe(exe, TimeSpan.FromMinutes(6)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FfprobeMediaProbe(exe, maximumOutputBytes: 0));
    }

    [Fact]
    public async Task MissingConfiguredProbeHasStableError()
    {
        using var files = new ProjectTestDirectory();
        var probe = new FfprobeMediaProbe(files.FilePath("missing-probe"));
        Assert.Equal(AcquisitionError.ProbeUnavailable,
            (await Assert.ThrowsAsync<MediaAcquisitionException>(() => probe.InspectAsync(files.FilePath("audio.wav")))).Error);
    }
}
