# Fase 2.3 – UltraStar-import og bevart kildemetadata

Status: aktiv, bestilt 2026-10-08. Branch: work/phase2-ultrastar-import.
Prosjekteieren valgte unversionerte filer + UltraStar v1, enkeltstemme først og metadata bevart gjennom lagring/gjenåpning. Duetter skal avvises, ikke nedgraderes.

Autoritet: PROJECT_MASTER.md §9–10/13–16, aksepterte ADR-0001/0002 og formatspesifikasjonen ved 7328e4df4ad9b88cb7120d4c9d8177a87945a57c. Kildene er MIT; egen parser skrives, ingen upstream parser kopieres.

## Kontrollpunkter og acceptance criteria

- [x] Metadata/migrasjon: generisk importkilde/header-samling i Core, prosjektformat v2 og eksplisitt v1-load-migrasjon; full rundtur uten metadata-/identitetstap.
- [x] Import (absolutt modus; relativ avgrensning venter på svar): separat UltraStar-adapter, eksplisitt profil, korrekt beat/pitch/GAP/video-konvertering, note-/frasestruktur, diagnostikk med linjenummer og tydelig avvisning av duett/ukjente major-versjoner.
- [ ] Verifikasjon: håndskrevne syntetiske fixtures, filtekst/encoding/robusthet, import → redigering → lagring/gjenåpning, Windows/Linux-tester og avgrenset PR.

Ingen writer/eksport, GUI, lyd, acquisition, modeller eller duettmodell. Ingen metadata/nettressurser hentes ved load. Cover/album/ukjente headers beholdes som original kildemetadata; aktuelle Title/Artist/Language og redigerte noter er domenets sannhet.

Åpent avklaringspunkt: legacy #RELATIVE:YES har en uklar initialoffset i primærkilden. Prosjekteieren er spurt om tydelig avvisning i første importer eller egen avklaring/implementasjon. Normal absolutt modus og metadataarbeidet er uavhengig av svaret.

Plan: én konsistent del om gangen, relevante tester og commit før neste del; ingen parallelle agenter/tunge benchmarks. Testscratch under .agent-local/. Master og aksepterte ADR-er bevares.

PR #1–#3 er fortsatt åpne. Ny PR avgrenses mot work/phase2-project-storage; ingen automatisk merge. Aktiv CI er fortsatt ikke tilgjengelig med dagens workflow-scope.

Stop conditions: uløst autoritetskonflikt, ny uavklart lisens, større scope eller nødvendig brukerbeslutning. Ikke «rett» ukjente noter/stemmer eller metadata ved stille sletting.

2026-10-08: første kontrollpunkt verifisert med Release-build uten warnings og 114/114 tester. V1 beholdes; v2 bevarer importkilde og samtlige headers.

2026-10-08: andre kontrollpunkt: separat importer og 65 importtester. Windows Release: 179/179 totalt. Relativ modus er ikke endelig avgrenset; leveransen er ikke ferdig før svar.
