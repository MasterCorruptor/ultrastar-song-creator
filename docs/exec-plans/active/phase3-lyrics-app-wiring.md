# B3 lyrics app wiring — checkpoint report
STATUS: CHECKPOINT — implementation and Windows verification complete; STOP FOR SUPERVISOR REVIEW.
TASK: phase3-lyrics-app-wiring, MEDIUM / SEQUENTIAL, one implementer.
MODEL USED: Sol High requested; actual runtime model/reasoning unknown.
BRANCH: work/phase3-lyrics-app-wiring.
BASE: accepted B2 ab5822d0abcdfafb87637ef08b590abc02076771 (source/test baseline c83152b/984Windows).
API/work-order baseline:4fd2b5c. TESTED IMPLEMENTATION/LAST CODE COMMIT:38e1b5a87a303e836f17eb1098bae8d4c8ce0176. Later HEAD is docs-only (resolve git log); source/test/project/lock bytes remain identical to this tested commit.
[Work order/API](../../PHASE3_LYRICS_APP_WIRING_WORK_ORDER.md), [accepted B2](../completed/phase3-lyrics-panel-component.md).

## Completed
Existing editor prototype now includes accepted LyricsPanel with the same EditingSession/history and cached playback/timeline. Production explicitly constructs real LrclibProvider, acquisition session and transferred B2 lifetime ownership; no startup search, autopick, fake fallback, metadata overwrite, audio matching or Generation.

EditorComposition captures each returned resource before the next acquisition. All factories are narrow internal delegates in EditorApplication.cs; incomplete/unreturned factory resources remain the factory's responsibility. Playback→provider→session→lifetime→panel→window→assignment failures follow the same observed cleanup path. The small internal EditorDesktopHost adapts Avalonia lifetime callbacks because its interfaces intentionally forbid client implementations; tests use the same app startup/shutdown logic with controlled host callbacks.

Normal lyrics-enabled window Closing detaches and disables the panel/editor commands, releases audio on UI first, and starts/reuses one observed CloseTask. Existing B2 background admission/drain/owned-provider disposal remains unchanged. Dispatcher stays responsive while ignored cancellation or synchronous callbacks delay closure. Final close is posted to UI after successful cleanup; repeated requests cannot dispose twice. Platform shutdown requests route through guarded closure; explicit desktop lifetime keeps the app alive until completion. Legacy public constructor and borrowed panel ownership stay supported.

Startup failure displays static guarded status while observed cleanup completes before normal shutdown. A partially assigned editor is still owned and cleaned. Cleanup failures, including audio/provider and faulted task paths, remain static guarded failures; no raw exception strings or forced close. Existing B2 shared failed CloseTask is reused; no automatic cleanup retry/recovery mechanism is added. Arbitrary OS hardkill cannot guarantee orderly cleanup.

## Verification at 38e1b5a
SDK10.0.401:16/16 new deterministic headless B3 cases passed. Locked full solution restore passed; Release build0warnings/0errors; full Windows1000/1000 (Core871,Audio17,Desktop112), zero skipped; full format verify passed.

New tests exercise:
- Actual composed panel/no startup request, real default LRCLIB provider construction without a search, manual prepare/apply, note change plus exact mixed-history undo/redo, frozen payload and cached waveform/playhead/playback continuity.
- Normal/platform-requested close with ignored cancellation; held callback proves audio disposal is on UI before new lyrics cancellation, dispatcher responsiveness, shared repeated close task and provider disposal after drain.
- Provider/audio cleanup failure, faulted close-task observation, static close guard and no duplicate disposal.
- Startup faults at playback/provider/session identity/panel/window/assignment; correct partial ownership/audio-first ordering and normal shutdown only after cleanup.
- Startup held callback/ignored provider keeps guarded status window and explicit app lifetime alive; cleanup failure stays guarded.
- Borrowed lyrics overload preserves caller session/provider; existing legacy constructor/editor/playback tests pass unchanged.

Controlled gates/TCS and synthetic fixtures only: no introduced sleeps, network requests or physical device. Tests use an app-specific host callback adapter, not an actual native classic-desktop event loop. Fixture teardown closes already-cleaned headless hosts after assertions; this is not a production forced-close path.

Actual ignored evidence paths:
.agent-local/phase1/continuation/lyrics-app-b3/restore.log
.agent-local/phase1/continuation/lyrics-app-b3/build.log
.agent-local/phase1/continuation/lyrics-app-b3/tests.log
.agent-local/phase1/continuation/lyrics-app-b3/format.log

210 existing protected source/test/tool/project/lock/master/ADR files are byte-identical to ab5822d; only two authorized existing production files changed, plus one new owned test file. B2/EditingSession/A/providers/Core/Projects/audio/shared contracts and all project/dependency/lock bytes unchanged. Original master, public inventory/privacy/vendor/document links/whitespace checked. No full suite is repeated for final docs-only changes.

## Changed files / limitations / next
Production: EditorApplication.cs (narrow internal ownership/factory/host adapter plus app wiring), EditorWindow.cs (panel insertion/internal overload/observed close guard). Tests: NEW LyricsApplicationLifetimeTests.cs only; existing tests unchanged. Necessary work-order/plan/AGENTS/README/testing status docs.

Native Windows/manual app interaction and physical playback, live LRCLIB response compatibility, Linux, failure of underlying Avalonia platform initialization, and full Search→Generate→Edit→Validate→Export acceptance remain unverified here. Cleanup failure retains guard; future explicit recovery UI is outside this order. This is not a release or an alpha/beta acceptance claim.

NEXT RECOMMENDED STEP: SUPERVISOR review actual B3 diff/evidence and decide the next bounded verification/integration order. OWNER DECISION REQUIRED: NO new architecture/scope/licensing choice identified. STOP locally committed; no push/PR/merge/main update/B1-B2 integration/new worker/Generation.
Main/origin/main stay d5a623104b40445f78abbb0cd409b4573153e9bc; B2 branch ab5822d and earlier A/B1 references preserved.
