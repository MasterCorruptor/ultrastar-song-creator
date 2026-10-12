# GUI-01 – sangredigeringstestflate

Bestilt av eierautorisert SUPERVISOR 2026-10-12 etter godkjent PR #9. MEDIUM, SEQUENTIAL, én worker. Autoritet: AGENTS, master v0.5, ADR-0001/0002, SUPERVISOR_POLICY, CORE_DOMAIN og DEVELOPMENT. Master og ADR-er bevares.

Mål: vedvarende Windows Avalonia-vindu med deterministisk egen immutable Song, sekunder/MIDI-timeline, notevalg, MoveNote gjennom SongHistory, undo/redo og forståelig validering. Gjeldende snapshot er eneste sangsannhet; ingen parallell muterbar modell. Enkel starttidskontroll er tilstrekkelig, drag kreves ikke.

Acceptance: faktisk Windows-start/lukking; visning og notevalg; gyldig flytting; ugyldig/ikke-finite/utenfor-frase-input bevarer snapshot; eksakt undo/redo og knappetilstand; meningsfulle UI→command/history→view-tester; eksisterende regresjoner, locked restore/Release build/test/format, master/ADR/v1-integritet og dokumentlenker/whitespace. Native input-smoke og bilde når praktisk; headless alene beviser ikke native input. Linux build/headless når praktisk, ingen uprøvd native GUI-påstand.

Kun nødvendige pinnede Avalonia-pakker med låste versjoner og artefaktvise lisenser/notices. Core forblir GUI-/I/O-uavhengig. Offline uten Python/modeller/media/konto/API. Ingen playback/waveform/playhead/seek, generation, acquisition-UI, prosjektfil-I/O/eksport-UI, installer, release, polering eller brede refaktoreringer.

Commit ved sammenhengende checkpoints, push og stopp for SUPERVISOR-review; ingen merge/neste blokk. Faktisk kvote leses før større blokk og checkpoint. Rapporter modell som ukjent hvis ikke eksponert; bestilt Sol Medium er ikke bekreftet observasjon. Eskaler bare reell autoritet-/lisens-/arkitekturkonflikt eller vesentlig scopeendring.

## Avgrenset native-verifikasjon

SUPERVISOR bestilte gui-01-native-smoke etter reset 2026-10-12: SMALL/SEQUENTIAL, kun eksisterende app og manglende native scenarioer, ingen kode/build-/suitegjentakelse/merge. Ett forsøk og avgrenset gjenoppretting viste fangst-/input-geometrifeil; se [gjeldende evidens](exec-plans/completed/gui-01-song-editing.md). Ingen ny ventende apptillatelse er rapportert. Stopp ved dokumentert checkpoint; full acceptance gjenstår.

## Godkjent integrasjon

SUPERVISOR godkjente GUI-01 acceptance etter direkte eierbekreftelse av alle manuelle native scenarioer og review av kode/pakkehashes. gui-01-integration er SMALL/SEQUENTIAL: avslutt plan/status og integrer via eksisterende PR #10 med kildeidentitet mot 1f95b8c. Ingen ny suite/build ved dokumentasjonsendring. Stopp etter integrasjonsrapport; playback/waveform krever ny separat ordre. Tidligere checkpoint-/verifikasjonstekst ovenfor er historisk.
