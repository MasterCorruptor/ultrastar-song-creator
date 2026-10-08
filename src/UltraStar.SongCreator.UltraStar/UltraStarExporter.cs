using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.UltraStar;

/// <summary>Renders a copy of current song data; does not mutate the project or open media.</summary>
public sealed partial class UltraStarExporter
{
    private const long MaximumBeat = 9007199254740991;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly Regex DecimalNumber = new(@"^[0-9]+([.,][0-9]+)?$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly string[] AssetHeaders = ["COVER", "BACKGROUND", "VOCALS", "INSTRUMENTAL"];
    private static readonly HashSet<string> ManagedHeaders = new(
        ["VERSION", "TITLE", "ARTIST", "LANGUAGE", "BPM", "GAP", "VIDEOGAP", "MP3", "AUDIO", "VIDEO", "ENCODING", "RELATIVE"],
        StringComparer.OrdinalIgnoreCase);
    private readonly int maximumBytes;

    public UltraStarExporter(int maximumBytes = UltraStarImporter.DefaultMaximumBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        this.maximumBytes = maximumBytes;
    }

    public ExportResult Render(Song song, ExportOptions options) => Render(song, options, null);

    private ExportResult Render(Song song, ExportOptions options, IReadOnlyDictionary<string, string>? packagedReferences)
    {
        ArgumentNullException.ThrowIfNull(song);
        ArgumentNullException.ThrowIfNull(options);
        var diagnostics = ImmutableArray.CreateBuilder<ExportDiagnostic>();
        var notes = ImmutableArray.CreateBuilder<QuantizedNote>();
        void Add(ExportCode code, string path, string message, Guid? id = null, ValidationSeverity severity = ValidationSeverity.Error) =>
            diagnostics.Add(new(severity, code, path, id, message));
        bool Errors() => diagnostics.Any(d => d.Severity == ValidationSeverity.Error);
        ExportResult Result(string? text = null) => new(text, diagnostics.ToImmutable(), notes.ToImmutable());

        if (!Enum.IsDefined(options.Format)) Add(ExportCode.InvalidOptions, "Options.Format", "Unknown export format.");
        foreach (var issue in SongValidator.Validate(song))
            Add(issue.Severity == ValidationSeverity.Error ? ExportCode.InvalidSong : ExportCode.DraftWarning,
                issue.Path, issue.Message, issue.EntityId, issue.Severity);
        if (Errors()) return Result();

        var source = song.ImportedSource;
        var headers = source?.Headers ?? [];
        if (source is not null && source.FormatId is not ("ultrastar-v1" or "ultrastar-unversioned"))
            Add(ExportCode.UnsupportedSource, "Song.ImportedSource", "Only UltraStar source headers can be interpreted by this adapter.");

        double? sourceGrid = null;
        var bpms = headers.Where(h => h.Name.Equals("BPM", StringComparison.OrdinalIgnoreCase) && h.Value.Length > 0).ToArray();
        if (bpms.Length == 1 && TryDecimal(bpms[0].Value, out var oldGrid) && oldGrid > 0) sourceGrid = oldGrid;
        var grid = options.GridBpm ?? sourceGrid;
        if (grid is null) Add(ExportCode.MissingGrid, "Options.GridBpm", "Provide an explicit quantization BPM for a song without a usable source BPM; musical tempo is not guessed.");
        else if (!double.IsFinite(grid.Value) || grid <= 0)
            Add(ExportCode.InvalidOptions, "Options.GridBpm", "Quantization BPM must be finite and positive.");
        var secondsPerBeat = grid is > 0 ? (60 / grid.Value) / 4 : 0;
        if (grid is not null && (!double.IsFinite(secondsPerBeat) || secondsPerBeat <= 0))
            Add(ExportCode.UnrepresentableTiming, "Options.GridBpm", "Grid cannot be represented as a finite positive time unit.");

        foreach (var (name, value) in new[] { ("TITLE", song.Metadata.Title), ("ARTIST", song.Metadata.Artist) })
        {
            if (string.IsNullOrWhiteSpace(value)) Add(ExportCode.MissingMetadata, $"Song.Metadata.{name}", "Title and artist are required for export.");
            else if (value != value.Trim()) Add(ExportCode.InvalidHeader, $"Song.Metadata.{name}", "Leading/trailing metadata whitespace cannot survive the format's header semantics.");
        }
        if (song.Metadata.Language is { Length: > 0 } language && language != language.Trim())
            Add(ExportCode.InvalidHeader, "Song.Metadata.Language", "Leading/trailing language whitespace cannot be preserved.");
        if (song.AudioOffsetSeconds < 0 || !double.IsFinite(song.AudioOffsetSeconds * 1000) ||
            !double.IsFinite(song.AudioOffsetSeconds - song.VideoOffsetSeconds))
            Add(ExportCode.InvalidOffset, "Song.Offsets", "GAP must be nonnegative and millisecond/video offsets finite; no timeline shift is guessed.");

        var audio = song.Media.Where(m => m.Kind == MediaKind.Audio).ToArray();
        var video = song.Media.Where(m => m.Kind == MediaKind.Video).ToArray();
        if (audio.Length == 0 || audio.Any(m => string.IsNullOrWhiteSpace(m.Location)))
            Add(ExportCode.MissingMedia, "Song.Media", "One nonempty main audio reference is required.");
        if (audio.Length > 1 || video.Length > 1)
            Add(ExportCode.AmbiguousMedia, "Song.Media", "Export requires exactly one main audio and at most one video; no reference is selected implicitly.");
        if (video.Any(m => string.IsNullOrWhiteSpace(m.Location)))
            Add(ExportCode.MissingMedia, "Song.Media", "Video reference is empty.");
        foreach (var media in song.Media)
            if (media.Location is not null && media.Location != media.Location.Trim())
                Add(ExportCode.InvalidHeader, "Song.Media", "Leading/trailing filename whitespace cannot survive header semantics.", media.Id);
        if (song.Phrases.Length == 0 || song.Phrases.All(p => p.Notes.Length == 0))
            Add(ExportCode.MissingNotes, "Song.Phrases", "A playable export requires at least one note.");

        foreach (var header in headers)
        {
            if (header.Name != header.Name.Trim() || !HeaderSafe(header.Name, header.Value))
                Add(ExportCode.InvalidHeader, "Song.ImportedSource.Headers", "Source header contains invalid name, line break, control character or Unicode.");
            if (header.Value.Length > 0 && (header.Name.Equals("DUETSINGERP2", StringComparison.OrdinalIgnoreCase) ||
                (header.Name.Length == 2 && char.ToUpperInvariant(header.Name[0]) == 'P' && header.Name[1] is >= '2' and <= '9')))
                Add(ExportCode.UnsupportedDuet, "Song.ImportedSource.Headers", "Duet metadata cannot be exported as a single voice.");
        }
        foreach (var name in AssetHeaders)
            if (headers.Count(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && h.Value.Length > 0) > 1)
                Add(ExportCode.InvalidHeader, "Song.ImportedSource.Headers", $"Duplicate nonempty #{name} references are ambiguous.");
        if (Errors()) return Result();

        long? Beat(double seconds, string path, Guid? id)
        {
            var coordinate = seconds / secondsPerBeat;
            if (!double.IsFinite(coordinate) || coordinate < 0 || coordinate > MaximumBeat)
            {
                Add(ExportCode.UnrepresentableTiming, path, "Time cannot be represented within the exact nonnegative beat range.", id);
                return null;
            }
            return (long)Math.Round(coordinate, MidpointRounding.AwayFromZero);
        }

        var body = new StringBuilder();
        for (var pi = 0; pi < song.Phrases.Length; pi++)
        {
            var phrase = song.Phrases[pi];
            if (phrase.Notes.Length == 0)
            {
                Add(ExportCode.EmptyPhrase, $"Song.Phrases[{pi}]", "An empty phrase cannot be represented without silently removing it.", phrase.Id);
                continue;
            }
            long lastEnd = 0;
            foreach (var note in phrase.Notes)
            {
                var path = $"Song.Phrases[{pi}].Notes";
                if (string.IsNullOrEmpty(note.Text) || note.Text.Any(c => c < ' ') || !UnicodeSafe(note.Text))
                {
                    Add(ExportCode.InvalidText, path, "Each exported note needs text without control characters or invalid Unicode.", note.Id);
                    continue;
                }
                var start = Beat(note.StartSeconds, path, note.Id);
                var end = Beat(note.EndSeconds, path, note.Id);
                if (start is null || end is null) continue;
                if (end <= start)
                {
                    Add(ExportCode.CollapsedNote, path, "Automatic endpoint rounding collapses this note; it is not lengthened or deleted silently.", note.Id);
                    continue;
                }
                var quantized = new QuantizedNote(note.Id, start.Value, end.Value, note.StartSeconds, note.EndSeconds,
                    start.Value * secondsPerBeat, start.Value * secondsPerBeat + (end.Value - start.Value) * secondsPerBeat,
                    note.DurationSeconds, (end.Value - start.Value) * secondsPerBeat);
                if (!double.IsFinite(quantized.ExportedEndSeconds) || quantized.ExportedEndSeconds <= quantized.ExportedStartSeconds)
                {
                    Add(ExportCode.UnrepresentableTiming, path, "Rounded interval overflows or loses its duration.", note.Id);
                    continue;
                }
                notes.Add(quantized);
                if (!FloatingEquivalent(note.StartSeconds, quantized.ExportedStartSeconds, secondsPerBeat) ||
                    !FloatingEquivalent(note.EndSeconds, quantized.ExportedEndSeconds, secondsPerBeat))
                    Add(ExportCode.TimingRounded, path, "Endpoints rounded to the nearest grid beats; exact deltas are in the note report.", note.Id, ValidationSeverity.Warning);
                var type = note.Type switch
                {
                    NoteType.Normal => ':', NoteType.Golden => '*', NoteType.Freestyle => 'F',
                    NoteType.Rap => 'R', NoteType.GoldenRap => 'G', _ => throw new InvalidOperationException("Validated note type required.")
                };
                var pitched = note.Type is NoteType.Normal or NoteType.Golden;
                if (!pitched && note.MidiPitch != 60)
                    Add(ExportCode.NonScoringPitchNormalized, path, "Freestyle/rap pitch has no scoring semantics and is emitted as 0.", note.Id, ValidationSeverity.Warning);
                body.Append(type).Append(' ').Append(start.Value.ToString(CultureInfo.InvariantCulture)).Append(' ')
                    .Append((end.Value - start.Value).ToString(CultureInfo.InvariantCulture)).Append(' ')
                    .Append((pitched ? note.MidiPitch - 60 : 0).ToString(CultureInfo.InvariantCulture)).Append(' ').Append(note.Text).Append('\n');
                lastEnd = Math.Max(lastEnd, end.Value);
            }
            if (phrase.StartSeconds != phrase.Notes.Min(n => n.StartSeconds) || phrase.EndSeconds != phrase.Notes.Max(n => n.EndSeconds))
                Add(ExportCode.PhraseBoundsDerived, $"Song.Phrases[{pi}]", "Import derives phrase bounds from its notes; wider editable bounds are project-only.", phrase.Id, ValidationSeverity.Warning);
            if (pi < song.Phrases.Length - 1) body.Append("- ").Append(lastEnd.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }
        if (song.Analysis.Artifacts.Length > 0 || song.Phrases.SelectMany(p => p.Notes).Any(n => n.Confidence is not null || n.AnalysisReferences.Length > 0))
            Add(ExportCode.ProjectOnlyData, "Song.Analysis", "Analysis/confidence stay in the project; the UltraStar text format does not serialize them.", severity: ValidationSeverity.Warning);
        if (Errors()) return Result();

        string Reference(string name, string original) =>
            packagedReferences is not null && packagedReferences.TryGetValue(name, out var value) ? value : original;
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TITLE"] = song.Metadata.Title, ["ARTIST"] = song.Metadata.Artist, ["BPM"] = DecimalText(grid!.Value),
            ["GAP"] = DecimalText(song.AudioOffsetSeconds * 1000), ["MP3"] = Reference("MP3", audio[0].Location)
        };
        if (song.Metadata.Language is { Length: > 0 } lang) values["LANGUAGE"] = lang;
        if (video.Length == 1)
        {
            values["VIDEO"] = Reference("VIDEO", video[0].Location);
            values["VIDEOGAP"] = DecimalText(song.AudioOffsetSeconds - song.VideoOffsetSeconds);
        }
        if (headers.Any(h => h.Name.Equals("AUDIO", StringComparison.OrdinalIgnoreCase)))
            values["AUDIO"] = values["MP3"];
        if (options.Format == ExportFormat.Unversioned)
        {
            if (headers.Any(h => h.Name.Equals("RELATIVE", StringComparison.OrdinalIgnoreCase))) values["RELATIVE"] = "NO";
            if (headers.Any(h => h.Name.Equals("ENCODING", StringComparison.OrdinalIgnoreCase))) values["ENCODING"] = "UTF-8";
        }

        var output = new StringBuilder();
        if (options.Format == ExportFormat.V1) output.Append("#VERSION:1.0.0\n");
        var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceRelative = headers.Any(h => h.Name.Equals("RELATIVE", StringComparison.OrdinalIgnoreCase) &&
            h.Value.Equals("YES", StringComparison.OrdinalIgnoreCase));
        foreach (var header in headers)
        {
            if (ManagedHeaders.Contains(header.Name))
            {
                if (values.TryGetValue(header.Name, out var current) && emitted.Add(header.Name))
                    output.Append('#').Append(header.Name.ToUpperInvariant()).Append(':').Append(current).Append('\n');
                if (!values.TryGetValue(header.Name, out var replacement) || replacement != header.Value)
                    Add(ExportCode.HeaderRegenerated, $"Header.{header.Name}", "Source header is regenerated/omitted according to current data and the selected output profile.", severity: ValidationSeverity.Warning);
                continue;
            }
            var value = Reference(header.Name, header.Value);
            if (header.Name.Equals("MEDLEYSTARTBEAT", StringComparison.OrdinalIgnoreCase) ||
                header.Name.Equals("MEDLEYENDBEAT", StringComparison.OrdinalIgnoreCase))
            {
                if (value.Length > 0)
                {
                    if (sourceRelative || sourceGrid is null || !long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var oldBeat) ||
                        oldBeat < 0 || oldBeat > MaximumBeat)
                    {
                        Add(ExportCode.InvalidMedley, $"Header.{header.Name}", "Medley beats require an unambiguous absolute source grid; relative/invalid cues are not guessed.");
                        continue;
                    }
                    var seconds = oldBeat * ((60 / sourceGrid.Value) / 4);
                    var rebased = Beat(seconds, $"Header.{header.Name}", null);
                    if (rebased is null) continue;
                    value = rebased.Value.ToString(CultureInfo.InvariantCulture);
                    if (!FloatingEquivalent(seconds, rebased.Value * secondsPerBeat, secondsPerBeat))
                        Add(ExportCode.TimingRounded, $"Header.{header.Name}", "Medley cue rounded to the selected export grid.", severity: ValidationSeverity.Warning);
                }
            }
            if (header.Value.Length > 0 &&
                new[] { "START", "END", "PREVIEWSTART" }.Contains(header.Name, StringComparer.OrdinalIgnoreCase) && !TryDecimal(header.Value, out _))
                Add(ExportCode.InvalidHeader, $"Header.{header.Name}", "Playback range/preview must be a finite nonnegative decimal.");
            output.Append('#').Append(header.Name).Append(':').Append(value).Append('\n');
        }
        foreach (var pair in values)
            if (emitted.Add(pair.Key)) output.Append('#').Append(pair.Key).Append(':').Append(pair.Value).Append('\n');
        foreach (var pair in values)
            if (!HeaderSafe(pair.Key, pair.Value))
                Add(ExportCode.InvalidHeader, $"Header.{pair.Key}", "Current metadata/media cannot be emitted as a single valid header.");
        if (Errors()) return Result();
        output.Append(body).Append("E\n");
        var rendered = output.ToString();
        if (Utf8.GetByteCount(rendered) > maximumBytes)
        {
            Add(ExportCode.OutputTooLarge, "Output", "Text exceeds the configured byte limit.");
            return Result();
        }
        return Result(rendered);
    }

