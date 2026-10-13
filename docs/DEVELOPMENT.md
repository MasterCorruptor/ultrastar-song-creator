# Utviklings- og repositoryprinsipper

Dette dokumentet beskriver det etablerte Fase 0-grunnlaget, den avsluttede Fase 1-evalueringen og aksepterte teknologivalg. Kravene følger [PROJECT_MASTER.md](PROJECT_MASTER.md), særlig §15–19. Ved uløste konflikter mellom lokale autoritative dokumenter skal berørt arbeid stoppes og rapporteres.

## Autoritet og synkronisering

Den lokale autoritative working tree ligger på prosjekteierens primære utviklingsmaskin i `G:\ChatGPT - Prosjektmappe\ultrastar-song-creator`.

Autoritetsrekkefølgen mellom prosjektkopier er:

1. Lokal autoritativ working tree.
2. GitHub `main` / `origin/main`, den høyeste delte og synkroniserte autoriteten.
3. Andre stabile commits eller referansebrancher.
4. WIP-/arbeidsbrancher, med autoritet kun innenfor sin eksplisitte arbeidsordre.

Bevar lokale autoritative endringer hvis GitHub avviker; ikke overskriv dem med en fjernkopi. Gjør relevante avvik synlige. Filenes tidsstempler avgjør ikke autoritet. Ønsket normaltilstand er lokal godkjent endring → commit → push → GitHub `main`.

## Brancher og integrasjon

`main` er stabil integrasjonsbranch. Første Fase 0-commit etablerer denne branchen. Senere ikke-trivielt arbeid utføres normalt på avgrensede `work/<oppgave>`-brancher. Commit ved naturlige, konsistente kontrollpunkter og bruk arbeidsbrancher som redundans når det er praktisk.

Integrer først når oppgavens acceptance criteria, relevante tester/kontroller og eventuell påkrevd menneskelig godkjenning er oppfylt. Praktisk brukertesting legges til naturlige kontrollpunkter; det kreves ikke ny godkjenning for hvert lite teknisk steg. Ikke bruk destruktiv synkronisering for å skjule avvik.

## Prosjektkunnskap og lokal arbeidstilstand

Masterspesifikasjon, dokumentasjon, relevante ADR-er, kode, tester og reproducerbare verktøy versjonskontrolleres. `docs/decisions/` reserveres for dokumenterte beslutninger; ingen produksjonsstack er besluttet i Fase 0.

`docs/exec-plans/active/` og `completed/` brukes bare for planer med verdi på tvers av arbeidsøkter eller varig gjennomføringshistorikk. Kortvarige planer, prompts, logger og scratch-data hører hjemme i Git-ignorert `.agent-local/`. Flytt nødvendig varig kunnskap til repositoryet før lokale arbeidsfiler disponeres.

Den innsendte `Master-plan-for-prosjektet-v0.5(1).txt` er bevart lokalt og ignorert av Git. `docs/PROJECT_MASTER.md` er den uendrede, versjonskontrollerte masterkopien.

## Miljø og utviklingskonvensjoner

Dagens minimum er Git. GitHub CLI brukes til repository-opprettelse og synkronisering. C#/.NET 10 LTS + Avalonia med separat Python-analyse er godkjent i ADR-0001. Core bygges nå med .NET 10; global.json velger SDK 10.0.401 med latestPatch roll-forward innen samme feature band. Runtimepatch følger valgt SDK for testkjøringen; ingen egen produkt-runtimepakke er distribuert. Se kommandoene nedenfor og [testdependencyinventaret](CORE_DEPENDENCIES.md). GUI-01 er integrert; øvrige runtime-/modellpakker er ikke implementert.

Fase 1 har evaluert gjenbruk, Windows/Linux-støtte, kvalitet, integrerbarhet og lisens og er avsluttet 2026-10-08. Prosjekteieren godkjenner vesentlige teknologivalg. Når produktimplementering bestilles, dokumenteres faktiske runtime-versjoner, installasjon, eksterne verktøy/modeller og build-/testkommandoer. Lås eller avgrens versjoner når reproduksjon krever det.

Følg etablerte konvensjoner for valgt språk og rammeverk. Ta i bruk modne formatterings-, lint- og typekontrollverktøy når de passer stacken. Legg bare til nødvendige dependencies etter evaluering. Hold midlertidig debugging ute av produksjonskode, oppdater dokumentasjon ved endringer og skriv meningsfulle reproducerbare tester og regression-tester.

