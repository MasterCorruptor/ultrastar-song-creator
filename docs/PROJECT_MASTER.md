# UltraStar Song Creator

## Master Project Specification v0.5

**Status:** Konsept- og planleggingsfase
**Arbeidstittel:** UltraStar Song Creator
**Primær plattform:** Windows
**Sekundær plattform:** Linux
**Primær utviklingsmetode:** Human-in-the-loop utvikling med ChatGPT, ChatGPT Work og Codex/Astra

---

# 1. Prosjektformål

UltraStar Song Creator skal være et desktopbasert authoring-verktøy for opprettelse, automatisk generering, validering og manuell redigering av karaokesanger til UltraStar-kompatible programmer.

Programmet skal redusere det manuelle arbeidet med å lage en UltraStar-sang ved å:

1. finne riktig sang/innspilling,
2. innhente tilgjengelige medier, metadata og sangtekst,
3. analysere lyd og vokal,
4. generere et komplett førsteutkast,
5. åpne førsteutkastet direkte i en visuell DAW-lignende editor,
6. la brukeren kontrollere og korrigere resultatet under avspilling,
7. validere prosjektet,
8. eksportere en ferdig UltraStar-kompatibel sang.

Det automatisk genererte resultatet er **et redigerbart arbeidsgrunnlag, ikke en endelig fasit**.

---

# 2. Produktets viktigste verdi

Kjerneproduktet er ikke selve AI-/analysemodulen.

**Den viktigste komponenten er den visuelle editoren.**

Eksisterende verktøy kan allerede generere deler av UltraStar-data automatisk. Prosjektets viktigste differensiering er derfor den sammenhengende arbeidsflyten:

**Søk → innhenting → automatisk generering → live visuell kontroll → direkte manuell korreksjon → validering → eksport**

Brukeren skal ikke behøve å:

* redigere UltraStar-filer manuelt i teksteditor,
* flytte data mellom flere programmer,
* regenerere hele sangen for små korreksjoner,
* bruke ChatGPT eller andre verktøy separat for timingkorreksjoner,
* eksportere og åpne sangen i UltraStar bare for å kontrollere hvordan den oppfører seg.

---

# 3. Ikke-forhandlingsbare prosjektkrav

## 3.1 Ingen obligatoriske brukerkontoer

Kjernefunksjonalitet skal ikke være avhengig av:

* betalt abonnement,
* personlig API-nøkkel,
* obligatorisk skylagring,
* brukerregistrering hos tredjepart,
* proprietær tjeneste som ikke kan erstattes.

Foretrukket teknologi:

**lokal open source → åpen anonym datakilde → egen implementasjon**

Dersom nødvendig funksjonalitet ikke finnes innenfor disse rammene, behandles funksjonen som et mulig eget underprosjekt.

---

## 3.2 Lokal behandling

Når nødvendige medier, modeller og metadata er hentet, skal analyse, redigering, prosjektlagring, validering og eksport kunne foregå lokalt.

GPU-akselerasjon kan støttes, men skal ikke være absolutt krav for programmet.

---

## 3.3 Plattformstrategi

Versjon 1 utvikles og distribueres først for Windows.

Under utviklingen kan programmet kjøres og deles som en utviklings-/portabel variant uten krav om ferdig installer. Dette er den praktiske distribusjonsformen frem til prosjektet nærmer seg en stabil release.

Det ønskede sluttmålet for Windows er en ordinær installer som gjør installasjon og oppdatering enkel for sluttbrukeren. En portabel utgave kan fortsatt tilbys dersom dette kan vedlikeholdes uten vesentlig ekstra kompleksitet.

Linux-støtte kommer etter at Windows-versjonen har fått en stabil arkitektur.

Arkitektur, programmeringsspråk, rammeverk og dependencies skal så langt praktisk mulig velges slik at Linux senere kan støttes uten grunnleggende omskriving. Unødvendige Windows-spesifikke avhengigheter skal unngås.

Plattformspesifikke komponenter som faktisk er nødvendige skal isoleres bak tydelige grensesnitt.

---

# 4. Hovedarkitektur

Programmet deles konseptuelt i tre primære systemer:

## A. Acquisition Engine

Ansvar:

* søke etter sang,
* identifisere artist og tittel,
* presentere relevante innspillinger,
* hente media,
* hente metadata,
* hente cover og eventuell video,
* finne sangtekst,
* identifisere riktig versjon.

## B. Generation Engine

Ansvar:

* lydpreprosessering,
* vokalseparasjon,
* BPM-/beatanalyse,
* pitch detection,
* note-segmentering,
* tekstalignment,
* stavelsesdeling,
* frasestruktur,
* confidence-beregning,
* generering av første UltraStar-lignende sangmodell.

## C. Authoring Editor

Ansvar:

* live avspilling,
* waveform,
* playhead,
* pitchreferanse,
* UltraStar-noter,
* tekst/stavelser,
* frasegrenser,
* redigering,
* kontinuerlig validering,
* preview,
* undo/redo,
* eksport.

---

# 5. Acquisition Engine

## 5.1 Søk

Brukeren skal kunne søke med:

* artist,
* sangtittel,
* artist + sangtittel.

Senere kan også støttes:

* direkte URL,
* lokal lydfil,
* lokal videofil,
* eksisterende UltraStar-prosjekt.

Programmet skal søke etter konkrete innspillinger, ikke bare abstrakte låttitler.