    private static bool TryDecimal(string value, out double number)
    {
        number = 0;
        return DecimalNumber.IsMatch(value) && double.TryParse(value.Replace(',', '.'),
            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number) && double.IsFinite(number);
    }

    private static bool UnicodeSafe(string value)
    {
        try { Utf8.GetByteCount(value); return true; }
        catch (EncoderFallbackException) { return false; }
    }

    private static bool HeaderSafe(string name, string value) =>
        !string.IsNullOrWhiteSpace(name) && !name.Contains(':') &&
        !name.Any(c => c < ' ' && c != '\t') && !value.Any(c => c < ' ' && c != '\t') &&
        UnicodeSafe(name) && UnicodeSafe(value);

    private static bool FloatingEquivalent(double original, double represented, double secondsPerBeat)
    {
        var noise = 8 * Math.Max(Math.Abs(Math.BitIncrement(original) - original), Math.Abs(Math.BitIncrement(represented) - represented));
        return Math.Abs(original - represented) <= Math.Min(noise, secondsPerBeat * 1e-7);
    }

    private static string DecimalText(double number)
    {
        if (number == 0) return "0";
        var raw = number.ToString("R", CultureInfo.InvariantCulture);
        var exponentAt = raw.IndexOf('E');
        if (exponentAt < 0) return raw;
        var negative = raw.StartsWith('-');
        var mantissa = raw[(negative ? 1 : 0)..exponentAt];
        var point = mantissa.IndexOf('.');
        var digits = mantissa.Replace(".", "");
        var position = (point < 0 ? mantissa.Length : point) + int.Parse(raw[(exponentAt + 1)..], CultureInfo.InvariantCulture);
        var expanded = position <= 0 ? "0." + new string('0', -position) + digits :
            position >= digits.Length ? digits + new string('0', position - digits.Length) :
            digits[..position] + "." + digits[position..];
        return negative ? "-" + expanded : expanded;
    }
}