## Verifikasjon i Fase 0

Kontroller den påkrevde strukturen og at lokale dokumentlenker finnes. Bekreft at masterkopien er byteidentisk med kilden, også i første commit. Git er konfigurert lokalt med `core.autocrlf=false` for å bevare kildeinnholdet.

Før commit brukes `git diff --cached --check` på de nye grunnlagsfilene. Etter commit kontrolleres ren working tree, branch `main`, upstream `origin/main` og samsvar mellom lokal og ekstern commit. Masterspesifikasjonen beholdes uendret også dersom den inneholder eksisterende formatteringsavvik.

Core build og domenetester er etablert i Fase 2.1. Brukerens arbeidsordre av 2026-10-07 åpnet Fase 1-kartlegging og avgrensede PoC-er. Prosjekteieren godkjente resultatene og avsluttet Fase 1 2026-10-08. Prosjekteieren bestilte Fase 2.1 2026-10-08 gjennom [arbeidsordren](PHASE2_FIRST_WORK_ORDER.md). Ytterligere deloppgaver krever egne avgrensede bestillinger.

## Evaluering i Fase 1

Se [auditten](PHASE1_AUDIT.md), [måleresultatene](PHASE1_RESULTS.md) og [reproduksjonsguiden](../tools/phase1/README.md). Python-venv, SDK, modeller og cacher ligger under `.agent-local/`. Varige probe-kilder, kandidat-/commitreferanser, låste evalueringsversjoner og utvalgte måleresultater versjonskontrolleres under `tools/phase1/`. Dette er ikke produksjonsdependencies eller valgte produktkommandoer.

Foreslåtte ADR-er kan committes som reviewgrunnlag, men blir ikke aksepterte teknologivalg gjennom commit/push alene. Masteren §15.2 og §18.2 regulerer godkjenning. Prosjekteieren har eksplisitt godkjent ADR-0001/0002 og ansett den dokumenterte evidensen og avgrensningene som tilstrekkelige til å avslutte Fase 1. Planen ligger i [completed/](exec-plans/completed/phase1-feasibility-audit.md); resterende kvalitets-/releasearbeid er ikke automatisk utført gjennom godkjenningen.

## Bekreftet lisensstrategi

MIT er bekreftet for egen prosjektkode gjennom prosjekteierens vedtak 2026-10-08 i [ADR-0002](decisions/ADR-0002-licensing-and-reuse.md). Separate LGPL-codecs tillates med dokumenterte source/notices/utskiftbarhetskrav; hver releaseartefakt må vurderes. Nye dependencies må vurderes separat mot prosjektets permissive mål; vesentlig andre lisens- eller distribusjonskrav krever eksplisitt prosjektvedtak.

## Core build og test – Fase 2.1

Kjør fra prosjektroten med .NET 10 SDK. global.json tillater ikke prerelease eller et annet feature band. Solution inneholder Core, Projects, UltraStar og Core.Tests; Phase 1-prober bygges separat. Restore har nettbehov første gang; selve build/test krever ingen konto, modeller, medier eller nettverksbruk fra domenet.

```text
dotnet restore UltraStar.SongCreator.slnx --locked-mode
dotnet build UltraStar.SongCreator.slnx -c Release --no-restore
dotnet test UltraStar.SongCreator.slnx -c Release --no-build --no-restore
dotnet format UltraStar.SongCreator.slnx --no-restore --verify-no-changes
```

Eksakt brukt SDK: 10.0.401; direkte testpakker: xUnit 2.9.3, runner 3.1.4 og Microsoft.NET.Test.Sdk 17.14.1. Alle fire prosjekter har lockfil. Core, Projects og UltraStar har ingen eksterne PackageReference. Kompilatoradvarsler behandles som feil. .NET-formatverktøyet fra SDK brukes; ingen separat formatter dependency.

Lokalt ble SDK-en under .agent-local/phase1/dotnet gjenbrukt ved å erstatte dotnet med den kjørbare filen og sette DOTNET_CLI_HOME/NUGET_PACKAGES til prosjektets ignorerte cachemapper. Dette er en valgfri lokal oppsettrute, ikke en forutsetning for en ny maskin.

