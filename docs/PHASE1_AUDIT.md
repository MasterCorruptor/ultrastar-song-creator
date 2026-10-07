# Fase 1 – Technology Feasibility & Reuse Audit

Dato: 2026-10-07. **Status: første kartlegging og avgrensede feasibility-prøver gjennomført; Fase 1 er fortsatt aktiv.** Produksjonsstacken er ikke valgt. Forslagene nedenfor trenger prosjekteierens vurdering, og de åpne kontrollpunktene må løses før Fase 1 lukkes.

## Konklusjon for neste arbeidstrinn

Prosjektet har en realistisk lokal vei med mye open-source-gjenbruk. Den mest lovende retningen å teste videre er **C#/.NET 10 + Avalonia for desktop/editor og en separat Python-worker for analysen**. SwiftF0 og librosa er prøvd på CPU; faster-whisper er prøvd med en lokal tiny-modell. Acquisition kan bygges rundt utskiftbare yt-dlp-, MusicBrainz- og lyric-adaptere.

Dette er en begrunnet anbefaling for videre Fase 1, ikke en akseptert produksjonsarkitektur. Audio-backend, ekte sangkvalitet, alignment, modellredistribusjon, ferdig Windows-pakke og Linux-smoke-test gjenstår. MIT for egen kode er fortsatt et mulig mål; installasjonspakken vil ha separate tredjepartsvilkår.

## Leveranser

- [Kandidat- og lisensmatrise](DEPENDENCIES.md): acquisition, metadata, lyrics, separation, ASR/alignment, beat/BPM, pitch, segmentering, format, playback, GUI og eksisterende editorer.
- [Måleresultater og avgrensninger](PHASE1_RESULTS.md): faktisk Windows-evaluering, syntetiske data og anonyme provider-kall.
- [Forslag ADR-0001](decisions/ADR-0001-desktop-and-analysis-stack.md): språk, GUI og isolert analyseprosess.
- [Forslag ADR-0002](decisions/ADR-0002-licensing-and-reuse.md): permissivt mål og artefaktvis lisensport.
- [Reproduserbare evalueringsverktøy](../tools/phase1/README.md), låste testversjoner og kilde-/måledata.

Masterspesifikasjon v0.5 er uendret. `src/` inneholder ingen produksjonskode. Evalueringsharnessene gir ingen offentlig API eller ferdig intern sangmodell.

## Anbefalt komponent per funksjon

| Funksjon | Retning | Hva må bekreftes før innføring? |
|---|---|---|
| Konkret innspilling/media | yt-dlp wheel/prosess + eget `MediaSource`-lag | Anonym tilgjengelighet for valgte kilder, ejs/runtime, binærnotices og versjonsvedlikehold |
| Metadata/cover | MusicBrainz core-data; valgfri CAA-adapter | Recording/release-match, ratebegrensning og separat coverrettighet |
| Word-/line-/plain-lyrics | AMLL når ID/versjon matcher; LRCLIB; lyrics.ovh best effort; manuell fallback | Word-level-format/dekning, faktisk innspillingsoffset og providerfeil |
| Dekoding/resampling | FFmpeg som isolert verktøy | Valgt LGPL-build/codecsett og eksplisitt distribusjonsløsning |
| Vokalseparasjon | Demucs 4.1.0-fork som baseline; audio-separator som alternativ | HTDemucs-weightvilkår, CPU/GPU-tid, vokallekkasje og moderne dependencykombinasjon |
| Lokal ASR | faster-whisper CPU int8; whisper.cpp reserve | Sangbenchmark, større modellvalg, PyAV/native vilkår og minne |
| Alignment av korrekt tekst | WhisperX alignment med ungated språkmodell, uten diarization | Singing-benchmark, norsk/språkdekning, egne weightvilkår og fallback når ord ikke alignes |
| BPM/beat/onset | librosa 1.0.0 baseline | Halvt/dobbelt tempo, rubato og musikalsk beatfase på ekte sanger |
| Pitch/referansekurve | SwiftF0 0.3.0 + pYIN-kontroll; torchcrepe/RMVPE alternativer | Vibrato, oktavfeil, stille stemmer, separasjonsartefakter og faktisk vokalconfidence |
| Note-/stavelses-/fraseforslag | Gjenbruk grunnsegmentering; eget domene-/alignmentlag | Samme pitch med flere stavelser, melisma, repetert tekst og ordbokvilkår |
| Desktop/timeline | Avalonia 12.1.3 kandidat + custom drawing-control | Native respons under avspilling, tekstlayout, input, undo/redo og pakking |
| Playback/loop/playhead | miniaudio-kandidat bak grensesnitt | Vedlikeholdbar .NET-binding/native adapter, sampleclock, presis seek/loop og Linux-backend |
| Sangmodell/prosjekt/UltraStar | Eget domene, versjonert lagring og formatadapter etter offisiell spesifikasjon | Egen Fase 2-arbeidsordre; ikke en kopi av en editors interne format |

## GUI-/språksammenligning

