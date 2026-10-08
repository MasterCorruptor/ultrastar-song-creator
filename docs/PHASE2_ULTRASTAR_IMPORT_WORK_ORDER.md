# Arbeidsordre: Fase 2.3 – UltraStar-import med kildemetadata

Status: bestilt og gjennomført 2026-10-08; avklaringen er lukket, verifisert og klar for review. Dette er en deloppgave i Fase 2.

Prosjekteieren godkjente neste trinn og ba om nødvendige avklaringer før antakelser. De konkrete svarene var unversionerte filer + v1, duett senere når enkeltstemmemodellen virker, og metadata bevart gjennom save/reopen slik at ny innhenting unngås.

## Scope og autoritet

Bruk [masteren](PROJECT_MASTER.md) §9–10/13–16 og aksepterte ADR-er. Separat .NET-adapter uten GUI/Python/medieåpning; syntetiske tester. Bevar opprinnelige headers som generisk importkilde, og legg til eksplisitt prosjektversjon/migrasjon fremfor stilltiende endring av v1. Egen parser bruker [primærkilden og den dokumenterte profilen](ULTRASTAR_IMPORT.md).

Implementer absolutt tidsmodus for unversionert format og v1, samt avklart relativ legacy-tid for unversionerte enkeltstemmer, note-/frasekonvertering og strukturert diagnostikk. Duetter, ukjente major-versjoner og ukjente note-/tidssemantikker skal ikke nedgraderes/reetiketteres stille. Writer/eksport, GUI, playback, acquisition, modeller og duettmodell er egne arbeidsordre.

## Acceptance criteria

- Beat/duration, MIDI-pitch, GAP og VIDEOGAP konverteres etter dokumentert kilde-/domenekontrakt.
- Notetyper, tekstmellomrom og note-/fraserekkefølge bevares; usorterte/overlappende noter gir warnings.
- Alle headers, også ukjente/tomme/dupliserte metadata, overlever redigering/undo/redo og prosjektlagring/gjenåpning uten ny innhenting.
- Feil, versjoner/stemmer, Unicode/encoding, BOM/linjeslutt, numeric boundaries, filgrenser/cancellation og originalfilbevaring testes.
- Locked restore/build/test/format, Windows/Linux-prøve, master-/ADR-/v1-fixtureintegritet og offentlig fil-/privacykontroll gjennomføres.
- Endelig avgrensning for #RELATIVE:YES lukkes med prosjekteieren før ferdigstatus; den dokumenterte tvetydigheten skal ikke løses ved antakelse.

## Avklaring og leveranse

Prosjekteieren bestilte implementering av relativ modus. [Avklaringsrapporten](RELATIVE_TIMING.md) viser pinnede primærkilder og en eksplisitt USDX-kompatibilitetsprofil: initial beat-offset 0, akkumulert beat-delta fra tofeltsmarkører og GAP separat i millisekunder. Relativ tid i v1 avvises. Vår importer retter ikke formatkilden eller gjetter manglende modus/felt.

203/203 tester bestod på Windows og nettisolert Linux, locked restore/Release build uten warnings og formatkontroll. Originalmetadata og konvertert tidsmodell overlever edit/undo/redo/save/reopen selv uten kildetekst. Master, aksepterte ADR-er og historisk v1-fixture er uendret. Public inventory/lenker/privacy bestod. Planen ligger i [completed/](exec-plans/completed/phase2-ultrastar-import.md), og [PR #4](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/4) er klargjort for review. Ingen automatisk merge eller oppstart av writer/eksport.