Det må kunne skilles mellom for eksempel:

* studioinnspilling,
* liveversjon,
* remaster,
* radio edit,
* acoustic version,
* coverversjon.

---

## 5.2 Media

Eksisterende open-source-verktøy skal brukes der det er hensiktsmessig.

Aktuelle kandidater omfatter:

* yt-dlp,
* FFmpeg.

Endelig valg gjøres først etter teknisk evaluering.

Programmet skal ha et generelt `MediaSource`-grensesnitt slik at andre kilder senere kan implementeres uten endringer i resten av applikasjonen.

---

# 6. Lyrics Acquisition Engine

Sangtekst skal ikke være avhengig av én bestemt leverandør.

Systemet skal kunne kombinere flere providers og velge beste kandidat.

Prioritet:

1. word-level synkronisert tekst,
2. line-level synkronisert tekst,
3. vanlig sangtekst,
4. flere åpne tekstkilder,
5. generelt nettsøk,
6. lokal transkripsjon,
7. manuell innliming.

Aktuelle kilder som skal evalueres inkluderer:

* AMLL TTML,
* LRCLIB,
* lyrics.ovh,
* andre åpne og konto-frie kilder.

Programmet skal kunne sammenligne flere tekstkandidater og beregne en confidence-verdi basert på blant annet:

* artist,
* tittel,
* varighet,
* versjon,
* tekstlikhet,
* ASR-resultat,
* alignment mot lyd.

Providerne skal være utskiftbare moduler.

---

# 7. Generation Engine

Analysefunksjonene skal behandles som separate komponenter.

## 7.1 Preprosessering

Ansvar:

* dekoding,
* formatnormalisering,
* sample-rate-konvertering,
* midlertidige arbeidsfiler,
* cache.

## 7.2 Vokalseparasjon

Målet er å isolere vokalsporet tilstrekkelig for:

* pitchanalyse,
* transkripsjon,
* timing,
* onset-detection.

Eksisterende open-source-modeller evalueres før eventuell egen implementasjon.

## 7.3 Transkripsjon og alignment

Systemet skal bruke innhentet korrekt tekst som primær tekst når mulig.

ASR skal hovedsakelig brukes til:

* timing,
* verifikasjon,
* alignment,
* fallback dersom tekst ikke finnes.

## 7.4 BPM og beat grid

Programmet skal estimere musikalsk tempo og etablere tidsgrunnlaget som senere kan konverteres til UltraStar-notedata.

## 7.5 Pitch

Systemet skal estimere vokalens fundamentale tonehøyde over tid.

Pitch-resultatet skal beholdes som analyseteknisk referansedata også etter at UltraStar-notene er generert.

## 7.6 Note-segmentering

Pitch-, timing-, tekst- og onset-data brukes til å generere individuelle sangnoter/stavelser.

## 7.7 Frasestruktur

Programmet skal foreslå naturlige frase-/linjeskift basert på:

* pauser,
* tekststruktur,
* musikalsk timing,
* eksisterende synkroniseringsdata.

---

# 8. Confidence-modell

Automatisk analyse skal ikke bare produsere et resultat.

Den skal også, der praktisk mulig, produsere informasjon om hvor pålitelig resultatet antas å være.

Eksempler:

* pitch confidence,
* lyric match confidence,
* timing confidence,
* note-boundary confidence,
* source confidence.

Editoren skal kunne bruke dette til å fremheve områder brukeren bør kontrollere.

---

# 9. Intern sangmodell

UltraStar-formatet skal være et import-/eksportformat, ikke programmets interne arbeidsmodell.

En intern modell skal minst kunne representere:

**Song**

* metadata
* source media
* audio/video offsets
* BPM/timing

**Phrase**

* start/slutt
* noter
* tekststruktur

**Note**

* start
* slutt/varighet
* pitch
* stavelse/tekst
* note type
* confidence
* analyse-referanser

**AnalysisData**

* waveform
* pitch curve
* beat grid
* vocal track
* alignment
* confidence-data

Dette gjør analyse, editor og eksport uavhengige av hverandre.

---

# 10. Prosjektformat

Brukeren skal kunne lagre arbeidet og fortsette senere uten ny analyse.

Prosjektformatet skal derfor inneholde:

* intern sangmodell,
* kildeinformasjon,
* metadata,
* tekst,
* brukerendringer,
* analyse-cache,
* eventuelle mediefiler/referanser,
* prosjektversjon.

Prosjektformatet skal ha eksplisitt versjonering slik at fremtidige programversjoner kan migrere eldre prosjekter.

---

# 11. Authoring Editor

Dette er prosjektets høyest prioriterte brukerfunksjon.

Editoren skal følge etablerte DAW- og videoredigeringskonvensjoner der disse er relevante.

## 11.1 Hovedvisning

Editoren skal kunne vise:

* tidslinje,
* waveform,
* playback cursor/playhead,
* pitch-kurve,
* redigerbare toneblokker,
* tekst/stavelser,
* frasegrenser,
* analyse-confidence,
* UltraStar-lignende live preview.

---

## 11.2 Live preview

Programmet skal under avspilling kunne vise sangen på en måte som etterligner hvordan den vil opptre i UltraStar.

Når brukeren endrer:

* tekst,
* pitch,
* notevarighet,
* timing,
* frasestruktur,

skal preview oppdateres uten eksport eller regenerering.

---

