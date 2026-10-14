# Lyrics panel B — local functional contract proposal

STATUS: Functional direction accepted in principle at301b885. B1 same-history apply contract is frozen below; B2 panel/dispatcher/admission/owned teardown remains proposed pending B1 review and separate dispatch. No UI launch or publication.
BASE accepted A440ea86e6e9b9a9cd72d0f8e37dfbb65039093b0, tested A e4b3923 / 47 new / 939 Windows cases; main6ddb743 unchanged.
[Plan](exec-plans/active/phase3-lyrics-panel-preparation.md), [frozen A/B behavior](PHASE3_LYRICS_FLOW_PROPOSAL.md), [actual A API](PHASE3_LYRICS_SESSION_WORK_ORDER.md).

A source branch and pending publication/callback approvals are preserved. This new local branch is preparation only and must not be used to publish the blocked A payload indirectly. No remote action or callback is authorized by this proposal.

## Actual baseline and minimal B ownership

EditingSession owns the sole SongHistory but exposes only Select/Move/Undo/Redo. SetSelectedLyrics already exists in Core. EditorApplication constructs DemoSong and UI-owned EditorPlaybackSession; EditorWindow subscribes to editing/playback changes and calls idempotent ReleasePlayback on close. Audio disposal verifies UI-thread access. A exposes immutable Current, Changed, typed actions and borrowed-provider asynchronous drain; it does not mutate Song. Desktop currently references Core/Audio, not Acquisition.

B adds functional lyrics controls to that prototype, keeping the timeline, demo playback and note operations. No design polishing, project replacement UI, open/save picker, media choice/download UI, file-to-PCM loader, alignment, note generation or lyrics-on-audio timing is included.

Proposed exact implementation ownership, only after this contract is reviewed:

- NEW Desktop/Lyrics/LyricsPanel.cs: small panel/adapter plus internal owned composition/lifetime helper in the same file; no generic command/plugin framework.
- EditingSession.cs: narrow public bool ApplySelectedLyrics(Core.SelectedLyrics value), using its existing history; generic undo/redo status wording.
- EditorWindow.cs: retain the existing public session/playback constructor. Add an internal lyrics-enabled composition overload; insert panel and normal-close guard, detach/lifetime hooks. Existing demo-only/headless call sites remain supported.
- EditorApplication.cs: explicit production LrclibProvider + A ownership and observed close/startup-failure cleanup.
- Desktop.csproj: local ProjectReference to Acquisition and InternalsVisibleTo Desktop.Tests for the internal composition test seam. No external PackageReference.
- Desktop/packages.lock.json: new acquisition Project entry depending on existing Core.
- Desktop.Tests/packages.lock.json: transitive acquisition Project entry and Acquisition dependency added to the Desktop Project entry. Existing external package versions/hashes remain unchanged. Verify actual restore diff, stop on unrelated package drift; no speculative lock edit now.
- NEW Desktop.Tests/LyricsPanelTests.cs; narrow EditingWindowTests.cs/EditorPlaybackIntegrationTests.cs regressions for same-history lyrics and close behavior as needed. Use existing headless infrastructure; no production fake transport.

Acquisition/Session/Provider/decoder/Core/Projects/audio/schema/master/ADRs/licenses remain unchanged. Tests borrow controlled A/provider instances through the internal composition seam; fake providers belong in test files only. Production always explicitly composes the real owned provider, never searches on startup or hides an initialization failure behind a fake backend.

## Controls and explicit selection

Use blank artist/title input, Search and Cancel; display the current bounded page with artist/title/duration/status and document variants. Show typed error/no-results/cancellation/retry and per-variant diagnostic code/severity/physical line, without raw exception messages or logging fetched lyrics. Query metadata does not overwrite song metadata.

No automatic first candidate, default chosen document or DemoSong search. Choosing an Available candidate requires a separate explicit document choice; Instrumental has an explicit no-document choice. Missing/Unsupported and unvalidated reported Word capability cannot enable Apply. Preview preserves text/line times/provenance and displays unsupported-format/failed-variant warnings, rather than hiding them. Source URLs remain inert metadata; no link fetch/browser launch.

Manual paste remains an explicit action: text edit invalidates the prior prepared preview, and a prepare-manual button uses A.SetManualText once. Do not call ManualLyrics again on Apply; reuse A.Current.PreparedSelection. Blank/invalid text leaves Apply disabled with typed validation. Search/no-results/provider error never clears the song's existing selected text.

