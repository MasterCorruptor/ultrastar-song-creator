# GUI-02 – delt kontrakt og senere komponenteierskap

Status 2026-10-12: felles kontrakt67bf70e og A/B-komponentene er SUPERVISOR-godkjent. Sekvensiell [editor binding](GUI_02_EDITOR_BINDING.md) er sluttgodkjent og integrert via [PR13](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/13), merge f5132145c6466d6815fb6b4ee857c4e7c15702b3. Avsnittene nedenfor bevarer den opprinnelige kontraktens API og fileierskap. [Arbeidsordre](GUI_02_SHARED_WORK_ORDER.md), [aktiv plan](exec-plans/completed/gui-02-shared-contract.md), [PCM-kontrakt](AUDIO_PLAYBACK.md).

## Compilable felles grunnlag

Namespace UltraStar.SongCreator.Desktop.Timeline, fil src/UltraStar.SongCreator.Desktop/Timeline/TimelineMapping.cs:
```csharp
public readonly record struct TimelineSpan(double LeftX, double RightX) // Width = RightX - LeftX
public sealed class TimelineMapping
{
    public static TimelineMapping Demo { get; }
    public TimelineMapping(double songStartSeconds = 0, double songEndSeconds = 8,
        double originX = 75, double pixelsPerSecond = 110);
    public double SongStartSeconds { get; }
    public double SongEndSeconds { get; }
    public double OriginX { get; }
    public double PixelsPerSecond { get; }
    public double RightX { get; }
    public bool TrySongToX(double songSeconds, out double x);
    public bool TryXToSong(double x, out double songSeconds);
    public bool TrySongToSourceFrame(double songSeconds, double audioOffsetSeconds,
        int sampleRate, long frameCount, out long frame);
    public double? SnapshotToSongSeconds(PlaybackSnapshot snapshot, double audioOffsetSeconds);
    public bool TrySourceIntervalToX(long startFrame, long endFrame, int sampleRate,
        long frameCount, double audioOffsetSeconds, out TimelineSpan span);
}
```
Dette er faktisk kode, uten GUI/device/timer/fil-I/O. Immutable mapping deles av begge senere komponenter. Viewport er sangsekunder [start,end], start>=0 og end>start, origin>=0, scale>0; alle tall og total pixel-extent må være finite og representerbart positive. Ugyldig constructor kaster ArgumentException/ArgumentOutOfRangeException. Default er0–8s/75/110, right955, samme koordinater som GUI-01. Kontrollens demo-bredde er980, waveform-høyde120; scrollbar brukes fremfor skjult reskalering. X/Width er kontroll-lokale Avalonia logical pixels (DIP), ikke skjermkoordinater. Senere app-integrasjon må dele viewport/scroll-offset mellom waveform og notecanvas.

Try-metoder gir false ved ugyldige/non-finite/utenfor-grense-input; out-data brukes kun ved true. X/song-punkter inkluderer begge viewport-endepunkter. Frame/rate valideres mot eksisterende PCM-profil (8000–192000, 0–PcmClip.MaxSamples frames som øvre mono-grense). Alle frames er kilde-PCM, ikke ticks/MIDI/GUI-tid.

Song-tid = source-frame/sample-rate − AudioOffsetSeconds. Seek source = songtid + offset, finite og i [0,FrameCount/sampleRate] før avrunding, nærmeste frame med midpoint away from zero. Ingen clamping av ugyldig seek eller automatisk start. Source-endepunkt er gyldig og gir backend Ended; Play på endpoint forblir Ended, søk tilbake gir Paused, Stop går til0/Stopped. Seek mens Playing fortsetter Playing med ny source-posisjon; paused seek er fortsatt Paused. [Audio-01](AUDIO_PLAYBACK.md) eier disse state-overgangene.

SnapshotToSongSeconds gir null ved Faulted/Disposed/ukjent state, ugyldig rate/framebounds/non-finite offset. Valid source-position utenfor viewport (f.eks. negativ sangtid før start) bevares som finite song-position; playhead-X er null når TrySongToX avviser den. Offsetendring projiserer samme snapshot på nytt og søker aldri automatisk. Presenter må også kontrollere at snapshot.SampleRate/FrameCount matcher playback.Clip. Ingen PCM kopieres inn i Song/History, og offset lagres ikke i denne mappingen.