Windows: locked restore, Release build og 55/55 tester bestod uten advarsler. Linux: samme SDK og kilde i Ubuntu-container med nettverket slått av, eksisterende NuGet-cache og egne artifacts under .agent-local/phase2; 55/55 tester bestod. Audit ble deaktivert bare for den nettisolerte restore-prøven fordi vulnerability-listen er en nettressurs; ordinær restore/CI beholder audit.

En ferdig [GitHub Actions-mal](../tools/ci/core.yml) beskriver locked restore/build/test/format på Windows og Ubuntu, med actions pinnet til commit-SHA. Den er ikke aktiv: GitHub avviste workflow-push fordi dagens tilgang mangler workflow-rettighet. Aktiver senere ved å kopiere malen til .github/workflows/core.yml og publisere med autorisert workflow-tilgang. Lokale Windows/Linux-prøver bestod; ingen CI-resultat er påstått. Core-CI verifiserer ikke native GUI/audio eller en installer.

[Core-kontrakten](CORE_DOMAIN.md) beskriver tid/pitch, snapshots, referanser og command-policy. Fase 2.2 implementerer versjonert prosjektlagring. Fase 2.3 legger til importeren nedenfor. Fase 2.4 implementerer writer/pakking etter eksportkontrakten nedenfor.

## Prosjektlagring – Fase 2.2

Den historiske kontrakten [format v1](PROJECT_FORMAT.md) bruker UTF-8 JSON og anbefalt filendelse .uscproject. API-en er ProjectStore.SaveAsync(path, song, cancellationToken) og LoadAsync(path, cancellationToken) i UltraStar.SongCreator.Projects. Load returnerer Song, schemaVersion, absolutt prosjektfilbane og validatorfunn. ProjectFormatException skiller invalid JSON/envelope/song, unsupported version og too-large; vanlige fil-/tilgangsfeil beholder .NETs I/O-exceptions.

Core holdes uavhengig av JSON/fil-I/O. Projects har bare en ProjectReference til Core; eksterne runtime-/testpakker er uendret. Bygg/test/format med de samme solution-kommandoene ovenfor. .editorconfig sikrer UTF-8/LF og fire spaces for C#; masteren formatteres ikke.

106/106 tester bestod på Windows og nettisolert Linux-container med SDK 10.0.401. Dette inkluderer tidligere 55 domenetester og 51 lagrings-/robusthetstester, med en håndskrevet v1-fixture, redigeringsrundtur, full felt-/referansebevaring, Unicode/kultur, schema/version-feil, grenser, cancellation og filbevaring/opprydding ved feil. Locked restore, Release build uten advarsler og formatkontroll bestod. Testfiler ligger under .agent-local/project-storage-tests og ryddes etter testene.

Filer og referanser gjenåpnes uten lyd, modeller eller inferens. Prosjektlageret kopierer/henter ikke medier og har ikke GUI eller persistent undo-historikk; Fase 2.4 legger til separat eksportpakker. v1 har ingen eldre støtteversjon. Fase 2.3 innfører [v2](PROJECT_FORMAT_V2.md) med eksplisitt v1-migrasjon. Standard filgrense er 64 MiB. Se formatdokumentet for rename-/cancellation-/krasjavgrensninger.

## UltraStar-import og prosjektformat v2 – Fase 2.3

UltraStarImporter.Parse(text, sourceFileReference?) og LoadAsync(path, cancellationToken) returnerer et komplett Song-utkast med diagnostikk eller null Song ved formatfeil. [Importprofilen](ULTRASTAR_IMPORT.md) beskriver beat/pitch/offset, encoding, kildemetadata og avgrensninger. Kilden beholdes i Core som generisk SourceDocument; ingen formatparser eller fil-I/O flyttes inn i Core. Projects skriver nå v2 med original metadata, og laster fortsatt v1 uten å endre originalfilen.

203/203 tester bestod på Windows og nettisolert Linux med SDK 10.0.401: de tidligere 106, åtte metadata-/migrasjonstester, 66 absolutte importtester og 23 relative tester. Locked restore, Release build uten warnings og formatkontroll bestod. Linux brukte samme kilde, separat artifacts under .agent-local/phase2-import, eksisterende NuGet-cache og audit deaktivert kun under offline restore. Ingen hosted CI påstås; malen er oppdatert for den nye lockfilen.

