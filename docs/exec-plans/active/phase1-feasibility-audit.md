# Fase 1 – Technology Feasibility & Reuse Audit

Status: aktiv. Første kartlegging og avgrensede feasibility-prøver gjennomført 2026-10-07; vesentlige kontrollpunkter gjenstår. Startet 2026-10-07. Arbeidsbranch: `work/phase1-feasibility-audit`.

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

## Gjenstående porter før Fase 1 kan lukkes

- [ ] Native playback/timeline: sampleclock, seek/loop/play-from-selection, tekst/waveform og workerbelastning.
- [ ] Rettighetsavklart sangbenchmark: separation, pitch, alignment og BPM på CPU/GPU med ground truth.
- [ ] Klarering av konkrete modell-/codec-/native-/ordbokartefakter, særlig HTDemucs-weights og ferdig FFmpeg/PyAV-rute.
- [ ] Minimalt Windows-pakkeoppsett og Linux-smoke-test.
- [ ] Prosjekteierens vedtak om hovedstack og eventuelle lisensavvik; oppdaterte aksepterte ADR-er.

Planen flyttes ikke til `completed/` før disse portene er håndtert eller avklart med eksplisitt vedtak. En første audit-/probe-leveranse alene fullfører ikke Fase 1.
