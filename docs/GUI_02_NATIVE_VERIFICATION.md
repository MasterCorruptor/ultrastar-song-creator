# GUI-02 native Windows verification

2026-10-13 (Europe/Oslo). STATUS: COMPLETE; manuell native Windows-akseptanse bekreftet av prosjekteieren og SUPERVISOR-sluttgodkjent. PR13 er integrert som f5132145c6466d6815fb6b4ee857c4e7c15702b3. SMALL/SEQUENTIAL/en worker; bestilt Sol Medium, faktisk modell/reasoning ukjent. SUPERVISOR-ordre etter godkjent kode/headless-checkpoint; ingen mergeordre.

Kandidatkode: 3619d1f18f7a27c9237f30c7e2d96caf46d61ee1, work/gui-02-editor-binding, [merget PR13](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/13). Ingen kilde-, test-, pakke- eller lockendringer i denne verifikasjonen. [Bindingkontrakt](GUI_02_EDITOR_BINDING.md), [Audio-kontrakt](AUDIO_PLAYBACK.md), [avsluttet rapport](exec-plans/completed/gui-02-editor-binding.md).

## Faktisk Windows-prøve

Ingen tidligere Desktop-prosess var aktiv. Eksisterende verifiserte Release-output ble startet med kjent lokal .NET10-runtime og repo som cwd, uten ny build/490-suite. Output er fra siste samlede Windows-build; etter den kom bare den dokumenterte whitespace-endringen i B-panelets switch-armer og dokumentasjon. Det hevdes derfor semantisk samme kandidat, ikke en ny eksakt bygging med3619d1f innebygd.

| Observert steg | Resultat |
| --- | --- |
| Faktisk kandidatprosess/vindu | «UltraStar Song Creator — GUI-02 testflate» returnert av sky.list_windows; prosessen responderte. |
| Native init | Accessibility viser aktive Spill/Søk-kontroller, tom PlaybackErrorLabel, normal demo-melding. EditorApplication sin faktiske MiniAudioPlayback-factory har returnert uten init-feil; ingen fallback finnes. Konkret device/backendnavn er ikke instrumentert i denne prøven. |
| Initial source/render-position | PlaybackPositionLabel «0 s», PlaybackStateLabel «Stoppet», Pause/Stopp disabled. Offset0. Ingen videre cursor-fremgang er målt i dette vinduet. |
| Redigeringskontroller | Hei/på/deg, StartInput og disabled Move/Undo/Redo ved intet valg er returnert i accessibility-treet. Ingen noteklikk er utført. |
| Skjermfangst | Ett forsøk med include_screenshot=true feilet: FrameArrived timed out: timed out waiting on channel. Samme fangstbegrensning som tidligere; ingen klikkgeometri/visuell evidens. Ingen capture-retry, alternative UI-API-er eller bypass. |
| Normal avslutning | Oppdatert accessibility-target, støttet sky.press_key Alt_L+F4. Påfølgende prosesskontroll bekreftet at testprosessen var avsluttet. Ingen Desktop-prosess var igjen; stderr var tom. Dette er normal tastaturlukking, ikke X-knappetest eller instrumentert backend-dispose/pollingmåling. |

Eksakte private build-output SHA256 ved oppstart:

| Fil | SHA256 |
| --- | --- |
| Desktop.exe | a7eb8ec0da88ddc41b9cbdcdd76ba0e7075b842f9c1937f699efc562ab73d6a5 |
| Desktop.dll | 6172a0a51331ad2c5e3f8a96f3cedee445d5f470ab6647163674a712cbf9e87e |
| Audio.dll | bd86be0acf923bbf50859e47da184733a3da90cea4ee29e696d62692310a3c4b |

Private stdout/stderr/Linux-output/script ligger under ignorert .agent-local/gui-02-native. Ingen process-ID, hardware/device-identifikatorer, medier eller binærer publiseres.

## Linux gjenbruk

Eksisterende ultrastar-phase1-linux-image/SDK10.0.401 og NuGet-cache ble gjenbrukt med network none, samme kilde og egne artifacts under .agent-local/gui-02-native/linux-artifacts. Locked restore bruker NuGetAudit=false kun fordi prøven er offline; ordinær restore beholder audit. En samlet Release-build:0 warnings/errors. En samlet testsuite:490/490 (Core422,Audio17,Desktop51). Ingen ny fysisk Linux-device/native GUI/probe, installer eller hørbarhetstest. Ingen nytt oppsett/image/download.

## Manuelt Windows-scenario – eierbekreftet bestått

Start fra repo-roten på work/gui-02-editor-binding:
~~~powershell
./tools/desktop/Start-Desktop.ps1
~~~

1. Trykk Spill. Bekreft at den svake demotonen høres og at posisjon/begge playheads beveger seg sammen. Trykk Pause: posisjonen skal stå stille.
2. Skriv2,5 i sangsekundfeltet og trykk Søk til tid. Begge playheads skal vise samme sangtid. Spill, deretter Stopp: posisjon0.
3. Søk7,5, trykk Spill: avspillingen skal ende ved8s med «Slutt på klippet» og disabled Spill.
4. Søk0 og Spill. Velg Hei, flytt til2,5, Angre og Gjør om. Noteblokken skal flytte1→2,5→1→2,5 mens waveform og lyd/playhead fortsetter konsistent.
5. Gjør vinduet smalt og scroll horisontalt: waveform/noter/playheads skal beholde samme tidsjustering.
6. Lukk med X. Bekreft at vinduet/prosessen avsluttes og ingen lyd fortsetter.

Rapporter bestått/feil per steg, spesielt hørbarhet og visuell synkronisering. Ingen systemvolum endres av testen. Render-cursor er source-position, ikke målt høyttalertid; OS/device kan ha bufret lyd som kort fortsetter etter pause/seek. Ingen ny queue-latency ble observert/målt og ingen latencygaranti gis.

Headless51 og tidligere Audio-01/device-evidens er separate bevis; de erstatter ikke denne kombinerte native/manuelle sluttesten. Ingen alpha-/releaseakseptanse påstås. Samlet GUI-02-scenario er nå eierbekreftet bestått; SUPERVISOR-sluttreview og brukerautorisert PR13-integrasjon er gjennomført. OWNER DECISION REQUIRED: NO.


## Eierbekreftet resultat 2026-10-13

Etter at AGENT ga startinstruks og de seks manuelle punktene direkte i chat, svarte prosjekteieren: «Alle tester bestått med forventet resultat». Dette bekrefter samtlige seks scenarioer: hørbar svak demotone/fremgang/pause, søk2,5 og like playheads/stop0, EOS8s, Hei-flytting/Angre/Gjør om under avspilling, felles horisontal scrolling og normal X-lukking uten videre lyd.

Evidensen er direkte eierobservert manuell Windows-akseptanse. Det er ingen ny automatisert native-klikk-/skjermfangstprøve, målt latency eller ny490-suite. Koden er uendret siden3619d1f; dokumentcheckpoint9b3e7b0 var gjeldende før bekreftelsen. Eksakt SHA/bygningshash for eierens startscript-run er ikke separat innhentet; ikke likestill det med AGENTs tidligere outputhashes.

Det tidligere manuelle scenario-gapet er lukket av denne bekreftelsen. CU-skjermfangstproblemet består som verktøybegrensning; ingen screenshot er tilgjengelig. Fysisk Linux-device/native GUI, latencygaranti, installer og alpha/release er fortsatt ikke verifisert. PR13 er merget; PR12 ble automatisk markert merget av GitHub fordi grunnlaget er inkludert. Ingen separat PR12-lukking.