Alle fixtures er håndskrevne syntetiske data. Load innhenter verken media/metadata eller modeller. Normale encodingfeil avvises uten gjetting; fil-I/O/cancellation bruker vanlige .NET exceptions. Relativ legacy-modus er avklart og implementert etter [USDX-profilen](RELATIVE_TIMING.md), med eksplisitt kompatibilitets-warning og akkumulerte offset-/tidsgrenser. Relativ tid i v1 avvises. [Planen](exec-plans/completed/phase2-ultrastar-import.md) er avsluttet, og PR #4 er klargjort for review. Ingen eksport/GUI eller automatisk merge inngår.

UltraStar-fixtures har betydningsfulle spaces på slutten av notetekst. .gitattributes unntar bare blank-at-eol for disse .txt-fixturene; øvrig whitespacekontroll beholdes, og testene kontrollerer tekstbevaring.

## UltraStar writer og komplett sangpakke – Fase 2.4

[Kontrakten](ULTRASTAR_EXPORT.md) har Render og PackageAsync med eksplisitt unversionert/v1-profil, absolutt tid, automatisk endpoint-avrunding og per-note-deltas. Kildens kvantiserings-BPM kan gjenbrukes; for en ny sang gis grid eksplisitt. Current song overrides operative source metadata uten å mutere prosjektet. Ingen ny runtimepakke eller prosjektversjon.

PackageAsync lager en ny mappe med song.txt/media, rebased relative references, portable/collision-safe navn og SHA-/byteverifiserte kopier. Current main audio/video og oppgitte cover/background/vocals/instrumental pakkes. Originaler beholdes og eksisterende mål overskrives ikke. Referansebaser, lokale paths, staging/rename, cancellation og avgrensninger er eksplisitte i kontrakten.

263/263 tester bestod på Windows og nettisolert Linux med SDK 10.0.401: 203 tidligere + 38 writer + 22 pakke. Locked restore, Release build uten warnings og formatkontroll bestod. Linux bruker artifacts under .agent-local/phase2-export og samme offline-policy som tidligere. Praktisk Windows-smoke med gyldig syntetisk WAV/PNG/MPEG4-pakke bestod reimport, checksums og FFmpeg-dekoding; se [resultater](PHASE2_EXPORT_RESULTS.md). Ingen karaokeapp/GUI/native product playback påstås.

Core/Projects/master/aksepterte ADR-er og historisk v1-fixture er byteuendret. Ingen hosted CI er aktivert; dagens mal dekker hele solution uten ekstra prosjekt/lockfil. [Planen](exec-plans/completed/phase2-ultrastar-export.md) er avsluttet. Ingen automatisk merge eller ny fase.

## Samlet integrasjon – 2026-10-09

Prosjekteieren bestilte gjennomgang/integrasjon av PR #1–#5. Disse er merget til main i rekkefølge med bevart commit-historikk. Samlet kandidat har 265/265 beståtte tester på Windows og nettisolert Linux, locked restore, Release build uten advarsler og formatkontroll. Ett filnavn-portabilitetsfunn ble rettet og regresjonstestet i PR #5 før merge.

[Integrasjonsrapporten](INTEGRATION_RESULTS.md) dokumenterer commits, review, integritet og gjeldende status. Tidligere avsnitt og arbeidsplaner beskriver historiske kontrollpunkter. Master/aksepterte ADR-er og v1-fixture er bevart. Ingen hosted CI, produktinstaller eller ny fase er startet; neste arbeidsordre avgrenses separat.

## Lokal mediekilde – Fase 3.1

Arbeidsordren av 2026-10-11 legger til Acquisition, IMediaSource/IMediaProbe, lokal source og separat FFprobe-inspeksjon. Prosjekteieren valgte referanser til originalfilen og filmetadata med manuelle overrides. [Kontrakten](LOCAL_MEDIA.md) beskriver normalisering, sporvalg, lagret proveniens og eksportgrensene; [teststatus](TESTING_STATUS.md) forklarer hvorfor det ennå ikke finnes en alpha/beta-app.

Solution har fire produktbiblioteker (Core/Projects/UltraStar/Acquisition), Core.Tests og MediaProbe.TestHost. Alle seks har lockfiler. Ingen ny ekstern NuGet-pakke eller schemaVersion. Testhost er bare egen child-prosess-fixture; ikke et produktprogram.

