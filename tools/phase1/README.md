# Fase 1-evalueringsverktøy

Dette er avgrensede feasibility-prøver, ikke produktkode eller valgte produksjonsdependencies. [Auditten](../../docs/PHASE1_AUDIT.md) og [resultatene](../../docs/PHASE1_RESULTS.md) forklarer formål og praktiske begrensninger.

Alle kommandoer nedenfor kjøres fra prosjektroten. Oppsettet er faktisk prøvd på Windows med Python 3.12.10. Linux- og clean-machine-reproduksjon er ikke verifisert.

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
