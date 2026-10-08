# UltraStar-eksport og sangpakke – Fase 2.4

Prosjekteieren valgte unversionert + v1 med absolutt tid, automatisk avrunding med rapport og komplett sangmappe med kopierte medier. Writer/pakker ligger i UltraStar-laget; Core og prosjektformat v1/v2 er uendret.

## API og flyt

UltraStarExporter.Render(song, options) lager tekst/diagnostikk uten fil-I/O. PackageAsync(destinationDirectory, song, options, progress?, cancellationToken) produserer en ny mappe med song.txt og media/. ExportOptions krever ExportFormat.Unversioned eller V1; GridBpm og ReferenceDirectory er valgfrie når en entydig importkilde gir disse.

ExportResult har nullable Text, immutable Diagnostics og QuantizedNote-rapport. Success betyr gyldig tekst; et vellykket PackageAsync-resultat har dessuten DirectoryPath og Assets. Ingen filmappe publiseres ved formaterrors. I/O-/tilgangsfeil og cancellation bruker .NET exceptions. Progress rapporterer relativ assetreferanse, kopierte bytes og filens bytestørrelse; exceptions fra en callback behandles som en avbrutt pakkeoperasjon.

Eksempel: PackageAsync(targetDirectory, song, new ExportOptions(ExportFormat.V1, GridBpm: 120, ReferenceDirectory: sourceDirectory), cancellationToken: ct). Målmappens parent må finnes; målmappe/fil må ikke finnes fra før. Alle test-/smoke-filer ligger under prosjektets ignorerte .agent-local/.

## Tidsrutenett og avviksrapport

GridBpm er UltraStar-filens kvantiseringsbase. Uten eksplisitt verdi brukes én ikke-tom, gyldig original BPM-header. En sang uten slik kilde trenger eksplisitt GridBpm; Song.BeatsPerMinute er musikalsk tempo og brukes ikke som en gjetning.

SekunderPerBeat = 60 / (4 × GridBpm). Start og slutt avrundes hver for seg til nærmeste beat, midtpunkter bort fra null. Varighet blir endBeat − startBeat. Dette bevarer felles grenser mellom nabonoter. Hvis slutten ikke blir etter starten, stoppes eksporten; noten slettes/forlenges ikke. Beats må være innen 0..2^53−1, og konverterte intervaller må være positive og finite.

Rapporten har én oppføring per note med original NoteId, start/end-beats, originale og rekonstruerte start/slutt/varighet i sekunder og eksakte deltas. Rekonstruksjonen følger importens start + varighet. TimingRounded-warning gis for endring utover begrenset floating-point-støy (maks åtte ULP, begrenset til 1e−7 av et beat). Også små numeriske forskjeller finnes i den detaljerte rapporten. Den opprinnelige sangen/notene/analysen endres ikke.

GAP er AudioOffsetSeconds × 1000; negativ eller ikke-representerbar GAP avvises. Med video brukes VIDEOGAP = AudioOffsetSeconds − VideoOffsetSeconds. Inaktiv video-offset uten video forblir prosjektdata. Tall skrives kultur-uavhengig som desimaler uten eksponentnotasjon; negativ null normaliseres til 0.

## Tekst, fraser og metadata

UTF-8 uten BOM, LF og E-terminator. V1 får VERSION:1.0.0 først; unversionert har ingen VERSION. Begge profiler bruker absolutte beats. Legacy relativ import er allerede konvertert i domenet; v1 utelater RELATIVE/ENCODING, mens eksisterende legacy-headere normaliseres til NO/UTF-8. Normal/golden skriver MIDI − 60; F/R/G skriver pitch 0 og beholder type/tekst.

Note-/fraserekkefølge og tekstmellomrom beholdes. Frasemarkør mellom grupper er største eksporterte noteend i gruppen; siste gruppe ender med E. Wider domain-phrase bounds og originale display-cues er ikke representert som tapsfri råtekst. PhraseBoundsDerived-warning angir når gjenimport får grenser fra notene. Ingen automatisk omsortering eller overlappreparasjon.

Gjeldende Title/Artist/Language, valgt grid, medier og offsets erstatter originale operative headers. Tom/ugyldig required metadata, tom notetekst/kontrolltegn/ugyldig Unicode, tomme fraser og tvetydige main media gir error. Unknown/øvrige source headers, casing, tomme verdier, duplikater og rekkefølge beholdes. HeaderRegenerated rapporterer operative profil-/modellendringer. Duettheaders avvises.

