"""Offline regression for the hash-pinned yt-dlp output/path contract. No media or network."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import socket
import sys
import tempfile

parser = argparse.ArgumentParser()
parser.add_argument("--wheel", type=Path, help="Use the pinned wheel directly, without installing a new runtime/package.")
args = parser.parse_args()
if args.wheel:
    wheel = args.wheel.resolve()
    assert hashlib.sha256(wheel.read_bytes()).hexdigest() == "1d57897e94c6665a0a6f9bc54b34e584284e32c034ffab3a7df25d8f7b24eedf"
    sys.path.insert(0, str(wheel))
import yt_dlp
from yt_dlp.version import __version__
assert __version__ == "2026.08.19", __version__

def no_network(*args, **kwargs):
    raise AssertionError("Network is forbidden in the literal-path regression.")
socket.socket.connect = no_network

root = Path(__file__).resolve().parents[2]
scratch = root / ".agent-local" / "phase3-media-download" / "literal-path-regression"
scratch.mkdir(parents=True, exist_ok=True)
initial = Path.cwd()
os.environ["USC_LITERAL_ENV"] = "expanded-environment"
cases = ["%(title)s", "%(id)s", "%USC_LITERAL_ENV%", "$USC_LITERAL_ENV", "${USC_LITERAL_ENV}",
         "ordinary%percent$dollar", "double%%percent$$dollar", "spaces æøå"]
reproduced = 0
unsafe_paths = 0
checks = 0
with tempfile.TemporaryDirectory(dir=scratch) as owned:
    base = Path(owned)
    try:
        for name in cases:
            stage = base / name / ".usc-download-fixture"
            stage.mkdir(parents=True)
            os.chdir(stage)  # Isolated validation process, not application-global cwd.
            for basename, extension in [("audio.m4a", "m4a"), ("video.mp4", "mp4")]:
                info = {"id": "expanded-id", "title": "expanded-title", "ext": extension}
                # Actual CLI parsing and the pinned template + get_output_path implementation.
                opts = yt_dlp.parse_options(["--ignore-config", "--no-plugin-dirs", "--no-cache-dir",
                                             "--output", basename]).ydl_opts
                assert not opts.get("paths")
                with yt_dlp.YoutubeDL(opts) as ydl:
                    filename = ydl.prepare_filename(info)
                    temporary = ydl.prepare_filename(info, dir_type="temp")
                assert filename == basename, (name, filename)
                assert temporary == basename, (name, temporary)
                actual = Path(filename).absolute()
                assert actual == stage / basename
                actual.write_bytes(b"Own offline fixture")
                assert actual.parent == stage and actual.is_file()
                # Original raw absolute argument: exercise the actual upstream bug without writing there.
                with yt_dlp.YoutubeDL({"outtmpl": str(stage / basename), "quiet": True,
                                      "cachedir": False, "plugin_dirs": []}) as old:
                    raw_filename = old.prepare_filename(info)
                if Path(raw_filename).absolute() != stage / basename:
                    reproduced += 1
                # A relative template plus an unescaped --paths is also unsafe: expand_path runs there.
                path_opts = yt_dlp.parse_options(["--ignore-config", "--no-plugin-dirs", "--no-cache-dir",
                    "--output", basename, "--paths", "home:" + str(stage), "--paths", "temp:" + str(stage)]).ydl_opts
                with yt_dlp.YoutubeDL(path_opts) as path_only:
                    path_filename = path_only.prepare_filename(info)
                if Path(path_filename).absolute() != stage / basename:
                    unsafe_paths += 1
                checks += 1
        assert reproduced >= 4, "The original raw-template defect was not reproduced."
        assert unsafe_paths >= 4, "Raw --paths environment expansion was not reproduced."
        assert sorted(p.name for p in base.iterdir()) == sorted(cases), "Unexpected sibling directory."
    finally:
        os.chdir(initial)
print(json.dumps({"version": __version__, "platform": sys.platform, "literal_file_checks": checks,
                  "original_unsafe_results": reproduced, "raw_paths_unsafe_results": unsafe_paths, "network": "forbidden", "scratch_cleaned": True}))
