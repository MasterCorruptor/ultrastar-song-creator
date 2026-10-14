# LRCLIB response decoder

STATUS: COMPLETE — implementation and required local verification complete; locally accepted by SUPERVISOR. Root and independent static review found no actionable findings for ce5d84f/2c8498f. New explicit owner authorization resolves the historical publication-permission block; publication/integration approved after review of ed171fe.
TASK: phase3-lrclib-response-decoder, MEDIUM / SEQUENTIAL, one worker.
MODEL USED: Sol Medium requested; actual model/reasoning not exposed and therefore unknown.
BRANCH: work/phase3-lrclib-response-decoder.
BASE: 42eb9f6b2a9f645cff07f71acd3753e50de8daf8.
PRE-CODE PROFILE COMMIT: 31e31ac.
IMPLEMENTATION COMMIT: ce5d84fd393dc2c7e08fba45facf4e3d9e679e25.
PUBLICATION-STATUS COMMIT: 2c8498f. Later policy checkpoint: 5dfda9d. These are local checkpoints; resolve current HEAD with git log.
[Profile](../../LRCLIB_RESPONSE_PROFILE.md), [work order](../../PHASE3_LRCLIB_RESPONSE_WORK_ORDER.md).

## Completed

Internal pure LrclibJson.Parse with bounded complete-body JSON/Unicode/schema validation, plain and reviewed LRC variants, explicit status/capabilities, typed diagnostics and sanitized payload errors. All records and aggregate budgets validated before requested limit. Original query/provider order/UTC receipt preserved.

Own synthetic tests cover accepted boundaries and rejected excess, types/aliases/Unicode, duplicate escaped names/IDs, cancellation, independent variants/status/diagnostics and full-batch truncation safety.

## Verification

2026-10-14, SDK 10.0.401:

- dotnet restore UltraStar.SongCreator.slnx --locked-mode: passed.
- dotnet build UltraStar.SongCreator.slnx -c Release --no-restore: passed, zero warnings/errors.
- Own LrclibJsonTests: 49/49 passed.
- dotnet test UltraStar.SongCreator.slnx -c Release --no-build --no-restore: 822/822 Windows tests passed (Core 754, Audio 17, Desktop 51), zero skipped.
- dotnet format UltraStar.SongCreator.slnx --no-restore --verify-no-changes: passed.
- Protected-source comparison: 280 existing files outside authorized documents byte-identical to integrated base; master also byte-identical to supplied original.
- Local Markdown links, JSON/public inventory, vendor integrity, host/hardware privacy and staged whitespace: passed.

## Files / modules changed

New Acquisition/Lyrics/LrclibJson.cs and Core.Tests/LrclibJsonTests.cs. Central LRCLIB profile/work order/this active plan and AGENTS status. Pre-code documents corrected historical main references in PHASE3_LRCLIB_ADAPTER_PROPOSAL.md and LYRICS_COMBINED_LOCAL_REVIEW.md.

No packages, locks, shared diagnostic seam, LrcParser, Core, Song, Projects/v3, master, ADRs or license changed.

## Unresolved / next step

Local supervisor code review and acceptance complete; publication/integration authorized; actual delivery verified against Git/PR history in the publication checkpoint. Transport, HTTP/gating/client/provider/GUI integration and live acquisition are outside this checkpoint. No Linux/native/network result claimed.

Implementation committed locally. Automatic approval review rejected the branch push: it considered earlier approval limited to another reviewed package and did not authorize publication of this new source-code payload. No push occurred and no workaround was attempted. This was the historical stop. New explicit owner authorization on 2026-10-14 now covers this decoder and later supervisor-reviewed project pushes to the same public repository.

Next step: retain the accepted local decoder checkpoint. Explicit authorization is now recorded in SUPERVISOR_POLICY.md; the concrete publication/integration order is now approved. A separately ordered [local transport preparation](phase3-lrclib-transport-preparation-local.md) completed the accepted HTTP contract without implementing it. HTTP still requires a separate scoped order.

OWNER DECISION REQUIRED: NO — publication permission is now explicitly granted. Document review and concrete publication/integration authorization are complete; new actual approval rejections must still be respected.
