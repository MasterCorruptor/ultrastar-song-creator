# Hva kan testes nå?

Historisk GUI-status 2026-10-13: GUI-02 er integrert via PR13 og den avgrensede lokale prototypen er SUPERVISOR-sluttgodkjent. Seks manuelle Windows-scenarioer er eierbekreftet bestått;490/490 Windows/Linux-tester. [Startguide og grenser](GUI_02_EDITOR_BINDING.md). Ingen alpha/beta/installer/release.

## Dagens funksjoner

- Sangmodell, validering og MoveNote med undo/redo.
- Prosjektlagring/gjenåpning, UltraStar-import og eksport med komplett sangmappe.
- Fase 3.1: lokal filinspeksjon, metadata/manuelle overrides, stream-valg og lagret proveniens.
- Fase 3.3 integrert: YouTube-søk, eksplisitt formatvalg og lokal originalnedlasting etter [kontrakten](ONLINE_MEDIA.md).
- Fase 3.2: valgfritt MusicBrainz-søk og eksplisitt innspillingsvalg etter [kontrakten](RECORDING_SEARCH.md), med offline lagring.

GUI-02 har waveform/playback/seek og visuell noteflytting med undo/redo av egen testsang/demotone. Filvelger, menyer, acquisition og prosjektfil-I/O er fortsatt ikke koblet til GUI.

## Teknisk kontroll

Fra prosjektroten med .NET 10 SDK, se [README](../README.md) og [utviklingsguiden](DEVELOPMENT.md):

```text
dotnet restore UltraStar.SongCreator.slnx --locked-mode
dotnet build UltraStar.SongCreator.slnx -c Release --no-restore
dotnet test UltraStar.SongCreator.slnx -c Release --no-build --no-restore
```

Testene trenger ikke FFprobe, Python, modeller eller sangfiler. tests/MediaProbe.TestHost er en liten egen child-prosess-fixture og bare testutstyr. Faktisk medieinspeksjon via [API-en](LOCAL_MEDIA.md) krever eksplisitt konfigurert egnet FFprobe.

## Tidligere visuelle prøver

Fase 1 laget tekniske tidslinje-/playback-prober. TimelineProbe rendrer syntetiske noteblokker til bilder uten et interaktivt produktvindu. NativeProbe har et avgrenset testvindu, kjører automatisk playback/seek/loop og avsluttes. Lokale exe-filer under ignorerte evalueringsmapper er prøver/verktøy/runtimes, ikke en alpha-editor.

Den lokale illustrasjonen .agent-local/phase1/timeline-results/timeline-0-8.png viser bare et tidligere renderingsforsøk, ikke gjeldende produktdesign, komplette menyer eller arbeidsflyt. [Fase 1-guiden](../tools/phase1/README.md) beskriver tekniske reproduksjonsprøver.

## Første praktiske brukertest

En meningsfull alpha trenger en sammenhengende brukerflate: åpne/opprette prosjekt, vise sangdata, gjøre en avgrenset redigering, lagre og eksportere med forståelige feil. Playback/timeline kommer som avgrenset editorarbeid. Masterplanen legger Acquisition i Fase 3, Generation i Fase 4 og editor i Fase 5. En tidligere testflate er et eget prioriteringsvalg for prosjekteieren.

Fase 3.1–3.3 har ingen GUI; separat GUI-01 er nå integrert. Ingen alpha-distribusjon.

Prosjekteieren har prioritert en tidlig fungerende Windows GUI-prototype. GUI-01 er bestilt separat etter det godkjente/integrerte [Fase 3.3-checkpointet](exec-plans/completed/phase3-media-download.md) etter [SUPERVISOR-policyen](SUPERVISOR_POLICY.md); se GUI-01-startguiden for faktisk checkpointstatus.

## Backend før neste GUI-binding

