# Lyrics acquisition session A — local tested checkpoint

STATUS: CHECKPOINT, implementation/Windows verification complete; STOP FOR SUPERVISOR REVIEW.
TASK: phase3-lyrics-acquisition-session, MEDIUM / SEQUENTIAL, single worker.
MODEL USED: Sol Medium requested; actual model/reasoning not exposed, unknown.
BRANCH: work/phase3-lyrics-acquisition-session.
BASE: accepted local136e41e57007fa7baa4e41f39c747abaf87af508; integrated main6ddb743 unchanged.
API BASELINE COMMIT: 650fc12.
TESTED IMPLEMENTATION SHA: e4b39231156732ee1243e3d77898371bc1789bc2.
CURRENT HEAD: later documentation-only checkpoint containing this report; resolve with git log.
[Order/API](../../PHASE3_LYRICS_SESSION_WORK_ORDER.md), [frozen behavior](../../PHASE3_LYRICS_FLOW_PROPOSAL.md), [completed preparation](../completed/phase3-lyrics-flow-preparation.md).

## Completed

Borrowed-provider, presentation-neutral immutable Current/revision/typed outcomes and errors. One provider invocation/one newest pending intent, immediately settled superseded outward tasks and observed actual completions. Full original-query/provider/page validation, no automatic choice/ranking; exact explicit selection/instrumental, stale rejection without mutation, invalid current choice retaining valid page and clearing preview, one frozen manual payload.

Manual/reset/cancel invalidate queued work without Song/history/I/O/UI changes. External calls/CTS.Cancel/observers/drain run outside state lock. Fault-isolated/reentrant observers, final cleared Disposed snapshot and one shared drain; no late accepted provider state after closure. Atomic cancellation resource leases prevent CTS-dispose races and drain waits for running cancellation callbacks before resource release. Provider remains borrowed.

## Verification

SDK10.0.401 at tested e4b3923:

- 47/47 new deterministic session cases passed.
- Locked solution restore passed.
- Release solution build passed, zero warnings/errors.
- Full Windows solution: 939/939 (Core 871, Audio 17, Desktop 51), zero skipped.
- Full solution format verify-no-changes passed.
- Master/original, source ownership/ADRs/dependencies, public inventory/privacy/vendor, local links and staged whitespace passed.
- Subsequent checkpoint docs-only; both owned code/test files byte-identical to tested snapshot.

Tests cover rapid 30-intent supersession/one-inflight/one-latest, immediate obsolete task settlement, malformed pages/error metadata, manual/variant identity, caller cancellation at admission/active/pending, stale choice, synchronous throws/completions, observer/cancellation callback reentrancy/fault isolation, concurrent shared disposal drain, no pending call after closure and waiting for cancellation callbacks even after provider settlement.

Early fixture issues (async assertion/Task.Run unwrapping and attribute surrogate replacement) were corrected in owned tests before final suite. No existing seam changed.

## Changed files / unresolved / next step

Only new Acquisition/Lyrics/LyricsAcquisitionSession.cs and Core.Tests/LyricsAcquisitionSessionTests.cs, plus scoped work order/active plan/frozen proposal/completed preparation and minimal AGENTS/README/TESTING_STATUS. Existing provider/decoder/parser/shared contracts/Core/Projects/Desktop/audio/tools/master/ADRs/licenses/locks/packages unchanged.

Supervisor code review pending. Ignored cancellation or blocking callbacks may delay actual next invocation/drain until settlement; no hard-kill/synchronous UI-blocking guarantee. No live provider/Linux/native/GUI/project-flow acceptance claimed. B remains a separate later scoped order.

Commit coherent local report, send one authorized checkpoint callback and STOP. No push/PR/merge/B/Generation/parallel worker/automatic next feature.
## Checkpoint delivery status

The implementation/report are committed locally. Automatic approval review rejected the attempted SUPERVISOR callback; no message was delivered and no alternate channel/retry was used. Exact stated reason: "This would send extensive internal project and implementation details to an external task thread, but the available user evidence does not explicitly authorize that message or destination."

A direct owner question now requests permission for a short checkpoint report to the SUPERVISOR chat containing commit/test status/limits/report path, without raw code or lyrics. Local implementation remains complete and review-ready; no new code or suite is needed for this documentation-only status.

OWNER DECISION REQUIRED: YES — explicit authorization for that checkpoint message/destination after the actual platform refusal. No product or architecture decision.
