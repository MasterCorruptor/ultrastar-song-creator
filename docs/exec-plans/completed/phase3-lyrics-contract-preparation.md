# Fase 3 lyrics contract preparation

STATUS: COMPLETE (SUPERVISOR-reviewet forberedelse). TASK: phase3-lyrics-contract-preparation. MODEL USED: ukjent; bestilt Sol Medium. SMALL/PARALLEL_SAFE read-only providerresearch + isolerte integratordocs. BRANCH: work/phase3-lyrics-contract. BASE:50815d86282b99271f6c1fddfa83ced44881147b.

[Arbeidsordre](../../PHASE3_LYRICS_WORK_ORDER.md), [proposed kontrakt](../../PHASE3_LYRICS_CONTRACT.md). Faktisk kvotegating ble gjort ved dispatch; ingen resetcredits brukt.

COMPLETED: faktisk Song/Note/Phrase/Analysis/SourceDocument, RecordingChoices, DownloadedMediaDraft, strengt prosjektformat2 og offline-store er undersøkt. Ingen lyrics-document finnes; foreslått normalisert in-memory fundament/manual og separat eksplicit generelt v3-persistensvalg. Masterprioritet bevares med senere generelt nettsøk/Generation, ikke skjult providerdefault eller audio-confidence. Bounds/timing/errors/cancellation/valg/offlinefixture og sekvensiell felles API-port/filgrenser er dokumentert.

FILES: AGENTS.md, docs/SUPERVISOR_POLICY.md og egen arbeidsordre/kontrakt/denne plan. Ingen providerresearch eller produktkode. Ekstern read-only worker rapporterer til SUPERVISOR, ikke pollet/messaget her.

VERIFICATION: ren main og exact baseline før branch; protected master/ADR/kode/tests/pakker/locks/øvrige docs byteintegritet, offentlig inventory/lenker/privacy og whitespace bestått før commit. Ingen490-suite nødvendig for docs-only; ingen netttest/lyrics hentet.

UNRESOLVED: offisielle provider-API-/lisensreview, supported timing/offset/transportprofiler og konkret persistensscope. NEXT RECOMMENDED STEP: SUPERVISOR reviewer dette forslag sammen med research, så bestiller compilable provider-uavhengig fundament eller konkrete rettinger. Ingen PR/merge/providerkode nå. OWNER DECISION REQUIRED: NO for dette forberedelsescheckpointet; eventuelle reelle lisens/arkitekturvalg avgjøres ved review. Eksakt commit-SHA rapporteres etter commit/push.

SUPERVISOR har reviewet forberedelsen og presisert in-memory fundament, LRCLIB som første senere adapter og separate instrumental/missing/unsupported/capabilities. Fortsettelse: [fundament](phase3-lyrics-foundation.md).
