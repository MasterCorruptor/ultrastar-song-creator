# Innspillingssøk og valg – Fase 3.2

[Arbeidsordren](PHASE3_RECORDING_SEARCH_WORK_ORDER.md) avgrenser dette til MusicBrainz-katalogsøk, eksplisitt valg og lagret proveniens på lokale sangutkast. Provider er valgfri. Ingen audio/video hentes og ingen GUI innføres.

## API og kandidatmodell

IRecordingSearchProvider har ProviderId og SearchAsync(RecordingQuery, cancellationToken). Første adapter er MusicBrainzRecordingProvider. Den bruker standard .NET HTTP/JSON; ingen ny pakke eller serverkode kopieres.

RecordingQuery har valgfri Artist/Title, Limit (1–100, default 25) og Offset (nonnegative, default 0). Minst ett søkefelt må finnes etter trimming. Felt begrenses til 512 UTF-16-enheter; interne kontrolltegn og ugyldig Unicode avvises. Artist og tittel blir quoted literal-felt med separat Lucene-escaping før URL-encoding. Brukeren får ikke et skjult avansert søkespråk.

RecordingPage har normalisert query, provider, count/offset, kandidater, UTC-hentetid og HasMore. Én side hentes per kall; ingen crawling, automatisk neste side eller background polling. Empty page er et gyldig resultat; HTTP-/JSON-feil maskeres ikke som null treff.

RecordingCandidate har provider-ID/recording-ID, tittel, artist-credits med ID/name/join phrase, valgfri varighet i sekunder, raw disambiguation, nullable video-flagg, tilgjengelige release-snapshots og kataloglenke. Krediterte navn prioriteres over artistens hovednavn; join phrases/credits bevares i rekkefølge. Samme tittel med ulike recording-ID-er dedupliseres ikke. Null varighet/video betyr ukjent. Release har ID, tittel og valgfri dato/land/disambiguation.

Title/disambiguation/release-kontekst kan hjelpe prosjekteieren å skille versjoner, men ingen studio/live/remaster/cover-klassifisering oppfinnes. Én MusicBrainz-recording kan finnes på flere utgivelser/masteringer; recording-ID alene beviser ikke et bestemt lydmaster. Treff og utgivelsessnapshot kan være ufullstendige. Søkescore er ikke confidence; score, tags, ratings, annotations og search-index-data lagres ikke i kandidat/prosjekt.

## HTTP-grensen

MusicBrainzRecordingProvider bruker fast HTTPS recording-endepunkt og read-only GET med JSON Accept og UltraStarSongCreator/0.1.0 + offentlig repository-URL som User-Agent. Ingen konto/API-nøkkel sendes. Egen standard-HttpClient bruker ikke cookies/automatisk redirect. Injisert HttpClient eies og konfigureres av kalleren; adapteren endrer ikke dens globale innstillinger eller disposer den.

Normale providerinstanser deler én prosessglobal gate: serialiserte forespørsler, minst 1 s mellom dispatch-tillatelser og server Retry-After-cooldown for 429/503. Gate bruker monoton tid. Cooldown avventes i avbrytbare intervaller; ingen automatisk retry. Andre programmer/prosesser på samme IP styres ikke av denne gaten.

Default request deadline er 30 s, konfigurerbar opptil 5 min, og omfatter send/body, ikke køen før dispatch. Caller-token avbryter også kø/cooldown. ResponseHeadersRead og bytebegrenset lesing kontrollerer både Content-Length og faktisk stream; default 2 MiB, maks 16 MiB. Strict UTF-8/JSON: depth 32, unike properties/recording/release-ID-er, maksimum forespurt antall kandidater, 100 credits og 250 releases per kandidat, korrekte typer og representerbar nonnegative millisekundvarighet.

RecordingSearchException skiller NetworkFailure, TimedOut, HttpFailure, RateLimited (429), Unavailable (503), ResponseTooLarge og InvalidResponse. HTTP-status og eventuell RetryAfter er tilgjengelig. 503 kan være generell serverbelastning; det beviser ikke at brukeren overskred en rategrense. Caller-cancellation beholder OperationCanceledException. Ingen nettfeil endrer et Song-objekt.

## Eksplisitt valg og lokal lagring

RecordingChoices.Select(page, recordingId, releaseId?) krever en entydig recording-ID fra siden. Valgfri release-ID må høre til kandidaten. Første recording/release velges aldri automatisk. Utelatt release betyr at ingen bestemt utgivelse er valgt; det er ikke en skjult default.

