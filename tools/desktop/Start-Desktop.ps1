# Development test surface, not an installer. Only repository-local build outputs are created.
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskLocalDotnet = Join-Path $taskRoot '.agent-local/phase1/dotnet/dotnet.exe'
$taskDotnet = if (Test-Path -LiteralPath $taskLocalDotnet) { $taskLocalDotnet } else { (Get-Command dotnet -ErrorAction Stop).Source }
$taskOldRoot = $env:DOTNET_ROOT
$taskOldHome = $env:DOTNET_CLI_HOME
$taskOldCache = $env:NUGET_PACKAGES
Push-Location -LiteralPath $taskRoot
try {
    if ((& $taskDotnet --version) -notmatch '^10\.') { throw 'Install .NET SDK 10 matching global.json.' }
    if ($taskDotnet -eq $taskLocalDotnet) {
        $env:DOTNET_ROOT = Split-Path $taskDotnet
        $env:DOTNET_CLI_HOME = Join-Path $taskRoot '.agent-local/phase1/dotnet-home'
        $env:NUGET_PACKAGES = Join-Path $taskRoot '.agent-local/phase1/nuget'
    }
    $taskProject = 'src/UltraStar.SongCreator.Desktop/UltraStar.SongCreator.Desktop.csproj'
    & $taskDotnet restore $taskProject --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Locked restore failed.' }
    & $taskDotnet build $taskProject -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed. Close an already running test window before building.' }
    $taskExe = Join-Path $taskRoot 'src/UltraStar.SongCreator.Desktop/bin/Release/net10.0/UltraStar.SongCreator.Desktop.exe'
    Start-Process -FilePath $taskExe -WorkingDirectory $taskRoot
} finally {
    Pop-Location
    $env:DOTNET_ROOT = $taskOldRoot
    $env:DOTNET_CLI_HOME = $taskOldHome
    $env:NUGET_PACKAGES = $taskOldCache
}