Audio-01 er godkjent og integrert etter [PCM-kontrakten](AUDIO_PLAYBACK.md): 449/449 Windows/Linux, ekte Windows Wasapi/miniaudio-smoke og Linux native loading uten fysisk device. På reviewbranchen finnes nå [GUI-02 editor-binding](GUI_02_EDITOR_BINDING.md) med waveform/transport/synkronisert playhead og fortsatt noteoperasjon.490/490 Windows-tester består; native kombinert GUI/playback er ikke testet. Historisk reviewstatus; kandidaten er senere integrert. Fullprodukt/alpha/release er ikke ferdig.


## GUI-02 verifikasjon 2026-10-13

[Native rapport/manuelle steg](GUI_02_NATIVE_VERIFICATION.md): Windows-kandidatvinduets faktiske lydinit/initialstatus og normal Alt+F4-prosessavslutning er bekreftet; screenshotfangst feilet. Kombinert visuell/hørbar acceptance gjenstår. Samlet kandidat har nå490/490 Windows og nettisolerte Linux-tester med locked restore/Release-build; ingen fysisk Linux-device. Dette avsnittet er historisk; senere eierakseptanse og sluttgodkjent integrasjon står nedenfor.


## Eierakseptanse 2026-10-13

Prosjekteieren bekreftet direkte i AGENT-chat: «Alle tester bestått med forventet resultat» etter startinstruks og seks manuelle GUI-02-punkter. Hørbarhet, play/pause/seek/stop/EOS, synlige synkroniserte playheads, noteflytting/undo/redo, delt scroll og X-lukking er dermed eierbekreftet bestått. [Proveniens og grenser](GUI_02_NATIVE_VERIFICATION.md). Tidligere utsagn om gjenstående manuell Windows-akseptanse er historiske. SUPERVISOR-sluttreview er gjennomført, PR13 er merget som f5132145c6466d6815fb6b4ee857c4e7c15702b3. Ingen alpha/release.

## Lyrics-fundament – kodegodkjent

Provider-uavhengige in-memory lyricsobjekter, validering, manuell tekst og eksplisitt kandidatvalg er kodegodkjent ved5492b44;549/549 Windows-tester består. [Kontrakt og grenser](PHASE3_LYRICS_CONTRACT.md). Ved dette historiske kontrollpunktet var HTTP-innhenting, LRC-parser, persistens og lyrics-GUI ikke implementert; end-to-end Lyrics Acquisition er ikke ferdig. Integrert via [PR15](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/15) som458176f13ed7de50cd3b503a7a7b194139444784; fundamentplanen er avsluttet.

## Simple line-LRC – godkjent pakke

[Avgrenset profil](LRC_PROFILE.md) ved6c8a563: pure bounded parser med bevart tekst/metadata og typed formatfeil,73 parser-tests og622/622 Windows-regresjoner bestått. Bare komplette angle-timestampformer avvises; nonzero offset/andre formater er utenfor profilen. Ingen HTTP, lyrics-GUI eller persistens i pakken; faktisk integrasjon fremgår av GitHub-historikken.

## Valgt lyrics og prosjektv3 – godkjent samlet pakke

Immutable valgt tekst/proveniens kan endres med undo/redo og lagres offline i strictv3; v1/v2-lesing og originalfiler bevares. Kandidatdiagnostikk er transient og persisteres ikke.773/773 Windows-tester består. [Gjennomgått evidens](LYRICS_COMBINED_LOCAL_REVIEW.md). Publisering/main-integrasjon er eierautorisert; aktuell PR/Git-historikk angir status. Ingen HTTP-lyricsinnhenting/ny GUI/native/Linux/release er verifisert i pakken.


## LRCLIB HTTP transport — integrert 2026-10-14

Decoder, lyricsdiagnostikk og valgt tekst i prosjekt v3 er integrert i main gjennom PR18/PR19. Integrert via PR20 som main6ddb743 finnes nå LrclibProvider med anonymt søk, separat serial gate, completion-spacing, overall deadline, Retry-After og begrenset responstolkning. Den er ikke koblet til GUI; SUPERVISOR og uavhengig reviewer har godkjent leveransen uten actionable findings ved da3c163; [kontrakt](LRCLIB_TRANSPORT_PROFILE.md) og [checkpoint](exec-plans/completed/phase3-lrclib-http-transport.md).

