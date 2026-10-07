# Utviklings- og repositoryprinsipper

Dette dokumentet beskriver det etablerte Fase 0-grunnlaget. Kravene følger [PROJECT_MASTER.md](PROJECT_MASTER.md), særlig §15–19. Ved uløste konflikter mellom lokale autoritative dokumenter skal berørt arbeid stoppes og rapporteres.

## Autoritet og synkronisering

Den lokale autoritative working tree ligger på prosjekteierens primære utviklingsmaskin i `G:\ChatGPT - Prosjektmappe\ultrastar-song-creator`.

Autoritetsrekkefølgen mellom prosjektkopier er:

1. Lokal autoritativ working tree.
2. GitHub `main` / `origin/main`, den høyeste delte og synkroniserte autoriteten.
3. Andre stabile commits eller referansebrancher.
4. WIP-/arbeidsbrancher, med autoritet kun innenfor sin eksplisitte arbeidsordre.

Bevar lokale autoritative endringer hvis GitHub avviker; ikke overskriv dem med en fjernkopi. Gjør relevante avvik synlige. Filenes tidsstempler avgjør ikke autoritet. Ønsket normaltilstand er lokal godkjent endring → commit → push → GitHub `main`.

## Brancher og integrasjon

`main` er stabil integrasjonsbranch. Første Fase 0-commit etablerer denne branchen. Senere ikke-trivielt arbeid utføres normalt på avgrensede `work/<oppgave>`-brancher. Commit ved naturlige, konsistente kontrollpunkter og bruk arbeidsbrancher som redundans når det er praktisk.

Integrer først når oppgavens acceptance criteria, relevante tester/kontroller og eventuell påkrevd menneskelig godkjenning er oppfylt. Praktisk brukertesting legges til naturlige kontrollpunkter; det kreves ikke ny godkjenning for hvert lite teknisk steg. Ikke bruk destruktiv synkronisering for å skjule avvik.

## Prosjektkunnskap og lokal arbeidstilstand

Masterspesifikasjon, dokumentasjon, relevante ADR-er, kode, tester og reproducerbare verktøy versjonskontrolleres. `docs/decisions/` reserveres for dokumenterte beslutninger; ingen produksjonsstack er besluttet i Fase 0.

`docs/exec-plans/active/` og `completed/` brukes bare for planer med verdi på tvers av arbeidsøkter eller varig gjennomføringshistorikk. Kortvarige planer, prompts, logger og scratch-data hører hjemme i Git-ignorert `.agent-local/`. Flytt nødvendig varig kunnskap til repositoryet før lokale arbeidsfiler disponeres.

Den innsendte `Master-plan-for-prosjektet-v0.5(1).txt` er bevart lokalt og ignorert av Git. `docs/PROJECT_MASTER.md` er den uendrede, versjonskontrollerte masterkopien.

## Miljø og utviklingskonvensjoner

Dagens minimum er Git. GitHub CLI brukes til repository-opprettelse og synkronisering. Ingen produksjonsspråk, GUI-rammeverk, runtime, build-system, testløper eller dependencies er valgt. Det finnes derfor ennå ingen kommando for å installere, bygge, teste eller kjøre produktet.

Fase 1 skal evaluere gjenbruk, Windows/Linux-støtte, kvalitet, integrerbarhet og lisens. Prosjekteieren godkjenner vesentlige teknologivalg. Når stacken er valgt, dokumenteres faktiske runtime-versjoner, installasjon, eksterne verktøy/modeller og build-/testkommandoer. Lås eller avgrens versjoner når reproduksjon krever det.

Følg etablerte konvensjoner for valgt språk og rammeverk. Ta i bruk modne formatterings-, lint- og typekontrollverktøy når de passer stacken. Legg bare til nødvendige dependencies etter evaluering. Hold midlertidig debugging ute av produksjonskode, oppdater dokumentasjon ved endringer og skriv meningsfulle reproducerbare tester og regression-tester.

## Verifikasjon i Fase 0

Kontroller den påkrevde strukturen og at lokale dokumentlenker finnes. Bekreft at masterkopien er byteidentisk med kilden, også i første commit. Git er konfigurert lokalt med `core.autocrlf=false` for å bevare kildeinnholdet.

Før commit brukes `git diff --cached --check` på de nye grunnlagsfilene. Etter commit kontrolleres ren working tree, branch `main`, upstream `origin/main` og samsvar mellom lokal og ekstern commit. Masterspesifikasjonen beholdes uendret også dersom den inneholder eksisterende formatteringsavvik.

Automatiske produkttester og build er ikke tilgjengelige i Fase 0. Teknologievaluering og produktkode krever senere arbeidsordrer.

## Foreløpig lisens

Prosjektet har foreløpig MIT-lisens. Endelig bekreftelse utsettes til dependency- og lisensgjennomgangen i Fase 1. Nye dependencies må vurderes separat mot prosjektets permissive mål; vesentlig andre lisens- eller distribusjonskrav krever eksplisitt prosjektvedtak.
