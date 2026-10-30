using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using HMUI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SaberWorkshop;

public sealed partial class EditorController {
    [UIComponent("number-dialog")] private ModalView numberDialog=null!;
    [UIComponent("library-dialog")] private ModalView libraryDialog=null!;
    [UIComponent("part-dialog")] private ModalView partDialog=null!;
    private SliderSetting? numericTarget;
    private readonly List<SliderSetting> sliders=new List<SliderSetting>();
    private string sliderSelection="";
    private bool replaceNumber;
    public string NumberTitle {get;private set;}="Value";
    public string NumberText {get;private set;}="0";
    public string NumberError {get;private set;}="";
    public string EditorTitle=>"Saber Workshop / "+state.Project.Name;
    public List<object> PartTypes {get;}=new List<object>{"Blade","Handle","Ring","Particles","Trail"};
    public string NewPartType {get;set;}="Blade";
    internal GameObject? PreviewForAudit=>preview;
    [UIAction("FormatWidth")] public string FormatWidth(float value)=>PartWidth.ToString("0.###",CultureInfo.InvariantCulture);
    [UIAction("FormatHeight")] public string FormatHeight(float value)=>PartHeight.ToString("0.###",CultureInfo.InvariantCulture);
    [UIAction("FormatDepth")] public string FormatDepth(float value)=>PartDepth.ToString("0.###",CultureInfo.InvariantCulture);
    [UIAction("FormatNumber")] public string FormatNumber(float value)=>value.ToString(Math.Abs(value)>=1000?"0":"0.##",CultureInfo.InvariantCulture);

    internal void WireSliders(GameObject root){
        sliders.Clear();
        foreach(var setting in root.GetComponentsInChildren<SliderSetting>(true)){
            sliders.Add(setting);
            var gesture=setting.Slider.gameObject.AddComponent<SliderTapInput>();
            gesture.Initialize(setting,OpenNumber);
        }
    }
    private void SyncSliderRanges(){
        string key=(Part?.Id??(Selected?PartKey():""))+":"+Property;
        bool reset=key!=sliderSelection;sliderSelection=key;
        foreach(var setting in sliders){
            if(!setting||setting.AssociatedValue==null)continue;
            var gesture=setting.Slider.GetComponent<SliderTapInput>();
            if(reset&&gesture){setting.Slider.minValue=gesture.InitialMin;setting.Slider.maxValue=gesture.InitialMax;}
            float value=Convert.ToSingle(setting.AssociatedValue.GetValue(),CultureInfo.InvariantCulture);
            ExpandSlider(setting,value);
        }
    }
    private static void ExpandSlider(SliderSetting setting,float value){
        if(float.IsNaN(value)||float.IsInfinity(value))return;
        if(value<setting.Slider.minValue)setting.Slider.minValue=value-Mathf.Max(.1f,Mathf.Abs(value)*.2f);
        if(value>setting.Slider.maxValue)setting.Slider.maxValue=value+Mathf.Max(.1f,Mathf.Abs(value)*.2f);
        setting.Slider.numberOfSteps=Mathf.Clamp(Mathf.RoundToInt((setting.Slider.maxValue-setting.Slider.minValue)/Mathf.Max(.0001f,setting.Increments))+1,2,20001);
    }
    private void OpenNumber(SliderSetting target){
        if(!numberDialog||target.AssociatedValue==null)return;
        numericTarget=target;NumberTitle=InputTitle(target.AssociatedValue.MemberName);
        NumberText=Convert.ToSingle(target.AssociatedValue.GetValue(),CultureInfo.InvariantCulture).ToString("0.###",CultureInfo.InvariantCulture);
        NumberError="";replaceNumber=true;NotifyNumber();
        if(preview)preview!.SetActive(false);
        numberDialog.Show(true,false);
    }
    private static string InputTitle(string name)=>name switch {
        "PartHeight"=>"Height · mm","PartWidth"=>"Width · mm","PartDepth"=>"Depth · mm",
        "PositionX"=>"Position X · mm","PositionY"=>"Position Y · mm","PositionZ"=>"Position Z · mm",
        "RotationX"=>"Rotation X · degrees","RotationY"=>"Rotation Y · degrees","RotationZ"=>"Rotation Z · degrees",
        "TrailWidth"=>"Trail width · mm","TrailTime"=>"Fade time · ms","TrailTaper"=>"Tail width · %",
        _=>System.Text.RegularExpressions.Regex.Replace(name,"([a-z])([A-Z])","$1 $2")
    };
    private void NotifyNumber(){NotifyPropertyChanged(nameof(NumberTitle));NotifyPropertyChanged(nameof(NumberText));NotifyPropertyChanged(nameof(NumberError));}
    private void Digit(string digit){if(replaceNumber){NumberText="";replaceNumber=false;}if(NumberText.Length<14)NumberText+=digit;NumberError="";NotifyNumber();}
    [UIAction("Num0")] public void Num0()=>Digit("0");[UIAction("Num1")] public void Num1()=>Digit("1");
    [UIAction("Num2")] public void Num2()=>Digit("2");[UIAction("Num3")] public void Num3()=>Digit("3");
    [UIAction("Num4")] public void Num4()=>Digit("4");[UIAction("Num5")] public void Num5()=>Digit("5");
    [UIAction("Num6")] public void Num6()=>Digit("6");[UIAction("Num7")] public void Num7()=>Digit("7");
    [UIAction("Num8")] public void Num8()=>Digit("8");[UIAction("Num9")] public void Num9()=>Digit("9");
    [UIAction("NumDot")] public void NumDot(){if(replaceNumber){NumberText="0";replaceNumber=false;}if(!NumberText.Contains("."))NumberText+=(NumberText==""?"0.":".");NotifyNumber();}
    [UIAction("NumSign")] public void NumSign(){replaceNumber=false;NumberText=NumberText.StartsWith("-")?NumberText.Substring(1):"-"+NumberText;NotifyNumber();}
    [UIAction("NumBack")] public void NumBack(){replaceNumber=false;if(NumberText.Length>0)NumberText=NumberText.Substring(0,NumberText.Length-1);NotifyNumber();}
    [UIAction("NumClear")] public void NumClear(){NumberText="";replaceNumber=false;NotifyNumber();}
    [UIAction("CloseNumber")] public void CloseNumber(){if(numberDialog)numberDialog.Hide(true);numericTarget=null;if(preview)preview!.SetActive(visible&&editorOpen&&!ShaderInspector);}
    [UIAction("ApplyNumber")] public void ApplyNumber(){
        if(!numericTarget||numericTarget!.AssociatedValue==null)return;
        if(!float.TryParse(NumberText,NumberStyles.Float,CultureInfo.InvariantCulture,out float value)||float.IsNaN(value)||float.IsInfinity(value)||Math.Abs(value)>100000){NumberError="Enter a valid number.";NotifyNumber();return;}
        Guard(()=>{var target=numericTarget!;bool old=refreshing;refreshing=true;try{ExpandSlider(target,value);}finally{refreshing=old;}target.AssociatedValue.SetValue(target.IsInt?(object)Mathf.RoundToInt(value):value);refreshing=true;try{target.ReceiveValue();}finally{refreshing=old;}CloseNumber();});
    }
    [UIAction("OpenLibrary")] public void OpenLibrary(){RefreshFiles();if(preview)preview!.SetActive(false);if(libraryDialog)libraryDialog.Show(true,false);}
    [UIAction("CloseLibrary")] public void CloseLibrary(){if(libraryDialog)libraryDialog.Hide(true);if(preview)preview!.SetActive(visible&&editorOpen&&!ShaderInspector);}
    [UIAction("AddPartDialog")] public void AddPartDialog(){if(preview)preview!.SetActive(false);if(partDialog)partDialog.Show(true,false);}
    [UIAction("ClosePartDialog")] public void ClosePartDialog(){if(partDialog)partDialog.Hide(true);if(preview)preview!.SetActive(visible&&editorOpen&&!ShaderInspector);}
    [UIAction("CreateChosenPart")] public void CreateChosenPart(){switch(NewPartType){case "Handle":AddHandle();break;case "Ring":AddRing();break;case "Particles":AddParticles();break;case "Trail":AddTrail();break;default:AddBlade();break;}ClosePartDialog();}
}

