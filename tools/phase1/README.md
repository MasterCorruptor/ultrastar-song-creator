# Fase 1-evalueringsverktøy

Dette er avgrensede feasibility-prøver, ikke produktkode eller valgte produksjonsdependencies. [Auditten](../../docs/PHASE1_AUDIT.md) og [resultatene](../../docs/PHASE1_RESULTS.md) forklarer formål og praktiske begrensninger.

Alle kommandoer nedenfor kjøres fra prosjektroten. De første avsnittene beskriver det opprinnelige Windows-kontrollpunktet. Senere native/portable/Linux-tester og sangbenchmark finnes nederst og i docs/PHASE1_CONTINUATION_RESULTS.md. Ren sluttbrukermaskin er ikke verifisert.

## Python / media / pitch / beat

`ffmpeg` og `ffprobe` må være tilgjengelige. Evalueringsverktøyene kopierer ikke disse binaries inn i prosjektet. Lisensrapporten flagger GPL/nonfree på brukt FFmpeg; dette er ingen bundlingsgodkjenning.

```powershell
py -3.12 -m venv .agent-local/phase1/venv
.agent-local/phase1/venv/Scripts/python.exe -m pip install --cache-dir .agent-local/phase1/pip-cache -r tools/phase1/requirements-evaluation.txt
.agent-local/phase1/venv/Scripts/python.exe -m pip check
.agent-local/phase1/venv/Scripts/python.exe tools/phase1/probe.py --analysis
```

`probe.py` uten `--analysis` trenger bare Python-standardbiblioteket og media-verktøyene. Analyse er opt-in. Den genererer egne syntetiske signaler; ingen sanger eller modeller hentes av denne prøven. SwiftF0-modellen følger evalueringspakken.

Provider-prøvene er særskilt opt-in og gjør tre anonyme read-only-kall. De kan gi timeout/HTTP-feil uten at den lokale media-/analyseprøven feiler. Evidensen bevarer schema/status, ikke lyrics.

```powershell
.agent-local/phase1/venv/Scripts/python.exe tools/phase1/probe.py --providers --output .agent-local/phase1/provider-recheck
```

## Lokal ASR-prøve

På Windows lages først en kort, egen engelsk SAPI-talefil uten å spille av lyd. Stemmeversjon kan påvirke reproduksjon. Dette er ikke sang.

```powershell
./tools/phase1/create_speech.ps1
.agent-local/phase1/venv/Scripts/python.exe tools/phase1/asr_probe.py --download-model
.agent-local/phase1/venv/Scripts/python.exe tools/phase1/asr_probe.py
```

Bare første kommando med `--download-model` tillater anonym nedlasting av den eksplisitt pinnede `Systran/faster-whisper-tiny`-revisjonen. Modellen ligger i `.agent-local/phase1/model-cache/`. Den andre kjøringen bruker bare lokal cache. Python-socketconnect avvises under modellast/inferens; dette er ikke OS-level nettisolasjon. PyAVs private native metadata-API brukes kun for inspeksjon av den pinnede evalueringsversjonen.

## Avalonia headless timeline

For Windows-evalueringen finnes en prosjektlokal SDK-nedlaster. Den laster .NET SDK **10.0.401 win-x64** og kontrollerer SHA512 fra Microsofts metadata før utpakking. Ingen global runtime eller PATH endres. Første SDK-/NuGet-henting trenger nett.

```powershell
py -3.12 tools/phase1/prepare_dotnet.py
./tools/phase1/run_timeline.ps1
```

Wrapperen bruker prosjektlokale SDK-/NuGet-/tmp-mapper, kjører locked restore fra `packages.lock.json` og en Release-headless-prøve. PoC-en tester faktisk Skia-pixel capture, viewport-culling, scroll/zoom og simulert wheel-input på 10 000 syntetiske noter. Den åpner ingen synlig native app og spiller ingen lyd.

Dette beviser ikke native playback, brukeropplevelse, tekstlayout, Linux-støtte eller 60 FPS i en ferdig editor. En faktisk brukertest kreves i videre Fase 1.

## Evidens og lokale artefakter