RecordingChoices.Attach(song, selection) returnerer et nytt immutable snapshot og krever local-media-v1. Bare eget valg-header endres; gjeldende Song.Metadata, mediefiler/referanser, offsets, noter og analysedata bevares. Den foreslåtte tittelen/artisten fra katalogen er tilgjengelig i selection, men overskriver ikke eierens metadata. BindRecordingChoice implementerer ISongCommand for eksisterende SongHistory; undo/redo gjenbruker eksakte snapshots. Samme identiske valg er en no-op.

Headeren catalog.recording.choice.v1 i SourceDocument.Headers inneholder et version=1 JSON-dokument med frosne eksplisitte DTO-er: provider/ID/title, artist-credits, duration/disambiguation/video, release-snapshots, sourceUrl, valgt nullable releaseId og hentetid. Gjeldende original fileReference/source-profil beholdes. Neste valg erstatter bare denne ene headeren; duplikater av eget valg-header avvises.

RecordingChoices.Read(song) leser kun lagret data; ingen provider/FFprobe/fil åpnes. Manglende valg gir null; korrupt, unsupported version, unknown/missing JSON-felt, for stor payload eller dupliserte properties/valg gir InvalidStoredChoice uten nettfallback. Formatets egne v1-DTO-er bevares ved fremtidig modellutvikling; en ny intern lagringsrepresentasjon krever eksplisitt ny choice-versjon/migrasjon.

Eksisterende ProjectStore v2 lagrer dette uten root-schemaendring. Historisk v1 og Core/Projects er uendret. Binding til UltraStar-importerte/ukjente source-profiler avvises foreløpig, slik at tekniske katalogdata ikke lekker inn i rå UltraStar-headers. Lokal source-eksport beholder valget i prosjektet og bruker fortsatt aktuelle sangdata/eksplisitt grid etter [eksportkontrakten](ULTRASTAR_EXPORT.md).

## Datavilkår og kildegrunnlag

Brukte recording/artist-credit/release-felter er MusicBrainz core-data under CC0. Supplementary annotations/tags/ratings og komplette search indexes inngår ikke i lagret data; serverkode og dokumentasjon har andre vilkår. Datasetlisens er ikke hosted-API-vilkår: tjenesten beskriver gratis ikke-kommersiell bruk og egne kommersielle planer. Denne valgfrie utviklingsadapteren innfører ingen obligatorisk betalt tjeneste; kommersiell bruk av hosted endpoint må vurderes separat før slike distribusjonsløfter.

Primærkilder kontrollert 2026-10-12: [API](https://musicbrainz.org/doc/MusicBrainz_API), [søkefelter/escaping](https://musicbrainz.org/doc/MusicBrainz_API/Search), [rate/User-Agent](https://musicbrainz.org/doc/MusicBrainz_API/Rate_Limiting), [core/supplementary-data](https://musicbrainz.org/doc/MusicBrainz_Database) og [datalisenser](https://musicbrainz.org/doc/About/Data_License). Dette klarerer ingen lyd, sangtekst eller coverrettighet.

## Verifikasjon

360/360 tester bestod på Windows og nettisolert Linux med .NET SDK 10.0.401 (tidligere 308 + 52 nye). Locked restore, Release build uten warnings/errors og formatkontroll bestod. HTTP-fixtures er egen syntetisk metadata; testen av offentlig constructor bruker faktisk felles sekund-spacing mot fake HTTP. Øvrige klokke-/cooldown-tester bruker kontrollert monoton testtid.

Én avgrenset Windows live-prøve av artist+title (ABBA / Dancing Queen, limit 5) bestod: 107 oppgitte treff, fem returnert. Recording 33044d7d-10f6-4e67-9ad5-571e6e7f4a3c hadde oppgitt varighet 223 s og ti release-snapshots. Prøven valgte eksplisitt denne ID-en, lagret prosjekt og leste valget offline med eierens title bevart. Dette er én tilgjengelighets-/integrasjonsprøve, ikke et kvalitetsbenchmark, defaultvalg eller dekning-/versjonsgaranti. Private prosjekt-/resultatfiler er ignorert.

Ingen GUI/native audio/media-download/installer eller hosted CI er verifisert gjennom dette trinnet. [Teststatus](TESTING_STATUS.md) beskriver dagens brukerbegrensninger.
