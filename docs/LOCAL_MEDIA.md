# Lokal mediekilde – Fase 3.1

Acquisition er et separat .NET 10-lag med ProjectReference bare til Core og ingen NuGet-runtimepakker. Se [arbeidsordren](PHASE3_LOCAL_MEDIA_WORK_ORDER.md). IMediaSource isolerer kilden; IMediaProbe isolerer inspeksjonen. Første source er LocalMediaSource (SourceId local-file).

## API og dataflyt

1. Konfigurer FfprobeMediaProbe med en eksplisitt absolutt bane til en egnet FFprobe-binær.
2. LocalMediaSource.InspectAsync(absoluteFilePath, cancellationToken) returnerer MediaCandidate med filbane, bytelengde og MediaInspection.
3. LocalSongDraft.Create(candidate, overrides?, audioStreamIndex?, videoStreamIndex?) returnerer SongDraft med Song og diagnostikk.
4. Lagre med ProjectStore; Load leser ikke mediefilen på nytt.

Originalfilen kopieres, flyttes eller endres ikke. URLs/file-URI-er og relative baner avvises. Symlinks/monterte filsystemer følger OS-semantikken; dette er ikke en filesystem-sandbox. Størrelse og last-write-tid sjekkes før/etter inspeksjon som best-effort endringskontroll, ikke en låst atomisk snapshot eller innholdshash.

Inspection har container, valgfri varighet, streams og tags. Streams har indeks, type, valgfri codec/rate/kanaler/dimensjoner/varighet, attached-picture-flagg og tags. Manglende/N/A-varighet forblir null; oppgitt varighet er ikke en verifisert playback-lengde.

## Spor og metadata

Et sangutkast krever lyd. Én lydstream velges entydig; flere krever audioStreamIndex. Flere reelle videostreams krever tilsvarende eksplisitt videoStreamIndex. Et valgt indeksnummer må identifisere riktig type. Embedded artwork er ikke video; stille video/bilde kan inspiseres, men blir ikke sanglyd.

Audio+video i samme fil gir separate MediaReference-ID-er til samme absolutte bane. Valgte stream-indekser lagres i proveniens for senere dekoding; MediaReference får ingen ny stream-egenskap. Senere playback må følge det lagrede sporvalget. Eksport dedupliserer filkopien. Offset starter på null; BPM/noter/analyse oppfinnes ikke.

Ved flere audio-/videostreams varsler SelectedStreamsRequireExtraction om senere preprocessing. UltraStar-formatet kan ikke lagre stream-indeksen: eksport av den opprinnelige filen blokkeres med AmbiguousMedia når valget ellers ville gå tapt. Etter eksplisitt senere ekstraksjon kan aktuelle mediereferanser peke på separate filer. Enkeltstream audio+video er støttet uten ekstraksjon.

Artist/tittel/språk: container-tags, deretter valgt audio, deretter valgt video. Tag-navn matches uten hensyn til case; ytre whitespace trimmes i gjeldende verdier. Tomme tags og language=und regnes som ukjent. Flere forskjellige verdier på samme prioritetsnivå gir ConflictingMetadata og tom verdi, ikke et tilfeldig valg. Ingen ISO-kodeoversetting eller matching-confidence inngår.

MetadataOverrides: null beholder filverdien; eksplisitt tom string tømmer feltet; manuelle verdier overstyrer og trimmes. language=und blir null også ved manuell overstyring. Manglende metadata gir redigerbare draft-warnings. Artist/tittel gjettes ikke fra filnavnet; rå tags bevares.

## Varig proveniens og eksport

SourceDocument.FormatId er local-media-v1; FileReference er originalfilens absolutte bane. Dette er en source-profil, ikke en ny schemaVersion. Headers har følgende navnerom:

| Navn | Verdi |
| --- | --- |
| source.id / source.bytes | SourceId / original bytelengde |
| format.name / format.durationSeconds | Container / valgfri varighet |
| selection.audioStream / selection.videoStream | Valgte indekser; video valgfri |
| format.tag.<originalt navn> | Original container-tag med case/verdi |
| stream.<indeks>.kind / codec / attachedPicture | Type / valgfri codec / true eller false |
| stream.<indeks>.sampleRate / channels / width / height / durationSeconds | Tilgjengelige tekniske felt |
| stream.<indeks>.tag.<originalt navn> | Original stream-tag |

Tags/streams beholder dokumentrekkefølgen; valgfrie fraværende felt utelates. Tall er invariant kultur. Dette er eksplisitt forespurte tekniske felt/tags, ikke alle mulige FFprobe-felter. Proveniens overlever prosjekt-save/load og endringer i Song.Metadata uten Core-/Projects-/schemaendring.

Writeren kjenner profilen, men sender ingen tekniske tags som UltraStar-headers. Proveniens blir i prosjektet med ProjectOnlyData-warning. Gjeldende metadata/medier/noter styrer eksport. GridBpm må gis eksplisitt; filens tempo-tag blir ikke automatisk et UltraStar-grid. Andre ukjente source-profiler avvises fortsatt. Album/øvrige tags er tilgjengelige i prosjektet; cover/albumheader oppfinnes ikke ved eksport.

## Prosess-/formatgrense

FFprobe startes uten shell/PATH-oppslag med ProcessStartInfo.ArgumentList og lukket stdin. Stdout/stderr leses samtidig med strict UTF-8 og outputgrenser. Default deadline 30 s, konfigurerbar opptil 5 min; stdout 1 MiB, maks 32 MiB; stderr min(stdoutgrense, 64 KiB). Ved avbrudd, timeout eller outputfeil avsluttes prosess-treet og prosessen avventes. Dette begrenser ikke alle prosessens CPU-/RAM-/diskressurser.

protocol_whitelist=file og format_whitelist=wav,mp3,flac,ogg,aac,mov utelukker nettverksprotokoller og playlist-demuxer. Den minimale Windows-builden har også nettverk deaktivert ved build. JSON: depth maks 32, 256 streams og 1024 tags per seksjon, unike properties/stream-ID-er, korrekte typer og finite nonnegative varigheter. Ekstra FFprobe-seksjoner tolkes ikke. Inspeksjon er ikke bevis på at alle codecs kan dekodes/spilles.

MediaAcquisitionException har AcquisitionError for ProbeUnavailable/Failed/TimedOut/OutputTooLarge, InvalidProbeData, UnsupportedMedia og AmbiguousStream. Vanlige fil-/tilgangsfeil og caller-cancellation beholder .NET-exceptions. Et gyldig draft har diagnostikk for metadata/proveniens; ingen stille korrigeringer.

## Gjenbruk og lisens

Adapteren følger godkjent separat FFmpeg-retning; ingen egen containerparser eller ny hovedstack. Windows-smoke bruker eksisterende minimal FFprobe 8.1.3 fra [Fase 1-inventaret](../tools/phase1/results/minimal-codec.json), source 330caae0c1acccd2222edc52a05940c574561ce5. Ingen ny binær lastes ned/distribueres. Recipe/source/LGPL-notices finnes privat i evalueringssettet; fremtidig produktpakke må følge ADR-0002 og faktiske artefaktkrav.

API-kilder: [FFprobe format/streams/tags](https://ffmpeg.org/ffprobe.html), [FFmpeg-protokoller](https://ffmpeg.org/ffmpeg-protocols.html), [ArgumentList](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.argumentlist?view=net-10.0). Den pinnede buildens konkrete format-/codecprofil gjelder.

Se [teststatus](TESTING_STATUS.md) for hva prosjekteieren kan prøve nå.
