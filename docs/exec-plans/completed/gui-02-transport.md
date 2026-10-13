# GUI-02 B – aktiv transportplan

STATUS: COMPLETE (SUPERVISOR-godkjent komponentcheckpoint). TASK: gui-02-transport. BRANCH: work/gui-02-transport. Base67bf70e6d8bc30c8c7d4caad3754a4a160ee0eb2. MODEL USED: ukjent; bestilt Sol Medium. MEDIUM/PARALLEL_WITH_DEPENDENCIES, B av to isolerte workers. Faktisk kvote ved start42%femtimer/58%uke igjen.

[Arbeidsordre](../../GUI_02_TRANSPORT_WORK_ORDER.md), [delt API/lifecycle/filgrenser](../../GUI_02_SHARED_CONTRACT.md).

1. Presenter/panel etter delt kontrakt, uten backend-eierskap eller selvstendig klokke.
2. Ekte headless UI-handler/render-snapshot-/failure-/lifecycle-tester, uavhengig av A.
3. Locked restore/Release Desktop-build/relevante Desktop-tester/format/protected-integritet/whitespace.
4. Commit/push på B-branch og stopp for review. Ingen GUI-wiring/PR/merge eller A-worktree-kontroll.


## Checkpoint 2026-10-12

COMPLETED: PlaybackPresenter/PlaybackPanel med borrowed IPcmPlayback, snapshotbasert posisjon, UI-thread-publicering, signed offset, validerte seek-kommandoer, knappestatus og synlige feil. Polling stopper ved detach/dispose; ingen backend/presenter disponeres av panelet. Ingen selvstendig lydklokke, native device eller app-wiring. Shared API er bevart.

REQUEST_FIX er gjennomført: korrekt UTF-8 i «Søk til tid», «Slutt på klippet» og feilteksten for starttid. Ny headless test kontrollerer eksakt synlig ordlyd gjennom klikk/seek/EOS. Faktiske sluttfiler er lest som UTF-8 og kontrollert for feil tegnkoding.

VERIFICATION:
- Tidligere locked restore av Desktop.Tests: bestått; prosjekt-/dependency-/lockfiler er byteidentiske med godkjent base etter rettingen.
- Release build Desktop.Tests (inkludert Desktop/Audio/Core-referanser), --no-restore: 0 warnings/errors.
- Desktop.Tests Release, --no-build --no-restore: 37/37 bestått, inkludert 14 B-tilfeller (13 eksisterende + norsk tekstregresjon). Headless klikk/tekst, ekte PCM render/snapshot, pause/seek/stop/EOS, offset/bounds, feil, UI-thread og lifecycle dekkes.
- dotnet format på de tre eide kode-/testfilene, deretter --verify-no-changes: bestått.
- Beskyttede tidligere trackede filer sammenlignet med 67bf70e: byteidentiske, med kun autoriserte operative AGENTS/policy-unntak. Master byteidentisk med original.
- Public inventory/dokumentlenker/whitespace kontrolleres før commit.

FILES: de tre eide PlaybackPresenter.cs/PlaybackPanel.cs/PlaybackPanelTests.cs, egen arbeidsordre/denne plan og operative AGENTS.md/SUPERVISOR_POLICY.md. Ingen A-/shared-/EditorWindow/Application-/csproj-/lockendring. Panellets new Presenter-property bevarer avtalt API-navn over Avalonia sin arvede property.

UNRESOLVED: ingen kjent komponentblokker. App-integrasjon, native playback/lydenhet og visuell sluttbrukertest gjenstår som separat ordre. Core422/Linux/native/CU er ikke kjørt på nytt; dette checkpointet gir ingen nye påstander om full app eller disse miljøene.

NEXT RECOMMENDED STEP: SUPERVISOR-review av pushet B-checkpoint, deretter separat avgrenset sekvensiell integrasjonsordre. Ingen PR/merge/cherry-pick nå. OWNER DECISION REQUIRED: NO.

Ressursstatus sist levert av SUPERVISOR før siste retting: 33% femtimer / 57% uke igjen; reset 13Oct02:38 / 18Oct07:04 Oslo. Faktisk modell/reasoning er ikke eksponert og rapporteres ukjent (bestilt Sol Medium). Verifikasjonen gjelder kodefilene i dette checkpointets commit; eksakt SHA leveres i sluttrapporten.

Godkjent B-commit: 14b81dbd8ba57b5ea7753751052c08d4bfb87e10. Fortsettelse: [editor binding](gui-02-editor-binding.md).
