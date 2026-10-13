# GUI-02 B – playback-presenter og panel

Bestilt av SUPERVISOR etter godkjent felles kontrakt67bf70e6d8bc30c8c7d4caad3754a4a160ee0eb2. MEDIUM, Sol Medium bestilt/faktisk modell ukjent. PARALLEL_WITH_DEPENDENCIES: delt dependency er reviewet, to workers totalt. B bruker primær checkout/work/gui-02-transport; A bruker separat ignorert waveform-worktree/branch. Ingen ekstra worker opprettes av B.

Autoritet: AGENTS/master/ADR/SUPERVISOR_POLICY og [eksakt kontrakt](GUI_02_SHARED_CONTRACT.md). Eksklusiv B-kode: Desktop/Playback/PlaybackPresenter.cs, PlaybackPanel.cs og Desktop.Tests/PlaybackPanelTests.cs. Integrasjonsansvarlig B kan føre denne ordren/aktive plan og oppdatere operative AGENTS/policy. Ingen A-/mapping-/felles test-/csproj-/lock-/EditorWindow/Applicationendringer.

Presenter låner IPcmPlayback, Refresh bruker ett snapshot, UI-thread Changed, signed offset uten seek, command/seek/error/buttonstate etter kontrakten. Panel poller bare attached og stopper/unsubscriber ved detach/dispose. Ingen borrowed backend/presenter-disposal. Backend/devices opprettes først senere av app-eier.

Acceptance: ekte PcmPlayback og målrettet backend-errorfixture, headless klikk/tekst, Render→Refresh-fremgang, pause/seek/stop/EOS, offset/bounds/knapper og detach/disposal uten backend-disposal. Locked restore, Release Desktop-build, relevante Desktop/B-tester, format/integritet/whitespace. Ingen ny full422Core/Linux/nativeprøve.

Ingen native tone/CU/GUI-app-wiring, integration/cherry-pick/merge/PR eller pakkeendring. Verifiser/commit/push sikkert checkpoint og stopp for SUPERVISOR-review. Rapporter faktisk kvote dersom scope vokser; ingen ny blokk startes ved lav kvote.
