using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using UltraStar.SongCreator.Acquisition;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Generation;

/// <summary>Pure lexical/in-memory validation, not proof of file content, identity or containment.</summary>
public static class PreprocessingValidation
{
    public const long MaxSourceBytes = 1024L * 1024 * 1024, MaxPcmFileBytes = 64L * 1024 * 1024;
    public const int SampleRate = 16000, MaxBins = 4096, MaxStreams = 256, MaxTags = 1024;
    public const long MaxFrames = SampleRate * 1800L;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    private static void Require(bool condition, PreprocessingError error)
    {
        if (!condition) throw new PreprocessingValidationException(error);
    }
    private static bool Text(string? value, int limit, bool required = true, bool controls = false)
    {
        if (value is null || value.Length > limit || (required && string.IsNullOrWhiteSpace(value))) return false;
        if (!controls && value.Any(char.IsControl)) return false;
        try { _ = Utf8.GetByteCount(value); return true; }
        catch (EncoderFallbackException) { return false; }
    }
    private static bool Hash(string? value) => value?.Length == 64 && value.All(c =>
        c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');
    private static bool OptionalPositive(int? value) => value is null || value > 0;
    private static bool OptionalDuration(double? value) => value is null || (double.IsFinite(value.Value) && value >= 0);
    private static string LocalPath(string? path)
    {
        Require(Text(path, 4096, controls: true), PreprocessingError.InvalidPath);
        try
        {
            Require(Path.IsPathFullyQualified(path!) &&
                !(Uri.TryCreate(path, UriKind.Absolute, out var uri) && !uri.IsFile), PreprocessingError.InvalidPath);
            var result = Path.GetFullPath(path!);
            Require(!string.IsNullOrEmpty(Path.GetFileName(result)), PreprocessingError.InvalidPath);
            return result;
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        { throw new PreprocessingValidationException(PreprocessingError.InvalidPath); }
    }
    private static bool SamePath(string? a, string? b) => string.Equals(LocalPath(a), LocalPath(b),
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    private static void Tags(ImmutableArray<MediaTag> tags)
    {
        Require(!tags.IsDefault && tags.Length <= MaxTags, PreprocessingError.InvalidMedia);
        foreach (var tag in tags)
            Require(tag is not null && Text(tag.Name, 256) && Text(tag.Value, 4096, required: false, controls: true),
                PreprocessingError.InvalidMedia);
    }

    public static void Validate(PreprocessingRequest? request)
    {
        Require(request is not null, PreprocessingError.InvalidIdentity);
        var r = request!;
        Require(r.JobId != Guid.Empty && Text(r.CallerId, 256), PreprocessingError.InvalidIdentity);
        var song = r.OriginalSong;
        Require(song is not null && song.Id != Guid.Empty && double.IsFinite(song.AudioOffsetSeconds) &&
            double.IsFinite(song.VideoOffsetSeconds), PreprocessingError.InvalidSong);
        Require(!song!.Media.IsDefault && r.SourceMediaId != Guid.Empty &&
            song.Media.All(m => m is not null), PreprocessingError.InvalidMedia);
        var media = song.Media.Where(m => m.Id == r.SourceMediaId).ToArray();
        Require(media.Length == 1 && media[0].Kind == MediaKind.Audio, PreprocessingError.InvalidMedia);
        Require(r.Candidate is not null, PreprocessingError.InvalidMedia);
        var candidate = r.Candidate!;
        Require(Text(candidate.SourceId, 256) && candidate.ByteLength > 0, PreprocessingError.InvalidMedia);
        Require(candidate.ByteLength <= MaxSourceBytes, PreprocessingError.LimitExceeded);
        Require(SamePath(media[0].Location, candidate.FilePath), PreprocessingError.InvalidPath);
        var inspection = candidate.Inspection;
        Require(inspection is not null && Text(inspection.Container, 256) &&
            OptionalDuration(inspection.DurationSeconds), PreprocessingError.InvalidMedia);
        Tags(inspection!.Tags);
        Require(!inspection.Streams.IsDefault && inspection.Streams.Length is > 0 and <= MaxStreams,
            PreprocessingError.InvalidStream);
        var ids = new HashSet<int>();
        foreach (var stream in inspection.Streams)
        {
            Require(stream is not null && stream.Index >= 0 && ids.Add(stream.Index) && Text(stream.Kind, 256) &&
                (stream.CodecName is null || Text(stream.CodecName, 256)) &&
                OptionalPositive(stream.SampleRate) && OptionalPositive(stream.Channels) &&
                OptionalPositive(stream.Width) && OptionalPositive(stream.Height) && OptionalDuration(stream.DurationSeconds),
                PreprocessingError.InvalidStream);
            Tags(stream!.Tags);
        }
        var selected = inspection.Streams.Where(s => s.Index == r.AudioStreamIndex).ToArray();
        Require(selected.Length == 1 && selected[0].IsAudio && !selected[0].AttachedPicture,
            PreprocessingError.InvalidStream);
        if (song.ImportedSource is { FormatId: LocalSongDraft.SourceFormatId } source)
        {
            Require(!source.Headers.IsDefault && source.Headers.All(h => h is not null), PreprocessingError.InvalidStream);
            Require(SamePath(source.FileReference, candidate.FilePath), PreprocessingError.InvalidPath);
            var retained = source.Headers.Where(h => h.Name == "selection.audioStream").ToArray();
            Require(retained.Length == 1 && int.TryParse(retained[0].Value, NumberStyles.None, CultureInfo.InvariantCulture,
                out var index) && index == r.AudioStreamIndex, PreprocessingError.InvalidStream);
        }
        Require(r.Lyrics is not null, PreprocessingError.InvalidLyrics);
        try
        {
            Require(SelectedLyricsEquality.AreEqual(song.SelectedLyrics, r.Lyrics), PreprocessingError.InvalidLyrics);
        }
        catch (SelectedLyricsValidationException) { throw new PreprocessingValidationException(PreprocessingError.InvalidLyrics); }
        Require(r.ExpectedSourceSha256 is null || Hash(r.ExpectedSourceSha256), PreprocessingError.InvalidHash);
    }

    public static void Validate(PreprocessingProposal? proposal)
    {
        Require(proposal is not null, PreprocessingError.InvalidIdentity);
        var p = proposal!;
        Validate(p.Request);
        Require(Hash(p.SourceSha256) && (p.Request.ExpectedSourceSha256 is null ||
            string.Equals(p.SourceSha256, p.Request.ExpectedSourceSha256, StringComparison.OrdinalIgnoreCase)),
            PreprocessingError.InvalidHash);
        Require(p.Producer is not null && Text(p.Producer.Name, 256) && Text(p.Producer.Revision, 256),
            PreprocessingError.InvalidProvenance);
        var pcm = p.Pcm;
        Require(pcm is not null && pcm.RelativePath == "analysis.wav" && pcm.SampleRate == SampleRate &&
            pcm.Channels == 1 && pcm.BitsPerSample == 16 && pcm.FrameCount > 0, PreprocessingError.InvalidPcm);
        Require(pcm!.FrameCount <= MaxFrames && pcm.FileBytes <= MaxPcmFileBytes, PreprocessingError.LimitExceeded);
        Require(pcm.FileBytes >= 44 + pcm.FrameCount * 2, PreprocessingError.InvalidPcm);
        Require(Hash(pcm.Sha256), PreprocessingError.InvalidHash);
        Require(OptionalDuration(p.OriginInMediaSeconds), PreprocessingError.InvalidPcm);
        Require(!p.Bins.IsDefault && p.Bins.Length > 0, PreprocessingError.InvalidWaveform);
        Require(p.Bins.Length <= MaxBins, PreprocessingError.LimitExceeded);
        long previous = 0;
        foreach (var bin in p.Bins)
        {
            Require(bin.StartFrame == previous && bin.EndFrame > bin.StartFrame && bin.EndFrame <= pcm.FrameCount &&
                double.IsFinite(bin.Minimum) && double.IsFinite(bin.Maximum) &&
                bin.Minimum >= -1 && bin.Maximum <= 1 && bin.Minimum <= bin.Maximum,
                PreprocessingError.InvalidWaveform);
            previous = bin.EndFrame;
        }
        Require(previous == pcm.FrameCount, PreprocessingError.InvalidWaveform);
    }
}
