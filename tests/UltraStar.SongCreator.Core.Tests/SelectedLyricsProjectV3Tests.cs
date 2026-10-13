using System.Collections.Immutable;
using System.Text.Json.Nodes;
using UltraStar.SongCreator.Acquisition.Lyrics;
using UltraStar.SongCreator.Projects;
using Core = UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Core.Tests;

public class SelectedLyricsProjectV3Tests
{
    private static string FixturePath(string version) => Path.Combine(ProjectTestDirectory.RepositoryRoot,
        "tests", "UltraStar.SongCreator.Core.Tests", "Fixtures", version + ".uscproject");

    [Theory]
    [InlineData("null")]
    [InlineData("manual")]
    [InlineData("plain-provider")]
    [InlineData("line")]
    [InlineData("word")]
    [InlineData("instrumental")]
    public async Task ExactSelectedPayloadAndAllOtherSongDataRoundTrip(string kind)
    {
        using var files = new ProjectTestDirectory();
        var word = SelectedLyricsEditingTests.Word();
        var value = kind switch
        {
            "null" => null,
            "manual" => SelectedLyricsEditingTests.Plain(),
            "plain-provider" => SelectedLyricsEditingTests.Plain() with { Provenance = word.Provenance },
            "line" => word with
            {
                Document = word.Document! with
                { SyncLevel = Core.SelectedLyricsSyncLevel.Line, Lines = word.Document.Lines.Select(l => l with { Words = [] }).ToImmutableArray() }
            },
            "instrumental" => word with { Kind = Core.SelectedLyricsKind.Instrumental, Document = null },
            _ => word
        };
        var song = SongFixture.Create() with
        {
            SelectedLyrics = value,
            ImportedSource = new()
            {
                FormatId = "local-media-v1",
                FileReference = "../data/source",
                Headers = [new("SourceUrl", "https://example.invalid/file"), new("X", " first "), new("x", "second"), new("X", "")]
            }
        };
        var store = new ProjectStore();
        await store.SaveAsync(files.FilePath(), song);
        var loaded = await store.LoadAsync(files.FilePath());
        Assert.Equal(3, loaded.SchemaVersion);
        Assert.True(Core.SelectedLyricsEquality.AreEqual(value, loaded.Song.SelectedLyrics));
        ProjectRoundTripTests.AssertSameSongValues(song, loaded.Song);
        var root = JsonNode.Parse(await File.ReadAllTextAsync(files.FilePath()))!;
        Assert.Equal(["format", "schemaVersion", "song", "importedSource", "selectedLyrics"], root.AsObject().Select(p => p.Key).ToArray());
        Assert.False(root["song"]!.AsObject().ContainsKey("selectedLyrics"));
        if (value?.Document is { } d) Assert.Equal(d.Text, root["selectedLyrics"]!["document"]!["text"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("v1", 1)]
    [InlineData("v2", 2)]
    public async Task LegacyMigrationPreservesOriginalBytesAndEverySongField(string fixture, int version)
    {
        using var files = new ProjectTestDirectory();
        var before = await File.ReadAllBytesAsync(FixturePath(fixture));
        await File.WriteAllBytesAsync(files.FilePath(), before);
        var store = new ProjectStore();
        var loaded = await store.LoadAsync(files.FilePath());
        Assert.Equal(version, loaded.SchemaVersion);
        Assert.Null(loaded.Song.SelectedLyrics);
        Assert.Equal(before, await File.ReadAllBytesAsync(files.FilePath()));
        if (version == 1) Assert.Null(loaded.Song.ImportedSource);
        else
        {
            Assert.Equal("Independent v2 Æ", loaded.Song.Metadata.Title);
            Assert.Equal("Own ", loaded.Song.Phrases[0].Notes[0].Text);
            Assert.Equal([new SourceHeader("TITLE", "Old claim"), new("x-Choice", " first "),
                new("x-Choice", "second"), new("AU", "instrumental")], loaded.Song.ImportedSource!.Headers.ToArray());
        }
        await store.SaveAsync(files.FilePath("saved"), loaded.Song);
        var saved = await store.LoadAsync(files.FilePath("saved"));
        Assert.Equal(3, saved.SchemaVersion); Assert.Null(saved.Song.SelectedLyrics);
        ProjectRoundTripTests.AssertSameSongValues(loaded.Song, saved.Song);
        Assert.Equal(before, await File.ReadAllBytesAsync(files.FilePath()));
    }

    [Theory]
    [InlineData("v1")]
    [InlineData("v2")]
    public async Task LegacyReadersRejectTheNewRootFieldRatherThanSilentlyDroppingIt(string fixture)
    {
        var root = JsonNode.Parse(await File.ReadAllTextAsync(FixturePath(fixture)))!;
        root["selectedLyrics"] = null;
        Assert.Equal(ProjectError.InvalidJson, (await Reject(root.ToJsonString())).Error);
    }

    [Fact]
    public async Task HandwrittenV3WordFixtureLoadsIndependentlyOfWriterAndRetainsExactClaims()
    {
        using var files = new ProjectTestDirectory();
        var before = await File.ReadAllBytesAsync(FixturePath("v3"));
        await File.WriteAllBytesAsync(files.FilePath(), before);
        var store = new ProjectStore();
        var loaded = await store.LoadAsync(files.FilePath());
        Assert.Equal(3, loaded.SchemaVersion);
        Assert.True(Core.SelectedLyricsEquality.AreEqual(SelectedLyricsEditingTests.Word(), loaded.Song.SelectedLyrics));
        Assert.Equal("Owner", loaded.Song.Metadata.Artist);
        Assert.Equal("downloaded-media-v1", loaded.Song.ImportedSource!.FormatId);
        Assert.Equal(before, await File.ReadAllBytesAsync(files.FilePath()));
        await store.SaveAsync(files.FilePath("saved"), loaded.Song);
        var saved = await store.LoadAsync(files.FilePath("saved"));
        Assert.True(Core.SelectedLyricsEquality.AreEqual(loaded.Song.SelectedLyrics, saved.Song.SelectedLyrics));
        ProjectRoundTripTests.AssertSameSongValues(loaded.Song, saved.Song);
    }

    [Fact]
    public async Task OnlyExplicitlyChosenVariantProjectsAndStores()
    {
        using var files = new ProjectTestDirectory();
        var chosen = new LyricsDocument("chosen", "chosen exact\n", LyricsSyncLevel.Plain, []);
        var candidate = new LyricsCandidate
        {
            Id = "candidate",
            ProviderId = "fixture",
            TextStatus = LyricsTextStatus.Available,
            SourceUrl = "https://example.invalid/lyrics",
            RetrievedAtUtc = SelectedLyricsEditingTests.At,
            Artist = "claim artist",
            Title = "claim title",
            Documents = [new("unselected", "do not persist this", LyricsSyncLevel.Plain, []), chosen]
        };
        var selected = SelectedLyricsProjection.FromSelection(new(candidate, chosen));
        var song = new SetSelectedLyrics(selected).Apply(SongFixture.Create());
        var store = new ProjectStore();
        await store.SaveAsync(files.FilePath(), song);
        var bytes = await File.ReadAllTextAsync(files.FilePath());
        Assert.DoesNotContain("unselected", bytes); Assert.DoesNotContain("do not persist this", bytes);
        var loaded = await store.LoadAsync(files.FilePath());
        Assert.Equal("chosen exact\n", loaded.Song.SelectedLyrics!.Document!.Text);
        Assert.Equal(song.Metadata, loaded.Song.Metadata);
    }

    [Theory]
    [InlineData("root-missing")]
    [InlineData("missing-nullable")]
    [InlineData("null-provenance")]
    [InlineData("null-lines")]
    [InlineData("null-line")]
    [InlineData("null-word")]
    [InlineData("null-text")]
    [InlineData("missing-end")]
    [InlineData("unknown-selected")]
    [InlineData("unknown-document")]
    [InlineData("unknown-source")]
    [InlineData("unknown-line")]
    [InlineData("unknown-word")]
    [InlineData("kind-enum")]
    [InlineData("kind-numeric")]
    [InlineData("origin-enum")]
    [InlineData("sync-enum")]
    [InlineData("time-enum")]
    [InlineData("date-type")]
    [InlineData("line-type")]
    [InlineData("word-type")]
    public async Task V3SchemaFailuresAreTypedInvalidJsonAndNeverRewrite(string mutation)
    {
        var root = JsonNode.Parse(await File.ReadAllTextAsync(FixturePath("v3")))!;
        var selected = root["selectedLyrics"]!; var document = selected["document"]!;
        var provenance = selected["provenance"]!; var line = document["lines"]![0]!; var word = line["words"]![0]!;
        switch (mutation)
        {
            case "root-missing": root.AsObject().Remove("selectedLyrics"); break;
            case "missing-nullable": provenance.AsObject().Remove("album"); break;
            case "null-provenance": selected["provenance"] = null; break;
            case "null-lines": document["lines"] = null; break;
            case "null-line": document["lines"]![0] = null; break;
            case "null-word": line["words"]![0] = null; break;
            case "null-text": document["text"] = null; break;
            case "missing-end": word.AsObject().Remove("endSeconds"); break;
            case "unknown-selected": selected["cache"] = new JsonObject(); break;
            case "unknown-document": document["rawLrc"] = "data"; break;
            case "unknown-source": provenance["capabilities"] = 0; break;
            case "unknown-line": line["offset"] = 1; break;
            case "unknown-word": word["confidence"] = 1; break;
            case "kind-enum": selected["kind"] = "Text"; break;
            case "kind-numeric": selected["kind"] = 0; break;
            case "origin-enum": provenance["origin"] = "Provider"; break;
            case "sync-enum": document["syncLevel"] = "unknown"; break;
            case "time-enum": document["timeReference"] = "songAudio"; break;
            case "date-type": provenance["retrievedAtUtc"] = "yesterday"; break;
            case "line-type": line["startSeconds"] = "1"; break;
            case "word-type": word["endSeconds"] = false; break;
        }
        Assert.Equal(ProjectError.InvalidJson, (await Reject(root.ToJsonString())).Error);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("sourceId")]
    [InlineData("startSeconds")]
    public async Task DuplicatePropertiesInsideSelectedHierarchyAreRejected(string property)
    {
        var json = await File.ReadAllTextAsync(FixturePath("v3"));
        json = json.Replace($"\"{property}\":", $"\"{property}\": null, \"{property}\":", StringComparison.Ordinal);
        Assert.Equal(ProjectError.InvalidJson, (await Reject(json)).Error);
    }

    [Theory]
    [InlineData("surrogate")]
    [InlineData("utf8")]
    public async Task MalformedUnicodeBecomesProjectFormatError(string mutation)
    {
        using var files = new ProjectTestDirectory();
        var json = await File.ReadAllTextAsync(FixturePath("v3"));
        if (mutation == "surrogate")
        {
            json = json.Replace("Source claim", "\\uD800", StringComparison.Ordinal);
            Assert.Equal(ProjectError.InvalidJson, (await Reject(json)).Error);
        }
        else
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var position = json.IndexOf("Source claim", StringComparison.Ordinal);
            // Work from encoded bytes because the preceding fixture includes multibyte Unicode.
            position = System.Text.Encoding.UTF8.GetByteCount(json.AsSpan(0, position));
            bytes[position] = 0xff;
            await File.WriteAllBytesAsync(files.FilePath(), bytes);
            Assert.Equal(ProjectError.InvalidJson,
                (await Assert.ThrowsAsync<ProjectFormatException>(() => new ProjectStore().LoadAsync(files.FilePath()))).Error);
        }
    }

    [Theory]
    [InlineData("text-document")]
    [InlineData("instrumental-document")]
    [InlineData("empty-text")]
    [InlineData("cr")]
    [InlineData("negative")]
    [InlineData("over-maximum")]
    [InlineData("end-before-start")]
    [InlineData("unordered-lines")]
    [InlineData("unordered-words")]
    [InlineData("outside-line")]
    [InlineData("provider-date")]
    [InlineData("provider-created")]
    [InlineData("provider-url")]
    [InlineData("manual-identity")]
    [InlineData("duration")]
    public async Task V3StructuralFailuresReportSelectedLyricsDomainIssue(string mutation)
    {
        var root = JsonNode.Parse(await File.ReadAllTextAsync(FixturePath("v3")))!;
        var selected = root["selectedLyrics"]!; var document = selected["document"]!;
        var source = selected["provenance"]!; var line = document["lines"]![0]!; var word = line["words"]![0]!;
        switch (mutation)
        {
            case "text-document": selected["document"] = null; break;
            case "instrumental-document": selected["kind"] = "instrumental"; break;
            case "empty-text": document["text"] = " "; break;
            case "cr": document["text"] = "line\r\n"; break;
            case "negative": line["startSeconds"] = -1; break;
            case "over-maximum": word["startSeconds"] = 14400.1; break;
            case "end-before-start": word["endSeconds"] = 0; break;
            case "unordered-lines": document["lines"]![1]!["startSeconds"] = 0; break;
            case "unordered-words": line["words"]![1]!["startSeconds"] = 0.5; break;
            case "outside-line": word["endSeconds"] = 4; break;
            case "provider-date": source["retrievedAtUtc"] = "2026-10-13T12:34:56+01:00"; break;
            case "provider-created": source["createdAtUtc"] = "2026-10-13T12:34:56Z"; break;
            case "provider-url": source["sourceUrl"] = "https://user:secret@example.invalid/a"; break;
            case "manual-identity": source["origin"] = "manual"; source["sourceId"] = "manual"; break;
            case "duration": source["durationSeconds"] = 0; break;
        }
        var error = await Reject(root.ToJsonString());
        Assert.Equal(ProjectError.InvalidSong, error.Error);
        Assert.Contains(error.Issues, i => i.Code == ValidationCode.InvalidSelectedLyrics && i.Path == "selectedLyrics");
    }

    [Theory]
    [InlineData("lines")]
    [InlineData("words")]
    [InlineData("total-words")]
    [InlineData("text-budget")]
    [InlineData("repeated-budget")]
    [InlineData("selected-budget")]
    public async Task PreflightRejectsOversizedCollectionsAndLogicalBudgets(string mutation)
    {
        var root = JsonNode.Parse(await File.ReadAllTextAsync(FixturePath("v3")))!;
        var selected = root["selectedLyrics"]!; var document = selected["document"]!;
        switch (mutation)
        {
            case "lines": document["lines"] = new JsonArray(Enumerable.Range(0, 10001).Select(_ => (JsonNode?)null).ToArray()); break;
            case "words": document["lines"]![0]!["words"] = new JsonArray(Enumerable.Range(0, 50001).Select(_ => (JsonNode?)null).ToArray()); break;
            case "total-words":
                foreach (var line in document["lines"]!.AsArray())
                    line!["words"] = new JsonArray(Enumerable.Range(0, 25001).Select(_ => (JsonNode?)new JsonObject
                    { ["text"] = "a", ["startSeconds"] = 1, ["endSeconds"] = null }).ToArray());
                break;
            case "text-budget": document["text"] = new string('é', 128 * 1024); break;
            case "repeated-budget": document["text"] = new string('a', 128 * 1024); document["lines"]![0]!["text"] = new string('a', 128 * 1024); break;
            case "selected-budget": selected["provenance"]!["artist"] = new string('é', 256 * 1024); break;
        }
        Assert.Equal(ProjectError.TooLarge, (await Reject(root.ToJsonString())).Error);
    }

    [Fact]
    public async Task LogicalDocumentBudgetBoundaryIsExactForEscapedJsonAndUnicode()
    {
        using var files = new ProjectTestDirectory();
        var value = SelectedLyricsEditingTests.Plain();
        // 64 structure + 6-byte id + repeated UTF8 text. JSON escaping is a different budget.
        value = value with { Document = value.Document! with { Text = new string('é', (256 * 1024 - 70) / 2) } };
        var store = new ProjectStore();
        await store.SaveAsync(files.FilePath(), SongFixture.Create() with { SelectedLyrics = value });
        Assert.True(Core.SelectedLyricsEquality.AreEqual(value, (await store.LoadAsync(files.FilePath())).Song.SelectedLyrics));
        var before = await File.ReadAllBytesAsync(files.FilePath());
        value = value with { Document = value.Document! with { Text = value.Document.Text + "é" } };
        Assert.Equal(ProjectError.InvalidSong,
            (await Assert.ThrowsAsync<ProjectFormatException>(() => store.SaveAsync(files.FilePath(), SongFixture.Create() with { SelectedLyrics = value }))).Error);
        Assert.Equal(before, await File.ReadAllBytesAsync(files.FilePath()));
    }

    [Fact]
    public async Task EveryFrozenSelectedFieldIsRequiredIncludingNullableFields()
    {
        var original = await File.ReadAllTextAsync(FixturePath("v3"));
        var paths = new[]
        {
            "selectedLyrics", "selectedLyrics.document", "selectedLyrics.provenance",
            "selectedLyrics.document.lines.0", "selectedLyrics.document.lines.0.words.0"
        };
        foreach (var path in paths)
        {
            var baseline = JsonNode.Parse(original)!;
            JsonNode target = baseline;
            foreach (var segment in path.Split('.'))
                target = int.TryParse(segment, out var index) ? target[index]! : target[segment]!;
            foreach (var name in target.AsObject().Select(p => p.Key).ToArray())
            {
                var root = JsonNode.Parse(original)!;
                target = root;
                foreach (var segment in path.Split('.'))
                    target = int.TryParse(segment, out var index) ? target[index]! : target[segment]!;
                target.AsObject().Remove(name);
                Assert.Equal(ProjectError.InvalidJson, (await Reject(root.ToJsonString())).Error);
            }
        }
    }

    [Theory]
    [InlineData("selected-type")]
    [InlineData("selected-case")]
    [InlineData("word-missing-text")]
    [InlineData("enum-surrogate")]
    [InlineData("property-surrogate")]
    [InlineData("format-surrogate")]
    public async Task MalformedV3TextAndTypesNeverLeakJsonDomExceptions(string mutation)
    {
        var json = await File.ReadAllTextAsync(FixturePath("v3"));
        switch (mutation)
        {
            case "enum-surrogate": json = json.Replace("\"kind\": \"text\"", "\"kind\": \"\\uD800\"", StringComparison.Ordinal); break;
            case "property-surrogate": json = json.Replace("\"kind\":", "\"\\uD800\":", StringComparison.Ordinal); break;
            case "format-surrogate": json = json.Replace("ultrastar-song-creator", "\\uD800", StringComparison.Ordinal); break;
            default:
                var root = JsonNode.Parse(json)!;
                if (mutation == "selected-type") root["selectedLyrics"] = true;
                else if (mutation == "selected-case")
                { root["SelectedLyrics"] = root["selectedLyrics"]!.DeepClone(); root.AsObject().Remove("selectedLyrics"); }
                else root["selectedLyrics"]!["document"]!["lines"]![0]!["words"]![0]!["text"] = null;
                json = root.ToJsonString(); break;
        }
        Assert.Equal(ProjectError.InvalidJson, (await Reject(json)).Error);
    }

    [Fact]
    public async Task ExplicitClearSavesNullAndCancelledSaveKeepsTheSelectedSnapshot()
    {
        using var files = new ProjectTestDirectory();
        var original = SongFixture.Create() with { SelectedLyrics = SelectedLyricsEditingTests.Word() };
        var store = new ProjectStore();
        await store.SaveAsync(files.FilePath(), original);
        var before = await File.ReadAllBytesAsync(files.FilePath());
        var cleared = new SetSelectedLyrics(null).Apply(original);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(files.FilePath(), cleared, cancellation.Token));
        Assert.Equal(before, await File.ReadAllBytesAsync(files.FilePath()));
        await store.SaveAsync(files.FilePath(), cleared);
        var loaded = await store.LoadAsync(files.FilePath());
        Assert.Null(loaded.Song.SelectedLyrics);
        ProjectRoundTripTests.AssertSameSongValues(cleared, loaded.Song);
        Assert.True(JsonNode.Parse(await File.ReadAllTextAsync(files.FilePath()))!.AsObject().ContainsKey("selectedLyrics"));
    }

