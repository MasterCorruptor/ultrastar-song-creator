## Gjeldende godkjenningsstatus

Begge kildepakker er SUPERVISOR-kodegodkjent og samlet verifisert ved5ade286:773/773 Windows-tester, build/format/integritet bestått. [Lokal combined-rapport](../../LYRICS_COMBINED_LOCAL_REVIEW.md) er gjeldende evidens og publiseringsstatus. Eier har2026-10-14 eksplisitt godkjent GitHub-destinasjonen og main-integrasjon. Tidligere lokal-only-stopp er opphevet; faktisk integrasjonsstatus angis av GitHub-PR/historikk og sluttverifikasjon. Ingen kodeendring eller ny testsuite begrunnes av dokumentavslutningen.

## Historisk pakkecheckpoint – original tekst nedenfor

Status/avventende review og testtall i teksten nedenfor gjelder det opprinnelige pakkecheckpointet, ikke nåstatusen. Historisk evidens bevares uendret.

# Selected lyrics editing and project v3

Status: implementation complete; awaiting supervisor review. Branch work/phase3-selected-lyrics-v3, base be209abd662b7654fb09171aff70479c1698b530. Scope is Core Song/validator/command, independent frozen Projects v3 DTO/preflight, owned tests/fixtures and [format profile](../../PROJECT_FORMAT_V3.md). Shared selected model/validation/equality, Acquisition/parser, dependencies, master, ADRs and historical v1 bytes are protected.

1. Commit exact frozen v3 profile before code.
2. Pair nullable Song field and pure command with v3 writer and explicit legacy readers; preserve old DTOs and strictness.
3. Add synthetic offline editing/history, independent legacy/v3 fixture, roundtrip, malformed/preflight and failed-save coverage. Keep real v2 reader tests.
4. Locked restore, Release build/tests, format, links, whitespace and protected byte comparison; commit/push clean consistent branch and stop for supervisor review. No PR or integration here.

No provider/native/media/network smoke or Linux acceptance is claimed. Temporary artifacts are isolated in this worktree's ignored .agent-local.


Completed: Song.SelectedLyrics and typed SongValidator issue, pure SetSelectedLyrics with full standalone validation/equality, independent v3 root/DTO/maps/preflight, explicit legacy readers. Legacy SongDocument, source DTOs/enums and ProjectStore remain unchanged. JSON property/envelope Unicode failures now translate to InvalidJson instead of leaking JsonDocument exceptions; strictness is retained.

Acceptance: 77 new synthetic offline tests cover exact null/manual/plain/line/word/instrumental roundtrips, independent handwritten v2/v3 fixtures, unchanged legacy bytes and explicit v3 save, selected-only projection/storage, replacement/clear/rejection/no-op redo and exact snapshots, complete required nullable field checks, unknown/duplicate/null/type/enums/Unicode/timing/provenance failures, preflight collection/document/selected budgets and exact Unicode budget boundary, cancellation and failed-save byte preservation. Legacy v2 source schema tests now read the handwritten v2 fixture rather than current writer output. Existing acquisition, recording-choice, source and import writer-version assertions now expect actual v3 save. The staging-only Song reflection assertion was removed; other standalone coverage is intact.

Verification: existing SDK 10.0.401 Windows locked restore passes; Release solution build 0 warnings/errors; 749/749 tests (Core681, Audio17, Desktop51), no skips; solution format verify-no-changes passes. Whitespace, new document links and protected byte comparisons pass. Source DTO/legacy song payload sections are unchanged. No new package/project/lock, provider/runtime/parser, GUI/native, live network or Linux evidence. Shared SelectedLyrics.cs staging XML summary remains protected by ownership; it is a known documentation-only follow-up.

Next: supervisor reviews this branch, then instructs permanent AGENT on integration. Worker stops here; no PR/main merge or next work.
