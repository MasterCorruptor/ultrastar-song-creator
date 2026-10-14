# LRCLIB transport — local pre-code proposal

STATUS: ACCEPTED pre-code contract by SUPERVISOR at ed171fe, with the explicit User-Agent/header-limit clarification below. HTTP is not implemented. TASK phase3-lrclib-transport-preparation-local, SMALL / SEQUENTIAL. No HTTP implementation or remote publication is authorized by this document.

[Checkpoint](exec-plans/completed/phase3-lrclib-transport-preparation-local.md), [historical adapter research/proposal](PHASE3_LRCLIB_ADAPTER_PROPOSAL.md), [accepted local decoder](LRCLIB_RESPONSE_PROFILE.md), [lyrics contract](PHASE3_LYRICS_CONTRACT.md).

## Baseline and request

Integrated main42eb9f6 includes shared diagnostics and project v3. Decoder ce5d84f/2c8498f is accepted locally with 49 decoder cases and 822/822 Windows tests, not integrated into main. Its historical publication-permission block is resolved by the later explicit owner approval; publication/integration is now approved after document review. Callback-policy5dfda9d is distinct from the later standing public-push authorization recorded in SUPERVISOR_POLICY.md. No new research/live lyrics or new test-suite execution accompanies this proposal.

Proposed public production API: sealed LrclibProvider : ILyricsSearchProvider, IDisposable; public parameterless constructor, ProviderId = lrclib and existing SearchAsync(LyricsQuery, CancellationToken). Validate the original query and send exactly one anonymous GET to https://lrclib.net/api/search with separately URI-escaped track_name=Title and artist_name=Artist. No trimming/guessed query parameters, q fallback, /get, retry, redirects, account keys or other requests. Source/provenance URLs remain inert.

