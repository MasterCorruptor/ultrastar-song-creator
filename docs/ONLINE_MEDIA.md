# Nettmedia – Fase 3.3

Status 2026-10-12: implementert/verifisert på arbeidsbranch, avventer SUPERVISOR-review før integrasjon. [Arbeidsordre](PHASE3_MEDIA_DOWNLOAD_WORK_ORDER.md) og [checkpoint](exec-plans/active/phase3-media-download.md). Ingen GUI eller full Fase 3-avslutning inngår.

## Kontrakt og brukerens valg

IMediaDownloadSource isolerer søk, ResolveAsync og DownloadAsync. YtDlpMediaSource er første adapter (SourceId youtube). Den eksisterende IMediaSource/LocalMediaSource og IMediaProbe brukes uendret for inspeksjon av ferdige lokale filer. Ingen container-/nettstedparser utvikles på nytt, og Core/Projects/UltraStar endres ikke.

MediaSearchQuery har valgfri artist/tittel og Limit 1–25 (default 10). Minst ett trimmet felt kreves; eksisterende 512 UTF-16-/Unicode-/kontrolltegn-grense gjenbrukes. Hele ytsearchN-forespørselen er ett prosessargument. Én begrenset flat søkeresultatside hentes uten medienedlasting. Tomme resultater er gyldige; nett-/toolfeil maskeres ikke som tomme treff. Ulike video-ID-er med samme tittel bevares; utilgjengelige null-slots utelates.

OnlineMediaCandidate har kilde-ID, video-ID, original tittel, uploader, valgfri varighet/live-flagg og kanonisk watch-URL. Uploader antas ikke å være sangartist. Ingen rå description, kommentarer, thumbnails eller tidsbegrensede CDN-URL-er videreføres.

ResolveAsync godtar HTTPS watch-, youtu.be-, mobile watch- og shorts-lenker til én konkret ID. Ekstra list/tidsparametere fjernes; dupliserte video-parametere, kanaler, playlists, ukjente domener, credentials og custom port avvises. ID skal ha elleve ASCII-tegn. Resolve kontrollerer at returnert ID samsvarer; live/upcoming og eksplisitt private/auth-krav avvises.

MediaSourceDetails har faktiske format-ID-er, extension, audio/video-codec, protocol, eventuell kjent filesize/bitrate/dimensjoner og DRM-flagg. Null betyr ukjent. Første støttede profil er separate HTTPS AAC/M4A-lydstrømmer og MP4-video uten egen lydstrøm. WebM, HLS/DASH, muxet video/audio og DRM blir ikke valgt gjennom denne profilen. MP4-kopiering/inspeksjon er ikke garanti for dekoding av alle video-codecs.

MediaDownloadChoices.Select(details, audioFormatId, videoFormatId?) krever eksplisitte tilgjengelige ID-er; det finnes ingen automatisk første kilde, format-, bitrate- eller oppløsningsdefault. Lyd og eventuell video må høre til samme YouTube-ID. Katalogvalget i MusicBrainz og det konkrete mediet er separate valg; tekst/varighet er kontrollinformasjon, ingen verifisert recording-match/confidence.

## Prosess og nedlasting

Konfigurer absolutte Python-, JS-runtime- og FFprobe-baner. Python kjører yt-dlp-wheel med -I/-X utf8; JS-runtime er eksplisitt node eller deno. Ingen shell eller PATH-oppslag. Lukket stdin, samtidig strict UTF-8-lesing av stdout/stderr og avventet prosess-tre-opprydding gjenbrukes fra ProbeProcess. Stdout-grense er 8 MiB; stderr 64 KiB.

Konfigurasjonsfiler, plugin-dirs, cookie-/browsercookie-import, cache, auto-update og remote EJS-komponenter er deaktivert. Netrc er ikke aktivert. Bare YouTube-/YouTubeSearch-extractors er valgt. yt-dlp retries/extractor-retries/fragment-retries er satt til null; ingen applikasjonsretry eller konto-fallback. Interne extractorforespørsler kan fortsatt være flere HTTP-kall.

Metadata-deadline er normalt 45 s, maksimum 5 min. Download-deadline er normalt 15 min for hele jobben, maksimum 2 timer, inkludert re-resolve, begge filer, inspeksjon/hash/manifest. Caller-cancellation bevares. MediaDownloadException skiller ToolUnavailable, ToolFailed, TimedOut, OutputTooLarge, InvalidData, SourceUnavailable, UnsupportedFormat, InvalidSelection og DownloadTooLarge. ToolFailed har eventuell exit-code, men beviser ikke ett bestemt nett-/serverproblem. Lokale fil-/probe-feil beholder sine eksisterende exceptions.

DownloadAsync(selection, newDirectoryPath) krever en ny absolutt mappe under en eksisterende parent. En fersk Resolve kontrollerer samme valgte formatprofil; endret/manglende format gir ny valganmodning, ingen stille erstatning. To separate originalstrømmer lastes uten extract-audio/reencode/merge/fixup. Filnavn er faste audio.m4a og eventuell video.mp4, aldri avledet fra fjern tittel.

