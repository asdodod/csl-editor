param(
  [Parameter(Mandatory=$true)][string]$BeatSaberDir,
  [string]$UnityEditor='C:\Program Files\Unity\Hub\Editor\2022.3.28f1\Editor\Unity.exe',
  [string]$UnityProject,
  [string]$OutputFolder
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(!$UnityProject){$UnityProject=Join-Path $repo '.tools/menu-preview'}
if(!$OutputFolder){$OutputFolder=Join-Path $repo '.local/menu-preview'}
foreach($folder in @('Assets/Editor','Packages','ProjectSettings')){New-Item -ItemType Directory -Force -Path (Join-Path $UnityProject $folder)|Out-Null}
$manifest=Join-Path $UnityProject 'Packages/manifest.json'
if(!(Test-Path -LiteralPath $manifest)){[IO.File]::WriteAllText($manifest,'{"dependencies":{}}')}
[IO.File]::WriteAllText((Join-Path $UnityProject 'ProjectSettings/ProjectVersion.txt'),"m_EditorVersion: 2022.3.28f1`n")
& (Join-Path $repo 'mod/tests/prepare-unity-harness.ps1') -UnityProject $UnityProject -GameDir $BeatSaberDir
& (Join-Path $PSScriptRoot 'prepare-preview.ps1') -UnityProject $UnityProject
New-Item -ItemType Directory -Force -Path $OutputFolder|Out-Null
$log=Join-Path $OutputFolder 'unity.log'
function Quote-Path([string]$Value){if($Value.Contains('"')){throw 'Invalid path'};return '"'+[IO.Path]::GetFullPath($Value)+'"'}
$arguments=@('-batchmode','-projectPath',(Quote-Path $UnityProject),'-executeMethod','PreviewBatch.Run','-logFile',(Quote-Path $log),'-previewMarkup',(Quote-Path (Join-Path $repo 'mod/SaberWorkshop/Editor.bsml')),'-previewOutput',(Quote-Path $OutputFolder))
$process=Start-Process -FilePath $UnityEditor -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden
$report=[IO.File]::ReadAllText($log)
if($process.ExitCode -ne 0 -or !$report.Contains('MENU_PREVIEW_VERIFIED')){throw "Menu preview failed. See $log"}
Write-Output "Offline menu preview verified: $OutputFolder"
Get-ChildItem -LiteralPath $OutputFolder -Filter '*.png'|Select-Object Name,Length
