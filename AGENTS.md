# Agentkart

UltraStar Song Creator skal bli et lokalt desktopverktøy for UltraStar-sanger. Generation-prosessrunner: review; Windows først, Linux-kompatibel arkitektur.

## Les først

Les docs/PROJECT_MASTER.md (v0.5), gjeldende arbeidsordre og relevante aksepterte ADR-er i docs/decisions/. Masteren endres bare etter eksplisitt bestilling. docs/DEVELOPMENT.md beskriver arbeidsflyt/build/test; README og docs/TESTING_STATUS.md beskriver nåværende leveranse. Historiske arbeidsplaner: docs/exec-plans/completed/.

## Autoritet og arbeid

Lokal autoritativ working tree står over GitHub main/origin/main, stabile referanser og WIP-brancher. Bevar godkjent lokal informasjon; tidsstempler avgjør ikke autoritet.

Bruk avgrensede work/<oppgave>-brancher og konsistente checkpoint-commits. Integrer når acceptance criteria, tester og nødvendig godkjenning er oppfylt. main skal være stabil og synkronisert. Arbeid innenfor bestilt scope i src/, tests/, tools/ og relevante docs/rotfiler. Scratch/cache/logg hører hjemme i ignorert .agent-local/; varig kunnskap flyttes til docs/.

## Arkitektur og status

Bevar acquisition/generation/editor-skillet, immutable sangmodell og undo/redo. Core har ikke formatparser/fil-I/O. Prosjektformat versjoneres/migreres eksplisitt; historisk v1 endres ikke stilltiende. Fase 1, 2.1–2.4 og 3.1–3.3 er integrert. GUI-01/Audio-01/GUI-02 er integrert; docs/GUI_02_EDITOR_BINDING.md beskriver akseptert prototype.

ADR-0001/0002 er akseptert: .NET/Avalonia, Python-prosess, utskiftbar miniaudio-backend, eksplisitt umxhq og MIT egen kode med separate LGPL-forpliktelser. Analyse er forslag med confidence/manuell korreksjon; ASR-default er ikke fastsatt. Ikke omgjør vedtak uten bestilling.

SUPERVISOR reviewer; AGENT implementerer/rapporterer hendelsesstyrt etter docs/SUPERVISOR_POLICY.md. Eiermandatet tillater kvotestyrt masterarbeid og gjenopptakelse. Lyrics-fundament/LRC/selected-typer er integrert. B1–B3 er integrert via PR22. G1A akseptert; G1B: docs/PHASE4_GENERATION_PROCESS_CHECKPOINT.md; STOP_FOR_REVIEW. Fase3 er ikke ferdig.

## Verifikasjon og stopp

Kjør locked restore/Release build/test/format etter utviklingsguiden. Kontroller masterintegritet, dokumentlenker og whitespace. Stopp berørt arbeid ved autoritativ konflikt, arkitekturbrudd, uavklart lisens eller nødvendig større scope-/teknologivalg. Kjernefunksjoner skal fungere lokalt uten obligatoriske tredjepartskontoer/API-nøkler.
