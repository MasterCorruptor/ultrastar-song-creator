# Audio-01 – avgrenset PCM-playback-kontrakt

Status 2026-10-12: implementert/verifisert på arbeidsbranch før SUPERVISOR-review. GUI er uendret.

Audio er et separat lag uten Core-/GUI-/fil-I/O-avhengighet. PcmClip eier én defensiv kopi av finite normaliserte float32-interleaved samples (mono/stereo, sample rate 8000–192000). Maks 16 millioner samples (~64 MB); demo 8s/48000/stereo. Ingen immutable Song kopierer PCM; tjenesten binder ett clip for hele levetiden. Tom clip er gyldig.

IPcmPlayback: Clip, atomisk PlaybackSnapshot(State, PositionFrames, SampleRate, FrameCount), Play/Pause/Stop/SeekFrame/SeekSeconds/Dispose. Initial Stopped/0. Pause fryser source-cursor og etterfølgende render gir nuller; Play fortsetter fra aktuell frame. Stop går til0/Stopped. Seek krever finite sekunder eller heltallig frame i [0,FrameCount], avrunder sekunder til nærmeste frame (midpoint away from zero). Ugyldig forespørsel avvises uten endring; seconds sjekkes mot klippets eksakte varighet før avrunding. Gyldig endpoint gir Ended; seek tilbake fra Ended gir Paused. Play ved endpoint forblir Ended (ingen skjult restart); Stop+Play starter på nytt. Render av siste frame gir Ended og nullfyller resten; alle tider er source frames/sample rate, ingen wall-clock/GUI-timerfasit.

PcmPlayback er den eksplisitte no-device render-kjernen, ikke silent fallback. Render fyller caller-eid Span<float>, krever hele frames og kopierer bare aktuell del av det samme clipet. Alle kommandoer/render/snapshot serialiseres med kort lås; ingen events/UI/native devicekall i denne låsen. Dispose er idempotent; snapshot kan fortsatt leses som Disposed, kommandoer avvises; sen render returnerer tystnad uten tilgang til samples.

MiniAudioPlayback eier kjernen og et stabilt native device/GCHandle. Device settes til float32 og clip-rate/channels, callback kaller samme Render. Device init/start skjer eksplisitt, feiler med forståelig PlaybackException; null-backend eller endret callback-format avvises, ingen mock-fallback. Device fortsetter å be om stillhet ved pause/stop/EOS. Lifecycle-lås rundt offentlige native-operasjoner holdes aldri av callback; device-uninit stopper/venter før renderer/GCHandle/native memory frigjøres. Callback kaller aldri device start/stop/uninit. Exceptions får ikke passere native callback; tystnad og Faulted-status ved renderfeil. Managed lås/GC medfører ingen hard real-time-garanti. OS/device som allerede har bufret output kan gi queued output-lag; cursor er source-render-position, ikke målt høyttalertid. Enhetstap/hotplug og device-switch er ikke dekket av første profil.

WaveformEnvelope inneholder bounded immutable per-bucket min/max over alle kanaler og half-open start/end-framegrenser, sample rate og totalframes. Første sample brukes som min/max-seed (ingen falsk nullbaseline). FramesPerBucket>=1, maks 100000 buckets; overskridelse avvises før allokering. Tom clip gir tom envelope; siste delvise bucket slutter eksakt ved FrameCount. Data beregnes én gang fra samme PcmClip, ikke per noteflytting. Envelope er display-data, ikke ny Core-analysis/prosjektkontrakt.

