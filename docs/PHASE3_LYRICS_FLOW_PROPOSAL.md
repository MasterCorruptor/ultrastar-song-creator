# Lyrics acquisition flow — proposed next functional slice

STATUS: CONTRACT FROZEN under SUPERVISOR acceptance of the A/B sequence at 10994cf and the required bounded-state/lifetime clarifications below. A session/API is now implemented locally at e4b3923 and awaits code review; B/GUI remains unimplemented. BASE main6ddb743e7d06853eb194bb83505ea62fe489a7b7 (PR20).
[Plan](exec-plans/completed/phase3-lyrics-flow-preparation.md), [master](PROJECT_MASTER.md) §6/Fase 3–6, [transport](LRCLIB_TRANSPORT_PROFILE.md), [lyrics contract](PHASE3_LYRICS_CONTRACT.md), [selected payload/project v3](PROJECT_FORMAT_V3.md).

## Actual implemented seams and gaps

| Area | Actual code / behavior | Missing functional connection |
| --- | --- | --- |
| Search | Acquisition.Lyrics ILyricsSearchProvider/LrclibProvider: original artist/title query, bounded page, typed errors and diagnostics | No application search state or artist/title controls |
| Choice/manual | LyricsSelections.Select checks page/provider/candidate/document identity; ManualLyrics.Create makes validated plain text; SelectedLyricsProjection produces immutable Core payload | No visible candidates/variants/preview or manual-paste action |
| Editing | Core.SetSelectedLyrics and SongHistory support validated replacement/explicit clear and no-op/undo/redo | Desktop.EditingSession privately owns history but exposes only note select/move/undo/redo; no lyrics apply method |
| Persistence | ProjectStore/ProjectJson writer v3 persists Song.SelectedLyrics; readers v1/v2/v3 exist | No Desktop project open/save UI or current project path/lifecycle |
| Composition | EditorApplication constructs EditingSession(DemoSong.Create()); EditorWindow/EditorPlaybackSession own demo controls/audio and explicit disposal | Desktop references Core/Audio only, no Acquisition/Projects reference or acquisition owner |
| Real media | Acquisition has local inspection, recording/media search/selection/download APIs | No GUI file/source choice; Audio currently supplies PcmClip/playback/waveform and DemoPcm, no file-to-PCM loader in this module; EditorPlaybackSession always builds DemoPcm |

Transport/decoder and selected payload are integrated and tested; the missing work is coordination/presentation, not another provider/parser/schema. No ranking, calibrated confidence, ASR or automatic notes exist in this slice. Generation (§7/Fase 4) and full editor/integration remain later work.

## Recommended sequence

**A — first, small headless acquisition session.** Implement a presentation-neutral coordinator in Acquisition.Lyrics using the existing Core dependency and Core.Tests reference. It owns transient search/selection state only; no SongHistory, Song mutation, file I/O, UI toolkit or provider registry. This freezes concurrency/lifetime/selection semantics before Desktop shares them. Existing provider/decoder/Core/Projects stay unchanged.

**B — next, minimal functional lyrics panel.** After A is reviewed, add a Desktop panel and narrow EditingSession apply seam. The panel renders the frozen state, shows an explicit selected variant and manual preview, and applies it to the current song through its existing history. Native acceptance is a separate real-user test after headless acceptance. This is functional wiring, not GUI polishing.

A can ship independently and does not pretend to expose user-facing lyrics acquisition. B depends on A's accepted public contract; do not parallelize these slices against an unstable contract. Real media/project GUI and Generation are separately scoped subsequent work, not prerequisites for A and not silently included in B.

## Frozen A behavior — implemented API baseline

New LyricsAcquisitionSession(ILyricsSearchProvider provider), IAsyncDisposable, presentation-neutral and provider-borrowing. The [A work order](PHASE3_LYRICS_SESSION_WORK_ORDER.md) spells the implemented action/type signatures inside the same owned file, preserving this frozen behavior. Provider ownership stays with composition; no fake production default, plugin framework or new transport injection.

Expose one immutable Current snapshot and Changed signal with monotonic revision. Snapshot includes status Idle/Searching/Results/NoResults/Failed/Cancelled/ManualReady/Disposed, bounded original query, eligible validated page, explicit preview/prepared Core.SelectedLyrics and bounded typed error. No Song, history or file I/O.

Implemented A actions: SearchAsync(query, callerToken), Cancel(), Reset(), Select(revision, providerId, candidateId, documentId), SetManualText(text), DisposeAsync(). SearchAsync returns an observed task with a bounded per-intent outcome (completed, rejected/failed, canceled, superseded or closed); expected validation/provider/cancellation outcomes update typed state, not raw exceptions. Superseded tasks settle deterministically even if their old provider call has not settled. Misuse after closure throws ObjectDisposedException.

