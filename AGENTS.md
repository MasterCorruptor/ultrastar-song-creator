# Agentkart

UltraStar Song Creator er et planlagt desktopverktøy for UltraStar-sanger. Editoren er hovedprioriteten. Windows først, senere Linux-støtte.

## Les først

Les `docs/PROJECT_MASTER.md` (v0.5), arbeidsordren og relevante aksepterte ADR-er i `docs/decisions/`. Masteren regulerer prosjektkravene. `docs/DEVELOPMENT.md` beskriver repository- og miljøprinsipper. Ikke omskriv masteren uten eksplisitt arbeidsordre.

## Autoritet og arbeidsflyt

Lokal autoritativ working tree på prosjekteierens primærmaskin står over GitHub `main`/`origin/main`, andre stabile referanser og WIP-brancher. Bevar nyere godkjent lokal informasjon; tidsstempler avgjør ikke autoritet. Se masteren §18.5.

Bruk avgrensede `work/<oppgave>`-brancher for ikke-trivielt arbeid. Commit ved konsistente kontrollpunkter. Integrer først når acceptance criteria og relevant verifikasjon er oppfylt, og innhent godkjenning der arbeidsordren krever det. `main` skal være stabil og synkronisert med GitHub.

## Scope og arkitektur

Arbeid i `src/`, `tests/`, `tools/` og relevant `docs/` innenfor arbeidsordren. Endre rotfiler når oppgaven krever det. Fase 0: prosjektgrunnlag. Ikke start Fase 1 eller produktimplementering.

Bevar skillet mellom acquisition, generation og editor. UltraStar er import-/eksportformat; intern sangmodell er separat. Undo/redo er et grunnkrav. Kjernefunksjoner skal kunne behandles lokalt uten obligatoriske tredjepartskontoer eller API-nøkler. Ikke lås stack eller innfør dependencies uten nødvendig evaluering og godkjenning.

## Verifikasjon og stopp

Build-, setup- og testkommandoer er ennå ikke etablert. Dokumenter dem når stacken velges. Nå: kontroller dokumentlenker, struktur og `git diff --check`.

Stopp berørt arbeid og rapporter uløste autoritative dokumentkonflikter, arkitekturbrudd, uavklart lisens eller nødvendig utvidelse av scope. Ikke omgjør aksepterte ADR-er uten arbeidsordre.

Lokal scratch, cache og agenttilstand skal ligge i `.agent-local/`. Flytt varig prosjektkunnskap til riktig dokument før lokale arbeidsfiler disponeres.
