# UltraStar Song Creator

UltraStar Song Creator skal bli et desktopverktøy for opprettelse, automatisk generering, validering og manuell redigering av UltraStar-kompatible karaokesanger. Den visuelle editoren er produktets viktigste komponent.

## Status

Fase 0 er etablert. **Fase 1 er avsluttet og godkjent av prosjekteieren 2026-10-08.** ADR-0001 og ADR-0002 er akseptert: C#/.NET 10 LTS + Avalonia, separat Python-analyse, utskiftbar miniaudio-backend, MIT for egen kode og separat dokumenterte LGPL-codecs. Open-Unmix med eksplisitt umxhq er separasjonsbaseline. **Fase 2 er påbegynt:** deloppgave 2.1 har intern sangmodell, validering og MoveNote med undo/redo og domenetester. **Fase 2.2 har versjonert prosjektlagring:** dagens sang, redigerte noter og analysepunkter kan lagres/gjenåpnes lokalt. **Fase 2.3 er implementert og verifisert:** import for unversionerte enkeltstemmer (absolutt og relativ tid) og UltraStar v1, med bevart kildemetadata i prosjektformat v2. Relativ legacy-tid følger dokumentert USDX-kompatibilitet; v1 + relativ tid avvises. **Fase 2.4 er implementert og verifisert:** unversionert/v1-eksport med absolutt tid, automatisk avrundingsrapport og komplette mapper med byteverifiserte mediekopier. GUI-01 er integrert som separat testflate.

**Integrert til main 2026-10-09:** PR #1–#5 er gjennomgått og merget. Samlet verifikasjon: 265/265 tester på Windows og nettisolert Linux. Se [integrasjonsrapporten](docs/INTEGRATION_RESULTS.md). Ingen ny produktfase er startet.

**Fase 3.1 er implementert og verifisert 2026-10-11:** lokal filinspeksjon, filmetadata/manuelle overrides og lagret source-proveniens. Originalfiler refereres uten kopiering. [Kontrakt](docs/LOCAL_MEDIA.md) og [teststatus for prosjekteieren](docs/TESTING_STATUS.md). Det finnes ennå ingen alpha/beta-app eller produkt-.exe.

**Fase 3.2 er implementert og verifisert 2026-10-12:** MusicBrainz-søk på artist/tittel, eksplisitt recording-/utgivelsesvalg og offline lagret katalogproveniens. Manuelle sangdata bevares. [Kontrakt og verifikasjon](docs/RECORDING_SEARCH.md). Ingen medienedlasting eller GUI inngår.

