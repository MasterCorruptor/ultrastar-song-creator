# Core domain – første avgrensede kontrakt

Fase 2.1 bruker C#/.NET 10 og immutable snapshots. Kontrakten er intern og har ingen versjonert lagrings-/IPC-/eksportrepresentasjon.

## Enheter og identitet

Song-tid er sekunder fra sangens logiske nullpunkt. Note/Phrase starter ikke før null; ender er eksklusive, slik at berørende noter ikke overlapper. Note lagrer start og varighet separat; MoveNote bevarer varigheten eksakt. Alle tider skal være finite. Audio/video-offset er signert: medietid = sangtid + offset. BPM er valgfritt, finite og positivt når oppgitt; dette er en enkel konstant tempoverdi, ikke en vedtatt variabel-tempo-/UltraStar-konvertering.

Note-pitch er heltallig MIDI 0–127 (C4 = 60). Pitch-analysedata kan beholde kontinuerlig MIDI-verdi. Dette er ikke UltraStar-formatets relative pitch eller ticks. Freestyle/rap har fortsatt et lagret pitchfelt; eksportregler og andre pitchrepresentasjoner kommer senere.

Song, Phrase, Note, MediaReference og AnalysisArtifact har ikke-tomme, stabile Guid-identiteter som skal være unike i sangens objektgraf. Analyse-/mediareferanser peker på identiteter, ikke filformatposisjoner. Validatoren åpner ikke mediefiler eller nettressurser.

## Redigering og rå analyse

Records, ImmutableArray og immutable verdier hindrer at brukerredigering muterer gamle snapshots. Note kopieres ved en endring; urørte fraser, metadata og AnalysisData gjenbrukes. Analysepunkter ligger separat, og rå arrays blir ikke kopiert per noteendring. Artefakter kan peke på eksternt analyseinnhold; ingen cache-/prosjektfilformat defineres her.

Confidence er valgfri, finite og 0–1. Null betyr ukjent, ikke null sikkerhet. Score er ikke kalibrert mellom modeller; Producer/ModelRevision og analyse-ID bevares. Analysepunkter har tid/verdi; kind angir waveform, pitch, beat, vocal, alignment eller confidence. Alignment/stems kan referere eksternt innhold. Dette er referansebærere, ikke en ferdig analyseprotokoll.

## Validering og kommandoer

Validatoren skal returnere stabile kode/severity/path/entity-funn uten mutasjon. Strukturelle feil er errors; overlapp og manglende tekst/metadata/media er warnings for et redigerbart utkast. Overlapp vurderes på tvers av fraser uten antakelse om at en duett må slettes. Fremtidige voices/tracks kan gi mer presis kontekst.

MoveNote gjelder én entydig note og en absolutt ny starttid innenfor samme frase. Frasegrenser endres ikke automatisk. En ugyldig endring kastes før et nytt snapshot publiseres. History eier én aktuell sang, og undo/redo gjenbruker de eksakte før/etter-snapshottene. Kommandoer skal være rene; historikken er for én redigeringstråd, ikke samtidige writers.

Prosjektlagring, import/eksport, GUI, playback og Python er utenfor Fase 2.1.

## Praktisk validerings- og historikkpolicy

Warnings blokkerer ikke MoveNote. Kommandoen kontrollerer egen målidentitet, notevarighet og frase-/tidsgrenser, men blokkerer ikke reparasjon av timing på grunn av en urelatert eksisterende tempo-/draftfeil. Validatoren kan kjøres separat på hele utkastet. Manglende freestyle-tekst er tillatt; manglende tekst for øvrige notetyper gir warning. Uinitialiserte collections/null objekter gir strukturerte errors. Kun finite datapunkter tillates i det minimale analyselaget; detaljert payload-/modellvalidering er senere arbeid.

Samlingene bevarer innlagt rekkefølge; flytting endrer ikke lyrics-rekkefølge eller sorterer noter automatisk. Phrase.Text er avledet fra note-teksten i denne rekkefølgen, ikke en separat lyricsfasit. En no-op lager ingen historikkpost og beholder redo. Historikken bevarer hele immutable snapshots med delt analysedata; historikkbegrensning/lagring og flertrådsredigering kommer senere.

## Separat prosjektlagring – Fase 2.2

Projects-laget mapper domenet til eksplisitte [v1-DTO-er](PROJECT_FORMAT.md) og lokal JSON-fil. Core kjenner fortsatt ikke JSON, filbaner for prosjektfilen eller save/load. Aktuelle brukerendringer og rå analysepunkter lagres uten ny analyse; undo-stakkene er ikke persistente. Datakontrakten for v1 er skilt fra domenets fremtidige utvikling.

## Generisk importkilde – Fase 2.3

Song.ImportedSource er valgfri SourceDocument med FormatId, valgfri FileReference og immutable SourceHeader-samling (Name/Value). Den beskriver opprinnelig kildemetadata, ikke aktuell redigert sangsannhet. Originale/ukjente headers og rekkefølge overlever snapshotendringer, undo/redo og [prosjektformat v2](PROJECT_FORMAT_V2.md). Core kjenner ikke headersemantikk, encoding eller UltraStar-fil-I/O. SourceFormatId må være ikke-tom og headernavn/verdier tilstede; duplikater/tomme verdier tillates i proveniensen.

[UltraStar-adapteren](ULTRASTAR_IMPORT.md) bruker denne generiske samlingen, konverterer til sekunder/MIDI og holder kvantiserings-BPM i kilden fremfor å anta musikalsk tempo. Dette endrer ikke domenets tids-/pitch- eller snapshotkontrakt.

## Separat writer/pakker – Fase 2.4

[UltraStar-eksport](ULTRASTAR_EXPORT.md) projiserer immutable Song til formatets representerbare data og kvantiserer i adapteren. Originale sekunder, ID/confidence, analysedata og source metadata muteres ikke. Eksportfilen er adskilt fra prosjektets v1/v2-kontrakt; Core kjenner fortsatt ikke UltraStar-ticks, kopiering, JSON eller codec-/fil-I/O. Ingen Core-kontraktendring inngår.
