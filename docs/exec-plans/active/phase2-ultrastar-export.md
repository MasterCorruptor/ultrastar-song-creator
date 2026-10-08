# Fase 2.4 – avgrenset UltraStar-eksport

Status: bestilt 2026-10-08; eksportvalgene er avklart. Branch work/phase2-ultrastar-export bygger på Fase 2.3-head 25e1c7532e74997c1d8577ae15d3e6bf3e0b270f. PR #1–#4 er fortsatt åpne; ingen automatisk merge.

Autoritet: PROJECT_MASTER.md §9/13–14/16–17, aksepterte ADR-er og de pinnede formatkildene/importprofilen. Intern sangmodell skal forbli adskilt fra UltraStar-writeren.

## Avklaringer sendt før implementering

1. Eksportprofiler: unversionert + v1 med absolutt tid, også legacy relativ eksport, eller bare v1.
2. Avrunding: blokkere som standard med eksplisitt valg, eller automatisk avrunding med rapport.
3. Leveranse: .txt og mediereferanser, eller komplett mappe med kopierte medier nå.

Svar bestemmer acceptance criteria og omfang. Ingen av disse valgene er antatt. Kartlegging og uavhengig kontraktarbeid kan fortsette mens avklaringen står åpen.

## Kontrollpunkter

- [x] Avgrenset arbeidsordre og eksplisitte eksport-/kvantiserings-/metadataregler.
- [ ] Separat writer/validering med rapporterte endringer og uten mutasjon av prosjektet.
- [ ] Meningsfulle import/export-rundturer og uavhengige syntetiske forventningsverdier.
- [ ] Windows/Linux, locked restore/build/test/format, master-/ADR-/v1-fixtureintegritet og public inventory/privacy.
- [ ] Dokumentasjon, avgrensede commits og PR mot work/phase2-ultrastar-import; ingen automatisk merge/neste fase.

## Forhold kartlagt fra eksisterende kontrakter

Source headers er opprinnelig proveniens, mens aktuelle note-/metadata-/media-/offsetverdier er sannhet. Writeren må ikke gjenskape gamle redigeringer fra source headers. #BPM i kildefilen er en kvantiseringsbase og ikke pålitelig musikalsk tempo; BPM skal ikke gjettes fra Song.BeatsPerMinute.

Core tillater draft-data som formatet ikke kan representere direkte: negativ audio-offset, tom notetekst, tomme fraser, usorterte/overlappende noter og flere audio-/videoreferanser. Eksportpolicy må håndtere disse eksplisitt før filen endres. Note-/fraserekkefølge kan ikke omskrives uten avklart regel/diagnostikk.

Relative media-/coverreferanser gjelder original sangfilens mappe når en importkilde finnes; å skrive til en annen mappe krever eksplisitt referansebase/rebasing eller mediakopiering. Ukjente headers bevares semantisk, men operative headers må følge valgt outputprofil og aktuell modell. Beat-baserte medley-headers trenger særlig omtanke hvis eksportens grid endres.

Ingen produksjonsdependency eller prosjektformatendring er planlagt uten behov/egen begrunnelse. GUI, acquisition, analyse, playback og duettmodell inngår ikke i den foreslåtte kjernewriteren.

2026-10-08: Prosjekteieren valgte unversionert + v1, begge med absolutt tid; automatisk avrunding med rapporterte avvik; komplett sangmappe med kopiert lyd/bilder/video. Implementeringen deles i writer/timing og pakking/fil-I/O med konsistente commits før sluttsjekk. Relative importer normaliseres til absolutt output. Ingen ny avrundings-/pakkeavklaring står åpen.

2026-10-08: writer/timing-kontrollpunkt: 241/241 Windows-tester bestod (203 tidligere + 38 writer-tester), Release build uten warnings. Ren Render og per-note-endpointrapport er implementert; komplett pakking er neste kontrollpunkt. Source/prosjekt/Core er ikke mutert.
