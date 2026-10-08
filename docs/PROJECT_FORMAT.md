# Prosjektformat v1 – Fase 2.2

Et lokalt prosjekt er én UTF-8 JSON-fil, anbefalt filendelse .uscproject. Dette er et prosjektformat for å fortsette redigering, ikke UltraStar-eksport. Ingen nye runtimepakker kreves.

## Kontrakt

Root har nøyaktig format, schemaVersion og song. format er ultrastar-song-creator, og schemaVersion er heltallet 1. Felt er case-sensitive. Hver lagret modell bruker en eksplisitt DTO uavhengig av C#-klassens beregnede properties og senere filformatadaptere.

song lagrer id, metadata (title/artist/language), media (id/kind/location/source), audioOffsetSeconds, videoOffsetSeconds, beatsPerMinute, phrases og analysis. Hver phrase lagrer id/startSeconds/endSeconds/notes. Note lagrer id/startSeconds/durationSeconds/midiPitch/text/type/confidence/analysisReferences. Analysis lagrer artifacts med id/kind/sourceMediaId/producer/modelRevision/contentReference/points; hvert punkt har timeSeconds/value.

Alle definerte fields er påkrevd, også nullable felt. Null er bare tillatt for language, source, beatsPerMinute, confidence, sourceMediaId, modelRevision og contentReference. Andre strenger kan være tomme som utkastdata, men ikke null. Arrays kan være tomme, ikke null. Identiteter/enheter følger [core-kontrakten](CORE_DOMAIN.md). Enumverdier skrives som camelCase-navn; heltallsenums og ukjente navn avvises.

Guid-ID-er, arrayrekkefølge, notevarighet, tekst, confidence og analysereferanser bevares. Note.endSeconds og Phrase.text lagres ikke separat; de er avledet i domenet. Den redigerte aktuelle Song lagres; undo-/redo-stakker gjenopprettes ikke ved gjenåpning.

## Analyse-cache og media

Analysepunktene i dagens AnalysisData lagres inline med provenance, slik at gjenåpning ikke kjører inferens. Eksternt analyseinnhold og lyd/video beholdes som referanser; denne leveransen kopierer, åpner eller laster ikke ned slike filer. Relative lokale referanser tolkes av en fremtidig caller relativt til prosjektfilens mappe; de lagres uendret. Flytting av prosjekt og relativt refererte filer sammen bevarer forholdet. Absolutte referanser er ikke portable mellom maskiner.

## Validering og versjonering

Lagring/gjenåpning bruker SongValidator. Strukturelle errors avvises med funn; utkast-warnings tillates og returneres ved gjenåpning. Ukjente fields, manglende fields, duplicate JSON-properties, null i påkrevde objects og feil typer avvises. Dette hindrer at filen lastes og senere lagres med stille tap av innhold.

v1 er første publiserte prosjektversjon; ingen eldre støttet versjon finnes. Ukjente eldre/nyere versjoner avvises eksplisitt uten å endre filen. Fremtidig v2 må definere egen DTO/migrasjon fra v1 før skriveformatet endres; ingen automatisk versjonsnedgradering skjer.

Standard filgrense er 64 MiB, konfigurerbar i store-konstruktøren. JSON-dybde er begrenset til 64. Dette er første enkle snapshotformat, ikke et optimalisert binært cachearkiv eller en ferdig allokasjonskvote for svært store prosjekter.

## Filoperasjoner

Save validerer og serialiserer før målfilen endres. En unik midlertidig fil opprettes i samme eksisterende mappe, skrives/flusjes og lukkes; deretter erstattes målfilen ved rename/move. Før dette commitpunktet beholder feil/cancellation gammel fil. Cancellation etter commitpunktet kan ikke love at operasjonen er ugjort. Midlertidige filer ryddes ved vanlige feil; prosesskrasj eller låste filer kan etterlate en tempfil.

Dette beskytter mot en halvskrevet prosjektfil ved vanlige programfeil. Full garanti ved strømbrudd, nettverksfilsystemer og samtidige eksterne writers er ikke etablert. Det opprettes ingen medier/cachemapper eller automatisk backup; caller må opprette prosjektmappen. Samtidige saves følger siste fullførte writer.

## Avgrensning

GUI/autosave, prosjektarkiv/mediakopiering, kryptering, recovery-UI, undo-historikklagring, ASR/Python/IPC og UltraStar parser/writer er utenfor denne deloppgaven.
