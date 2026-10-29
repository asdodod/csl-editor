param([Parameter(Mandatory=$true)][string]$BeatSaberDir,[Parameter(Mandatory=$true)][string]$PythonExecutable,[Parameter(Mandatory=$true)][string]$UnityPyPath,[Parameter(Mandatory=$true)][string]$BaseBundle)
$ErrorActionPreference='Stop'
$folder=Join-Path $BeatSaberDir 'UserData/SaberWorkshop'
New-Item -ItemType Directory -Force -Path (Join-Path $folder 'Exporter') | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'export_csl.py') -Destination (Join-Path $folder 'Exporter/export_csl.py')
$baseTarget=Join-Path $folder 'Exporter/base.saber'
Copy-Item -LiteralPath $BaseBundle -Destination $baseTarget
$settings=@{PythonExecutable=$PythonExecutable;UnityPyPath=$UnityPyPath;BaseBundle=$baseTarget;CustomSabers=(Join-Path $BeatSaberDir 'CustomSabers');Script=(Join-Path $folder 'Exporter/export_csl.py')}
[IO.File]::WriteAllText((Join-Path $folder 'exporter.json'),($settings|ConvertTo-Json),[Text.UTF8Encoding]::new($false))
Write-Output 'Local CSL exporter configured.'