Apply is a synchronous UI-thread command only when a validated prepared snapshot is still current, the panel's input/selection generation matches its last accepted render and the same EditingSession/song identity is targeted. Display “Egen testsang/demo” and current target metadata. A successful apply changes only Song.SelectedLyrics; it does not claim the queried recording matches the demo audio.

## UI revision and action dispatch

Panel construction, control updates, target validation and EditingSession actions occur on Dispatcher.UIThread. Borrow A and editing session; Changed can arrive on any thread. Capture panel lifetime/input generation, post a render, then read latest A.Current on UI thread. Ignore detached/obsolete queued rendering or a snapshot older than the last rendered revision.

Query/manual edits immediately clear displayed eligibility and disable Apply/candidate selection on UI, then request Reset. Do not rely on a delayed Reset notification to prevent an old preview from applying. When presenting provider results, also require query equality with the current input snapshot; changing inputs cannot re-enable the old page.

A.SearchAsync may execute cancellation callbacks before returning its task; Cancel, Reset, SetManualText and DisposeAsync can do so too. Start these A calls on a background task, not directly in click/text-change/closing handlers. Select notifications are marshalled likewise. Observe all task results/faults and return static typed UI status only while the panel is alive.

Keep this adapter bounded: at most one synchronous A action-dispatch job plus one latest pending input/reset/search/manual intent; coalesce repeated input changes. Disable duplicate action submissions while their intent is being admitted. Do not await a long provider Search task inside this dispatch queue: capture its returned task and observe it separately, so cancel/manual admission remains possible while the provider is in flight. A retains its own one-provider/one-latest-search bound.

If a synchronous A cancellation callback blocks admission, UI remains responsive and input can still be edited; later pending action waits for that callback to settle. Do not promise hard interruption or immediate admission in that case. Apply/undo/redo are not coalesced or replayed as background intents; apply stays disabled until the applicable preview is current and action admission has settled.

Internal lifetime helper owns observation/drain tasks and dispatch admission, not another SongHistory or a transport pipeline. Rejected stale actions never silently retry against a new revision. Ordinary late/superseded outcomes are discarded; expected close-time ObjectDisposedException is observed without raw UI/log exposure.

## Same-history apply and explicit boundaries

ApplySelectedLyrics calls history.Execute(new SetSelectedLyrics(value)) on current song at the UI application point. Use static success/no-op/rejection wording and notify existing Changed; do not expose history or execute against the song captured when search began. Equal payload returns false without clearing redo. Note edits made during search, metadata, media, offsets, selected-note identity and playback ownership are preserved.

Undo/Redo messages become “Endringen er angret/gjort om”, because history now includes lyrics changes. Move validation/messages stay intact. Preview selection is transient and is not automatically undone with song history; the current stored selected lyrics is rendered separately, so undo cannot falsely display a preview as already applied.

Project v3 already persists selected Core payload; no new persistence code/schema is needed here. No A query/page/errors/diagnostics are written into Song. LyricsSourceSeconds stays unaligned to song/audio time; no AudioOffset conversion or lyrics-to-note projection in this panel.

## Normal-close / detach and asynchronous owned teardown

Detach first on UI thread: disable all panel commands, invalidate panel lifetime/input generation, unsubscribe A.Changed and drop pending adapter intents. Detached renders/outcomes do nothing. A is borrowed by the view, so detach alone does not dispose it or its provider.

Production internal lifetime owns the session/provider and one reusable CloseTask. In a cancelable normal window-closing handler, keep the window/app lifetime alive for that task; repeated close requests reuse it. Invoke existing ReleasePlayback on UI thread before asynchronous lyrics cleanup. Audio cleanup stays UI-thread-only/idempotent.

Start CloseTask on a background worker **before** calling A.DisposeAsync, since that call can synchronously run cancellation callbacks. It closes A admission/cancels work, observes adapter jobs/search tasks, awaits the shared A drain and all started adapter admissions, then disposes the owned LrclibProvider. No provider disposal before A/cancellation callback settlement and no newly queued action after detach.

On successful completion, post the final permitted window close back to UI thread; the existing app Exit audio release remains harmless/idempotent. Observe the close task centrally; event handlers start/observe an owned Task rather than unhandled async-void work. A failed drain/cleanup produces a static failure indication and keeps the normal-close guard until an explicit later action; do not force close or log sensitive exception payloads. OS/process termination cannot be promised orderly cleanup.

