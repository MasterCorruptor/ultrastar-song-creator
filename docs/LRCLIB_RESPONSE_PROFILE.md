# LRCLIB pure response decoder

STATUS: implemented, locally verified and accepted by SUPERVISOR; root and independent static review found no actionable findings for ce5d84f/2c8498f. Publication is expressly owner-authorized by the later 2026-10-14 policy addition; publication/integration approved after document review at ed171fe. Pre-code profile frozen at 31e31ac. Internal Acquisition.Lyrics.LrclibJson.Parse(ReadOnlySpan<byte> utf8, LyricsQuery originalQuery, DateTimeOffset retrievedAtUtc, CancellationToken cancellationToken=default) returns LyricsPage. [Ordre](PHASE3_LRCLIB_RESPONSE_WORK_ORDER.md), [plan](exec-plans/completed/phase3-lrclib-response-decoder.md), [adapterproposal](PHASE3_LRCLIB_ADAPTER_PROPOSAL.md).

No HTTP/status/retry/client/gate/provider/fetch/parser/shared/Core/v3 changes. Input is a response body, not a networkoperation. Caller token checks before/between/after bounded synchronous work, OCE unchanged. Query uses existing validation; bad callerquery retains its typed validation error, supplied default/nonUTCreceipt is ArgumentOutOfRangeException. Payload failures are sanitized LyricsSearchException InvalidResponse; resourcebounds are ResponseTooLarge, no raw/innerexceptiontext.

Input4MiB, strictUTF8 including escapedlone-surrogates in known/unknown keys/strings, JSONdepth16/rootarray<=20, duplicateproperties recursively and IDs rejected. Count21 is ResponseTooLarge. Unknownproperties ignored within same bounds. All records validated before originalrequestedlimit, no fakequery. One supplied UTCtimestamp and original providerorder/query are retained.

Require positiveINTEGERInt64id/boolinstrumental, no coercion. Optional name/trackName/artistName/albumName/duration/plainLyrics/syncedLyrics/lyricsfile may be absent/null; optionalhasWordSync unknown unless bool. Nonnullaliases must be ordinalequal. PrefertrackName thenname; metadata exact, Versionnull. Duration finitepositive<=14400. Provider lrclib, invariantdecimalID/inert https://lrclib.net/api/get/{id}, no fetch. MatchUnknown with optional known durationdelta, neverconfidence/ranking.

Bound metadata4096UTF16 and normalizedplain/synced raw256KiB before blankignore. Nonblankplain makes independently validated plain document, synced only via existingLrcParser. Success line-lrc, no fallbackplain from failure. Plain survives malformed/timing/unsupported/NoLyrics LRC with typeddiags. Anyoversize fatal even if alternativevalid. Withoutdocs: malformed/timingLRC InvalidResponse evenwithYAML; unsupportedLRC/YAML Unsupported; elseMissing. Instrumentaltrue only without nonblanklegacylyrics/affirmativewordflag; YAMLalone not contradiction. No falseemptydraft or Word.

lyricsfile is INLINE string, nonempty declares unsupportedlyricsfile-yaml and warning, no YAMLparse/URLfollow/persist. Richerformats do not suppress validPlain/Line. ReportedWordSync from bool only; nonblankLRC can reportline=true otherwiseunknown. Diagnostics map LRC explicitZeroOffset Info, NoLyrics/unsupported/invalidsyntax/timing Warning, appformat lrc-simple-line-v1/variant line-lrc/physical line. YAMLunsupported Warning with nullvariant/line. Use existing candidate/pagebudget256KiB/4MiB and max64diags.

Strictallresults via ValidateProviderResults(lrclib,originalQuery,all,20) before Take(min(Limit,20)), Pagevalidation afterward. [] validempty. Own synthetictests only; no liveAPI/netresearch/lyricsfixtures or claims beyond puredecoder. HTTPtransportkontrakten er nå akseptert i LRCLIB_TRANSPORT_PROFILE.md; implementasjon krever egen ordre.


## Verification — 2026-10-14

[Implementation](../src/UltraStar.SongCreator.Acquisition/Lyrics/LrclibJson.cs) and [own synthetic tests](../tests/UltraStar.SongCreator.Core.Tests/LrclibJsonTests.cs). SDK 10.0.401:

- 49/49 decoder tests passed. Coverage includes required/optional fields, types/nulls, aliases, numeric forms, duration/ID boundaries, exact input/document/metadata/depth limits, Unicode/surrogates/escaped duplicates, cancellation, provider order/query/receipt, independent variants/status/diagnostics, all-record validation before limit, aggregate budget growth and LRC expansion.
- Locked solution restore and Release build passed, zero warnings/errors.
- Full Windows solution suite passed 822/822: Core 754, Audio 17, Desktop 51; zero skipped.
- Full solution format verification, staged whitespace, local document links, public inventory/privacy and vendor checks passed.
- 280 protected existing files byte-identical to integrated main42eb9f6; master also byte-identical to supplied local original. Dependencies/locks, shared seams/parser, Core/Song/Projects/v3, ADRs and license unchanged.

Cancellation is cooperative at checkpoints, not immediate preemption inside synchronous JSON/LRC parsing. No live API, Linux, native playback, transport or end-to-end acquisition result is claimed. Local supervisor code acceptance is complete; publication and integration are approved under the new standing mandate. No automatic HTTP implementation.

Historical implementation checkpoint: ce5d84f. Branch publication was previously rejected by automatic approval review for missing payload authorization. New explicit owner authorization on 2026-10-14 resolves that permission gap; supervisor approved publication/integration after reviewing ed171fe. See the completed plan and actual Git/PR history for delivery status.