70 nye offline transport-/gate-tester bruker egne syntetiske svar og lokal klokke. Ved testet commit db51699 består 892/892 Windows-tester (Core 824, Audio 17, Desktop 51), locked restore, Release build uten advarsler og full formatkontroll. Ingen nytt live API, ekte sangtekst, Linux/native eller alpha/beta-resultat er påstått. Hele sangtekstflyten og brukergrensesnittet er fortsatt ikke ferdige.

Neste foreslåtte avgrensning er [sangtekstflyt](PHASE3_LYRICS_FLOW_PROPOSAL.md): først headless acquisition-session, deretter minimalt tekstpanel og eksplisitt apply/undo. Dette er kun et forslag, ingen ny implementasjon eller GUI-aksept.

## Lyrics acquisition session A — kodegodkjent checkpoint

Headless session A er implementert lokalt, med immutable/revisjonert tilstand, ett aktivt søk og én nyeste ventende intensjon, eksplisitt variant/manuell tekst, typed feil og sikker cancellation/disposal-drain. Den låner provider og endrer ikke Song, undo/redo eller prosjektfiler. [Arbeidsordre/API](PHASE3_LYRICS_SESSION_WORK_ORDER.md), [checkpoint](exec-plans/completed/phase3-lyrics-acquisition-session.md).

Ved testet e4b3923 består 47 nye session-cases og 939/939 Windows-tester (Core 871, Audio 17, Desktop 51), zero skipped; locked restore, Release uten warnings/errors og full formatkontroll består. Kontrollerte providers/TCS brukes, ingen live API eller real-time sleeps. Ingen nye Linux/native/GUI/alpha/beta- eller fullworkflow-resultater. Sessionkode er godkjent ved de2b09f av SUPERVISOR og uavhengig reviewer uten actionable findings; B-panelet og samme-history apply er fortsatt neste separate steg.

## Lyrics editor apply B1 — lokalt checkpoint

ApplySelectedLyrics bruker eksisterende EditingSession-historikk mot gjeldende Song. Valgt tekst kan anvendes programmatisk med undo/redo; no-op/ugyldig/null endrer ikke sang eller redo. Null er en eksplisitt avvisning, ikke clear. Ingen tekstpanel/fil- eller nettverksflyt er lagt til. [Kontrakt/report](exec-plans/completed/phase3-lyrics-editor-apply.md).

Testet aaedb1d:14nye/953totale Windows-tester(Core871,Audio17,Desktop65), zero skipped; locked restore,Release0warnings/errors og full formatkontroll består. Faktiske source-time/proveniens og øvrige sangfelter bevares. Ingen nytt native/Linux/live/GUI/fullworkflow-resultat. B1 er akseptert av SUPERVISOR ved fb6fcb6. B2 og ekstern levering behandles separat.

## Lyrics panel component B2 — lokalt reviewcheckpoint

Ved testet2d137f4 består24nye/977totale Windows-tester (Core871,Audio17,Desktop89), zero skipped; locked restore,Release0warnings/errors og full formatkontroll består. [Komponentrapport](exec-plans/completed/phase3-lyrics-panel-component.md) beskriver syntetiske headless-prøver for eksplisitt valg/manuell tekst/frozen apply, immediate/stale eligibility, bounded admission og trygg asynkron close med held cancellation/ignored provider og lånt/eid lifetime.

Dette er en selvstendig komponent, ennå ikke koblet til EditorWindow/EditorApplication; B3 wiring/normal-window-close/startup/audio cleanup er neste separate arbeid etter review. Ingen nytt native/manuelt/live/Linux/alpha-beta/fullworkflow-resultat. Bare eksisterende lokal Acquisition-prosjektreferanse er lagt til; ingen nye eksterne pakker. B2 er ikke publisert. Eksakt A099884b er publisert separat i PR21; main er ikke endret av denne leveransen.