## 11.3 Grunnleggende noteoperasjoner

Brukeren skal kunne:

* velge én eller flere noter,
* flytte noter i tid,
* flytte pitch opp/ned,
* endre notevarighet,
* opprette note,
* slette note,
* splitte note,
* slå sammen noter,
* kopiere,
* klippe ut,
* lime inn,
* duplisere.

Teksten skal følge relevant note/stavelse på en forutsigbar måte.

---

## 11.4 Tekstredigering

Brukeren skal kunne:

* redigere tekst direkte,
* endre stavelsesdeling,
* flytte tekst mellom noter,
* kombinere eller splitte tekst/note-koblinger.

---

## 11.5 DAW-lignende navigasjon

Editoren skal støtte:

* play/pause,
* scrubbing,
* seek,
* zoom,
* horisontal scrolling,
* vertikal scrolling der relevant,
* markering av område,
* play from selection,
* loop selection.

**Play from selection og loop selection skal regnes som sentrale arbeidsverktøy, ikke sekundære funksjoner.**

---

## 11.6 Standard hurtigtaster

Programmet skal så langt mulig følge kjente desktop-/DAW-konvensjoner.

Eksempel:

* `Ctrl+C` – Copy
* `Ctrl+V` – Paste
* `Ctrl+X` – Cut
* `Ctrl+Z` – Undo
* `Ctrl+Y` / `Ctrl+Shift+Z` – Redo
* `Ctrl+A` – Select all
* `Delete` – Delete selection
* `Space` – Play/Pause

Andre hurtigtaster defineres i GUI-designfasen.

---

# 12. Undo/Redo

Undo/Redo er et grunnleggende arkitekturkrav.

Redigeringsmotoren bør benytte et command-basert system hvor operasjoner kan utføres og reverseres.

Eksempler:

* MoveNote
* ResizeNote
* ChangePitch
* SplitNote
* MergeNotes
* DeleteNote
* EditLyrics
* ChangePhraseBoundary
* PasteNotes

Dette skal ikke implementeres som et sent tillegg.

---

# 13. Validering

Validering skal foregå både:

* kontinuerlig under redigering,
* eksplisitt før eksport.

Mulige kontrollpunkter:

* overlappende noter,
* manglende tekst,
* note uten stavelse,
* tekst uten note,
* ulogiske pitch-sprang,
* ugyldige frasegrenser,
* manglende media,
* ugyldige metadata,
* ugyldig UltraStar-struktur.

Warnings og errors skal skilles.

---

# 14. UltraStar eksport

Programmet skal kunne produsere en komplett sangmappe som kan brukes direkte i UltraStar-kompatibel programvare.

Eksportmodulen skal være separat fra den interne sangmodellen.

Eksport skal kunne omfatte:

* sangdata,
* artist/tittel,
* BPM,
* GAP/timing,
* noter,
* tekst,
* fraser,
* audio,
* cover,
* eventuell video,
* video-offset,
* relevante metadata.

Eksport skal valideres før ferdigstillelse.

---

# 15. Teknologiprinsipp

Prosjektet skal i utgangspunktet **ikke utvikle eksisterende standardproblemer fra bunnen av** dersom en moden og kompatibel open-source-løsning finnes.

Før implementering skal eksisterende løsninger undersøkes for:

* funksjonalitet,
* kodekvalitet,
* plattformstøtte,
* ytelse,
* lisens,
* vedlikeholdsstatus,
* integrerbarhet,
* avhengigheter,
* offline-støtte.

Aktuelle eksisterende prosjekter og teknologier skal behandles som kandidater, ikke automatisk som valgte dependencies.

Dette inkluderer blant annet tidligere identifiserte:

* UltraSinger,
* ultrasongs,
* UltraStar Play,
* yt-dlp,
* FFmpeg,
* vokalseparasjonsverktøy,
* ASR-/alignment-verktøy,
* pitch-analyseverktøy.

## 15.1 Utviklingskonvensjoner

Prosjektet skal som hovedregel følge etablerte konvensjoner for valgt programmeringsspråk, rammeverk og verktøykjede fremfor å utvikle egne stilregler uten konkret behov.

Når teknologistacken er valgt skal prosjektet normalt bruke standardiserte verktøy for formattering, linting og eventuell statisk typekontroll når modne løsninger finnes.

Agenter og utviklere skal:

* følge eksisterende prosjektstil når en slik er etablert,
* prioritere konsistent og lesbar kode,
* ikke introdusere en ny kode- eller mappestil uten konkret begrunnelse,
* ikke legge til dependencies uten et faktisk behov,
* vurdere nye dependencies mot lisens, vedlikeholdsstatus, plattformstøtte og integrerbarhet,
* holde midlertidig debuggingkode ute av produksjonskode,
* skrive reproducerbare tester der dette er praktisk,
* legge til regression-test ved feilrettinger når dette gir meningsfull beskyttelse mot gjentakelse,
* oppdatere relevant dokumentasjon dersom en implementasjonsendring gjør eksisterende dokumentasjon feil.

Eksakte formatterings-, lint- og typekontrollverktøy skal ikke låses før teknologistacken er valgt.

## 15.2 Reproduserbart utviklingsmiljø

Prosjektets utviklingsmiljø skal kunne rekonstrueres på en ny maskin uten avhengighet av udokumentert lokal kunnskap.