Default samlet mediegrense er 1 GiB, maksimum 16 GiB. Kjent deklarert størrelse kontrolleres først. Egen staging overvåkes hvert 100 ms, og faktiske filstørrelser kontrolleres til slutt. Dette er best-effort: rask vekst kan overskride grensen før observasjon; det er ingen OS-diskkvote eller CPU/RAM-sandbox. Kun toppnivåfiler overvåkes; uventede filer/undermapper avvises før publisering.

Ferdige filer skal være regulære og ha forventet enkeltstående audio-/video-streamtype ved faktisk IMediaProbe-inspeksjon. SHA-256 beregnes, frosset source.json skrives, deretter publiseres hele mappen med rename i samme parent. Eksisterende mappe/filer overskrives ikke. Feil/avbrudd rydder bare jobbens validerte staging. Rename er commit-punktet: etter publisering returneres suksess selv ved sent avbrudd. Dette er ikke en fsync-/strømtapsgaranti eller en beskyttelse mot alle samtidige filesystem-endringer. Det ferdige mediet beholdes lokalt.

## Sangutkast og offline proveniens

DownloadedMediaDraft.Create(result, metadataOverrides?, recordingSelection?) lager et nytt lokalt sangutkast; eksisterende redigerte Song-objekter/media/analyser erstattes ikke. Filmetadata/manual overrides følger LocalSongDraft. Kildetittel/uploader fyller ikke automatisk artist/tittel. Et eksplisitt katalogvalg kan knyttes til det nye utkastet med RecordingChoices. Brukeren må fortsatt kontrollere versjon og offsets; ingen analyse/BPM/noter oppfinnes.

media.download.source.v1 i local-media-v1 inneholder eget version=1 med frosne private DTO-er: source/video-ID, kanonisk URL, tittel/uploader/varighet, UTC-ferdigtid og audio/valgfri video med absolutt referanse, format-ID, extension/codecs, faktisk bytes og SHA-256. source.json ved siden av mediet har samme format. Bredden i det offentlige søkemodell-API-et endrer ikke disse DTO-ene.

Read(song) tolker bare dette lagrede snapshotet. Missing/unknown/duplicate properties, unsupported version, feil profil, ugyldige referanser/hash eller duplisert eget header gir InvalidData uten nett-/fil-/probe-fallback. Windows-referanser kan leses som proveniens også på Linux; dette gjør ikke den opprinnelige Windows-banen tilgjengelig på Linux.

Prosjektformat v2/root-schema er uendret. Gjeldende manuelle sangdata beholder forrang. Source-snapshot og catalogue-choice blir i prosjektet; eksisterende eksport bruker gjeldende metadata/media og kopierer lyd/video, uten å lekke tekniske headers eller rå kildeinformasjon til sangdatafilen. Undo/redo på sangdata følger fortsatt Core; undo sletter ikke nedlastede filer. Mediene må finnes ved senere faktisk bruk/eksport.

## Gjenbruk, lisens og verifikasjon

[Artefaktinventaret](../tools/acquisition/dependencies.json) og [hashlåst requirements](../tools/acquisition/requirements-media.txt) dokumenterer yt-dlp 2026.8.19 og yt-dlp-ejs 0.8.0. Standard .NET process/HTTP/JSON og eksisterende FFprobe-profil gjenbrukes. Ingen ny NuGet-pakke/lockfil eller binær redistribusjon. JS er en konfigurert providerhjelper, ingen endring av desktopstacken. Faktisk produktpakking må klarere Python/JS og alle tredjepartsartefakter etter ADR-0002.

Primærkilder kontrollert 2026-10-12: [yt-dlp bruk/dependencies/lisenser](https://github.com/yt-dlp/yt-dlp/tree/2026.08.19), [EJS-oppsett](https://github.com/yt-dlp/yt-dlp/wiki/EJS), [EJS-kilde/lisens](https://github.com/yt-dlp/ejs/tree/0.8.0) og de faktiske hashkontrollerte wheel-arkivene. Datatilgang og verktøylisens klarerer ikke medias rettigheter.

414/414 tester bestod på Windows og nettisolert Linux, inkludert 54 nye syntetiske tester og faktisk egen .NET-child-prosess. Locked restore, Release build uten advarsler og formatkontroll bestod. Windows-nettprøven brukte faktisk adapter, yt-dlp/EJS/Node og eksisterende minimal FFprobe: to søketreff, eksplisitt 19 s-kilde, format 140/133, 309288 audio-bytes og 433081 video-bytes, hash/inspeksjon, offline prosjekt-save/load med bevart manuell tittel og to eksportassets. Testvalgene er ikke produksjonsdefaults.

Den gamle upstream-testvideoen var utilgjengelig, mens den eksplisitte korte røykprøvekilden fungerte anonymt. Dette er én tilgjengelighetsprøve, ingen garanti for alle videoer, nett/IP-er eller framtidige extractorendringer. Linux-testen er offline arkitektur-/prosess-/fil-I/O-verifikasjon; ekte YouTube, Python/EJS/JS og FFprobe-nettmedia er her prøvd på Windows. Medier, rå infojson, signed URLs, cacher/binærer og private resultater publiseres ikke.
