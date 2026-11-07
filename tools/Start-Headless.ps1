<#
.SYNOPSIS
  Starts a built MindAttic Unity player with no window, in the shared headless mode (MAU-T18).

.DESCRIPTION
  Runs the player exe with: --headless -batchmode -nographics -logFile <path> [your args]
  --headless is the package's own switch (Unity.Shared.Core.Headless: session mute, run in background, vSync off,
  one "MINDATTIC_HEADLESS_READY <productName>" log line); -batchmode -nographics are Unity's, and are what make
  the player open no window and render nothing. Prints the log path and the process id, and returns an object
  with ProcessId, LogFile and ReadyLine. With -Wait it waits for the ready line and reports it.

  Games call it by relative path from their own run scripts:
    & "$PSScriptRoot\..\..\Unity.Shared\tools\Start-Headless.ps1" -Exe Build\Game.exe -Wait

  CI that launches the exe directly passes the same flags itself:
    Game.exe --headless -batchmode -nographics -logFile <path>

.PARAMETER Exe
  The built player's exe (e.g. Build\Windows\Unity.Eve.exe).

.PARAMETER LogFile
  Where the player writes its log. Default: %TEMP%\<exe name>-headless-<UTC yyyyMMddTHHmmssZ>.log

.PARAMETER Wait
  Wait for the MINDATTIC_HEADLESS_READY line before returning. Exit code 1 on timeout (the player is left
  running unless -StopOnTimeout), 2 if the player exits first.

.PARAMETER TimeoutSeconds
  How long -Wait waits. Default 120.

.PARAMETER StopOnTimeout
  With -Wait, stop the player if the ready line does not come in time.

.PARAMETER GameArgs
  Anything else is passed through to the player after the headless flags (e.g. -muted, -profiles-root <path>).

.EXAMPLE
  .\Start-Headless.ps1 -Exe D:\Builds\Eve\Unity.Eve.exe -Wait -TimeoutSeconds 60 -profiles-root D:\tmp\p
#>
[CmdletBinding(PositionalBinding = $false)]
param(
  [Parameter(Mandatory = $true, Position = 0)][string]$Exe,
  [string]$LogFile,
  [switch]$Wait,
  [int]$TimeoutSeconds = 120,
  [switch]$StopOnTimeout,
  [Parameter(ValueFromRemainingArguments = $true)][string[]]$GameArgs
)

$ErrorActionPreference = 'Stop'
$ReadyMarker = 'MINDATTIC_HEADLESS_READY'

$exePath = (Resolve-Path -LiteralPath $Exe).Path
if (-not $LogFile) {
  $stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
  $LogFile = Join-Path ([IO.Path]::GetTempPath()) ("{0}-headless-{1}.log" -f [IO.Path]::GetFileNameWithoutExtension($exePath), $stamp)
}
$LogFile = [IO.Path]::GetFullPath($LogFile)
$logDir = Split-Path -Parent $LogFile
if (-not (Test-Path -LiteralPath $logDir)) { New-Item -ItemType Directory -Force -Path $logDir | Out-Null }
if (Test-Path -LiteralPath $LogFile) { Remove-Item -LiteralPath $LogFile -Force }

function Quote([string]$a) {
  if ($a -eq '') { return '""' }
  if ($a -notmatch '[\s"]') { return $a }
  return '"' + ($a -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"'
}

$argList = @('--headless', '-batchmode', '-nographics', '-logFile', $LogFile) + @($GameArgs | Where-Object { $_ -ne $null })
$argLine = ($argList | ForEach-Object { Quote $_ }) -join ' '

$proc = Start-Process -FilePath $exePath -ArgumentList $argLine -WorkingDirectory (Split-Path -Parent $exePath) -PassThru
Write-Host "Headless: $exePath $argLine"
Write-Host "Log: $LogFile"
Write-Host "PID: $($proc.Id)"

$readyLine = $null
if ($Wait) {
  $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
  while ($true) {
    if (Test-Path -LiteralPath $LogFile) {
      try {
        # The player keeps the log open for writing: read it shared.
        $fs = [IO.File]::Open($LogFile, 'Open', 'Read', 'ReadWrite, Delete')
        try { $text = (New-Object IO.StreamReader($fs)).ReadToEnd() } finally { $fs.Dispose() }
        $hit = ($text -split "`r?`n") | Where-Object { $_.StartsWith($ReadyMarker) } | Select-Object -First 1
        if ($hit) { $readyLine = $hit.Trim(); break }
      } catch [IO.IOException] { }
    }
    if ($proc.HasExited) {
      Write-Host "Player exited (code $($proc.ExitCode)) before $ReadyMarker; see $LogFile"
      exit 2
    }
    if ([DateTime]::UtcNow -ge $deadline) {
      Write-Host "No $ReadyMarker line within $TimeoutSeconds s; see $LogFile"
      if ($StopOnTimeout) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue; Write-Host "Stopped PID $($proc.Id)." }
      exit 1
    }
    Start-Sleep -Milliseconds 250
  }
  Write-Host "Ready: $readyLine"
}

[pscustomobject]@{ ProcessId = $proc.Id; LogFile = $LogFile; ReadyLine = $readyLine }
