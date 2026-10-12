# GUI-01 – lokal sangredigeringstestflate

Status 2026-10-12: implementert på work/gui-01-song-editing, checkpoint før SUPERVISOR-review. Dette er første avgrensede GUI-testflate, ikke en alpha/release. [Arbeidsordre](GUI_01_WORK_ORDER.md), [checkpoint](exec-plans/active/gui-01-song-editing.md).

## Start på Windows

Fra repositoryroten med PowerShell:
```powershell
./tools/desktop/Start-Desktop.ps1
```
Scriptet bruker eksisterende privat SDK/cache hvis tilgjengelig, ellers installert .NET SDK 10 etter global.json. Det gjenoppretter låste pakker, bygger Release og åpner et vedvarende native vindu. Første restore på ny maskin krever NuGet; selve appen fungerer offline uten Python/modeller/medier/konto. Lukk testvinduet før ny build. Ingen installer eller selvstendig runtimepakke følger med.

Kjørbar build ligger i src/UltraStar.SongCreator.Desktop/bin/Release/net10.0/UltraStar.SongCreator.Desktop.exe. Direkte dobbeltklikk krever tilgjengelig .NET 10-runtime; startscriptet setter DOTNET_ROOT når lokal SDK brukes.

Alternativ med riktig SDK/runtime:
```powershell
dotnet restore UltraStar.SongCreator.slnx --locked-mode
dotnet build UltraStar.SongCreator.slnx -c Release --no-restore
dotnet run --project src/UltraStar.SongCreator.Desktop -c Release --no-build --no-restore
```

## Prøv dette

1. Klikk «Hei» i tidslinjen. Sangens start er 1 sekund, varighet 1 sekund, pitch MIDI 60.
2. Skriv 2,5 i startfeltet og trykk «Flytt note». Blokken flytter seg, gjeldende tid vises.
3. Trykk «Angre», så «Gjør om». Snapshot og blokken gjenopprettes; knappetilstand følger historikken.
4. Prøv NaN, negativ tid eller 7,5. Flyttingen avvises med melding; sang og historikk bevares.
5. Flytt til 3 sekunder: overlapp med neste note vises som tillatt utkast-warning. Lukk vinduet normalt.

Testsangen er egen, deterministisk og uten lyd. Ingen endringer lagres etter lukking. Tidslinjen har fast testområde 0–8 sekunder og MIDI 58–66, horisontal scrolling på smale vinduer. Dette er ikke en generell viewport/zoom-kontrakt.

## Modell og verifikasjon

EditingSession eier eksisterende SongHistory. Notevalg lagrer bare ID; alle noter/labels projiseres fra Current. Flytting bruker MoveNote, validering bruker SongValidator. Core er uendret; ingen muterbar parallell Song, fil-I/O eller formatkonvertering. Code-behind er begrenset til testflate/layout/input; ingen ny MVVM-dependency.

Windows og nettisolert Linux: locked restore, Release build uten advarsler og 432/432 tester (422 eksisterende + 10 headless GUI). Testene bruker faktisk pointer-/tekstinput, knapper, geometry, eksakte snapshotreferanser, ugyldig input, no-op/redo og valideringsoppdatering. Headless-testing følger [Avalonias dokumentasjon](https://docs.avaloniaui.net/docs/testing/headless-xunit).

Native Windows-prosess/vindu ble startet og observert som responderende. Computer Use get_window_state ventet på egen apptillatelse og endte med «Computer Use app approval timed out». Native kontrollinput, skjermbilde og normal vinduslukking er derfor IKKE verifisert. Egen testprosess ble stoppet før rebuild for å frigjøre apphost; dette beviser ikke normal close. Ingen native Linux-GUI-smoke påstås. Review/merge avventer dette konkrete evidensgapet eller SUPERVISORs uttrykkelige vurdering.

Ingen lyd/playback/waveform/seek, acquisition/generation, filvelger/prosjektlagring/eksport, installer eller release inngår.

## Dependencies og notices

[Inventaret](GUI_01_DEPENDENCIES.json) registrerer 50 faktiske NuGet-arkiver fra begge nye lockfiler: eksakte versjoner, SHA-256, contentHash, kildereferanser, lisensfelt og notice-filhashes. Desktop: Avalonia.Desktop og Fluent 12.1.3, samme evaluerte komponentfamilie. Ingen fontpakke; systemfonter gjenbrukes. GUI-tester: Avalonia.Headless.XUnit 12.1.3 krever xUnit v3; bare det nye isolerte testprosjektet bruker xunit.v3 3.2.2. Eksisterende xUnit v2-suite og lockfiler er uendret.

Managed Avalonia/MicroCom/SkiaSharp/HarfBuzzSharp er MIT; testverktøy er MIT/Apache-2.0 med faktiske notices. ANGLEs konkrete LICENSE er BSD 3-clause-lignende og krever attribution/disclaimer. Native Skia/HarfBuzz-notices er kontrollert fra de faktiske nupkg-arkivene; de har egne tredjepartsvilkår og kan ikke merkes bare prosjekt-MIT. Ved senere redistribusjon skal eksakte LICENSE/NOTICE/THIRD-PARTY-NOTICES og relevante source-vilkår medfølge, også .NET-runtime. Dette checkpointet distribuerer ingen runtimepakke eller binærer. [Fase 1-lisensgrunnlag](PHASE1_LICENSES.md) og ADR-0002 består.
