param([Parameter(Mandatory=$true)][string]$BeatSaberDir)
$ErrorActionPreference='Stop'
$modRoot=$PSScriptRoot
dotnet build (Join-Path $modRoot 'Legacy/Legacy.csproj') -c Release "-p:BeatSaberDir=$BeatSaberDir"
if($LASTEXITCODE -ne 0){throw 'Legacy build failed'}
dotnet build (Join-Path $modRoot 'SaberWorkshop/SaberWorkshop.csproj') -c Release "-p:BeatSaberDir=$BeatSaberDir"
if($LASTEXITCODE -ne 0){throw 'Workshop build failed'}
$version=(Get-Content -LiteralPath (Join-Path $modRoot 'SaberWorkshop/manifest.json') -Raw|ConvertFrom-Json).version
$package=Join-Path $modRoot "../.local/mod-package-$version"
New-Item -ItemType Directory -Force -Path (Join-Path $package 'Plugins') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $package 'UserData/SaberWorkshop/Textures') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $package 'UserData/SaberWorkshop/Projects') | Out-Null
Copy-Item -LiteralPath (Join-Path $modRoot 'SaberWorkshop/bin/Release/net472/SaberWorkshop.dll') -Destination (Join-Path $package 'Plugins/SaberWorkshop.dll')
Copy-Item -LiteralPath (Join-Path $modRoot 'Legacy/LICENSE') -Destination (Join-Path $package 'LICENSE.Legacy.txt')
Copy-Item -LiteralPath (Join-Path $modRoot '../LICENSE') -Destination (Join-Path $package 'LICENSE.txt')
$exporter=Join-Path $package 'ExporterSetup'
New-Item -ItemType Directory -Force -Path $exporter | Out-Null
foreach($file in @('export_csl.py','install-local.ps1')){Copy-Item -LiteralPath (Join-Path $modRoot "exporter/$file") -Destination (Join-Path $exporter $file)}
Copy-Item -LiteralPath (Join-Path $modRoot 'exporter/Unity') -Destination $exporter -Recurse -Force
$zip=Join-Path $modRoot "../SaberWorkshop-$version-BeatSaber-1.40.8.zip"
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -Force
Write-Output "Built package: $zip"