Valg av programmeringsspråk, GUI-rammeverk, build-system og øvrig hovedstack skal baseres på teknisk research og prosjektkrav. ChatGPT/Work skal legge frem begrunnede forslag, og prosjekteieren godkjenner vesentlige teknologivalg.

Når stacken er valgt skal repositoryet dokumentere de opplysningene som faktisk er nødvendige for reproduksjon, blant annet:

* støttede eller nødvendige runtime-versjoner,
* dependency-installasjon,
* build- og testkommandoer,
* nødvendige eksterne verktøy og modeller,
* eventuelle versjonskrav,
* hvordan en ny utviklingsmaskin kommer til en fungerende tilstand.

Versjoner skal låses eller avgrenses når dette er nødvendig for stabil reproduksjon.

Maskinspesifikke filer, cache, store regenererbare arbeidsdata og andre lokale artefakter skal holdes utenfor Git når de ikke har varig prosjektverdi.

Det skal ikke produseres detaljert eller spekulativ setup-dokumentasjon for en teknologistack som ennå ikke er valgt. Dokumentasjonen skal opprettes og utvides når den inneholder konkret informasjon som er nødvendig for å bygge, teste, kjøre eller videreutvikle prosjektet.

---

# 16. Utviklingsfaser

## Fase 0 – Prosjektgrunnlag

Mål:

* opprette offentlig GitHub-repository for prosjektet,
* etablere branch-strategi med `main` som stabil integrasjonsbranch og avgrensede WIP-/arbeidsbrancher,
* etablere dokumentstruktur og skille mellom versjonskontrollert prosjektkunnskap og lokal agenttilstand,
* etablere autoritetshierarki mellom lokal working tree, GitHub `main` og WIP-/arbeidsbrancher, samt regler for konflikthåndtering,
* etablere `.gitignore` for lokal agent-workspace, cache, scratch-data og andre ikke-autoritative arbeidsartefakter,
* etablere AGENTS.md,
* etablere minimumsmiljøet som trengs for build/test og teknologievaluering uten å låse produksjonsstacken før Fase 1,
* dokumentere prinsipper for reproduserbart utviklingsmiljø,
* dokumentere utviklingskonvensjoner på prinsippnivå,
* etablere foreløpig permissiv lisensstrategi med MIT som mål, med endelig bekreftelse etter dependency- og lisensgjennomgangen i Fase 1.

Ingen omfattende produktkode og ingen prematur låsing av programmeringsspråk, GUI-rammeverk eller produksjonsverktøykjede.

## Fase 1 – Teknologikartlegging og proof-of-concept

Undersøk hver nødvendig funksjon:

* eksisterende open-source-løsninger,
* integrerbarhet,
* Windows/Linux,
* lisens,
* ytelse,
* kvalitet.

Bygg små testharness der sammenligning kreves.

Resultatet skal være dokumenterte teknologivalg.

## Fase 2 – Core domain

Implementer:

* intern sangmodell,
* prosjektformat,
* UltraStar parser/writer,
* validator,
* grunnleggende tester.

Ingen full GUI nødvendig.

## Fase 3 – Acquisition

Implementer:

* sangsøk,
* source selection,
* media acquisition,
* metadata,
* Lyrics Acquisition Engine.

Output skal være normaliserte interne objekter.

## Fase 4 – Generation

Integrer:

* audio preprocessing,
* vocals,
* transcription/alignment,
* BPM,
* pitch,
* notes,
* phrases,
* confidence.

Resultatet skal være et komplett redigerbart sangutkast.

## Fase 5 – Editor

Bygg editoren rundt den etablerte interne modellen.

Første editorversjon prioriterer funksjon foran grafisk polering.

Må støtte:

* playback,
* waveform,
* playhead,
* noter,
* tekst,
* flytting,
* resize,
* pitch,
* split,
* merge,
* undo/redo,
* play from selection,
* loop selection.

## Fase 6 – Integrasjon og UX

Koble hele arbeidsflyten:

**Search → Generate → Edit → Validate → Export**

Forbedre:

* intuitivitet,
* respons,
* tastaturflyt,
* menyer,
* feilhåndtering,
* live preview,
* confidence-visning.

## Fase 7 – Stabilisering

* regression testing,
* ytelsesarbeid,
* crash handling,
* Windows-installer,
* stabil Windows release,
* eventuell portabel Windows-build dersom dette kan vedlikeholdes uten vesentlig ekstra kompleksitet,
* dokumentasjon.

Linux bring-up starter når Windows-versjonen har stabil arkitektur.

---

# 17. Testing

Testing skal utføres kontinuerlig.

Minimum:

* unit tests,
* parser/export round-trip tests,
* integration tests,
* prosjektformat-tester,
* UI command tests,
* regression tests.

Analysekomponentene bør også kunne benchmarkes mot kjente korrekt formatterte sanger.

Målbare parametere kan inkludere:

* BPM error,
* onset timing error,
* pitch error,
* duration error,
* lyrics alignment error,
* note count discrepancy.

## 17.1 Automatisk verifikasjon og praktisk brukertesting

Automatiske tester, statiske kontroller og andre relevante verifikasjoner skal kjøres kontinuerlig i den grad de passer arbeidsoppgaven.

Praktisk brukertesting av prosjekteieren er ikke nødvendig etter hver liten implementasjonsendring.

Brukertesting skal i stedet legges til naturlige kontrollpunkter der:

* en sammenhengende funksjon eller arbeidsflyt er faktisk testbar,
* UX eller praktisk oppførsel trenger menneskelig vurdering,
* en beslutning vil påvirke videre implementasjon,
* eller en milepæl er klar for kontroll før neste større arbeidstrinn.

En arbeidsordre kan definere ett eller flere eksplisitte human-review-kontrollpunkter når dette er relevant.

Mellom slike kontrollpunkter kan agenten utføre flere avgrensede og verifiserte delendringer uten å kreve ny brukergodkjenning for hvert enkelt teknisk steg.

---

# 18. Repository som source of truth

Chat-samtaler skal ikke være prosjektets eneste hukommelse.

Repositoryet skal inneholde autoritativ dokumentasjon og annen prosjektkunnskap som har varig verdi for implementasjon, vedlikehold, testing eller forståelse av prosjektet.

Midlertidig agenttilstand, scratch-data og andre arbeidsartefakter som bare er relevante for én lokal arbeidsøkt skal ikke versjonskontrolleres med mindre de senere vurderes å ha varig prosjektverdi.

## 18.1 GitHub og repository-strategi

Prosjektets autoritative Git-repository skal ligge på GitHub.

Repositorynavn:

`ultrastar-song-creator`

Prosjektnavnet i dokumentasjon og brukergrensesnitt skal fortsatt være:

**UltraStar Song Creator**

Repositoryet skal være offentlig med mindre et senere konkret behov tilsier noe annet.

Begrunnelsen er både prosjektets ønskede åpne karakter og at offentlig repository reduserer enkelte praktiske begrensninger knyttet til blant annet samarbeid, forks og GitHub-hosted CI/Actions.

### Branch-strategi

`main` skal representere en konsistent, testet og godkjent prosjektstatus.

Omfattende agentarbeid og andre ikke-trivielle endringer skal normalt ikke utføres direkte på `main`.

Avgrensede arbeidsoppgaver skal normalt utføres på egne WIP-/arbeidsbrancher, for eksempel:

```text
work/phase0-agents-md
work/task-004-ultrastar-parser
```

Arbeidsbrancher skal brukes som både isolasjon og redundans under pågående arbeid.

Agenten skal committe ved naturlige og konsistente kontrollpunkter når dette er praktisk, slik at arbeid kan gjenopptas dersom en agentøkt avbrytes eller ressursbudsjettet tar slutt.

Ferdig arbeid merges til `main` først når relevante acceptance criteria og verifikasjoner er oppfylt.

## 18.2 Lisensstrategi

Prosjektets egen kode skal sikte mot en permissiv open-source-lisens som tillater at andre kan:

* lese og studere kildekoden,
* endre kildekoden,
* distribuere egne versjoner,
* bruke prosjektet som grunnlag for videre arbeid.

**MIT License er foreløpig mål for prosjektets egen kode.**

Endelig lisensbekreftelse utsettes til Fase 1 har gjennomført dependency- og lisensgjennomgang.

Tredjepartsavhengigheter skal vurderes separat. En dependency skal ikke innføres dersom lisensvilkårene påfører prosjektet vesentlig andre distribusjons- eller lisenskrav enn den tiltenkte permissive modellen uten eksplisitt prosjektvedtak.

Lisenskompatibilitet er derfor et eksplisitt kriterium i Technology Feasibility & Reuse Audit.

## 18.3 Versjonskontrollert prosjektkunnskap

Følgende typer filer skal normalt versjonskontrolleres:

* prosjektets masterspesifikasjon,
* README,
* AGENTS.md,
* produktspesifikasjoner,
* arkitekturdokumentasjon,
* dependency- og lisensdokumentasjon,
* editorspesifikasjon,
* UltraStar-formatdokumentasjon,
* teststrategi,
* aksepterte eller foreslåtte ADR-er,
* arbeidsplaner som har verdi på tvers av agentøkter eller inngår i prosjektets varige gjennomføringshistorikk,
* kildekode,
* tester,
* reproducerbare verktøy og scripts.

Foreslått struktur:

```text
/
├── .gitignore
├── AGENTS.md
├── README.md
├── docs/
│   ├── PROJECT_MASTER.md
│   ├── PRODUCT_SPEC.md
│   ├── ARCHITECTURE.md
│   ├── DEPENDENCIES.md
│   ├── EDITOR_SPEC.md
│   ├── ULTRASTAR_FORMAT.md
│   ├── TEST_STRATEGY.md
│   ├── decisions/
│   │   └── ADR-xxxx-*.md
│   └── exec-plans/
│       ├── active/
│       ├── completed/
│       └── backlog.md
├── src/
├── tests/
└── tools/
```

`docs/exec-plans/` er ikke ment som permanent lagringssted for alle agenters operative planer.

En exec-plan skal bare committes dersom den:

* skal brukes på tvers av flere arbeidsøkter,
* koordinerer flere avgrensede leveranser,
* dokumenterer en vesentlig migrasjon eller implementeringssekvens,
* eller på annen måte har varig prosjektverdi.

Kortvarige planer for én enkelt agentøkt skal normalt behandles som lokal arbeidstilstand.

## 18.4 Lokal agenttilstand og `.gitignore`

Arbeidsdata som ikke er nødvendige for programmets funksjon og heller ikke har en eksplisitt forklarende eller historisk verdi for en ekstern utvikler skal normalt holdes utenfor Git.

Dette omfatter blant annet:

* agent-scratchpads,
* midlertidige prompts og planer,
* lokale ressurs- og tokennotater,
* midlertidige arbeidslogger,
* cache,
* regenererbare mellomresultater,
* uferdige analyseutkast,
* lokale benchmark-data som enkelt kan regenereres,
* andre verktøyspesifikke midlertidige filer.

Prosjektet skal ha en dedikert lokal agent-workspace som ignoreres av Git, for eksempel:

```text
.agent-local/
```

Ytterligere verktøyspesifikke cache- eller arbeidsmapper skal legges til `.gitignore` når de faktisk tas i bruk.

Lokale arbeidsfiler skal ikke være eneste sted hvor en beslutning, spesifikasjon eller nødvendig prosjektstatus finnes. Dersom lokal informasjon blir nødvendig for videre utvikling, skal den flyttes eller kondenseres til riktig versjonskontrollert dokument før den lokale arbeidsfilen anses som disponibel.


## 18.5 Autoritetshierarki og konflikthåndtering

Prosjektet skal skille mellom **hvilken kopi av prosjektstatusen som er mest autoritativ** og **hvilket dokument som regulerer et bestemt fagområde**.

Denne seksjonen definerer autoritet mellom lokale filer, GitHub og arbeidsbrancher.

### Autoritetsrekkefølge

Ved forskjeller mellom flere kopier av prosjektets filer gjelder følgende rekkefølge:

1. **Lokal autoritativ working tree på prosjekteierens primære utviklingsmaskin**
2. **GitHub `main` / `origin/main`**
3. **Andre stabile commits eller branches som brukes som referanse**
4. **WIP-/arbeidsbrancher**

Den lokale autoritative working tree er høyeste kildeinstans fordi den kan inneholde godkjente endringer som ennå ikke er committet eller pushet.

GitHub `main` er prosjektets høyeste **delte og synkroniserte** autoritet og skal representere siste publiserte stabile prosjektstatus.

WIP-/arbeidsbrancher representerer pågående, avgrenset eller eksperimentelt arbeid og har derfor ikke generell autoritet over den lokale stabile prosjektstatusen eller `main`.

### Lokal working tree mot GitHub `main`

Dersom en lokal autoritativ fil sier A og tilsvarende fil på GitHub `main` sier B, skal A behandles som gjeldende prosjektstatus.

Agenten skal i denne situasjonen anta at den lokale filen kan representere en nyere, ennå ikke synkronisert beslutning.

Agenten skal:

* bevare den lokale versjonen,
* ikke stille erstatte den med GitHub-versjonen,
* gjøre avviket synlig dersom det er relevant for arbeidsordren,
* og unngå operasjoner som kan overskrive nyere lokal informasjon.

Når en lokal endring er akseptert som varig prosjektstatus, skal den committes og pushes så snart det er praktisk slik at GitHub `main` ikke blir unødvendig hengende etter.

### WIP-/arbeidsbrancher

En WIP-/arbeidsbranch skal ikke automatisk overstyre autoritative filer i lokal stabil working tree eller `main`.

Endringer på en arbeidsbranch har autoritet **innenfor den eksplisitte arbeidsordren som branchen representerer**, men skal ikke tolkes som en generell prosjektbeslutning før de er:

1. ferdigstilt,
2. verifisert,
3. godkjent der godkjenning kreves,
4. og integrert i den stabile prosjektstatusen.

En agent som arbeider på en WIP-branch skal derfor skille mellom:

* eksisterende autoritativ prosjektstatus,
* og foreslåtte eller pågående endringer som bare finnes på arbeidsbranchen.

### Tidsstempel er ikke autoritet

Filens endringstid, opprettelsesdato eller andre filesystem-tidsstempler skal ikke brukes som selvstendig grunnlag for å avgjøre hvilken prosjektversjon som er autoritativ.

Autoritet bestemmes av:

* hvor filen befinner seg i prosjektets definerte arbeidsflyt,
* hvilken branch/status den tilhører,
* om endringen er eksplisitt godkjent,
* og reglene i denne spesifikasjonen.

### Konflikt mellom lokale autoritative dokumenter

Dette autoritetshierarkiet løser konflikter mellom **kopier og repository-status**, men gir ikke en agent rett til å velge fritt mellom to motstridende lokale autoritative dokumenter.

Dersom to lokale autoritative dokumenter gir uforenlige instrukser og konflikten ikke kan løses av en eksplisitt dokumentregel eller akseptert ADR, skal agenten:

* ikke bruke filens tidsstempel som avgjørelse,
* ikke velge den løsningen som virker mest sannsynlig,
* ikke stille endre ett av dokumentene,
* stoppe den berørte delen av arbeidet,
* og rapportere konflikten til prosjekteieren.

### Synkroniseringsprinsipp

Den ønskede normaltilstanden er:

**lokal autoritativ working tree → commit → push → GitHub `main`**

Avvik mellom lokal stabil prosjektstatus og GitHub `main` skal derfor normalt være midlertidige.

GitHub fungerer som den delte, redundante og historiske prosjektkilden, mens den lokale autoritative working tree kan ligge foran mellom godkjent lokal endring og neste synkronisering.


---

# 19. AGENTS.md

`AGENTS.md` skal være kort.

Den skal fungere som et kart, ikke som hele prosjektmanualen.

