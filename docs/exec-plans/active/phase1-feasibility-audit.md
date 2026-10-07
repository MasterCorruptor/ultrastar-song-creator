# Fase 1 – Technology Feasibility & Reuse Audit

Status: aktiv – avsluttende research/prøver og teknologianbefaling levert; prosjekteierens teknologi- og fasevedtak gjenstår. Gjennomført 2026-10-07. Startet 2026-10-07. Arbeidsbranch: `work/phase1-feasibility-audit`.

## Mål og rammer

Gjennomfør teknologikartleggingen i PROJECT_MASTER.md §16 og §26. Brukerens arbeidsordre er å starte Fase 1 etter masteren. Denne planen koordinerer kildekartlegging, lisensvurdering, praktiske undersøkelser og teknologiforslag, og bevarer milepælstatus på tvers av økter.

Masterspesifikasjonen forblir uendret. Ingen produksjonskode, ny obligatorisk konto/API-nøkkel eller låsing av hovedstack. Uavklarte lisenser blokkerer innføring av berørte komponenter, men ikke undersøkelse av alternativer. Prosjekteieren godkjenner vesentlige teknologivalg etter at konkrete forslag er dokumentert.

## Leveranser og acceptance criteria

- [x] Kandidatmatrise med primærkilder, versjons-/commitreferanser, lisens og modellvilkår, Windows/Linux, offline-/kontobehov, vedlikehold og integrasjon.
- [x] Dekning av media/metadata, lyrics, separasjon, ASR/alignment, BPM, pitch, notesegmentering, UltraStar-format, avspilling, timeline og eksisterende editorer.
- [x] Små reproducerbare feasibility-tester der de løser en konkret usikkerhet; skill målte resultater fra dokumentasjon og antakelser.
- [x] Begrunnede forslag til stack og komponenter, egenutvikling og alternativer; ingen ADR markert som akseptert uten prosjekteierens vedtak.
- [x] Dokumenterte tekniske risikoer og gjenstående benchmark-/lisensporter.
- [x] Første dokumentasjons-/probe-leveranse verifisert og klargjort som lokal commit på arbeidsbranchen. Prosjekteieren har godkjent offentlig publisering etter at maskinvareinformasjon er fjernet; publiserte filer og historikk kontrolleres før push.

## Arbeidsrekkefølge

1. Kontroller prosjektgrunnlag og tilgjengelig evalueringsmiljø.
2. Les upstream-dokumentasjon og lisensfiler; avvis uegnede bindinger.
3. Kjør avgrensede tester med syntetiske data og anonyme provider-kall der forsvarlig.
4. Skriv dependency-matrise, audit og foreslåtte ADR-er.
5. Kontroller kilder, lenker, masterintegritet og Git-diff. Commit lokalt; offentlig push og review følger publiseringsgodkjenningen og kontroll av redigert payload.

## Kontrollpunkt

Produksjonsstack og lisensstrategi forelegges prosjekteieren. Research kan integreres uten at forslag automatisk blir aksepterte beslutninger. Fase 2 starter ikke som del av denne arbeidsordren.


## Første konkrete resultater

Se `docs/PHASE1_AUDIT.md`, `docs/DEPENDENCIES.md` og `docs/PHASE1_RESULTS.md`. Det er gjennomført syntetisk media-/pitch-/BPM-prøve, offentlig provider-probe, lokal CPU-ASR med cache-only modellast og headless Avalonia-timeline. To ADR-er er foreslått, ingen er akseptert. Evidens og reproduksjon ligger i `tools/phase1/`.

## Porter etter videre arbeidsordre

- [x] Native playback/seek/loop/play-from-selection: Windows-lydenhet og samplekontroll, Linux no-device PCM.
- [x] Realistisk timeline: ekte waveform/manuelle noter, egne ordplassholdere, sample-cursor, zoom og tung UMX-worker med målt prosessstopp.
- [x] Ekte sangbenchmark: 40 annoterte solovokaler, 17 ASR-klipp, tre miksinger for alignment/ASR og tre hele sanger for CPU-separation. GPU og source-stem-SDR er eksplisitt uprøvd, ikke skjult som bestått.
- [x] Lisensavgrensning: MIT UMXHQ/Whisper/SwiftF0 og egen minimal LGPL-FFmpeg; konkret stock PyAV/x264/x265-rute og uklare weights avvist. Installer-SBOM/source/notices er en egen fremtidig releaseforpliktelse.
- [x] Minimalt Windows-pakkeoppsett: faktisk self-contained .NET + embedded Python/native sidecars, uten utviklingsverktøy i PATH, startet fra pakkemappen med medfølgende kode/modeller.
- [x] Faktisk Linux-smoke: native Avalonia/miniaudio-kjerne + Python/UMX/whisper.cpp/direkte alignment, med nettverket av under inferens. Xvfb/no-device, ikke fysisk desktop/audio.
- [x] Oppdatert kandidat-/risikovurdering, publiserbar evidens uten maskinvareprofil og avsluttende anbefaling med konkrete tradeoffs/beslutninger.
- [ ] Prosjekteierens vedtak om stack, playback, lisensprofil, modellbaseline, tilstrekkelig evidens og faseavslutning. Ingen ADR er akseptert.

Se [anbefalingen](../../PHASE1_RECOMMENDATION.md), [målingene](../../PHASE1_CONTINUATION_RESULTS.md) og [artefaktlisenser](../../PHASE1_LICENSES.md). CPP/PCM-alignment-ruten ble prøvd etter at dypere lisenskontroll avdekket x264/x265 i PyAVs «LGPL»-rapporterte bundle. Den avsluttende anbefalte pakken inneholder ikke stock PyAV.

Planen blir i active/ til prosjekteieren har gjennomgått teknologivalgene og eksplisitt godkjent faseavslutning. Den nye publiseringen er en researchleveranse, ikke et produksjonsstackvedtak. Ingen produktkode eller Fase 2 er startet.
