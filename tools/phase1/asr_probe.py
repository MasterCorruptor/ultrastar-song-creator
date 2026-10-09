"""Phase 1 local ASR smoke probe. Synthetic speech does not establish singing accuracy."""
from __future__ import annotations
import argparse
import hashlib
import importlib.metadata
import json
import os
import pathlib
import platform
import re
import socket
import subprocess
import time
from datetime import datetime, timezone

ROOT = pathlib.Path(__file__).resolve().parents[2]
LOCAL = ROOT / '.agent-local/phase1'
MODEL_ID = 'Systran/faster-whisper-tiny'
REVISION = 'd90ca5fe260221311c53c58e660288d3deb8d356'
REFERENCE = 'This is a local speech recognition test. The music editor will keep all analysis on this computer.'


def distance(a, b):
    previous = list(range(len(b) + 1))
    for i, word in enumerate(a, 1):
        row = [i]
        for j, other in enumerate(b, 1):
            row.append(min(row[-1] + 1, previous[j] + 1, previous[j - 1] + (word != other)))
        previous = row
    return previous[-1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--download-model', action='store_true', help='Allow initial anonymous model download')
    args = parser.parse_args()
    output = LOCAL / 'asr'
    output.mkdir(parents=True, exist_ok=True)
    os.environ['HF_HOME'] = str(LOCAL / 'hf-home')
    os.environ['HF_HUB_CACHE'] = str(LOCAL / 'model-cache')
    os.environ['HF_HUB_DISABLE_TELEMETRY'] = '1'
    os.environ['HF_HUB_DISABLE_XET'] = '1'
    from huggingface_hub import snapshot_download
    model_path = snapshot_download(repo_id=MODEL_ID, revision=REVISION, token=False,
        cache_dir=str(LOCAL / 'model-cache'), local_files_only=not args.download_model,
        allow_patterns=['model.bin', 'config.json', 'tokenizer.json', 'vocabulary.*', 'preprocessor_config.json'])
    source, normalized = output / 'synthetic-speech.wav', output / 'speech-16k.wav'
    if not source.exists():
        raise RuntimeError('Create the synthetic speech fixture using create_speech.ps1 first')
    subprocess.run(['ffmpeg', '-nostdin', '-hide_banner', '-loglevel', 'error', '-y',
                    '-i', str(source), '-ac', '1', '-ar', '16000', str(normalized)], check=True, timeout=30)
    import soundfile as sf
    import av
    from faster_whisper import WhisperModel
    audio, rate = sf.read(normalized, dtype='float32')
    if rate != 16000:
        raise RuntimeError('Audio was not normalized')
    os.environ['HF_HUB_OFFLINE'] = '1'
    # Deny Python socket connections during model loading and inference.
    original_connect, original_connect_ex = socket.socket.connect, socket.socket.connect_ex
    def denied(*_args, **_kwargs):
        raise RuntimeError('Network connection attempted during offline ASR probe')
    socket.socket.connect = denied
    socket.socket.connect_ex = denied
    try:
        start = time.perf_counter()
        model = WhisperModel(model_path, device='cpu', compute_type='int8', cpu_threads=4, local_files_only=True)
        setup_seconds = time.perf_counter() - start
        start = time.perf_counter()
        segments, info = model.transcribe(audio, language='en', beam_size=5, word_timestamps=True, vad_filter=False)
        segments = list(segments)  # faster-whisper is lazy; include iteration in timings.
        inference_seconds = time.perf_counter() - start
    finally:
        socket.socket.connect = original_connect
        socket.socket.connect_ex = original_connect_ex
    transcript = ' '.join(segment.text.strip() for segment in segments)
    reference_words = re.findall(r'[a-z]+', REFERENCE.lower())
    actual_words = re.findall(r'[a-z]+', transcript.lower())
    words = [word for segment in segments for word in (segment.words or [])]
    if not actual_words or not words:
        raise RuntimeError('No transcription or word timestamps were produced')
    if any(not (0 <= word.start <= word.end <= len(audio) / rate + 0.1) for word in words):
        raise RuntimeError('Word timestamp outside audio duration')
    libraries = {}
    for name, meta in av._core.library_meta.items():
        configuration = meta.get('configuration', '')
        libraries[name] = {'version': meta.get('version'), 'declared_license': meta.get('license'), 'gpl_enabled': '--enable-gpl' in configuration,
                           'nonfree_enabled': '--enable-nonfree' in configuration}
    report = {'generated_utc': datetime.now(timezone.utc).isoformat(), 'status': 'passed',
        'python': platform.python_version(), 'model_id': MODEL_ID, 'revision': REVISION,
        'model_sha256': hashlib.sha256((pathlib.Path(model_path) / 'model.bin').read_bytes()).hexdigest(),
        'model_card_license': 'mit', 'model_credentials_sent': False,
        'device': 'cpu', 'compute_type': 'int8', 'cpu_threads': 4,
        'python_socket_connections_denied_during_inference': True,
        'input_seconds': len(audio) / rate, 'setup_seconds': setup_seconds,
        'inference_seconds': inference_seconds, 'real_time_factor': inference_seconds / (len(audio) / rate),
        'reference': REFERENCE, 'transcript': transcript, 'word_count': len(words),
        'synthetic_speech_word_error_rate': distance(reference_words, actual_words) / len(reference_words),
        'versions': {name: importlib.metadata.version(name) for name in ('faster-whisper', 'ctranslate2', 'av', 'huggingface-hub')},
        'pyav_ffmpeg_libraries': libraries,
        'limitations': ['English SAPI synthetic speech, not singing', 'no forced alignment of supplied lyrics',
                       'no acoustic word-boundary accuracy validation', 'no OS-level network isolation',
                       'tiny model only; no GPU or Linux execution']}
    (output / 'asr-report.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(report, indent=2))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
