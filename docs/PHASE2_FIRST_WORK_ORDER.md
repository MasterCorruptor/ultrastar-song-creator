# Forslag: Fase 2.1 – intern sangmodell og reverserbar kjerneredigering

**Status: forslag til prosjekteier, ikke bestilt eller startet.** Utarbeidet 2026-10-08 etter avsluttet Fase 1. Dette er første del av masterens Fase 2, ikke hele fasen.

## Mål

Etabler en liten, GUI-uavhengig .NET-kjerne som kan representere og validere et redigerbart sangutkast, og vise at en noteendring kan utføres, angres og gjøres om uten tap av tekst, confidence eller analysereferanser.

## Autoritative kilder og kontekst

- [PROJECT_MASTER.md](PROJECT_MASTER.md), særlig §9 intern sangmodell, §12 undo/redo, §13 validering, §16 Fase 2 og §22 arbeidsordre.
- Akseptert [ADR-0001](decisions/ADR-0001-desktop-and-analysis-stack.md) og [ADR-0002](decisions/ADR-0002-licensing-and-reuse.md).
- C#/.NET 10 LTS er godkjent. Ingen produktkode, modellkontrakt, prosjektformat eller produkt-build/testkommandoer er etablert.

## In scope

- Minimalt .NET-solution-/build-/testoppsett for core domain under src/ og tester under tests/.
- Song, Phrase og Note med relevante metadata, mediareferanser, offsets/timing, tekst, notetype og valgfri confidence/proveniens etter masteren §9.
- Et lite separat AnalysisData-/referanselag; store lyd-/analysearrayer skal ikke kopieres ved hver noteendring.
- Dokumenterte tids- og pitchenheter og stabile identiteter. Intern modell skal være uavhengig av UltraStar-ticks og filsyntaks; formatkonvertering kommer senere.
- Ren domenevalidering med forskjell på errors og warnings; minst ugyldig/not-finite timing, ikke-positiv notevarighet, confidence utenfor dokumentert skala, dublette identiteter og brutte referanser. Overlapp/manglende tekst skal gi forklarte, kontekstavhengige funn uten stille sletting.
- En grunnleggende command-/undo-/redo-mekanisme og én MoveNote-operasjon, med uendret varighet og tapfri reversering.
- Kort domene-/miljødokumentasjon, nødvendig .gitignore og dokumenterte build-/testkommandoer.

## Out of scope

Avalonia-vindu/timeline, playback, Python/IPC, nedlasting/providers, AI-modeller, analysepipeline, CUDA, installer, prosjektserialisering/migrering og UltraStar parser/writer/eksport. Resten av Fase 2 deles i senere arbeidsordre.

## Constraints

Bruk .NET 10 LTS og plattformnøytral C#-kjerne uten GUI-, playback-, worker- eller filformatavhengigheter. Ingen konto/API-nøkkel, nettverk eller modeller i testkjøring. Dependencies skal være minimale, begrunnede og lisenskontrollerte. Dokumenter eksakte SDK-/testversjoner som faktisk velges. Bevar masteren og aksepterte ADR-er. Arbeid i prosjektets autoritative mappe på en avgrenset work/-branch.

## Acceptance criteria

1. Solution kan bygges og meningsfulle domenetester kjøres fra prosjektroten med dokumenterte kommandoer.
2. Et syntetisk Song med flere fraser/noter kan konstrueres med tekst, confidence og analysereferanser uten GUI/audio/Python.
3. Validering returnerer strukturerte, stabile funn og endrer ikke sangen; relevante grenseverdier og ugyldige verdier testes.
4. MoveNote endrer bare forventet timing. Undo gjenoppretter eksakt opprinnelig tilstand; redo gjenoppretter endringen. Ny endring etter undo forkaster gammel redo-gren. Ugyldig endring må ikke gi delvis mutasjon.
5. Testene viser at brukerredigering ikke muterer rå analysedata eller mister provenance/confidence.
6. Ingen analysemodell, mediefil, runtime-binær eller lokal cache committes. Dokumentasjonen angir at prosjektlagring/import/eksport fortsatt ikke er implementert.

## Verification

Kjør restore/build/test med valgte verktøy, dokumenter de faktiske kommandoene, og kontroller relevante grenseverdier, command-historikk og uavhengighet mellom domene/analyse. Bekreft masterens byteintegritet, dokumentlenker og git diff --check. Ingen tung sangbenchmark kreves for denne domenedeloppgaven.

## Stop conditions

Stopp berørt arbeid og rapporter hvis modellen krever brudd på master/akseptert ADR, uklare obligatoriske lisenser eller avhengighet til GUI/Python/UltraStar-filformat. Rapportér uavklarte vesentlige enhets-/datastrukturvalg før en større offentlig kontrakt låses; ikke utvid oppgaven til hele applikasjonen.

## Deliverable

En avgrenset PR med core domain, meningsfulle tester, dokumenterte enheter/kontrakter og reproducerbare build-/testkommandoer. Sluttrapporten angir verifikasjon, begrensninger og neste foreslåtte deloppgave: versjonert prosjektlagring, før UltraStar parser/writer.
