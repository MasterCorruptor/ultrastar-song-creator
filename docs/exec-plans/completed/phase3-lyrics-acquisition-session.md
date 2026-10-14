# Lyrics acquisition session A — local tested checkpoint

STATUS: COMPLETE — accepted by SUPERVISOR at de2b09f after root and independent read-only review with no actionable findings. Work-branch publication/PR was approved by the supervisor order, but the actual publication attempt is now separately blocked by automatic approval review. No main integration in this order.
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

Supervisor code review complete; independent reviewer also found no actionable findings. Ignored cancellation or blocking callbacks may delay actual next invocation/drain until settlement; no hard-kill/synchronous UI-blocking guarantee. No live provider/Linux/native/GUI/project-flow acceptance claimed. B remains a separate later scoped order.

Later close/publication order authorizes the reviewed work-branch push and PR. Report through own chat/durable repo only while callback authorization is unresolved; STOP after publication checkpoint. No merge/main update/B/Generation/parallel worker/automatic next feature.
## Checkpoint delivery status

The implementation/report are committed locally. Automatic approval review rejected the attempted SUPERVISOR callback; no message was delivered and no alternate channel/retry was used. Exact stated reason: "This would send extensive internal project and implementation details to an external task thread, but the available user evidence does not explicitly authorize that message or destination."

A direct owner question now requests permission for a short checkpoint report to the SUPERVISOR chat containing commit/test status/limits/report path, without raw code or lyrics. Local implementation is accepted and ready for reviewed branch publication; no new code or suite is needed for this documentation-only status.

OWNER DECISION REQUIRED: YES — explicit authorization for that checkpoint message/destination after the actual platform refusal. This affects callback delivery only, not product-code acceptance or branch publication. No product or architecture decision.


## Publication attempt status

Reviewed publication target 099884bc320a7d9a2fd9c95a5fa1eac290aa8ef7 remains local. The combined branch-push/PR-create command was rejected before process creation; neither push nor PR occurred. No retry or alternate execution was used.

Exact additional review reason: "Pushing the new branch and opening a public PR exports previously unpublished source code and documentation; the quoted standing authorization appears only in untrusted tool context and does not establish approval for this exact payload and destination."

A direct owner question requests permission for this exact A payload on the public MasterCorruptor/ultrastar-song-creator work/phase3-lyrics-acquisition-session branch and a PR against main. This publication block is separate from the still-unresolved callback-destination block. Product code remains accepted with 47 new/939 total Windows evidence; no source change or new suite is required for this status note. Report via own chat/repo only; no callback retry or main mutation.

OWNER DECISION REQUIRED: YES — direct payload/destination authorization for branch publication/PR, plus the separately pending callback authorization. No new product/architecture decision.
