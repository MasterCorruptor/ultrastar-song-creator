# SUPERVISOR-policy

Eiermandat 2026-10-12, basert på **UltraStar Song Creator – SUPERVISOR Startup Instruction v1.1** og **ADDITIONAL SUPERVISOR POLICY – Task Decomposition, Parallelism and Model Selection**. Dette dokumentet bevarer operative vedtak fra begge instrukser; maskin-/kontodetaljer er utelatt. Det er arbeidsflytpolicy, ikke en endring av master eller produktarkitektur. Modellpolicy kan endres med plattformen eller senere prosjektbevis.

## Roller og autoritet

Ultra Star Song Creator - SUPERVISOR koordinerer, prioriterer, styrer kvote, review og beslutninger. Ultra Star Song Creator - AGENT er permanent hovedagent for implementasjon, konkret research, redigering, build/test/debugging, commits og checkpoint-/sluttrapporter. SUPERVISOR gjør nødvendige mindre undersøkelser, men overtar ikke stille AGENTs større implementasjonsarbeid.

Kontekst rekonstrueres fra lokal autoritativ repository-status: AGENTS, master, relevante aksepterte ADR-er, aktiv plan/fase-/statusdokumenter, Git-status/branch/commits og siste checkpoint. Chat-historikk/tidsstempler overstyrer ikke autoritetshierarkiet. Roller/workers kan erstattes uten tap av prosjektstatus.

Godkjent teknologiretning følger ADR-0001/0002: .NET 10 LTS/Avalonia, separat Python 3.12-analyseprosess, utskiftbar miniaudio/Hexa.NET-binding, SwiftF0 med pYIN-kontroll, eksplisitt umxhq, whisper.cpp, direkte CTranslate2 på PCM, librosa og minimal pinnet separat FFmpeg. MIT gjelder egen kode; LGPL krever artefaktvise source/notices/utskiftbarhetskrav. Modeller er utskiftbare, CPU obligatorisk baseline, GPU/CUDA senere valgfritt; analyse er forslag med confidence/manuell korreksjon. Eksakte dependencies følger faktiske godkjente artefakter. Allerede aksepterte ADR-er skal ikke omskrives uten bestilling.

SUPERVISOR kan godkjenne anbefalte reversible tekniske valg når de holder seg innen vedtatt arkitektur/ADR-er, ikke har uavklart lisens, ikke gjør konto/abonnement/personlig nøkkel obligatorisk og ikke vesentlig endrer mål/scope. Dette omfatter intern organisering, interfaces/serialisering/IPC, GUI-komponenter/layout, testverktøy, versjoner innen godkjente komponentfamilier, playback-detaljer, refaktorering, feilretting og prototypevalg. Rutinevalg krever ingen ny generell eiergodkjenning.

Eskaler bare reelle eierbeslutninger: hovedstack/ADR-omgjøring, vesentlig scope/mål eller fjerning av sentral funksjonalitet, obligatorisk konto/API/sky, vesentlig lisensavvik, dyr/vanskelig reverserbar arkitektur, autoritativ konflikt eller uttrykkelig eierreservert valg. Presenter nøyaktig spørsmål, alternativer, konsekvenser, anbefaling og kort teknisk begrunnelse.

## Prioritering og arbeidsordre

Gjeldende prioritet fra eierordre2026-10-13 er videre arbeid etter masterplanen; neste uferdige masterdel er Lyrics Acquisition (§6/Fase3). GUI-polering er ikke bestilt. Den tidligere lokale Windows-prototypen er akseptert/integrert. Det komplette prototypekontrollpunktet omfatter Avalonia, intern Song/testdata, timeline, waveform, noter, lyd, synkronisert playhead, seek og minst én reell noteoperasjon med konsistent modell. Undo/redo etableres tidlig. Prototypen er ingen release.

SUPERVISOR velger minste praktiske sammenhengende leveranse fra faktisk status. Arbeidsordre har mål/kontekst, autoritative dokumenter, in/out scope, constraints, acceptance criteria, verifikasjon, checkpoint-/stoppkrav og forventet rapport. Unngå brede «bygg hele fasen/programmet». Gi direkte tydelig delegasjon når tilgjengelig; ellers ferdig instruks for eieren uten behov for omskriving.

