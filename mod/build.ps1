param([Parameter(Mandatory=$true)][string]$BeatSaberDir)
$ErrorActionPreference='Stop'
$modRoot=$PSScriptRoot
dotnet build (Join-Path $modRoot 'Legacy/Legacy.csproj') -c Release "-p:BeatSaberDir=$BeatSaberDir"
if($LASTEXITCODE -ne 0){throw 'Legacy build failed'}
dotnet build (Join-Path $modRoot 'SaberWorkshop/SaberWorkshop.csproj') -c Release "-p:BeatSaberDir=$BeatSaberDir"
if($LASTEXITCODE -ne 0){throw 'Workshop build failed'}
$package=Join-Path $modRoot '../.local/mod-package'
New-Item -ItemType Directory -Force -Path (Join-Path $package 'Plugins') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $package 'UserData/SaberWorkshop/Textures') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $package 'UserData/SaberWorkshop/Projects') | Out-Null
Copy-Item -LiteralPath (Join-Path $modRoot 'SaberWorkshop/bin/Release/net472/SaberWorkshop.dll') -Destination (Join-Path $package 'Plugins/SaberWorkshop.dll')
Copy-Item -LiteralPath (Join-Path $modRoot 'README.md') -Destination (Join-Path $package 'README.md')
Copy-Item -LiteralPath (Join-Path $modRoot 'Legacy/LICENSE') -Destination (Join-Path $package 'LICENSE.Legacy.txt')
Copy-Item -LiteralPath (Join-Path $modRoot '../LICENSE') -Destination (Join-Path $package 'LICENSE.txt')
$zip=Join-Path $modRoot '../SaberWorkshop-0.1.0-BeatSaber-1.40.8.zip'
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -Force
Write-Output "Built package: $zip"
