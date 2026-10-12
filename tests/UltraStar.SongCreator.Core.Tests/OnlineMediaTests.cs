using System.Collections.Immutable;
using System.Text.Json;
using UltraStar.SongCreator.Acquisition;

namespace UltraStar.SongCreator.Core.Tests;

public sealed class OnlineMediaTests
{
    internal const string Id = "ABCdef12345";
    internal const string Url = "https://www.youtube.com/watch?v=" + Id;
    internal const string Details = """
        {"id":"ABCdef12345","title":"Own title","uploader":"Own uploader","duration":12.5,"is_live":false,"extractor_key":"Youtube",
         "formats":[
          {"format_id":"140","ext":"m4a","acodec":"mp4a.40.2","vcodec":"none","protocol":"https","filesize":null,"abr":128},
          {"format_id":"133","ext":"mp4","acodec":"none","vcodec":"avc1.4d4015","protocol":"https","filesize":null,"width":320,"height":240},
          {"format_id":"251","ext":"webm","acodec":"opus","vcodec":"none","protocol":"https"},
          {"format_id":"18","ext":"mp4","acodec":"mp4a.40.2","vcodec":"avc1","protocol":"https"}
         ],"description":"ignored private/raw text","url":"https://example.test/expiring-secret-url"}
        """;
    internal const string Search = """
        {"entries":[{"id":"ABCdef12345","title":"Own title","duration":12.5,"uploader":"Own uploader","ie_key":"Youtube"},
        null,{"id":"DEFabc67890","title":"Own title","duration":null}]}
        """;
    internal static MediaSourceDetails Page(string body = Details) => YtDlpJson.Details(body, Url, DateTimeOffset.UtcNow);

