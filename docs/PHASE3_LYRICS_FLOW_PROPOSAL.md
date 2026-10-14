# Lyrics acquisition flow — proposed next functional slice

STATUS: PROPOSED, awaiting SUPERVISOR review; no new API, coordinator or GUI is implemented by this preparation. BASE main6ddb743e7d06853eb194bb83505ea62fe489a7b7 (PR20).
[Plan](exec-plans/active/phase3-lyrics-flow-preparation.md), [master](PROJECT_MASTER.md) §6/Fase 3–6, [transport](LRCLIB_TRANSPORT_PROFILE.md), [lyrics contract](PHASE3_LYRICS_CONTRACT.md), [selected payload/project v3](PROJECT_FORMAT_V3.md).

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

## Proposed A contract — names are proposals, not existing APIs

New LyricsAcquisitionSession(ILyricsSearchProvider provider), IAsyncDisposable. Provider is **borrowed**; the composition owner remains responsible for disposing it after the session drains. No production default/fake provider, general plugin framework or new public transport injection.

Expose one immutable Current snapshot and a Changed signal with monotonic revision. Snapshot has status Idle/Searching/Results/NoResults/Failed/Cancelled/ManualReady/Disposed, original query, validated page if current, explicit preview choice/prepared Core.SelectedLyrics if any, and optional bounded typed error. These are transient memory only.

Proposed actions: SearchAsync(query, callerToken), Cancel(), Reset(), Select(revision, providerId, candidateId, documentId), SetManualText(text), DisposeAsync(). A returns/exposes a prepared immutable payload; it never applies it to a Song.

- Starting search validates the original query, invalidates the previous request generation, cancels it and clears page/preview/error. No implicit normalization, guessed metadata or first-result choice.
- Accept results only if generation is still current and the session is alive. Validate Page plus original query/provider identity. Empty success is NoResults. Invalid returned batches become sanitized InvalidResponse/ResponseTooLarge. Preserve provider order/diagnostics; no score/ranking.
- Cancel updates current status and invalidates pending completion. Late success/error from canceled or superseded requests is observed and discarded, including providers that ignore cancellation. It cannot restore an old page/selection or overwrite a manual choice.
- Select requires the current revision and an exact member through LyricsSelections.Select. Available requires an explicit document; Instrumental requires explicit null-document choice. Missing/Unsupported cannot be applied. Reported Word capability never invents Word timing.
- SetManualText cancels/invalidates provider work, clears provider results and validates through ManualLyrics.Create. Successful manual text creates one frozen choice/prepared payload; repeated Apply must reuse that payload, not regenerate timestamps/IDs. Invalid manual input records a typed validation outcome and leaves no applicable preview. It never changes the song.
- Reset or editing query/manual input in B invalidates corresponding old preview/results. This affects acquisition state only; existing Song.SelectedLyrics remains until an explicit Apply/clear command.
- State transitions use short serialized synchronization; no lock is held across provider awaits. Changed signals occur outside locks and may arise on completion threads. The UI adapter marshals to Dispatcher.UIThread, reads latest immutable revision and discards outdated queued rendering; no UI types enter Acquisition.
- Observe all request completions; dispose per-request cancellation sources when that request settles. DisposeAsync marks closed, invalidates/cancels outstanding work, awaits its settlement and emits no late notifications. It does not dispose the borrowed provider. The owner then disposes an owned provider. This respects the transport's unsupported concurrent provider-disposal contract; no sync UI blocking or hard-kill promise.
- Errors expose existing LyricsSearchError with optional HTTP status/retry or LyricsValidationError for input/choice. Unexpected provider failures have only a bounded static unexpected-failure indication; no raw Message/InnerException, payload, URL or headers reach presentation/logs. Selection/page text is retained only within existing budgets.

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

A only: NEW Acquisition/Lyrics/LyricsAcquisitionSession.cs (its small state/error types may remain in that file) and NEW Core.Tests/LyricsAcquisitionSessionTests.cs. Existing provider/parser/shared contracts/Core/Projects/Desktop/projects/locks unchanged. Own controlled providers/TCS fixtures test success/no-results/typed errors, malformed/mismatched page, revision identity/variant/instrumental rules, manual validation, no implicit choice, late success/error after supersession/cancel/manual/dispose, disposal drain/borrowed ownership and immutable prepared payload. No real sleeps/network.

B only after A review: NEW Desktop/Lyrics/LyricsPanel.cs (small UI adapter), narrow EditingSession.cs, EditorWindow.cs, EditorApplication.cs wiring, Desktop project-reference inventory and own NEW Desktop.Tests/LyricsPanelTests.cs plus narrow existing session regression tests as needed. Confirm exact files before dispatch; no parallel seam owner. Headless acceptance: explicit input/search/cancel, visible typed status/diagnostics, explicit provider/manual selection, safe UI-thread revisions, disabled invalid Apply, actual same-history apply/undo/redo/no-op preserving notes/media/metadata/offset, close during pending search, and no live requests. Existing project tests demonstrate selected v3 persistence; add a real new flow round-trip only if it verifies composition rather than duplicating the serializer.

Native B acceptance later: manually exercise search/cancel/explicit choice/manual fallback/apply/undo/redo in a visible prototype with synthetic fixtures; actual provider interoperability and real-song matching/content rights remain separately evidenced. No alpha/beta or complete Search→Generate→Edit→Validate→Export claim.

NEXT: SUPERVISOR review/freeze A contract and issue that bounded implementation order only after actual quota gating. This preparation stops at local docs commit/callback; no implementation, push/PR/merge/live API/native UI or automatic next task. OWNER DECISION REQUIRED: none identified for these reversible routine choices.
