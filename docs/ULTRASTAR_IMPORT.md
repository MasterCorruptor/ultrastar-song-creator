# UltraStar-import – Fase 2.3

Arbeidsordren av 2026-10-08 gjelder unversionerte filer og UltraStar v1, enkeltstemme først og alle originale headers bevart ved lagring/gjenåpning. Dette er en GUI-uavhengig importer, ingen writer, eksport eller duettimplementasjon.

## API og resultat

UltraStar.SongCreator.UltraStar har kun ProjectReference til Core. UltraStarImporter.Parse(text, sourceFileReference?) parser allerede dekodet Unicode; LoadAsync(path, cancellationToken) leser en lokal fil og lagrer absolutt kildereferanse. ImportResult har nullable Song, immutable Diagnostics og Success. Ved errors er Song null; warnings tillater et komplett redigerbart utkast. Hvert funn har severity, ImportCode og 1-basert linjenummer når en bestemt linje er årsaken. Manglende felt/terminator og globale modellfunn kan ha Line = null.

Vanlige fil-/tilgangsfeil og cancellation følger .NET exceptions. Ingen media, cover eller URL åpnes, kopieres eller lastes ned. Import lager nye stabile Guid-ID-er; prosjektlagring bevarer dem. Import av samme kilde på nytt lager et nytt utkast.

## Støttet profil

- Manglende/tom VERSION betyr unversionert format. Eksplisitt versjon må være 1.minor.patch med desimale heltall; 1.0.0 og 1.1.0 er ekvivalente. Alle v1 minor/patch-verdier tillates, mens ukjente headers beholdes uten funksjonstolkning. V2-draft og andre major-versjoner avvises.
- TITLE, ARTIST, BPM og MP3 kreves med ikke-tom verdi etter kjernespesifikasjonen, også når AUDIO overstyrer MP3. Headernavn sammenlignes uten hensyn til case; ytre whitespace i navn/verdi fjernes som tillatt i formatet. Original casing, verdier, tomme headers, ukjente headers, duplikater og rekkefølge lagres i ImportedSource.
- To ikke-tomme operative headers med samme navn avvises som tvetydige. Ukjente duplikater bevares. Dette er en eksplisitt importpolicy; spesifikasjonen definerer ingen konfliktprioritet for dupliserte operative headers.
- AUDIO overstyrer MP3; VIDEO og LANGUAGE mappes når oppgitt. Album/cover/background/creator, medley, start/end, URLs og øvrige headers beholdes i originalsamlingen. START/END valideres og gir warning om at playback-range ennå ikke brukes.
- De fem notetypene :, *, F, R og G støttes. Ukjent notetype avvises uten erstatning. Bare implisitt eller eksplisitt P1 støttes; v1 eksplisitt P1 krever header #P1. Andre stemmer og ikke-tomme duettheaders avvises uten å flate ut stemmer.
- Tomme linjer og CR/LF/CRLF/EOF støttes. UTF-8 BOM tolereres. Note-tekst bevarer innledende/avsluttende spaces; nøyaktig én delimiter etter pitch fjernes. Flere separatorer mellom tall tolereres. V1 støtter Unicode-whitespace som separator; legacy space/tab. Headerkanter trimmes med Unicode-whitespace som interoperabilitetspolicy.
- Tom note-tekst tolereres som redigerbart utkast med samme warnings som domenet (freestyle trenger ikke tekst). Kontrolltegn i note-tekst avvises. Dette er en dokumentert toleranse utover grammatikken som angir minst ett teksttegn.
- E avslutter body; trailer ignoreres. Filleseren kutter trailer ved ASCII E-linjen før dekoding, slik at ugyldige trailerbytes ikke skader sangen. Manglende E gir warning og siste frase beholdes.
- Standardgrense er 8 MiB, justerbar i konstruktøren. Filens totale byteantall kontrolleres før dekoding, også trailer. Dekodet Parse-tekst må også passe UTF-8-bytebudsjettet. Dette er ikke et allokeringsbudsjett.

## Tid, pitch og fraser