Startup failure uses the same owned asynchronous cleanup path (with UI audio release first) and observes completion before normal app shutdown. No actual search is initiated during construction. This prevents a new owner from leaking its provider/session even if panel/window construction fails.

Headless composition injects borrowed session/provider and existing audio fixtures. The internal helper can be exercised with explicitly transferred fixture ownership to prove dispose order; ordinary borrowed panel construction never disposes caller resources. No public low-level fake HttpClient/provider default is introduced.

## Offline/headless acceptance and later native acceptance

Future tests must show:

- Blank initial inputs/no startup search, explicit encoded user intent, Search/Cancel/typed status/no-results and visible diagnostic/variant/instrumental/manual rules; no autopick or hidden fallback.
- Immediate input-edit invalidation, stale/reordered dispatcher renders and rejected stale choices cannot enable Apply; duplicate action admission/reset coalescing remains bounded and search observation does not block cancel/manual.
- Apply uses the frozen payload/current song, same-history note+lyrics undo/redo and equal no-op retaining redo; metadata/media/offsets/notes/selection remain consistent.
- Detach/lifetime generation ignores posted work; close during queued/active search with ignored cancellation or deliberately held cancellation callback does not block the UI dispatcher, releases audio on UI, waits for A drain before provider disposal, observes all faults and reuses one CloseTask. Borrowed resources are not disposed; no new call after detach/closure.
- Startup composition failure and normal close cleanup are observable and ordered; real provider/network/device is never used in headless tests. Existing note/playback tests still pass.

Later native/manual acceptance is a separately authorized visible-prototype check with synthetic deterministic fixtures: explicit input/choice/manual/apply/undo/redo, close while search is pending, and preserved playback/timeline. Respect actual native-UI permissions then; no UI is launched in preparation. Live LRCLIB compatibility, real media/project GUI, audio matching, content rights and full Search→Generate→Edit→Validate→Export remain separate evidence/gaps.

NEXT: SUPERVISOR reviews the bounded B contract/file ownership/lifetime before any implementation order and fresh quota gating. Commit local docs only and STOP. No publication, callback retry, merge/main update, production/test/project/lock edit or automatic feature start. OWNER DECISION REQUIRED: no new product decision identified; existing A external-delivery approvals remain separately pending.

## Accepted sequential split — B1 then B2

B1 is a separate small local implementation order: only EditingSession.cs and new LyricsEditingSessionTests.cs. ApplySelectedLyrics uses existing private history/current song, returns true only for real changes; equal payload is a no-op preserving redo. Expected invalid payload returns false/static rejection preserving state/history. **Null returns false with static rejection and never clears selected lyrics.** Changed is raised after success/no-op/rejection. Successful undo/redo uses generic change wording; note Move behavior is unchanged. No new project reference or GUI/lifecycle work in B1. [B1 work order](exec-plans/completed/phase3-lyrics-editor-apply.md).

B2 remains the proposed panel/UI/lifetime/project-reference scope above and requires B1 review plus a new bounded implementation order. No combined UI/lifecycle implementation or blocked A publication is authorized through B1.

B1 local implementation checkpoint: aaedb1d,14new/953total Windows tests, zero skipped; locked restore/Release0warnings/format pass. [B1 report](exec-plans/completed/phase3-lyrics-editor-apply.md) awaits root code review. This does not implement/approve B2 controls/dispatcher/lifetime or grant publication permissions.

## Current dispatch — B2 component / B3 wiring

B1 is accepted at fb6fcb6 (tested aaedb1d, 953 Windows cases). B2 is now ordered as a local component and headless tests only: [work order](PHASE3_LYRICS_PANEL_COMPONENT_WORK_ORDER.md). EditorWindow/EditorApplication, playback release and startup/normal-window-close wiring remain B3, requiring later review/order. Earlier pending A publication/callback statements above describe historical preparation only: exact A099884b is published in PR21 and short checkpoint callbacks are explicitly authorized. No B1/B2 publication or merge is included here.

B2 component checkpoint2d137f4 is implemented locally with24new/977total Windows cases; [report](exec-plans/active/phase3-lyrics-panel-component.md) awaits SUPERVISOR review. Only component + internal owned close and headless fixtures; window/app/startup/audio-release wiring remains B3. No new production search is active in the app.