| Alternativ | Fordel for dette prosjektet | Kostnad / usikkerhet | Prioritet |
|---|---|---|---|
| C# + Avalonia + Python-worker | Desktopkonvensjoner, permissivt GUI, tydelig skille mellom editor og analysekrasj | To språk, workerpakking og native lyd-binding; headless er ikke native UX | Anbefalt videre PoC |
| Python + PySide6 | Direkte analyseintegrasjon og moden Qt-grafikk | GUI-tråd må beskyttes, Qt-modullisenser og distribusjonsvilkår; GPL-only-moduler må unngås | Reserve hvis enkeltspråk gir klar fordel |
| Tauri + TypeScript + Rust/Python | Webcanvas og isolerte sidecars | Flere runtimeflater, WebView2/WebKitGTK-forskjeller og audioklokke/pakking | Reserve ved tydelig web-UI-preferanse |
| Electron + TypeScript/Python | Ensartet Chromium og webbiblioteker | Større pakke og minne; native fil/audio-integrasjon må prøves | Reserve hvis webviewforskjeller blir avgjørende |
| Unity-basert videreføring av Play | Eksisterende preview/editorreferanse | Proprietær engine og utviklervilkår gir svakere samsvar med utskiftbar lokal open source | Referanse, ikke hovedforslag |

Vurderingen bygger på masterens editorprioritet, lokal behandling, Windows først og senere Linux. Headless Avalonia-prøven viser at custom rendering er teknisk gjennomførbar; den gir ikke bevis for at de andre GUI-ene er tregere. Upstream Avalonia klassifiserer Windows 10 22H2 som Tier 2. Plattformens supportnivå må tas med i en senere native brukertest; lokal maskinvare-/OS-profil publiseres ikke. Se [plattformdokumentasjonen](https://docs.avaloniaui.net/docs/supported-platforms).

## Hva bør utvikles internt senere?

Intern sangmodell, analyseadaptere, versionert prosjektlagring, provider-/innspillingsmatching, kommando-/undo-system, note-/stavelses-/fraseregler, confidence-presentasjon, valideringsregler og editorens spesialiserte timeline/preview bør være prosjektets eget domene. Eksisterende dekodere, kildesøk, inferensmotorer og grunnleggende signalanalyse gjenbrukes.

En tidslinje kontrollert av en audio-sampleclock og et reversibelt kommandosystem må vurderes tidlig. En GUI-timer skal ikke bli autoritativ tidskilde. Analysefeil må ikke ødelegge redigerte noter. Dette er designkrav for senere implementering, ikke et definert IPC-/filformat i denne auditten.

## Tekniske risikoer og kontrollpunkter

| Risiko | Konsekvens | Neste kontroll |
|---|---|---|
| Kode-, weight- og codec-lisenser avviker | En MIT-merket repo kan fortsatt gi uegnet ferdigpakke | Avklar HTDemucs-weights og valgt FFmpeg/PyAV-build; opprett konkret notices-/kilde-/modellmanifest før pakking |
| ASR/CTC er trent på tale | Vokal, kor og melisma kan få feil ord/timing | Sammenlign kjent korrekt tekst mot ekte vokal; mål median/P95 ord-/stavelsesfeil og behold fallback |
| Kun ett monofonisk pitchspor | Duetter/backing vocals, oktavfeil og instrumental-lekkasje | Benchmark pitch mot annotert vokal, separert og uten separation |
| Beat-tempo kan dobles/halveres eller endre seg | Feil eksportgrid til tross for god audio-timing | Variabelt tempo, intro/gap og gridfase; skill sekunder fra UltraStar-ticks |
| Note er ikke lik ord/stavelse | Feil segmentering selv med god F0 | Repetert note, melisma, ordsplit og språkspesifikke ordbøker |
| Provider/API endres eller krever konto | Acquisition svikter for enkelte kilder | Flere providers, timeout/ratebegrensning/cache, manuell tekst og senere local-media adapter |
| CPU/GPU minne og pakkevolum | Ingen GPU skal fortsatt gi en fungerende, om tregere, rute | Mål hel sang på CPU og tilgjengelig GPU; sekvensielle jobber og avbrudd; ikke start CUDA-installasjon som kjernekrav |
| Headless-resultater overføres ikke direkte til native UI | God microbenchmark kan skjule input-/audio-stutter | Synlig native timeline med playback/loop, waveform, tekst og bakgrunnsworker |
| Linux er bare dokumentert | Native bibliotek, font- og audio-backend kan bryte | Én konkret Linux-smoke-test av samme PoC og pakking før hovedstack bekreftes |
| Egen prosess/worker-pakking | Feil versjoner, tunge installs, ødelagt cancel/restart | Test begrenset prosesskontrakt, lokal modellcache, start/stopp/cancel og reproduksjon på ren maskin |

Lisensusikkerhet blokkerer **innføring/distribusjon av den berørte komponenten**, ikke hele kartleggingen. Ingen slike kandidater er installert i produktet. FFmpeg GPL-binary er brukt som allerede installert evalueringsverktøy, ikke kopiert til prosjektets release.

## Neste avgrensede Fase 1-del

1. Vurder ADR-0001-retningen sammen med prosjekteieren; behold status «foreslått» inntil vedtak. Masteren §15.2 legger vesentlige teknologivalg til prosjekteieren.
2. Gjør en native playback/timeline-PoC med loop/play-from-selection, avspillingens tidskilde og respons mens worker kjører. Ingen full editor eller Fase 2-domeneimplementering.
3. Velg et lite, rettighetsavklart sangkorpus med ground truth. Sammenlign separation, F0, alignment og BPM på CPU/GPU. Mål faktisk kvalitet, ressursbruk og avbrudd.
4. Løs modell-/artefaktlisenser og et minimalt Windows-pakke-/Linux-smoke-oppsett for de kandidatene som består.
5. Oppdater anbefalingene fra målinger, innhent nødvendige vedtak og registrer aksepterte ADR-er før Fase 1 avsluttes. Først deretter kan en separat Fase 2-ordre utformes.

Denne auditten lukker ikke åpne poster ved antakelse. Sluttmålet for Fase 1 er dokumenterte, godkjente teknologivalg med tilstrekkelig praktisk og lisensmessig grunnlag.
