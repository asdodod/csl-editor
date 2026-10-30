param([Parameter(Mandatory=$true)][string]$UnityProject)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$manifestPath=Join-Path $UnityProject 'Packages/manifest.json'
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json -AsHashtable
foreach($module in @('ui','imgui')){$manifest.dependencies["com.unity.modules.$module"]='1.0.0'}
$manifest.dependencies['com.unity.ugui']='1.0.0'
[IO.File]::WriteAllText($manifestPath,($manifest|ConvertTo-Json -Depth 10))
$target=Join-Path $UnityProject 'Assets/MenuPreview'
New-Item -ItemType Directory -Force -Path (Join-Path $target 'Editor')|Out-Null
foreach($file in @('PreviewMenu.cs','PreviewTextSize.cs','PreviewTap.cs')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination (Join-Path $target $file)}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PreviewBatch.cs') -Destination (Join-Path $target 'Editor/PreviewBatch.cs')
Copy-Item -LiteralPath (Join-Path $repo 'mod/SaberWorkshop/Editor.bsml') -Destination (Join-Path $target 'Editor.bsml')
Write-Output "Offline menu preview prepared: $target"

$shared=[IO.File]::ReadAllText((Join-Path $repo "mod/SaberWorkshop/MaterialThumbnail.cs")).Replace("namespace SaberWorkshop;","namespace SaberWorkshop {")+"`n}"
[IO.File]::WriteAllText((Join-Path $target "MaterialThumbnail.cs"),$shared)

$theme=[IO.File]::ReadAllText((Join-Path $repo "mod/SaberWorkshop/WorkshopTheme.cs")).Replace("namespace SaberWorkshop;","namespace SaberWorkshop {").Replace('Assembly.GetExecutingAssembly().GetManifestResourceStream("SaberWorkshop.workshop-theme.json")','File.OpenRead(@"'+(Join-Path $repo 'mod/SaberWorkshop/Assets/workshop-theme.json')+'")')+"`n}"
[IO.File]::WriteAllText((Join-Path $target "WorkshopTheme.cs"),$theme)

$help=[IO.File]::ReadAllText((Join-Path $repo "mod/SaberWorkshop/TutorialContent.cs")).Replace("namespace SaberWorkshop;","namespace SaberWorkshop {")+"`n}"
[IO.File]::WriteAllText((Join-Path $target "TutorialContent.cs"),$help)