308/308 tester bestod på Windows og nettisolert Linux med SDK 10.0.401: tidligere 265 og 43 nye. Locked restore, Release build uten advarsler og formatkontroll bestod. Prosessprøver bruker faktiske .NET-child-prosesser for argumenter, stdout/stderr, outputgrenser, invalid UTF-8, timeout og cancellation. Linux bruker samme bindede kilde og cache med egne artifacts; audit er deaktivert kun under offline restore.

Praktisk Windows-smoke med den eksisterende minimale FFprobe 8.1.3-builden bestod for egen 2 s PCM-WAV og MPEG4/AAC-video: tags, varighet, manuell tittel, prosjekt-save/load, senere syntetisk note og pakettrundtur. En egen to-spors M4A bekreftet eksplisitt sporvalg og avvisning av tapsfull original-fileksport. Filene forble byteuendret. Ingen media/binærer/cacher eller maskinvareinformasjon publiseres.

Linux-testene verifiserer normalisering/fil-I/O og den faktiske prosessgrensen med egen testhost; selve FFprobe-smoken i dette trinnet er kjørt på Windows. Ingen Linux-codec-/playback-/GUI-/installerakseptanse påstås. CI-malen inkluderer de nye lockfilene, men er fortsatt ikke aktivert. Ingen nettsøk/acquisition-provider, lyrics, analyse, GUI eller videre deloppgave er implementert.

## Innspillingssøk – Fase 3.2

Bestilt/avklart 2026-10-12: MusicBrainz og eksplisitt recording-/release-valg først, med mediesøk/nedlasting som senere leveranse. [Kontrakten](RECORDING_SEARCH.md) beskriver API, HTTP-grense/rate, normalisering, source-choice-v1 og datavilkår. Valget kan undo/redo-bindes til lokal source og lagres/gjenåpnes uten nett eller metadataoverskriving.

360/360 tester bestod på Windows og nettisolert Linux med SDK 10.0.401 (308 tidligere + 52 nye): query/Unicode, core-kandidater, HTTP-status/body/timeout/cancellation, felles throttle/cooldown, eksplisitt valg, frozen persistence, save/load, undo/redo og eksportens current metadata. Locked restore, Release build uten advarsler og formatkontroll bestod. Linux brukte eksisterende cache, egne artifacts og audit deaktivert kun under offline restore.

En avgrenset live MusicBrainz-prøve av artist+tittel bestod, inkludert offline valgt-ID-rundtur; detaljer/begrensninger står i kontrakten. Ingen new dependency/lockfil/schema, Core-/Projects-/UltraStar-/ADR-/masterendring. Hosted CI og acquisition-GUI er fortsatt ikke etablert.

## Nettmedia – Fase 3.3 integrert