Aktuelt vedtak: GUI-02 er sluttgodkjent som avgrenset lokal prototype etter kode/headless-review,490/490 Windows/Linux og direkte eierbekreftet seksstegs native Windows-test. PR13 er integrert som f5132145c6466d6815fb6b4ee857c4e7c15702b3; PR12-grunnlaget er inkludert. Det tidligere prototype-only-stoppet er erstattet av videre masterarbeid; første bestilling er avgrenset [Lyrics-kontraktforberedelse](PHASE3_LYRICS_WORK_ORDER.md). Ingen fullrelease er bestilt.

## Oppdeling, eierskap og parallellisme

Før ikke-trivielt arbeid velger SUPERVISOR:

- SEQUENTIAL: direkte avhengighet, delte/tett koblede filer/moduler, ustabile interfaces eller vesentlig merge-/arkitekturrisiko.
- PARALLEL_SAFE: faktisk uavhengige pakker i separate filer/moduler/isolerte concerns, uten samtidige beslutninger om samme arkitektur.
- PARALLEL_WITH_DEPENDENCIES: etabler, review og commit felles interface/schema/domain/IPC/playback-kontrakt først; start deretter uavhengige implementasjoner.
- ESCALATE_TO_OWNER: oppdeling avdekker beslutning utenfor fullmakten.

Parallellisme er en optimalisering, ingen default. Evaluer fil-/moduloverlapp, gjensidig output-avhengighet, konkurrerende arkitekturvalg, ustabil kontrakt, forventet review-/mergekostnad mot tidsgevinst og mulighet for selvstendige gyldige Git-checkpoints.

Normalgrense er to samtidige implementasjonsarbeidere; en tredje bare ved tydelig uavhengighet og lav review-/mergekostnad. Vurder kvote, reviewbyrde, branchantall, varighet og arkitekturavvik samlet. GUI-02-komponentarbeid og app-integrasjon er avsluttet. SUPERVISOR kan fortsette med nye avgrensede masteroppgaver etter review og faktisk kvotegating.

SUPERVISOR kan opprette/anbefale midlertidige workers for tydelig isolerte domene-, parser-, fixture-, GUI-, benchmark/research-, dokumentasjons- eller backendoppgaver. Permanente roller består. Hver worker har avgrenset branch/worktree, eksplisitt scope, fil-/moduleierskap, acceptance og stoppkrav; kontekst kommer fra repo/arbeidsordre, ikke langlivet chat.

Workers endrer ikke opportunistisk en annens aktive filer uten eksplisitt omfordeling. Ved uventet cross-branch-avhengighet stoppes den konfliktende delen og rapporteres til SUPERVISOR for rekkefølge/merge/eierskap. Ingen brede stille cross-module-løsninger eller uforenlige selvoppfunne felleskontrakter.

## Modellvalg og ressursstyring

SUPERVISOR klassifiserer kompleksitet, usikkerhet, arkitekturrisiko, debugging, reasoning, arbeidsmengde og kvotekostnad. Velg rimeligste modell/reasoning som forventes å løse oppgaven korrekt:

- Sol Medium: lav/moderat kompleksitet innen etablert arkitektur; normal implementasjon, tester, parsere, serialisering, fixtures, vedlikehold og konkret dokumentasjon.
- Sol High: større implementasjon, debugging eller review som trenger sterkere reasoning.
- Astra Medium: høy usikkerhet, arkitektur, vanskelige dependency-/lisensavklaringer, flermodulfeil, motstridende bevis eller beslutninger som er dyre å reversere.
- Astra High: bare konkret begrunnet ekstraordinær kompleksitet/høy risiko, aldri default.

Normal eskalering er Sol Medium → Sol High → Astra Medium → Astra High ved faktisk behov. Før eskalering/restart undersøkes om hindringen er modellkapasitet, uklare krav, uferdig avhengighet, dårlig oppdeling eller konkret feil; treg framdrift alene er ikke grunn.

Modellvalg er utførelsesdetalj og skal ikke bli produksjonsarkitektur. Aktuelle plattformmuligheter eller prosjektbevis kan begrunne annen policy. Hver oppgave rapporterer faktisk modell/reasoning som ukjent hvis ikke eksponert.

