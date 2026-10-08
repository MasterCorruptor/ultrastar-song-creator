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

Dagens minimum er Git. GitHub CLI brukes til repository-opprettelse og synkronisering. C#/.NET 10 LTS + Avalonia med separat Python-analyse er godkjent i ADR-0001. Core bygges nå med .NET 10; global.json velger SDK 10.0.401 med latestPatch roll-forward innen samme feature band. Runtimepatch følger valgt SDK for testkjøringen; ingen egen produkt-runtimepakke er distribuert. Se kommandoene nedenfor og [testdependencyinventaret](CORE_DEPENDENCIES.md). GUI og øvrige runtime-/modellpakker er ikke implementert.

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

Kjør fra prosjektroten med .NET 10 SDK. global.json tillater ikke prerelease eller et annet feature band. Solution inneholder Core, Projects og Core.Tests; Phase 1-prober bygges separat. Restore har nettbehov første gang; selve build/test krever ingen konto, modeller, medier eller nettverksbruk fra domenet.

```text
dotnet restore UltraStar.SongCreator.slnx --locked-mode
dotnet build UltraStar.SongCreator.slnx -c Release --no-restore
dotnet test UltraStar.SongCreator.slnx -c Release --no-build --no-restore
dotnet format UltraStar.SongCreator.slnx --no-restore --verify-no-changes
```

Eksakt brukt SDK: 10.0.401; direkte testpakker: xUnit 2.9.3, runner 3.1.4 og Microsoft.NET.Test.Sdk 17.14.1. Begge prosjekter har lockfil. Core har ingen eksterne PackageReference. Kompilatoradvarsler behandles som feil. .NET-formatverktøyet fra SDK brukes; ingen separat formatter dependency.

Lokalt ble SDK-en under .agent-local/phase1/dotnet gjenbrukt ved å erstatte dotnet med den kjørbare filen og sette DOTNET_CLI_HOME/NUGET_PACKAGES til prosjektets ignorerte cachemapper. Dette er en valgfri lokal oppsettrute, ikke en forutsetning for en ny maskin.

Windows: locked restore, Release build og 55/55 tester bestod uten advarsler. Linux: samme SDK og kilde i Ubuntu-container med nettverket slått av, eksisterende NuGet-cache og egne artifacts under .agent-local/phase2; 55/55 tester bestod. Audit ble deaktivert bare for den nettisolerte restore-prøven fordi vulnerability-listen er en nettressurs; ordinær restore/CI beholder audit.

En ferdig [GitHub Actions-mal](../tools/ci/core.yml) beskriver locked restore/build/test/format på Windows og Ubuntu, med actions pinnet til commit-SHA. Den er ikke aktiv: GitHub avviste workflow-push fordi dagens tilgang mangler workflow-rettighet. Aktiver senere ved å kopiere malen til .github/workflows/core.yml og publisere med autorisert workflow-tilgang. Lokale Windows/Linux-prøver bestod; ingen CI-resultat er påstått. Core-CI verifiserer ikke native GUI/audio eller en installer.

[Core-kontrakten](CORE_DOMAIN.md) beskriver tid/pitch, snapshots, referanser og command-policy. Fase 2.2 implementerer versjonert prosjektlagring. UltraStar parser/writer og eksport er neste mulige deloppgaver og er ikke implementert.

## Prosjektlagring – Fase 2.2

[Format v1](PROJECT_FORMAT.md) bruker UTF-8 JSON og anbefalt filendelse .uscproject. API-en er ProjectStore.SaveAsync(path, song, cancellationToken) og LoadAsync(path, cancellationToken) i UltraStar.SongCreator.Projects. Load returnerer Song, schemaVersion, absolutt prosjektfilbane og validatorfunn. ProjectFormatException skiller invalid JSON/envelope/song, unsupported version og too-large; vanlige fil-/tilgangsfeil beholder .NETs I/O-exceptions.

Core holdes uavhengig av JSON/fil-I/O. Projects har bare en ProjectReference til Core; eksterne runtime-/testpakker er uendret. Bygg/test/format med de samme solution-kommandoene ovenfor. .editorconfig sikrer UTF-8/LF og fire spaces for C#; masteren formatteres ikke.

106/106 tester bestod på Windows og nettisolert Linux-container med SDK 10.0.401. Dette inkluderer tidligere 55 domenetester og 51 lagrings-/robusthetstester, med en håndskrevet v1-fixture, redigeringsrundtur, full felt-/referansebevaring, Unicode/kultur, schema/version-feil, grenser, cancellation og filbevaring/opprydding ved feil. Locked restore, Release build uten advarsler og formatkontroll bestod. Testfiler ligger under .agent-local/project-storage-tests og ryddes etter testene.

Filer og referanser gjenåpnes uten lyd, modeller eller inferens. Ingen mediakopiering, nettverkshenting, GUI eller persistent undo-historikk er implementert. v1 har ingen eldre støtteversjon å migrere fra; fremtidige versjoner må legge til eksplisitt migrasjon. Standard filgrense er 64 MiB. Se formatdokumentet for rename-/cancellation-/krasjavgrensninger.
