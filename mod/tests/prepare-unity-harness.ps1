param([Parameter(Mandatory=$true)][string]$UnityProject,[Parameter(Mandatory=$true)][string]$GameDir)
$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$target=Join-Path $UnityProject 'Assets/WorkshopVerification'
New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -LiteralPath (Join-Path $GameDir 'Libs/Newtonsoft.Json.dll') -Destination (Join-Path $target 'Newtonsoft.Json.dll')
$manifestPath=Join-Path $UnityProject 'Packages/manifest.json'
$manifest=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -AsHashtable
foreach($module in @('assetbundle','animation','particlesystem','imageconversion')){$manifest.dependencies["com.unity.modules.$module"]='1.0.0'}
[IO.File]::WriteAllText($manifestPath,($manifest|ConvertTo-Json -Depth 10))
New-Item -ItemType Directory -Force -Path (Join-Path $UnityProject 'Assets/Editor') | Out-Null
$fakeRoot=Join-Path $repoRoot '.local/workshop-test-game'
New-Item -ItemType Directory -Force -Path (Join-Path $fakeRoot 'CustomSabers') | Out-Null
foreach($sourceName in @('SPR_-_Zoog_v4 (1).saber','MWT - MK1 TEK.saber','Batonblade (Simple).saber')){Copy-Item -LiteralPath (Join-Path $GameDir "CustomSabers/$sourceName") -Destination (Join-Path $fakeRoot "CustomSabers/$sourceName")}
foreach($file in @('Models.cs','WorkshopState.cs','WorkbenchLayout.cs','WorkshopAssets.cs')){
  $code=[IO.File]::ReadAllText((Join-Path $repoRoot "mod/SaberWorkshop/$file"))
  $code=$code.Replace('namespace SaberWorkshop;','namespace SaberWorkshop {')+"`n}"
  $code=$code.Replace('Assembly.GetExecutingAssembly().GetManifestResourceStream("SaberWorkshop.workshop-tools.bundle")','File.OpenRead(@"'+(Join-Path $repoRoot 'mod/SaberWorkshop/Assets/workshop-tools.bundle')+'")')
  $code=$code.Replace('Assembly.GetExecutingAssembly().GetManifestResourceStream("SaberWorkshop.size-guide.json")','File.OpenRead(@"'+(Join-Path $repoRoot 'mod/SaberWorkshop/Assets/size-guide.json')+'")')
  $code=$code.Replace('AppDomain.CurrentDomain.BaseDirectory' ,'@"'+$fakeRoot.Replace('"','""')+'"')
  [IO.File]::WriteAllText((Join-Path $target $file),$code)
}
$adapter=@'
using System.Threading.Tasks;
using UnityEngine;
namespace AssetBundleLoadingTools.Utilities {
 public static class AssetBundleExtensions {
  public static Task<AssetBundle> LoadFromFileAsync(string path)=>Task.FromResult(AssetBundle.LoadFromFile(path));
  public static Task<T> LoadAssetAsync<T>(AssetBundle bundle,string path) where T:Object=>Task.FromResult(bundle.LoadAsset<T>(path));
 }
 public static class ShaderRepair {public static Task FixShadersOnGameObjectAsync(GameObject root)=>Task.CompletedTask;}
}
'@
[IO.File]::WriteAllText((Join-Path $target 'GameAdapters.cs'),$adapter)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'VerifyWorkshop.cs') -Destination (Join-Path $UnityProject 'Assets/Editor/VerifyWorkshop.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'VerifyCSLExport.cs') -Destination (Join-Path $UnityProject 'Assets/Editor/VerifyCSLExport.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'VerifyWorkbenchLayout.cs') -Destination (Join-Path $UnityProject 'Assets/Editor/VerifyWorkbenchLayout.cs')
Write-Output "Prepared Unity verification harness at $target"

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'VerifyCreationFeatures.cs') -Destination (Join-Path $UnityProject 'Assets/Editor/VerifyCreationFeatures.cs')
