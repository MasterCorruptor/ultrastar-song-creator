# UltraStar Song Creator

UltraStar Song Creator skal bli et desktopverktøy for opprettelse, automatisk generering, validering og manuell redigering av UltraStar-kompatible karaokesanger. Den visuelle editoren er produktets viktigste komponent.

## Status

Repositoryet inneholder prosjektgrunnlaget for **Fase 0**. Produktfunksjonalitet er ikke implementert. Programmeringsspråk, GUI-rammeverk, produksjonsdependencies og build-/testverktøy er ikke valgt. Fase 1 krever en egen arbeidsordre.

## Prosjektkilder

- [Master Project Specification v0.5](docs/PROJECT_MASTER.md) er prosjektets autoritative masterspesifikasjon.
- [AGENTS.md](AGENTS.md) er et kort kart for agenter og utviklere.
- [Utviklings- og repositoryprinsipper](docs/DEVELOPMENT.md) beskriver autoritet, brancher, verifikasjon og miljø.

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
│   ├── decisions/
│   └── exec-plans/
│       ├── active/
│       └── completed/
├── src/
├── tests/
└── tools/
```

De foreløpig tomme mappene har minimale `.gitkeep`-filer. Lokal agenttilstand hører hjemme i den Git-ignorerte mappen `.agent-local/`.

## Kom i gang

Fase 0 krever Git for å lese historikk og arbeide med repositoryet. GitHub CLI er brukt til den første GitHub-opprettelsen, men er ikke et krav for applikasjonen eller senere utvikling. Det finnes ennå ingen applikasjon å kjøre og ingen etablerte build- eller automatiske testkommandoer. Se [DEVELOPMENT.md](docs/DEVELOPMENT.md) for dagens verifikasjon og prinsippene som gjelder når stacken velges.

## Lisens

[MIT License](LICENSE) er prosjektets foreløpige lisens. Endelig lisensbekreftelse følger dependency- og lisensgjennomgangen i Fase 1. Tredjepartsavhengigheter må vurderes separat.
