# Integrasjonsrapport – PR #1–#5

Gjennomført 2026-10-09 etter prosjekteierens eksplisitte bestilling. Avsluttet Fase 1 og implementert Fase 2.1–2.4 er gjennomgått og merget til main. Ingen ny produktfase er startet.

## Integrert leveranse

PR-ene ble merget i rekkefølge med merge commits. Hver etterfølgende PR ble flyttet fra sin arbeidsbranch-base til main etter at forgjengeren var integrert. Bekreftet head ble kontrollert før hver merge; ingen historikk ble squash-et eller omskrevet. Arbeidsbrancher og private artefakter er bevart.

| PR | Leveranse | Merge commit |
| --- | --- | --- |
| [1](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/1) | Fase 1: verifisert grunnlag og aksepterte teknologivalg | 8c4c72ac883aba4586f97f4bf4590c4e43de3bc6 |
| [2](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/2) | Fase 2.1: intern sangmodell, validering og undo/redo | dd7547322aa14d7e0a5db79b79abde2ac258ebe6 |
| [3](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/3) | Fase 2.2: versjonert prosjektlagring og tapfri gjenÃ¥pning | 8701e3ef7c19a86b4ba7cac59b0f91c2f49701a3 |
| [4](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/4) | Fase 2.3: UltraStar-import med relativ tid og metadata | 6d86c605b9c658df760dcbce62faab05d972d4c0 |
| [5](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/5) | Fase 2.4: UltraStar-eksport med avrundingsrapport og komplett sangmappe | ad3b359f8bd3438e8814ee6b661cbc7758cd6059 |

GitHub main etter PR #5 har byteidentisk versjonskontrollert filinnhold med den samlede, lokalt verifiserte kandidaten b9d354e. Integrasjonsrapport/status legges deretter inn som en separat dokumentasjonsendring. Dokumentasjonsoppdateringen endrer ingen produksjonskode eller tester.

## Review og rettelse

Gjennomgangen omfattet godkjente teknologivalg og lisensavgrensninger, intern modell/validering/undo, prosjektformat og v1-migrasjon, absolutte/relative importprofiler, eksportens kvantisering/metadata og pakkens referanser/kopiering/avbrudd. Testene omfatter også lagring, redigering, eksport og reimport uten mutasjon av originalprosjektet.

Ett konkret funn ble rettet i PR #5 før merge: et langt assetnavn kunne ved avkorting få punktum eller space på slutten. Dette er ikke portabelt til Windows. Pakkeren trimmer nå også etter avkorting, før reserved-name- og kollisjonskontroll. En test demonstrerte feilen før rettelsen; to regresjonstilfeller kontrollerer filkopien og referansen ved reimport. Ingen andre blokkerende funn ble identifisert i gjennomgangen.

## Samlet verifikasjon

| Kontroll | Resultat |
| --- | --- |
| Windows, .NET SDK 10.0.401 | Locked restore, Release build med 0 advarsler/feil, 265/265 tester og formatkontroll bestod. |
| Nettisolert Linux-container, samme SDK/kilde | Locked restore, Release build med 0 advarsler/feil og 265/265 tester bestod; samme formatterte kilder. |
| Testgrunnlag | 203 tidligere tester, 38 writer-/rundturtester og 24 pakketester. |
| Master | Byteidentisk med innsendt kilde og Fase 0-main; SHA-256 6906458b83664221520d85774a885de7360cedb70ee123e2d41f62f9693a81a9. |
| Aksepterte ADR-er og historisk v1-fixture | Byteuendret; ingen nye teknologivedtak eller schemaendringer. |
| Offentlig innhold | Ingen medier, modeller, runtime-binærer, cacher eller maskinvareinformasjon; MIT-hjelpere har bevart innhold og lisens. |
| Dokumentasjon/whitespace | Lokale lenker, JSON-inventar og full integrasjonsdiff kontrollert. |

Linux-testen brukte samme prosjektmappe, egen ignorert artifacts-mappe og eksisterende NuGet-cache med nettverket deaktivert. NuGet-audit var deaktivert kun under offline restore; Windows-restore beholdt audit. Testlogger og arbeidsartefakter er lokale og ignorerte.

## Status og begrensninger

- Fase 1 er avsluttet og akseptert; teknologivedtakene består.
- Fase 2.1–2.4 er implementert, verifisert og integrert: core, prosjektlagring, UltraStar-import og komplett eksportpakke.
- Historiske arbeidsplaner/resultater beskriver status ved sine leveransetidspunkter. Denne rapporten og README beskriver integrasjonsstatusen.
- Hosted CI er fortsatt ikke aktivert: den eksisterende workflow-tilgangen mangler nødvendig rettighet. Malen i tools/ci/core.yml er bevart. Ingen CI-pass er påstått.
- Denne integrasjonen er ikke ny native playback-/GUI-/installer-/modellverifikasjon. Tidligere dokumenterte Fase 1- og eksport-smoke-begrensninger består.
- Ingen Fase 3 eller annen produktutvidelse inngår. Neste arbeidsordre må avgrenses og avklares med prosjekteieren før implementering.

Se [utviklingsguiden](DEVELOPMENT.md), [eksportresultatene](PHASE2_EXPORT_RESULTS.md) og [avsluttet integrasjonsplan](exec-plans/completed/repository-integration.md).
