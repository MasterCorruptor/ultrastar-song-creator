# Fase 1 – teknologianbefaling til prosjekteier

Dato: 2026-10-07. **Forslag til beslutning; ingen produksjonsstack er vedtatt. Fase 2 er ikke startet.**

## Anbefaling

Velg **C#/.NET 10 LTS + Avalonia**, en egen timeline-kontroll og **miniaudio bak et utskiftbart playback-grensesnitt**. Kjør analyse i en separat, versjonert Python-prosess; bruk whisper.cpp som native ASR-sidecar og CTranslate2 direkte for kjent-tekst-alignment. Distribuer egen .NET-runtime og Python-runtime; brukeren skal ikke installere utviklingsverktøy eller opprette en konto.

Dette støttes nå av native Windows-avspilling, timeline med ekte waveform og manuelt annoterte noter, en portable Windows-prøve og faktisk Linux-kjøring. Fordelen er et konsistent desktoplag og direkte tilgang til analysebiblioteker. Kostnaden er to språk, to runtimeflater, større pakke (lokal ukomprimert researchpakke ca. 1405 MiB, ikke en optimalisert installer) og arbeid med prosesskommunikasjon, cancellation og tredjepartsartefakter.

Behold egen sangmodell, redigering og undo uavhengig av analyse og UltraStar-format. Bruk lydmotorens sample-posisjon som tidskilde på Windows; GUI-timeren oppdaterer bare visningen. Linux-prøvens manuelle PCM-pumping er en testmetode, ikke anbefalt produktarkitektur.

## Foreslåtte komponentvalg

| Område | Anbefalt retning | Viktig kompromiss |
|---|---|---|
| Desktop | C#/.NET 10 LTS + Avalonia; testversjon 12.1.3 | Custom timeline er gjennomførbar, men input, tilgjengelighet og ferdig editor må utvikles og testes senere. |
| Native lyd | miniaudio 0.11.25; Hexa.NET.MiniAudio 1.0.1 som første binding | Samme native PCM-kjerne virket på Windows/Linux. Binding og native minne krever tydelig eierskap; fysisk Linux-lydenhet er uprøvd. |
| Analyse | Separat Python 3.12-prosess, CPU som obligatorisk baseline | Lett og tung analyse kan få separate dependencyprofiler. Ingen produksjons-IPC er definert av PoC-en. |
| Pitch | SwiftF0 0.3.0, pYIN som kontroll/fallback | SwiftF0 var raskere og mer treffsikker i dette solovokalkorpuset. Ingen universell kvalitetsrangering, duettstøtte eller validering på alle separerte vokaler. |
| Notesegmentering | SwiftF0s grunnsegmentering + eget senere alignment-/stavelseslag | Note-F1 rundt 0,76 viser behov for korreksjon. Pitchgrenser er ikke stavelsesgrenser. |
| Vokalseparasjon | Open-Unmix 1.3.0 med **umxhq 1.0.1**, eksplisitt modell-ID | Både kode og disse vektene har MIT-grunnlag. Tyngre PyTorch-pakke; må ikke falle tilbake til noncommercial umxl automatisk. Kvalitet på fulle miksinger er ikke rangert mot moderne Demucs/Roformer. |
| ASR | whisper.cpp 1.9.5 som native CPU-sidecar, WAV fra egen codec | MIT-kode/Whisper-vekter; bygget og prøvd på Windows/Linux. Generisk CPU-build tok ca. 6–9 s per 29 s miksing. Tiny hadde 20–44% WER her; modellen er en probe, ikke endelig kvalitetsvalg. Stock PyAV-bundle avvises. |
| Alignment | Direkte CTranslate2-API på PCM + to uendrede MIT-hjelpere; utskiftbar kjent-tekst-adapter | 136 ord matchet; median ca. 0,12–0,15 s, P95 opptil 0,98 s. Ingen PyAV-dependency. Full-song-/stavelsesadapter og manuell korreksjon gjenstår i et senere produkt. |
| BPM/beat | librosa som første baseline, med manuell tempo-/fasebekreftelse | Ekte miksinger er kjørt, men har ikke beat-ground-truth. Halv-/dobbelt tempo og rubato må fortsatt håndteres. |
| Media/codec | Egen minimal, pinnet FFmpeg-build som separat prosess; PCM til playback/analyse | Færre transitive codecs og konkret kildepakke. Avgrenset formatstøtte; flere codecs krever ny vurdering. |
| Acquisition/tekst | Tidligere audit: yt-dlp-wheel, MusicBrainz, AMLL/LRCLIB og manuell fallback | Kildematching og faktisk tilgjengelighet varierer. Providerlisens klarerer ikke selve sangteksten eller coveret. |

## Hva kontrollpunktene faktisk viste

