# Hva kan testes nå?

Status 2026-10-12: ingen alpha-/beta eller installer. Første avgrensede GUI-01 testflate finnes på arbeidsbranch før review; [startguide og begrensninger](GUI_01_TEST_SURFACE.md). Solution bygger nå også Desktop.exe. Native input og normal close er manuelt verifisert av prosjekteieren med forventet resultat; endelig review gjenstår.

## Dagens funksjoner

- Sangmodell, validering og MoveNote med undo/redo.
- Prosjektlagring/gjenåpning, UltraStar-import og eksport med komplett sangmappe.
- Fase 3.1: lokal filinspeksjon, metadata/manuelle overrides, stream-valg og lagret proveniens.
- Fase 3.3 integrert: YouTube-søk, eksplisitt formatvalg og lokal originalnedlasting etter [kontrakten](ONLINE_MEDIA.md).
- Fase 3.2: valgfritt MusicBrainz-søk og eksplisitt innspillingsvalg etter [kontrakten](RECORDING_SEARCH.md), med offline lagring.

Kjernemodulene har ennå ingen filvelger, menyer eller playbackknapper. GUI-01 gir visuell flytting med undo/redo av egen testsang; acquisition og prosjektfil-I/O er fortsatt API-er.

## Teknisk kontroll

Fra prosjektroten med .NET 10 SDK, se [README](../README.md) og [utviklingsguiden](DEVELOPMENT.md):

```text
dotnet restore UltraStar.SongCreator.slnx --locked-mode
dotnet build UltraStar.SongCreator.slnx -c Release --no-restore
dotnet test UltraStar.SongCreator.slnx -c Release --no-build --no-restore
```

Testene trenger ikke FFprobe, Python, modeller eller sangfiler. tests/MediaProbe.TestHost er en liten egen child-prosess-fixture og bare testutstyr. Faktisk medieinspeksjon via [API-en](LOCAL_MEDIA.md) krever eksplisitt konfigurert egnet FFprobe.

## Tidligere visuelle prøver

Fase 1 laget tekniske tidslinje-/playback-prober. TimelineProbe rendrer syntetiske noteblokker til bilder uten et interaktivt produktvindu. NativeProbe har et avgrenset testvindu, kjører automatisk playback/seek/loop og avsluttes. Lokale exe-filer under ignorerte evalueringsmapper er prøver/verktøy/runtimes, ikke en alpha-editor.

Den lokale illustrasjonen .agent-local/phase1/timeline-results/timeline-0-8.png viser bare et tidligere renderingsforsøk, ikke gjeldende produktdesign, komplette menyer eller arbeidsflyt. [Fase 1-guiden](../tools/phase1/README.md) beskriver tekniske reproduksjonsprøver.

## Første praktiske brukertest

En meningsfull alpha trenger en sammenhengende brukerflate: åpne/opprette prosjekt, vise sangdata, gjøre en avgrenset redigering, lagre og eksportere med forståelige feil. Playback/timeline kommer som avgrenset editorarbeid. Masterplanen legger Acquisition i Fase 3, Generation i Fase 4 og editor i Fase 5. En tidligere testflate er et eget prioriteringsvalg for prosjekteieren.

Fase 3.1–3.3 har ingen GUI; separat GUI-01 finnes nå på arbeidsbranch. Ingen alpha-distribusjon.

Prosjekteieren har prioritert en tidlig fungerende Windows GUI-prototype. GUI-01 er bestilt separat etter det godkjente/integrerte [Fase 3.3-checkpointet](exec-plans/completed/phase3-media-download.md) etter [SUPERVISOR-policyen](SUPERVISOR_POLICY.md); se GUI-01-startguiden for faktisk checkpointstatus.
