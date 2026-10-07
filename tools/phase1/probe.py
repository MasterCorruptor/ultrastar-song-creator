"""Bounded Phase 1 feasibility probes; this is not application code.

Core uses the standard library plus installed ffmpeg/ffprobe. Analysis is opt-in
and uses the separate evaluation environment. Provider requests are opt-in and
retain schema/availability measurements only, never song lyrics.
"""
from __future__ import annotations

import argparse
import array
import hashlib
import importlib.metadata
import json
import math
import pathlib
import platform
import shutil
import statistics
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import wave
from datetime import datetime, timezone

ROOT = pathlib.Path(__file__).resolve().parents[2]
USER_AGENT = 'UltraStarSongCreator-Phase1/0.1 (https://github.com/MasterCorruptor/ultrastar-song-creator)'


def run_command(arguments: list[str], timeout: int = 30) -> tuple[str, float]:
    start = time.perf_counter()
    result = subprocess.run(arguments, capture_output=True, encoding='utf-8', timeout=timeout)
    if result.returncode:
        raise RuntimeError(f'{arguments[0]} failed ({result.returncode}): {result.stderr[-1000:]}')
    return result.stdout, time.perf_counter() - start


def core_probe(output: pathlib.Path) -> dict:
    ffmpeg, ffprobe = shutil.which('ffmpeg'), shutil.which('ffprobe')
    if not ffmpeg or not ffprobe:
        raise RuntimeError('Core media probe requires installed ffmpeg and ffprobe')
    fixture = output / 'synthetic-stereo.wav'
    rate, duration = 44100, 6
    samples = array.array('h')
    for index in range(rate * duration):
        t = index / rate
        samples.extend((round(12000 * math.sin(2 * math.pi * 220 * t)),
                        round(10000 * math.sin(2 * math.pi * 440 * t))))
    if sys.byteorder != 'little':
        samples.byteswap()
    with wave.open(str(fixture), 'wb') as handle:
        handle.setnchannels(2)
        handle.setsampwidth(2)
        handle.setframerate(rate)
        handle.writeframes(samples.tobytes())
    mp3, normalized = output / 'synthetic.mp3', output / 'normalized-mono.wav'
    _, encode_time = run_command([ffmpeg, '-nostdin', '-hide_banner', '-loglevel', 'error', '-y',
                                 '-i', str(fixture), '-codec:a', 'libmp3lame', '-q:a', '2', str(mp3)])
    _, decode_time = run_command([ffmpeg, '-nostdin', '-hide_banner', '-loglevel', 'error', '-y',
                                 '-i', str(mp3), '-ac', '1', '-ar', '16000', '-codec:a', 'pcm_s16le', str(normalized)])
    info_text, _ = run_command([ffprobe, '-v', 'error', '-show_entries',
                               'stream=sample_rate,channels:format=duration', '-of', 'json', str(normalized)])
    info = json.loads(info_text)
    with wave.open(str(normalized), 'rb') as handle:
        frames, channels, sample_rate = handle.getnframes(), handle.getnchannels(), handle.getframerate()
    assert (frames, channels, sample_rate) == (96000, 1, 16000), 'Normalization changed expected duration/channel/rate'
    version, _ = run_command([ffmpeg, '-version'])
    # UltraStar v1 timing has four file ticks per musical beat, pitch is relative to C4.
    # These are specification arithmetic checks, not an importer/exporter implementation.
    bpm, gap_ms = 120, 1000
    file_tick = ((1500 - gap_ms) / 1000) * (bpm / 60) * 4
    assert file_tick == 4
    assert gap_ms + file_tick * 60000 / (4 * bpm) == 1500
    assert 69 - 60 == 9 and 58 - 60 == -2
    return {
        'status': 'passed', 'fixture_sha256': hashlib.sha256(fixture.read_bytes()).hexdigest(),
        'input_seconds': duration, 'output_frames': frames, 'output_rate': sample_rate,
        'output_channels': channels, 'encode_seconds': encode_time, 'decode_resample_seconds': decode_time,
        'ffprobe': info, 'ffmpeg_version': version.splitlines()[0],
        'ffmpeg_gpl_enabled': '--enable-gpl' in version,
        'ffmpeg_nonfree_enabled': '--enable-nonfree' in version,
        'spec_checks': ['quadrupled_BPM_ticks', 'GAP_milliseconds', 'pitch_relative_to_C4'],
    }


