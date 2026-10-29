param([Parameter(Mandatory=$true)][string]$BeatSaberDir)
$ErrorActionPreference='Stop'
$managed=Join-Path $BeatSaberDir 'Beat Saber_Data/Managed'
$null=[Reflection.Assembly]::LoadFrom((Join-Path $managed 'UnityEngine.CoreModule.dll'))
$bsml=[Reflection.Assembly]::LoadFrom((Join-Path $BeatSaberDir 'Plugins/BSML.dll'))
$parse=$bsml.GetType('BeatSaberMarkupLanguage.Parse',$true)
$vectors=@('anchor-min','anchor-max','pivot','anchored-position','size-delta')
$floats=@('preferred-width','preferred-height','spacing','pad','pad-top','pad-bottom','font-size','min','max','increment')
$bools=@('child-control-height','child-control-width','child-expand-height','child-expand-width','word-wrapping','apply-on-change','bind-value','integer-only')
$checked=0
foreach($file in @('Editor.bsml','Gameplay.bsml')){
    [xml]$xml=Get-Content -LiteralPath (Join-Path $PSScriptRoot "../SaberWorkshop/$file") -Raw
    foreach($element in $xml.SelectNodes('//*')){
        foreach($attribute in $element.Attributes){
            $method=$null
            if($vectors -contains $attribute.Name){$method='Vector2'}
            elseif($floats -contains $attribute.Name){$method='Float'}
            elseif($bools -contains $attribute.Name){$method='Bool'}
            if($method -and !$attribute.Value.StartsWith('~')){
                try{$null=$parse.GetMethod($method).Invoke($null,[object[]]@($attribute.Value));$checked++}
                catch{throw "$file <$($element.Name)> $($attribute.Name)='$($attribute.Value)': $($_.Exception.Message)"}
            }
        }
    }
}
$rejected=$false
try{$null=$parse.GetMethod('Vector2').Invoke($null,[object[]]@('0,0'))}catch{$rejected=$true}
if(!$rejected){throw 'Expected invalid comma-separated BSML coordinates to be rejected'}
Write-Output "Installed BSML verified $checked markup values. Invalid coordinate regression rejected."
