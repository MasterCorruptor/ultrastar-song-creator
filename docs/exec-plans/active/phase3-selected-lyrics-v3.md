# Selected lyrics editing and project v3

Status: frozen profile before implementation. Branch work/phase3-selected-lyrics-v3, base be209abd662b7654fb09171aff70479c1698b530. Scope is Core Song/validator/command, independent frozen Projects v3 DTO/preflight, owned tests/fixtures and [format profile](../../PROJECT_FORMAT_V3.md). Shared selected model/validation/equality, Acquisition/parser, dependencies, master, ADRs and historical v1 bytes are protected.

1. Commit exact frozen v3 profile before code.
2. Pair nullable Song field and pure command with v3 writer and explicit legacy readers; preserve old DTOs and strictness.
3. Add synthetic offline editing/history, independent legacy/v3 fixture, roundtrip, malformed/preflight and failed-save coverage. Keep real v2 reader tests.
4. Locked restore, Release build/tests, format, links, whitespace and protected byte comparison; commit/push clean consistent branch and stop for supervisor review. No PR or integration here.

No provider/native/media/network smoke or Linux acceptance is claimed. Temporary artifacts are isolated in this worktree's ignored .agent-local.
