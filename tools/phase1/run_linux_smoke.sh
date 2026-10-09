#!/bin/sh
set -eu
base=/project/.agent-local/phase1/continuation/linux
mkdir -p "$base/results"
export DOTNET_CLI_HOME="$base/dotnet-home"
export NUGET_PACKAGES="$base/nuget"
dotnet publish tools/phase1/NativeProbe -p:NuGetAudit=false -c Release -r linux-x64 --self-contained true --artifacts-path "$base/artifacts" -o "$base/app"
"$base/app/NativeProbe" .agent-local/phase1/continuation/native-audio.wav "$base/results" --offline
xvfb-run -a "$base/app/NativeProbe" .agent-local/phase1/continuation/native-audio.wav "$base/results" /eval/bin/python tools/phase1/analysis_worker_probe.py --offline-ui
/eval/bin/pip check
