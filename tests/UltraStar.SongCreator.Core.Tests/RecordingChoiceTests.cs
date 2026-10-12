using System.Text.Json.Nodes;
using UltraStar.SongCreator.Acquisition;
using UltraStar.SongCreator.Core;
using UltraStar.SongCreator.Projects;
using UltraStar.SongCreator.UltraStar;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class RecordingChoiceTests
{
    private static Song Draft() => new()
    {
        Metadata = new() { Title = "Owner edited title", Artist = "Owner edited artist", Language = "nob" },
        Media = [new() { Kind = MediaKind.Audio, Location = Path.GetFullPath(".agent-local/phase3-recording-search/own.wav") }],
        Phrases = [new() { StartSeconds = 0, EndSeconds = 1, Notes = [new() { DurationSeconds = 1, Text = "La" }] }],
        ImportedSource = new()
        {
            FormatId = LocalSongDraft.SourceFormatId,
            FileReference = Path.GetFullPath(".agent-local/phase3-recording-search/own.wav"),
            Headers = [new("source.id", "local-file"), new("format.tag.artist", "Raw original artist"), new("selection.audioStream", "0")]
        }
    };

    [Fact]
    public void ChoiceRequiresExplicitIdentityAndDoesNotDefaultRelease()
    {
        var page = RecordingSearchTests.Page();
        var selection = RecordingChoices.Select(page, page.Candidates[1].Id);
        Assert.Equal("live at own venue", selection.Candidate.Disambiguation);
        Assert.Null(selection.ReleaseId);
        Assert.Equal(RecordingSelectionError.NotFound,
            Assert.Throws<RecordingSelectionException>(() => RecordingChoices.Select(page, "not a candidate")).Error);
        var duplicate = page with { Candidates = [page.Candidates[0], page.Candidates[0]] };
        Assert.Equal(RecordingSelectionError.Ambiguous,
            Assert.Throws<RecordingSelectionException>(() => RecordingChoices.Select(duplicate, RecordingSearchTests.RecordingId)).Error);
    }

    [Fact]
    public void OptionalReleaseMustBelongToChosenRecording()
    {
        var page = RecordingSearchTests.Page();
        var selection = RecordingChoices.Select(page, RecordingSearchTests.RecordingId, RecordingSearchTests.ReleaseId);
        Assert.Equal(RecordingSearchTests.ReleaseId, selection.ReleaseId);
        Assert.Equal(RecordingSelectionError.InvalidCandidate,
            Assert.Throws<RecordingSelectionException>(() => RecordingChoices.Select(page, page.Candidates[1].Id, RecordingSearchTests.ReleaseId)).Error);
    }

    [Fact]
    public void BindingChangesOnlyChoiceProvenanceAndDoesNotMutateOriginal()
    {
        var original = Draft();
        var choice = RecordingChoices.Select(RecordingSearchTests.Page(), RecordingSearchTests.RecordingId, RecordingSearchTests.ReleaseId);
        var bound = RecordingChoices.Attach(original, choice);
        Assert.Same(original.Metadata, bound.Metadata);
        Assert.Equal(original.Media, bound.Media);
        Assert.Equal(original.Phrases, bound.Phrases);
        Assert.Same(original.Analysis, bound.Analysis);
        Assert.Equal(original.ImportedSource!.FileReference, bound.ImportedSource!.FileReference);
        Assert.Equal(original.ImportedSource.Headers.ToArray(), bound.ImportedSource.Headers.Take(3).ToArray());
        Assert.Equal(3, original.ImportedSource.Headers.Length);
        var decoded = RecordingChoices.Read(bound)!;
        Assert.Equal(choice.Candidate.Id, decoded.Candidate.Id);
        Assert.Equal(choice.Candidate.Artists.ToArray(), decoded.Candidate.Artists.ToArray());
        Assert.Equal(choice.Candidate.Releases.ToArray(), decoded.Candidate.Releases.ToArray());
        Assert.Equal(choice.ReleaseId, decoded.ReleaseId);
        Assert.Same(bound, RecordingChoices.Attach(bound, choice));
        var json = bound.ImportedSource.Headers.Single(h => h.Name == RecordingChoices.HeaderName).Value;
        Assert.DoesNotContain("score", json);
        Assert.DoesNotContain("ignored supplementary tag", json);
    }

    [Fact]
    public void UndoRedoAndReplacementRestoreExactSnapshotsAndKeepOneChoice()
    {
        var initial = Draft();
        var page = RecordingSearchTests.Page();
        var first = RecordingChoices.Select(page, page.Candidates[0].Id);
        var second = RecordingChoices.Select(page, page.Candidates[1].Id);
        var history = new SongHistory(initial);
        Assert.True(history.Execute(new BindRecordingChoice(first)));
        var afterFirst = history.Current;
        Assert.True(history.Execute(new BindRecordingChoice(second)));
        var afterSecond = history.Current;
        Assert.Single(afterSecond.ImportedSource!.Headers.Where(h => h.Name == RecordingChoices.HeaderName));
        Assert.True(history.Undo());
        Assert.Same(afterFirst, history.Current);
        Assert.False(history.Execute(new BindRecordingChoice(first))); // no-op preserves redo.
        Assert.True(history.CanRedo);
        Assert.True(history.Redo());
        Assert.Same(afterSecond, history.Current);
        Assert.True(history.Undo());
        Assert.True(history.Undo());
        Assert.Same(initial, history.Current);
        Assert.Null(RecordingChoices.Read(initial));
    }

    [Fact]
    public async Task SaveLoadReadsChoiceOfflineAndExportKeepsOnlyCurrentSongMetadata()
    {
        using var files = new ProjectTestDirectory();
        var selected = RecordingChoices.Select(RecordingSearchTests.Page(), RecordingSearchTests.RecordingId, RecordingSearchTests.ReleaseId);
        var song = RecordingChoices.Attach(Draft(), selected);
        var path = files.FilePath();
        var store = new ProjectStore();
        await store.SaveAsync(path, song);
        var before = await File.ReadAllBytesAsync(path);
        var loaded = await store.LoadAsync(path); // No provider exists in this test.
        Assert.Equal(2, loaded.SchemaVersion);
        Assert.Equal(song.Metadata, loaded.Song.Metadata);
        Assert.Equal(song.ImportedSource!.Headers.ToArray(), loaded.Song.ImportedSource!.Headers.ToArray());
        var choice = RecordingChoices.Read(loaded.Song)!;
        Assert.Equal(selected.Candidate.Id, choice.Candidate.Id);
        Assert.Equal(selected.ReleaseId, choice.ReleaseId);
        Assert.Equal(selected.Candidate.DurationSeconds, choice.Candidate.DurationSeconds);
        Assert.Equal(selected.Candidate.Disambiguation, choice.Candidate.Disambiguation);
        Assert.Equal(selected.RetrievedAtUtc, choice.RetrievedAtUtc);
        var exported = new UltraStarExporter().Render(loaded.Song, new(ExportFormat.V1, GridBpm: 120));
        Assert.True(exported.Success);
        Assert.Contains("#TITLE:Owner edited title", exported.Text!);
        Assert.DoesNotContain("catalog.recording", exported.Text!);
        Assert.DoesNotContain(RecordingSearchTests.RecordingId, exported.Text!);
        Assert.Equal(before, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public void UnsupportedSourceDoesNotLeakChoiceIntoRawUltraStarHeaders()
    {
        var song = Draft() with { ImportedSource = new() { FormatId = "ultrastar-v1", Headers = [] } };
        var selected = RecordingChoices.Select(RecordingSearchTests.Page(), RecordingSearchTests.RecordingId);
        Assert.Equal(RecordingSelectionError.UnsupportedSongSource, Assert.Throws<RecordingSelectionException>(() => RecordingChoices.Attach(song, selected)).Error);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("null-artists")]
    [InlineData("bad-release")]
    public void CorruptStoredChoiceIsRejectedWithoutNetworkFallback(string mutation)
    {
        var bound = RecordingChoices.Attach(Draft(), RecordingChoices.Select(RecordingSearchTests.Page(), RecordingSearchTests.RecordingId));
        var header = bound.ImportedSource!.Headers.Single(h => h.Name == RecordingChoices.HeaderName);
        var json = JsonNode.Parse(header.Value)!.AsObject();
        switch (mutation)
        {
            case "version": json["version"] = 2; break;
            case "missing": json.Remove("title"); break;
            case "extra": json["unknown"] = true; break;
            case "null-artists": json["artists"] = null; break;
            case "bad-release": json["releaseId"] = "not-in-candidate"; break;
        }
        var source = bound.ImportedSource with { Headers = [new(RecordingChoices.HeaderName, json.ToJsonString())] };
        Assert.Equal(RecordingSelectionError.InvalidStoredChoice,
            Assert.Throws<RecordingSelectionException>(() => RecordingChoices.Read(bound with { ImportedSource = source })).Error);
    }

    [Fact]
    public void DuplicateJsonAndDuplicateHeaderAreNotSilentlySelected()
    {
        var bound = RecordingChoices.Attach(Draft(), RecordingChoices.Select(RecordingSearchTests.Page(), RecordingSearchTests.RecordingId));
        var header = bound.ImportedSource!.Headers.Single(h => h.Name == RecordingChoices.HeaderName);
        var duplicate = bound with { ImportedSource = bound.ImportedSource with { Headers = [header, header] } };
        Assert.Equal(RecordingSelectionError.InvalidStoredChoice, Assert.Throws<RecordingSelectionException>(() => RecordingChoices.Read(duplicate)).Error);
        var value = header.Value.Replace("\"version\":1", "\"version\":1,\"version\":1");
        var duplicateJson = bound with { ImportedSource = bound.ImportedSource with { Headers = [header with { Value = value }] } };
        Assert.Equal(RecordingSelectionError.InvalidStoredChoice, Assert.Throws<RecordingSelectionException>(() => RecordingChoices.Read(duplicateJson)).Error);
    }

    [Fact]
    public void ProviderMismatchAndInventedSourceUrlsAreRejected()
    {
        var page = RecordingSearchTests.Page();
        Assert.Throws<RecordingSelectionException>(() => RecordingChoices.Select(page with { ProviderId = "other" }, RecordingSearchTests.RecordingId));
        var candidate = page.Candidates[0] with { SourceUrl = "https://example.test/media" };
        Assert.Throws<RecordingSelectionException>(() => RecordingChoices.Attach(Draft(), new(candidate, null, page.RetrievedAtUtc)));
    }
}
