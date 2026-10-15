# G1B generation process — implementation baseline
STATUS: IMPLEMENTING. MEDIUM/LARGE / SEQUENTIAL / one implementer; Sol High requested, actual runtime model/reasoning unknown.
BASE11684fd0cf2fbb010d4a938c092df091c9f97e0c; work/phase4-generation-process. Main16b9c18 and original refs preserved.
[Frozen semantics](PHASE4_GENERATION_PROCESS_PREPARATION.md) governs leaf-only usage, exact reason priority, permanent quarantine and independent local-release ownership.

## Concrete internal API / implementation boundaries
GenerationProcess(TimeProvider? clock=null,TimeSpan? drainGrace=null), RunAsync(GenerationProcessOptions,CancellationToken caller=default):Task<GenerationProcessResult>, CloseAsync():one reusable Task<GenerationProcessCleanup>.
Options record: Executable, ImmutableArray<string> Arguments, WorkingDirectory; init Timeout=30s, StdoutLimit=1MiB, StderrLimit=64KiB. Maxima10min/32MiB/64KiB, <=256args and64KiB combined UTF8 text. Grace defaults2s and must be positive<=10s.
Result: Reason, ExitCode?, immutable stdout/stderr on Success only, Cleanup reference. Reasons Success/Cancelled/CloseRequested/TimedOut/Unavailable/OutputLimit/PipeFailure/NonZeroExit/DrainIncomplete/Busy/Closed/InvalidInvocation.
Cleanup.State is LocalReleased/Pending/Quarantined; WhenLocalReleased nonfaulting Task tracks actual owned operations/handles independently of permanent quarantine. No IAsyncDisposable or automatic recovery.
Rejection order: Closed/quarantined, precancel, invalid invocation, Busy; none changes another admitted invocation. Valid calls have one slot/no queue. Atomic publication uses observed caller cancel > explicit close > limit > pipe failure > deadline > unavailable/nonzero > success; DrainIncomplete only replaces otherwise-successful incomplete completion. Late cancellation cannot rewrite a result.
All native start/kill/read/close/dispose operations off UI/outside locks, with small owned test hooks only before operations; hooks delay/fault the same owner path, never supply a fake process backend. Reader EOF is recorded only on natural read0 before deliberate abortion. Bytes stay opaque.
Admission timeout includes queued start; root-exit EOF grace is clipped by execution deadline, cleanup has its own finite grace. Public result may return Pending/Quarantined while a strongly held observed tracker finishes local tasks. No unbounded finally, lost tasks, false EOF or descendant guarantee. Quarantined never reopens admission/file ownership even after local release.
Caller lifetime owns pending job files; this runner never deletes them. Direct audited no-child executable/script is a precondition, not OS containment.

## Exclusive files / verification / stop
NEW Generation/GenerationProcess.cs (small internal types/hooks in same file), NEW Generation.Tests/GenerationProcessTests.cs, NEW tests/Generation.TestHost/{Generation.TestHost.csproj,Program.cs,packages.lock.json}. Only Generation.csproj friend access, Generation.Tests.csproj hostreference/copy target + actual local test lock graph, one solution host entry. No external versions/hash changes or other project/lock/source edits.

Controlled terminal fixtures/private pipe handshakes and owned child handles; gates/manual TimeProvider, no sleeps. Cover literal arguments/CWD/closed stdin/opaque bytes/exact caps/floods/nonzero/hang; cancellation/close/limit/pipe/deadline priority and atomic late publication; Busy/invalid/reuse/repeated close; inherited writer/root-before-EOF/eventual release/permanent quarantine; detached descendant limitation; start/kill/read/dispose stalls/fault observation with same production coordinator and safe fixture cleanup.

Targeted tests, coherent code commit, locked restore/Release0warnings/full Windows/format, original master/ADRs/protected bytes/refs/locks/privacy/vendor/inventory/docs/whitespace. Actual tested SHA/final HEAD/ignored evidence paths; STOP FOR SUPERVISOR REVIEW. No pipeline/staging/codec/Python/JSON/model/GUI/schema/cache/network/native app/remote/main update/next worker.