public sealed class SliderTapInput : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IDragHandler {
    private SliderSetting setting=null!;
    private Action<SliderSetting> tap=null!;
    private Vector2 downPosition;private Vector3 downWorld;private float downTime,stableValue,startValue;private bool held,moved;
    public float InitialMin {get;private set;}public float InitialMax {get;private set;}
    public void Initialize(SliderSetting source,Action<SliderSetting> onTap){setting=source;tap=onTap;InitialMin=source.Slider.minValue;InitialMax=source.Slider.maxValue;ReadStable();}
    private void ReadStable(){if(setting&&setting.AssociatedValue!=null)stableValue=Convert.ToSingle(setting.AssociatedValue.GetValue(),CultureInfo.InvariantCulture);}
    private void LateUpdate(){if(!held)ReadStable();}
    private bool Moved(PointerEventData e)=>(e.position-downPosition).sqrMagnitude>36||(downWorld!=Vector3.zero&&e.pointerCurrentRaycast.worldPosition!=Vector3.zero&&(e.pointerCurrentRaycast.worldPosition-downWorld).sqrMagnitude>.000036f);
    public void OnPointerDown(PointerEventData e){if(!setting.Interactable)return;held=true;moved=false;downTime=Time.unscaledTime;downPosition=e.position;downWorld=e.pointerCurrentRaycast.worldPosition;startValue=stableValue;}
    public void OnDrag(PointerEventData e){if(Moved(e))moved=true;}
    public void OnPointerUp(PointerEventData e){if(!held)return;held=false;if(moved||Time.unscaledTime-downTime>.3f||Moved(e))return;setting.AssociatedValue?.SetValue(startValue);tap(setting);}
    private void OnDisable(){held=false;}
}