Fase 3.3 er integrert via [PR #9](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/9) etter SUPERVISOR-godkjenning og CONTINUE 2026-10-12. [Kontrakten](ONLINE_MEDIA.md) og [checkpointet](exec-plans/completed/phase3-media-download.md) beskriver acceptance/evidens. [SUPERVISOR-policy](SUPERVISOR_POLICY.md) dokumenterer eiermandatet, sekvensiell/parallel arbeidsfordeling, modell-/kvotepolicy og rapport-/reviewkrav.

Konfigurer en egen Python 3.12-venv og installer bare hashlåste wheels:
```powershell
python -m venv .agent-local/media-dev
.agent-local/media-dev/Scripts/python.exe -m pip install --no-deps --require-hashes -r tools/acquisition/requirements-media.txt
```
Dette er en utviklingsrute på Windows; Linux-venv bruker bin/python. Produktet installerer ingen verktøy i dette checkpointet. Konfigurer en kompatibel lokal Node/Deno-runtime og eksisterende egnet FFprobe via absolutte baner. Windows-nettprøven brukte Node 24.19.0, EJS 0.8.0 og minimal FFprobe 8.1.3; ingen ny runtimepakke vedtas/distribueres. Actual wheel-hashes/notices og avgrensninger står i [inventaret](../tools/acquisition/dependencies.json).

API-sekvens: SearchAsync → brukerens eksplisitte source-URL → ResolveAsync → MediaDownloadChoices.Select med konkrete format-ID-er → DownloadAsync til ny mappe → DownloadedMediaDraft.Create med eventuelle manuelle overrides/eksplisitt recording choice → ProjectStore. Ikke velg første kandidat/format skjult, og ikke anta at YouTube-uploader er artist. Ingen GUI inngår.

414/414 tester bestod på Windows og nettisolert Linux med SDK 10.0.401 (360 tidligere + 54 nye), locked restore og Release build uten advarsler. SDK format/verify-no-changes og integritets-/inventar-/lenke-/whitespacekontroll bestod. Linux brukte egen artifacts og audit av kun ved offline restore; faktisk ekstern provider-runtime/nettmedia ble prøvd på Windows.

Windows faktisk adapterprøve lastet eksplisitte originale audio-/videoformater, inspiserte/hashkontrollerte dem, bevarte eiermetadata gjennom offline save/load og pakket to eksportassets. Privat smoke har egen syntetisk note for eksport; den er ingen sanggenerering, produktdefault eller kvalitets-/tilgjengelighetsgaranti. Private medier, rå data/signed URLs, runtime/binærer og konto-/maskindata publiseres ikke.

CI-malen er fortsatt ikke aktivert. Den historiske workflow-rettighetsblokkeringen er løst, men aktivering er en egen avgrenset oppgave og ikke del av Fase 3.3.

Fase 3.3 REQUEST_FIX literal-paths: native staging-cwd og faste relative --output-navn erstatter rå absolutt output/--paths. 422/422 tester består på begge OS, med åtte native foreldresti-/cleanup-regresjoner. tools/acquisition/test_literal_paths.py validerer faktisk pinnet yt-dlp-template/path-oppførsel uten nett (venv-python på Windows, hashkontrollert wheel-import på Linux) og gjenskaper opprinnelig feil. Ingen nye pakker/nettmedia-smoke. Sti-rettingen er godkjent og inngår i integrasjonen. Kode/tests/tools er byteidentiske med verifisert 71afe4d; dokumentasjons-/mergeendringer alene begrunner ingen gjentakelse av testsett eller nettprober.

## GUI-01 integrert

[Startguide/kontrakt](GUI_01_TEST_SURFACE.md), [arbeidsordre](GUI_01_WORK_ORDER.md), [avsluttet plan](exec-plans/completed/gui-01-song-editing.md) og [faktisk pakkeinventar](GUI_01_DEPENDENCIES.json). Desktop og isolert Desktop.Tests er lagt til solution. Windows/Linux locked restore/Release build/test består (432 totalt); SDK-format består. Native Windows-scenarioene, inklusive input og normal close, er manuelt bekreftet bestått av prosjekteieren. Computer Use-fangst feilet; skjermbilde mangler. SUPERVISOR har godkjent acceptance, og GUI-01 integreres via [PR #10](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/10). Ingen videre blokk starter her.

## Audio-01 integrert

[Arbeidsordre](AUDIO_01_WORK_ORDER.md), [kontrakt/reproduksjon/evidens](AUDIO_PLAYBACK.md), [avsluttet plan](exec-plans/completed/audio-01-pcm-playback.md) og [pakke/native/notices-inventar](AUDIO_01_DEPENDENCIES.json). Audio har ingen Core-/GUI-/fil-I/O-reference; Audio.Tests og AudioProbe er egne solutionprosjekter. 449 Windows/Linux-tester og native Windows-default-device-smoke består. Linux fysisk device og akustisk hørbarhet/latency er ikke verifisert. Native callbackformat/null-backend/lifecycle kontrolleres; GUI-01 og lagringskontrakter er bevart. Audio-01 er godkjent og integrert via [PR #11](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/11). GUI-binding starter først etter ny separat ordre.

## GUI-02 samlet editor-binding – integrert

[Kontrakt/start/evidens](GUI_02_EDITOR_BINDING.md), [arbeidsordre](GUI_02_EDITOR_BINDING_WORK_ORDER.md) og [avsluttet plan](exec-plans/completed/gui-02-editor-binding.md). Godkjente A/B-komponenter er samlet sekvensielt; appwiring eier cached PCM/envelope, miniaudio, presenter/panel og close-opprydding. Samlet Windows locked restore/Release build490tester består. Senere samlet Linux490/490 og seks manuelle native Windows-scenarioer bestod. SUPERVISOR-sluttgodkjent og merget via PR13 som f5132145c6466d6815fb6b4ee857c4e7c15702b3. Ingen fysisk Linux-device, målt akustisk latency eller release.