- `upstream-snapshot.json`: eksakte upstream-commits og primærkilder, ingen produksjonslock.
- `results/`: utvalgte varige måle-/schema-/lisensmetadata fra evalueringen.
- `requirements-evaluation.txt` / `TimelineProbe/packages.lock.json`: kun evalueringsversjoner, ikke en release-SBOM.
- `.agent-local/phase1/`: raw probes, modellcache, syntetisk audio, PNG, venv, SDK og øvrige cacher; Git-ignorert.
- `TimelineProbe/bin/` og `obj/`: regenererbar output, Git-ignorert.

Ikke commit modeller, hentede song lyrics, audio eller maskinspesifikke filer. Ved rerun skal ikke historisk evidens under `results/` automatisk overskrives; nye funn må vurderes og dokumenteres eksplisitt.


Publisert evidens utelater lokal maskinvareprofil, OS-build og driverversjoner etter prosjekteierens valg. Råmålinger i `.agent-local/` beholdes lokalt; nye evidensfiler må kontrolleres for tilsvarende opplysninger før eventuell publisering.


## Videre Fase 1: korpus og native CPU-profil

Gjeldende forslag: [teknologianbefaling](../../docs/PHASE1_RECOMMENDATION.md), [nye resultater](../../docs/PHASE1_CONTINUATION_RESULTS.md) og [artefaktlisenser](../../docs/PHASE1_LICENSES.md). Ingen kommando her installerer et produkt eller starter Fase 2.

**Stock PyAV er avvist fra den anbefalte distribusjonsprofilen.** Tidligere probes med faster-whisper/PyAV beholdes som researchreferanse; deres rapporterte LGPL-flagg var ikke nok til å klarere bundled x264/x265. Den avsluttende profilen bruker whisper.cpp, direkte CTranslate2 med to uendrede MIT-hjelpere, Open-Unmix umxhq, SwiftF0 og egen minimal FFmpeg. Ingen av/faster-whisper-pakke i denne profilen.

### Forbered data og native kilder

Følgende er eksplisitte nett-nedlastinger av de pinnede researchartefaktene. Vocadito v3, tre engelske Jamendo BY/BY-SA-sanger og MIT UMXHQ-checkpoints beholdes lokalt. Ingen lyrics/audio/modeller skal committes.

~~~powershell
.agent-local/phase1/venv/Scripts/python.exe tools/phase1/prepare_song_corpora.py
py -3.12 tools/phase1/prepare_native_sources.py
~~~

Whisper.cpp/source/model-hashes er hardkodet og kontrolleres i prepareren. FFmpeg er en eksakt commit; sourcehash sammenlignes med results/minimal-codec.json. De offentlige målefilene skal ikke automatisk overskrives ved rerun.

### Sangmålinger

Vocadito-proben krever den pinnede tiny-modellen fra den tidligere ASR-prepareringen; angi snapshotmappen eksplisitt. Den lagrer bare mål, ikke lyrics/transcript. Begge pitch-detektorer bruker 50–1200 Hz; noter matches med 50 ms / 50 cent, ingen offsetgrense.

~~~powershell
.agent-local/phase1/venv/Scripts/python.exe tools/phase1/vocadito_benchmark.py --corpus .agent-local/phase1/continuation/vocadito --model MODEL_SNAPSHOT_DIRECTORY --output .agent-local/phase1/continuation/rerun-vocadito
~~~

alignment_probe.py er den historiske 29 s-prøven som også preparerer de private mono/48 kHz-miksingsfixtures fra korpuset. Den bruker den opprinnelige research-venven, med egen PyAV-dekoderadapter. separation_probe.py er kun den avviste Spleeter-referansen; normal installasjon/pip check skal ikke fremstilles som bestått.

openunmix_probe.py bruker --corpus, --models og --output, samt --full for private filer med suffiks .full.wav. Full-song-prøven måler sekvensielle 30 s-chunks uten crossfade; den er ikke en ferdig produktseparator. requirements-separation-cpu.txt låser første separate UMX-venv, ikke den avsluttende portable profilen.

### Egen minimal Windows codec

Docker brukes bare som build-/Linux-evalueringsmiljø. Prosjektet bind-mountes; ingen alternativ prosjektcheckout opprettes. Builderavhengigheter i image er verktøy, ikke produktpayload.

~~~powershell
docker build -f tools/phase1/LinuxSmoke.Dockerfile -t ultrastar-phase1-linux tools/phase1
docker build -f tools/phase1/MinimalCodec.Dockerfile -t ultrastar-phase1-codec tools/phase1
docker run --rm --mount "type=bind,source=$pwd,target=/project" ultrastar-phase1-codec sh tools/phase1/build_minimal_codec.sh
~~~

