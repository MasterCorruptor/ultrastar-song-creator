# Generation foundation - local integration preparation

STATUS: PREPARED LOCALLY / STOP FOR EXACT-HEAD SUPERVISOR REVIEW.
Task phase4-generation-foundation-integration; SMALL/SEQUENTIAL; one implementer; Sol Medium requested, actual runtime settings not asserted.
Branch: work/phase4-generation-foundation-integration. No remote/main change or next implementation stage.

## Exact anchors

| Anchor | Exact commit |
| --- | --- |
| main / origin/main / intended PR base | 16b9c1891c91c10bd99f377b5fa6e38e1b440f92 |
| G1B accepted review HEAD | 7da57a6765f251ccda8a0b2df817bcfa0625c0cd |
| Actual tested code / Windows verification | f6524d9c4134ea203d50fc3844e3f089c646e9b7 |
| G1B docs closure / integration payload anchor | cbb49536d562f69b723c5892c5c47eff66a30706 |
| Payload anchor tree | 62f966f78eb877092b886d9253a73128ce3f2842 |

Final review HEAD is the Git commit containing this report and the local PR draft; resolve git rev-parse HEAD. It differs from the exact payload anchor only by docs, and is not a newly tested code SHA. Exact final HEAD, inventory/diff and ref checks are recorded in ignored review-payload.json after commit, avoiding a self-referential SHA.

## Accepted scope and lineage

PR22 local docs closure e63d914fcea76beaf86e229690f3e05c8b4c1427 records already integrated lyrics composition and native/live/Linux gaps. PR22 code is already in main; no new lyrics code is introduced here.

G1 readiness/gap direction0fe274c0dd9c3945a2dba2658c9a1e6eb8824e3d is accepted; its [larger proposed flow](PHASE4_GENERATION_PREPARATION.md) remains unimplemented apart from the two smaller accepted subsets:
- G1A accepted23b8591b913a6d1387c8c0bc3f896f34cdf408da / tested6e0ef348f531bb4f5bea5ade4fb937692491ce2c; docs closure5b7ee1eeffaaecc0b0f1754165cc6ee04bc27c4e. Immutable transient preprocessing request/proposal types and pure validation:69 cases,1069 Windows at that historical checkpoint. [Completed report](exec-plans/completed/phase4-preprocessing-contract.md).
- G1B preparation11684fd0cf2fbb010d4a938c092df091c9f97e0c; implementation and P2 explicit pipe-ownership correction accepted at the anchors above.49 process cases include eight P2 regressions. [Acceptance](PHASE4_GENERATION_PROCESS_CHECKPOINT.md), [completed implementation](exec-plans/completed/phase4-generation-process.md), [completed preparation](exec-plans/completed/phase4-generation-process-preparation.md).

The Generation library references existing Core/Acquisition normalized types without changing Song/history/persistence. Pure contracts validate explicit immutable input, provenance and decoded frame/time/shape claims; they do not inspect media bytes or generate notes.

The internal runner owns one configured trusted leaf process, literal arguments, closed stdin and concurrent bounded opaque output. Genuine EOF, root exit and explicit wrapper/base-stream disposal are distinct requirements. Outcomes freeze; Pending retains unsettled tasks/handles; Quarantined permanently blocks admission and future workspace release, even after local settlement. This is not arbitrary-descendant containment, and the runner never deletes job directories.

## Reused verification by exact byte identity

At f6524d9c4134ea203d50fc3844e3f089c646e9b7: locked restore, Release0warnings/0errors, full solution format and **1118/1118 Windows**, zero skipped: Core871 + Audio17 + Desktop112 + Generation118=G1A69+G1B49. No suite/backend probe was rerun on docs closure/integration HEAD.

**230 files** are byte-identical to that tested commit, covering src/tests/tools, solution/config, master/ADRs and LICENSE. This preparation adds no source/test/build/lock path. All **13 locks already present in main are byte-identical**. New production lock contains only local projects;13 external entries in Generation.Tests match existing Core.Tests versions/content hashes exactly; the new terminal host lock is empty. No new external production dependency, distributed codec/runtime/model/binary, vendor change or licence decision. MIT/ADR0002 is unchanged; later distribution still needs artifact-specific notices/source/licence gates.