Som normal målsetting skal filen holdes rundt **200–250 ord**. Dersom nye regler gjør filen vesentlig større, skal detaljer normalt flyttes til riktig dokument under `docs/`, mens `AGENTS.md` beholder den operative hovedregelen og en henvisning.

Build-, test- og setup-kommandoer kan være ufullstendige i Fase 0 dersom teknologistacken ennå ikke er valgt. De skal fylles inn så snart kommandoene faktisk er etablert.

Den skal fortelle agenten:

* hva prosjektet er,
* hvilke dokumenter som er autoritative,
* hvordan autoritet mellom lokal working tree, GitHub `main` og WIP-/arbeidsbrancher skal tolkes,
* hvordan prosjektet bygges,
* hvordan tester kjøres,
* hvilke arkitekturregler som ikke skal brytes,
* når agenten skal stoppe og rapportere,
* hvilke mapper den normalt kan endre.

Detaljer skal ligge i `docs/`.

---

# 20. Beslutningslogg

Arkitekturvalg som er vanskelige eller kostbare å reversere skal registreres som Architecture Decision Records.

Eksempel:

```text
ADR-0001 Internal song model
ADR-0002 GUI framework
ADR-0003 Audio analysis backend
ADR-0004 Project storage format
ADR-0005 Lyrics acquisition strategy
```

Hver ADR skal dokumentere:

* problem,
* alternativer,
* valgt løsning,
* begrunnelse,
* konsekvenser.

Agenter skal ikke omgjøre en akseptert ADR uten eksplisitt arbeidsordre.

---

# 21. Rollefordeling

## Brukeren

Prosjekteier og endelig beslutningstaker.

Ansvar:

* produktmål,
* UX-vurdering,
* prioriteringer,
* godkjenning av viktige designvalg og vesentlige teknologivalg,
* praktisk testing ved naturlige human-review-kontrollpunkter.

## ChatGPT

Brukes primært som:

* kravanalytiker,
* produktarkitekt,
* diskusjonspartner,
* dokumentforfatter,
* reviewer,
* oppgaveplanlegger,
* rådgiver ved valg av programmeringsspråk, rammeverk og øvrig teknologistack.

ChatGPT skal hjelpe med å gjøre ideer om til eksplisitte krav før dyrere agentarbeid startes.

Når tekniske valg krever kompetanse prosjekteieren ikke forventes å ha, skal ChatGPT/Work etter nødvendig research legge frem et begrunnet forslag basert på prosjektets krav og identifiserte tradeoffs. Prosjekteieren er endelig beslutningstaker.

## ChatGPT Work

Brukes til større flertrinnsoppgaver som:

* teknologiresearch,
* dependency-evaluering,
* sammenligning,
* dokumentasjon,
* analyser som krever flere filer/verktøy,
* koordinering av avgrensede leveranser.

## Codex/Astra

Brukes primært til:

* repository-analyse,
* kodeimplementering,
* testing,
* refaktorering,
* bygging,
* debugging,
* automatiserte benchmarks,
* konkrete tekniske arbeidsordre.

---

# 22. Agentregler

En agent skal ikke få beskjed om å «bygge hele programmet».

Hver arbeidsordre skal være avgrenset.

En arbeidsordre skal minst inneholde:

**Mål**
Hva som konkret skal være annerledes når oppgaven er ferdig.

**Kontekst**
Relevante moduler, filer og spesifikasjoner.

**In scope**
Hva agenten kan endre.

**Out of scope**
Hva den uttrykkelig ikke skal arbeide med.

**Constraints**
Arkitektur-, lisens-, plattform- og dependencykrav.

**Acceptance criteria**
Objektive krav som må være oppfylt.

**Verification**
Tester/kommandoer agenten skal kjøre.

**Stop conditions**
Situasjoner hvor agenten skal stoppe og rapportere i stedet for å improvisere.

**Deliverable**
Hva agenten skal levere og rapportere.

---

## 22.1 Ressursbevisst agentarbeid

Codex, Astra og andre utførende agenter skal planlegge arbeidet med hensyn til begrenset kontekst-, token- og kjøretidsbudsjett.

Agenten skal ikke bruke en uforholdsmessig stor del av tilgjengelige ressurser på omfattende planlegging, generell repository-utforskning eller analyse som ikke er nødvendig for den aktuelle arbeidsordren.

Før større arbeid starter skal agenten:

* identifisere den minste sammenhengende leveransen som oppfyller arbeidsordrens mål,
* prioritere nødvendige undersøkelser fremfor generell utforskning,
* dele omfattende arbeid i klart avgrensede deloppgaver dersom hele oppgaven ikke med rimelig sikkerhet kan fullføres, testes og rapporteres innenfor én arbeidsøkt,
* unngå å starte flere deloppgaver enn den forventer å kunne ferdigstille eller bringe til et definert kontrollpunkt,
* bevare en fungerende og dokumentert prosjektstatus mellom deloppgaver.

Planlegging skal være tilstrekkelig til å redusere risiko for feil og unødvendig omarbeid, men skal ikke bli et mål i seg selv.

Når ressursbegrensninger begynner å true fullføring av arbeidsordren, skal agenten prioritere i følgende rekkefølge:

1. fullføre påbegynt konsistent endring,
2. kjøre relevante verifikasjoner,
3. dokumentere faktisk status og eventuelle gjenværende problemer,
4. beskrive neste konkrete arbeidssteg.