**Fase 3.3 er integrert 2026-10-12 via [PR #9](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/9)**, med YouTube-søk, eksplisitt formatvalg og nedlasting av original lyd/valgfri video. SUPERVISOR har godkjent leveransen og sti-rettingen; [kontrakt](docs/ONLINE_MEDIA.md) og [checkpoint](docs/exec-plans/completed/phase3-media-download.md). Ingen GUI/alpha inngår.

## Prosjektkilder

- [Master Project Specification v0.5](docs/PROJECT_MASTER.md) er prosjektets autoritative masterspesifikasjon.
- [AGENTS.md](AGENTS.md) er et kort kart for agenter og utviklere.
- [Utviklings- og repositoryprinsipper](docs/DEVELOPMENT.md) beskriver autoritet, brancher, verifikasjon og miljø.
- [Fase 1-audit](docs/PHASE1_AUDIT.md), [kandidat-/lisensmatrise](docs/DEPENDENCIES.md) og [måleresultater](docs/PHASE1_RESULTS.md) dokumenterer evalueringen.
- [Teknologianbefaling og beslutningspunkter](docs/PHASE1_RECOMMENDATION.md), [videre målinger](docs/PHASE1_CONTINUATION_RESULTS.md) og [artefaktlisenser](docs/PHASE1_LICENSES.md) er gjeldende beslutningsgrunnlag.
- [Akseptert desktop-/analysearkitektur](docs/decisions/ADR-0001-desktop-and-analysis-stack.md) og [lisensstrategi](docs/decisions/ADR-0002-licensing-and-reuse.md) dokumenterer prosjekteierens vedtak.
- [Avsluttet Fase 1-plan](docs/exec-plans/completed/phase1-feasibility-audit.md) dokumenterer leveransen og aksepterte begrensninger.
- [Gjeldende prosjektformat v2](docs/PROJECT_FORMAT_V2.md), [historisk v1](docs/PROJECT_FORMAT.md) og [avsluttet lagringsplan](docs/exec-plans/completed/phase2-project-storage.md) dokumenterer Fase 2.2.
- [Fase 2.1-arbeidsordre](docs/PHASE2_FIRST_WORK_ORDER.md), [core domain/enheter](docs/CORE_DOMAIN.md) og [testdependencies](docs/CORE_DEPENDENCIES.md) dokumenterer den første kjernedeloppgaven.
- [UltraStar-importprofil](docs/ULTRASTAR_IMPORT.md), [arbeidsordre](docs/PHASE2_ULTRASTAR_IMPORT_WORK_ORDER.md) og [avsluttet importplan](docs/exec-plans/completed/phase2-ultrastar-import.md) dokumenterer Fase 2.3. [Relativ tidsprofil](docs/RELATIVE_TIMING.md) beskriver avklaringen og kompatibilitetsgrensene.
- [Eksportkontrakt](docs/ULTRASTAR_EXPORT.md), [resultater](docs/PHASE2_EXPORT_RESULTS.md), [arbeidsordre](docs/PHASE2_ULTRASTAR_EXPORT_WORK_ORDER.md) og [avsluttet eksportplan](docs/exec-plans/completed/phase2-ultrastar-export.md) dokumenterer Fase 2.4.
- [Evalueringsverktøy](tools/phase1/README.md) inneholder reproduksjonskommandoer; prober bygges separat fra Core/Projects/UltraStar-solution.

## Struktur

```text
.
├── .gitignore
├── AGENTS.md
├── README.md
├── LICENSE
├── docs/
│   ├── PROJECT_MASTER.md
│   ├── DEVELOPMENT.md
│   ├── PHASE1_AUDIT.md
│   ├── PHASE1_RESULTS.md
│   ├── DEPENDENCIES.md
│   ├── decisions/
│   └── exec-plans/
│       ├── active/
│       └── completed/
├── src/
├── tests/
└── tools/
```

Mapper som fortsatt er tomme har minimale `.gitkeep`-filer. Lokal agenttilstand hører hjemme i den Git-ignorerte mappen `.agent-local/`.

## Kom i gang

Fase 0 krever Git for å lese historikk og arbeide med repositoryet. GitHub CLI er brukt til den første GitHub-opprettelsen, men er ikke et krav for applikasjonen eller senere utvikling. En avgrenset GUI-testflate er tilgjengelig; se startguiden nedenfor. Bygg/test produktkjernen med .NET 10 SDK fra prosjektroten:

```text
dotnet restore UltraStar.SongCreator.slnx --locked-mode
dotnet build UltraStar.SongCreator.slnx -c Release --no-restore
dotnet test UltraStar.SongCreator.slnx -c Release --no-build --no-restore
```

Core/Projects/UltraStar krever ingen Python, modeller eller mediefiler; restore trenger pakker én gang eller en eksisterende NuGet-cache. Fase 1-prøvene kjøres separat som beskrevet i [tools/phase1/README.md](tools/phase1/README.md). Se [DEVELOPMENT.md](docs/DEVELOPMENT.md) for arbeidsflyt og miljøprinsipper.

## Lisens

[MIT License](LICENSE) er bekreftet for egen prosjektkode 2026-10-08. Tredjepartsavhengigheter, modeller og distribuerte binærer har egne vilkår, jf. [ADR-0002](docs/decisions/ADR-0002-licensing-and-reuse.md).

## Første GUI-testflate

GUI-01 er integrert via [PR #10](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/10) etter SUPERVISOR-godkjenning. [Startguide og faktiske begrensninger](docs/GUI_01_TEST_SURFACE.md). Den viser egen testsang og støtter notevalg/flytting/angre/gjør om; ingen lyd eller filåpning. Manuell native Windows-smoke er bekreftet bestått av prosjekteieren; ingen alpha/release påstås.

## Integrert lydbackend

Audio-01 er godkjent og integrert via [PR #11](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/11): utskiftbar in-memory PCM-playback og waveform-envelope, faktisk Windows miniaudio-device og Linux no-device/library-loading. [Kontrakt/evidens](docs/AUDIO_PLAYBACK.md). GUI-02 er integrert via [PR13](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/13): waveform, transport, synkronisert playhead og noteflytting/undo/redo. Alle seks native Windows-scenarioer er eierbekreftet bestått,490/490 tester består på Windows/Linux. [Start og begrensninger](docs/GUI_02_EDITOR_BINDING.md). Avgrenset lokal prototype, ingen alpha/release.

## Lyrics-fundament – kodegodkjent

Provider-uavhengige in-memory lyricsobjekter, validering, manuell tekst og eksplisitt kandidatvalg er kodegodkjent ved5492b44;549/549 Windows-tester består. [Kontrakt og grenser](docs/PHASE3_LYRICS_CONTRACT.md). Ved dette historiske kontrollpunktet var HTTP-innhenting, LRC-parser, persistens og lyrics-GUI ikke implementert; end-to-end Lyrics Acquisition er ikke ferdig. Integrert via [PR15](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/15) som458176f13ed7de50cd3b503a7a7b194139444784; fundamentplanen er avsluttet.

## Simple line-LRC – godkjent pakke

[Avgrenset profil](docs/LRC_PROFILE.md) ved6c8a563: pure bounded parser med bevart tekst/metadata og typed formatfeil,73 parser-tests og622/622 Windows-regresjoner bestått. Bare komplette angle-timestampformer avvises; nonzero offset/andre formater er utenfor profilen. Ingen HTTP, lyrics-GUI eller persistens i pakken; faktisk integrasjon fremgår av GitHub-historikken.

## Valgt lyrics og prosjektv3 – godkjent samlet pakke

Immutable valgt tekst/proveniens kan endres med undo/redo og lagres offline i strictv3; v1/v2-lesing og originalfiler bevares. Kandidatdiagnostikk er transient og persisteres ikke.773/773 Windows-tester består. [Gjennomgått evidens](docs/LYRICS_COMBINED_LOCAL_REVIEW.md). Publisering/main-integrasjon er eierautorisert; aktuell PR/Git-historikk angir status. Ingen HTTP-lyricsinnhenting/ny GUI/native/Linux/release er verifisert i pakken.

LRCLIB HTTP-transport er integrert via PR20 på main6ddb743 med 892/892 Windows-tester; ikke koblet til GUI eller ferdig alpha/beta. Se [gjeldende teststatus](docs/TESTING_STATUS.md) og [transportkontrakten](docs/LRCLIB_TRANSPORT_PROFILE.md).

[Neste foreslåtte sangtekstflyt](docs/PHASE3_LYRICS_FLOW_PROPOSAL.md) har en kodegodkjent A-session med 939/939 Windows-tester; GUI-wiring gjenstår.

B1 apply via eksisterende editorhistorikk er akseptert med 953/953 Windows-tester; [checkpoint](docs/exec-plans/completed/phase3-lyrics-editor-apply.md). B2-komponenten er godkjent etter review, med 984/984 Windows-tester; [checkpoint](docs/exec-plans/completed/phase3-lyrics-panel-component.md). Kobling inn i appvinduet er et senere B3-trinn.

Sangtekstøkten A er integrert via [PR21](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/21) som main d5a6231; lokal/GitHub main er like. Ved B2-avslutningen var panelrettingen godkjent separat lokalt, før kobling inn i appvinduet.

B2 er avsluttet lokalt; B3 ble deretter bestilt etter ny kapasitetskontroll. Ingen ny publisering eller main-endring inngår i avslutningen.

## B3 — lokalt appwiring-checkpoint

Tekstpanelet er nå koblet til editorprototypen med blanke søkefelt, eksplisitt LRCLIB-komposisjon og observert oppstarts-/lukkeopprydding. Samme notehistorikk/timeline/playback bevares.16nye/1000totale Windows-tester består; [rapport](docs/exec-plans/completed/phase3-lyrics-app-wiring.md) er godkjent av SUPERVISOR som lokal offline-komposisjon. Native/manuell/live/Linux og fullworkflow er ikke verifisert. Leveransen er lokal, uten ny publisering/main-integrasjon eller Generation.

Samlet B1–B3 er nå klargjort på en egen lokal integrasjonsbranch, med uendret testet kode og konfliktfri main-ancestry. [Integrasjonsrapport og senere nativeplan](docs/PHASE3_LYRICS_EDITOR_INTEGRATION.md). Avventer review før publisering; ingen ny main-endring/native-test eller videre fase inngår.

## Sangtekstflyt B1–B3 integrert

[PR22](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/22) er merget som main16b9c18; GitHub/main og lokal main samsvarer. Hele merge-treet er identisk med godkjent941e765/testet kode38e1b5a (1000/1000Windows). [Integrasjonsrapport](docs/PHASE3_LYRICS_EDITOR_INTEGRATION.md). Native/manuell/live/Linux/fullworkflow er fortsatt ikke verifisert; ingen Generation/release er startet. Denne lokale sluttdokumentasjonen er ikke publisert gjennom PR22.