### Bounded work and latest intent

At most **one actual provider invocation in flight and one pending latest search intent**. A new valid search invalidates/cancels the active generation and replaces the pending intent. Complete an obsolete pending intent as superseded immediately. Do not create unlimited provider tasks, cancellation sources or gate waiters from rapid Search calls. Invoke only the newest eligible pending intent after the active invocation settles; no retries.

Searching represents either current active search or latest pending intent, with its validated original query and no eligible page/preview/error. Validate accepted Page plus original query and provider identity; malformed/mismatched returned batches map to sanitized InvalidResponse/ResponseTooLarge. Empty valid success becomes NoResults; nonempty success becomes Results. Never select first candidate automatically or invent confidence.

All active completions are observed. A canceled/superseded/closed generation's success or error is discarded and cannot replace newer state, including manual choice. A borrowed provider ignoring cancellation can delay the next actual search and disposal until settlement; it cannot block manual/reset/cancel actions. Production LRCLIB's 30-second deadline remains its own transport contract, not a coordinator hard-kill guarantee.

### Explicit state/action outcomes

- Idle: no query/page/preview/error. Reset invalidates/cancels active generation, clears pending intent and returns Idle.
- Searching: query present, page/preview absent. Cancel while Searching clears pending intent, invalidates/cancels active work and becomes Cancelled; page/preview absent and unselectable. Cancel in other live states is a no-op.
- Results: current validated page can be selected. Select checks revision first. A stale revision is rejected **without any mutation**, preserving the newer state and its valid preview/error. Current invalid identity/variant choice clears applicable preview, keeps the validated page/Results for a later selection and records typed validation. A valid choice clears that choice error.
- Available needs explicit validated document through LyricsSelections.Select; Instrumental needs explicit null-document choice. Missing/Unsupported cannot be applied. Reported Word flag never invents Word timing.
- NoResults/Failed/Cancelled have no eligible page or preview; Select cannot use old pages. Expected provider errors carry LyricsSearchError and optional status/retry. Invalid input/choice carries LyricsValidationError. Invalid search input supersedes pending work and leaves Failed without retaining unbounded invalid query data.
- SetManualText invalidates/cancels active work, clears pending intent/page/query and validates once through ManualLyrics.Create. Valid text becomes ManualReady with one frozen choice/prepared payload. Invalid input becomes Failed with typed validation and no applicable preview. Manual text remains usable despite an ignored cancellation or unavailable provider.
- B calls Reset when query/manual edits invalidate a preview. Reset/Cancel/manual/search never change Song.SelectedLyrics. Apply reuses the prepared immutable payload, not regenerated manual IDs/timestamps.
- Disposed is final: query/page/preview/error cleared. New public actions throw ObjectDisposedException; repeated/concurrent DisposeAsync returns the same drain task rather than starting another drain.

### Synchronization, notifications and disposal

Use short serialized state synchronization only. Capture/mutate internal state under lock; invoke providers, await tasks, execute CTS.Cancel/cancellation callbacks, notify observers and wait for disposal **outside** that lock. Dispose a per-operation cancellation source only after its corresponding invocation settles. All superseded/pending returned tasks must have deterministic settlement.

Changed notifications may be reordered across threads. Revisions never decrease; UI dispatches to Dispatcher.UIThread and reads latest immutable Current, discarding obsolete queued rendering. Ordinary subscriber faults are isolated per subscriber: do not corrupt state, classify them as provider failures, skip other subscribers/cleanup or log raw exception/provider data. No new diagnostic framework.

DisposeAsync atomically closes admission, invalidates/cancels active work, clears pending intent, sets the final cleared Disposed snapshot and sends at most one terminal Changed notification outside the lock. Late provider completions produce no notifications/state mutations. Concurrent disposal reuses one drain task, observing active settlement and releasing owned cancellation sources once. Composition disposes its owned provider **after** this drain. No synchronous UI wait; a provider ignoring cancellation delays drain until settlement.

Unexpected provider faults expose only a bounded static unexpected-failure indication; no raw Message/InnerException, payload, URLs or headers reach presentation/logs. Candidate/selection text stays within existing page/document budgets.

## Proposed B binding and ownership

Artist/title start as explicit editable user input; do not automatically search synthetic DemoSong metadata. Search and cancel controls, bounded candidate list with artist/title/duration/status, explicit variant selector, plain/line preview, typed warnings/error/no-results/retry display, manual paste and Apply. Unsupported formats/failed variants stay visible; Apply is disabled without a validated explicit preview. No search on startup and no hidden retries/fallback request.

