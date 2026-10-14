# LRCLIB HTTP transport work order

TASK phase3-lrclib-http-transport, MEDIUM / SEQUENTIAL, one implementation worker. Sol High requested; actual model/reasoning not exposed, unknown. Branch work/phase3-lrclib-http-transport from integrated main c9dc7e4aa132468d971d7d0d64500909db68a7d1.

Authority: master §6/Fase 3, accepted ADR-0001/0002, DEVELOPMENT.md, lyrics contract and [accepted transport profile](LRCLIB_TRANSPORT_PROFILE.md). [Plan](exec-plans/active/phase3-lrclib-http-transport.md).

Implement only new Acquisition/Lyrics/LrclibProvider.cs and LrclibRequestGate.cs, with own new Core.Tests/LrclibProviderTests.cs and LrclibRequestGateTests.cs. Synthetic terminal handlers/fake clocks stay in these test files. Preserve existing decoder/parser/contracts/shared diagnostics, Core/Song/Projects/v3, MusicBrainz/GUI/tools/master/ADRs/licenses/dependencies/locks. Required cross-file seam changes stop for concrete supervisor review.

Parameterless controlled owned client; internal borrowed terminal-fixture constructor only. One original escaped query, no fallback/retry/redirect/auth/compression. Process-wide dedicated serial completion gate, 500 ms spacing, linked 30 s deadline before queue/cooldown, caller cancellation precedence, full cleanup. Accepted raw Retry-After decimal/date rules, maximum whole-second TimeSpan delta and no downcap; extreme valid delta fails closed only LRCLIB until process restart, no public reset. Bound declared/streamed body, status-first sanitized errors and reuse unchanged decoder.

Offline acceptance: query/handler/ownership; all statuses without reading error bodies; media/encoding and declared/streamed limits; network/read/decoder errors; queue/cooldown/header/body deadlines and caller precedence; completion spacing/shared instances/cleanup and reuse; Retry-After fallback/date/long/extreme/leading-zero/multiple cases; maximum cooldown, overflow-safe monotonic arithmetic, bounded chunks and isolated fail-closed state.

Locked restore, Release build, full Windows solution tests, solution format, source ownership/master/dependency/privacy/doclinks/whitespace checks. No live API/lyrics, new packages, Linux/native/GUI acceptance or new research.

Commit coherent implementation/evidence locally, send one authorized checkpoint callback and STOP FOR SUPERVISOR REVIEW. No push, PR, merge or automatic next feature in this order. Publication authorization does not override this local review stop.
