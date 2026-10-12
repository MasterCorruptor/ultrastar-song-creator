# Audio-01 – aktiv plan

STATUS: IN PROGRESS. TASK: audio-01-pcm-playback-contract. BRANCH: work/audio-01-pcm-playback. Base 0003bd2d0187316cf958129ea2090b4c78dbdbc6. MODEL USED: ukjent; bestilt gpt-6.1-sol/high. MEDIUM/SEQUENTIAL, én worker. Kvote før start: 81 % femtimer / 64 % uke gjenstår, faktisk avlest.

[Arbeidsordre](../../AUDIO_01_WORK_ORDER.md), [kontrakt](../../AUDIO_PLAYBACK.md).

1. Kontrakt/immutable bounded PCM/waveform/render-kjerne og meningsfulle boundary/lifecycle/samtidighetstester → verifisert commit/push.
2. Ekte native miniaudio-backend og egen kort probe → Windows device-smoke/Linux no-device/native loading, dependency/notices-inventar.
3. Full locked restore/Release build/test/format, integritet/lenker/privacy → checkpoint/draft PR og stopp for SUPERVISOR-review. Ingen GUI/merge.