Sekunder per kildebeat er 60 / (4 × #BPM). Note.startSeconds = start-beat × sekunderPerBeat; varighet = duration × sekunderPerBeat. Intern sangtid null er kildebeat 0. GAP er millisekunder i lyd: AudioOffsetSeconds = GAP / 1000. VIDEOGAP er signert forsinkelse av video relativt til lyd: VideoOffsetSeconds = AudioOffsetSeconds − VIDEOGAP. Medietid følger domenets sangtid + offset.

BPM/GAP må være representerbare ikke-negative desimaler (BPM positiv); komma/punktum støttes uavhengig av prosesskultur. VIDEOGAP kan være negativ. START bruker sekunder og END millisekunder; de endrer ikke importerte notetider. NaN/Infinity/eksponentnotasjon og negative note-beats avvises. Start og varighet må kunne summeres innen 2^53−1 beats før konvertering; varighet må være positiv. Overflow, underflow eller tap av positiv varighet ved double-konvertering gir feil.

Normal/golden pitch er halvtoner relativt C4: MIDI = kildepitch + 60. MIDI-grensene 0–127 håndheves uten clamp/transponering. F/R/G bærer ingen pitchsemantikk; intern placeholder er 60. Ikke-null kildepitch for disse gir warning. Confidence er null (ukjent), og ingen analyseartefakter oppdiktes.

#BPM er en kvantiseringsbase som kan avvike fra musikalsk tempo. Song.BeatsPerMinute settes derfor ikke fra den; den opprinnelige BPM-headeren bevares. Import garanterer tidskonvertering, ikke tempoanalyse.

Frasemarkører grupperer note-tekst uten å flytte noter. Domenets frasegrenser er min(start) og max(end) for notene i gruppen. Tomme/gjentatte markører ignoreres med warning, og markører før siste noteend gir warning. Kilderekkefølgen beholdes også ved usorterte eller overlappende noter; relevante warnings returneres med linjer.

## Kildemetadata og prosjekt

ImportedSource har formatId ultrastar-unversioned/ultrastar-v1, eventuell kildereferanse og alle headers. [Prosjektformat v2](PROJECT_FORMAT_V2.md) lagrer samlingen; v1-prosjekter migreres eksplisitt i minnet med null importkilde. Relative importerte referanser gjelder original sangfils mappe når kildereferansen finnes. Reopen bruker lagret domenetilstand og headers; den leser ikke original sangfil og innhenter ikke metadata.

Kildemetadata er proveniens, mens redigerte domenefelt/noter er aktuell sannhet. Importen bevarer ikke original byte-encoding, whitespace rundt headers eller rå body/frasemarkør-cues som en tapsfri teksteditor. Den lover ingen identisk fremtidig eksport; writerens regler er en egen arbeidsordre.

## Åpent avklaringspunkt

Legacy #RELATIVE:YES avvises foreløpig tydelig med UnsupportedRelativeTiming. Primærkilden initialiserer relativ offset fra GAP, men blander denne med beatverdier selv om GAP er dokumentert i millisekunder. Prosjekteieren er spurt om relativ modus skal avvises i første importer eller avklares/implementeres nå. Den endelige avgrensningen er ikke vedtatt; dette punktet må lukkes før leveransen anses ferdig. En markør med ekstra relativ-offset uten deklarert relativ modus gir feil; modus gjettes aldri.

## Primærkilder og lisens

Referanse: UltraStar-Deluxe/format ved commit 7328e4df4ad9b88cb7120d4c9d8177a87945a57c, hentet 2026-10-08. Unversionert spesifikasjon og publisert v1 er brukt; v2 er upublisert draft og utenfor scope. Egen parser er skrevet uten upstream parserkode eller sangmateriale. Spesifikasjonsrepositoryets lisens er MIT. Ingen ny NuGet-pakke introduseres; CodePagesEncodingProvider kommer fra .NET-runtime.

- [Unversionert format](https://github.com/UltraStar-Deluxe/format/blob/7328e4df4ad9b88cb7120d4c9d8177a87945a57c/The%20UltraStar%20File%20Format%20%28Unversioned%29.md), SHA-256 d213193d2ac9d9933f22e6ef0ca82f11134bff6cafbcef11f25c78f94bef09d6.
- [Publisert v1](https://github.com/UltraStar-Deluxe/format/blob/7328e4df4ad9b88cb7120d4c9d8177a87945a57c/The%20UltraStar%20File%20Format%20%28v1%29.md), SHA-256 adcde54fbbc76c3ee4173d74972663896bca2b5da741584fe43338ab2fcab022.
- [MIT-lisens](https://github.com/UltraStar-Deluxe/format/blob/7328e4df4ad9b88cb7120d4c9d8177a87945a57c/LICENSE), SHA-256 2ebf803c706073ebf32ecaf9dfa0a237286ea60e2bafccc5f43f49287c46ee03.

## Verifikasjon

Håndskrevne syntetiske fixtures dekker begge profiler, alle notetyper, fractional comma-BPM, GAP/videooffset, Unicode og AUDIO-preferanse. Testene dekker feil/diagnostikk, encoding, BOM/linjeslutt, versjoner/stemmer, numeriske grenser, usorterte/overlappende noter og import → redigering → undo/redo → Save v2 → reopen med identiske metadata/referanser. Ingen ekte sangtekst eller lyd er brukt.