    [Fact]
    public async Task InvalidSelectedSavePreservesExistingBytesAndCreatesNoTemporaryFiles()
    {
        using var files = new ProjectTestDirectory();
        var store = new ProjectStore();
        var song = SongFixture.Create() with { SelectedLyrics = SelectedLyricsEditingTests.Word() };
        await store.SaveAsync(files.FilePath(), song);
        var before = await File.ReadAllBytesAsync(files.FilePath());
        var invalid = song.SelectedLyrics! with { Provenance = song.SelectedLyrics!.Provenance with { RetrievedAtUtc = null } };
        var error = await Assert.ThrowsAsync<ProjectFormatException>(() => store.SaveAsync(files.FilePath(), song with { SelectedLyrics = invalid }));
        Assert.Equal(ProjectError.InvalidSong, error.Error);
        Assert.Contains(error.Issues, i => i.Code == ValidationCode.InvalidSelectedLyrics);
        Assert.Equal(before, await File.ReadAllBytesAsync(files.FilePath()));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(files.FilePath())!));
    }

    private static async Task<ProjectFormatException> Reject(string json)
    {
        using var files = new ProjectTestDirectory();
        await File.WriteAllTextAsync(files.FilePath(), json);
        var before = await File.ReadAllBytesAsync(files.FilePath());
        var error = await Assert.ThrowsAsync<ProjectFormatException>(() => new ProjectStore().LoadAsync(files.FilePath()));
        Assert.Equal(before, await File.ReadAllBytesAsync(files.FilePath()));
        return error;
    }
}
