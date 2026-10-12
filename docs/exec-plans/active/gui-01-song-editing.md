# GUI-01 – aktiv plan

STATUS: CHECKPOINT. TASK: gui-01-song-editing-test-surface. BRANCH: work/gui-01-song-editing. Base: 005a7089742ce7ad33f3103460a7446ee6e20164. MODEL USED: ukjent; bestilt Sol Medium. MEDIUM/SEQUENTIAL, én worker.

[Arbeidsordre](../../GUI_01_WORK_ORDER.md). Kvote før start: 73 % femtimer / 78 % uke gjenstår, avlest kontoverktøy; ingen ekstra workers.

1. Etabler pinnet desktopprosjekt og snapshot-basert rendering/fixture.
2. Koble notevalg/MoveNote/History/validering og meningsfulle headless UI-tester.
3. Locked restore/build/test/format; faktisk Windows input-smoke; Linux build/headless når praktisk; integritet/lisens/inventar.
4. Oppdater startinstruks/status/evidens; commit/push; stopp for SUPERVISOR-review. Ingen merge eller playback.

## Verifisert implementasjonscheckpoint

COMPLETED: Desktop-vindu, deterministisk egen Song, sekund-/MIDI-timeline med notevalg, MoveNote/SongHistory, undo/redo/validering og startscript. Ingen Core-/prosjekt-/formatendring. [Startguide/kontrakt](../../GUI_01_TEST_SURFACE.md) og [pakkemanifest](../../GUI_01_DEPENDENCIES.json).

VERIFICATION: Windows locked restore/Release build/test/SDK-format består; 0 warnings/errors ved endelig build. 422 eksisterende + 10 ekte headless input/layout/historikk-tester. Linux eksisterende container, network none, cache og egne artifacts; audit av kun offline: locked restore/Release build/test består, samme 432/432. Master/ADR/historiske v1-fixtures og åtte tidligere lockfiler kontrolleres byteidentiske mot base; ingen hosted CI. Tidlig rebuild mens testvinduet kjørte fikk apphost-lås; egen prosess ble stoppet, endelig build bestod.

Native Windows-vindu/prosess startet og responderte. Computer Use get_window_state krevde egen apptillatelse og returnerte «Computer Use app approval timed out». Ingen native input/screenshot/normal close-evidens; dette er et konkret acceptance-gap, ikke bestått. Ingen alternativ UI-automatisering brukt for å omgå tillatelsen. GUI-runtime er framework-dependent; ingen installer/release, native Linux-GUI eller playback. Native prosessen stoppet før rebuild; kan åpnes med startscript.

FILES / MODULES CHANGED: nye Desktop og Desktop.Tests, to nye lockfiler, solution, tools/desktop-startscript, arbeidsordre/plan/inventar/startguide og korte statusdokumenter. Eksisterende moduler/locks/master/ADR er bevart.

UNRESOLVED: faktisk native input og normal close må verifiseres med tilgjengelig appkontroll eller menneskelig prøve før full acceptance; ingen kjent headless/domain-regresjon. NEXT RECOMMENDED STEP: SUPERVISOR reviewer checkpoint og avklarer akkurat native verifikasjonsgap før eventuell integrasjon. Ingen ny playback-/GUI-blokk eller merge. OWNER DECISION REQUIRED: NO for teknologivalg; plattformens apptillatelse må gis dersom Computer Use skal brukes. Faktisk modell/reasoning ukjent; bestilt Sol Medium. Kvote ved kontroll etter apptidsavbrudd: 19 % femtimer / 70 % uke gjenstår. LAST COMMIT leses fra Git/finalrapport, denne planen inngår i samme checkpoint.

Start-Desktop.ps1 er prøvd på Windows: locked restore/build bestod uten advarsler, korrekt Unicode-vindustittel og responderende apphost observert etter UTF-8-retting. Testvinduet er åpnet for lokal brukerprøve. Native input/normal close er fortsatt uprøvd.