Recipe deaktiverer autodetect/network og velger bare erklærte audioformater, PCM-output og nødvendige audiofiltre. FFmpeg source og recipe legges ved lokal output. GIT_CEILING_DIRECTORIES forhindrer at FFmpeg feilaktig tar prosjektets Git-versjon som sin egen. Binary -L/konfigurasjon skal kontrolleres; flere codecs krever ny port.

### Windows native ASR og portable prøve

CMake og MSVC Build Tools brukes bare til denne native source-builden; produktets sluttbruker trenger dem ikke. Eksempelwrapperen bruker Visual Studio 18 2026-generatoren som faktisk ble prøvd. Angi CMakePath via PowerShell-parameteren -CMakePath dersom CMake ikke er i PATH. CPU-proben deaktiverer CUDA/Vulkan/FFmpeg/OpenMP/BLAS/AVX-varianter; dette er ikke en produksjonsoptimalisering.

~~~powershell
./tools/phase1/build_whisper_cpp.ps1 -CMakePath PATH_TO_CMAKE
./tools/phase1/prepare_portable.ps1
~~~

prepare_portable.py regenererer bare den validerte package-outputens Python site-packages og henter låste CPU-wheels med prosjektlokal cache. Den kopierer worker/probekode, MIT-hjelpere, native ASR/codec og pinnede modellcacher; samler faktiske notices. Det er en lokal researchpakke, ikke ferdig installer-SBOM eller offentlig release.

Fra pakkemappen kan native app kjøres med absolute fixture-data utenfor pakken:

~~~powershell
$env:PATH="$env:SystemRoot\System32;$env:SystemRoot"
$env:ULTRASTAR_PHASE1_UMX_CACHE="ABSOLUTE_PORTABLE_PATH/models/umxhq"
./app/NativeProbe.exe ABSOLUTE_48KHZ_PCM_WAV results ./python/python.exe worker/analysis_worker_probe.py
./python/python.exe worker/portable_dependencies_probe.py
~~~

NativeProbe krever audio.wav output-directory [--offline | python worker.py]. --offline tester PCM-loop/pause/seek uten lydenhet. --offline-ui er Linux/Xvfb-prøven med PCM-pumping; Windows default bruker en fysisk/default lydenhet og lavt volum. WAV er normalisert 48 kHz PCM16; valgfri .notes.csv-sidecar har start,pitch_hz,duration. Alle UI-/cancellation-asserts må passere.

whisper_cpp_probe.py bruker --exe, --model, --codec, --output og valgfri --work. Rå JSON/transcripts/systeminfo skrives bare i ignorert output; public metrics er eksplisitt separat. --prepared-wav brukes i Linux med allerede preparert PCM. direct_alignment_probe.py bruker --model, --corpus, --pcm, --output og kun den direkte CTranslate2-API-en.

### Linux smoke med nettverket av under inferens

~~~powershell
docker build -f tools/phase1/LinuxAnalysis.Dockerfile -t ultrastar-phase1-linux-analysis tools/phase1
docker build -f tools/phase1/LinuxNativeAnalysis.Dockerfile -t ultrastar-phase1-native-analysis tools/phase1
docker run --rm --network none --mount "type=bind,source=$pwd,target=/project" ultrastar-phase1-native-analysis sh tools/phase1/run_linux_native_analysis.sh
docker run --rm --network none --env ULTRASTAR_PHASE1_UMX_CACHE=/project/.agent-local/phase1/continuation/portable/models/umxhq --mount "type=bind,source=$pwd,target=/project" ultrastar-phase1-native-analysis sh tools/phase1/run_linux_smoke.sh
~~~

Første .NET restore krever nett eller ferdig lokal NuGet-cache; kommandoen med --network none ble kjørt etter cachepreparering. Linux bruker Xvfb/no-device PCM, ikke fysisk audio. Ingen Wayland-/ren maskin-/GPU-garanti følger av denne testen.

Vendored MIT-hjelpere har original LICENSE/proveniens. Alle binaries, modell-/korpusdata, private logs, screenshots og regenererbare outputs blir i .agent-local/ eller ignorerte buildmapper. Se aktive arbeidsplanen for beslutningsporten; ADR-er forblir foreslått.
