param()
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location -LiteralPath $projectRoot
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.agent-local/phase1/dotnet-home'
$env:NUGET_PACKAGES = Join-Path $projectRoot '.agent-local/phase1/nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_NOLOGO = '1'
$env:TEMP = Join-Path $projectRoot '.agent-local/phase1/tmp'
$env:TMP = $env:TEMP
New-Item -ItemType Directory -Path $env:TEMP -Force | Out-Null
$dotnetPath = Join-Path $projectRoot '.agent-local/phase1/dotnet/dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetPath)) {
    throw 'Run tools/phase1/prepare_dotnet.py with Python 3.12 first, or adapt the probe to an explicitly chosen SDK.'
}
& $dotnetPath restore 'tools/phase1/TimelineProbe/TimelineProbe.csproj' --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Locked restore failed' }
& $dotnetPath run --project 'tools/phase1/TimelineProbe/TimelineProbe.csproj' --configuration Release --no-restore -- '.agent-local/phase1/timeline-results'
if ($LASTEXITCODE -ne 0) { throw 'Timeline probe failed' }
