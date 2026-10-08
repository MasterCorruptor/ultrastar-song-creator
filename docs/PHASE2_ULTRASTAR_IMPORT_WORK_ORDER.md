# Arbeidsordre: Fase 2.3 – UltraStar-import med kildemetadata

Status: bestilt 2026-10-08; implementerte kontrollpunkter verifisert, endelig avgrensning venter på avklaring. Dette er en deloppgave i Fase 2.

Prosjekteieren godkjente neste trinn og ba om nødvendige avklaringer før antakelser. De konkrete svarene var unversionerte filer + v1, duett senere når enkeltstemmemodellen virker, og metadata bevart gjennom save/reopen slik at ny innhenting unngås.

## Scope og autoritet

Bruk [masteren](PROJECT_MASTER.md) §9–10/13–16 og aksepterte ADR-er. Separat .NET-adapter uten GUI/Python/medieåpning; syntetiske tester. Bevar opprinnelige headers som generisk importkilde, og legg til eksplisitt prosjektversjon/migrasjon fremfor stilltiende endring av v1. Egen parser bruker [primærkilden og den dokumenterte profilen](ULTRASTAR_IMPORT.md).

Implementer normal absolutt tidsmodus for unversionert format og v1, note-/frasekonvertering og strukturert diagnostikk. Duetter, ukjente major-versjoner og ukjente note-/tidssemantikker skal ikke nedgraderes/reetiketteres stille. Writer/eksport, GUI, playback, acquisition, modeller og duettmodell er egne arbeidsordre.

## Acceptance criteria

- Beat/duration, MIDI-pitch, GAP og VIDEOGAP konverteres etter dokumentert kilde-/domenekontrakt.
- Notetyper, tekstmellomrom og note-/fraserekkefølge bevares; usorterte/overlappende noter gir warnings.
- Alle headers, også ukjente/tomme/dupliserte metadata, overlever redigering/undo/redo og prosjektlagring/gjenåpning uten ny innhenting.
- Feil, versjoner/stemmer, Unicode/encoding, BOM/linjeslutt, numeric boundaries, filgrenser/cancellation og originalfilbevaring testes.
- Locked restore/build/test/format, Windows/Linux-prøve, master-/ADR-/v1-fixtureintegritet og offentlig fil-/privacykontroll gjennomføres.
- Endelig avgrensning for #RELATIVE:YES lukkes med prosjekteieren før ferdigstatus; den dokumenterte tvetydigheten skal ikke løses ved antakelse.

## Avklaringspunkt og leveranse

Primærkilden blander GAP-millisekunder med relativ beat-offset. Prosjekteieren er spurt om første importer skal avvise relativ modus eller om denne skal avklares og implementeres nå. Det uavhengige absolutte import-/metadataarbeidet er gjennomført og samlet i draft-PR. Planen ligger i [active/](exec-plans/active/phase2-ultrastar-import.md). Ingen automatisk merge eller oppstart av neste deloppgave.
