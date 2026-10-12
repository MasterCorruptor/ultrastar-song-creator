# Agentkart

UltraStar Song Creator skal bli et lokalt desktopverktøy for UltraStar-sanger. Editoren er hovedprioriteten; Windows først, Linux-kompatibel arkitektur.

## Les først

Les docs/PROJECT_MASTER.md (v0.5), gjeldende arbeidsordre og relevante aksepterte ADR-er i docs/decisions/. Masteren endres bare etter eksplisitt bestilling. docs/DEVELOPMENT.md beskriver arbeidsflyt/build/test; README og docs/TESTING_STATUS.md beskriver nåværende leveranse. Historiske arbeidsplaner finnes i docs/exec-plans/completed/.

## Autoritet og arbeid

Lokal autoritativ working tree står over GitHub main/origin/main, stabile referanser og WIP-brancher. Bevar godkjent lokal informasjon; tidsstempler avgjør ikke autoritet.

Bruk avgrensede work/<oppgave>-brancher og konsistente checkpoint-commits. Integrer når acceptance criteria, tester og nødvendig godkjenning er oppfylt. main skal være stabil og synkronisert. Arbeid innenfor bestilt scope i src/, tests/, tools/ og relevante docs/rotfiler. Scratch/cache/logg hører hjemme i ignorert .agent-local/; varig kunnskap flyttes til docs/.

## Arkitektur og status

Bevar acquisition/generation/editor-skillet, immutable sangmodell og undo/redo. Core har ikke formatparser/fil-I/O. Prosjektformat versjoneres/migreres eksplisitt; historisk v1 endres ikke stilltiende. Fase 1, 2.1–2.4 og 3.1–3.3 er integrert. Fase 3.3 følger docs/ONLINE_MEDIA.md og avsluttet plan i docs/exec-plans/completed/. GUI-01 er integrert; se docs/GUI_01_TEST_SURFACE.md. Audio-01 venter review; se docs/AUDIO_PLAYBACK.md.

ADR-0001/0002 er akseptert: .NET/Avalonia, Python-prosess, utskiftbar miniaudio-backend, eksplisitt umxhq og MIT egen kode med separate LGPL-forpliktelser. Analyse er forslag med confidence/manuell korreksjon; ASR-default er ikke fastsatt. Ikke omgjør vedtak uten bestilling.

SUPERVISOR koordinerer/reviewer; AGENT implementerer etter docs/SUPERVISOR_POLICY.md. Neste arbeid er sekvensielt og krever avgrenset SUPERVISOR-ordre.

## Verifikasjon og stopp

Kjør locked restore/Release build/test/format etter utviklingsguiden. Kontroller masterintegritet, dokumentlenker og whitespace. Stopp berørt arbeid ved autoritativ konflikt, arkitekturbrudd, uavklart lisens eller nødvendig større scope-/teknologivalg. Kjernefunksjoner skal fungere lokalt uten obligatoriske tredjepartskontoer/API-nøkler.
