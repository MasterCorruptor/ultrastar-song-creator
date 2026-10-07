# Fase 1 – måleresultater og avgrensninger

Utført 2026-10-07. Dette er feasibility-prøver på Windows med syntetiske data. Resultatene er ikke et kvalitetsbenchmark på ekte sang, en produktakseptanse eller et vedtak om produksjonsstack.

## Miljø

- Windows-evaluering. Maskinvareprofil, lokal OS-build og driverversjoner beholdes bare i lokal arbeidstilstand etter prosjekteierens publiseringsvalg. GPU-inferens er **ikke** kjørt.
- Python 3.12.10 i prosjektlokal venv. Valg av denne versjonen er for evaluering, ikke en besluttet produksjonsruntime.
- librosa 1.0.0, SwiftF0 0.3.0, NumPy 2.5.3, SciPy 1.18.1, Numba 0.68.0 og ONNX Runtime 1.30.0.
- Avalonia 12.1.3, prosjektlokalt .NET SDK 10.0.401 / runtime 10.0.12. SDK-arkivet ble kontrollert mot SHA512 fra Microsofts release-metadata; ingen global SDK-installasjon.
- Installert FFmpeg `N-121938-g2456a39581-20251130`; GPL og version3 aktivert, nonfree ikke aktivert. Bare brukt til evaluering, ikke kopiert til release.

Publiserte tider gjelder én lokal evaluering; uten offentlig maskinvareprofil kan de ikke brukes som reproduserbar hardware-sammenligning. De dokumenterer feasibility og relative observasjoner innenfor samme kjøring.

Eksakte Python- og NuGet-testversjoner er låst under [tools/phase1](../tools/phase1/README.md). Ingen Linux-, ren-maskin-, top-memory- eller samlet sangpipeline-test er utført.

## Lokal mediahåndtering og formatregning

En selvlaget 6-sekunders stereo PCM-fil ved 44,1 kHz ble kodet til MP3, deretter dekodet og normalisert til mono PCM ved 16 kHz. Resultatet hadde **96 000 samples, én kanal og 6,000 sekunders varighet**. Encode/decode/resampling fungerte lokalt. Kort fil og varm prosesscache gjør tidsbruk lite representativ for store medier.

