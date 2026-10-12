# Audio-01 – PCM-playback og waveform-kontrakt

SUPERVISOR-ordre 2026-10-12 etter godkjent GUI-01/main 0003bd2. MEDIUM/SEQUENTIAL, én worker, bestilt gpt-6.1-sol/high; faktisk modell/reasoning rapporteres ukjent hvis ikke eksponert. Autoritet: AGENTS/master v0.5, SUPERVISOR_POLICY, ADR-0001/0002, CORE_DOMAIN/DEVELOPMENT og Fase 1 playback-evidens.

Mål: separat utskiftbar PCM-playback-service med miniaudio/Hexa.NET, ett autoritativt frame/sample-rate-par og bounded waveform-envelope av samme immutable clip. Første profil er in-memory float32 interleaved mono/stereo; egen lavvolum 8s-fixture. Kontrakt reviewes før GUI-binding.

Acceptance: samme render/clock-bane uten device dekker samples/frames/pause/resume/stop/seek/EOS/dispose og samtidige kommandoer; waveform-grenser/final delvis bucket/tom clip, bounded ressurser og ingen kopiering ved noteredigering. Faktisk kort Windows native smoke dekker init/start/cursor/pause/resume/seek/sikker avslutning; konkret gap ved manglende device. Locked restore/Release build/test/format og 432 tidligere regresjoner; Linux no-device og native loading når praktisk. Master/ADR/historiske fixtures/lagringsformat bevares; pakkehashes/native assets/notices dokumenteres. Ingen målt akustisk latency påstås.

Utenfor scope: GUI-endring/binding/tegning/playhead, loop/device-switch, filparser/audioimport/codecs, Python/generation/acquisition, installer/release/CI og polering. Kontroller faktisk kvote ved checkpoint, behold margin; commit/push ved naturlige konsistente steg. Stopp ved reviewbart checkpoint/draft PR, ingen merge/GUI-02 uten separat review/ordre. Ingen alternative mock-backends ved devicefeil.
