$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$output = Join-Path $projectRoot '.agent-local/phase1/asr'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$stream = New-Object -ComObject SAPI.SpFileStream
$voice = New-Object -ComObject SAPI.SpVoice
try {
    $voice.Rate = 0
    $stream.Open((Join-Path $output 'synthetic-speech.wav'), 3, $false)
    $voice.AudioOutputStream = $stream
    $voice.Speak('This is a local speech recognition test. The music editor will keep all analysis on this computer.') | Out-Null
    $stream.Close()
} finally {
    [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($voice) | Out-Null
    [System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($stream) | Out-Null
}
