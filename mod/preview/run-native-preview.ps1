param(
 [Parameter(Mandatory=$true)][string]$BeatSaberDir,
 [string]$UnityEditor='C:\Program Files\Unity\Hub\Editor\2022.3.28f1\Editor\Unity.exe',
 [string]$UnityProject,
 [string]$OutputFolder
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(!$UnityProject){$UnityProject=Join-Path $repo '.tools/native-ui-probe'}
if(!$OutputFolder){$OutputFolder=Join-Path $repo '.local/native-ui-probe'}
& (Join-Path $PSScriptRoot 'prepare-native-probe.ps1') -BeatSaberDir $BeatSaberDir -UnityProject $UnityProject
New-Item -ItemType Directory -Force -Path $OutputFolder|Out-Null
$log=Join-Path $OutputFolder 'unity.log'
function Quote-Path([string]$Value){if($Value.Contains('"')){throw 'Invalid path'};return '"'+[IO.Path]::GetFullPath($Value)+'"'}
$arguments=@('-batchmode','-projectPath',(Quote-Path $UnityProject),'-executeMethod','NativeWidgetProbe.Run','-logFile',(Quote-Path $log),'-gameBundles',(Quote-Path (Join-Path $BeatSaberDir 'Beat Saber_Data/StreamingAssets/aa/StandaloneWindows64')),'-probeOutput',(Quote-Path $OutputFolder))
$process=Start-Process -FilePath $UnityEditor -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden
$report=[IO.File]::ReadAllText($log)
if($process.ExitCode -ne 0 -or !$report.Contains('NATIVE_UI_PROBE_COMPLETE')){throw "Native preview failed. See $log"}
Write-Output "Native UI preview verified: $OutputFolder"
Get-ChildItem -LiteralPath $OutputFolder -Filter '*.png'|Select-Object Name,Length
