# B1–B3 lyrics editor integration — local preparation
STATUS: CHECKPOINT / STOP FOR SUPERVISOR REVIEW BEFORE PUBLICATION.
TASK phase3-lyrics-editor-integration; SMALL / SEQUENTIAL / one implementer. Sol Medium requested; actual runtime model/reasoning unknown.
BRANCH work/phase3-lyrics-editor-integration. Target for a later authorized PR: main at https://github.com/MasterCorruptor/ultrastar-song-creator.
BASE main/origin/main d5a623104b40445f78abbb0cd409b4573153e9bc; neither ref is changed here.
PAYLOAD HEAD: resolve git HEAD and ignored .agent-local/phase1/continuation/lyrics-editor-integration/review-payload.json, written after the final local commit.
TOTAL PUBLIC DIFF: 25 files changed, 2405 insertions and 17 deletions against main d5a6231 (including this report/plans).
[Active plan](exec-plans/active/phase3-lyrics-editor-integration.md).

## Authority / scope / exact accepted chain
A is already integrated in main through PR21; this payload adds accepted B1+B2+B3, local reference metadata and workflow/status docs. No master/ADR/scope/license or new feature/provider/Generation change.

- B1 same existing editor history: accepted fb6fcb63fec6df97a7a604e3f2fd34bff8f634c9, tested aaedb1db67eda09d44142789ba6c758f132bb1eb,14new/953Windows.
- B2 explicit panel and bounded owned/borrowed lifetime: accepted5d10cfb7148234abcb0de9b9a668d1dcca484061, tested c83152b56a34cc7dc3bcdea06d31b7d0f24f00a1,31component/984Windows; completed closure ab5822d.
- B3 actual production LRCLIB/window/app composition: accepted bc7d17ba4b934dcd8aac3512c3c1f98c37c638e4, tested38e1b5a87a303e836f17eb1098bae8d4c8ce0176,16new/1000Windows (Core871/Audio17/Desktop112), zero skipped; docs closure bb0c5d613f37211110ea714f39ef7a1e8f249c90.
- Local merge9bafcf0a4c6f94ad5b507b74c6134343a5580c10 includes main d5a6231 as parent; no conflicts and entire tree identical to B3 closure bb0c5d6 before this integration report. Original A/B1/B2/B3 branches/commits preserved.

Latest implementation is exactly the tested B3 source/test/project/lock payload. No repeat suite is justified by documentation-only/identical-tree integration. Current local checks cover byte identity, ancestry/clean status/main stability, docs/privacy/vendor/inventory/whitespace.

## Public diff and exact PR preparation
Production diff against main: EditingSession.cs; EditorWindow.cs; EditorApplication.cs; new Lyrics/LyricsPanel.cs; Desktop.csproj (existing local Acquisition reference + friend test access); two Desktop/Desktop.Tests lockfiles (only reviewed local project graph).
Tests: new LyricsEditingSessionTests.cs, LyricsPanelTests.cs, LyricsApplicationLifetimeTests.cs. No external package/version/hash changes; A/provider/Core/Projects/audio/schema/shared contracts/master/ADRs unchanged from main.
Documentation: accepted work orders, proposals, completed plans, workflow policy/README/AGENTS/testing status, and this local integration order/report. Historical pending/refusal passages describe earlier checkpoints; latest acceptance sections govern current status.

Exact final public diffstat/name-status and prepared title/body are in ignored .agent-local/phase1/continuation/lyrics-editor-integration/{diffstat.txt,name-status.txt,pr-title.txt,pr-body.md,review-payload.json}. They describe a future ordinary project PR against main. No push/PR/remote access/merge/main update occurs in this preparation order.

## Evidence and limits
Existing ignored Windows evidence: .agent-local/phase1/continuation/lyrics-app-b3/{restore.log,build.log,tests.log,format.log}. [Accepted B3 report](exec-plans/completed/phase3-lyrics-app-wiring.md) records actual test/source refs and relevant component evidence.
1000 Windows tests and zero-warning Release/locked restore/full format were run at38e1b5a. This is automatic/offline evidence: actual native classic-desktop loop/MainWindow.Show/platform startup/manual interaction, physical playback, live LRCLIB, Linux and full Search→Generate→Edit→Validate→Export remain unverified. The internal callback adapter is not evidence of the native event loop. Cleanup failure remains guarded/static; automatic recovery is not added.

## Later native/manual verification plan — not executed here
1. Schedule a separate Windows verification order with the owner and required native-UI permissions. Build/run the reviewed Desktop entry point in the normal classic desktop lifetime; retain logs/screenshots privately with exact SHA. Use a dedicated test window/session so concurrent computer use does not invalidate click evidence.
2. Begin offline/manual with own synthetic text: confirm blank artist/title/no startup request, separately displayed demo target/current stored text, explicit prepare/apply. Move a note and use mixed undo/redo; check waveform/playhead/transport continuity and cached demo audio where a physical device is available.
3. Close normally and repeat close requests; verify actual window and process exit plus audio-before-lyrics cleanup. Native pending/held-callback scenarios require a separately scoped controlled fixture/instrumented test host; existing headless gates do not establish native coverage. Record scenarios not reproducibly exercised as gaps.
4. If live-provider verification is separately authorized, submit an explicit LRCLIB query, choose candidate/document and test cancel/close while pending when observable. Do not infer deterministic ignored-cancellation coverage from a fast real response. Fetched text/responses remain private; no payload/hardware/account data in public evidence.
5. Scope Linux smoke separately using the same reviewed code/locked dependencies and available runtime; distinguish headless tests, actual GUI/classic loop and physical playback. Fullworkflow/Generation/release acceptance requires later work and is not unlocked by these checks.

NEXT: review exact combined local payload and prepared PR text. Publish or merge only under a later concrete SUPERVISOR order with applicable human approval and fresh preflight. OWNER DECISION REQUIRED: NO new architecture/scope choice for local preparation. No next feature/worker or native UI launch.
