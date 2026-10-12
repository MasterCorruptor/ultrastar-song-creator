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

## GUI-01 native-smoke – separat SMALL-ordre 2026-10-12

STATUS: CHECKPOINT (native acceptance fortsatt ufullstendig). TASK: gui-01-native-smoke. MODEL USED: ukjent; bestilt Sol Medium. SEQUENTIAL, én worker. Base/kodecheckpoint: 1f95b8c14e202cdbdb9d9adf303aceea0202fe1e. Egen kvote før start: 97 % femtimer / 67 % uke gjenstår, faktisk avlest etter reset. SUPERVISORs kode-/pakkehashreview godkjente ingen full acceptance og ingen merge. Denne delen dokumenterer det historiske automatiseringsforsøket; gjeldende native status følger under manuell eierverifikasjon.

| Native scenario | Faktisk observert resultat |
| --- | --- |
| Start og målvalg | Eksisterende Release-apphost startet uten restore/build; kun eget GUI-01-vindu valgt gjennom returnert sky-window. |
| Før redigering | Accessibility-tre viste Egen testsang, sekunder 0–8, MIDI 58–66, Hei/på/deg!, Ingen note valgt, tomt startfelt, disabled Flytt/Angre/Gjør om, manglende-lyd-warning. |
| Skjermfangst | get_window_state med screenshot feilet: FrameArrived timed out. Ved ett aktiverings-/gjenopprettingsforsøk: window capture timed out. Ingen skjermbilde-evidens. |
| Velg Hei | Klikk mot faktisk observert element avvist: coordinate input geometry is unavailable. Ingen vellykket input/resultat påstås. |
| Flytt 1→2,5; Angre→1; Gjør om→2,5 | Ikke utført/verifisert native fordi input-geometri manglet. |
| NaN / negativ / utenfor frase; overlapp ved 3 | Ikke utført/verifisert native. Bestått headless-evidens erstatter ikke dette. |
| Normal close / prosessavslutning | Ikke utført/verifisert; eget testvindu er bevart til manuell prøve. |

COMPLETED: avgrenset verifikasjonsforsøk, presist fangst-/inputhinder og manuell oppskrift dokumentert. Verktøyet returnerte ikke en ny apptillatelsesforespørsel; ingen slik forespørsel er kjent som ventende. Ingen gjentatte lange approval-venter eller alternativ UI-automatisering. Privat logg i .agent-local/gui-01/native-smoke-attempt.json; ingen personlige vinduer/data eller skjermbilder publiseres.

VERIFICATION: repo/remote checkpoint og main identitet kontrollert; source/tests/tools fortsatt identiske med 1f95b8c. Dokumentlenker/inventar/privacy/whitespace kontrolleres før commit. Tidligere 432 Windows/Linux-tester/build/format gjenbrukes uten gjentakelse; denne ordren endrer bare dokumentasjon. PR #10 holdes draft, main 005a708 bevart. FILES: denne aktive planen og GUI_01_TEST_SURFACE, arbeidsordrens native-tillegg. LAST COMMIT rapporteres fra Git etter dokumentasjonscommit/push.

UNRESOLVED: skjermfangst/input-geometri hindrer native acceptance. NEXT RECOMMENDED STEP: SUPERVISOR vurderer reparert appkontroll eller konkret manuell eierprøve (Hei→2,5→Angre/Gjør om; NaN/-1/7,5; start3-overlapp; vanlig X-lukking). Ikke ny funksjonsblokk/merge. OWNER DECISION REQUIRED: ingen ny teknologigodkjenning; manuell verifikasjon kan kreve eierens praktiske medvirkning.

## Manuell eierverifikasjon – 2026-10-12

STATUS: CHECKPOINT, native scenarioer bestått manuelt; endelig SUPERVISOR-review gjenstår. TASK: gui-01-native-smoke. MODEL USED: ukjent; bestilt Sol Medium. BRANCH: work/gui-01-song-editing. Kode/tests/tools identiske med 1f95b8c.

Eieren opplyste at annen samtidig datamaskinbruk hadde skjedd under klikktesten. Ett nytt foreground-forsøk gjennom samme Computer Use-workflow fikk fortsatt FrameArrived timed out og ble straks stoppet. Årsaken er ikke teknisk bekreftet; ingen produktdefekt påstås. Ingen ny tillatelsesforespørsel/alternativ automation brukt.

Etter den konkrete firepunkts manuelle oppskriften bekreftet prosjekteieren direkte: «Alle tester er bestått med forventet resultat».

| Native scenario | Evidens/resultat |
| --- | --- |
| Hei-valg, flytt start1→2,5 | Eierens manuelle test: forventet valg/starttid/blokkoppdatering bestått. |
| Angre→1, Gjør om→2,5 | Eierens manuelle test: forventet gjenoppretting bestått. Eksakte snapshotreferanser er tidligere headless-evidens. |
| NaN, −1, 7,5 | Eierens manuelle test: avvisning uten endret note bestått. |
| Start3 | Eierens manuelle test: forventet overlapp-warning bestått. |
| Normal X-lukking | Eierens manuelle test: bestått. Separat prosesskontroll etter bekreftelsen viste 0 GUI-01-prosesser, uten nytt UI-input. |

COMPLETED: native scenario-gap lukket med eksplisitt menneskelig evidens. VERIFICATION: bare dokumentasjon/status endret; tidligere 432 Windows/Linux/build/format gjenbrukes kildeidentisk, ingen suite/build/restore gjentas. Dokumentlenker/privacy/whitespace og git/sourceidentitet kontrolleres. FILES: testguide, denne aktive planen og korte statushenvisninger i AGENTS/README/TESTING_STATUS/DEVELOPMENT.

UNRESOLVED: ingen gjenværende native scenario-gap etter eierens bekreftelse; automatisert fangst/screenshot er fortsatt utilgjengelig. Ingen native Linux-GUI, lyd eller release inngår. NEXT RECOMMENDED STEP: endelig SUPERVISOR-review og uttrykkelig integrasjonsordre. PR #10 holdes draft; ingen merge/ny blokk. OWNER DECISION REQUIRED: NO. LAST COMMIT leses fra Git/finalrapport etter dokumentasjonscommit.