B2 REQUEST_FIX ved c83152b: begge P2-funn rettet i eide komponent/testfiler. Eksplisitt forberedelse binder én revisjon og immutable payload; ekstern/reentrant erstatning kan ikke anvendes. Innspillingsvalg venter på eksplisitt variant uten falsk valideringsfeil.31komponent/984totale Windows-tester (Core871,Audio17,Desktop96), zero skipped; locked restore/Release0warnings/format bestått. [Oppdatert rapport/evidens](exec-plans/completed/phase3-lyrics-panel-component.md). Ingen nytt native/live/Linux/B3-resultat.

A er nå integrert via PR21 som main d5a6231 etter ny direkte eiergodkjenning. Hele merge-treet er identisk med godkjent A099884b, testet kode e4b3923 med939cases; dette er ingen ny testkjøring. B2 fixed984cases gjelder separat lokal c83152b og er nå godkjent av SUPERVISOR ved 5d10cfb. Native/live/Linux/B3 er ikke verifisert her.

## B2-komponent sluttgodkjent

SUPERVISOR har godkjent begge reviewrettingene ved5d10cfb/testet c83152b:31komponent/984Windows-cases (Core871,Audio17,Desktop96), zero skipped. Dette er automatisk/offline komponentakseptanse. [Avsluttet plan](exec-plans/completed/phase3-lyrics-panel-component.md). Ingen ny suite for docs-only avslutning; kode/prosjekter/locks/tester bevares. STOP_FOR_RESET før B3 og ny kapasitets-/statuskontroll; native/live/Linux/B3/fullworkflow er fortsatt uverifisert.

## B3 appwiring — lokalt reviewcheckpoint

Testet38e1b5a:16nye/1000totale Windows-tester (Core871,Audio17,Desktop112), zero skipped; locked restore/Release0warnings/full format består. [Faktisk rapport/evidens](exec-plans/completed/phase3-lyrics-app-wiring.md). Produksjon bruker ekte LRCLIB uten startuprequest; samme editorhistorikk/playback er beholdt. Kontrollerte headless-prøver dekker normalclose, UIaudio før drain, held callback/ignored cancellation, delvis startup-eierskap og statisk failureguard. Ingen ny dependency/project/lockfilendring eller faktisk nett/device i testene.

Dette er appkomposisjon via injiserbare interne host-callbacks; faktisk native classic-desktop-loop/manuell Windows, live provider, Linux og fullworkflow er ikke akseptert her. Ingen ny publisering/main-endring. Avventer SUPERVISOR-review; videre fase starter ikke automatisk.

## B3 sluttgodkjent lokalt

SUPERVISOR og uavhengig review har godkjent bc7d17b/testet38e1b5a som automatisk/offline-komposisjon:16nye/1000Windows, zero skipped, ingen actionable funn. [Avsluttet rapport](exec-plans/completed/phase3-lyrics-app-wiring.md). Kode/prosjekt/lock/testbytes bevares ved docsclosure, ingen ny suite. Faktisk native classic-desktop-loop/Show/platforminitialisering/manuell/live/Linux/fullworkflow er fortsatt ikke verifisert. Neste separate ordre klargjør samlet lokal integrasjon for review, uten publisering/mainendring.

## Samlet lokal B1–B3-integrasjon til review

[Integrasjonsrapport](PHASE3_LYRICS_EDITOR_INTEGRATION.md) samler aksepterte kode-/testrefs og presise native/manual/live/Linux-gap. Lokal main-merge9bafcf0 har identisk filinnhold med B3-closure bb0c5d6; source/test/project/lockbytes matcher testet38e1b5a/1000Windows. Ingen ny suite for docs-only/identisk-tre-klargjøring. Main d5a6231 er bevart; ingen ny remote/publisering/nativeUI eller feature startet.

