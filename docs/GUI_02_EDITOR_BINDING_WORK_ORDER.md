# GUI-02 editor binding

SUPERVISOR CONTINUE 2026-10-12. MEDIUM/SEQUENTIAL, en implementasjonsarbeider; bestilt Sol Medium, faktisk modell/reasoning ukjent.

Autoritet: AGENTS, uendret master/aksepterte ADR-er, SUPERVISOR_POLICY, [felles kontrakt](GUI_02_SHARED_CONTRACT.md), [GUI-01](GUI_01_TEST_SURFACE.md) og [Audio](AUDIO_PLAYBACK.md).

Godkjent B: 14b81dbd8ba57b5ea7753751052c08d4bfb87e10. Godkjent A: 012c794a237af8882e2b08fb0fa639a75f2e9d5c. Felles base: 67bf70e6d8bc30c8c7d4caad3754a4a160ee0eb2. Ny branch work/gui-02-editor-binding fra B; cherry-pick A uten A-worktreeendring. Samlet Desktop-build/test og sikkert checkpoint kommer før wiring.

Etter dette checkpointet leses faktisk kvote: under 25% femtimer gir STOP_FOR_RESET før wiring. Ellers avgrenset wiring, med omtrent 10% margin til review. Ingen ekstra worker.

Scope: EditorWindow/EditorApplication, små Desktop-eier/bindingtyper, nye integrationstester og operative docs. Del mapping/offset/scroll mellom waveform/noter/transport. DemoPcm/envelope caches en gang; Presenter Snapshot er eneste playhead-klokke. Bevar notevalg/MoveNote/undo/redo. App-start oppretter ekte MiniAudioPlayback og viser initfeil uten mockfallback; headless test/default testconstructor oppretter aldri native device. En eier rydder panel, presenter, backend i rekkefølge ved close/exit, og delvis init ryddes.

Acceptance: reelle headless klikk/tekst, render/refresh identisk waveform/note-X, pause/seek/stop/EOS, noteredigering/undo/redo med stabil PCM/envelope identity, delt scroll, disposal/ingen polling og simulert synlig initfeil med fortsatt redigering. Locked restore/Release solution build/full Windows suite en gang på samlet kandidat, format/integritet/lenker/whitespace.

Ingen Core/Projects/Acquisition/Audio-/master-/ADR-/manifest-/pakke-/lockendring uten konkret behov. Ingen native GUI/CU/tone/Linux, filimport/decoder/analyse/release/loop/polering. Flytt avsluttede komponentplaner til completed. Ved ferdig wiring commit/push og en draft PR mot main, attach til chat; PR12 bevares som referanse. Stopp for review, ingen merge eller neste oppgave.
