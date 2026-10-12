# Audio-01 – aktiv plan

STATUS: CHECKPOINT. TASK: audio-01-pcm-playback-contract. BRANCH: work/audio-01-pcm-playback. Base 0003bd2d0187316cf958129ea2090b4c78dbdbc6. MODEL USED: ukjent; bestilt gpt-6.1-sol/high. MEDIUM/SEQUENTIAL, én worker. Kvote før start: 81 % femtimer / 64 % uke gjenstår, faktisk avlest.

[Arbeidsordre](../../AUDIO_01_WORK_ORDER.md), [kontrakt](../../AUDIO_PLAYBACK.md).

1. Kontrakt/immutable bounded PCM/waveform/render-kjerne og meningsfulle boundary/lifecycle/samtidighetstester → verifisert commit/push.
2. Ekte native miniaudio-backend og egen kort probe → Windows device-smoke/Linux no-device/native loading, dependency/notices-inventar.
3. Full locked restore/Release build/test/format, integritet/lenker/privacy → checkpoint/draft PR og stopp for SUPERVISOR-review. Ingen GUI/merge.

## Ferdig reviewbart checkpoint

COMPLETED: utskiftbart IPcmPlayback, bounded immutable PcmClip/DemoPcm, delt PcmPlayback-render/clock/state-kjerne, WaveformEnvelope og faktisk MiniAudioPlayback. Native lifecycle serialiseres separat fra callback/render; uninit join før GCHandle/native memory frigjøres. Ingen null-device-fallback, GUI-/Core-/fil-I/O-endring. [Kontrakt/evidens](../../AUDIO_PLAYBACK.md), [inventar](../../AUDIO_01_DEPENDENCIES.json).

LAST COMMIT (verifisert kode): a201c41d75d6ae8a316f6e08e090975ce2167a1c. Første kontrakt/render-checkpoint43ba354 ble verifisert/pushet før native backend. Endelig dokumentasjon ligger i senere checkpoint; final/Git angir siste SHA. Faktisk modell/reasoning ukjent; bestilt gpt-6.1-sol/high, ingen ekstra worker.

VERIFICATION: Windows locked restore/Release build0warnings0errors/test449/449/SDK-format består. Nettisolert Linux locked restore (audit av bare offline), Release build0warnings0errors/test449/449 og faktisk native-library-load0.11.25 består. 17 Audio-tester +432 tidligere. Windows faktisk Wasapi init/start/rendercursor, pause stabil120ms ved5280frames, seek24000, resume, endpoint/near-end/EOS, stop0, samtidig kommando-loop/dispose638iterasjoner,16,6844ms og stabil disposed-snapshot120ms. Samme render-kjerne testes uten device. Ingen fysisk Linux-device eller akustisk latency/hørbarhetsbevis.

FILES / MODULES CHANGED: Audio, isolert Audio.Tests, AudioProbe/notices, tre nye lockfiler, solution/.gitignore, arbeidsordre/kontrakt/inventar/aktiv plan og korte statusreferanser. Bare2 nye runtimepakker, begge evaluert og samme hashes som Fase1. Totalt15 faktiske pakker inventoriert; miniaudio sourcecommit/notices/nativeWin-x64/Linux-x64assets hashet. Ingen CI/release/installer.

INTEGRITY: eksisterende src/tests/tools/lockfiler mot main0003bd2, masteroriginal/aksepterte ADR-er/historiske fixtures og GUI-01-kontrakter kontrolleres bytebevart. Offentlig inventar/JSON/dokumentlenker/privacy/whitespace kontrolleres før finalcommit. Ren main/upstream/GitHubmain0003bd2 bevares, ingen merge.

UNRESOLVED: ingen kjent blokkerende feil innen profilen; managed lock/GC uten hard real-time-garanti, queued output-lag, klippeier må Dispose. Ingen hørbarhetsbekreftelse/akustisk loopback, fysisk Linux-device/hotplug/switch/devicefeilmatrise eller uavhengig reproducerbar provider-build. Resten av prototypen er ikke oppfylt. NEXT RECOMMENDED STEP: SUPERVISOR reviewer kontrakt/kode/native-vs-no-device-evidens/notices før integrasjonsordre; deretter separat GUI-02 med play/pause/stop, waveform/playhead/seek bundet til denne ene clock. Ingen slik binding starter her. OWNER DECISION REQUIRED: NO. Kvote ved naturlig nativecheckpoint:72%femtimer/63%uke igjen, faktisk avlest.