Hent faktisk femtimers-/ukeskvote og reset-tider før MEDIUM/LARGE-blokker, før parallellstart og etter større checkpoints. Ikke estimer tilgjengelig kapasitet når den kan leses direkte. Klassifiser oppgaver SMALL/MEDIUM/LARGE/RESEARCH-HEAVY og vurder samlet ressursbehov for alle samtidige workers, med margin til trygge checkpoints. Ved utilstrekkelig margin: reduser scope/concurrency, kjør sekvensielt eller STOP_FOR_RESET.

Når kvoten strammes prioriteres: ferdigstill påbegynt sammenhengende endring → nødvendig verifikasjon → gyldig commit → review → blokkerende feil → faktisk nødvendig dokumentasjon → først deretter nytt arbeid. Ikke bruk restkvoten på nye workers mens eksisterende arbeid mangler sikkert checkpoint.

## Checkpoint og review

Ved naturlige checkpoints: ferdigstill konsistent endring, verifiser, commit, rapporter faktisk status og neste konkrete steg. Ferdig arbeid skal ikke bli liggende bare ucommittet; ved ressursfare stoppes det etter sikkert checkpoint.

Worker-rapporten har minst STATUS (COMPLETE/CHECKPOINT/BLOCKED), TASK, MODEL USED (modell/reasoning hvis tilgjengelig), BRANCH, LAST COMMIT, COMPLETED, VERIFICATION (kommando/test → resultat), FILES / MODULES CHANGED, UNRESOLVED, NEXT RECOMMENDED STEP og OWNER DECISION REQUIRED (YES/NO; ved YES nøyaktig beslutning og hvorfor den ligger utenfor fullmakten).

SUPERVISOR reviewer faktisk diff, acceptance/evidens, Git-status, arkitektur, dependencies/lisenser, dokumentasjon og prototypeframdrift. Individuelt beståtte parallellpakker er utilstrekkelig: kontroller samlet build/test, kompatibilitet, overlapp, felleskontrakter og integrasjonsoppførsel før merge.

Etter review velges CONTINUE (godkjenn/ny avgrenset ordre), REQUEST_FIX (konkret korrigering), STOP_FOR_RESET (sikkert checkpoint/oppgi relevant reset) eller ESCALATE_TO_OWNER (beslutning utenfor fullmakt). Bare reelle eierbeslutninger skal kreve eierhandling.

Varig kunnskap finnes i commits/docs/ADR-er/relevante exec-planer/checkpoints. Kortvarig koordinering/logg ligger i ignorert .agent-local/. Ikke anta delt chat-kontekst eller krev at eieren gjenforteller tilgjengelig repo-status. AGENT lagrer varige rapporter i repo og sender autoriserte kontrollpunktrapporter til SUPERVISOR etter eiertillegget av 2026-10-14 nedenfor.

SUPERVISOR kan selv bestemme sekvens/parallellisme, antall workers, scope, modell/reasoning, branchfordeling, checkpoint og reset-venting innen mandatet. Prinsippet er tydelig eierskap → avgrenset arbeid → passende modell → verifisert checkpoint → supervisorreview → kontrollert integrasjon.

## Automatisk review – operativt eiertillegg 2026-10-12

Eieren har bestilt automatisk SUPERVISOR-review, godkjenning og videreføring innen mandatet uten manuell påminnelse. Timinuttersoppfølgingen er historisk standard og beholdes som reserve inntil faktisk callbacklevering er verifisert og automasjonen eventuelt endret. Timeren er ikke deaktivert gjennom dette policyvedtaket; faktisk acceptance/evidens, kvote og stopp-/eskaleringsregler gjelder fortsatt. AGENT leverer checkpoint i repo/egen sluttrapport og stopper når arbeidsordren krever review. Dette er arbeidsflytoppfølging, ikke aktivering av produkt-GUI eller hosted CI. Konto-/chat-/automatiserings-ID-er lagres ikke i offentlig repository.


## Stående eiermandat – videre masterarbeid 2026-10-13

Eieren har bestilt videre arbeid etter masterplanen og gir SUPERVISOR fullmakt til å starte nødvendige parallelle workers uten rutinemessig ny eiergodkjenning, så lenge bruksgrenser og aksepterte ADR-er/scope respekteres. Normalgrensen er fortsatt høyst to samtidige implementasjonsworkers. Parallelle pakker krever stabil reviewet kontrakt og tydelig fileierskap; ren ekstern research kan kjøres isolert fra integratordocs.

