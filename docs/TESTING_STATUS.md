# Hva kan testes nå?

Status 2026-10-12: det finnes ingen alpha-/betaversjon av UltraStar Song Creator med brukergrensesnitt og ingen produkt-.exe/installer. Dagens implementering er kjernemoduler og Acquisition-API. Å bygge solution lager biblioteker og testverktøy.

## Dagens funksjoner

- Sangmodell, validering og MoveNote med undo/redo.
- Prosjektlagring/gjenåpning, UltraStar-import og eksport med komplett sangmappe.
- Fase 3.1: lokal filinspeksjon, metadata/manuelle overrides, stream-valg og lagret proveniens.
- Fase 3.2: valgfritt MusicBrainz-søk og eksplisitt innspillingsvalg etter [kontrakten](RECORDING_SEARCH.md), med offline lagring.

Disse har ennå ingen filvelger, menyer, playbackknapper eller visuell redigering. Manuell overstyring skjer via API.

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

Ingen GUI eller alpha-distribusjon er startet gjennom Fase 3.1/3.2.