def analysis_probe() -> dict:
    import numpy as np
    import librosa
    from swift_f0 import SwiftF0, segment_notes

    sr, hop = 16000, 256
    spans = [(0.4, 1.5, 220.0), (1.8, 2.9, 329.6275569), (3.2, 4.3, 440.0)]
    timeline = np.arange(sr * 5, dtype=np.float64) / sr
    fixtures = {}
    for kind in ('harmonic', 'vibrato'):
        audio = np.zeros_like(timeline)
        expected = np.full_like(timeline, np.nan)
        for start, end, frequency in spans:
            selected = (timeline >= start) & (timeline < end)
            local_time = timeline[selected] - start
            f0 = np.full_like(local_time, frequency)
            if kind == 'vibrato':
                f0 = frequency * np.exp2(0.3 * np.sin(2 * np.pi * 5 * local_time) / 12)
            phase = 2 * np.pi * np.cumsum(f0) / sr
            envelope = np.minimum(1.0, np.minimum(local_time / 0.03, (end - start - local_time) / 0.03))
            audio[selected] = envelope * (0.5 * np.sin(phase) + 0.15 * np.sin(2 * phase) + 0.06 * np.sin(3 * phase))
            expected[selected] = f0
        fixtures[kind] = (audio.astype(np.float32), expected)
    setup = time.perf_counter()
    detector = SwiftF0(threads=1, spin=False)
    swift_setup = time.perf_counter() - setup
    audio = fixtures['harmonic'][0]
    cold_times = {}
    for name in ('pyin', 'swift-f0'):
        start = time.perf_counter()
        if name == 'pyin':
            librosa.pyin(audio, fmin=65, fmax=1047, sr=sr, frame_length=2048, hop_length=hop)
        else:
            detector.detect(audio, sr)
        cold_times[name] = time.perf_counter() - start
    rows = []
    for kind, (audio, expected) in fixtures.items():
        for name in ('pyin', 'swift-f0'):
            elapsed = []
            note_count = None
            for _ in range(2):
                start = time.perf_counter()
                if name == 'pyin':
                    pitch, voiced, confidence = librosa.pyin(audio, fmin=65, fmax=1047,
                        sr=sr, frame_length=2048, hop_length=hop)
                    times = librosa.times_like(pitch, sr=sr, hop_length=hop)
                else:
                    result = detector.detect(audio, sr)
                    pitch, confidence, times = result.pitch_hz, result.confidence, result.timestamps
                    voiced = confidence >= 0.5
                    note_count = len(segment_notes(result))
                elapsed.append(time.perf_counter() - start)
            expected_at_frame = expected[np.minimum((times * sr).astype(int), len(expected) - 1)]
            interior = np.zeros(len(times), dtype=bool)
            silence = np.ones(len(times), dtype=bool)
            for begin, end, _ in spans:
                interior |= (times >= begin + 0.12) & (times <= end - 0.12)
                silence &= ~((times >= begin - 0.12) & (times <= end + 0.12))
            correct_shape = len(pitch) == len(confidence) == len(times)
            usable = interior & voiced & np.isfinite(pitch) & (pitch > 0)
            cents = 1200 * np.abs(np.log2(pitch[usable] / expected_at_frame[usable]))
            coverage = float(np.count_nonzero(usable) / np.count_nonzero(interior))
            silence_fp = float(np.mean(voiced[silence]))
            median_cents = float(np.median(cents)) if len(cents) else None
            assert correct_shape and coverage >= 0.8 and median_cents is not None and median_cents < 50, f'{name}/{kind}: pitch feasibility failed'
            rows.append({'detector': name, 'fixture': kind, 'duration_seconds': 5,
                'warm_seconds': elapsed, 'warm_median_seconds': statistics.median(elapsed),
                'warm_real_time_factor': statistics.median(elapsed) / 5,
                'voiced_coverage': coverage, 'median_abs_cents': median_cents,
                'p95_abs_cents': float(np.quantile(cents, 0.95)),
                'silence_false_voiced_fraction': silence_fp, 'proposed_note_count': note_count,
                'input_sha256': hashlib.sha256(audio.tobytes()).hexdigest()})
    beat_rows = []
    # A synthetic click train is not a benchmark of complex musical beat tracking.
    for tempo in (90, 120):
        click_times = np.arange(0.5, 20, 60 / tempo)
        audio = librosa.clicks(times=click_times, sr=sr, length=sr * 20)
        elapsed = []
        for _ in range(2):
            start = time.perf_counter()
            estimated, frames = librosa.beat.beat_track(y=audio, sr=sr, hop_length=hop, trim=False)
            elapsed.append(time.perf_counter() - start)
        estimated = float(np.asarray(estimated).reshape(-1)[0])
        beat_times = librosa.frames_to_time(frames, sr=sr, hop_length=hop)
        interior_beats = beat_times[(beat_times >= 1) & (beat_times <= 19)]
        nearest = np.min(np.abs(interior_beats[:, None] - click_times[None, :]), axis=1)
        assert abs(estimated - tempo) / tempo < 0.05, 'Synthetic tempo feasibility failed'
        beat_rows.append({'expected_bpm': tempo, 'estimated_bpm': estimated,
            'relative_tempo_error': abs(estimated - tempo) / tempo,
            'median_nearest_click_error_seconds': float(np.median(nearest)),
            'beat_count': len(beat_times), 'seconds': elapsed})
    return {'status': 'passed', 'swift_model_setup_seconds': swift_setup, 'cold_seconds': cold_times,
        'pitch': rows, 'beat': beat_rows, 'versions': {name: importlib.metadata.version(name)
        for name in ('numpy', 'scipy', 'librosa', 'numba', 'swift-f0', 'onnxruntime')},
        'limitations': ['synthetic mono audio only', 'no separation/ASR/alignment inference',
        'no actual singing accuracy benchmark', 'no GPU benchmark', 'no Linux run',
        'two warm samples per detector, not statistically representative']}