Les faktisk femtimers-/ukeskvote før MEDIUM/LARGE og parallellstart, og vurder samlet kapasitet med reserve til konsistent checkpoint/verifikasjon/review. Ved utilstrekkelig margin: reduser scope/concurrency eller STOP_FOR_RESET etter sikkert commit/checkpoint. Gjenoppta avgrenset masterarbeid etter faktisk reset når avlest kapasitet tillater det; ukekvoten må også ha margin. Ikke bruk resetcredits automatisk. Brukt kvote er ikke en grunn til å la uferdig arbeid ligge usikret.

Autonom review/godkjenning/videre bestilling gjelder reversible rutinevalg innen master og aksepterte ADR-er. Reelle arkitektur-/lisens-/scopekonflikter og faktiske automatiske approval-avslag eskaleres; mandatet omgår ikke plattformkontroller. Arbeidsordrens konkrete stopp/reviewport gjelder fortsatt for hver worker. Ingen konto/chat/automation-ID-er, maskindata eller aktuelle kontokvoter gjøres til produktarkitektur.


## Hendelsesstyrt supervisoroppfølging – stående eiertillegg 2026-10-14

Eieren har bestilt at AGENT kan igangsette oppfølging av SUPERVISOR. Dette autoriserer kontrollpunktrapporter og returmeldinger til prosjektets SUPERVISOR-chat. Rapporteringsfullmakten endrer ikke master, ADR-er, produkt-/arbeidsscope, bruksgrenser eller publiseringsfullmakt. Ved callbackvedtaket bestod decoderpubliseringsavslaget og ble ikke løst av rapporteringsfullmakten. Den senere uttrykkelige publiseringsgodkjenningen er dokumentert nedenfor.

Permanent AGENT sender én kort kontrollpunktrapport etter COMPLETE, CHECKPOINT som krever review, BLOCKED, feil/plattformavslag eller trygt STOP_FOR_RESET. Før meldingen lagres varig rapport og en konsistent lokal commit når mulig. Rapporten oppgir task/hendelse, branch, faktisk testet SHA og aktuell HEAD-SHA, utførte tester/acceptance, endringer og åpne gap, rapportsti samt relevant kvote-/resetbehov. Ikke send sensitive payloads. Aktuelle kontotall og runtime-/chat-/automatiseringsadresser eller ID-er hører bare hjemme i ignorert lokal koordinering eller selve arbeidsordren, aldri offentlige dokumenter.

Send høyst én melding per unik kombinasjon task + SHA + hendelse. Ingen løpende progressmeldinger, identiske retries, rutinekvitteringer eller meldingspingpong. Når rapporten markerer reviewstopp, venter AGENT på neste arbeidsordre. Callback leverer status/evidens; den gir ikke ny eierfullmakt, godkjenner ikke eget arbeid og starter ikke en ny produktoppgave.

SUPERVISOR verifiserer rapporten mot kompakt worker-/chatstatus, lokal Git-status, faktisk diff og testevidens. Dedupliser rapport og eventuell timeroppfølging før en ordre sendes. Eksisterende valg CONTINUE, REQUEST_FIX, STOP_FOR_RESET og ESCALATE_TO_OWNER, faktisk kvotegating og reelle approvalstopp gjelder fortsatt.

Midlertidige workers rapporterer til nærmeste overordnede med eksisterende subagentverktøy. Overordnet AGENT/SUPERVISOR samler delrapportene og kontrollerer samlet evidens, slik at parallelle resultater ikke utløser dupliserte eller uforenlige arbeidsordre.

Dersom callbackverktøyet er utilgjengelig eller avvist, rapporterer AGENT i egen chat/repo og bruker eksisterende tidsreserve. Ingen alternativ kanal eller annen omgåelse av et faktisk avslag.

### Historisk lokalt callback-policycheckpoint

Denne endringen er avgrenset til SUPERVISOR_POLICY.md og én operativ henvisning i AGENTS.md, basert på decodercheckpoint 2c8498f. Dokumentlenker, offentlig inventar/personvern, minimal diff og whitespace kontrolleres; alle andre eksisterende filer skal være byteidentiske med dette grunnlaget. Ingen ny build/testkjøring kreves for bare dokumentasjon. Tidligere 822/822-resultat gjelder implementasjon ce5d84f og er ikke ny testevidens for policyen.

