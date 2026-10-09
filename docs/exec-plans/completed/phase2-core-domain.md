# Fase 2.1 – core domain og grunnleggende undo/redo

Status: gjennomført og verifisert 2026-10-08, klar for review. Bestilt 2026-10-08. Arbeidsbranch: work/phase2-core-domain.
Scope: [arbeidsordren](../../PHASE2_FIRST_WORK_ORDER.md). Master og aksepterte ADR-er bevares.

## Rekkefølge og kontrollpunkter

- [x] Modell/build: GUI-uavhengig Song/Phrase/Note og analyse-/mediareferanser, dokumenterte enheter, meningsfulle modelltester, vellykket build/test og commit.
- [x] Validering: stabile errors/warnings, grenseverdier og ikke-muterende kontroller; test og commit.
- [x] Kjerneredigering: MoveNote og snapshot-basert undo/redo, atomiske feil og redo-forgrening; test og commit.
- [x] Overlevering: samlet Windows/Linux-verifikasjon, dependencylisenser/lockfiler, dokumentlenker/masterintegritet, dokumentasjon og avgrenset PR klargjort for publisering.

## Ressursbruk og gjenopptak

Én sammenhengende deloppgave av gangen; ingen parallelle agenter, tunge modeller eller nye sangbenchmarks. Kjør berørte tester før hvert konsistente kontrollpunkt. Registrer faktisk verifikasjon og neste steg her. Ikke la en ferdig del vente på omfattende research i neste del. Kvoten kan ikke garanteres; dersom økten avbrytes, gir planen og commits et eksplisitt gjenopptakspunkt.

Fase 1 er godkjent, men PR #1 er ennå ikke merget. Denne branchen bygger på det godkjente lokale grunnlaget; ny PR skal avgrenses mot Fase 1-branchen inntil den er integrert. Ingen automatisk merge.

## Status

Oppstart: SDK 10.0.401 fra lokalt evalueringsmiljø gjenbrukes. Core uten eksterne runtimepakker. xUnit/VSTest fra SDK-mal brukes som testverktøy; unødvendig coverage-dependency tas ut. Ingen prosjektserialisering, UltraStar-parser/writer, GUI, lyd eller analyse integreres.

Kontrollpunkt 1: restore/build Release bestod uten advarsler; 3/3 modelltester bestod på Windows. SDK 10.0.401, xUnit 2.9.3, runner 3.1.4 og Test.Sdk 17.14.1. Neste steg: ren validator og grenseverdier.

Kontrollpunkt 2: 35/35 tester bestod. Validatoren gir stabile paths/koder, errors for strukturelle problemer og warnings for utkastkontekst; ingen mutasjon. Neste steg: MoveNote og undo/redo.

Kontrollpunkt 3: 55/55 tester bestod på Windows. MoveNote bevarer varighet/tekst/confidence/proveniens, undo/redo gjenoppretter eksakte snapshots, feil/no-op bevarer historikken og ny endring etter undo forkaster redo-grenen. Neste steg: format-/Linux-/dependencykontroll og PR.

Sluttkontroll: locked restore, Release build uten advarsler, 55/55 tester på Windows og nettisolert Linux-container samt dotnet format --verify-no-changes bestod. Faktiske testpakker/lisenser/hashes er inventarført; core har ingen eksterne runtimepakker. Master/aksepterte ADR-er, lokale lenker og offentlig payload kontrolleres før siste commit. Planen arkiveres som gjennomført; review/merge gjenstår. Neste anbefalte bestilling er versjonert prosjektlagring, deretter UltraStar parser/writer.

Publiseringskontroll: GitHub avviste innføring av .github/workflows/core.yml fordi tilgjengelig token mangler workflow-scope. Den samme ferdige YAML-en leveres som tools/ci/core.yml uten aktiv CI. Lokale Windows/Linux-tester er verifisert; CI-aktivering er en separat tilgangsoppgave og blokkerer ikke den bestilte domenedeloppgaven.
