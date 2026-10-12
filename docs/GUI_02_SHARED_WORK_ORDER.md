# GUI-02 – delt kontrakt før komponentarbeid

SUPERVISOR-ordre 2026-10-12: gui-02-shared-contract, SMALL/PARALLEL_WITH_DEPENDENCIES, foreløpig én worker, bestilt Sol Medium (faktisk innstilling ukjent). Base/main86660f5. Autoritet: AGENTS/master/ADR-er/SUPERVISOR_POLICY, GUI_01_TEST_SURFACE/EditorWindow/EditingSession og AUDIO_PLAYBACK.

Mål: minste reviewbare compilable grunnlag for to uavhengige senere GUI-komponenter: delt koordinat/offset/frame-clock-mapping og konkret API-/fil-/lifecyclekontrakt. Desktop→Audio ProjectReference og nødvendige locks, ingen nye eksterne pakker.

Acceptance: avgrenset mappingtype/tester, konkret uavhengig scope/signatur/eierskap for waveform-workerA og transport-workerB, locked restore/Release build/test/format, tidligere449regresjoner og master/ADR/integritet/lenker/whitespace. Audio/PCM forblir utenfor Song/Core. Demo0–8s/origin75/scale110, song=source−offset og source=song+offset; avvis ikke-finite/out-of-range seek, eksplisitte playhead-null/EOS-regler.

Ingen waveform-/transportkomponenter, EditorWindow/Application-wiring, GUI/tone/CU-test, nye dependencies, full release eller redesign. Ingen workers/worktrees/chats/subagents opprettes her. Commit/push sikkert felles checkpoint og stopp for SUPERVISOR-review; denne kontrakten må reviewes før eventuell parallellstart med samlet kvotevurdering. Ingen merge/GUI-02-implementasjon uten senere ordre.
