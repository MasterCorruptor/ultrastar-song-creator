# Fase 2.2 – versjonert prosjektlagring

Status: gjennomført og verifisert 2026-10-08, klar for review. Bestilt av prosjekteieren 2026-10-08. Arbeidsbranch: work/phase2-project-storage.

Mål: lagre og gjenåpne dagens redigerte sang og analyse-cache/referanser uten ny analyse. Autoritet: PROJECT_MASTER.md §9–10/16, aksepterte ADR-0001/0002 og [v1-kontrakten](../../PROJECT_FORMAT.md).

## Acceptance criteria og kontrollpunkter

- [x] Format/library: separat .NET Projects-lag, eksplisitte DTO-er, v1-discriminator og versjon, tapfri rundtur og håndskrevet fixture; test/commit.
- [x] Robusthet: strict schema/unknown-version/error/warning, filgrense og cancellation, gammel fil beskyttet ved mislykket save; test/commit.
- [x] Overlevering: locked build/test på Windows/Linux, format/master/ADR/lenkekontroll, oppdatert status og avgrenset PR klargjort.

Scope: snapshot/filformat, analysepunkter/provenance, media-/cache-referanser, eksisterende Song-validator og filoperasjoner. Ingen nye runtime-dependencies; core forblir uavhengig av JSON/fil-I/O.

Out of scope: UltraStar import/eksport, GUI/autosave, audio/Python, eksterne filnedlastinger/mediapakker og faktisk migrasjon fra et ikke-eksisterende eldre format.

Verification: fixture/rundtur med redigerte noter og analysis, ugyldig/ukjent JSON, versjoner, kultur, fil-I/O, byteintegritet ved failure/cancel og tidligere regresjonstester. Testscratch under prosjektets .agent-local/. Oppgaven deles i komplette commits; ingen parallelle agenter eller tunge modeller.

Stop conditions: master/ADR-konflikt, uavklart dependencylisens eller behov for større scope; ikke skjul datasletting eller nedgradering.

Fase 1/2.1 er levert i åpne PR-er #1/#2. Ny PR avgrenses mot work/phase2-core-domain; ingen automatisk merge. CI-tilgang mangler workflow-scope, så lokalt verifisert CI-mal beholdes som mal.

Neste kontrollpunkt: implementer schema og eksplisitt mapping uten å koble serialisering til domenets avledede properties.

Kontrollpunkt 1: Release build uten advarsler og 60/60 tester bestod på Windows. Uavhengig v1-fixture, alle note-/analyse-enums, redigert tilstand, nullable fields, Unicode, kultur, deterministisk output og advarsler/referanser er prøvd. Neste steg: robusthet og filfeil.

Kontrollpunkt 2: 103/103 Windows-tester bestod, inkludert strict schema, unknown-version, UTF-8/BOM, filgrenser, avbrudd før I/O, gammel-fil-bevaring og temp-opprydding ved rename-feil. OS kan rapportere IOException eller UnauthorizedAccessException for en mappe som mål; begge kontrolleres. Neste steg: format/locked restore, Linux og overlevering.

Sluttkontroll: 106/106 tester på Windows og nettisolert Linux, locked restore og Release build uten advarsler, samt formatkontroll bestod. Ingen ny ekstern dependency. Master/aksepterte ADR-er og offentlig payload kontrolleres før siste commit. CI-mal er oppdatert; aktiv CI er fortsatt blokkert av manglende workflow-scope. Deloppgaven er gjennomført; review/merge gjenstår. Neste foreslåtte deloppgave: avgrenset UltraStar parser/writer mot offisiell formatprofil.

Avsluttende enum-kontroll: v1 bruker et fryst sett med eksakte enum-navn; sammensatte/numeriske strings og feil case avvises. Dette løser standardkonverterens aksept av kombinerte navn og hindrer at fremtidige domene-enums utvider schema v1 automatisk. 106/106 tester bestod etter rettingen på begge plattformer.
