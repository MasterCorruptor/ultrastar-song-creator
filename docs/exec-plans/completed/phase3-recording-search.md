# Fase 3.2 – innspillingssøk

Implementert og verifisert 2026-10-12 etter prosjekteierens valg av MusicBrainz først. Se [arbeidsordren](../../PHASE3_RECORDING_SEARCH_WORK_ORDER.md).

- [x] Providerkontrakt, bokstavelig query og normalisering av core-kandidater.
- [x] HTTP-grense, felles ratebegrensning, timeout/cancellation/feil.
- [x] Eksplisitt recording/release-valg og offline source-proveniens.
- [x] Windows/Linux-tester, avgrenset live-prøve, integritet/format/inventar.
- [x] Dokumentert kontrakt og lokal review.
- [x] Publisert PR og klargjort integrasjon.

Ingen medienedlasting, lyrics, GUI eller neste deloppgave inngår.

## Gjennomføring

360/360 tester bestod på Windows og nettisolert Linux: 308 tidligere + 52 nye. Locked restore, Release build uten advarsler og formatkontroll bestod. Faktisk felles gate-spacing og syntetisk HTTP/body/cancellation ble kontrollert uten nett; én avgrenset live Windows-prøve bestod på MusicBrainz og offline prosjektvalg.

Valg krever ID og optional release-kontekst. Raw title/disambiguation/credits beholdes; ingen oppdiktet kvalitets-/versjonsfasit. Attach er en ren ISongCommand og beholder current metadata/media/noter/analyse. Egen frozen choice-v1 lagres i local-media-v1 via eksisterende prosjektformat v2; ingen root-schemaendring eller API-oppslag ved gjenåpning.

Master, aksepterte ADR-er, Core/Projects/UltraStar og historisk v1-fixture/lockfiler er bevart. Ingen supplementary-data-/score- eller mediepublisering, ny dependency eller GUI. [Kontrakten](../../RECORDING_SEARCH.md) samler API, vilkår, verifikasjon og grenser. Fase 3 er ikke fullført; mediesøk/nedlasting og lyrics gjenstår som avgrensede arbeidsordre.

## Integrasjonsreferanse

[PR #8](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/8) samler leveransen. Publisering brukte appens eksisterende GitHub-kobling fordi lokal Git/GitHub-innlogging var ugyldig. Publisert Git-tre ble kontrollert identisk med den verifiserte lokale leveransen; lokale checkpoint-commits er bevart. PR-ens faktiske merge-status er integrasjonsreferansen.
