# Fase 2.3 – UltraStar-import og bevart kildemetadata

Status: aktiv, bestilt 2026-10-08. Branch: work/phase2-ultrastar-import.
Prosjekteieren valgte unversionerte filer + UltraStar v1, enkeltstemme først og metadata bevart gjennom lagring/gjenåpning. Duetter skal avvises, ikke nedgraderes.

Autoritet: PROJECT_MASTER.md §9–10/13–16, aksepterte ADR-0001/0002 og formatspesifikasjonen ved 7328e4df4ad9b88cb7120d4c9d8177a87945a57c. Kildene er MIT; egen parser skrives, ingen upstream parser kopieres.

## Kontrollpunkter og acceptance criteria

- [x] Metadata/migrasjon: generisk importkilde/header-samling i Core, prosjektformat v2 og eksplisitt v1-load-migrasjon; full rundtur uten metadata-/identitetstap.
- [x] Import (absolutt modus; relativ avgrensning venter på svar): separat UltraStar-adapter, eksplisitt profil, korrekt beat/pitch/GAP/video-konvertering, note-/frasestruktur, diagnostikk med linjenummer og tydelig avvisning av duett/ukjente major-versjoner.
- [x] Verifikasjon (PR publiseres som draft med åpen avklaring): håndskrevne syntetiske fixtures, filtekst/encoding/robusthet, import → redigering → lagring/gjenåpning, Windows/Linux-tester og avgrenset PR.

Ingen writer/eksport, GUI, lyd, acquisition, modeller eller duettmodell. Ingen metadata/nettressurser hentes ved load. Cover/album/ukjente headers beholdes som original kildemetadata; aktuelle Title/Artist/Language og redigerte noter er domenets sannhet.

Åpent avklaringspunkt: legacy #RELATIVE:YES har en uklar initialoffset i primærkilden. Prosjekteieren er spurt om tydelig avvisning i første importer eller egen avklaring/implementasjon. Normal absolutt modus og metadataarbeidet er uavhengig av svaret.

Plan: én konsistent del om gangen, relevante tester og commit før neste del; ingen parallelle agenter/tunge benchmarks. Testscratch under .agent-local/. Master og aksepterte ADR-er bevares.

PR #1–#3 er fortsatt åpne. Ny PR avgrenses mot work/phase2-project-storage; ingen automatisk merge. Aktiv CI er fortsatt ikke tilgjengelig med dagens workflow-scope.

Stop conditions: uløst autoritetskonflikt, ny uavklart lisens, større scope eller nødvendig brukerbeslutning. Ikke «rett» ukjente noter/stemmer eller metadata ved stille sletting.

2026-10-08: første kontrollpunkt verifisert med Release-build uten warnings og 114/114 tester. V1 beholdes; v2 bevarer importkilde og samtlige headers.

2026-10-08: andre kontrollpunkt: separat importer og 65 importtester. Windows Release: 179/179 totalt. Relativ modus er ikke endelig avgrenset; leveransen er ikke ferdig før svar.

2026-10-08: Windows og nettisolert Linux bestod 180/180 tester, locked restore og Release build uten warnings. Windows formatkontroll bestod. Metadata-rundtur og v1-migrasjon dekkes; master/aksepterte ADR-er og v1-fixture er byteuendret. Public inventory/lenker/privacy bestod. Ingen ekte lyd, sangtekster eller hardwareinformasjon publiseres.

Status: implementerte kontrollpunkter er verifisert, men Fase 2.3 er ikke avsluttet. Prosjekteierens svar om #RELATIVE:YES er nødvendig for endelig scope. Ny PR holdes draft på work/phase2-project-storage; ingen automatisk merge eller oppstart av writer/eksport.

Publisert draft-PR: [#4](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/4). Tre implementerings-/verifikasjonscommits er pushet; working tree og upstream er konsistente. Neste handling er prosjekteierens relative-mode-svar, deretter eventuell scope-/kodejustering og ferdigstatus. Ikke start writer/eksport automatisk.

2026-10-08: Prosjekteieren bestilte avklaring og implementering av relativ modus. Pinnede USDX-/Vocaluxe-kilder viser initial beat-offset 0 og GAP separat; forskjeller mot formatteksten/Performous er dokumentert i RELATIVE_TIMING.md. Legacy relative støttes med eksplisitt warning; v1 relative forblir avvist. 23 nye regresjonstester; 203/203 bestod på Windows og nettisolert Linux, locked restore/Release build/format uten warnings. Avklaringspunktet er lukket. Neste kontrollpunkt er avsluttende status, integritetskontroll og oppdatering av PR #4 fra draft til review-klar.
