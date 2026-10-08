# Arbeidsordre: Fase 2.4 – UltraStar writer og komplett sangmappe

Status: bestilt og gjennomført 2026-10-08; klar for review. Prosjekteieren godkjente neste avgrensede trinn etter importleveransen og valgte deretter:
- unversionert + UltraStar v1, begge med absolutt tid,
- automatisk avrunding med rapporterte avvik,
- komplett sangmappe med kopierte lyd-, bilde- og videofiler nå.

Autoritet: [masteren](PROJECT_MASTER.md) §9/13–14/16–17 og aksepterte ADR-er. Det eksisterende .NET-formatlaget utvides; Core og prosjektformat v1/v2 holdes uendret. Ingen ny runtimepakke, GUI, analyse, acquisition, playback eller duettmodell.

## Scope og acceptance criteria

1. Ren writer av aktuell redigert Song med eksplisitt outputprofil, UTF-8 uten BOM, LF og absolutt timing. Relative importer normaliseres uten å gjenbruke gammel relativ body.
2. Kvantiseringsbase fra eksplisitt GridBpm eller én gyldig original BPM-header; musikalsk Song.BeatsPerMinute brukes ikke som en gjetning. Start/slutt avrundes til nærmeste beat; midtpunkter bort fra null. Per-note-rapport inneholder originale/eksporterte sekunder, beats og start-/slutt-/varighetsavvik.
3. Noter som kollapser etter avrunding, negative GAP, tvetydige medier, tomme/ugyldige required-felt, ikke-representerbare tider og ugyldig tekst gir error før ferdigstillelse. Ingen stille sletting, forlengelse, pitch-transponering eller omsortering.
4. Gjeldende Title/Artist/Language/media/GAP/video/BPM overskriver original operative metadata. Ukjente/øvrige headers og rekkefølge bevares. Encoding/RELATIVE/VERSION følger outputprofil. Medley-beats reskaleres fra entydig absolutt kildegrid; tvetydige relative cues avvises. Original prosjektmetadata/analysedata/snapshots muteres ikke.
5. Pakke til en ny målmappe med song.txt og lokale medier/bilder. MP3/AUDIO peker på aktuell audio; VIDEO og kildeheaders COVER/BACKGROUND/VOCALS/INSTRUMENTAL pakkes når oppgitt. Referansebase er eksplisitt eller original importfils absolutte mappe, aldri prosessens arbeidsmappe. Ingen metadata eller URL hentes fra nett.
6. Relative referanser omskrives til kopierte filer. Navnekollisjoner og Windows/Linux-navn håndteres deterministisk, og like kildefiler kopieres én gang. Bytelengde og SHA-256 returneres per pakket fil. Ingen transcoding eller kvalitetspåstand om medieinnhold.
7. Forhåndsvalidering, kopiering til unik midlertidig søstermappe og rename som commitpunkt; eksisterende målmappe overskrives ikke. Feil/cancellation før commit etterlater ingen ferdig sangmappe og endrer ikke kildene.
8. Uavhengig forventet .txt, import → export → import, noteendring/prosjektlagring, metadata, avrunding, filbevaring/cancellation og pakkeintegritet testes med syntetiske data. Windows/Linux, locked restore/build/test/format, master-/ADR-/v1-fixtureintegritet, docs/lenker og privacy kontrolleres.

## Kontrollpunkter og leveranse

Writer/timing verifiseres og committes før pakking/fil-I/O. Etter relevante tester publiseres avgrenset PR mot work/phase2-ultrastar-import. Ingen automatisk merge eller oppstart av neste fase. [Avsluttet plan](exec-plans/completed/phase2-ultrastar-export.md) følger gjennomføringen.

Avgrensning: ingen relativ output, duett, ny importdialekt, tapsfri råtekstreproduksjon, mediakonvertering eller kopi av prosjekt/analysedata til sangmappen. ID/confidence/analyse bevares i prosjektet, mens UltraStar-eksport er formatets representerbare sangdata.

Leveranse: [eksportkontrakt](ULTRASTAR_EXPORT.md), [263/263 Windows/Linux-tester og mediesmoke](PHASE2_EXPORT_RESULTS.md), separat writer/pakker og uendret Core/prosjekt. Ingen automatisk merge/neste arbeidstrinn.
