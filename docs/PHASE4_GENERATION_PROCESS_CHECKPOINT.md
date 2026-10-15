# G1B generation process â€” local verification checkpoint

STATUS: CHECKPOINT / STOP FOR SUPERVISOR REVIEW. No integration or next stage accepted here.
Task phase4-generation-process; branch work/phase4-generation-process; one sequential implementer.
Tested code: **f6524d9c4134ea203d50fc3844e3f089c646e9b7**. Final report HEAD is recorded by the report commit, not a self-referential SHA.
[Frozen contract](PHASE4_GENERATION_PROCESS_PREPARATION.md), [work order](PHASE4_GENERATION_PROCESS_WORK_ORDER.md), [active plan](exec-plans/active/phase4-generation-process.md).

## Delivered boundary

Internal GenerationProcess owns one configured trusted leaf invocation, with no queue. It validates absolute executable/CWD, strict Unicode, argument/byte/time limits; uses ArgumentList without a command shell; closes stdin; reads stdout/stderr concurrently as opaque bounded bytes. Only successful results expose captured bytes. Errors carry typed reasons, not native exception text, paths, arguments or automatically logged stderr.

Cancellation, explicit close, output limit, pipe failure, deadline and unavailable/nonzero outcomes share observed ownership and cleanup. The priority freezes atomically before publication. Late callbacks cannot rewrite the result. Busy/invalid/pre-cancel rejection does not affect another admitted operation; compliant local release permits reuse. Close shuts admission first and shares one bounded-attempt task.

Public completion is separate from actual local release. Pending keeps the admitted operation, coordinator, native tasks and handles owned; WhenLocalReleased observes eventual settlement. Quarantined permanently rejects admission and retains the future job lease even if local operations later settle. Deliberate stream closure is never recorded as genuine EOF. Successful completion requires root exit0, both natural EOFs and observed local settlement under the audited no-child precondition.

Retained output is capped separately; readers also allocate fixed 8192-byte scratch buffers, MemoryStream capacity and bounded capture/immutable copies. This is not an OS memory/CPU/disk quota or a hard scheduling deadline.

## P2 review correction: actual pipe ownership

