# UltraStar Song Creator

UltraStar Song Creator skal bli et desktopverktøy for opprettelse, automatisk generering, validering og manuell redigering av UltraStar-kompatible karaokesanger. Den visuelle editoren er produktets viktigste komponent.

## Status

Fase 0 er etablert. **Fase 1 har nå kandidatmatrise, native playback/timeline-prøver, ekte sangbenchmark, portable Windows-prøve, Linux smoke og en avsluttende teknologianbefaling.** Prosjekteierens beslutning om stack og faseavslutning gjenstår. Produktfunksjonalitet og produksjonsstack er ikke implementert eller vedtatt; Fase 2 er ikke startet.

## Prosjektkilder

- [Master Project Specification v0.5](docs/PROJECT_MASTER.md) er prosjektets autoritative masterspesifikasjon.
- [AGENTS.md](AGENTS.md) er et kort kart for agenter og utviklere.
- [Utviklings- og repositoryprinsipper](docs/DEVELOPMENT.md) beskriver autoritet, brancher, verifikasjon og miljø.
- [Fase 1-audit](docs/PHASE1_AUDIT.md), [kandidat-/lisensmatrise](docs/DEPENDENCIES.md) og [måleresultater](docs/PHASE1_RESULTS.md) dokumenterer evalueringen.
- [Teknologianbefaling og beslutningspunkter](docs/PHASE1_RECOMMENDATION.md), [videre målinger](docs/PHASE1_CONTINUATION_RESULTS.md) og [artefaktlisenser](docs/PHASE1_LICENSES.md) er gjeldende beslutningsgrunnlag.
- [Evalueringsverktøy](tools/phase1/README.md) inneholder reproduksjonskommandoer; disse velger ingen produksjonsstack.

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

[MIT License](LICENSE) er prosjektets foreløpige lisens. Endelig lisensbekreftelse følger dependency- og lisensgjennomgangen i Fase 1. Tredjepartsavhengigheter må vurderes separat.