Production owns a controlled HttpClient and HttpClientHandler: AllowAutoRedirect=false, UseCookies=false, AutomaticDecompression=None, ordinary TLS validation, no credentials, explicit MaxResponseHeadersLength=64 (KiB). Client Timeout=InfiniteTimeSpan so one explicit overall deadline governs the operation. Send User-Agent: UltraStarSongCreator/0.0-prototype (+https://github.com/MasterCorruptor/ultrastar-song-creator) and Accept: application/json; do not request compression. No public borrowed client/handler or general injection overload.

Proposed internal friend-test constructor: LrclibProvider(HttpClient client, LrclibRequestGate gate, TimeProvider clock). It borrows a terminal fixture client that does not itself follow redirects, and isolated gate/clock; it does not dispose these. Timeout and body limits remain the production constants; fake time exercises them. No global mutable fixture backend. The test constructor is not a production injection guarantee.

## Ownership, gate and deadline

Production uses one dedicated process-wide LRCLIB gate, independent of MusicBrainz. A provider disposes its owned client/handler, never the shared gate. Request, response, body stream, cancellation sources and lease are cleaned up on every acquired path, including send/read failure and cancellation. Provider disposal is idempotent; subsequent SearchAsync throws ObjectDisposedException. Concurrent disposal during an active call is outside the supported caller contract.

Use a serial lease through sending, headers, body read and bounded decoding. In a finally path, dispose body/response/request before recording terminal completion/abort and releasing the lease. Next actual request starts no earlier than 500 ms after that terminal timestamp, including failures. A canceled waiter that never sent does not invent a request/completion timestamp. Cooldown waiting holds serialization; cleanup and semaphore release occur even when waiting is canceled.

A linked 30-second overall deadline begins before gate acquisition/cooldown and covers headers, body and bounded decoder checkpoints. Use the supplied TimeProvider for the deadline and waits. Caller cancellation takes precedence and retains OperationCanceledException; otherwise deadline cancellation becomes sanitized LyricsSearchException TimedOut. Check tokens before/after synchronous parse and between bounded stages; no hard-preemption promise. A long server cooldown persists after individual calls time out.

Actual existing RecordingRequestGate records request start and MusicBrainz creates its deadline after EnterAsync. Neither behavior is copied into this completion-based contract, and those existing files remain unchanged.

## Accepted Retry-After contract

For 429/503 only, inspect raw header values through Headers.NonValidated, not merely the typed RetryAfter getter. Missing, ambiguous multiple values, invalid syntax, negative delta or invalid/unrepresentable date use a local one-second cooldown and exposed RetryAfter=null. No raw header or exception text is logged or exposed.

For a single value, remove HTTP optional whitespace (space/tab). Scan ASCII decimal digits first: no sign, fraction or exponent. Ignore leading zeroes, compare significant digits with 922337203685, the largest whole-second delta representable in TimeSpan. Construct accepted delta ticks with checked integer arithmetic, never floating point. This handles syntactically valid deltas above the standard parser's range without packages or unbounded integer conversion. Header parsing is bounded by the controlled production response-header limit; fixture inputs must honor the same bound.

If not a decimal delta, use RetryConditionHeaderValue.TryParse for HTTP-date and require Date. Calculate its nonnegative difference from the supplied clock's UTC receipt; a valid past date means zero and exposed zero retry. Invalid date parsing uses the declared fallback. Capture the receipt once for classification/provenance, preserving existing decoder receipt semantics.

A read-only local .NET 10.0.11 probe found TryParse accepts 2147483647 seconds but rejects 2147483648 and 922337203685, as well as negative/invalid inputs. It also rejects the unrepresentable 922337203686. Thus its false result does not distinguish valid long delta, valid unrepresentable delta and invalid syntax. The raw digit check is necessary. This is a local parser observation, not a new solution test or live provider fact.

**Accepted extreme policy:** a syntactically valid nonnegative delta larger than TimeSpan representation marks only the LRCLIB shared gate unavailable for the rest of this process. Original 429 retains RateLimited, original 503 retains Unavailable, both with actual status and null retry metadata plus a static explanation. Later LRCLIB calls return static Unavailable without sending; caller cancellation still takes precedence. Other providers and manual lyrics remain usable. Process restart reconstructs the gate. No public reset API, account/authorization reset or quota-reset mechanism.

This conservative policy may disable LRCLIB until restart after a corrupt or adversarial extreme header. It avoids downcapping an unrepresentable wait and avoids adding persistence, a reset interface or arbitrary numeric storage. No smaller bounded wait policy both preserves that advertised duration and offers automatic resumption in TimeSpan; declaring it invalid and waiting one second would violate the no-downcap requirement. SUPERVISOR accepted this recommendation in the review of ed171fe; implementation still requires its own bounded order.

## Overflow-safe shared cooldown

Store monotonic start timestamp plus remaining TimeSpan duration, rather than adding a huge duration to DateTimeOffset or an absolute timestamp. Under the gate lock, subtract elapsed time with saturation at zero. When extending, retain max(existing remaining, new duration) and anchor that remaining duration at now; never shorten outstanding cooldown. Do not expose a clamped delay as the provider's retry value.

Actual wait is max(completion spacing remaining, cooldown remaining), delayed in chunks no larger than one minute and canceled by the caller/overall deadline. Recompute from the monotonic clock after each chunk. Long representable deltas remain whole in gate state even though a call ordinarily expires after 30 seconds. Gate state mutation, fail-closed checks, completion recording and release must be lock-safe; repeated lease disposal releases once.

## Response and sanitized errors

Send with ResponseHeadersRead. Classify unsuccessful status before reading an error body; never require/log/decode that body. Only exactly 200 enters successful decoding. 200[] is a valid empty page; 404 search is HttpFailure, not no-match. 429 is RateLimited, 503 Unavailable, 3xx/400/other non-200 statuses HttpFailure. HttpRequestException/read IOException become NetworkFailure, without raw/inner exceptions. Local query and disposal failures remain caller errors.

On 200, check declared Content-Length and count streamed bytes against 4 MiB before retention/decoding. Accept absent Content-Type or application/json (including parameters); reject explicit other/malformed media type. Accept absent Content-Encoding or identity only; reject compression/other encoding. Reuse unchanged strict LrclibJson.Parse with original query and one UTC receipt, including all-record validation before limit and document/page budgets. Read/encoding/media/schema errors remain typed, sanitized and contain no payload/URLs/headers/inner text.

## Smallest later implementation scope and offline acceptance

Only new Acquisition/Lyrics/LrclibProvider.cs, LrclibRequestGate.cs and new Core.Tests/LrclibProviderTests.cs/LrclibRequestGateTests.cs with own synthetic terminal handlers/fake time. The accepted LrclibJson.cs, shared diagnostics/validation, LrcParser, Core/Song/Projects/v3, GUI, dependencies/locks, master/ADRs/licenses remain unchanged. Escalate an actual required seam change.

Offline acceptance must cover:

- Encoded query, single send, owned handler flags/no redirects and borrowed/owned disposal; no hidden requests or payload logging.
- Status-first non-JSON error bodies, 200 empty/results, 404/429/503/3xx/other statuses, body declaration/stream bounds, media/encoding, network/read failure and unchanged decoder errors.
- Deadline including queue/cooldown/read/parse checkpoints; caller precedence, completion spacing across instances and cancellation/cleanup without deadlock.
- Missing/invalid/negative/multiple headers; zero/past/future dates; 2147483648 and maximum whole-second TimeSpan delta retained; next value/extreme digits fail closed; leading zeroes; maximum outstanding cooldown, overflow-free time arithmetic, bounded wait chunks, timeout followed by later gate reuse.
- Original extreme status/null metadata and later zero-send Unavailable; process/isolated-gate scope. No public reset or global test backend.

The contract is accepted; the next implementation order still requires actual quota gating and explicit scoped dispatch. Preparation is complete. Decoder/docs publication and integration are approved separately under the standing mandate; HTTP implementation does not start automatically.