Bind one panel to one EditingSession. Add a narrow ApplySelectedLyrics(Core.SelectedLyrics value) method that executes SetSelectedLyrics on its existing private SongHistory and raises Changed. Use current song at the synchronous UI-thread application point, preserving note changes made while searching. No second history and no automatic apply. Equal-payload no-op preserves redo; undo/redo restores recorded snapshots. Replace move-only Undo/Redo messages with truthful generic change wording once lyrics commands are available.

A future project/session replacement must cancel/reset or dispose the old acquisition binding before reusing UI; an old result must never apply to another song/session. Ordinary note editing in the same song does not auto-invalidate the chosen lyrics preview. Show current target song and make explicit that the present application is a demo-song prototype; query metadata does not overwrite Song.Metadata or prove an audio/recording match.

EditorApplication owns one production LrclibProvider and session when B is authorized. Window closure unsubscribes events, asynchronously drains the acquisition session, then disposes provider; playback retains its established UI-thread owner/disposal. Headless tests inject borrowed terminal providers and existing audio fixtures, never live network/native devices.

Desktop's new **local ProjectReference to Acquisition** (and resulting project-reference lock inventory if changed) is an explicit B-only scope item requiring review. No new external package is proposed. Projects integration is unnecessary for the lyrics panel; v3 round-trip can be verified with existing project tests. No schema change.

## Time and persistence boundaries

Selected lyrics stay in LyricsSourceSeconds, preserving text, source receipt/manual creation, line starts/optional ends and provenance. Desktop playback maps source frames through song seconds (sourceSeconds = songSeconds + AudioOffsetSeconds). Lyrics source times are not proven to match that audio; do not apply AudioOffset to lyrics, draw them as aligned note times, infer ends/word timestamps or alter existing notes. Alignment belongs to Generation.

Project v3 persists only the explicit selected Core payload. Search query/page/capabilities/diagnostics, errors and transient preview are not stored. Opening a project must restore stored selection without provider requests or metadata refetch; use existing v3 seam when project UI is later ordered.

## File ownership and meaningful acceptance

A only: NEW Acquisition/Lyrics/LyricsAcquisitionSession.cs (its small state/error types may remain in that file) and NEW Core.Tests/LyricsAcquisitionSessionTests.cs. Existing provider/parser/shared contracts/Core/Projects/Desktop/projects/locks unchanged. Own controlled providers/TCS fixtures test success/no-results/typed errors, malformed/mismatched page, revision identity/variant/instrumental rules, manual validation, no implicit choice, late success/error after supersession/cancel/manual/dispose, disposal drain/borrowed ownership and immutable prepared payload. Add acceptance for rapid three-plus supersessions proving one invocation/one latest intent, settled obsolete tasks and no extra queued calls; stale Select preserving a newer valid preview; observer reentrancy/fault isolation and other-subscriber/cleanup continuation; ignored-cancel provider with usable manual/reset/cancel; repeated concurrent disposal, one drain/final notification, no pending search after closure and no late notifications. No real sleeps/network.

B only after A review: NEW Desktop/Lyrics/LyricsPanel.cs (small UI adapter), narrow EditingSession.cs, EditorWindow.cs, EditorApplication.cs wiring, Desktop project-reference inventory and own NEW Desktop.Tests/LyricsPanelTests.cs plus narrow existing session regression tests as needed. Confirm exact files before dispatch; no parallel seam owner. Headless acceptance: explicit input/search/cancel, visible typed status/diagnostics, explicit provider/manual selection, safe UI-thread revisions, disabled invalid Apply, actual same-history apply/undo/redo/no-op preserving notes/media/metadata/offset, close during pending search, and no live requests. Existing project tests demonstrate selected v3 persistence; add a real new flow round-trip only if it verifies composition rather than duplicating the serializer.

Native B acceptance later: manually exercise search/cancel/explicit choice/manual fallback/apply/undo/redo in a visible prototype with synthetic fixtures; actual provider interoperability and real-song matching/content rights remain separately evidenced. No alpha/beta or complete Search→Generate→Edit→Validate→Export claim.

NEXT: A was explicitly dispatched after verified capacity and is locally implemented/tested; [implementation checkpoint](exec-plans/active/phase3-lyrics-acquisition-session.md) awaits SUPERVISOR review. No B implementation, push/PR/merge/live API/native UI or automatic next task. OWNER DECISION REQUIRED: none identified for these reversible routine choices.

Implementation evidence: tested e4b39231156732ee1243e3d77898371bc1789bc2, 47 new/939 total Windows tests, zero skipped; locked restore, Release zero warnings/errors and full format verification passed. This is headless A evidence, not lyrics-panel/native/fullworkflow acceptance. CTS cleanup/drain also waits for executing cancellation callbacks; callbacks/provider tasks must eventually settle, without a hard-kill guarantee.
