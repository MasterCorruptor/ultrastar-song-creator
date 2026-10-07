# Agentkart

UltraStar Song Creator er et planlagt desktopverktøy for UltraStar-sanger. Editoren er hovedprioriteten; Windows først, senere Linux.

## Les først

Les `docs/PROJECT_MASTER.md` (v0.5), arbeidsordren og relevante aksepterte ADR-er i `docs/decisions/`. Masteren regulerer prosjektkravene og endres bare ved eksplisitt arbeidsordre. `docs/DEVELOPMENT.md` beskriver arbeidsflyten; `docs/PHASE1_AUDIT.md` gir aktuell kartlegging. Foreslåtte ADR-er er ikke vedtak.

## Autoritet og arbeidsflyt

Lokal autoritativ working tree på prosjekteierens primærmaskin står over GitHub `main`/`origin/main`, andre stabile referanser og WIP-brancher. Bevar godkjent lokal informasjon; tidsstempler avgjør ikke autoritet. Se masteren §18.5.

Bruk avgrensede `work/<oppgave>`-brancher for ikke-trivielt arbeid. Commit ved konsistente kontrollpunkter. Integrer når acceptance criteria, verifikasjon og eventuell påkrevd godkjenning er oppfylt. `main` skal være stabil og synkronisert.

## Scope og arkitektur

Arbeid i `src/`, `tests/`, `tools/` og relevant `docs/` innenfor arbeidsordren. Endre rotfiler når oppgaven krever det. Aktuell fase er Fase 1: research og avgrensede PoC-er. Produksjonsimplementering og Fase 2 krever ny arbeidsordre.

Bevar skillet acquisition/generation/editor, separat intern sangmodell og grunnleggende undo/redo. Kjernefunksjoner skal fungere lokalt uten obligatoriske tredjepartskontoer/API-nøkler. Ingen produksjonsstack er vedtatt. Prosjekteieren godkjenner vesentlige teknologivalg. Uavklarte modell-/binærlisenser blokkerer innføring av berørte komponenter.

## Verifikasjon og stopp

Produktets build/setup/testkommandoer er ikke etablert. Evalueringskommandoer og begrensninger finnes i `tools/phase1/README.md`. Kontroller også dokumentlenker, masterintegritet og `git diff --check`.

Stopp berørt arbeid og rapporter uløste autoritative dokumentkonflikter, arkitekturbrudd, uavklart lisens eller nødvendig scopeutvidelse. Ikke omgjør aksepterte ADR-er uten arbeidsordre.

Scratch, cache og agenttilstand hører hjemme i Git-ignorert `.agent-local/`. Flytt varig prosjektkunnskap til riktig dokument.