Waveform-buckets er half-open source-frameintervaller. TrySourceIntervalToX validerer 0<=start<end<=FrameCount, mapper begge ender med samme offset, klipper mot viewport og gir false for tom/utelukkende utenfor/berørende intervaller. Siste delvise bucket slutter ved eksakt FrameCount; tom envelope har ingen buckets. Min/max er allerede normaliserte amplituder over alle kanaler; ingen ny beregning eller rebinning per redraw/noteredigering.

## Worker A – waveform-visning (bare etter ny ordre)

Eksklusivt fileierskap:
- src/UltraStar.SongCreator.Desktop/Controls/WaveformControl.cs
- tests/UltraStar.SongCreator.Desktop.Tests/WaveformControlTests.cs

Namespace UltraStar.SongCreator.Desktop.Controls. Avtalt API som A implementerer selv:
```csharp
public sealed class WaveformControl : Avalonia.Controls.Control
{
    public WaveformControl(WaveformEnvelope envelope, TimelineMapping mapping,
        double audioOffsetSeconds = 0);
    public WaveformEnvelope Envelope { get; }
    public TimelineMapping Mapping { get; }
    public double AudioOffsetSeconds { get; set; }
    public double? PlayheadSongSeconds { get; set; }
}
```
Ingen dummyklasse opprettes i felles grunnlag. Envelope/Mapping lånes immutable; samme objekter gjenbrukes. Offset setter krever finite; playhead er null eller finite, utenfor viewport skjules. Endringer invalidaterer bare visningen. Envelope skal komme fra factory i Audio-01; avvis malformed envelope/ikke-finite amplituder eller framegrenser med ArgumentException, ingen delvis tegning. Tom envelope viser tom bølge uten exception. Width980/Height120 i demo. A viser waveform, og eventuelt playheadlinje fra mottatt Song-position; ingen egen klokke, timer, transport, device, seek-handler eller Song-mutasjon. Timer-/offsetwiring er senere integrasjon.

Acceptance A: headless kontroll/layout/rendering og envelope-/offset-/clipping-/empty-/playhead-data, samme min/max/source-time som delt mapping, ingen audio-device/native init og ingen PCM-kopiering. Bygg og A-tester skal fungere uten B-filer. Ikke endre EditorWindow/EditorApplication, presenter/panel, mapping, csproj/locks, felles tester eller dokumentasjon. Stopp ved verifisert/pushet komponentcheckpoint for review.

## Worker B – playback-presenter/panel (bare etter ny ordre)

Eksklusivt fileierskap:
- src/UltraStar.SongCreator.Desktop/Playback/PlaybackPresenter.cs (inkludert egen view-state nedenfor)
- src/UltraStar.SongCreator.Desktop/Playback/PlaybackPanel.cs
- tests/UltraStar.SongCreator.Desktop.Tests/PlaybackPanelTests.cs

Namespace UltraStar.SongCreator.Desktop.Playback. Avtalt API:
```csharp
public readonly record struct PlaybackViewState(PlaybackSnapshot Snapshot,
    double? SongPositionSeconds, double? PlayheadX,
    bool CanPlay, bool CanPause, bool CanStop, bool CanSeek, string? Error);
public sealed class PlaybackPresenter : IDisposable
{
    public PlaybackPresenter(IPcmPlayback playback, TimelineMapping mapping,
        double audioOffsetSeconds = 0);
    public TimelineMapping Mapping { get; }
    public double AudioOffsetSeconds { get; }
    public PlaybackViewState ViewState { get; }
    public event EventHandler? Changed;
    public void Refresh();
    public void SetAudioOffset(double seconds);
    public void Play(); public void Pause(); public void Stop();
    public bool SeekSongSeconds(double seconds);
    public bool SeekSongText(string? text);
}
public sealed class PlaybackPanel : Avalonia.Controls.UserControl, IDisposable
{
    public PlaybackPanel(PlaybackPresenter presenter);
    public PlaybackPresenter Presenter { get; }
}
```
B skaper ingen native backend. Presenter låner IPcmPlayback og immutable mapping; Refresh leser én atomisk Snapshot som fasit for alle view-data/knapper, ingen wall-clock-estimering. Changed publiseres bare på UI-tråden, ingen native callback-subscription. SetAudioOffset validerer finite, republiserer uten seek. Seek validerer med TrySongToSourceFrame og sender akkurat SeekFrame til backend; ugyldig request gir false/forståelig Error og ingen backend-command. Tekst tillater norsk komma/invariant punkt, avviser NaN/infinity og feil tekst. Andre backend-/disposefeil vises uten krasj eller mock-fallback; Faulted/Disposed/ugyldig snapshot gir nullposition/nullplayhead og alle transportknapper disabled.

