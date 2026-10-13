# Lyrics storage contract preparation

STATUS: COMPLETE (SUPERVISOR-reviewet retning; pakke1 godkjent). TASK: phase3-lyrics-storage-contract-preparation. SMALL/SEQUENTIAL, bestilt Sol Medium/faktisk modell-reasoning ukjent. BRANCH work/phase3-lyrics-storage-contract, BASE94a1a806661e7c8e9093830c9be03b69f25979c4. [Ordre](../../PHASE3_LYRICS_STORAGE_WORK_ORDER.md), [proposal](../../PHASE3_LYRICS_STORAGE_PROPOSAL.md).

TASK A complete: PR16 merget som94a1a80, lokal main/remote samme SHA, tree identisk med PRHEAD02a8d0d og produkt/tests6c8a563 bevares.622/622 historisk sluttkode gjenbrukes; ingen suite ved docs-only.

COMPLETED: faktisk SongHistory.ReferenceEquals/no-redo-on-failure, immutable selection med allevarianter, manuell UTC-semantikk og shared SongDocument/currentVersion-routing er analysert. Separat read-only research er brukt uten polling/fileiendringer. Proposal skiller typed valgtpayload fra caches, foreslår konkrete provenienstider/no-op/bounds, frozenv3/eksplicit1-2-3reader og legacyfixtures/atomicstore/offline.

VERIFICATION: docs/inventory/privacy/whitespace og alle tidligere trackedfiles unntatt avgrenset AGENTkart byteintegritet bestått før commit. Masteroriginal/ADR/kode/tests/dependencies/locks bevares. Ingen ny testsuite/Linux/native/nett/v3kode.

UNRESOLVED: endelige Core/DTO/proveniensnavn og selectedbudget/preflight/no-op-/projectiondetaljer før API/schema fryses. NEXT: SUPERVISOR-kontraktreview og separat integratorordre før kode. OWNER DECISION REQUIRED: NO for disse reversible kontraktvalg. Ingen PR/merge/HTTP/storagekode i TASK B. Faktisk kvotegating er gjort med checkpointreserve; ingen kontodata/resetcredits publiseres. Eksakt proposal-SHA rapporteres etter commit/push.

Pakke1-staging fryser standalone API og projection før Song/v3, slik at dagensv2 ikke stille mister et nytt felt. Videre: [selected foundation](../active/phase3-selected-lyrics-foundation.md).
