# Fase 2.2 – versjonert prosjektlagring

Status: aktiv, bestilt av prosjekteieren 2026-10-08. Arbeidsbranch: work/phase2-project-storage.

Mål: lagre og gjenåpne dagens redigerte sang og analyse-cache/referanser uten ny analyse. Autoritet: PROJECT_MASTER.md §9–10/16, aksepterte ADR-0001/0002 og [v1-kontrakten](../../PROJECT_FORMAT.md).

## Acceptance criteria og kontrollpunkter

- [x] Format/library: separat .NET Projects-lag, eksplisitte DTO-er, v1-discriminator og versjon, tapfri rundtur og håndskrevet fixture; test/commit.
- [ ] Robusthet: strict schema/unknown-version/error/warning, filgrense og cancellation, gammel fil beskyttet ved mislykket save; test/commit.
- [ ] Overlevering: locked build/test på Windows/Linux, format/master/ADR/lenkekontroll, oppdatert status og avgrenset PR.

Scope: snapshot/filformat, analysepunkter/provenance, media-/cache-referanser, eksisterende Song-validator og filoperasjoner. Ingen nye runtime-dependencies; core forblir uavhengig av JSON/fil-I/O.

Out of scope: UltraStar import/eksport, GUI/autosave, audio/Python, eksterne filnedlastinger/mediapakker og faktisk migrasjon fra et ikke-eksisterende eldre format.

Verification: fixture/rundtur med redigerte noter og analysis, ugyldig/ukjent JSON, versjoner, kultur, fil-I/O, byteintegritet ved failure/cancel og tidligere regresjonstester. Testscratch under prosjektets .agent-local/. Oppgaven deles i komplette commits; ingen parallelle agenter eller tunge modeller.

Stop conditions: master/ADR-konflikt, uavklart dependencylisens eller behov for større scope; ikke skjul datasletting eller nedgradering.

Fase 1/2.1 er levert i åpne PR-er #1/#2. Ny PR avgrenses mot work/phase2-core-domain; ingen automatisk merge. CI-tilgang mangler workflow-scope, så lokalt verifisert CI-mal beholdes som mal.

Neste kontrollpunkt: implementer schema og eksplisitt mapping uten å koble serialisering til domenets avledede properties.

Kontrollpunkt 1: Release build uten advarsler og 60/60 tester bestod på Windows. Uavhengig v1-fixture, alle note-/analyse-enums, redigert tilstand, nullable fields, Unicode, kultur, deterministisk output og advarsler/referanser er prøvd. Neste steg: robusthet og filfeil.
