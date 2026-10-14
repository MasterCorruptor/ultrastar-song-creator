# Lyrics editor apply B1 — work order

STATUS: IN PROGRESS. TASK phase3-lyrics-editor-apply, SMALL / SEQUENTIAL, one worker.
MODEL USED: Sol Medium requested; actual model/reasoning unknown.
BRANCH: work/phase3-lyrics-editor-apply.
BASE: B preparation301b8853f03de018a0302bdf15c1d95673ce6a57. A publication source440ea86 and B preparation branch unchanged; main6ddb743 unchanged.
[Split B contract](../../PHASE3_LYRICS_PANEL_PROPOSAL.md).

Exclusive source: Desktop/EditingSession.cs. New tests: Desktop.Tests/LyricsEditingSessionTests.cs. No panel/window/application/playback/A/provider/Core/Projects/csproj/lock/dependency/master/ADR/license changes.

Precise B1 contract: public bool ApplySelectedLyrics(Core.SelectedLyrics value) uses existing private SongHistory.Execute(new SetSelectedLyrics(value)) against current song. True only actual change. Equal validated payload false preserving redo; expected malformed payload rejection false/static local text preserving current/history. Null is explicitly rejected with static text/false (NOT a clear command). Notify existing Changed after success/no-op/rejection. UI-thread caller ownership unchanged. Generic successful Undo/Redo wording “Endringen er angret/gjort om”; Move behavior/messages unchanged.

Synthetic tests: current-note edits preserved, exact mixed-history undo/redo, distinct equal payload no-op retaining redo/new change clears redo, malformed/null rejection preserves current/history, note/metadata/media/offset/text/timing/provenance preservation and Changed/message behavior.

Targeted tests → local code checkpoint → locked restore/Release/full Windows/format → source/master/deps/privacy/docs/whitespace → local tested report/commit. STOP for root review; no B2/push/PR/merge/main update/live/native/worker. Existing external-delivery refusals remain binding; own chat/repo only.