    internal sealed class Probe : IMediaProbe
    {
        internal bool WrongStreams { get; set; }
        internal bool Fail { get; set; }
        public Task<MediaInspection> InspectAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Fail) throw new MediaAcquisitionException(AcquisitionError.ProbeFailed, "Own fixture failure");
            var audio = Path.GetExtension(path) == ".m4a" && !WrongStreams;
            return Task.FromResult(new MediaInspection("mov,mp4,m4a,3gp,3g2,mj2", 12.5,
                [new(0, audio ? "audio" : "video", audio ? "aac" : "h264", false,
                    audio ? 44100 : null, audio ? 2 : null, audio ? null : 320, audio ? null : 240, 12.5, [])],
                [new("title", "File title"), new("artist", "File artist")]));
        }
    }

    internal sealed class Rig
    {
        internal List<string[]> Calls { get; } = [];
        internal string Body { get; set; } = Details;
        internal string Mode { get; set; } = "";
        internal Probe Probe { get; } = new();
        internal YtDlpMediaSource Source(long maximum = 1024, TimeSpan? timeout = null) =>
            new(Probe, Run, Path.Combine(ProjectTestDirectory.RepositoryRoot, "node-placeholder"),
                "node", TimeSpan.FromSeconds(5), timeout ?? TimeSpan.FromSeconds(5), maximum);

        private async Task<ProbeOutput> Run(IReadOnlyList<string> args, TimeSpan timeout, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Calls.Add(args.ToArray());
            if (args.Contains("--flat-playlist")) return new(0, Search, "");
            var position = args.ToList().IndexOf("--output");
            if (position < 0) return new(0, Body, "");
            var path = args[position + 1];
            if (Mode == "fail-video" && path.EndsWith(".mp4")) return new(7, "", "Own error");
            await File.WriteAllBytesAsync(path, Mode.StartsWith("large") ? new byte[2048] : "Own synthetic media"u8.ToArray(), token);
            if (Mode == "residual") await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(path)!, "unwanted.part"), "Own part", token);
            if (Mode is "hang" or "large-hang") await Task.Delay(Timeout.Infinite, token);
            if (Mode == "race-destination") Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(path)!)!, "download"));
            return new(0, "", "");
        }
    }

    [Theory]
    [InlineData("https://youtu.be/ABCdef12345?t=10")]
    [InlineData("https://www.youtube.com/watch?v=ABCdef12345&list=ignored")]
    [InlineData("https://m.youtube.com/watch?v=ABCdef12345#ignored")]
    [InlineData("https://youtube.com/shorts/ABCdef12345")]
    public void DirectLinksIdentifyOnlyOneConcreteVideo(string input) => Assert.Equal(Url, YtDlpJson.CanonicalUrl(input));

    [Theory]
    [InlineData("http://www.youtube.com/watch?v=ABCdef12345")]
    [InlineData("https://youtube.com.evil.test/watch?v=ABCdef12345")]
    [InlineData("https://user:pass@youtube.com/watch?v=ABCdef12345")]
    [InlineData("https://youtube.com:444/watch?v=ABCdef12345")]
    [InlineData("https://youtube.com/playlist?list=own")]
    [InlineData("https://youtube.com/watch?v=ABCdef12345&v=DEFabc67890")]
    [InlineData("https://youtu.be/ABCdef12345/extra")]
    [InlineData("ytsearch5:own")]
    [InlineData("--exec=unexpected")]
    public void NonVideoAndForeignInputsAreRejected(string input) => Assert.Throws<ArgumentException>(() => YtDlpJson.CanonicalUrl(input));

    [Theory]
    [InlineData("Own artist", null)]
    [InlineData(null, "Own title")]
    [InlineData("Own artist", "Own title")]
    public async Task AllSearchModesAreBoundedAndDoNotDownload(string? artist, string? title)
    {
        var rig = new Rig();
        var page = await rig.Source().SearchAsync(new(artist, title, 3));
        Assert.Equal(2, page.Candidates.Length);
        Assert.Equal(page.Candidates[0].Title, page.Candidates[1].Title);
        Assert.Null(page.Candidates[1].DurationSeconds);
        var args = Assert.Single(rig.Calls);
        Assert.Contains("--flat-playlist", args);
        Assert.DoesNotContain("--output", args);
        Assert.StartsWith("ytsearch3:", args[^1]);
        Assert.Equal("--", args[^2]);
        foreach (var flag in new[] { "--ignore-config", "--no-plugin-dirs", "--no-remote-components", "--no-cookies", "--no-cookies-from-browser", "--no-cache-dir", "--no-update", "--no-playlist" })
            Assert.Contains(flag, args);
        Assert.DoesNotContain("--exec", args);
    }

    [Fact]
    public async Task SpecialTextIsOneArgumentAndInvalidQueriesDoNotCallTool()
    {
        var rig = new Rig();
        var text = "æøå \" ; & | $(literal)";
        await rig.Source().SearchAsync(new(Title: text, Limit: 3));
        Assert.Equal("ytsearch3:" + text, rig.Calls[0][^1]);
        foreach (var query in new[] { new MediaSearchQuery(), new(Title: "Own", Limit: 0), new(Title: "Own", Limit: 26), new(Title: "a\0b") })
            await Assert.ThrowsAnyAsync<ArgumentException>(() => rig.Source().SearchAsync(query));
        Assert.Single(rig.Calls);
    }

    [Fact]
    public void CandidateFormatsPreserveInformationAndRequireExplicitSelection()
    {
        var page = Page();
        Assert.Equal(12.5, page.Candidate.DurationSeconds);
        Assert.Equal("Own uploader", page.Candidate.Uploader);
        Assert.Null(page.Formats[0].ByteLength);
        Assert.Equal(128, page.Formats[0].AudioBitrateKbps);
        var selection = MediaDownloadChoices.Select(page, "140", "133");
        Assert.Equal(320, selection.Video!.Width);
        Assert.Null(MediaDownloadChoices.Select(page, "140").Video);
        Assert.Throws<MediaDownloadException>(() => MediaDownloadChoices.Select(page, "missing"));
        Assert.Equal(MediaDownloadError.UnsupportedFormat, Assert.Throws<MediaDownloadException>(() => MediaDownloadChoices.Select(page, "251")).Error);
        Assert.Throws<MediaDownloadException>(() => MediaDownloadChoices.Select(page, "140", "18"));
        Assert.Throws<MediaDownloadException>(() => MediaDownloadChoices.Validate(selection with { Audio = selection.Audio with { Extension = "webm" } }));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{")]
    [InlineData("""{"id":"invalid","title":"Own","formats":[]}""")]
    [InlineData("""{"id":"ABCdef12345","id":"ABCdef12345","title":"Own","formats":[]}""")]
    [InlineData("""{"id":"ABCdef12345","title":"Own","duration":-1,"formats":[]}""")]
    [InlineData("""{"id":"ABCdef12345","title":"Own","formats":null}""")]
    public void MalformedMetadataHasDistinctError(string body) =>
        Assert.Equal(MediaDownloadError.InvalidData, Assert.Throws<MediaDownloadException>(() => Page(body)).Error);

    [Fact]
    public void IdentityLiveDrmAndPaginationLimitsCannotBeHidden()
    {
        Assert.Throws<MediaDownloadException>(() => Page(Details.Replace(Id, "DEFabc67890")));
        Assert.Equal(MediaDownloadError.SourceUnavailable, Assert.Throws<MediaDownloadException>(() => Page(Details.Replace("\"is_live\":false", "\"is_live\":true"))).Error);
        var drm = Page(Details.Replace("\"abr\":128", "\"abr\":128,\"has_drm\":true"));
        Assert.Throws<MediaDownloadException>(() => MediaDownloadChoices.Select(drm, "140"));
        Assert.Throws<MediaDownloadException>(() => YtDlpJson.Search(Search, new(Title: "Own", Limit: 1), DateTimeOffset.UtcNow));
        Assert.Empty(YtDlpJson.Search("""{"entries":[]}""", new(Title: "Own"), DateTimeOffset.UtcNow).Candidates);
        Assert.Throws<MediaDownloadException>(() => Page(Details.Replace("\"format_id\":\"133\"", "\"format_id\":\"140\"")));
    }

    [Fact]
    public async Task PrecancelDoesNotRunTool()
    {
        var rig = new Rig();
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rig.Source().SearchAsync(new(Title: "Own"), stop.Token));
        Assert.Empty(rig.Calls);
    }
}
