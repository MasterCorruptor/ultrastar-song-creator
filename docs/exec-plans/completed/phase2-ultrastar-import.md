# Fase 2.3 – UltraStar-import og bevart kildemetadata

Status: gjennomført og verifisert 2026-10-08; klargjort for review i [PR #4](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/4). Branch: work/phase2-ultrastar-import, base work/phase2-project-storage.

Prosjekteieren valgte unversionerte filer + v1, enkeltstemme først og metadata bevart gjennom save/reopen. Etter første draft bestilte eieren avklaring og implementering av relativ modus. Duetter skal avvises, ikke nedgraderes.

Autoritet: PROJECT_MASTER.md §9–10/13–16, aksepterte ADR-0001/0002 og formatspesifikasjonen ved 7328e4df4ad9b88cb7120d4c9d8177a87945a57c. Egen parser er skrevet; upstream parserkode/biblioteker er ikke kopiert.

## Kontrollpunkter og acceptance criteria

- [x] Metadata/migrasjon: generisk importkilde/header-samling i Core, prosjektformat v2 og eksplisitt v1-load-migrasjon; full rundtur uten metadata-/identitetstap.
- [x] Import: separat adapter, eksplisitt absolutt/legacy relativ profil, korrekt beat/pitch/GAP/video-konvertering, note-/frasestruktur og diagnostikk med linjenummer.
- [x] Relativ avklaring: pinnede USDX-/Vocaluxe-kilder viser initial beat-offset 0 og GAP separat; forskjeller mot formatteksten/Performous er dokumentert i RELATIVE_TIMING.md. Legacy YES støttes med eksplisitt warning; v1 YES forblir avvist. Akkumulerte grenser kontrolleres før konvertering.
- [x] Verifikasjon: håndskrevne syntetiske fixtures, tekst/encoding/robusthet, import → edit → undo/redo → save/reopen og Windows/Linux-tester.
- [x] Integritet/publisering: master/aksepterte ADR-er og v1-fixture byteuendret, lenker/public inventory/privacy og Git-whitespacekontroll bestod. Avgrenset PR uten privat sangmateriale/hardware/cache.

## Gjennomføring og resultat

Første kontrollpunkt innførte metadata/migrasjon med 114 tester. Absolutt importer ga 180 tester og draft-PR med relativ avklaring åpen. Prosjekteierens videre bestilling lukket avklaringen med 23 nye tester og dokumentert kompatibilitetsprofil.

203/203 tester bestod på Windows og nettisolert Linux med SDK 10.0.401. Locked restore/Release build uten warnings og Windows formatkontroll bestod. Testscratch/artifacts ble holdt under .agent-local/. Ingen konto, modeller, medier eller nettressurser brukes av import/test/load. CI-malen er oppdatert, men hosted CI er fortsatt ikke aktiv med dagens workflow-tilgang.

Ingen writer/eksport, GUI, playback, acquisition, analysemodell eller duettmodell inngår. Originale headers er proveniens; aktuelle metadata/noter er domenets sannhet. Ingen tapsfri råteksteksport eller kompatibilitet med alle relative parserdialekter loves. Eksterne spillbinærer er ikke kjørt; evidensen er pinnet kildekode og egne regresjonstester.

PR #1–#3 er fortsatt åpne, og #4 bygger på #3. Ingen automatisk merge. Fase 2.3 er ferdig som avgrenset deloppgave, ikke hele Fase 2. Neste mulige arbeidsordre er UltraStar writer/eksport med eksplisitte regler for kvantisering, metadata og round-trip; den skal ikke startes automatisk.
