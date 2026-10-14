# Lyrics acquisition session A — work order and API baseline

TASK phase3-lyrics-acquisition-session, MEDIUM / SEQUENTIAL, one worker. Sol Medium requested; actual model/reasoning unknown. Branch work/phase3-lyrics-acquisition-session from accepted local 136e41e57007fa7baa4e41f39c747abaf87af508, preserving unpublished preparation history; integrated main6ddb743 remains unchanged.

Authority: [frozen behavior](PHASE3_LYRICS_FLOW_PROPOSAL.md), master §6/Fase3, accepted ADRs and DEVELOPMENT. [Plan](exec-plans/completed/phase3-lyrics-acquisition-session.md). Only NEW Acquisition/Lyrics/LyricsAcquisitionSession.cs and NEW Core.Tests/LyricsAcquisitionSessionTests.cs. Small public immutable state/error/outcome types share the production file. No existing provider/parser/contracts/Core/Projects/Desktop/schema/packages/locks/tools/master/ADR/license changes.

Precise public API baseline:

- sealed LyricsAcquisitionSession(ILyricsSearchProvider provider) : IAsyncDisposable; borrowed provider, no default/registry.
- LyricsSessionSnapshot Current { get; }; event EventHandler? Changed.
- Task<LyricsSessionOutcome> SearchAsync(LyricsQuery query, CancellationToken cancellationToken = default).
- LyricsSessionOutcome Cancel(), Reset(), Select(long revision, string providerId, string candidateId, string? documentId), SetManualText(string text).
- ValueTask DisposeAsync(), sharing one drain Task across repeated/concurrent calls.
- Immutable snapshot: Revision, Status, Query?, Page?, Selection?, Core.PreparedSelection?, Error?. Status: Idle, Searching, Results, NoResults, Failed, Cancelled, ManualReady, Disposed.
- Outcome kinds: Succeeded, Unchanged, Rejected, Failed, Cancelled, Superseded, Closed. Expected outcomes return typed values; misuse after disposal throws ObjectDisposedException.
- Immutable error: Kind Provider/Validation/Unexpected, optional LyricsSearchError, LyricsValidationError, bounded HTTP status and nonnegative retry. No free-form message/inner/payload/URL/header. Undefined provider codes become Unexpected; malformed metadata is dropped and retry retained only for rate/unavailable errors.

One actual invocation and one newest pending intent; superseded outward tasks settle immediately while actual completion remains observed. Provider/cancellation/observer/drain calls outside state lock. Original-query/provider/page validation, typed outcomes and no automatic choice. Select stale revision changes nothing; current invalid Results choice clears preview but retains page; non-Results Select rejects without mutating ineligible state. Validate/project outside lock then recheck revision before committing.

Manual invalidates active/pending and uses existing ManualLyrics once; prepared snapshot remains stable across reads. Reset clears all transient state, Cancel only changes Searching; neither mutates Song. Fault-isolated/reentrant observers, no late provider state after closure, final cleared Disposed plus at most one terminal notification; owner awaits drain before provider disposal. Ignored cancellation can delay actual next invocation/drain, not manual/reset/cancel.

Deterministic owned fixtures cover all frozen concurrency/selection/disposal/error criteria. Locked restore, Release, full Windows suite, format, protected byte identity/master/ADRs/deps/privacy/doclinks/whitespace; no live/Linux/native/GUI claim.

Original implementation review stop was satisfied at de2b09f; root and independent review accepted tested e4b3923 without actionable findings. A later close order authorizes work-branch push/PR only, then stop. Callback remains blocked pending separate message-destination approval; use own chat/repo. No merge/main update/B/Generation/extra worker or new product decision.
