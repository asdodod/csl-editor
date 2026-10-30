param([Parameter(Mandatory=$true)][string]$BeatSaberDir,[Parameter(Mandatory=$true)][string]$UnityProject)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
foreach($folder in @('Assets/Editor','Assets/GameLibraries','Packages','ProjectSettings')){New-Item -ItemType Directory -Force -Path (Join-Path $UnityProject $folder)|Out-Null}
$managed=Join-Path $BeatSaberDir 'Beat Saber_Data/Managed'
$null=[Reflection.Assembly]::LoadFrom('C:\Program Files\Unity\Hub\Editor\2022.3.28f1\Editor\Data\Managed\Unity.Cecil.dll')
$pending=[Collections.Generic.Queue[string]]::new();foreach($name in @('HMUI','VRUI','Unity.TextMeshPro')){$pending.Enqueue($name)}
$seen=[Collections.Generic.HashSet[string]]::new()
while($pending.Count){$name=$pending.Dequeue();if(!$seen.Add($name)){continue};if($name -match '^(mscorlib|netstandard|System($|\.)|UnityEngine($|\.)|UnityEditor($|\.))'){continue};$file=Join-Path $managed ($name+'.dll');if(!(Test-Path -LiteralPath $file)){continue};Copy-Item -LiteralPath $file -Destination (Join-Path $UnityProject "Assets/GameLibraries/$name.dll") -Force;$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($file);try{foreach($reference in $assembly.MainModule.AssemblyReferences){$pending.Enqueue($reference.Name)}}finally{$assembly.Dispose()}}
$manifest=@{dependencies=@{'com.unity.ugui'='1.0.0'}}
$builtin='C:\Program Files\Unity\Hub\Editor\2022.3.28f1\Editor\Data\Resources\PackageManager\BuiltInPackages'
foreach($module in @('assetbundle','imageconversion','ui','imgui','animation','particlesystem','audio','physics','physics2d','jsonserialize','xr','subsystems','uielements')){if(Test-Path -LiteralPath (Join-Path $builtin "com.unity.modules.$module")){$manifest.dependencies["com.unity.modules.$module"]='1.0.0'}}
[IO.File]::WriteAllText((Join-Path $UnityProject 'Packages/manifest.json'),($manifest|ConvertTo-Json -Depth 10))
[IO.File]::WriteAllText((Join-Path $UnityProject 'ProjectSettings/ProjectVersion.txt'),"m_EditorVersion: 2022.3.28f1`n")
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NativeWidgetProbe.cs') -Destination (Join-Path $UnityProject 'Assets/Editor/NativeWidgetProbe.cs')
& (Join-Path $repo 'mod/tests/prepare-unity-harness.ps1') -UnityProject $UnityProject -GameDir $BeatSaberDir
& (Join-Path $PSScriptRoot 'prepare-preview.ps1') -UnityProject $UnityProject
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NativeMenuCapture.cs') -Destination (Join-Path $UnityProject 'Assets/Editor/NativeMenuCapture.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'NativeButtonRegression.cs') -Destination (Join-Path $UnityProject 'Assets/Editor/NativeButtonRegression.cs')
$surface=[IO.File]::ReadAllText((Join-Path $repo 'mod/SaberWorkshop/WorkshopSurface.cs')).Replace('namespace SaberWorkshop;','namespace SaberWorkshop {')+"`n}"
[IO.File]::WriteAllText((Join-Path $UnityProject 'Assets/WorkshopSurface.cs'),$surface)
$curve=[IO.File]::ReadAllText((Join-Path $repo 'mod/SaberWorkshop/CurvedRawImageMesh.cs')).Replace('namespace SaberWorkshop;','namespace SaberWorkshop {')+"`n}"
[IO.File]::WriteAllText((Join-Path $UnityProject 'Assets/CurvedRawImageMesh.cs'),$curve)
# Zenject in this game has a self assembly reference. Disable the Editor's
# import-time validation for test copies only; never alter game DLL bytes.
Get-ChildItem (Join-Path $UnityProject 'Assets/GameLibraries') -Filter '*.dll'|ForEach-Object{
 $metaPath=$_.FullName+'.meta'
 if(Test-Path -LiteralPath $metaPath){$meta=[IO.File]::ReadAllText($metaPath).Replace('validateReferences: 1','validateReferences: 0')}
 else{$meta="fileFormatVersion: 2`nguid: $([Guid]::NewGuid().ToString('N'))`nPluginImporter:`n  serializedVersion: 2`n  validateReferences: 0`n  platformData:`n  - first:`n      Any: `n    second:`n      enabled: 1`n      settings: {}`n"}
 [IO.File]::WriteAllText($metaPath,$meta)
}
Write-Output ('Prepared native UI probe with '+(Get-ChildItem (Join-Path $UnityProject 'Assets/GameLibraries') -Filter '*.dll').Count+' local game assemblies. No game files changed.')