MEDLEYSTARTBEAT/END reskaleres fra entydig absolutt kildegrid ved gridskifte; eventuell avrunding gir warning. Relative/ugyldige medley-cues avvises uten gjetting. START/END/PREVIEWSTART må være finite ikke-negative desimaler; de beholdes som kildeangitte tider og utgjør ikke et implementert playback-range-UI.

Guid-ID-er, confidence, rå analyse og undo er prosjektdata. Eksporten muterer ikke disse og varsler når analyse/confidence ikke kan lagres i tekstformatet. UltraStar-gjenimport oppretter nye ID-er; det er semantisk rundtur, ikke prosjektfil-rundtur. Teksten valideres av importeren før den kan returneres/publiseres, i tillegg til egne writer-regler og uavhengige forventningsfixtures.

## Kopierte medier og referansebase

Pakking krever én main audio og høyst én video. Aktuelle audio/video mappes; originale COVER/BACKGROUND/VOCALS/INSTRUMENTAL pakkes når oppgitt. MP3 og eventuell AUDIO peker på samme aktuelle kopierte audio, selv om originalheaderen navnga en tidligere fil. Dublette ikke-tomme assetheaders avvises som tvetydige. Album, creator, URLs og andre metadata bevares; nettressurser lastes ikke ned.

Absolutte lokale paths kan brukes som kilde. Relative paths bruker eksplisitt absolutt ReferenceDirectory, ellers mappe fra absolutt ImportedSource.FileReference. En relativ/ukjent kilde uten eksplisitt base gir error; prosessens cwd brukes ikke. Original .txt trenger ikke finnes. Windows-backslash i relative referanser støttes også på Linux. Fil-URL og andre URL-referanser avvises som aktive assetkilder.

Output bruker relative media/-referanser. Filnavn avledes deterministisk fra basename med portable ASCII-tegn; andre tegn erstattes, reserved Windows-navn får prefiks og case-insensitive kollisjoner får nummersuffiks. To referanser til samme normaliserte lokale path deler kopi (Windows path-sammenligning uten case, Linux med case). Dette er ikke deduplisering av hardlinks/symlinks eller ulike filer med likt innhold.

Manglende oppgitt fil stopper pakken. Kopi utføres med bounded streaming, kildehash og separat verifikasjon av målfil. Lengdeendringer under kopiering avvises. Assets rapporterer original lokal path, relativ outputpath, bytelengde og SHA-256. Filer transcodes ikke og har samme bytes som kopiert snapshot. Pakkens relative referanser virker etter flytting og uten originalkildene.

## Ferdigstillelse og feil

Forhåndsvalidering og lokal filkartlegging skjer før staging. Medier og song.txt skrives/flusjes til unik søstermappe under samme parent; Directory.Move publiserer mappen uten overwrite. Eksisterende target, også en mappe som oppstår mellom preflight og commit, endres ikke. Cancellation kontrolleres før commit; etter commit kan den ikke ugjøre ferdig mappe.

Vanlige pre-commit-feil/cancellation rydder bare den opprettede stagingmappen etter kontroll av absolutt avgrensning og reparse-point. Prosesskrasj/strømbrudd/låste filer kan etterlate staging; dette er ikke en backup-/recovery- eller adversarial-filesystemgaranti. Ingen brukerprosjekt/kilde/parent slettes, og ingen eksisterende pakke oppdateres automatisk.

## Verifikasjon og begrensning

263/263 tester bestod på Windows og nettisolert Linux med SDK 10.0.401: 203 tidligere + 38 writer-/rundturtester + 22 pakketester. Locked restore, Release build uten warnings og formatkontroll bestod. Testscratch/artifacts er ignorert, og runtime-/testdependencysettet er uendret.

En lokal Windows-smoke brukte gyldig, syntetisk PCM WAV, PNG-cover og MPEG4-video; pakken kunne reimporteres, alle kopier samsvarte i hash og FFmpeg-dekoding bestod. [Resultatrapporten](PHASE2_EXPORT_RESULTS.md) viser bytes/hash og avrunding. FFmpeg var kun et eksisterende privat evalueringsverktøy; eksporteren krever bare .NET-fil-I/O og distribuerer ingen codecbinær.

Dette er lokal struktur-/fil-/tidsverifikasjon. Karaokeprogrammenes UI/scoring/native playback er ikke kjørt med den ferdige pakken. Exporteren decoder ikke brukerens medier eller lover støtte for alle codecs/dialekter. Ingen relativ output, duett eller ny GUI er implementert.

Formatkilder/proveniens og MIDI/beat-regler står i [importprofilen](ULTRASTAR_IMPORT.md) og [relativ avklaring](RELATIVE_TIMING.md); de samme pinnede MIT-spesifikasjonene brukes. Ingen upstream writerkode er kopiert.
