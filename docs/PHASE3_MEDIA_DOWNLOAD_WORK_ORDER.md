# Fase 3.3 – YouTube-mediesøk og nedlasting

Bestilt og avklart av prosjekteieren 2026-10-12: YouTube-søk og direkte YouTube-lenker via yt-dlp, lyd og valgfri video, kildens lydformat uten omkoding. Autoritet: master v0.5 §3–5, §9–10, §15–18 og aksepterte ADR-er. Fase 3.1/3.2 er integrert.

## Leveranse

Utskiftbar nettmediekilde i Acquisition, bokstavelig artist-/tittelsøk, begrensede kandidater og eksplisitt valg av kilde og tilgjengelige format-ID-er. MusicBrainz-katalog og YouTube-media holdes adskilt; varighet/tittel er kontrollinformasjon og ingen sikker match-confidence oppfinnes.

Nedlasting gjelder én konkret video-ID, lyd med valgfri video fra samme ID, til en eksplisitt ny lokal mappe. Separate opprinnelige strømmer beholdes uten reencoding/merging. Første formatprofil er AAC/M4A-lyd og MP4-video; øvrige formatprofiler krever egen codec-verifikasjon. Format-ID og kvalitet velges eksplisitt; ingen bitrate-/oppløsningsdefault vedtas. Ingen eksisterende mappe/filer overskrives.

yt-dlp kjøres via pinnet wheel/Python, med lokalt installert kompatibel EJS og eksplisitt JS-runtime. Ingen GPL-standalone, default-extra med mutagen, kontoinnlogging, cookie-import, plugins, remote code-download eller auto-update. Verktøybaner konfigureres eksplisitt; framtidig distribusjon krever faktisk artefaktinventar etter ADR-0002.

Prosessgrensen har avbrytbar timeout, begrenset stdout/stderr, lukket stdin, separate argumenter og opprydding av prosess-tre. Nedlasting har samlet bytegrense med best-effort periodisk overvåking og endelig filkontroll, staging og publisering av ferdig mappe etter vellykket inspeksjon/hash. Begrensningene skal beskrives ærlig; dette er ingen OS-sandbox.

Lagret kilde-ID/URL, valgte formater, faktisk størrelse/hash og hentetid skal kunne gjenåpnes offline i eksisterende local-media-v1/prosjektformat v2. Gjeldende manuelle metadata beholdes. Ingen Core-/Projects-/root-schema-/master-/ADR-endring.

## Verifikasjon og grenser

Meningsfulle syntetiske tester for URL/query, metadata/formater/valg, feil/timeout/cancellation/bytegrenser, filbevaring/opprydding, faktisk prosessgrense, draft/proveniens, save/load og eksport. Locked restore/build/test/format på Windows og nettisolert Linux. Avgrenset nettprøve hvis anonym YouTube er tilgjengelig; nettblokkering rapporteres uten omgåelse eller falsk bestått-status. Kontroller masterintegritet, dokumentlenker, inventar/privacy og whitespace.

Ingen lyrics, cover, generation, GUI/alpha, installer eller full Fase 3-avslutning. Ingen videre deloppgave startes automatisk. Stopp ved uavklart lisens, nødvendig konto/nøkkel eller vesentlig endring av vedtatt scope/stack.

## Historiske checkpointkrav

Prosjekteierautorisert SUPERVISOR-ordre av 2026-10-12 krever verifisert commit/checkpointrapport og stopp før integrasjon. Gjeldende fordeling er SEQUENTIAL med én implementasjonsagent. [Policyen](SUPERVISOR_POLICY.md) bevarer begge eierinstruksene; [checkpointet](exec-plans/completed/phase3-media-download.md) er reviewgrunnlaget. Neste GUI-blokk starter ikke gjennom denne arbeidsordren.

## Avgrenset REQUEST_FIX – 2026-10-12

phase3-media-download-literal-paths er SMALL/SEQUENTIAL etter bekreftet P2 i faktisk yt-dlp-output-template/expand_path. Rett bokstavelige målmapper, også %(...)s, %VAR%, $VAR/${VAR} og vanlig prosent/dollar, uten scope-/dependencyendring. Native cleanup-/publiseringsregresjon og faktisk pinnet offline yt-dlp-probe på Windows/Linux; pushet korrigert checkpoint og stopp for SUPERVISOR-review. Ingen ny nett-smoke/GUI/merge.

## Integrasjonsordre – avsluttet 2026-10-12

SUPERVISOR ga CONTINUE for SMALL/SEQUENTIAL phase3-media-download-integration etter kvotereset. Godkjent ff73d86/a433836 integreres via [PR #9](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/9), med kilde/test/tool-identitet mot 71afe4d, bevarte autoritative dokumenter og oppdatert status. Ingen nye tester/nettprober når kun dokumentasjon/merge-metadata endres. Stopp etter integrasjon; GUI krever neste separate ordre.
