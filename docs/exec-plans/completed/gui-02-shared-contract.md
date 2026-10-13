# GUI-02 – aktiv felles kontraktplan

STATUS: COMPLETE (SUPERVISOR-godkjent felles grunnlag). TASK: gui-02-shared-contract. BRANCH: work/gui-02-shared-contract. Base86660f5ff47e3bffdafa0e28b79a80d364225e80. MODEL USED: ukjent; bestilt Sol Medium. SMALL/PARALLEL_WITH_DEPENDENCIES, foreløpig én worker; ingen ekstra worker/worktree opprettet. Faktisk kvote før start54%femtimer/60%uke igjen.

[Arbeidsordre](../../GUI_02_SHARED_WORK_ORDER.md), [API-/fileierskap](../../GUI_02_SHARED_CONTRACT.md).

1. Compilable ren TimelineMapping og finite/offset/seek/clock/waveform-interval-tester; Desktop→Audio-ref/locks.
2. Konkret uavhengig A/B-scope/signatur/lifecycle, sekvensiell app-integration og filgrenser dokumentert.
3. Locked restore/Release build/test449regresjoner+nye mappingtester/format og master/ADR/øvrig kode/lock-integritet/lenker/whitespace.
4. Commit/push og stopp for kontraktreview, ingen komponent/GUI/tone/CU-test/merge eller workerstart.

## Verifisert felles checkpoint

COMPLETED: ren immutable TimelineMapping/TimelineSpan med demo-defaults, signed offset, frame/seek/clock/null/clip-boundary-projeksjon og13 mappingtestcases. Desktop→Audio reference og to låste Desktop-lockoppdateringer, bare tidligere godkjente Hexa-pakker transitive. Konkret A/B-API, filgrenser, borrowed lifecycle og sekvensiell app-wiring dokumentert; ingen faktiske A/B-klasser eller UI-endringer.

VERIFICATION: Windows locked restore/Release build0warnings0errors/test462/462 (449 eksisterende+13 mapping) og SDK-format/verify-no-changes. Desktop23=10 eksisterendeGUI+13mapping, Audio17, Core422. Pure mapping tester krever ingen native playback; regressionsuiten laster eksisterende native bibliotekstest, spiller ingen tone. Ingen native GUI/CU-test/probe eller Linux-retest i denne SMALL-forberedelsen. Masteroriginal/ADR/alle tidligere moduler/fixtures/øvrige locks og pakke/notices-manifester er bytebevart. Desktop lockfile-diff har kun godkjente Audio dependencies/contentHashes og ProjectReference, ingen nye eksterne versjoner. JSON/dokumentlenker/privacy/whitespace kontrolleres før commit.

FILES: Timeline/TimelineMapping.cs, TimelineMappingTests.cs, Desktop.csproj og to locks, GUI_02_SHARED_WORK_ORDER/CONTRACT/denne plan. EditorWindow/Application/EditingSession/DemoSong og Audio er uendret.

UNRESOLVED: ingen kjent blokkerende feil i mappingen; waveform/presenter/panel/wiring/playback-GUI er ikke implementert. Kontrakten er foreslått for senere workers, må reviewgodkjennes før start. NEXT RECOMMENDED STEP: SUPERVISOR reviewer dette eksakte checkpointet og velger eventuell A/B-dispatch med to isolerte branches/worktrees fra samme reviewed SHA etter samlet kvotevurdering. A eier bare WaveformControl.cs/WaveformControlTests.cs; B bare PlaybackPresenter.cs/PlaybackPanel.cs/PlaybackPanelTests.cs. Begge bygger uten den andre; integrasjonsansvarlig eier felles prosjekt-/lock-/mapping-/app-/docfiler. Ingen slike workers/worktrees opprettes her, ingen merge. OWNER DECISION REQUIRED: NO. LAST COMMIT rapporteres fra Git/finalrapport etter commit/push.

Kvote ved verifisert kontraktcheckpoint:49%femtimer/59%uke gjenstår, faktisk avlest. Ingen samtidig worker startet; samlet neste ressursvurdering tilhører SUPERVISOR.

Godkjent kontraktcommit: 67bf70e6d8bc30c8c7d4caad3754a4a160ee0eb2. Komponenter og [editor binding](../active/gui-02-editor-binding.md) er senere separate leveranser. PR12 er fortsatt referanse, ikke merget.