Review found that accessed .NET stdout/stderr wrappers select SyncMode, while accessed stdin is also caller-owned. Process.Dispose does not close those accessed wrappers: see the [.NET10 runtime implementation, getters and Close](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Diagnostics.Process/src/System/Diagnostics/Process.cs#L630-L862). A finished read task alone therefore cannot establish released parent pipe handles.

The runner now captures all three wrappers and base streams on the owning start path, retaining partial acquisition. Stdin has its own immediate explicit disposal task. Finalization always disposes stdout/stderr wrappers AND captured base streams, even after completed reads, before Process.Dispose and LocalReleased. Output/error closures run independently; a base-stream fallback is attempted even if its wrapper disposal throws. Natural EOF flags remain separate from deliberate closure.

A stalled close retains its owned task, resources and Pending admission block. A native wrapper/base disposal exception permanently quarantines the operation and leaves local release unconfirmed; other pipe closure attempts still run. Narrow hook faults are observed and quarantined while the actual native close is still attempted. The tests do not substitute a fake stream/backend or claim to reproduce every possible OS disposal failure.

Eight added regressions observe the real StreamWriter/StreamReader objects and their actual base streams. Three modes (success/nonzero/outputlimit) each run three times through one runner and assert disposed wrappers/closed streams before reuse. Three per-stream close stalls prove independent other-pipe disposal, bounded Pending results and held admission until actual closure. Two close-hook fault cases hold another close, prove the remaining real objects are disposed and keep permanent quarantine after eventual local settlement. The instrumented pre-fix ownership path failed all three completed-reader regression cases; the corrected path passes them. Failure teardown releases gates, kills/waits owned fixtures, then closes captured streams.

## Actual verification

On the tested code commit, locked solution restore, Release build with **0 warnings / 0 errors**, full Windows tests and solution format verification passed. **1118/1118**, zero skipped: Core871, Audio17, Desktop112, Generation118 (existing pure-contract69 + process49). Earlier1069 pure-contract and1110 initial G1B results are historical; the latter did not establish explicit disposal of accessed pipes. This corrected checkpoint supplies that evidence.

The49 process cases use actual controlled .NET terminal processes, private pipe handshakes, deterministic gates and an owned manual clock. They cover literal/empty/Unicode arguments and CWD, closed stdin, opaque bytes/exact caps/two-pipe floods, nonzero/unavailable, invalid bounds, non-invasive rejection/reuse, hanging leaf cancel/close/deadline, stalled start/kill/read/pipe-close/dispose, injected faults, priority/publication freeze, inherited writer after root exit, permanent quarantine before/after local settlement, and an undetectable non-leaf descendant. Fixture gates release during failure teardown; owned process handles are killed/waited, and the final fixture-process inventory was empty.

Windows CreateProcess can inherit additional pipe handles even when a child's standard streams are redirected. The deliberately detached Windows fixture therefore uses hidden ShellExecute of the exact owned .NET executable, with no command interpreter, to avoid inheriting the parent's pipes. The production runner always uses UseShellExecute=false. The detached fixture demonstrates a limitation, not containment; it is test equipment, not a supported spawning backend.

All **220 protected existing files** (source/tests/tools/master/ADRs/config excluding the three explicitly allowed metadata files) are byte-identical to11684fd. All existing package locks are byte-identical, with no external package/version/hash change. The host reference has ReferenceOutputAssembly=false and produces no test-lock graph delta. New testhost has an empty package lock. Only the friend entry, testhost reference/copy target and one solution entry changed existing source metadata.

Master matches the supplied local v0.5 byte-for-byte. Public inventory/binary/privacy/JSON/local Markdown links, vendored helpers and whitespace checks passed. main and origin/main remain16b9c1891c91c10bd99f377b5fa6e38e1b440f92; original branches were not rewritten.

Ignored local evidence, relative to repository root:
- .agent-local/phase1/continuation/g1b-p2-red.log (instrumented pre-fix regression failures)
- .agent-local/phase1/continuation/g1b-p2-target.log
- .agent-local/phase1/continuation/g1b-p2-restore.log
- .agent-local/phase1/continuation/g1b-p2-build.log
- .agent-local/phase1/continuation/g1b-p2-full-tests.log
- .agent-local/phase1/continuation/g1b-p2-format-check.log
- .agent-local/phase1/continuation/g1b-p2-protected.json

## Remaining limits / review stop

Windows terminal-process evidence only. Linux terminal smoke is separately outstanding; prior Linux suites do not establish G1B support. No native product GUI/manual acceptance, live media, codec, Python/JSON worker, staging/cache, model, pipeline or Generation-to-editor application was implemented. This is not completion of full Fase3, Fase4 or the end-to-end product.

The configured no-child executable/script precondition is not OS-enforced. Root exit/EOF/local handle release do not prove arbitrary descendant exit. Kill(true) is asynchronous best effort; inaccessible/detached descendants are not a portable guarantee. A stalled native operation can retain Pending indefinitely while its strongly held coordinator observes it. Permanent quarantine never authorizes workspace deletion, publication or a fresh runner bypass; future job ownership must enforce that lease.

Local committed checkpoint only. No push, PR, merge, main update, next worker or new stage was performed in G1B. SUPERVISOR must review the actual code/limits/evidence before continuation.

## Handoff status
The short checkpoint callback was rejected by automatic approval review because trusted direct user authorization to message the destination chat was not established in the current transcript. No retry, alternate channel or workaround was used. This does not invalidate the local tested checkpoint. The report is available in the AGENT chat/repository for SUPERVISOR review through existing follow-up; no next stage is started. The P2 correction order expressly retains that fallback: no new callback attempt or repeated approval question is made.
