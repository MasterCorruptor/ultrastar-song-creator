# Fase 1 – Technology Feasibility & Reuse Audit

**Status: avsluttet og godkjent av prosjekteieren 2026-10-08.** Teknologivalg og lisensstrategi er registrert i aksepterte ADR-0001/0002. Gjennomført 2026-10-07. Startet 2026-10-07. Arbeidsbranch: `work/phase1-feasibility-audit`.

## Mål og rammer

Gjennomfør teknologikartleggingen i PROJECT_MASTER.md §16 og §26. Brukerens arbeidsordre er å starte Fase 1 etter masteren. Denne planen koordinerer kildekartlegging, lisensvurdering, praktiske undersøkelser og teknologiforslag, og bevarer milepælstatus på tvers av økter.

Masterspesifikasjonen forblir uendret. Researchen innførte ingen produksjonskode eller ny obligatorisk konto/API-nøkkel. Hovedstacken er nå godkjent etter gjennomført evaluering. Uavklarte lisenser blokkerer innføring av berørte komponenter, men ikke undersøkelse av alternativer. Prosjekteieren godkjenner vesentlige teknologivalg etter at konkrete forslag er dokumentert.

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

Se `docs/PHASE1_AUDIT.md`, `docs/DEPENDENCIES.md` og `docs/PHASE1_RESULTS.md`. Det er gjennomført syntetisk media-/pitch-/BPM-prøve, offentlig provider-probe, lokal CPU-ASR med cache-only modellast og headless Avalonia-timeline. ADR-ene var foreslått i første kontrollpunkt; begge ble eksplisitt akseptert av prosjekteieren 2026-10-08. Evidens og reproduksjon ligger i `tools/phase1/`.

## Porter etter videre arbeidsordre

- [x] Native playback/seek/loop/play-from-selection: Windows-lydenhet og samplekontroll, Linux no-device PCM.
- [x] Realistisk timeline: ekte waveform/manuelle noter, egne ordplassholdere, sample-cursor, zoom og tung UMX-worker med målt prosessstopp.
- [x] Ekte sangbenchmark: 40 annoterte solovokaler, 17 ASR-klipp, tre miksinger for alignment/ASR og tre hele sanger for CPU-separation. GPU og source-stem-SDR er eksplisitt uprøvd, ikke skjult som bestått.
- [x] Lisensavgrensning: MIT UMXHQ/Whisper/SwiftF0 og egen minimal LGPL-FFmpeg; konkret stock PyAV/x264/x265-rute og uklare weights avvist. Installer-SBOM/source/notices er en egen fremtidig releaseforpliktelse.
- [x] Minimalt Windows-pakkeoppsett: faktisk self-contained .NET + embedded Python/native sidecars, uten utviklingsverktøy i PATH, startet fra pakkemappen med medfølgende kode/modeller.
- [x] Faktisk Linux-smoke: native Avalonia/miniaudio-kjerne + Python/UMX/whisper.cpp/direkte alignment, med nettverket av under inferens. Xvfb/no-device, ikke fysisk desktop/audio.
- [x] Oppdatert kandidat-/risikovurdering, publiserbar evidens uten maskinvareprofil og avsluttende anbefaling med konkrete tradeoffs/beslutninger.
- [x] Prosjekteierens vedtak 2026-10-08 om stack, playback, lisensprofil, modellbaseline, tilstrekkelig evidens og faseavslutning. ADR-0001 og ADR-0002 er akseptert.

Se [anbefalingen](../../PHASE1_RECOMMENDATION.md), [målingene](../../PHASE1_CONTINUATION_RESULTS.md) og [artefaktlisenser](../../PHASE1_LICENSES.md). CPP/PCM-alignment-ruten ble prøvd etter at dypere lisenskontroll avdekket x264/x265 i PyAVs «LGPL»-rapporterte bundle. Den avsluttende anbefalte pakken inneholder ikke stock PyAV.

Planen er flyttet til completed/ etter eksplisitt godkjenning 2026-10-08. Dokumenterte begrensninger er akseptert som ikke-blokkerende for faseavslutning og videreføres som kvalitets-/releasekrav. Fase 1-PR-en klargjøres for merge; ingen produktkode eller Fase 2 er startet.

## Overlevering

Gjennomførte kontroller: native Windows-playback/seek/loop og timeline under analysebelastning, ekte sangbenchmark, artefaktlisensgjennomgang med avvisning av stock PyAV/uklare weights, portable Windows-prøve og Linux-container-smoke. Ren Windows-maskin, fysisk Linux-audio, større norsk-/ASR-/miksingsbenchmark, GPU og endelig release-SBOM/source/notices gjenstår som senere arbeid.

Se [forslag til første Fase 2-arbeidsordre](../../PHASE2_FIRST_WORK_ORDER.md). Forslaget er ikke bestilt og ingen aktiv Fase 2-plan er opprettet.
