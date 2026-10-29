using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.FloatingScreen;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace SaberWorkshop;

// A world-space workbench. It does not present a FlowCoordinator or replace Solo.
public sealed class WorkshopWall : IDisposable
{
    private readonly EditorController editor;
    private FloatingScreen? screen;
    public Transform? Transform=>screen?screen!.transform:null;

    public WorkshopWall(EditorController editor){this.editor=editor;SceneManager.sceneLoaded+=SceneLoaded;}
    public void Show(){
        if(!screen){
            var created=FloatingScreen.CreateFloatingScreen(new Vector2(330,126),true,Vector3.zero,Quaternion.identity,0,true);
            try{
                created.name="Saber Workshop Wall";created.transform.localScale=Vector3.one*.012f;
                created.HandleSide=FloatingScreen.Side.Top;
                using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("SaberWorkshop.Editor.bsml"))
                using(var reader=new StreamReader(stream!))BSMLParser.Instance.Parse(reader.ReadToEnd(),created.gameObject,editor);
                CompactFields(created.gameObject);
                screen=created;
            }catch{Object.Destroy(created.gameObject);throw;}
        }
        screen!.gameObject.SetActive(true);Recenter();Canvas.ForceUpdateCanvases();
        int count=screen.GetComponentsInChildren<Button>(false).Count(b=>((RectTransform)b.transform).rect.width>0&&((RectTransform)b.transform).rect.height>0);
        Plugin.Log.Info("Workshop wall opened: "+count+" visible buttons; "+screen.ScreenSize+" world scale "+screen.transform.lossyScale);
    }
    public void Recenter(){
        if(!screen)return;
        var camera=Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c=>c.isActiveAndEnabled&&c.stereoEnabled)??Camera.main;
        if(!camera)return;
        var yaw=Quaternion.Euler(0,camera.transform.eulerAngles.y,0);
        var position=camera.transform.position+yaw*new Vector3(0,-.1f,2.25f);position.y=Mathf.Max(1.35f,position.y);
        screen!.transform.SetPositionAndRotation(position,yaw);
    }
    public void Hide(){if(screen)screen!.gameObject.SetActive(false);}
    private static void CompactFields(GameObject root){
        foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)){
            text.fontSize=Mathf.Min(text.fontSize,3);text.fontSizeMax=Mathf.Min(text.fontSizeMax,3);
            text.enableWordWrapping=false;text.overflowMode=TextOverflowModes.Ellipsis;
        }
        foreach(var setting in root.GetComponentsInChildren<IncDecSetting>(true)){
            var picker=setting.transform.Find("ValuePicker") as RectTransform;
            if(picker)picker!.sizeDelta=new Vector2(32,picker.sizeDelta.y);
            LabelWidth(setting.transform.Find("NameText") as RectTransform,34);
        }
        foreach(var setting in root.GetComponentsInChildren<StringSetting>(true)){
            if(setting.BoundingBox)setting.BoundingBox.sizeDelta=new Vector2(36,setting.BoundingBox.sizeDelta.y);
            LabelWidth(setting.transform.Find("NameText") as RectTransform,38);
        }
        foreach(var setting in root.GetComponentsInChildren<GenericSliderSetting>(true)){
            var slider=setting.Slider.transform as RectTransform;if(slider)slider!.sizeDelta=new Vector2(32,0);
            LabelWidth(setting.transform.Find("Title") as RectTransform,34);
        }
        foreach(var setting in root.GetComponentsInChildren<DropDownListSetting>(true)){
            var parent=setting.transform.parent;var label=parent.Find("Label") as RectTransform;if(!label)continue;
            var layout=parent.GetComponent<HorizontalOrVerticalLayoutGroup>();if(layout)layout!.enabled=false;
            label!.anchorMin=Vector2.zero;label.anchorMax=new Vector2(.38f,1);label.offsetMin=Vector2.zero;label.offsetMax=new Vector2(-1,0);
            var dropdown=(RectTransform)setting.transform;dropdown.anchorMin=new Vector2(.38f,0);dropdown.anchorMax=Vector2.one;dropdown.offsetMin=Vector2.zero;dropdown.offsetMax=Vector2.zero;
        }
    }
    private static void LabelWidth(RectTransform? label,float right){if(!label)return;label!.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;label.offsetMin=new Vector2(1,0);label.offsetMax=new Vector2(-right,0);}
    private void SceneLoaded(Scene scene,LoadSceneMode mode){if(scene.name=="GameCore"||scene.name=="EmptyTransition")editor.CloseEditor();}
    public void Dispose(){SceneManager.sceneLoaded-=SceneLoaded;if(screen)Object.Destroy(screen!.gameObject);screen=null;}
}
