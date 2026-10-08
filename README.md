# UltraStar Song Creator

UltraStar Song Creator skal bli et desktopverktøy for opprettelse, automatisk generering, validering og manuell redigering av UltraStar-kompatible karaokesanger. Den visuelle editoren er produktets viktigste komponent.

## Status

Fase 0 er etablert. **Fase 1 er avsluttet og godkjent av prosjekteieren 2026-10-08.** ADR-0001 og ADR-0002 er akseptert: C#/.NET 10 LTS + Avalonia, separat Python-analyse, utskiftbar miniaudio-backend, MIT for egen kode og separat dokumenterte LGPL-codecs. Open-Unmix med eksplisitt umxhq er separasjonsbaseline. Produktfunksjonalitet er ikke implementert; Fase 2 er ikke startet.

## Prosjektkilder

- [Master Project Specification v0.5](docs/PROJECT_MASTER.md) er prosjektets autoritative masterspesifikasjon.
- [AGENTS.md](AGENTS.md) er et kort kart for agenter og utviklere.
- [Utviklings- og repositoryprinsipper](docs/DEVELOPMENT.md) beskriver autoritet, brancher, verifikasjon og miljø.
- [Fase 1-audit](docs/PHASE1_AUDIT.md), [kandidat-/lisensmatrise](docs/DEPENDENCIES.md) og [måleresultater](docs/PHASE1_RESULTS.md) dokumenterer evalueringen.
- [Teknologianbefaling og beslutningspunkter](docs/PHASE1_RECOMMENDATION.md), [videre målinger](docs/PHASE1_CONTINUATION_RESULTS.md) og [artefaktlisenser](docs/PHASE1_LICENSES.md) er gjeldende beslutningsgrunnlag.
- [Akseptert desktop-/analysearkitektur](docs/decisions/ADR-0001-desktop-and-analysis-stack.md) og [lisensstrategi](docs/decisions/ADR-0002-licensing-and-reuse.md) dokumenterer prosjekteierens vedtak.
- [Avsluttet Fase 1-plan](docs/exec-plans/completed/phase1-feasibility-audit.md) dokumenterer leveransen og aksepterte begrensninger.
- [Forslag til første Fase 2-arbeidsordre](docs/PHASE2_FIRST_WORK_ORDER.md) avventer bestilling.
- [Evalueringsverktøy](tools/phase1/README.md) inneholder reproduksjonskommandoer; ingen produkt-build er etablert.

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

Fase 0 krever Git for å lese historikk og arbeide med repositoryet. GitHub CLI er brukt til den første GitHub-opprettelsen, men er ikke et krav for applikasjonen eller senere utvikling. Det finnes ennå ingen produktapplikasjon eller etablerte produkt-build-/testkommandoer. Fase 1-prøvene kjøres separat som beskrevet i [tools/phase1/README.md](tools/phase1/README.md). Se [DEVELOPMENT.md](docs/DEVELOPMENT.md) for arbeidsflyt og miljøprinsipper.

## Lisens

[MIT License](LICENSE) er bekreftet for egen prosjektkode 2026-10-08. Tredjepartsavhengigheter, modeller og distribuerte binærer har egne vilkår, jf. [ADR-0002](docs/decisions/ADR-0002-licensing-and-reuse.md).
