# GUI-02 editor binding – checkpoint

Status: implementert på work/gui-02-editor-binding, avventer SUPERVISOR-review. Ingen main-merge. [Arbeidsordre](GUI_02_EDITOR_BINDING_WORK_ORDER.md), [aktiv rapport](exec-plans/active/gui-02-editor-binding.md), [felles kontrakt](GUI_02_SHARED_CONTRACT.md).

## Start og avgrensning

Fra repositoryroten: ./tools/desktop/Start-Desktop.ps1. Samme SDK/runtimekrav og exe-bane som [GUI-01-guiden](GUI_01_TEST_SURFACE.md). Startscriptet bygger gjeldende checkout; main gir fortsatt tidligere GUI-01. Den nye kandidaten har waveform over notene, Spill/Pause/Stopp, sangsekunder og Søk til tid, samt eksisterende notevalg/flytting/angre/gjør om.

App-start åpner faktisk miniaudio default-device for egen svak 8-sekunders demotone (220Hz, maksimal amplitude0,02). Lyd starter først med Spill. Ingen sangfil, decoder eller nettkilde er koblet til editoren. Init-/devicefeil vises; noteredigering er fortsatt tilgjengelig, uten skjult mock-backend.

Prøvescenario for senere native review: Spill, Pause, søk2,5, Spill, Stopp og søk7,5→Spill→slutt; observer samsvarende playhead over waveform/noter. Flytt Hei til2,5, Angre/Gjør om mens avspilling pågår. På smalt vindu scroller waveform og noter sammen. Lukk normalt. Disse native/hørbare scenarioene er ikke utført i denne ordren.

## Eierskap og klokke

EditorPlaybackSession caches DemoPcm og envelope en gang, bruker samme immutable mapping og eier backend→presenter→panel. EditorApplication bruker eksplisitt MiniAudioPlayback-factory ved faktisk app-start. Default EditorWindow(session) har ingen audio-init; testene injiserer eksplisitt managed PcmPlayback eller feilfixture. Ingen global mutable backend.

Waveform og notecanvas ligger i samme980-DIP stack/scroll-container. Noter/ticks og begge playheads bruker samme mapping; source-sekunder minus Song.AudioOffsetSeconds. Presenter.Changed oppdaterer waveform og notelinje; null/offviewport skjules. Timeren leser bare snapshot og estimerer aldri lydtid. Envelope/PCM gjenbrukes ved redraw/valg/MoveNote/undo/redo, og lagres aldri i Song.

Normal close/app-exit bruker idempotent ReleasePlayback: unsubscribe vindu/session, panel.Dispose (timerstop/unsubscribe), presenter.Dispose, backend.Dispose. Ved delvis initfeil ryddes returnert backend; MiniAudioPlayback rydder sin egen feilende native constructor. Backend opprettes aldri av testconstructor.

## Evidens og grenser

Samlet Windows locked restore, Release solution build uten warnings/errors og490/490 tester: Core422 + Audio17 + Desktop51. Desktop51 inkluderer10 GUI-01,13 mapping,8 waveform,14 transport og6 bindingtester. Reelle headless pointer-/teksthandlinger bekrefter render→refresh, pause/seek/stop/EOS, to like playhead-X, signed offset/preroll, delt scroll/alignment, noteoperasjon/undo/redo, cache identity, initfeil og close/ingen videre polling/disposalrekkefølge.

Samlet solution-formatkontroll avdekket fem whitespaceavvik i B-panelets switch-armer; kun formatering er korrigert uten API-/adferdsendring. verify-no-changes kontrolleres før commit. Ingen dependency-/lock-/Core-/Audio-/master-/ADR-endring.

Headless gir ingen ny native GUI-/hørbarhet-/latencyakseptanse. Ingen ny Linux-/tone-/CU-test er kjørt. Ingen alpha/beta/installer/release eller generell filåpning/lagring/eksportflyt i GUI. Tidligere native Audio-01 og manuell GUI-01-evidens dekker ikke automatisk denne kombinerte kandidaten.


## Verifikasjonsoppfølging 2026-10-13

[Native rapport og manuell sluttest](GUI_02_NATIVE_VERIFICATION.md): faktisk Windows-vindu/lydinit og normal Alt+F4-prosessavslutning er observert. Skjermfangst feilet med FrameArrived timeout; knappe-/visuell/hørbar samlet acceptance gjenstår. Ny samlet nettisolert Linux Release-build/locked restore490/490 består. Tidligere avsnitt beskriver opprinnelig bindingordre; denne oppfølgingen gir fortsatt ingen fysisk Linux-device eller full prototypemilepæl.
