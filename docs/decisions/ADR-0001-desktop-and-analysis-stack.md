# ADR-0001 – Desktop/editor og isolert analyseworker

Dato: 2026-10-07. **Status: foreslått; ikke akseptert.** Prosjekteierens vedtak mangler. Dette dokumentet er et begrunnet forslag til videre Fase 1, ikke instruks om å bygge Fase 2.

## Problem

Editoren skal ha direkte noteredigering, waveform, live preview, undo/redo og presis avspilling/loop. Tung, lokal audioanalyse må ikke blokkere editoren. Windows er første plattform; Linux skal kunne støttes uten grunnleggende omskriving. Ingen obligatoriske kontoer, personlige API-nøkler eller proprietær hovedengine.

## Alternativer

1. C#/.NET + Avalonia desktop med Python som separat analyseworker.
2. Python + PySide6/Qt for hele applikasjonen, med egen workerprosess for tunge jobber.
3. Tauri + web-UI, Rust-prosesslag og Python-sidecar.
4. Electron + web-UI og Python-worker.
5. Gjenbruk av Unity-baserte UltraStar Play som hovedapplikasjon.

Se [auditten](../PHASE1_AUDIT.md) og [dependency-matrisen](../DEPENDENCIES.md) for konkrete fordeler, lisenser og kompromisser.

## Foreslått løsning

Prøv videre **C#/.NET 10 LTS og Avalonia 12.1.3** for desktop/editor, med en avgrenset **Python-worker** for eksisterende signal-/modellbiblioteker. Python 3.12 er dagens evalueringsruntime; endelig runtime/patch og dependencysett velges etter kompatibilitet og pakking.

Hold intern sangmodell og brukerendringer uavhengig av UltraStar-format og analyseworker. Timeline tegnes som spesialisert kontroll rundt én konsistent audio-tidskilde. Playback-backend isoleres; miniaudio er kandidat, men bindingen er ikke valgt.

Prosesskommunikasjon skal først undersøkes med et minimalt testharness. Ingen offentlig IPC-kontrakt, lagringsmodell eller produksjonsmappestruktur vedtas her.

## Begrunnelse

Avalonia har permissiv core-lisens, Windows/Linux-retning og gjennomførbar custom-rendering i den utførte headless-prøven. C# gir et naturlig desktoplag; Python gir direkte tilgang til flere modne analysekomponenter. Prosessisolasjon gjør det mulig å avbryte/restarte tunge jobber og beskytte editoren mot analysekrasj.

PySide er et sterkt alternativ dersom fordelene ved ett språk oppveier konkret Qt-compliance og distribusjon. Web-UI kan også fungere, men ekstra webview-/Rust-/sidecarflater gir flere ting å verifisere. Ingen ytelsesrangering av de uprøvde GUI-alternativene er etablert.

## Konsekvenser og porter før aksept

- To språk og to runtimeflater krever reproducerbar pakking og en liten, versjonert workergrense.
- Windows 10 22H2 er dokumentert som Tier 2 i dagens Avalonia-støtte. Native bruk på eierens maskin må prøves.
- Audio-backend/binding, presis seek/loop, workerbelastning, native input og tekst/waveform må testes.
- Linux-smoke-test og minimum Windows-pakke må lykkes på valgt dependencysett.
- Modell-/binærlisenser avklares, og dokumenterte sangbenchmarks må være tilstrekkelige.
- Prosjekteieren godkjenner hovedstacken etter masteren §15.2. Vedtak og dato fylles inn først etter faktisk godkjenning.

## Vedtak

Ikke vedtatt. Ingen produksjonsspråk, GUI eller playback-backend er låst.
