using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using UltraStar.SongCreator.Acquisition.Lyrics;
using Core = UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class LrclibJsonTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 14, 4, 0, 0, TimeSpan.Zero);
    private static readonly LyricsQuery Query = new("Egen artist", "Egen tittel", DurationSeconds: 10, Limit: 1);
    private static JsonObject Record(long id = 1) => new() { ["id"] = id, ["instrumental"] = false };
    private static byte[] Bytes(params JsonObject[] entries) => Encoding.UTF8.GetBytes(new JsonArray(entries.Select(e => e.DeepClone()).ToArray()).ToJsonString());
    private static LyricsPage Decode(params JsonObject[] records) => LrclibJson.Parse(Bytes(records), Query, At);
    private static void Failure(byte[] body, LyricsSearchError expected = LyricsSearchError.InvalidResponse)
    {
        var error = Assert.Throws<LyricsSearchException>(() => LrclibJson.Parse(body, Query, At));
        Assert.Equal(expected, error.Error); Assert.Null(error.InnerException);
        Assert.DoesNotContain("secret-payload", error.Message);
    }

    [Fact]
    public void PlainAndLineUnicodeMetadataOriginalQueryReceiptAndProviderOrderArePreserved()
    {
        var a = Record(9); a["name"] = "  Egen æøå 🎵  "; a["trackName"] = "  Egen æøå 🎵  ";
        a["artistName"] = " Artist "; a["albumName"] = " Album "; a["duration"] = 8.25;
        a["plainLyrics"] = "  Hei æøå  \r\nHei\r\n";
        a["syncedLyrics"] = "[0:2][0:1] Hei \r\n[0:1]Igjen\r\n";
        a["hasWordSync"] = true; a["lyricsfile"] = "https://never-fetch.invalid/secret-payload";
        a["unknown"] = new JsonObject { ["anything"] = new JsonArray(1, 2) };
        var b = Record(3); var query = Query with { Limit = 25, VersionHint = "Userhint" };
        var page = LrclibJson.Parse(Bytes(a, b), query, At);
        Assert.Same(query, page.Query); Assert.Equal(new[] { "9", "3" }, page.Candidates.Select(c => c.Id));
        var c = page.Candidates[0]; Assert.Equal(At, c.RetrievedAtUtc); Assert.Equal(At, page.RetrievedAtUtc);
        Assert.Equal("https://lrclib.net/api/get/9", c.SourceUrl); Assert.Equal(a["trackName"]!.GetValue<string>(), c.Title);
        Assert.Equal(" Artist ", c.Artist); Assert.Equal(" Album ", c.Album); Assert.Null(c.Version);
        Assert.Equal(-1.75, c.Match.DurationDeltaSeconds); Assert.Equal(LyricsMatchIndication.Unknown, c.Match.Title);
        Assert.Equal("  Hei æøå  \nHei\n", c.Documents[0].Text);
        var line = c.Documents[1]; Assert.Equal(" Hei \nIgjen\n", line.Text);
        Assert.Equal(new[] { 1d, 1d, 2d }, line.Lines.Select(l => l.StartSeconds));
        Assert.All(line.Lines, l => { Assert.Null(l.EndSeconds); Assert.Empty(l.Words); });
        Assert.True(c.Capabilities.ReportedWordSync); Assert.True(c.Capabilities.ReportedLineSync);
        Assert.Equal(new[] { "lyricsfile-yaml" }, c.Capabilities.UnsupportedFormats.ToArray());
        Assert.DoesNotContain(c.Documents, d => d.SyncLevel == LyricsSyncLevel.Word);
        var selected = SelectedLyricsProjection.FromSelection(new(c, line));
        Assert.Equal(Core.SelectedLyricsSyncLevel.Line, selected.Document!.SyncLevel);
        Assert.Equal(c.RetrievedAtUtc, selected.Provenance.RetrievedAtUtc);
        Assert.DoesNotContain("secret-payload", JsonSerializer.Serialize(c));
    }
    [Fact]
    public void EmptyOptionalLegacyNullAndNameFallbackHaveNoInventedMetadataOrSync()
    {
        Assert.Empty(LrclibJson.Parse(Encoding.UTF8.GetBytes("[]"), Query, At).Candidates);
        var a = Record(); a["name"] = "Egen"; a["trackName"] = null; a["hasWordSync"] = null;
        a["plainLyrics"] = " \r\n "; a["syncedLyrics"] = null; a["lyricsfile"] = "";
        a["duration"] = null; a["albumName"] = null; a["artistName"] = null;
        var c = Assert.Single(Decode(a).Candidates);
        Assert.Equal("Egen", c.Title); Assert.Null(c.Artist); Assert.Null(c.DurationSeconds);
        Assert.Null(c.Capabilities.ReportedWordSync); Assert.Null(c.Capabilities.ReportedLineSync);
        Assert.Equal(LyricsTextStatus.Missing, c.TextStatus); Assert.Empty(c.Documents);
        Assert.Null(c.Match.DurationDeltaSeconds);
    }

    public static IEnumerable<object[]> InvalidRecords()
    {
        foreach (var property in new[] { "id", "instrumental" })
        {
            var absent = Record(); absent.Remove(property); yield return [Bytes(absent)];
            var nulled = Record(); nulled[property] = null; yield return [Bytes(nulled)];
        }
        foreach (var id in new JsonNode[] { JsonValue.Create(0)!, JsonValue.Create(-1)!, JsonValue.Create("1")!, JsonValue.Create(1.5)! })
        { var r = Record(); r["id"] = id; yield return [Bytes(r)]; }
        foreach (var property in new[] { "instrumental", "hasWordSync" })
        { var r = Record(); r[property] = "false"; yield return [Bytes(r)]; }
        foreach (var property in new[] { "name", "trackName", "artistName", "albumName", "plainLyrics", "syncedLyrics", "lyricsfile" })
        { var r = Record(); r[property] = 1; yield return [Bytes(r)]; }
        foreach (var duration in new JsonNode[] { JsonValue.Create(0)!, JsonValue.Create(-1)!, JsonValue.Create(14401)!, JsonValue.Create("10")! })
        { var r = Record(); r["duration"] = duration; yield return [Bytes(r)]; }
        var alias = Record(); alias["name"] = "secret-payload"; alias["trackName"] = "OTHER"; yield return [Bytes(alias)];
    }
    [Theory, MemberData(nameof(InvalidRecords))]
    public void InvalidRequiredFieldsTypesAliasesAndDurationsAreSanitized(byte[] body) => Failure(body);

    [Theory]
    [InlineData("""[{}]""")]
    [InlineData("{}")]
    [InlineData("[null]")]
    [InlineData("[1]")]
    [InlineData("not-json secret-payload")]
    [InlineData("""[{"id":1,"instrumental":false,"id":2}]""")]
    [InlineData("""[{"id":1,"instrumental":false,"unknown":{"x":1,"x":2}}]""")]
    [InlineData("""[{"id":1,"instrumental":false,"duration":1e999}]""")]
    [InlineData("""[{"id":9223372036854775808,"instrumental":false}]""")]
    [InlineData("""[{"id":1.0,"instrumental":false}]""")]
    public void InvalidJsonRootDuplicatesAndNumericFormsAreRejected(string body) => Failure(Encoding.UTF8.GetBytes(body));

    [Theory]
    [InlineData("[00:60]secret-payload", LyricsVariantDiagnosticCode.InvalidSyntax)]
    [InlineData("[241:00]secret-payload", LyricsVariantDiagnosticCode.TimingOutOfRange)]
    [InlineData("[offset:1]\n[0:0]Egen", LyricsVariantDiagnosticCode.UnsupportedFormat)]
    [InlineData("[au:instrumental]", LyricsVariantDiagnosticCode.NoLyrics)]
    public void ValidPlainSurvivesEveryFailedLrcKindWithVisibleTypedDiagnostics(string lrc, LyricsVariantDiagnosticCode expected)
    {
        var r = Record(); r["plainLyrics"] = " Egen tekst "; r["syncedLyrics"] = lrc;
        var c = Assert.Single(Decode(r).Candidates);
        Assert.Equal(LyricsTextStatus.Available, c.TextStatus); Assert.Equal(LyricsSyncLevel.Plain, Assert.Single(c.Documents).SyncLevel);
        var diagnostic = Assert.Single(c.VariantDiagnostics);
        Assert.Equal(expected, diagnostic.Code); Assert.Equal("lrc-simple-line-v1", diagnostic.FormatId);
        Assert.Equal("line-lrc", diagnostic.VariantId); Assert.Equal(LyricsVariantDiagnosticSeverity.Warning, diagnostic.Severity);
        if (expected != LyricsVariantDiagnosticCode.NoLyrics) Assert.Equal(1, diagnostic.PhysicalLineNumber);
    }
    [Fact]
    public void ZeroOffsetIsInfoAndWithoutPlainFailureIsNotSilentlyConvertedEvenWithYaml()
    {
        var valid = Record(); valid["syncedLyrics"] = "[offset:0]\n[0:1] Egen ";
        var c = Assert.Single(Decode(valid).Candidates);
        Assert.Equal(LyricsVariantDiagnosticCode.ExplicitZeroOffset, Assert.Single(c.VariantDiagnostics).Code);
        Assert.Equal(LyricsVariantDiagnosticSeverity.Info, c.VariantDiagnostics[0].Severity);
        Assert.Equal(1, c.Documents[0].Lines[0].StartSeconds);
        foreach (var lrc in new[] { "[00:60]Egen", "[241:00]Egen" })
        { var r = Record(); r["syncedLyrics"] = lrc; r["lyricsfile"] = "opaque yaml"; Failure(Bytes(r)); }
        var noLyrics = Record(); noLyrics["syncedLyrics"] = "[au:instrumental]";
        Assert.Equal(LyricsTextStatus.Missing, Decode(noLyrics).Candidates[0].TextStatus);
    }
    [Fact]
    public void InstrumentalMissingUnsupportedYamlAndWordFlagAreDistinct()
    {
        var instrumental = Record(); instrumental["instrumental"] = true; instrumental["lyricsfile"] = "opaque-yaml";
        var c = Decode(instrumental).Candidates[0]; Assert.Equal(LyricsTextStatus.Instrumental, c.TextStatus); Assert.Empty(c.Documents);
        foreach (var property in new[] { "plainLyrics", "syncedLyrics" })
        { var r = Record(); r["instrumental"] = true; r[property] = "Egen"; Failure(Bytes(r)); }
        var contradiction = Record(); contradiction["instrumental"] = true; contradiction["hasWordSync"] = true; Failure(Bytes(contradiction));
        var missing = Record(); missing["hasWordSync"] = true;
        Assert.Equal(LyricsTextStatus.Missing, Decode(missing).Candidates[0].TextStatus);
        var yaml = Record(); yaml["lyricsfile"] = " "; yaml["hasWordSync"] = true;
        Assert.Equal(LyricsTextStatus.Unsupported, Decode(yaml).Candidates[0].TextStatus);
        Assert.Empty(Decode(yaml).Candidates[0].Documents);
        var unsupported = Record(); unsupported["syncedLyrics"] = "Untimed own text";
        Assert.Equal(LyricsTextStatus.Unsupported, Decode(unsupported).Candidates[0].TextStatus);
    }

    [Fact]
    public void Utf8EscapedLoneSurrogatesAndUnknownPropertyDepthAreRejected()
    {
        Failure([0xff]); Failure([0xc0, 0xaf]);
        var slash = ((char)92).ToString();
        foreach (var code in new[] { "D800", "DC00" })
        {
            var escaped = slash + "u" + code;
            Failure(Encoding.UTF8.GetBytes("""[{"id":1,"instrumental":false,"plainLyrics":"SURROGATE"}]""".Replace("SURROGATE", escaped)));
            Failure(Encoding.UTF8.GetBytes("""[{"id":1,"instrumental":false,"unknown":"SURROGATE"}]""".Replace("SURROGATE", escaped)));
            Failure(Encoding.UTF8.GetBytes("""[{"id":1,"instrumental":false,"SURROGATE":0}]""".Replace("SURROGATE", escaped)));
        }
        var deep = string.Concat(Enumerable.Repeat("""{"x":""", 17)) + "0" + new string('}', 17);
        Failure(Encoding.UTF8.GetBytes("""[{"id":1,"instrumental":false,"unknown":NESTED}]""".Replace("NESTED", deep)));
    }
    [Fact]
    public void AllRecordsDuplicatesAndBudgetAreCheckedBeforeLimitOne()
    {
        var all = Enumerable.Range(1, 20).Select(i => Record(i)).ToArray();
        var page = Decode(all); Assert.Single(page.Candidates); Assert.Equal("1", page.Candidates[0].Id); Assert.Same(Query, page.Query);
        all[19]["duration"] = 0; Failure(Bytes(all)); all[19]["duration"] = null;
        all[19]["id"] = 1; Failure(Bytes(all));
        Failure(Bytes(Enumerable.Range(1, 21).Select(i => Record(i)).ToArray()), LyricsSearchError.ResponseTooLarge);
        var normalizedOverflow = Enumerable.Range(1, 20).Select(i =>
        { var r = Record(i); r["plainLyrics"] = new string('a', 120000); r["syncedLyrics"] = "[0:0][0:1][0:2]" + new string('b', 40000); return r; }).ToArray();
        Assert.True(Bytes(normalizedOverflow).Length < LrclibJson.MaxInputBytes);
        Failure(Bytes(normalizedOverflow), LyricsSearchError.ResponseTooLarge);
    }
    [Fact]
    public void BlankLegacyFieldsLrcExpansionAndMetadataDoNotHideResourceExcess()
    {
        Failure(new byte[LrclibJson.MaxInputBytes + 1], LyricsSearchError.ResponseTooLarge);
        foreach (var field in new[] { "plainLyrics", "syncedLyrics" })
        { var r = Record(); r[field] = new string(' ', LyricsValidation.MaxDocumentBytes + 1); Failure(Bytes(r), LyricsSearchError.ResponseTooLarge); }
        var hugeLrc = Record(); hugeLrc["plainLyrics"] = "Valid own text";
        hugeLrc["syncedLyrics"] = string.Concat(Enumerable.Repeat("[0:0]", 10001)) + "Egen";
        Failure(Bytes(hugeLrc), LyricsSearchError.ResponseTooLarge);
        var largePlain = Record(); largePlain["plainLyrics"] = new string('a', LyricsValidation.MaxDocumentBytes);
        Failure(Bytes(largePlain), LyricsSearchError.ResponseTooLarge);
        var metadata = Record(); metadata["trackName"] = new string('a', 4097);
        Failure(Bytes(metadata), LyricsSearchError.ResponseTooLarge);
    }
    [Fact]
    public void CallerCancellationQueryAndReceiptRemainExplicitCallerContracts()
    {
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => LrclibJson.Parse([0xff], Query, At, stop.Token));
        Assert.Throws<LyricsValidationException>(() => LrclibJson.Parse(Bytes(), Query with { Limit = 0 }, At));
        Assert.Throws<ArgumentOutOfRangeException>(() => LrclibJson.Parse(Bytes(), Query, default));
        Assert.Throws<ArgumentOutOfRangeException>(() => LrclibJson.Parse(Bytes(), Query, At.ToOffset(TimeSpan.FromHours(1))));
        var maxId = Record(long.MaxValue); maxId["duration"] = 14400;
        Assert.Equal(long.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture), Decode(maxId).Candidates[0].Id);
    }
    [Fact]
    public void ExactInputAndPlainDocumentBudgetsAreAcceptedAndNextByteIsRejected()
    {
        var prefix = """[{"id":1,"instrumental":false,"unknown":""" + "\"";
        var suffix = "\"}]";
        var atInputLimit = Encoding.UTF8.GetBytes(prefix + new string('a', LrclibJson.MaxInputBytes - prefix.Length - suffix.Length) + suffix);
        Assert.Equal(LrclibJson.MaxInputBytes, atInputLimit.Length);
        Assert.Equal(LyricsTextStatus.Missing, LrclibJson.Parse(atInputLimit, Query, At).Candidates[0].TextStatus);
        var plain = Record();
        // Document overhead is 64 bytes plus the five-byte "plain" variant identity.
        plain["plainLyrics"] = new string('a', LyricsValidation.MaxDocumentBytes - 69);
        Assert.Equal(LyricsValidation.MaxDocumentBytes - 69, Decode(plain).Candidates[0].Documents[0].Text.Length);
        plain["plainLyrics"] = new string('a', LyricsValidation.MaxDocumentBytes - 68);
        Failure(Bytes(plain), LyricsSearchError.ResponseTooLarge);
    }

    [Fact]
    public void ExactMetadataAndJsonDepthLimitsRemainUsable()
    {
        var record = Record();
        record["trackName"] = new string('æ', 4096);
        Assert.Equal(new string('æ', 4096), Decode(record).Candidates[0].Title);
        // Root array + record + fourteen nested objects = sixteen containers.
        var nested = string.Concat(Enumerable.Repeat("""{"x":""", 14)) + "0" + new string('}', 14);
        var body = """[{"id":1,"instrumental":false,"unknown":NESTED}]""".Replace("NESTED", nested);
        Assert.Single(LrclibJson.Parse(Encoding.UTF8.GetBytes(body), Query, At).Candidates);
    }

    [Fact]
    public void EscapedNamesCannotHideDuplicatesAndValidSurrogatePairsArePreserved()
    {
        Failure(Encoding.UTF8.GetBytes("""[{"id":1,"instrumental":false,"\u0069d":2}]"""));
        Failure(Encoding.UTF8.GetBytes("""[{"id":1,"instrumental":false,"unknown":{"x":1,"\u0078":2}}]"""));
        var body = """[{"id":1,"instrumental":false,"plainLyrics":"Egen \uD83C\uDFB5 \\uD800","unknown":{"\uD83C\uDFB5":"\uD83C\uDFB5"}}]""";
        var candidate = LrclibJson.Parse(Encoding.UTF8.GetBytes(body), Query, At).Candidates[0];
        Assert.Equal("Egen 🎵 \\uD800", candidate.Documents[0].Text);
    }

    [Fact]
    public void RicherUnsupportedVariantsRetainPlainAndLineDocumentsAndDiagnostics()
    {
        var record = Record();
        record["plainLyrics"] = "Egen tekst";
        record["syncedLyrics"] = "[0:1]Egen <0:1.0>tekst";
        record["lyricsfile"] = "opaque-yaml";
        var candidate = Decode(record).Candidates[0];
        Assert.Equal(LyricsSyncLevel.Plain, Assert.Single(candidate.Documents).SyncLevel);
        Assert.Equal(new[] { "lyricsfile-yaml", "lrc-simple-line-v1" }, candidate.Capabilities.UnsupportedFormats.ToArray());
        Assert.Equal(2, candidate.VariantDiagnostics.Length);
        Assert.All(candidate.VariantDiagnostics, d => Assert.Equal(LyricsVariantDiagnosticCode.UnsupportedFormat, d.Code));
        Assert.Null(candidate.VariantDiagnostics[0].VariantId);
        Assert.Null(candidate.VariantDiagnostics[0].PhysicalLineNumber);
        Assert.Equal(1, candidate.VariantDiagnostics[1].PhysicalLineNumber);
        record["syncedLyrics"] = "[0:1]Egen tekst";
        candidate = Decode(record).Candidates[0];
        Assert.Equal(new[] { LyricsSyncLevel.Plain, LyricsSyncLevel.Line }, candidate.Documents.Select(d => d.SyncLevel));
        Assert.Equal("lyricsfile-yaml", Assert.Single(candidate.VariantDiagnostics).FormatId);
    }

    [Fact]
    public void BlankCrLfIsNormalizedBeforeBudgetAndInstrumentalValidation()
    {
        var record = Record();
        record["plainLyrics"] = string.Concat(Enumerable.Repeat("\r\n", LyricsValidation.MaxDocumentBytes));
        record["syncedLyrics"] = " \r\n ";
        record["instrumental"] = true;
        var candidate = Decode(record).Candidates[0];
        Assert.Equal(LyricsTextStatus.Instrumental, candidate.TextStatus);
        Assert.Empty(candidate.Documents);
        Assert.Empty(candidate.VariantDiagnostics);
        record["plainLyrics"] = string.Concat(Enumerable.Repeat("\r\n", LyricsValidation.MaxDocumentBytes + 1));
        Failure(Bytes(record), LyricsSearchError.ResponseTooLarge);
    }

}