Master matches supplied local v0.5. Existing private tools check public inventory/privacy/binaries/JSON, Markdown targets, vendor integrity, AGENTS length and whitespace. Main/origin/main and every pre-existing ref except the new integration branch remain preserved.

Ignored evidence under .agent-local/phase1/continuation/generation-foundation-integration/:
- verified-source-identity.json:230 hashes and old-lock/dependency checks.
- refs-before-integration.json and review-payload.json:exact anchors/ref/final checks.
- name-status.txt, diffstat.txt and candidate.diff:complete committed diff vs main.
Actual execution logs remain .agent-local/phase1/continuation/g1b-p2-{target,restore,build,full-tests,format-check}.log, belonging to testedf6524d9. The instrumented pre-fix red log is historical regression evidence.

## Complete changed-path inventory vs main

30 paths, including this report and PR draft; A/M/D mean added/modified/deleted. No binary payload.

    M	AGENTS.md
    M	README.md
    M	UltraStar.SongCreator.slnx
    M	docs/PHASE3_LYRICS_EDITOR_INTEGRATION.md
    A	docs/PHASE4_GENERATION_FOUNDATION_INTEGRATION.md
    A	docs/PHASE4_GENERATION_FOUNDATION_PR_DRAFT.md
    A	docs/PHASE4_GENERATION_PREPARATION.md
    A	docs/PHASE4_GENERATION_PROCESS_CHECKPOINT.md
    A	docs/PHASE4_GENERATION_PROCESS_PREPARATION.md
    A	docs/PHASE4_GENERATION_PROCESS_WORK_ORDER.md
    A	docs/PHASE4_PREPROCESSING_CONTRACT_WORK_ORDER.md
    M	docs/TESTING_STATUS.md
    D	docs/exec-plans/active/phase3-lyrics-editor-integration.md
    A	docs/exec-plans/completed/phase3-lyrics-editor-integration.md
    A	docs/exec-plans/completed/phase4-generation-preparation.md
    A	docs/exec-plans/completed/phase4-generation-process-preparation.md
    A	docs/exec-plans/completed/phase4-generation-process.md
    A	docs/exec-plans/completed/phase4-preprocessing-contract.md
    A	src/UltraStar.SongCreator.Generation/GenerationProcess.cs
    A	src/UltraStar.SongCreator.Generation/PreprocessingContracts.cs
    A	src/UltraStar.SongCreator.Generation/PreprocessingValidation.cs
    A	src/UltraStar.SongCreator.Generation/UltraStar.SongCreator.Generation.csproj
    A	src/UltraStar.SongCreator.Generation/packages.lock.json
    A	tests/Generation.TestHost/Generation.TestHost.csproj
    A	tests/Generation.TestHost/Program.cs
    A	tests/Generation.TestHost/packages.lock.json
    A	tests/UltraStar.SongCreator.Generation.Tests/GenerationProcessTests.cs
    A	tests/UltraStar.SongCreator.Generation.Tests/PreprocessingContractTests.cs
    A	tests/UltraStar.SongCreator.Generation.Tests/UltraStar.SongCreator.Generation.Tests.csproj
    A	tests/UltraStar.SongCreator.Generation.Tests/packages.lock.json

## Review stop and limits

[Local PR title/body](PHASE4_GENERATION_FOUNDATION_PR_DRAFT.md) describes this scope. No PR exists for this candidate; no push, PR, merge or main update is performed here.

Linux terminal smoke is outstanding. Leaf-only is a usage precondition; native stalls may retain Pending indefinitely, and quarantine never permits automatic workspace deletion or a fresh-runner bypass. No G1 staging/copy/hash verification/codec/JSON/Python worker, analysis models/ASR/pitch/alignment/BPM generation, GUI binding, native/live/fullworkflow, installer/release or full Phase3/Phase4 acceptance is added.

NEXT: exact-head SUPERVISOR review, then a separate concrete remote order if accepted. No next implementation block. OWNER DECISION REQUIRED: NO new technology/licence/scope choice for local preparation.

The prior short callback was rejected by automatic approval review. Only this repository and own final report are used for existing SUPERVISOR follow-up; no callback retry, alternate channel or repeated approval question.
