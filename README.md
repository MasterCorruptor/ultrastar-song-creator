# UltraStar Song Creator

UltraStar Song Creator skal bli et desktopverktøy for opprettelse, automatisk generering, validering og manuell redigering av UltraStar-kompatible karaokesanger. Den visuelle editoren er produktets viktigste komponent.

## Status

Fase 0 er etablert. **Fase 1 er avsluttet og godkjent av prosjekteieren 2026-10-08.** ADR-0001 og ADR-0002 er akseptert: C#/.NET 10 LTS + Avalonia, separat Python-analyse, utskiftbar miniaudio-backend, MIT for egen kode og separat dokumenterte LGPL-codecs. Open-Unmix med eksplisitt umxhq er separasjonsbaseline. **Fase 2 er påbegynt:** deloppgave 2.1 har intern sangmodell, validering og MoveNote med undo/redo og domenetester. **Fase 2.2 har versjonert prosjektlagring:** dagens sang, redigerte noter og analysepunkter kan lagres/gjenåpnes lokalt. **Fase 2.3 er implementert og verifisert:** import for unversionerte enkeltstemmer (absolutt og relativ tid) og UltraStar v1, med bevart kildemetadata i prosjektformat v2. Relativ legacy-tid følger dokumentert USDX-kompatibilitet; v1 + relativ tid avvises. **Fase 2.4 er implementert og verifisert:** unversionert/v1-eksport med absolutt tid, automatisk avrundingsrapport og komplette mapper med byteverifiserte mediekopier. GUI er ikke implementert.

## Prosjektkilder

- [Master Project Specification v0.5](docs/PROJECT_MASTER.md) er prosjektets autoritative masterspesifikasjon.
- [AGENTS.md](AGENTS.md) er et kort kart for agenter og utviklere.
- [Utviklings- og repositoryprinsipper](docs/DEVELOPMENT.md) beskriver autoritet, brancher, verifikasjon og miljø.
- [Fase 1-audit](docs/PHASE1_AUDIT.md), [kandidat-/lisensmatrise](docs/DEPENDENCIES.md) og [måleresultater](docs/PHASE1_RESULTS.md) dokumenterer evalueringen.
- [Teknologianbefaling og beslutningspunkter](docs/PHASE1_RECOMMENDATION.md), [videre målinger](docs/PHASE1_CONTINUATION_RESULTS.md) og [artefaktlisenser](docs/PHASE1_LICENSES.md) er gjeldende beslutningsgrunnlag.
- [Akseptert desktop-/analysearkitektur](docs/decisions/ADR-0001-desktop-and-analysis-stack.md) og [lisensstrategi](docs/decisions/ADR-0002-licensing-and-reuse.md) dokumenterer prosjekteierens vedtak.
- [Avsluttet Fase 1-plan](docs/exec-plans/completed/phase1-feasibility-audit.md) dokumenterer leveransen og aksepterte begrensninger.
- [Gjeldende prosjektformat v2](docs/PROJECT_FORMAT_V2.md), [historisk v1](docs/PROJECT_FORMAT.md) og [avsluttet lagringsplan](docs/exec-plans/completed/phase2-project-storage.md) dokumenterer Fase 2.2.
- [Fase 2.1-arbeidsordre](docs/PHASE2_FIRST_WORK_ORDER.md), [core domain/enheter](docs/CORE_DOMAIN.md) og [testdependencies](docs/CORE_DEPENDENCIES.md) dokumenterer den første kjernedeloppgaven.
- [UltraStar-importprofil](docs/ULTRASTAR_IMPORT.md), [arbeidsordre](docs/PHASE2_ULTRASTAR_IMPORT_WORK_ORDER.md) og [avsluttet importplan](docs/exec-plans/completed/phase2-ultrastar-import.md) dokumenterer Fase 2.3. [Relativ tidsprofil](docs/RELATIVE_TIMING.md) beskriver avklaringen og kompatibilitetsgrensene.
- [Eksportkontrakt](docs/ULTRASTAR_EXPORT.md), [resultater](docs/PHASE2_EXPORT_RESULTS.md), [arbeidsordre](docs/PHASE2_ULTRASTAR_EXPORT_WORK_ORDER.md) og [avsluttet eksportplan](docs/exec-plans/completed/phase2-ultrastar-export.md) dokumenterer Fase 2.4.
- [Evalueringsverktøy](tools/phase1/README.md) inneholder reproduksjonskommandoer; prober bygges separat fra Core/Projects/UltraStar-solution.

## Struktur

```text
.
├── .gitignore
├── AGENTS.md
├── README.md
├── LICENSE
├── docs/
│   ├── PROJECT_MASTER.md
│   ├── DEVELOPMENT.md
│   ├── PHASE1_AUDIT.md
│   ├── PHASE1_RESULTS.md
│   ├── DEPENDENCIES.md
│   ├── decisions/
│   └── exec-plans/
│       ├── active/
│       └── completed/
├── src/
├── tests/
└── tools/
```

Mapper som fortsatt er tomme har minimale `.gitkeep`-filer. Lokal agenttilstand hører hjemme i den Git-ignorerte mappen `.agent-local/`.

## Kom i gang

Fase 0 krever Git for å lese historikk og arbeide med repositoryet. GitHub CLI er brukt til den første GitHub-opprettelsen, men er ikke et krav for applikasjonen eller senere utvikling. Det finnes ennå ingen GUI-applikasjon. Bygg/test produktkjernen med .NET 10 SDK fra prosjektroten:

```text
dotnet restore UltraStar.SongCreator.slnx --locked-mode
dotnet build UltraStar.SongCreator.slnx -c Release --no-restore
dotnet test UltraStar.SongCreator.slnx -c Release --no-build --no-restore
```

Core/Projects/UltraStar krever ingen Python, modeller eller mediefiler; restore trenger pakker én gang eller en eksisterende NuGet-cache. Fase 1-prøvene kjøres separat som beskrevet i [tools/phase1/README.md](tools/phase1/README.md). Se [DEVELOPMENT.md](docs/DEVELOPMENT.md) for arbeidsflyt og miljøprinsipper.

## Lisens

[MIT License](LICENSE) er bekreftet for egen prosjektkode 2026-10-08. Tredjepartsavhengigheter, modeller og distribuerte binærer har egne vilkår, jf. [ADR-0002](docs/decisions/ADR-0002-licensing-and-reuse.md).
