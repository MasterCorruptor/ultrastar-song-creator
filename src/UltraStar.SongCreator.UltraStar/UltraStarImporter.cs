using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.UltraStar;

/// <summary>Single-voice absolute-time import. Does not open referenced media or fetch metadata.</summary>
public sealed class UltraStarImporter
{
    public const int DefaultMaximumBytes = 8 * 1024 * 1024;
    private readonly int maximumBytes;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly Regex Decimal = new(@"^-?[0-9]+([.,][0-9]+)?$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex Version = new(@"^1\.[0-9]+\.[0-9]+$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public UltraStarImporter(int maximumBytes = DefaultMaximumBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        this.maximumBytes = maximumBytes;
    }

    public ImportResult Parse(string text, string? sourceFileReference = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        try
        {
            if (Utf8.GetByteCount(text) > maximumBytes)
                return Failure(ImportCode.InputTooLarge, "Song text exceeds the configured byte limit.");
        }
        catch (EncoderFallbackException)
        {
            return Failure(ImportCode.InvalidEncoding, "Song text contains invalid Unicode.");
        }
        return new Parser(text, sourceFileReference).Parse();
    }

    public async Task<ImportResult> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        cancellationToken.ThrowIfCancellationRequested();
        var absolute = Path.GetFullPath(filePath);
        await using var stream = new FileStream(absolute, FileMode.Open, FileAccess.Read, FileShare.Read,
            81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length > maximumBytes) return Failure(ImportCode.InputTooLarge, "Song file exceeds the configured byte limit.");
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + count > maximumBytes)
                return Failure(ImportCode.InputTooLarge, "Song file exceeds the configured byte limit.");
            buffer.Write(chunk, 0, count);
        }
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = BeforeTrailer(buffer.ToArray());
        // ASCII-compatible headers select explicit legacy encodings before any lyric is decoded.
        var scan = bytes.AsSpan();
        if (scan.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) scan = scan[3..];
        string headerText;
        try { headerText = Utf8.GetString(scan); }
        catch (DecoderFallbackException) { headerText = Encoding.Latin1.GetString(scan); }
        var preliminary = ReadHeaderPairs(headerText);
        var version = preliminary.LastOrDefault(h => h.Name.Equals("VERSION", StringComparison.OrdinalIgnoreCase) && h.Value.Length > 0);
        var declared = preliminary.Where(h => h.Name.Equals("ENCODING", StringComparison.OrdinalIgnoreCase) && h.Value.Length > 0).ToArray();
        Encoding encoding = Utf8;
        if (version is null && declared.Length > 0)
        {
            if (declared.Length > 1)
                return Failure(ImportCode.DuplicateHeader, "Duplicate nonempty #ENCODING headers are ambiguous.", declared[1].Line);
            var name = declared[0].Value.ToUpperInvariant();
            if (name is not ("UTF8" or "UTF-8" or "CP1252" or "CP1250"))
                return Failure(ImportCode.UnsupportedEncoding, "Supported legacy encodings are UTF8, CP1252 and CP1250.", declared[0].Line);
            if (name.StartsWith("CP", StringComparison.Ordinal))
                encoding = CodePagesEncodingProvider.Instance.GetEncoding(name == "CP1252" ? 1252 : 1250,
                    EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)!;
        }
        try
        {
            var input = bytes.AsSpan();
            if (input.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
            {
                if (encoding != Utf8) return Failure(ImportCode.InvalidEncoding, "UTF-8 BOM conflicts with declared legacy encoding.");
                input = input[3..];
            }
            return Parse(encoding.GetString(input), absolute);
        }
        catch (DecoderFallbackException)
        {
            return Failure(ImportCode.InvalidEncoding, "Song file is not valid in its declared encoding; no encoding is guessed.");
        }
    }


    private static byte[] BeforeTrailer(byte[] bytes)
    {
        // E is an ASCII line marker in all supported encodings. The ignored trailer need not decode.
        var start = 0;
        for (var end = 0; end <= bytes.Length; end++)
        {
            if (end < bytes.Length && bytes[end] is not (10 or 13)) continue;
            var left = start;
            var right = end;
            while (left < right && bytes[left] is 32 or 9) left++;
            while (right > left && bytes[right - 1] is 32 or 9) right--;
            if (right - left == 1 && bytes[left] == (byte)'E') return bytes[..end];
            start = end + 1;
        }
        return bytes;
    }

    private static ImportResult Failure(ImportCode code, string message, int? line = null) =>
        new(null, [new(ValidationSeverity.Error, code, line, message)]);

    private sealed record Header(string Name, string Value, int Line);

    private static IEnumerable<(string Text, int Number)> Lines(string input)
    {
        using var reader = new StringReader(input.TrimStart('\uFEFF'));
        var number = 0;
        while (reader.ReadLine() is { } text) yield return (text, ++number);
    }

    private static List<Header> ReadHeaderPairs(string text)
    {
        var headers = new List<Header>();
        foreach (var (line, number) in Lines(text))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith('#')) break;
            var colon = line.IndexOf(':');
            if (colon > 1) headers.Add(new(line[1..colon].Trim(), line[(colon + 1)..].Trim(), number));
        }
        return headers;
    }

    private sealed class Parser(string text, string? sourceFile)
    {
        private const long MaximumExactInteger = 9007199254740991;
        private readonly List<ImportDiagnostic> diagnostics = [];
        private readonly List<Header> originalHeaders = [];
        private readonly Dictionary<string, Header> headers = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<(string Text, int Number)> body = [];
        private readonly List<Phrase> phrases = [];
        private readonly List<Note> phraseNotes = [];
        private readonly Dictionary<Guid, int> noteLines = [];
        private bool versioned;
        private double secondsPerBeat;
        private long previousStart = -1;
        private bool ended;

        private void Add(ImportCode code, int? line, string message, ValidationSeverity severity = ValidationSeverity.Error) =>
            diagnostics.Add(new(severity, code, line, message));

        private string? Value(string name) => headers.TryGetValue(name, out var h) ? h.Value : null;
        private int? HeaderLine(string name) => headers.TryGetValue(name, out var h) ? h.Line : null;
        private bool HasErrors => diagnostics.Any(d => d.Severity == ValidationSeverity.Error);

        public ImportResult Parse()
        {
            ReadSections();
            ReadHeaders();
            if (HasErrors) return new(null, diagnostics.ToImmutableArray());
            foreach (var (line, number) in body)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Trim() == "E") { ended = true; break; }
                ReadBodyLine(line, number);
            }
            ClosePhrase();
            if (!ended) Add(ImportCode.MissingEndMarker, null, "Missing E terminator; EOF is accepted.", ValidationSeverity.Warning);
            if (HasErrors) return new(null, diagnostics.ToImmutableArray());

            var audioOffset = Number("GAP", 0, false) / 1000;
            var videoGap = Number("VIDEOGAP", 0, true);
            var media = new List<MediaReference>
            {
                new() { Kind = MediaKind.Audio, Location = Value("AUDIO") ?? Value("MP3")!, Source = "ultrastar" }
            };
            if (Value("VIDEO") is { } video) media.Add(new() { Kind = MediaKind.Video, Location = video, Source = "ultrastar" });
            var song = new Song
            {
                Metadata = new() { Title = Value("TITLE")!, Artist = Value("ARTIST")!, Language = Value("LANGUAGE") },
                Media = media.ToImmutableArray(),
                AudioOffsetSeconds = audioOffset,
                VideoOffsetSeconds = audioOffset - videoGap,
                // UltraStar BPM is a file quantization base, not reliably the musical tempo.
                BeatsPerMinute = null,
                Phrases = phrases.ToImmutableArray(),
                ImportedSource = new()
                {
                    FormatId = versioned ? "ultrastar-v1" : "ultrastar-unversioned",
                    FileReference = sourceFile,
                    Headers = originalHeaders.Select(h => new SourceHeader(h.Name, h.Value)).ToImmutableArray()
                }
            };
            foreach (var issue in SongValidator.Validate(song))
            {
                int? line = issue.EntityId is { } id && noteLines.TryGetValue(id, out var n) ? n : null;
                var code = issue.Code switch
                {
                    ValidationCode.NoteOverlap => ImportCode.OverlappingNotes,
                    ValidationCode.MissingText => ImportCode.MissingText,
                    _ => ImportCode.InvalidSong
                };
                Add(code, line, issue.Message, issue.Severity);
            }
            return new(HasErrors ? null : song, diagnostics.ToImmutableArray());
        }

        private void ReadSections()
        {
            var inBody = false;
            foreach (var (line, number) in Lines(text))
            {
                if (!inBody && string.IsNullOrWhiteSpace(line)) continue;
                if (!inBody && line.StartsWith('#'))
                {
                    var colon = line.IndexOf(':');
                    if (colon <= 1 || string.IsNullOrWhiteSpace(line[1..colon]))
                        Add(ImportCode.InvalidHeader, number, "Header requires a nonempty name and a colon.");
                    else originalHeaders.Add(new(line[1..colon].Trim(), line[(colon + 1)..].Trim(), number));
                }
                else
                {
                    inBody = true;
                    body.Add((line, number));
                    if (line.Trim() == "E") break; // Never inspect the required-to-be-ignored trailer.
                }
            }
        }

        private void ReadHeaders()
        {
            foreach (var h in originalHeaders.Where(h => h.Value.Length > 0))
            {
                if (!headers.TryAdd(h.Name, h) && IsOperational(h.Name))
                    Add(ImportCode.DuplicateHeader, h.Line, $"Duplicate nonempty #{h.Name} header is ambiguous.");
            }
            if (Value("VERSION") is { } version)
            {
                versioned = true;
                if (!Version.IsMatch(version))
                    Add(ImportCode.UnsupportedVersion, HeaderLine("VERSION"), "Only UltraStar major version 1 is supported; version must be 1.minor.patch.");
            }
            foreach (var name in new[] { "TITLE", "ARTIST", "BPM", "MP3" })
                if (Value(name) is null) Add(ImportCode.MissingHeader, null, $"Missing required #{name}.");
            var bpm = Number("BPM", 0, false);
            if (bpm <= 0) Add(ImportCode.InvalidTiming, HeaderLine("BPM"), "BPM quantization base must be positive.");
            secondsPerBeat = (60 / bpm) / 4;
            if (!double.IsFinite(secondsPerBeat) || secondsPerBeat <= 0)
                Add(ImportCode.InvalidTiming, HeaderLine("BPM"), "BPM cannot be represented as a positive finite time unit.");
            Number("GAP", 0, false);
            Number("VIDEOGAP", 0, true);
            foreach (var range in new[] { "START", "END" })
                if (Value(range) is not null)
                {
                    Number(range, 0, false);
                    Add(ImportCode.RetainedPlaybackRange, HeaderLine(range), $"#{range} is retained as source metadata; no playback range is applied.", ValidationSeverity.Warning);
                }
            if (Value("RELATIVE") is { } relative)
            {
                if (relative.Equals("YES", StringComparison.OrdinalIgnoreCase))
                    Add(ImportCode.UnsupportedRelativeTiming, HeaderLine("RELATIVE"), "Relative timing requires a separately verified adapter; no absolute interpretation is attempted.");
                else if (!relative.Equals("NO", StringComparison.OrdinalIgnoreCase))
                    Add(ImportCode.InvalidHeader, HeaderLine("RELATIVE"), "RELATIVE must be YES or NO.");
            }
            for (var voice = 2; voice <= 9; voice++)
                if (Value($"P{voice}") is not null)
                    Add(ImportCode.UnsupportedDuet, HeaderLine($"P{voice}"), "Multiple-voice headers require the future duet model.");
            if (Value("DUETSINGERP2") is not null)
                Add(ImportCode.UnsupportedDuet, HeaderLine("DUETSINGERP2"), "Duet metadata requires the future duet model.");
            if (!versioned && Value("ENCODING") is { } declared &&
                !new[] { "UTF8", "UTF-8", "CP1252", "CP1250" }.Contains(declared, StringComparer.OrdinalIgnoreCase))
                Add(ImportCode.UnsupportedEncoding, HeaderLine("ENCODING"), "Unsupported explicit legacy encoding.");
        }

        private static bool IsOperational(string name) =>
            new[] { "VERSION", "TITLE", "ARTIST", "BPM", "MP3", "AUDIO", "VIDEO", "GAP", "VIDEOGAP",
                "LANGUAGE", "RELATIVE", "ENCODING", "START", "END", "DUETSINGERP1", "DUETSINGERP2",
                "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9" }.Contains(name, StringComparer.OrdinalIgnoreCase);

        private double Number(string name, double fallback, bool signed)
        {
            if (Value(name) is not { } raw) return fallback;
            if (!Decimal.IsMatch(raw) || (!signed && raw.StartsWith('-')) ||
                !double.TryParse(raw.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
            {
                Add(ImportCode.InvalidHeader, HeaderLine(name), $"#{name} must be a representable {(signed ? "signed" : "nonnegative")} decimal.");
                return fallback;
            }
            return value;
        }

        private bool Whitespace(char c) => versioned ? char.IsWhiteSpace(c) : c is ' ' or '\t';

        private bool Token(string line, ref int position, out string token)
        {
            if (position >= line.Length || !Whitespace(line[position])) { token = ""; return false; }
            while (position < line.Length && Whitespace(line[position])) position++;
            var start = position;
            while (position < line.Length && !Whitespace(line[position])) position++;
            token = line[start..position];
            return token.Length > 0;
        }

        private static bool Integer(string raw, bool signed, out long number)
        {
            var digits = signed && raw.StartsWith('-') ? raw.AsSpan(1) : raw.AsSpan();
            number = 0;
            return digits.Length > 0 && !digits.ContainsAnyExceptInRange('0', '9') &&
                long.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number);
        }

        private void ReadBodyLine(string line, int number)
        {
            if (line.StartsWith('#')) { Add(ImportCode.UnexpectedHeader, number, "Header appears after the body began."); return; }
            if (line.StartsWith('P'))
            {
                var voice = line.TrimEnd();
                if (voice != "P1") Add(ImportCode.UnsupportedDuet, number, "Only the implicit/explicit P1 voice is supported; other voices are not flattened.");
                else if (versioned && Value("P1") is null) Add(ImportCode.MissingHeader, number, "Explicit v1 P1 requires #P1.");
                else if (!versioned && previousStart >= 0 && body.First(b => !string.IsNullOrWhiteSpace(b.Text)).Text.TrimEnd() != "P1")
                    Add(ImportCode.InvalidLine, number, "Unversioned voice declarations must begin the body.");
                return;
            }
            var position = 1;
            if (line.StartsWith('-'))
            {
                if (!Token(line, ref position, out var raw) || !Integer(raw, false, out var marker) ||
                    marker > MaximumExactInteger || line[position..].Any(c => !Whitespace(c)))
                    Add(ImportCode.InvalidLine, number, "Absolute phrase marker requires exactly one nonnegative beat integer.");
                else
                {
                    if (phraseNotes.Count == 0)
                        Add(ImportCode.PhraseMarker, number, "Empty/consecutive phrase marker is ignored.", ValidationSeverity.Warning);
                    else if (marker * secondsPerBeat < phraseNotes.Max(n => n.EndSeconds))
                        Add(ImportCode.PhraseMarker, number, "Phrase marker precedes the last note end; note timing is retained.", ValidationSeverity.Warning);
                    ClosePhrase();
                }
                return;
            }
            var type = line[0] switch
            {
                ':' => NoteType.Normal,
                '*' => NoteType.Golden,
                'F' => NoteType.Freestyle,
                'R' => NoteType.Rap,
                'G' => NoteType.GoldenRap,
                _ => (NoteType?)null
            };
            if (type is null) { Add(ImportCode.InvalidNote, number, "Unknown note type is not silently replaced."); return; }
            if (!Token(line, ref position, out var rawStart) || !Token(line, ref position, out var rawDuration) ||
                !Token(line, ref position, out var rawPitch) || !Integer(rawStart, false, out var start) ||
                !Integer(rawDuration, false, out var duration) || !Integer(rawPitch, true, out var pitch) ||
                start > MaximumExactInteger || duration > MaximumExactInteger - start || duration <= 0)
            {
                Add(ImportCode.InvalidNote, number, "Note requires representable nonnegative start, positive duration and signed integer pitch.");
                return;
            }
            if (position < line.Length && !Whitespace(line[position]))
            {
                Add(ImportCode.InvalidLine, number, "Note text requires a whitespace delimiter.");
                return;
            }
            var lyric = position < line.Length ? line[(position + 1)..] : "";
            if (lyric.Any(c => c < ' ')) { Add(ImportCode.InvalidNote, number, "Note text contains a control character."); return; }
            var pitched = type is NoteType.Normal or NoteType.Golden;
            if (pitched && (pitch < -60 || pitch > 67))
            {
                Add(ImportCode.InvalidPitch, number, "Scored pitch cannot be represented in MIDI 0..127; no clamp/transposition is applied.");
                return;
            }
            if (!pitched && pitch != 0)
                Add(ImportCode.IgnoredPitch, number, "Rap/freestyle pitch has no format semantics; MIDI placeholder 60 is used.", ValidationSeverity.Warning);
            if (start < previousStart)
                Add(ImportCode.UnsortedNotes, number, "Note order is not chronological; source lyric order is retained.", ValidationSeverity.Warning);
            previousStart = start;
            var note = new Note
            {
                StartSeconds = start * secondsPerBeat,
                DurationSeconds = duration * secondsPerBeat,
                MidiPitch = pitched ? (int)pitch + 60 : 60,
                Text = lyric,
                Type = type.Value
            };
            if (!double.IsFinite(note.EndSeconds) || note.DurationSeconds <= 0 || note.EndSeconds <= note.StartSeconds)
            {
                Add(ImportCode.InvalidTiming, number, "Converted note time overflows or loses its positive duration.");
                return;
            }
            phraseNotes.Add(note);
            noteLines.Add(note.Id, number);
        }

        private void ClosePhrase()
        {
            if (phraseNotes.Count == 0) return;
            phrases.Add(new()
            {
                StartSeconds = phraseNotes.Min(n => n.StartSeconds),
                EndSeconds = phraseNotes.Max(n => n.EndSeconds),
                Notes = phraseNotes.ToImmutableArray()
            });
            phraseNotes.Clear();
        }
    }
}
