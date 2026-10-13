using UltraStar.SongCreator.Acquisition.Lyrics;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class LrcParserTests
{
    private static LrcParseResult Success(string text)
    {
        var r = LrcParser.Parse(text); Assert.Equal(LrcParseStatus.Success, r.Status); Assert.NotNull(r.Document);
        LyricsValidation.Document(r.Document); Assert.Equal(LyricsSyncLevel.Line, r.Document.SyncLevel);
        Assert.Equal(LyricsTimeReference.LyricsSourceSeconds, r.Document.TimeReference);
        Assert.All(r.Document.Lines, l => { Assert.Null(l.EndSeconds); Assert.Empty(l.Words); }); return r;
    }
    [Theory]
    [InlineData("[0:0]Egen", 0)]
    [InlineData("[00:00]Egen", 0)]
    [InlineData("[1:02]Egen", 62)]
    [InlineData("[01:02.1]Egen", 62.1)]
    [InlineData("[01:02.12]Egen", 62.12)]
    [InlineData("[01:02.123]Egen", 62.123)]
    [InlineData("[01:02:1]Egen", 62.1)]
    [InlineData("[01:02:12]Egen", 62.12)]
    [InlineData("[01:02:123]Egen", 62.123)]
    [InlineData("[1:02:03]Egen", 62.03)] // explicitly colon-fraction, not an hours interpretation
    [InlineData("[240:00]Egen", 14400)]
    [InlineData("[240:00.000]Egen", 14400)]
    public void AsciiMinuteSecondFractionsAndInclusiveSourceBounds(string input, double expected) =>
        Assert.Equal(expected, Assert.Single(Success(input).Document!.Lines).StartSeconds, 9);

    [Fact]
    public void MultiTagsExpandAndSortStablyButProjectedTextKeepsPhysicalOrderAndNoDuplicatesDisappear()
    {
        var r = Success("[00:03][00:01]  Sen tekst  \n[00:01]Tidlig\n[00:01]Tidlig\n");
        Assert.Equal("  Sen tekst  \nTidlig\nTidlig\n", r.Document!.Text);
        Assert.Equal(new[] { 1d, 1d, 1d, 3d }, r.Document.Lines.Select(l => l.StartSeconds));
        Assert.Equal(new[] { "  Sen tekst  ", "Tidlig", "Tidlig", "  Sen tekst  " }, r.Document.Lines.Select(l => l.Text));
        Assert.Equal("[00:03][00:01]  Sen tekst  \n[00:01]Tidlig\n[00:01]Tidlig\n", r.NormalizedLrc);
    }
    [Fact]
    public void NormalizationPreservesUnicodeBlankPhysicalTimedLinesAndTrailingSpacesAndLines()
    {
        var r = Success("\r\n[ar: Egen artist ]\r[00:02] æøå 🎵  \r\n[00:01]\r\n  \r\n");
        Assert.Equal("\n æøå 🎵  \n\n  \n", r.Document!.Text);
        Assert.Equal("", r.Document.Lines[0].Text); Assert.Equal(" æøå 🎵  ", r.Document.Lines[1].Text);
        Assert.DoesNotContain("\r", r.NormalizedLrc); Assert.Equal(new LrcMetadata("ar", " Egen artist ", 2), Assert.Single(r.Metadata));
    }
    [Fact]
    public void KnownMetadataIsPreservedWithoutOverwritingLyricsOrInferringInstrumental()
    {
        var names = new[] { "ar", "ti", "al", "by", "re", "ve", "au", "length" };
        var raw = string.Join("\n", names.Select(n => "[" + n + ":instrumental]")) + "\n[ar:other]\n[00:01]Egen";
        var r = Success(raw);
        Assert.Equal(9, r.Metadata.Length); Assert.Equal(names, r.Metadata.Take(8).Select(m => m.Name));
        Assert.Equal("other", r.Metadata[8].Value); Assert.Equal("Egen", r.Document!.Text);
        Assert.Equal(LrcParseStatus.NoLyrics, LrcParser.Parse("[au:instrumental]").Status);
    }
    [Fact]
    public void LiteralBracketsRemainLyricsAndMalformedAdditionalNumericTimestampDoesNotDisappear()
    {
        Assert.Equal("[chorus] [123] tekst: egen", Success("[00:01][chorus] [123] tekst: egen").Document!.Text);
        Assert.Equal("[123] tekst: egen", Success("[00:01][123] tekst: egen").Document!.Text);
        Assert.Equal("[ar:Egen]", Success("[00:01][ar:Egen]").Document!.Text);
        Assert.Equal("[unfinished", Success("[00:01][unfinished").Document!.Text);
        Assert.Equal(LrcParseStatus.MalformedInput, LrcParser.Parse("[00:01][00:xx]Egen").Status);
        Assert.Equal(LrcParseStatus.MalformedInput, LrcParser.Parse("[00:01][00:02").Status);
    }
    [Theory]
    [InlineData("[offset:0]")]
    [InlineData("[offset:+0]")]
    [InlineData("[offset:-0]")]
    public void ExplicitZeroOffsetIsPreservedAndDoesNotChangeSourceTime(string offset)
    {
        var r = Success(offset + "\n[00:01]Egen");
        Assert.Equal(1, r.Document!.Lines[0].StartSeconds);
        Assert.Equal("offset", Assert.Single(r.Metadata).Name);
        Assert.Equal(new LrcDiagnostic(LrcDiagnosticCode.ExplicitZeroOffset, 1), Assert.Single(r.Diagnostics));
    }
    [Theory]
    [InlineData("[offset:1]", LrcParseStatus.UnsupportedFormat, LrcDiagnosticCode.UnsupportedOffset)]
    [InlineData("[offset:-1]", LrcParseStatus.UnsupportedFormat, LrcDiagnosticCode.UnsupportedOffset)]
    [InlineData("[offset:2147483647]", LrcParseStatus.UnsupportedFormat, LrcDiagnosticCode.UnsupportedOffset)]
    [InlineData("[offset:2147483648]", LrcParseStatus.MalformedInput, LrcDiagnosticCode.InvalidOffset)]
    [InlineData("[offset: 0]", LrcParseStatus.MalformedInput, LrcDiagnosticCode.InvalidOffset)]
    [InlineData("[offset:NaN]", LrcParseStatus.MalformedInput, LrcDiagnosticCode.InvalidOffset)]
    [InlineData("[offset:0.0]", LrcParseStatus.MalformedInput, LrcDiagnosticCode.InvalidOffset)]
    [InlineData("[offset:]", LrcParseStatus.MalformedInput, LrcDiagnosticCode.InvalidOffset)]
    [InlineData("[offset:0]\n[offset:0]", LrcParseStatus.MalformedInput, LrcDiagnosticCode.DuplicateOffset)]
    [InlineData("[offset:1]\n[offset:1]", LrcParseStatus.MalformedInput, LrcDiagnosticCode.DuplicateOffset)]
    [InlineData("[offset:-1]\n[offset:0]", LrcParseStatus.MalformedInput, LrcDiagnosticCode.DuplicateOffset)]
    public void UnsupportedMalformedAndDuplicateOffsetsAreDistinct(string prefix, LrcParseStatus status, LrcDiagnosticCode code)
    {
        var r = LrcParser.Parse(prefix + "\n[00:01]Egen");
        Assert.Equal(status, r.Status); Assert.Null(r.Document); Assert.Equal(code, r.Diagnostics.Last().Code);
        Assert.Equal(prefix + "\n[00:01]Egen", r.NormalizedLrc);
    }
    [Theory]
    [InlineData("[00:60]Egen", LrcParseStatus.MalformedInput)]
    [InlineData("[00:001]Egen", LrcParseStatus.MalformedInput)]
    [InlineData("[00:01,5]Egen", LrcParseStatus.MalformedInput)]
    [InlineData("[00:01.]Egen", LrcParseStatus.MalformedInput)]
    [InlineData("[00:01.1234]Egen", LrcParseStatus.MalformedInput)]
    [InlineData("[-1:00]Egen", LrcParseStatus.MalformedInput)]
    [InlineData("[+1:00]Egen", LrcParseStatus.MalformedInput)]
    [InlineData("[NaN:00]Egen", LrcParseStatus.MalformedInput)]
    [InlineData("[Infinity:00]Egen", LrcParseStatus.MalformedInput)]
    [InlineData("[240:00.001]Egen", LrcParseStatus.TimingOutOfRange)]
    [InlineData("[241:00]Egen", LrcParseStatus.TimingOutOfRange)]
    [InlineData("[999999999999999999999:00]Egen", LrcParseStatus.TimingOutOfRange)]
    [InlineData("[01:02:03.456]Egen", LrcParseStatus.UnsupportedFormat)]
    [InlineData("[01h:02:03]Egen", LrcParseStatus.UnsupportedFormat)]
    [InlineData("[00:01:02:03]Egen", LrcParseStatus.UnsupportedFormat)]
    [InlineData("[00:01]Hei<00:01.1>deg", LrcParseStatus.UnsupportedFormat)]
    [InlineData("[00:01]Egen\nUstemplet tekst", LrcParseStatus.UnsupportedFormat)]
    [InlineData("[unknown:x]\n[00:01]Egen", LrcParseStatus.UnsupportedFormat)]
    [InlineData(" [00:01]Egen", LrcParseStatus.UnsupportedFormat)]
    [InlineData("[٠٠:01]Egen", LrcParseStatus.UnsupportedFormat)]
    [InlineData("lyrics: egen", LrcParseStatus.UnsupportedFormat)]
    [InlineData("<tt>egen</tt>", LrcParseStatus.UnsupportedFormat)]
    public void InvalidOrUnsupportedInputCannotBecomeFalseSynchronizedOrPlainLyrics(string raw, LrcParseStatus status)
    {
        var r = LrcParser.Parse(raw); Assert.Equal(status, r.Status); Assert.Null(r.Document);
        Assert.NotEmpty(r.Diagnostics); Assert.InRange(r.Diagnostics.Last().LineNumber!.Value, 1, raw.Split('\n').Length);
    }
    [Theory]
    [InlineData("")]
    [InlineData(" \r\n\r")]
    [InlineData("[ar:artist]\n[ti:title]")]
    [InlineData("[00:01] \n[00:02]")]
    public void EmptyMetadataOrWhitespaceLyricsHaveNoSyncDocument(string raw)
    {
        var r = LrcParser.Parse(raw); Assert.Equal(LrcParseStatus.NoLyrics, r.Status); Assert.Null(r.Document);
    }
    [Fact]
    public void InvalidUnicodeIsConstructedWithoutXunitSerializationReplacement()
    {
        foreach (var code in new[] { 0xd800, 0xdc00 })
        {
            var r = LrcParser.Parse("[00:01]" + new string((char)code, 1));
            Assert.Equal(LrcParseStatus.MalformedInput, r.Status); Assert.Null(r.NormalizedLrc); Assert.Null(r.Document);
            Assert.Equal(LrcDiagnosticCode.InvalidUnicode, Assert.Single(r.Diagnostics).Code);
        }
        Assert.Throws<ArgumentNullException>(() => LrcParser.Parse(null!));
    }
    [Fact]
    public void InputAndNormalizedDocumentBudgetsCountUtf8BytesAndEveryExpansion()
    {
        var text = new string('é', (LyricsValidation.MaxDocumentBytes - 120) / 4); // doc72 + line48 + both UTF8 text copies
        Assert.Equal(text, Success("[0:0]" + text).Document!.Text);
        Assert.Equal(LrcParseStatus.TooLarge, LrcParser.Parse("[0:0]" + text + "é").Status);
        Assert.Equal(LrcParseStatus.TooLarge, LrcParser.Parse(new string('a', LrcParser.MaxInputBytes + 1)).Status);
        Assert.Equal(LrcParseStatus.TooLarge, LrcParser.Parse(new string('é', LrcParser.MaxInputBytes / 2 + 1)).Status);
        Assert.Equal(LrcParseStatus.TooLarge, LrcParser.Parse(new string('a', LrcParser.MaxInputBytes * 2 + 1)).Status);
        var smallText = new string('a', 10000);
        Assert.Equal(LrcParseStatus.TooLarge, LrcParser.Parse(string.Concat(Enumerable.Repeat("[0:0]", 30)) + smallText).Status);
    }
    [Fact]
    public void HugePrefixCountsPhysicalLinesAndMetadataAreRejectedBeforeExpansion()
    {
        Assert.Equal(LrcParseStatus.TooLarge, LrcParser.Parse(string.Concat(Enumerable.Repeat("[0:0]", 10001)) + "Egen").Status);
        Assert.Equal(LrcParseStatus.TooLarge, LrcParser.Parse(new string('\n', LrcParser.MaxPhysicalLines) + "[0:0]Egen").Status);
        var r = Success(new string('\n', LrcParser.MaxPhysicalLines - 1) + "[0:0]Egen");
        Assert.Equal(LrcParser.MaxPhysicalLines - 1, r.Document!.Text.Count(c => c == '\n'));
        Assert.Equal(LrcParseStatus.TooLarge, LrcParser.Parse(string.Concat(Enumerable.Repeat("[ar:a]\n", 129)) + "[0:0]Egen").Status);
        Success("[ar:" + new string('é', 2048) + "]\n[0:0]Egen");
        Assert.Equal(LrcParseStatus.TooLarge, LrcParser.Parse("[ar:" + new string('é', 2049) + "]\n[0:0]Egen").Status);
    }
}
