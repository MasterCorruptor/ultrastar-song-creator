# Fase 3.1 – lokal medieimport

Status: implementert og verifisert 2026-10-11 etter prosjekteierens bestilling og avklaring av referanser/filmetadata/manuelle overrides. Se [arbeidsordren](../../PHASE3_LOCAL_MEDIA_WORK_ORDER.md).

- [x] Acquisition-kontrakt, lokal source og kontrollert FFprobe-adapter.
- [x] Normalisert draft, stream-valg, metadata-overrides og varig proveniens.
- [x] Prosjekt-/eksportintegrasjon uten schemaendring.
- [x] Tester, Windows/Linux og praktisk syntetisk FFprobe-smoke.
- [x] Status/testguide, review og avgrenset PR.

Master, aksepterte ADR-er og opprinnelige fixtures endres ikke. Ingen GUI eller ny deloppgave inngår.

## Gjennomføring

308/308 tester bestod på Windows og nettisolert Linux (265 tidligere + 43 nye), locked restore, Release build uten advarsler og formatkontroll. Faktiske .NET-child-prosesser verifiserer prosessgrensen; Linux krever ingen FFprobe-installasjon for testsettet.

Windows-smoke med eksisterende minimal FFprobe 8.1.3 bestod på egen PCM-WAV, MPEG4/AAC-video og to-spors M4A. Metadata, manuell overstyring, varig proveniens/save/load, pakettrundtur og kildebevaring ble kontrollert. Review la til en eksplisitt eksportgrense for originalfiler med flere streams, fordi UltraStar ikke kan representere valgte stream-indekser.

Core/Projects, master, aksepterte ADR-er og historisk v1-fixture er bevart. Ingen ny ekstern NuGet-pakke, schemaVersion eller distribuert runtime. AGENTS er forkortet til et operativt kart med detaljer i docs, i samsvar med masterens §19.

[Source-kontrakt](../../LOCAL_MEDIA.md), [utviklingsguide/evidens](../../DEVELOPMENT.md) og [teststatus for prosjekteieren](../../TESTING_STATUS.md) dokumenterer leveransen. Ingen alpha-/GUI-implementering eller videre deloppgave inngår.
