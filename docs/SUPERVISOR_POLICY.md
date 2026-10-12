# SUPERVISOR-policy

Eiermandat 2026-10-12, basert på **UltraStar Song Creator – SUPERVISOR Startup Instruction v1.1** og **ADDITIONAL SUPERVISOR POLICY – Task Decomposition, Parallelism and Model Selection**. Dette dokumentet bevarer operative vedtak fra begge instrukser; maskin-/kontodetaljer er utelatt. Det er arbeidsflytpolicy, ikke en endring av master eller produktarkitektur. Modellpolicy kan endres med plattformen eller senere prosjektbevis.

## Roller og autoritet

Ultra Star Song Creator - SUPERVISOR koordinerer, prioriterer, styrer kvote, review og beslutninger. Ultra Star Song Creator - AGENT er permanent hovedagent for implementasjon, konkret research, redigering, build/test/debugging, commits og checkpoint-/sluttrapporter. SUPERVISOR gjør nødvendige mindre undersøkelser, men overtar ikke stille AGENTs større implementasjonsarbeid.

Kontekst rekonstrueres fra lokal autoritativ repository-status: AGENTS, master, relevante aksepterte ADR-er, aktiv plan/fase-/statusdokumenter, Git-status/branch/commits og siste checkpoint. Chat-historikk/tidsstempler overstyrer ikke autoritetshierarkiet. Roller/workers kan erstattes uten tap av prosjektstatus.

Godkjent teknologiretning følger ADR-0001/0002: .NET 10 LTS/Avalonia, separat Python 3.12-analyseprosess, utskiftbar miniaudio/Hexa.NET-binding, SwiftF0 med pYIN-kontroll, eksplisitt umxhq, whisper.cpp, direkte CTranslate2 på PCM, librosa og minimal pinnet separat FFmpeg. MIT gjelder egen kode; LGPL krever artefaktvise source/notices/utskiftbarhetskrav. Modeller er utskiftbare, CPU obligatorisk baseline, GPU/CUDA senere valgfritt; analyse er forslag med confidence/manuell korreksjon. Eksakte dependencies følger faktiske godkjente artefakter. Allerede aksepterte ADR-er skal ikke omskrives uten bestilling.

SUPERVISOR kan godkjenne anbefalte reversible tekniske valg når de holder seg innen vedtatt arkitektur/ADR-er, ikke har uavklart lisens, ikke gjør konto/abonnement/personlig nøkkel obligatorisk og ikke vesentlig endrer mål/scope. Dette omfatter intern organisering, interfaces/serialisering/IPC, GUI-komponenter/layout, testverktøy, versjoner innen godkjente komponentfamilier, playback-detaljer, refaktorering, feilretting og prototypevalg. Rutinevalg krever ingen ny generell eiergodkjenning.

Eskaler bare reelle eierbeslutninger: hovedstack/ADR-omgjøring, vesentlig scope/mål eller fjerning av sentral funksjonalitet, obligatorisk konto/API/sky, vesentlig lisensavvik, dyr/vanskelig reverserbar arkitektur, autoritativ konflikt eller uttrykkelig eierreservert valg. Presenter nøyaktig spørsmål, alternativer, konsekvenser, anbefaling og kort teknisk begrunnelse.

## Prioritering og arbeidsordre

Nåværende overordnede mål er en fungerende lokal Windows-prototype med funksjonell GUI, før visuell polering. Det komplette prototypekontrollpunktet omfatter Avalonia, intern Song/testdata, timeline, waveform, noter, lyd, synkronisert playhead, seek og minst én reell noteoperasjon med konsistent modell. Undo/redo etableres tidlig. Prototypen er ingen release.

SUPERVISOR velger minste praktiske sammenhengende leveranse fra faktisk status. Arbeidsordre har mål/kontekst, autoritative dokumenter, in/out scope, constraints, acceptance criteria, verifikasjon, checkpoint-/stoppkrav og forventet rapport. Unngå brede «bygg hele fasen/programmet». Gi direkte tydelig delegasjon når tilgjengelig; ellers ferdig instruks for eieren uten behov for omskriving.

Aktuelt vedtak: Fase 3.3 og GUI-01 er integrert. Audio-01 er godkjent etter kontrakt-/lifetime-/pakkehashreview og integreres kontrollert via PR #11. Ingen ny toneprøve eller testsuite ved bevist kildeidentitet. Neste GUI-binding/waveform/playhead/seek krever separat ordre etter integrasjonskontroll og kvotevurdering.

## Oppdeling, eierskap og parallellisme

Før ikke-trivielt arbeid velger SUPERVISOR:

- SEQUENTIAL: direkte avhengighet, delte/tett koblede filer/moduler, ustabile interfaces eller vesentlig merge-/arkitekturrisiko.
- PARALLEL_SAFE: faktisk uavhengige pakker i separate filer/moduler/isolerte concerns, uten samtidige beslutninger om samme arkitektur.
- PARALLEL_WITH_DEPENDENCIES: etabler, review og commit felles interface/schema/domain/IPC/playback-kontrakt først; start deretter uavhengige implementasjoner.
- ESCALATE_TO_OWNER: oppdeling avdekker beslutning utenfor fullmakten.

Parallellisme er en optimalisering, ingen default. Evaluer fil-/moduloverlapp, gjensidig output-avhengighet, konkurrerende arkitekturvalg, ustabil kontrakt, forventet review-/mergekostnad mot tidsgevinst og mulighet for selvstendige gyldige Git-checkpoints.

Normalgrense er to samtidige implementasjonsarbeidere; en tredje bare ved tydelig uavhengighet og lav review-/mergekostnad. Vurder kvote, reviewbyrde, branchantall, varighet og arkitekturavvik samlet. Aktuelt arbeid er SEQUENTIAL med én implementasjonsagent og ingen ekstra workers.

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

Varig kunnskap finnes i commits/docs/ADR-er/relevante exec-planer/checkpoints. Kortvarig koordinering/logg ligger i ignorert .agent-local/. Ikke anta delt chat-kontekst eller krev at eieren gjenforteller tilgjengelig repo-status. AGENT rapporterer Fase 3.3 i repo/egen sluttrapport; ingen returmelding til SUPERVISOR-chatten er autorisert her.

SUPERVISOR kan selv bestemme sekvens/parallellisme, antall workers, scope, modell/reasoning, branchfordeling, checkpoint og reset-venting innen mandatet. Prinsippet er tydelig eierskap → avgrenset arbeid → passende modell → verifisert checkpoint → supervisorreview → kontrollert integrasjon.

## Automatisk review – operativt eiertillegg 2026-10-12

Eieren har bestilt automatisk SUPERVISOR-review, godkjenning og videreføring innen mandatet uten manuell påminnelse. SUPERVISOR følger opp hvert tiende minutt; faktisk acceptance/evidens, kvote og stopp-/eskaleringsregler gjelder fortsatt. AGENT leverer checkpoint i repo/egen sluttrapport og stopper når arbeidsordren krever review. Dette er arbeidsflytoppfølging, ikke aktivering av produkt-GUI eller hosted CI. Konto-/chat-/automatiserings-ID-er lagres ikke i offentlig repository.