| Port | Evidens | Praktisk avgrensning |
|---|---|---|
| Native playback, seek og loop | Windows-lydenhet: pause/resume, seek og seks loop-runder. Separat PCM-prøve kontrollerte seek-cursor og at loop holdt seg innenfor valgte samples. | Ingen akustisk loopback, hardwarelatency eller device-switch-test. WAV ved 48 kHz; komprimert media dekodes først. |
| Timeline/playback-integrasjon | Ekte waveform, ekte annoterte noter, selvlagde ordplassholdere, playhead fra native cursor og zoom under avspilling; Python analyserte samtidig og ble avbrutt. | Automatisert prøve i native vindu, ingen ferdig editor eller subjektiv brukertest. Ikke en generell 60 FPS-garanti. |
| Ekte sangbenchmark | 40 solovokaler / ca. 13,6 minutter, 17 engelske ASR-prøver, tre annoterte miksinger for alignment/ASR, samt tre komplette sanger for CPU-separation. | Små, eksplisitte datasett. Ingen norsk sang, duetter, kilde-stem-SDR på miksingene eller GPU-kvalitetsbenchmark. |
| Modell-/codec-/binærlisenser | Artefaktinventar, modellhashes, dokumenterte avvisninger og en minimal FFmpeg-kilde/build-rute. Se lisensrapporten. | Grunnlag for teknologivalg, ikke en godkjenning av en fremtidig full installasjonspakke. |
| Windows-distribusjon | Self-contained .NET-app + CPython embeddable + SwiftF0/ONNX Runtime og Open-Unmix/PyTorch, whisper.cpp og direkte CTranslate2 kjørte uten installert Python/.NET i PATH. | Samme Windows-vert; ikke ren VM, installer, signering eller oppdateringssystem. Alle acquisitionskomponenter er ikke inkludert i minimumpakken; den avviste PyAV-bundlen er fjernet. |
| Linux smoke | Samme C#-kilde, Linux-native miniaudio, Avalonia under Xvfb, separat Python-worker og Open-Unmix, whisper.cpp og direkte CTranslate2 med nettverket deaktivert. | Linux-container; ikke fysisk desktop, Wayland, PulseAudio/PipeWire/ALSA-driver eller komplett Linux-distribusjon. |

Detaljer og målte tall finnes i [resultatene](PHASE1_CONTINUATION_RESULTS.md). Begrensningene er ikke skjult bak «passed». En smoke-test bekrefter at denne arkitekturen kan kjøres; den er ikke en releaseakseptanse.

## Alternativer

| Alternativ | Når det ville være et bedre valg | Hvorfor det ikke anbefales først nå |
|---|---|---|
| Python/PySide6 | Prosjekteieren prioriterer ett språk og tett analyseintegrasjon høyere enn eget desktoplag. | Krever fortsatt workerisolasjon, konkret Qt-modul-/LGPL-vurdering og distribusjonsarbeid. Ikke praktisk sammenlignet med denne PoC-en. |
| Tauri | Et webbasert editor-UI er et eksplisitt produktønske. | Flere språk og forskjellige OS-webviews; lyd og Python-sidecar må fortsatt løses. |
| Electron | Ensartet Chromium er viktigere enn pakkestørrelse og runtimeflate. | Større grunnpakke og ekstra native audio-integrasjon; ingen sammenlignende ytelsestest er gjort. |

Anbefalingen bygger på krav og utført verifikasjon. Den påstår ikke at de uprøvde alternativene er tregere.

## Beslutninger som kreves av prosjekteier

1. **Hovedstack:** Godkjenne C#/.NET 10 LTS + Avalonia + separat Python-prosess, eller velge et av alternativene. Godkjenning gjelder arkitektur; evalueringspatcher skal ikke automatisk bli evige produksjonsversjoner.
2. **Playback:** Godkjenne miniaudio som første backend, med Hexa-binding som utgangspunkt og et utskiftbart grensesnitt. Akseptere at fysisk Linux-audio og akustisk latency må verifiseres før Linux-release.
3. **Lisensprofil:** Beholde MIT for egen kode og tillate separat LGPL-codec med source/notices/utskiftbarhet, samt dokumenterte compiler-runtime-exceptions. Alternativt kreve en strengere pakkeprofil og ta funksjons-/byggekostnaden. Ingen godkjenning kan gjøre uklare modellrettigheter gyldige.
4. **Separasjonsbaseline:** Velge MIT-lisensiert umxhq som konservativ baseline. Demucs/Roformer holdes utenfor standardpakken. Spleeter er en målt referanse med gammel Windows-dependencyflate, ikke foreslått standard.
5. **Automatikk og kvalitet:** Godta at pitch, noter, alignment og BPM gir forslag med confidence og manuell korreksjon. Beholde modellvalg utskiftbart, og kreve større ASR-/norsk-/miksingsbenchmark før et kvalitetsløfte eller default ASR-modell bestemmes.
6. **Distribusjon og ressursbruk:** Godta medfølgende runtimes og at PyTorch gjør pakken større; modeller kan hentes til en verifisert lokal cache uten konto. CUDA/GPU er et valgfritt senere tillegg, ikke et krav for CPU-ruten.
7. **Faseovergang:** Avgjøre om den dokumenterte evidensen og avgrensningene er tilstrekkelige til å lukke Fase 1. Eventuelle utvidede Fase 1-tester må bestilles konkret. **Fase 2 krever eksplisitt godkjenning og egen arbeidsordre.**

ADR-0001 og ADR-0002 står fortsatt som **foreslått**. Arbeidsplanen blir stående i active/ til teknologivalg og faseavslutning er besluttet. Verken publisering av denne researchen eller merge av en dokumentasjons-PR er i seg selv slik godkjenning.
