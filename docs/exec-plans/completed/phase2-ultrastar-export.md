# Fase 2.4 – UltraStar writer og komplett sangmappe

Status: gjennomført og verifisert 2026-10-08; klar for review. Branch work/phase2-ultrastar-export, base work/phase2-ultrastar-import (25e1c7532e74997c1d8577ae15d3e6bf3e0b270f). PR #1–#4 er fortsatt åpne; ingen automatisk merge.

Prosjekteieren valgte unversionert + v1 med absolutt output, automatisk avrunding med rapport og komplett sangmappe med kopiert lyd/bilder/video. Arbeidsordre og eksportkontrakt dokumenterer scope. Autoritet: PROJECT_MASTER.md §9/13–14/16–17, aksepterte ADR-er og pinnede formatkilder.

## Kontrollpunkter

- [x] Avklarte eksport-/kvantiserings-/metadata-/pakkeregler.
- [x] Ren writer av aktuell modell; explicit grid/profil, avrundingsdeltas og formatvalidering uten prosjektmutasjon.
- [x] Ny targetmappe med relative medier, collision-safe filenames, dedup og verified SHA/bytekopi.
- [x] Uavhengig .txt-forventning, import/export-import og prosjekt-save/load/edit/export.
- [x] Preflight/staging/rename; missing/local reference, mid-copy cancellation, feil/cleanup, target-race og relocation.
- [x] Windows/Linux, locked restore/build/test/format, master-/ADR-/Core-/Projects-/v1-fixtureintegritet og public inventory/privacy.
- [x] Praktisk Windows-smoke med gyldig syntetisk WAV/PNG/MPEG4, reimport og mediedekoding.
- [x] Avsluttet dokumentasjon, konsistente commits og avgrenset PR mot importbranchen.

## Gjennomføring og evidens

Writer/timing ble committet etter 241 tester (203 tidligere + 38 writer). Pakkekontrollpunktet gir 263 tester (+22 pakke). Alle 263 bestod på Windows og nettisolert Linux med SDK 10.0.401. Release build uten warnings, locked restore og formatkontroll bestod. Scratch/artifacts/media/binærer ble holdt ignorert under .agent-local/.

Smoke pakket to noter og gyldige WAV/PNG/MPEG4-assets; kopier samsvarte i hash, media kunne dekodes og teksten reimporteres. Ingen karaokeapp/GUI ble kjørt. FFmpeg var kun eksisterende privat evalueringsverktøy, ingen ny produktdependency.

Source headers er proveniens, mens aktuelle noter/metadata/medier/offsets er sannhet. Grid hentes eksplisitt eller fra source BPM, ikke fra gjetning om musikalsk tempo. Ukjente metadata beholdes; operative headers/refs normaliseres. Medley-rescaling, collapsed notes, negative GAP og tvetydige medier har eksplisitte regler. Ingen stille sletting/forlengelse/sortering.

## Avgrensning og videre arbeid

Ingen relativ output, duett, ny importdialekt, tapsfri råtekst, analyse/GUI/acquisition/playback eller mediatranscoding. Core/prosjektformat og aksepterte ADR-er er uendret. Eksisterende target/source/prosjekt endres ikke av pakkeeksporten. Ingen faktisk installer, hosted CI eller karaoke-app-interop påstås.

Planen avslutter Fase 2.4 som avgrenset deloppgave. Neste foreslåtte trinn er gjennomgang/integrasjon av de stablede PR-ene før ny produktfase; det startes ikke automatisk. Ingen automatisk merge.

Publisert leveranse: [PR #5](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/5), review-klar mot work/phase2-ultrastar-import. Ingen merge utført. Neste foreslåtte arbeidsordre: gjennomgang og integrasjon av PR #1–#5; separat godkjenning før start.
