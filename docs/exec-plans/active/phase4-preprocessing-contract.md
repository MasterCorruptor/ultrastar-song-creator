# G1A preprocessing contracts — checkpoint report
STATUS: CHECKPOINT / STOP FOR SUPERVISOR REVIEW.
TASK phase4-preprocessing-contract, MEDIUM / SEQUENTIAL / one implementer.
MODEL USED: Sol Medium requested; actual runtime model/reasoning unknown.
BRANCH work/phase4-preprocessing-contract. BASE0fe274c0dd9c3945a2dba2658c9a1e6eb8824e3d.
API baseline a24ac54. TESTED CODE6e0ef348f531bb4f5bea5ade4fb937692491ce2c. Later HEAD is documentation-only; resolve git log.
[Frozen API/work order](../../PHASE4_PREPROCESSING_CONTRACT_WORK_ORDER.md), [accepted direction/gaps](../../PHASE4_GENERATION_PREPARATION.md).

## Completed / changed files
New Generation library with sealed get-only request/proposal, immutable producer/PCM/bin records and pure typed validation. Request retains exact caller/job/Song/Candidate/Lyrics references; validated equal explicit lyrics may be a distinct object. Native path equivalence and actual selected audio stream resolve consistently without source-label equality; known local-media-v1 retained stream/path must match. No metadata inference, new payload IDs/timestamps or Song edits.

Proposal retains request, source/PCM hash claims, producer revision and bounded contiguous frame bins. DecodedPcmSeconds and exact frame endpoints stay separate from Song/Lyrics clocks; unknown origin remains null. Optional explicit origin is an unverified caller claim. Pure checks validate shape/hash/lexical path/provenance, never real files/hash bytes/containment or decoded backend data.

Files: NEW src/UltraStar.SongCreator.Generation/{UltraStar.SongCreator.Generation.csproj,PreprocessingContracts.cs,PreprocessingValidation.cs,packages.lock.json}; NEW tests/UltraStar.SongCreator.Generation.Tests/{UltraStar.SongCreator.Generation.Tests.csproj,PreprocessingContractTests.cs,packages.lock.json}. Exactly two solution entries. Core+Acquisition project references only; no external production package. New test dependencies/versions/content hashes exactly match Core.Tests. No old project/lock/source/test/tool/master/ADR changes.

## Verification at6e0ef34
SDK10.0.401:69/69 new deterministic no-file/process/network/device contract cases; full Windows1069/1069 (Core871,Audio17,Desktop112,Generation69), zero skipped. Locked restore passed; Release0warnings/0errors; full format verify passed.

Tests exercise actual normalized LocalSongDraft/DownloadedMediaDraft construction, differing source labels, retained stream mismatch/ambiguity/type/unknown/null/default, canonical native path alias/case/URL/relative/NUL/mismatch, explicit manual/provider/instrumental and valid distinct equal lyrics, malformed/null/different lyrics, static sanitized errors, stale captured Song despite same Song.Id after a note edit, offsets/lyrics-source time vs decoded exact frames/unknown origin, producer/hash/PCM/bins/overflow/limits and immutable-builder isolation.

One test fixture comparison initially used separately allocated ImmutableArray wrappers; corrected before final verification to reuse the exact PCM/bins while changing only the claimed origin. No implementation failure is hidden by skipped/reduced tests.

Actual ignored evidence:
.agent-local/phase1/continuation/generation-contract-g1a/restore.log
.agent-local/phase1/continuation/generation-contract-g1a/build.log
.agent-local/phase1/continuation/generation-contract-g1a/tests.log
.agent-local/phase1/continuation/generation-contract-g1a/format.log

213 existing source/test/tool/project/lock/master/ADR files are byte-identical to0fe274c. Solution differs only by two new project entries; new test lock external package values/hashes equal existing Core.Tests; production lock has only local project entries. Original master, existing refs/main/origin16b9c18, privacy/public inventory/vendor/local links/whitespace checked. Later docs-only commit preserves tested source/project/lock/test bytes; no repeated full suite.

## Limits / next / decision
This is in-memory contract acceptance evidence only, not proof of file existence/snapshot/hash/copy/security containment/codec/PCM content, real process cancellation/drain, JSON IPC or Python/backend operation. No staging/FFmpeg/process/worker/model/ASR/GUI/persistence/schema/native/live/Linux/fullworkflow implementation or acceptance is added. Larger G1 and Phase3 gaps remain.

NEXT: SUPERVISOR reviews exact contract/API/source/dependencies/tests/evidence before any G1B work. STOP locally committed; no push/PR/merge/main update/new worker. OWNER DECISION REQUIRED: NO new owner-reserved choice identified. No shared API was changed.
