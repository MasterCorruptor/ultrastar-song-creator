# Lyrics panel component B2 — checkpoint report
STATUS: CHECKPOINT — implementation/verification complete; STOP FOR SUPERVISOR REVIEW.
TASK: phase3-lyrics-panel-component. MEDIUM / SEQUENTIAL, single worker.
MODEL USED: Sol High requested; actual model/reasoning unknown.
BRANCH: work/phase3-lyrics-panel-component.
BASE: accepted B1 fb6fcb63fec6df97a7a604e3f2fd34bff8f634c9.
API baseline:17c0de1. TESTED IMPLEMENTATION/LAST CODE COMMIT:2d137f4770161672aefbd761b7e63815506dbd65. Later current HEAD is a docs-only checkpoint; resolve git log.
[Work order/API](../../PHASE3_LYRICS_PANEL_COMPONENT_WORK_ORDER.md), [parent functional contract](../../PHASE3_LYRICS_PANEL_PROPOSAL.md), [accepted B1](../completed/phase3-lyrics-editor-apply.md).

## Completed
Borrowed functional LyricsPanel with blank explicit inputs, search/cancel, explicit candidate/document/instrumental choice, manual preparation, typed static status/retry/diagnostics, exact text and provenance/source-time preview. No automatic first candidate or document. Stored song text/target shown separately. Apply reuses the immutable A payload through existing B1 history at the current UI song, retaining note edits/no-op redo behavior.

Synchronous TextProperty changes immediately disable old eligibility before deferred text/render events or background Reset. Generation/current revision/query/lifetime checks reject stale queued renders and selection; external borrowed-session changes cannot silently enable Apply. A actions run off UI in one synchronous dispatch job plus one latest pending intent; provider task observation does not hold the queue. Observer faults are isolated and all outcomes/faults observed without raw exception text.

Detach invalidates/unsubscribes/disables and drops pending actions without disposing borrowed resources. Internal explicitly owned helper returns one reusable Task<bool> (false/static cleanup failure flag); cleanup starts on a background worker before A.DisposeAsync, observes admitted work/searches, waits A drain, then disposes owned provider. Ignored cancellation or held callbacks can delay drain without blocking dispatcher; no hard interruption promise. Borrowed close leaves session/provider usable by caller.

## Verification at 2d137f4
SDK10.0.401:24/24 new deterministic offline/headless component cases passed. Locked full solution restore, Release build0warnings/0errors, full Windows977/977 (Core871,Audio17,Desktop89), zero skipped, full format verify passed.

Tests cover no startup search/autopick; explicit Unicode query, line/plain preview/diagnostics/source time, same current history/frozen identity/undo-redo/no-op, manual invalidation/rejection, explicit instrumental/unavailable, cancel/no-results/typed provider retry errors, stale selection/external revision/queued preview and detach. Controlled TCS/cancellation gates prove latest-intent coalescing, search-independent manual admission, held-callback UI responsiveness, close-initiated cancellation off UI, shared close task and disposal order, borrowed reuse and cleanup/observer/action faults. No sleeps/network/device/native window.

207 existing protected source/tests/tools/master/ADR files byte-identical to B1, excluding only authorized csproj and two locks. Exact lock diff adds Acquisition Project and Desktop's corresponding Project dependency; all external entries/versions/content hashes and other lockfile bytes unchanged. Initial force-evaluate restore rewrote line endings in three protected lockfiles; byte identity was restored after semantic equality check before verification. Original master copy, public inventory/privacy/vendor/document links and whitespace checked.

Code source/test/project/lock bytes remain identical to tested2d137f4 after documentation-only checkpoint. Original A440ea86, B preparation301b885, B1fb6fcb6 and main6ddb743 references preserved. A exact099884b was already published separately in PR21.

## Changed files / unresolved / next
Only NEW Desktop/Lyrics/LyricsPanel.cs + Desktop.Tests/LyricsPanelTests.cs, Desktop.csproj (local Acquisition reference + friend access), Desktop/Desktop.Tests locks (local project graph), and minimal workflow/status docs.

B3 EditorWindow/EditorApplication production composition, UI audio release, normal-window-close guard and startup-failure cleanup are NOT implemented here. Component exposes detach/owned close/cleanup result for that later integration. Native/manual/live LRCLIB/Linux/fullworkflow/alpha-beta acceptance remains pending; no new claim or release. No public push/PR/merge/main update for B1/B2 and no new worker.

NEXT RECOMMENDED STEP: SUPERVISOR review actual component diff and evidence, then separately scope B3 wiring if accepted. OWNER DECISION REQUIRED: NO new product/architecture choice. PR21 merge permission is managed separately; this component order does not grant it.
