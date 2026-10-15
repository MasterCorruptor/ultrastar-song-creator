# G1B generation process — local verification checkpoint

STATUS: CHECKPOINT / STOP FOR SUPERVISOR REVIEW. No integration or next stage accepted here.
Task phase4-generation-process; branch work/phase4-generation-process; one sequential implementer.
Tested code: **9780cb30a54ef26a6328ac689f8741083784e96e**. Final report HEAD is recorded by the report commit, not a self-referential SHA.
[Frozen contract](PHASE4_GENERATION_PROCESS_PREPARATION.md), [work order](PHASE4_GENERATION_PROCESS_WORK_ORDER.md), [active plan](exec-plans/active/phase4-generation-process.md).

## Delivered boundary

Internal GenerationProcess owns one configured trusted leaf invocation, with no queue. It validates absolute executable/CWD, strict Unicode, argument/byte/time limits; uses ArgumentList without a command shell; closes stdin; reads stdout/stderr concurrently as opaque bounded bytes. Only successful results expose captured bytes. Errors carry typed reasons, not native exception text, paths, arguments or automatically logged stderr.

Cancellation, explicit close, output limit, pipe failure, deadline and unavailable/nonzero outcomes share observed ownership and cleanup. The priority freezes atomically before publication. Late callbacks cannot rewrite the result. Busy/invalid/pre-cancel rejection does not affect another admitted operation; compliant local release permits reuse. Close shuts admission first and shares one bounded-attempt task.

Public completion is separate from actual local release. Pending keeps the admitted operation, coordinator, native tasks and handles owned; WhenLocalReleased observes eventual settlement. Quarantined permanently rejects admission and retains the future job lease even if local operations later settle. Deliberate stream closure is never recorded as genuine EOF. Successful completion requires root exit0, both natural EOFs and observed local settlement under the audited no-child precondition.

Retained output is capped separately; readers also allocate fixed 8192-byte scratch buffers, MemoryStream capacity and bounded capture/immutable copies. This is not an OS memory/CPU/disk quota or a hard scheduling deadline.

## Actual verification

On the tested code commit, locked solution restore, Release build with **0 warnings / 0 errors**, full Windows tests and solution format verification passed. **1110/1110**, zero skipped: Core871, Audio17, Desktop112, Generation110 (existing pure-contract69 + new process41). The earlier1069 result is historical, not the evidence for this checkpoint.

The41 new cases use actual controlled .NET terminal processes, private pipe handshakes, deterministic gates and an owned manual clock. They cover literal/empty/Unicode arguments and CWD, closed stdin, opaque bytes/exact caps/two-pipe floods, nonzero/unavailable, invalid bounds, non-invasive rejection/reuse, hanging leaf cancel/close/deadline, stalled start/kill/read/pipe-close/dispose, injected faults, priority/publication freeze, inherited writer after root exit, permanent quarantine before/after local settlement, and an undetectable non-leaf descendant. Fixture gates release during failure teardown; owned process handles are killed/waited, and the final fixture-process inventory was empty.

Windows CreateProcess can inherit additional pipe handles even when a child's standard streams are redirected. The deliberately detached Windows fixture therefore uses hidden ShellExecute of the exact owned .NET executable, with no command interpreter, to avoid inheriting the parent's pipes. The production runner always uses UseShellExecute=false. The detached fixture demonstrates a limitation, not containment; it is test equipment, not a supported spawning backend.

All **220 protected existing files** (source/tests/tools/master/ADRs/config excluding the three explicitly allowed metadata files) are byte-identical to11684fd. All existing package locks are byte-identical, with no external package/version/hash change. The host reference has ReferenceOutputAssembly=false and produces no test-lock graph delta. New testhost has an empty package lock. Only the friend entry, testhost reference/copy target and one solution entry changed existing source metadata.

Master matches the supplied local v0.5 byte-for-byte. Public inventory/binary/privacy/JSON/local Markdown links, vendored helpers and whitespace checks passed. main and origin/main remain16b9c1891c91c10bd99f377b5fa6e38e1b440f92; original branches were not rewritten.

Ignored local evidence, relative to repository root:
- .agent-local/phase1/continuation/g1b-target.log
- .agent-local/phase1/continuation/g1b-restore.log
- .agent-local/phase1/continuation/g1b-build.log
- .agent-local/phase1/continuation/g1b-full-tests.log
- .agent-local/phase1/continuation/g1b-format-check.log
- .agent-local/phase1/continuation/g1b-protected.json

## Remaining limits / review stop

Windows terminal-process evidence only. Linux terminal smoke is separately outstanding; prior Linux suites do not establish G1B support. No native product GUI/manual acceptance, live media, codec, Python/JSON worker, staging/cache, model, pipeline or Generation-to-editor application was implemented. This is not completion of full Fase3, Fase4 or the end-to-end product.

The configured no-child executable/script precondition is not OS-enforced. Root exit/EOF/local handle release do not prove arbitrary descendant exit. Kill(true) is asynchronous best effort; inaccessible/detached descendants are not a portable guarantee. A stalled native operation can retain Pending indefinitely while its strongly held coordinator observes it. Permanent quarantine never authorizes workspace deletion, publication or a fresh runner bypass; future job ownership must enforce that lease.

Local committed checkpoint only. No push, PR, merge, main update, next worker or new stage was performed in G1B. SUPERVISOR must review the actual code/limits/evidence before continuation.

## Handoff status
The short checkpoint callback was rejected by automatic approval review because trusted direct user authorization to message the destination chat was not established in the current transcript. No retry, alternate channel or workaround was used. This does not invalidate the local tested checkpoint. The report is available in the AGENT chat/repository for SUPERVISOR review through existing follow-up; no next stage is started.