def provider_probe() -> list[dict]:
    requests = [
        ('lrclib', 'https://lrclib.net/api/search?' + urllib.parse.urlencode(
            {'artist_name': 'Rick Astley', 'track_name': 'Never Gonna Give You Up'})),
        ('lyrics.ovh', 'https://api.lyrics.ovh/v1/Rick%20Astley/Never%20Gonna%20Give%20You%20Up'),
        ('musicbrainz', 'https://musicbrainz.org/ws/2/recording/?' + urllib.parse.urlencode(
            {'query': 'recording:"Never Gonna Give You Up" AND artist:"Rick Astley"', 'fmt': 'json', 'limit': 3})),
    ]
    results = []
    for name, url in requests:
        start = time.perf_counter()
        request = urllib.request.Request(url, headers={'User-Agent': USER_AGENT, 'Accept': 'application/json'})
        row = {'provider': name, 'url': url, 'credentials_sent': False}
        try:
            with urllib.request.urlopen(request, timeout=15) as response:
                data = json.load(response)
                row.update({'status': response.status, 'content_type': response.headers.get('Content-Type')})
            if name == 'lrclib':
                row.update({'result_count': len(data), 'keys': sorted(data[0]) if data else [],
                    'with_synced_lyrics': sum(bool(item.get('syncedLyrics')) for item in data),
                    'with_plain_lyrics': sum(bool(item.get('plainLyrics')) for item in data)})
            elif name == 'lyrics.ovh':
                row.update({'keys': sorted(data), 'has_lyrics': bool(data.get('lyrics'))})
            else:
                row.update({'keys': sorted(data), 'total_count': data.get('count'),
                    'returned_count': len(data.get('recordings', []))})
        except urllib.error.HTTPError as error:
            row.update({'status': error.code, 'error': str(error)})
        except (urllib.error.URLError, TimeoutError, ValueError, OSError) as error:
            row.update({'status': 'error', 'error': str(error)})
        row['elapsed_seconds'] = time.perf_counter() - start
        results.append(row)
    return results


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--analysis', action='store_true')
    parser.add_argument('--providers', action='store_true')
    parser.add_argument('--output', type=pathlib.Path, default=ROOT / '.agent-local/phase1/results')
    args = parser.parse_args()
    output = args.output.resolve()
    if not output.is_relative_to(ROOT / '.agent-local'):
        parser.error('Probe output must stay under repository .agent-local/')
    output.mkdir(parents=True, exist_ok=True)
    report = {'generated_utc': datetime.now(timezone.utc).isoformat(), 'python': platform.python_version(),
              'platform': platform.system(), 'scope': 'Phase 1 feasibility; no production stack decision'}
    failed = False
    for name, enabled, call in [('core', True, lambda: core_probe(output)),
                               ('analysis', args.analysis, analysis_probe),
                               ('providers', args.providers, provider_probe)]:
        if enabled:
            try:
                report[name] = call()
            except Exception as error:
                failed = True
                report[name] = {'status': 'failed', 'error': f'{type(error).__name__}: {error}'}
    (output / 'report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 1 if failed else 0


if __name__ == '__main__':
    raise SystemExit(main())
