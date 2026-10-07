#!/bin/sh
set -eu
base=/project/.agent-local/phase1/continuation
export GIT_CEILING_DIRECTORIES="$base"
cmake -S "$base/whisper-cpp-source" -B "$base/linux/whisper-build" -DGGML_NATIVE=OFF -DGGML_CUDA=OFF -DGGML_VULKAN=OFF -DGGML_BLAS=OFF -DGGML_OPENMP=OFF -DWHISPER_FFMPEG=OFF -DWHISPER_BUILD_TESTS=OFF -DGGML_AVX=OFF -DGGML_AVX2=OFF -DGGML_FMA=OFF -DGGML_F16C=OFF -DCMAKE_BUILD_TYPE=Release
cmake --build "$base/linux/whisper-build" --target whisper-cli -j4
/eval/bin/python tools/phase1/whisper_cpp_probe.py --exe "$base/linux/whisper-build/bin/whisper-cli" --model "$base/ggml-tiny.bin" --codec unused --prepared-wav --output "$base/linux/results/whisper-cpp.json"
/eval/bin/python tools/phase1/direct_alignment_probe.py --model "$base/portable/models/ct2-tiny" --output "$base/linux/results/direct-alignment.json"
/eval/bin/pip check
