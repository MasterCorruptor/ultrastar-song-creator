# Lyrics flow preparation — local checkpoint

STATUS: CONTRACT_FROZEN / STOP_FOR_RESET. SUPERVISOR accepted the A/B sequence at 10994cf; requested bounded-state/lifetime clarifications applied. No implementation.
TASK: phase3-lyrics-flow-preparation, SMALL / SEQUENTIAL / docs-only, one worker.
MODEL USED: Sol Medium requested; actual model/reasoning not exposed, unknown.
BRANCH: work/phase3-lyrics-flow-preparation.
BASE: 6ddb743e7d06853eb194bb83505ea62fe489a7b7.
CURRENT HEAD: local preparation commit containing this report; resolve with git log.
[Proposal](../../PHASE3_LYRICS_FLOW_PROPOSAL.md).

## Completed / gaps

Read actual provider/manual selection/projection, immutable Core/SetSelectedLyrics/SongHistory, v3 ProjectStore/ProjectJson and Desktop session/window/composition/playback/project references. Confirmed backend available but no UI search/choice/manual/apply coordinator. Desktop only edits a demo song and uses DemoPcm; it has no acquisition/project-file UI, no Acquisition/Projects reference, and EditingSession exposes no lyrics command seam.

Recommended A: Acquisition-only transient session with borrowed provider, immutable/revisioned state, cancellation/stale-result/disposal rules and explicit prepared choice; no Song/history mutation. B after A review: minimal lyrics panel + narrow same-history EditingSession apply, controlled composition/UI-thread cleanup, local Acquisition project reference. Real media/project UI and Generation remain separately scoped.

A now specifies one active provider invocation/one pending latest intent, deterministic superseded tasks, concrete typed/stale-selection outcomes, external callbacks/cancellation outside locks, isolated observer faults, one final disposed snapshot/notification and reusable drain task. Future tests cover rapid supersession/reentrancy/ignored cancellation/disposal. No new tests now. No automatic selection/ranking/ASR/timing inference or schema/package/account change. Explicit time/provenance/persistence boundary retained.

## Verification / changed files

Only new proposal/this plan and minimal current-status updates in AGENTS, README, TESTING_STATUS and LRCLIB_TRANSPORT_PROFILE. Document links/privacy/public inventory/vendor/whitespace and protected-file byte identity checked. Master equals supplied original; source/tests/tools/ADRs/licenses/dependencies/locks unchanged from integrated main. No new suite for docs-only; existing db51699 70-new/892-Windows evidence remains historical tested snapshot, not a new coordinator/UI result.

## Unresolved / next step

Frozen behavior follows SUPERVISOR contract clarifications; exact API/type signatures belong to the next same-scope implementation baseline. No implementation dispatched yet. Functional media/project GUI gaps and actual lyrics/audio match remain later work, not new blocks on A. Commit final two-document clarification locally, send one unique CONTRACT_FROZEN / STOP_FOR_RESET callback and stop for reset. Resume A only under a new explicit SUPERVISOR scoped dispatch after fresh actual capacity reading, not elapsed time. No reset credits/purchases. No push/PR/merge/live API/native UI/extra worker or automatic implementation.
OWNER DECISION REQUIRED: NO.