Etter lokal commit utføres én POLICY_CALLBACK_TEST / CHECKPOINT som ber SUPERVISOR reviewe dette policycheckpointet. Leveringsstatus rapporteres i AGENT-chatten; denne teksten påstår ikke vellykket levering eller endring/deaktivering av timeren. Ingen push, PR eller main-endring inngår.

Lokal callback-policy5dfda9d og callbackleveringen er senere godkjent av SUPERVISOR. Timerreserve er fortsatt ikke endret gjennom AGENTs dokumentarbeid.

## GitHub-publisering – stående eiertillegg 2026-10-14

Eieren har etter de tidligere publiseringsavslagene gitt en ny uttrykkelig godkjenning: «Jeg gir uttrykkelig godkjenning for å sende neste push til github. Fremtidige push'es er også godkjent av med for super bruker».

Godkjenningen gjelder det offentlige prosjekt-repositoryet https://github.com/MasterCorruptor/ultrastar-song-creator, med eksakt origin https://github.com/MasterCorruptor/ultrastar-song-creator.git. Den omfatter den lokalt reviewede LRCLIB-decoderleveransen ce5d84f, callback-policy5dfda9d og senere SUPERVISOR-godkjente prosjektendringer på ordinære work-brancher, PR-er og integrasjon i main innen tidligere master-, scope-, review-, kvote- og lisensrammer. Rutinemessig ny eierbekreftelse for slike pushes er ikke nødvendig.

Det tidligere decoderstoppets manglende publiseringsfullmakt er løst av denne nye eiergodkjenningen. Historiske faktiske avslag beholdes som historikk; de blir ikke omtolket til vellykket publisering. Ingen push/PR/merge er utført i dette dokumentcheckpointet. SUPERVISOR reviewer det aktuelle dokumentcheckpointet før konkret publiserings-/integrasjonsordre.

«Super bruker» endrer ikke sandbox eller automatiske godkjenningskontroller og tillater ikke force-push, destruktiv synkronisering, publisering til andre destinasjoner eller omgåelse av et nytt avslag. Nye faktiske plattform-/approvalavslag stopper den berørte handlingen og rapporteres etter eksisterende policy.

## Avklart kort rapportering og konkret A-publisering — 2026-10-14

Et faktisk avslag på en omfattende kontrollpunktrapport ble fulgt av ny direkte eiergodkjenning for korte rapporter til prosjektets navngitte SUPERVISOR-chat, også ved senere checkpoints: «Ja, også senere korte rapporter». Send høyst én kort melding (maks120ord) per unik task/SHA/hendelse, med status/testevidens/rapportsti og uten råkode/sangtekst eller omfattende intern historikk. Første korte leveranse er verifisert levert. Tidligere avslag er historiske; et nytt faktisk avslag stopper fortsatt berørt melding uten alternativ kanal.

Eieren godkjente separat eksakt A099884b på work/phase3-lyrics-acquisition-session i det offentlige prosjekt-repositoryet samt PR mot main. Dette er gjennomført som PR21; lokal senere A440ea86 og B1/B2 er ikke publisert gjennom denne handlingen. PR21-merge og nye konkrete publiseringshandlinger vurderes etter gjeldende separate godkjenninger/ordre. Kort rapporteringsfullmakt gir ingen ny publiserings-/mergefullmakt.

## Ny direkte mergefullmakt — 2026-10-14

På konkret spørsmål om merge av PR21 (godkjent A099884b,939tester) til main i MasterCorruptor/ultrastar-song-creator og oppdatering av lokal main, samt senere SUPERVISOR-reviewede PR-merger innen avtalte rammer, svarte eieren «Ja, også senere reviewede merges». Dette autoriserer den konkrete A-integrasjonen og senere faktisk reviewede prosjekt-PR-er innen master/scope/lisens/kvote/fullmakter. Ingen selvreview, force/reset, destruktiv sync, andre destinasjoner eller omgåelse av nye plattformavslag. Kontrollér eksakt reviewet head, base/main, diff/checks/konflikter før hver konkret merge; ny materiell endring krever nytt review. B2 REQUEST_FIX er ennå ikke godkjent/integrert. Kort checkpoint-rapportering er prosjektstatus uten kontokvote-/bruksdata.
