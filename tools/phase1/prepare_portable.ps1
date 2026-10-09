param([string]$Output='.agent-local/phase1/continuation/portable')
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location -LiteralPath $root
$destination=[IO.Path]::GetFullPath((Join-Path $root $Output))
if(-not $destination.StartsWith($root+[IO.Path]::DirectorySeparatorChar)){throw 'Output must stay inside project'}
$env:DOTNET_CLI_HOME="$root/.agent-local/phase1/dotnet-home"
$env:NUGET_PACKAGES="$root/.agent-local/phase1/nuget"
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
& "$root/.agent-local/phase1/dotnet/dotnet.exe" publish tools/phase1/NativeProbe -c Release -r win-x64 --self-contained true -o "$destination/app"
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& py -3.12 tools/phase1/prepare_portable.py --output $destination
exit $LASTEXITCODE