Agenten skal heller levere en mindre, ferdig og verifisert deloppgave enn å starte en større implementasjon som blir stående halvferdig.

Dersom hele arbeidsordren viser seg å være for omfattende for én arbeidsøkt, skal agenten stoppe ved et naturlig og reproducerbart kontrollpunkt og rapportere hva som er ferdigstilt, hva som er verifisert og hva som gjenstår.

Agenten skal ikke basere strategien på at den kjenner nøyaktig hvor mye token-, kontekst- eller kjøretidsbudsjett som gjenstår. Arbeidet skal derfor være strukturert slik at meningsfulle delresultater oppnås fortløpende.

Rapporter og dokumenter skal bare produseres når de har en konkret funksjon for implementasjon, beslutning, verifikasjon, overlevering eller varig prosjektkunnskap. Agenten skal ikke bruke ressurser på ekstra rapporter som bare gjentar arbeidsordren, eksisterende autoritativ dokumentasjon eller informasjon som ikke påvirker videre arbeid.

Sluttrapportering skal være tilstrekkelig til å vise hva som faktisk ble gjort og verifisert, men skal holdes proporsjonal med oppgavens størrelse.

---

# 23. Stop conditions

Codex/Work skal normalt stoppe og rapportere dersom:

* løsningen krever brudd på et definert arkitekturkrav,
* en obligatorisk konto/API-nøkkel viser seg nødvendig,
* lisensstatus er uavklart,
* spesifikasjonen er internt selvmotsigende,
* to lokale autoritative dokumenter gir uforenlige instrukser som ikke kan løses av en eksplisitt regel eller akseptert ADR,
* en større offentlig API-/datastruktur må antas uten dokumentasjon,
* oppgaven krever omfattende endring utenfor definert scope,
* tester viser et fundamentalt problem med eksisterende design.

Agenten skal ikke «løse» slike situasjoner ved stille å endre prosjektets premisser.

---

# 24. Arbeidsordre-mal

```text
TASK:
[Kort navn]

OBJECTIVE:
[Én presis måltilstand]

AUTHORITATIVE DOCUMENTS:
- docs/...
- ADR-...

CURRENT STATE:
[Relevant eksisterende status]

IN SCOPE:
- ...

OUT OF SCOPE:
- ...

CONSTRAINTS:
- Windows-first / Linux-compatible architecture
- No mandatory account/API-key dependencies
- Follow existing domain model
- Do not change accepted ADRs

IMPLEMENTATION REQUIREMENTS:
- ...

ACCEPTANCE CRITERIA:
1. ...
2. ...
3. ...

VERIFICATION:
- run ...
- run ...
- inspect ...

STOP CONDITIONS:
Stop and report before implementation if:
- ...
- ...

FINAL REPORT:
Return:
- changed files
- implementation summary
- tests executed and results
- unresolved issues
- recommended next task
```

---

# 25. Agentisk arbeidsstrategi

Arbeid skal generelt følge:

**Plan → avgrens → implementer → test → review → merge → dokumenter**

Store oppgaver deles opp før implementering.

Parallelle agenter brukes bare når arbeidsoppgavene faktisk er uavhengige og ikke forventes å endre de samme modulene.

Agenten skal ikke bruke betydelig tid på å utforske produktkrav som kunne vært avklart billigere i vanlig ChatGPT først.

Prosjekteieren skal ikke avbrytes for godkjenning av hvert enkelt lite teknisk steg. Når menneskelig vurdering er nødvendig, skal arbeidet så langt mulig struktureres rundt naturlige kontrollpunkter hvor en sammenhengende funksjon, UX-flyt eller teknisk beslutning kan vurderes meningsfullt.

Mellom slike kontrollpunkter skal agenten bruke automatiske tester og andre relevante verifikasjoner for å holde arbeidet konsistent.

---

# 26. Første agentiske milepæl

Første større Work/Codex-oppgave skal ikke være å skrive hele applikasjonen.

Den første tekniske milepælen skal være:

## Technology Feasibility & Reuse Audit

Agenten skal undersøke eksisterende open-source-komponenter for:

* medieinnhenting,
* lyrics,
* vokalseparasjon,
* ASR/alignment,
* BPM,
* pitch,
* note segmentation,
* UltraStar-format,
* avspilling,
* timeline rendering,
* eksisterende UltraStar-editorer.

Resultatet skal være:

1. konkret dependency-matrise,
2. lisensgjennomgang,
3. Windows/Linux-vurdering,
4. benchmark/proof-of-concept der nødvendig,
5. anbefalt komponent per funksjon,
6. komponenter som bør utvikles internt,
7. identifiserte tekniske risikoer.

**Ingen produksjonsarkitektur skal låses før denne evalueringen er gjennomført.**

---

# 27. Prosjektets foreløpige suksesskriterium

En tidlig komplett versjon anses funksjonelt vellykket når brukeren kan:

1. søke etter en sang,
2. velge korrekt kilde/innspilling,
3. la programmet hente nødvendige data,
4. generere et brukbart førsteutkast,
5. åpne dette direkte i editoren,
6. spille sangen i editoren,
7. oppdage en feil,
8. korrigere timing/pitch/tekst med direkte manipulering,
9. bruke loop/play-from-selection for å kontrollere endringen,
10. se endringen i UltraStar-lignende preview,
11. validere prosjektet,
12. eksportere en fungerende UltraStar-sang.

Dette er prosjektets første komplette ende-til-ende-mål.
