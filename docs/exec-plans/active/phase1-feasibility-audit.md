# Fase 1 – Technology Feasibility & Reuse Audit

Status: aktiv. Startet 2026-10-07. Arbeidsbranch: `work/phase1-feasibility-audit`.

## Mål og rammer

Gjennomfør teknologikartleggingen i PROJECT_MASTER.md §16 og §26. Brukerens arbeidsordre er å starte Fase 1 etter masteren. Denne planen koordinerer kildekartlegging, lisensvurdering, praktiske undersøkelser og teknologiforslag, og bevarer milepælstatus på tvers av økter.

Masterspesifikasjonen forblir uendret. Ingen produksjonskode, ny obligatorisk konto/API-nøkkel eller låsing av hovedstack. Uavklarte lisenser blokkerer innføring av berørte komponenter, men ikke undersøkelse av alternativer. Prosjekteieren godkjenner vesentlige teknologivalg etter at konkrete forslag er dokumentert.

## Leveranser og acceptance criteria

- [ ] Kandidatmatrise med primærkilder, versjons-/commitreferanser, lisens og modellvilkår, Windows/Linux, offline-/kontobehov, vedlikehold og integrasjon.
- [ ] Dekning av media/metadata, lyrics, separasjon, ASR/alignment, BPM, pitch, notesegmentering, UltraStar-format, avspilling, timeline og eksisterende editorer.
- [ ] Små reproducerbare feasibility-tester der de løser en konkret usikkerhet; skill målte resultater fra dokumentasjon og antakelser.
- [ ] Begrunnede forslag til stack og komponenter, egenutvikling og alternativer; ingen ADR markert som akseptert uten prosjekteierens vedtak.
- [ ] Dokumenterte tekniske risikoer og gjenstående benchmark-/lisensporter.
- [ ] Dokumentasjon, verifikasjon og konsistent commit pushet på arbeidsbranchen for kontroll.

## Arbeidsrekkefølge

1. Kontroller prosjektgrunnlag og tilgjengelig evalueringsmiljø.
2. Les upstream-dokumentasjon og lisensfiler; avvis uegnede bindinger.
3. Kjør avgrensede tester med syntetiske data og anonyme provider-kall der forsvarlig.
4. Skriv dependency-matrise, audit og foreslåtte ADR-er.
5. Kontroller kilder, lenker, masterintegritet og Git-diff. Commit og push.

## Kontrollpunkt

Produksjonsstack og lisensstrategi forelegges prosjekteieren. Research kan integreres uten at forslag automatisk blir aksepterte beslutninger. Fase 2 starter ikke som del av denne arbeidsordren.
