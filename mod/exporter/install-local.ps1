param([Parameter(Mandatory=$true)][string]$BeatSaberDir,[Parameter(Mandatory=$true)][string]$PythonExecutable,[Parameter(Mandatory=$true)][string]$UnityEditor,[string]$UnityProject)
$ErrorActionPreference='Stop'
$folder=Join-Path $BeatSaberDir 'UserData/SaberWorkshop'
if(!$UnityProject){$UnityProject=Join-Path $folder 'UnityExporter'}
foreach($part in @('Assets/Editor','Assets/Runtime','Packages','ProjectSettings')){New-Item -ItemType Directory -Force -Path (Join-Path $UnityProject $part) | Out-Null}
foreach($file in @('BuildWorkshopCSL.cs')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Unity/Editor/$file") -Destination (Join-Path $UnityProject "Assets/Editor/$file")}
foreach($file in @('SaberDescriptor.cs','CustomSaber.asmdef')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Unity/Runtime/$file") -Destination (Join-Path $UnityProject "Assets/Runtime/$file")}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Unity/WorkshopGlow.shader') -Destination (Join-Path $UnityProject 'Assets/WorkshopGlow.shader')
Copy-Item -LiteralPath (Join-Path $BeatSaberDir 'Libs/Newtonsoft.Json.dll') -Destination (Join-Path $UnityProject 'Assets/Editor/Newtonsoft.Json.dll')
[IO.File]::WriteAllText((Join-Path $UnityProject 'Packages/manifest.json'),'{"dependencies":{"com.unity.modules.assetbundle":"1.0.0","com.unity.modules.imageconversion":"1.0.0"}}')
[IO.File]::WriteAllText((Join-Path $UnityProject 'ProjectSettings/ProjectVersion.txt'),"m_EditorVersion: 2022.3.28f1`n")
New-Item -ItemType Directory -Force -Path (Join-Path $folder 'Exporter') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'export_csl.py') -Destination (Join-Path $folder 'Exporter/export_csl.py')
$settings=@{PythonExecutable=$PythonExecutable;UnityEditor=$UnityEditor;UnityProject=[IO.Path]::GetFullPath($UnityProject);CustomSabers=(Join-Path $BeatSaberDir 'CustomSabers');Script=(Join-Path $folder 'Exporter/export_csl.py')}
[IO.File]::WriteAllText((Join-Path $folder 'exporter.json'),($settings|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
Write-Output 'Native Unity CSL exporter configured.'
