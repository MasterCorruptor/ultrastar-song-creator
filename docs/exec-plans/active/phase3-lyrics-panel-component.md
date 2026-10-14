# Lyrics panel component B2 — checkpoint report
STATUS: CHECKPOINT — review fixes implemented and verified; STOP FOR SUPERVISOR REVIEW.
TASK: phase3-lyrics-panel-component. MEDIUM / SEQUENTIAL, single worker.
MODEL USED: Sol High requested; actual model/reasoning unknown.
BRANCH: work/phase3-lyrics-panel-component.
BASE: accepted B1 fb6fcb63fec6df97a7a604e3f2fd34bff8f634c9.
API baseline:17c0de1. TESTED IMPLEMENTATION/LAST CODE COMMIT:c83152b56a34cc7dc3bcdea06d31b7d0f24f00a1. Original component code:2d137f4. Later current HEAD is a docs-only checkpoint; resolve git log.
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

## REQUEST_FIX — actual verification at c83152b
SUPERVISOR and independent review identified two P2 defects at a07a0d7: a reentrant observer could replace a just-prepared payload before its receipt, and normal candidate choice called Select with no document, producing a false validation error. Both are corrected within the two owned B2 code/test files.

Explicit manual/variant preparation now binds a completed successful synchronous action to exactly one state revision from its admission baseline and the same immutable prepared snapshot. Observation/render must match that binding; newer external state, including identical text, cannot authorize Apply. Search/reset/cancel/candidate guidance never authorize a payload. No reconstruction of ManualLyrics, new timestamps, silent retries or changed A/Core contracts.

Candidate choice only invalidates local preview/eligibility and checks its page revision; Select is deferred to the explicit variant choice. Ordinary candidate→variant and switching a previously selected candidate show normal guidance without an A validation error. Genuine stale choices still reject safely. Normal error statuses use understandable static wording rather than raw enum names.

Seven additional deterministic cases: reentrant manual replacement with different/identical text, external ManualReady during reset/search/cancel, provider variant replaced by observer, and changing a chosen candidate. Existing two-step selection/error tests are strengthened. 31/31 component cases; full Windows984/984 (Core871,Audio17,Desktop96), zero skipped. Locked restore, Release0warnings/errors, full format verify passed; no extra suite after later docs-only changes.

Actual ignored evidence: .agent-local/phase1/continuation/lyrics-panel-b2-fix/restore.log, build.log, tests.log, format.log. These are exact local evidence paths, not distributable product artefacts. Current docs checkpoint keeps source/tests identical to c83152b. 210 other frozen source/test/project/lock/tool/master/ADR files are byte-identical to a07a0d7; master original/privacy/vendor/links/whitespace checks passed.

B3/native/live/Linux remain pending. PR21 A939 merge is separately owner-approved and scheduled after this safe checkpoint; it does not integrate B1/B2. No new feature starts before component review.

## Separate A integration completed after safe B2 checkpoint

After B2 review fixes were tested at c83152b and saved at docs checkpoint fe2bce7, owner-approved PR21 https://github.com/MasterCorruptor/ultrastar-song-creator/pull/21 was merged at exact head099884bc320a7d9a2fd9c95a5fa1eac290aa8ef7. Preflight origin/base/head/diff unchanged, MERGEABLE/CLEAN, no hosted checks configured. Merge commit d5a623104b40445f78abbb0cd409b4573153e9bc is verified on GitHub/main, origin/main and local main. Entire merge tree is identical to approved A099884b; no repeated A suite was required. A939 evidence remains distinct from B2 fixed984 tests.

Local main advanced by ordinary fast-forward fetch without switching/reset/force; A440ea86, B1fb6fcb6 and B preparation301b885 refs preserved. B2 stays on work/phase3-lyrics-panel-component, with no source changes or hidden main integration/publication. Later HEAD is documentation only relative to tested c83152b. STOP FOR SUPERVISOR REVIEW; B3 not started.