## PR22 — samlet sangtekstintegrasjon

Samlet B1–B3 er integrert som main16b9c1891c91c10bd99f377b5fa6e38e1b440f92 ved eksakt reviewet/publisert941e765. GitHub/main, origin/main og lokal main er like; hele merge-treet matcher godkjent941e765, kode/test/prosjekt/lockbytes testet38e1b5a med1000Windows (Core871/Audio17/Desktop112), zero skipped. Ingen ny suite for identisk merge-tre; hosted checks var ikke konfigurert i preflight. [Rapport](PHASE3_LYRICS_EDITOR_INTEGRATION.md). Native/live/Linux/fullworkflow forblir gap; lokal docsclosure er ikke automatisk publisert. Ingen videre fase startet.

## Generation-forberedelse — docs only

[Repo-/gapanalyse og foreslått G1-kontrakt](PHASE4_GENERATION_PREPARATION.md) avgrenser lokal PCM-preprocessing og separat modellfri worker før senere analysemodeller. Fase3 har fortsatt flerkilde/TTML/confidence/ASR-/realmediaGUI/native/live/Linux-gap; ingen faseavslutning eller ny backend-/kvalitetsakseptanse påstås. Kun dokumentasjon, ingen ny suite; eksisterende1000Windows ved38e1b5a er ikke ny Generation-evidens. Avventer review før implementasjon eller delte schema-/APIendringer.

## G1A — ren preprocessing-kontrakt til review

Ved testet6e0ef34 består69nye/1069totale Windows-tester (Core871,Audio17,Desktop112,Generation69), zero skipped; locked restore/Release0warnings/full format består. [API/rapport](exec-plans/completed/phase4-preprocessing-contract.md). Nye tester har ingen fysiskfil/prosess/nett/device; de kontrollerer normaliserte input, uendret immutable snapshot/tekst, eksplisitt stream/path og utdataenes tids-/frame-/proveniens-/hashformatgrenser. Ingen faktisk dekoding, hashverifikasjon, worker, analysemodell, schema/GUI eller native/live/Linux/fullworkflow-akseptanse. Gamle prosjekt-/lock-/kodebytes er bevart. Avventer SUPERVISOR-review før G1B; ingen publisering/mainendring.

G1A er sluttgodkjent av SUPERVISOR ved23b8591/testet6e0ef34,69nye/1069Windows, uten actionable funn. [Avsluttet kontraktrapport](exec-plans/completed/phase4-preprocessing-contract.md). Dette er bare ren in-memory/lexical kontraktakseptanse; ingen ny suite ved docsclosure, ingen fil/hash/prosess/backendverifikasjon. G1B forberedes separat uten implementasjon.

Historisk forberedelsescheckpoint: G1B var kun [prosessforberedelse til review](PHASE4_GENERATION_PROCESS_PREPARATION.md), ingen runnerkode eller ny suite. Forslaget begrenser til betrodde leaf-prosesser og skiller root-exit/EOF/lokal ressursfrigjøring fra etterkommergaranti, med bounded cleanup-forsøk og holdt admission/arbeidsmappe ved pending/quarantine. Offisiell .NET10-dokumentasjon er kontrollert; ingen ny terminal/runtime/native/Linux-akseptanse påstås.

## G1B accepted local process foundation
[Generation process report](PHASE4_GENERATION_PROCESS_CHECKPOINT.md): testedf6524d9c4134ea203d50fc3844e3f089c646e9b7,1118/1118 Windows (49 process cases), zero skipped; locked restore/Release0warnings/format passed. Pending/Quarantined ownership and terminal fixtures only. SUPERVISOR accepted reviewed7da57a6/testedf6524d9 including P2. Linux terminal smoke and product workflow remain outstanding. main16b9c18 unchanged; accepted local checkpoint, no publication. [Completed plan](exec-plans/completed/phase4-generation-process.md). No new suite for documentation closure.
