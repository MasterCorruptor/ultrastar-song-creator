# Lyrics panel component B2
STATUS: IMPLEMENTING. MEDIUM / SEQUENTIAL, one worker. Sol High requested; actual model/reasoning unknown.
Base: accepted B1 fb6fcb63fec6df97a7a604e3f2fd34bff8f634c9, source/tests identical to tested aaedb1d (953 Windows cases). Local branch work/phase3-lyrics-panel-component.

## Scope and ownership
Only new Desktop/Lyrics/LyricsPanel.cs and Desktop.Tests/LyricsPanelTests.cs; Desktop.csproj adds local Acquisition reference and Desktop.Tests friend access. Only Desktop and Desktop.Tests lockfiles may change their local project graph; external packages/hashes stay unchanged. Existing EditingSession/window/application/playback, A/provider/Core/Projects/audio/schema/master/ADRs/licenses and other locks stay unchanged.

Build a borrowed functional view with blank explicit artist/title, search/cancel, bounded candidate/variant/diagnostic status, manual preparation, exact read-only preview and explicit Apply through accepted B1 on current song/history. No automatic selection/search/retry, metadata overwrite, alignment, link fetching or hidden fallback.

## Internal API baseline
Public LyricsPanel(EditingSession, LyricsAcquisitionSession) borrows both resources. Controls and commands require UI thread; Detach disables/unsubscribes/invalidate without disposing borrowed resources. Internal constructor accepts LyricsPanelLifetime. Internal LyricsPanelLifetime(session, ownedProvider=null) borrows by default; explicitly supplying a disposable provider transfers ownership of BOTH provider and its matching session. Composition caller must supply the actual provider used by the session.

Lifetime.Enqueue(generation, action) admits at most one synchronous action plus latest pending intent. Action returns a task; search outcome is observed separately, never holds the admission queue. Completion receipts carry captured generation/revision and static fault indication; observers are isolated. No external callbacks under locks. CloseAsync returns one reusable Task<bool>; false means static cleanup failure. It closes admission/discards pending, starts background cleanup before session DisposeAsync, observes admitted jobs/searches, waits owned A drain, then disposes owned provider. Borrowed close never disposes session/provider.

UI actions invalidate eligibility immediately. Receipt renders require latest intent/input generation, settled admission and exact current revision/query; externally changed or stale snapshots cannot Apply. Frozen Core prepared payload reused at synchronous UI apply point. Stored lyrics/target shown separately from preview.

## Acceptance and stop
Deterministic offline/headless tests: no startup request/autopick; explicit queries/cancel/no-results/static errors/diagnostics/variants/instrumental/manual; stale renders/selection/edit invalidation; bounded coalescing/held cancellation callback without blocking UI; same current history/undo/redo/no-op; detach ignores late work; owned close waits admitted work/A drain, repeated close identity, borrowed ownership, observed faults. No native GUI or live provider acceptance.

Targeted tests, coherent code commit, locked restore/Release zero warnings/full Windows suite/format and protected source/master/ADRs/lock/private inventory/document/whitespace checks. Stop committed local checkpoint for SUPERVISOR review. B3 window/app/startup wiring, native/manual/live/Linux verification, B1/B2 publication/merge/main update are outside this order.
