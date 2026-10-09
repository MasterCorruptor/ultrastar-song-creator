param([string]$CMakePath)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location -LiteralPath $root
if(-not $CMakePath){$CMakePath=(Get-Command cmake -ErrorAction Stop).Source}
$env:GIT_CEILING_DIRECTORIES="$root/.agent-local/phase1/continuation"
& $CMakePath -S .agent-local/phase1/continuation/whisper-cpp-source -B .agent-local/phase1/continuation/whisper-cpp-build -G 'Visual Studio 18 2026' -A x64 -DGGML_NATIVE=OFF -DGGML_CUDA=OFF -DGGML_VULKAN=OFF -DGGML_BLAS=OFF -DGGML_OPENMP=OFF -DWHISPER_FFMPEG=OFF -DWHISPER_BUILD_TESTS=OFF -DGGML_AVX=OFF -DGGML_AVX2=OFF -DGGML_FMA=OFF -DGGML_F16C=OFF
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& $CMakePath --build .agent-local/phase1/continuation/whisper-cpp-build --config Release --target whisper-cli -j 4
exit $LASTEXITCODE