For gyldig, ikke-tomt clip: CanPlay ved Stopped/Paused og PositionFrames<FrameCount; CanPause bare Playing; CanSeek ved Stopped/Playing/Paused/Ended. CanStop ved Playing/Paused/Ended eller PositionFrames>0 (også stopped etter seek). Empty clip har alle knapper disabled. Seek endpoint og EOS følger Audio-01, ingen skjult restart. Stopped/paused snapshot er stabilt selv når GUI-timer går.

Panel har knapper/name PlaybackPlayButton, PlaybackPauseButton, PlaybackStopButton, PlaybackSeekButton, TextBox PlaybackSeekInput, synlig song-position/state/feilmelding. UI-knapper bruker presenter, og en DispatcherTimer (demo16ms) kan bare kalle Refresh, aldri endre audio-posisjon. Start poll bare mens attached; stopp/unsubscribe ved detach/Dispose. Det er mulig å bruke Refresh direkte i headless-test, ingen fysisk device nødvendig.

Presenter.Dispose stopper egen view-publicering/markerer utilgjengelig men disposer ALDRI lånt backend. Panel.Dispose stopper timer/unsubscribe men disposer ALDRI presenter/backend. Senere app-eier oppretter backend/presenter/panel og lukker i rekkefølge panel→presenter→backend. Dette hindrer callback-use-after-free og skjult dobbel ownership. B endrer ikke A-kontroll/tester eller felles/EditorWindow/Application/csproj/locks/docs.

Acceptance B: headless klikking/tekst til virkelig PcmPlayback/History-uavhengig source-state, manuell Render→Refresh for faktisk fremgang; pause/seek/stop/EOS/null/error, offset/bounds/knappetilstand og detach/disposal uten å dispose lånt backend. Ingen audio-klokke genereres av GUI-timer. Bygg/B-tester uten A-filer; stopp ved verifisert/pushet checkpoint.

## Sekvensiell integrasjon og repositorygrenser

Bare integrasjonsansvarlig eier EditorWindow.cs, EditorApplication.cs, EditingSession.cs, DemoSong.cs, csproj/solution/locks, TimelineMapping.cs/TimelineMappingTests.cs, eksisterende felles test-appbuilder og docs/rotfiler. Ingen parallelle endringer der. Desktop→Audio-reference og to låste Desktop/Desktop.Tests-lockoppdateringer følger dette grunnlaget. Det finnes ingen ny ekstern packageversjon: Hexa.NET.MiniAudio/HexaGen er tidligere godkjente Audio-01-pakker som nå blir transitive Desktop-pakker. [Audio-inventar/notices](AUDIO_01_DEPENDENCIES.json) gjelder; historisk [GUI-01-inventar](GUI_01_DEPENDENCIES.json) bevares.

Senere integrasjon oppretter DemoPcm+envelope én gang, oppretter faktisk backend først ved app-integration med synlig initfeil, deler mapping/offset, binder presenter data til waveform/note-playhead og bevarer notevalg/MoveNote/undo-redo. PCM/envelope må ikke kopieres ved Song-snapshotendring. Native Windows/hørbarhet/close verifiseres da separat, ikke nå.

Foreslått isolasjon: A på work/gui-02-waveform, B på work/gui-02-transport, hver i egen arbeidsmappe/worktree fra nøyaktig samme SUPERVISOR-reviewede kontraktcommit. Disse opprettes IKKE nå. Hver kan bare legge til egne SDK-globbede .cs/testfiler; ingen felles prosjekt-/lockendringer. Supervisor må godkjenne kontrakt, samlet kvote og utsende separate begrensede ordre før start. Ved faktisk felles interfacebehov stoppes berørt del for kontraktreview, ingen stilltiende motstridende API. Etter komponentreview følger samlet sekvensiell build/test/app-wiring i egen ordre. Ingen merge i denne forberedelsen.