[Arbeidsordre](AUDIO_01_WORK_ORDER.md), [aktiv plan](exec-plans/active/audio-01-pcm-playback.md). Backend er basert på [miniaudios low-level device/callback-kontrakt](https://miniaud.io/docs/manual/index.html), evaluert Hexa.NET.MiniAudio 1.0.1 / miniaudio 0.11.25. Faktisk evidens og [pakke-/nativeinventar](AUDIO_01_DEPENDENCIES.json) følger nedenfor.

## Binding til Song-tid i neste blokk

PositionFrames/SampleRate er mediets source-tid. Fremtidig GUI mapper sangtid = source-tid − Song.AudioOffsetSeconds, og seek mapper source-tid = sangtid + offset etter CORE_DOMAIN. Backend kjenner ikke Song/offsets. Samme mapping brukes for waveform; ingen GUI-timer blir separat playback-klokke. Demo har offset0 og 8s-sang/PCM. Kontrakten må reviewes før denne bindingen implementeres.

## Reproduserbar kontroll

Med SDK etter global.json, fra repositoryroten:
```powershell
dotnet restore UltraStar.SongCreator.slnx --locked-mode
dotnet build UltraStar.SongCreator.slnx -c Release --no-restore
dotnet test UltraStar.SongCreator.slnx -c Release --no-build --no-restore
dotnet format UltraStar.SongCreator.slnx --no-restore --verify-no-changes
dotnet tools/audio/AudioProbe/bin/Release/net10.0/AudioProbe.dll --native .agent-local/audio-01/windows-native
```
Native-proben spiller en egen kort lavamplitude-tone (maks0,02); standardenhet kreves. Ingen ekstern lydfil eller konto. Ingen systemvolum endres. No-device-render testes i Audio.Tests; proben kan også bruke --load-only for biblioteklasting uten device. Lokalt gjenbrukes privat SDK/cache som beskrevet i DEVELOPMENT.

Linux-kontroll: eksisterende container/SDK10.0.401 med network none, NUGET_PACKAGES til eksisterende cache, separat --artifacts-path .agent-local/audio-01/linux-artifacts. Locked restore bruker NuGetAudit=false kun offline, så Release build/test. Kjør probe-DLL fra Linux artifacts med --load-only og .agent-local/audio-01/linux-load. Fysisk Linux-device er ikke prøvd.

## Faktisk evidens og avgrensninger

Windows og Linux: 449/449 (432 tidligere +17 Audio), locked restore, Release build 0 warnings/errors og Windows SDK-format bestått. Render/sample-/channeldata sammenlignes mot kjent referanse over ulike chunkstørrelser; pause/resume/stop/seek/EOS, ugyldige requests, tomt klipp, dispose/samtidighet og waveform-envelope/bounds/final bucket er dekket. NativeLibrary-versjon er0.11.25 på begge OS.

Windows native: faktisk Wasapi-backend, float32/48000/stereo, init/start og fremadgående source-cursor. Siste prøve: pauseframe5280 stabil120ms, paused seek nøyaktig24000 (0,5s), resume, endpoint/near-end/EOS, stop0 og dispose mens kommando-loop kjørte. 638 kommandoiterasjoner før disposed-avvisning; dispose16,6844ms, snapshot stabil120ms etterpå. Dette er én kort prøve, ikke latency-/ytelsesgaranti. Private resultater ligger under .agent-local/audio-01; ingen device-/maskinvareidentifikatorer publiseres.

Ingen akustisk loopback, målt høyttalertid eller eierbekreftet hørbarhet. Ingen fysisk Linux-device, device-hotplug/switch eller systematisk alle-OS-devicefeilprøve. Første profil krever korrekt Dispose av eieren. Managed lås/GC betyr at hard real-time ikke loves. En allerede bufret lydblokk kan fortsatt høres etter pause/seek; render-cursor er korrekt kildeposisjon. Ingen GUI-binding/loop/audioimport/codecs eller full prototype i denne leveransen.

## Dependency/lisens

Runtime: Hexa.NET.MiniAudio1.0.1 og HexaGen.Runtime1.1.24, eksakt samme pakke-SHA256 som Fase1. miniaudio ABI0.11.25 er lastet på Win-x64 og Linux-x64. [Inventaret](AUDIO_01_DEPENDENCIES.json) har15 faktiske runtime/test/probe-pakker, versjoner/contentHash/SHA256/kildereferanser/notices og begge native asset-hashes. Binding/runtime er MIT, miniaudio tilbyr Public Domain eller MIT No Attribution (sistnevnte valgt). [Notices](../tools/audio/notices/miniaudio-LICENSE), [binding](../tools/audio/notices/hexa.net.miniaudio-LICENSE.txt), [interop runtime](../tools/audio/notices/hexagen.runtime-LICENSE.txt) bevares; kilde- og pakke-notices er normalisert til LF med uendret tekst. Testpakker gjenbruker det etablerte MIT/Apache-2.0-grunnlaget og distribueres ikke som produkt-runtime.

Providerens pinnede build-recipe angir upstream0.11.25, men bruker mutable cmake-actions v1.x.x; uavhengig reproducerbar native build hevdes ikke. Inventaret peker til eksakt miniaudio-kildecommit og pakke/native-hashes. Ingen binærrelease/installer lages; fremtidig distribusjon må beholde faktiske tredjepartsnotices/runtimevilkår etter ADR-0002. Ingen nye codec-/modellvilkår eller endret MIT egenkode. GUI-01/runtimeinventar er uendret.
