# B3 lyrics application wiring
STATUS: COMPLETE — local offline composition accepted at bc7d17b/tested38e1b5a. MEDIUM / SEQUENTIAL / one implementer, Sol High requested (actual runtime model/reasoning unknown).
Base: accepted local B2 ab5822d0abcdfafb87637ef08b590abc02076771; source/tests identical tested c83152b (984 Windows cases). Branch work/phase3-lyrics-app-wiring. Main/origin/main d5a6231 remains separate A integration.

## Ownership and boundaries
Production edits only EditorWindow.cs and EditorApplication.cs; small internal factory/composition helper stays in EditorApplication.cs. Tests: new LyricsApplicationLifetimeTests.cs, existing EditingWindowTests/EditorPlaybackIntegrationTests only if needed. B2 panel/lifetime, EditingSession, A/provider/Core/Projects/audio/shared contracts/master/ADRs/project/lock/dependency bytes frozen. Functional composition only; no GUI polish, startup request/autopick, metadata overwrite, recording/audio-match promise or Generation.

Preserve public legacy EditorWindow(session,playback=null). Internal lyrics overload accepts existing panel and cleanup callback for narrow headless ownership tests. Panel is inserted in existing editor; same editing session/history, timeline and cached playback.

## Concrete internal composition / close baseline
EditorFactories supplies narrow synchronous resource-returning delegates for playback, paired provider/owner, session, panel, window. Default provider is real LrclibProvider, never a fake fallback. Factories own partial resources until successfully returned; EditorComposition captures every returned resource before the next acquisition. Session/provider ownership explicitly transfers to existing B2 lifetime before panel creation.

EditorComposition.TryStart(attach) acquires resources on UI and captures startup failure without exposing exceptions. CleanupAsync is one shared observed Task<bool>: detach/invalidate on UI, ReleasePlayback/Dispose on UI first, then existing B2 panel/lifetime CloseAsync off-UI drain/provider disposal. Before a session/lifetime exists, returned provider ownership still gets background disposal after audio. All failure paths observe completion. No raw exception strings.

Lyrics-enabled EditorWindow guards normal Closing, starts one observed CloseTask, remains alive while draining, and posts final permitted Close to UI only on success. Repeated close reuses it. Cleanup failure shows static text/retains guard; no forced close. Legacy constructor keeps original behavior and borrowed resources remain borrowed.

EditorApplication uses explicit desktop shutdown mode while it owns startup/cleanup. It assigns production window only through captured composition, requests guarded normal close on platform shutdown requests, and performs normal shutdown only after successful final close. Startup failures display static status in a guarded failure window; observed startup-cleanup completion precedes shutdown. Cleanup failure keeps app lifetime/guard alive. No unobserved async-void; OS hardkill cannot guarantee orderly cleanup.

## Acceptance / verification / stop
Deterministic headless full composition: blank panel/no request; manual/apply/note edits/same undo-redo; timeline/playback continuity; ignored cancellation and held synchronous callback close stays responsive; UI audio precedes owned lyrics drain/provider disposal; shared repeated close; cleanup failure/static guard; startup faults at playback/provider/session/panel/window/assignment with correct partial ownership and shutdown ordering; legacy/borrowed regressions.

Targeted tests, coherent code commit, locked restore/Release zero warnings/full Windows/format; frozen-byte/master/ADR/lock/privacy/inventory/vendor/docs/whitespace checks, actual tested SHA/final HEAD/local evidence paths. Stop local committed review checkpoint. No push/PR/merge/main update/B1-B2 integration/new worker/Generation. Native/manual/live/Linux/fullworkflow acceptance remains separate.

Avalonia lifetime interfaces intentionally forbid client implementations. A small internal EditorDesktopHost record in EditorApplication.cs adapts only explicit-shutdown mode, main-window assignment, shutdown and exit/request observation to app-specific callbacks. Production wraps the real Avalonia lifetime; deterministic tests inject those callbacks, not a public backend or alternate app path.

## Actual local evidence

Tested code38e1b5a:16new/1000total Windows cases (Core871/Audio17/Desktop112), zero skipped; locked restore/Release0warnings/full format passed. [Checkpoint and explicit limits](exec-plans/completed/phase3-lyrics-app-wiring.md). All frozen project/lock/source seams retained; no native/live/Linux or publication acceptance. STOP FOR REVIEW.

## Final acceptance

SUPERVISOR and independent review found no actionable findings at bc7d17b/tested38e1b5a (1000Windows). [Completed report](exec-plans/completed/phase3-lyrics-app-wiring.md). Documentation closure preserves tested bytes; native/manual/live/Linux/fullworkflow remain separate. Combined integration preparation is a new local-only order, not publication authority.
