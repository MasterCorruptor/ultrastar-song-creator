# Fase 2.1 – core domain og grunnleggende undo/redo

Status: aktiv, bestilt 2026-10-08. Arbeidsbranch: work/phase2-core-domain.
Scope: [arbeidsordren](../../PHASE2_FIRST_WORK_ORDER.md). Master og aksepterte ADR-er bevares.

## Rekkefølge og kontrollpunkter

- [x] Modell/build: GUI-uavhengig Song/Phrase/Note og analyse-/mediareferanser, dokumenterte enheter, meningsfulle modelltester, vellykket build/test og commit.
- [ ] Validering: stabile errors/warnings, grenseverdier og ikke-muterende kontroller; test og commit.
- [ ] Kjerneredigering: MoveNote og snapshot-basert undo/redo, atomiske feil og redo-forgrening; test og commit.
- [ ] Overlevering: samlet Windows/Linux-verifikasjon, dependencylisenser/lockfiler, dokumentlenker/masterintegritet, ren commit/push og avgrenset PR.

## Ressursbruk og gjenopptak

Én sammenhengende deloppgave av gangen; ingen parallelle agenter, tunge modeller eller nye sangbenchmarks. Kjør berørte tester før hvert konsistente kontrollpunkt. Registrer faktisk verifikasjon og neste steg her. Ikke la en ferdig del vente på omfattende research i neste del. Kvoten kan ikke garanteres; dersom økten avbrytes, gir planen og commits et eksplisitt gjenopptakspunkt.

Fase 1 er godkjent, men PR #1 er ennå ikke merget. Denne branchen bygger på det godkjente lokale grunnlaget; ny PR skal avgrenses mot Fase 1-branchen inntil den er integrert. Ingen automatisk merge.

## Status

Oppstart: SDK 10.0.401 fra lokalt evalueringsmiljø gjenbrukes. Core uten eksterne runtimepakker. xUnit/VSTest fra SDK-mal brukes som testverktøy; unødvendig coverage-dependency tas ut. Ingen prosjektserialisering, UltraStar-parser/writer, GUI, lyd eller analyse integreres.

Kontrollpunkt 1: restore/build Release bestod uten advarsler; 3/3 modelltester bestod på Windows. SDK 10.0.401, xUnit 2.9.3, runner 3.1.4 og Test.Sdk 17.14.1. Neste steg: ren validator og grenseverdier.
