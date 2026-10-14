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

## Lyrics acquisition session A — lokalt checkpoint

Headless session A er implementert lokalt, med immutable/revisjonert tilstand, ett aktivt søk og én nyeste ventende intensjon, eksplisitt variant/manuell tekst, typed feil og sikker cancellation/disposal-drain. Den låner provider og endrer ikke Song, undo/redo eller prosjektfiler. [Arbeidsordre/API](PHASE3_LYRICS_SESSION_WORK_ORDER.md), [checkpoint](exec-plans/active/phase3-lyrics-acquisition-session.md).

Ved testet e4b3923 består 47 nye session-cases og 939/939 Windows-tester (Core 871, Audio 17, Desktop 51), zero skipped; locked restore, Release uten warnings/errors og full formatkontroll består. Kontrollerte providers/TCS brukes, ingen live API eller real-time sleeps. Ingen nye Linux/native/GUI/alpha/beta- eller fullworkflow-resultater. Sessionkode avventer SUPERVISOR-review; B-panelet og samme-history apply er fortsatt neste separate steg.