Formatregningen ble kontrollert mot [offisiell UltraStar v1-spesifikasjon](https://github.com/UltraStar-Deluxe/format/blob/7328e4df4ad9b88cb7120d4c9d8177a87945a57c/The%20UltraStar%20File%20Format%20%28v1%29.md): file-ticks bruker firedoblet `#BPM`, `#GAP` måles i millisekunder, og note-pitch er halvtoner relativt til C4. A4/MIDI 69 gir derfor UltraStar-pitch 9. Dette er en regnekontroll, **ikke** en implementert parser, exporter eller kompatibilitetstest.

[Maskinlesbar media-/analyse-evidens](../tools/phase1/results/cpu-media-analysis.json).

## CPU-pitch og grunnsegmentering

To 5-sekunders mono-fixtures ved 16 kHz, tre tonespenn hver: 220, 329,63 og 440 Hz. Den ene har harmoniske overtoner; den andre i tillegg vibrato på ±0,3 halvtone ved 5 Hz. Begge har pauser. Pitchfeil beregnes mot syntetisk kjent F0 og med 120 ms margin fra notegrensene. Voicingdekning måles på de indre tonespennene; silence false positives måles med tilsvarende margin.

pYIN: `fmin=65`, `fmax=1047`, frame 2048, hop 256. SwiftF0: én CPU-tråd, `spin=False`, default confidencegrense 0,5 og grunnsegmentering. Begge får samme waveform. Etter ett første kall måles to gjentakelser per fixture. Swift-tiden inkluderer `segment_notes`; pYIN-tiden inkluderer ikke tilsvarende notegruppering.

| Detector | Fixture | Median varm tid, 5 s lyd | Median / P95 absolutt pitchfeil | Voicingdekning | Falskt voiced i pauser |
|---|---|---|---|---|---|
| pyin | harmonic | 100.31 ms | 0.79 / 0.79 cent | 100% | 0% |
| swift-f0 | harmonic | 27.98 ms | 5.35 / 7.19 cent | 100% | 0% |
| pyin | vibrato | 101.04 ms | 10.26 / 18.68 cent | 100% | 0% |
| swift-f0 | vibrato | 28.22 ms | 4.95 / 15.68 cent | 100% | 0% |

SwiftF0 foreslo tre noter i begge fixtures. Dette sier ikke at stavelsesgrenser eller fraser kan utledes fra pitch alene. Sangtekst/alignment er ikke koblet inn.

Det første pYIN-kallet i denne prosessen tok 5,58 s; SwiftF0s første detect-kall tok ca. 27,83 ms og modelloppsett ca. 42,63 ms. «Første kall» er ikke lik en ny maskin: disk-/JIT-cacher kan finnes fra tidligere evaluering. En tidligere pilot med librosa 0.11.0 ble erstattet med en kontroll mot dagens stabile 1.0.0; tabellen viser 1.0.0-resultatene.

Tolkning: begge er brukbare CPU-kandidater på disse enkle signalene. SwiftF0 var raskere i denne prøven; pYIN hadde mindre pitchfeil på signalet uten vibrato. Ingen generell rangering på vokalkvalitet, duetter, glissando, backing vocals eller separasjonsartefakter følger av dette.

## BPM/beat

20-sekunders syntetiske klikkspor ved 90 og 120 BPM, 16 kHz, hop 256, `trim=False`. Det ble kjørt to kall per tempo.

| Forventet BPM | Estimert BPM | Relativ tempofeil | Median avstand til nærmeste klikk |
|---|---|---|---|
| 90 | 89.286 | 0.794% | 22.67 ms |
| 120 | 120.968 | 0.806% | 18.00 ms |

Dette dokumenterer installasjon, output og frame-grid-feil på enkle klikk. Nærmeste-klikk-avstand er ikke full beat precision/recall. Ekstrapolerte beats, musikalsk fase, halv-/dobbelt-tempo og variabelt tempo må testes separat. Første beat-kall kan inkludere init/JIT; repeterte tider og beatantall finnes i JSON.

## Anonyme provider-kall

Ett read-only-kall per provider med prosjektidentifiserende User-Agent. Ingen credentials, cookies eller personlig API-nøkkel ble sendt. Ingen sangtekst ble lagret i evidensfilen.

| Provider | Målt resultat | Hva resultatet betyr |
|---|---|---|
| LRCLIB | HTTP 200, 20 kandidater; alle hadde plain/synced felt med innhold | Artist/tittel-søk fungerer anonymt ved denne kontrollen. API-et returnerte også `hasWordSync` og `lyricsfile`; faktisk word-level-innhold ble ikke evaluert. |
| MusicBrainz | HTTP 200, 73 recording-treff, tre returnert med satt limit | Anonym recording-søk er mulig; matching til ønsket innspilling gjenstår. |
| lyrics.ovh | Timeout, målt ca. 30 s | Denne ene forespørselen feilet. Det er ikke bevis på at tjenesten alltid er nede; den bør være best effort og ha fallback. |

Socket-timeout var satt til 15 s, men observert total tid var ca. 30 s; socket-timeout er ikke en hard frist for hele requesten. En produksjonsadapter må ha samlet tidsbudsjett, cancellation, begrenset retry og provider-ratekontroll. Ett kall måler ikke kapasitet, dekning, SLA eller fremtidig kontofri tilgang.

[Provider-evidens](../tools/phase1/results/provider-probes.json).

## Lokal ASR uten nett under inferens

En engelsk, syntetisk SAPI-stemme leste en egen 17-ords testtekst til en lokal WAV-fil. Lyden ble ikke spilt av eller publisert. `faster-whisper 1.2.1` / `CTranslate2 4.8.2` kjørte modellen `Systran/faster-whisper-tiny` på CPU int8 med fire tråder. Den nøyaktige modellrevisjonen er `d90ca5fe260221311c53c58e660288d3deb8d356`, model.bin-SHA256 er registrert i evidensfilen. Modellkortet angir MIT, uten gated tilgang.

Første modellnedlasting var anonym. Den verifiserte kjøringen lastet kun fra lokal modellcache, med `local_files_only=True`, offline Hub-innstilling og Python-socketforbindelser avvist under modellast og inferens. Dette er ikke OS-nivå nettisolasjon.

- Lydvarighet: 6.933 s.
- Modellast: 0.160 s.
- Full inferens, inkludert iterasjon over lazy segments: 0.271 s.
- Output: 17 ord med timestamps innenfor lydens varighet; syntetisk speech WER 0%.

Testen viser CPU-/lokalcache-integrasjon, ikke gjenkjenning eller word-boundary-nøyaktighet på sang. Ingen forced alignment av innhentet tekst, norsk sang, GPU eller stor modell ble testet.

Den installerte PyAV 19.0.1-wheelen rapporterte **LGPL-3.0-or-later for sine FFmpeg-biblioteker**, uten GPL eller nonfree-flagg. Bindingens BSD-lisens dekker derfor ikke hele codec-bundlen. Opplysningene ble lest fra wheelens native metadata; andre PyAV-versjoner/bygg trenger egen kontroll.

[ASR-evidens og native lisensmetadata](../tools/phase1/results/asr-offline.json).

## Avalonia / timeline

En separat `net10.0`-prøve med Avalonia.Headless/Skia 12.1.3 tegnet 10 000 deterministisk genererte noter fordelt over 300 s. Rendereren tegnet bare noter som overlapper viewport. Det ble fanget faktiske 1200×600-pixelbilder, kontrollert ulike pixelfingeravtrykk ved zoom/scroll og simulert musehjul som endret zoom. Ett bilde er også visuelt kontrollert for noter, grid og playhead.

Det ble målt 30 offscreen-frames per viewport etter en warmup-frame. Timing inkluderer invalidering og frame capture; bildefilskriving/hash er utenfor målt tid.

| Offset / viewport | Synlige av 10 000 noter | Median frame | P95 frame |
|---|---|---|---|
| 0 s / 8 s | 267 | 2.938 ms | 4.418 ms |
| 100 s / 8 s | 274 | 2.814 ms | 3.303 ms |
| 100 s / 20 s | 674 | 3.277 ms | 4.729 ms |

Tolkning: custom viewport-rendering er gjennomførbar på testmaskinen. Headless erstatter native vindu/compositor og er **ikke en 60 FPS-garanti**. Noteredigering, waveform, sangtekstlayout, audio device, sampleclock, seek/loop, workerbelastning og Linux er ikke testet her.

[Timeline-evidens](../tools/phase1/results/avalonia-headless.json). Lokal generert PNG og alle binaries ligger i ignorert `.agent-local/` eller ignorerte buildmapper.

## Utførte kontroller

- Media-output og formatregning bestod.
- Fire pitch/fixture-kombinasjoner bestod feasibility-grensen: minst 80% voiced coverage og under 50 cent medianfeil i indre syntetiske tonespenn.
- Begge syntetiske beatestimater hadde mindre enn 5% tempofeil.
- Offline ASR produserte tekst og ordtider; tale-WER ble målt, ikke brukt som bevis på sangkvalitet.
- Timeline-prøven bestod pixel capture, culling, forskjellige zoom/scroll-bilder og simulert wheel-input. Bygg etter API-retting var uten warnings/errors.
- `pip check` meldte ingen ødelagte dependencykrav; NuGet locked restore bestod. Dette erstatter ikke vurdering av native-/modellvilkår.

Feasibility-grensene er valgt for disse prøvene og er ikke prosjektets produksjonskvalitetskrav. Videre sangbenchmark og native brukertesting er beskrevet i [auditten](PHASE1_AUDIT.md).
