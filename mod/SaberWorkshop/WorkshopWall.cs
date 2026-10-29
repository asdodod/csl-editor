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
            var created=FloatingScreen.CreateFloatingScreen(WorkbenchLayout.Size,false,WorkbenchLayout.Position,Quaternion.identity,WorkbenchLayout.Radius,true);
            try{
                created.name="Saber Workshop Wall";created.transform.localScale=Vector3.one*WorkbenchLayout.Scale;
                created.GetComponent<Canvas>().sortingOrder=10;
                // Only individual scroll views clip their contents. The curved wall is not a flat mask.
                created.GetComponent<RectMask2D>().enabled=false;
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
        WorkbenchLayout.Place(screen!.transform);
    }
    public void Hide(){if(screen)screen!.gameObject.SetActive(false);}
    private static void CompactFields(GameObject root){
        foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)){
            text.fontSize=3;text.fontSizeMin=2.6f;text.fontSizeMax=3;
            text.enableWordWrapping=false;text.overflowMode=TextOverflowModes.Ellipsis;
        }
        foreach(var setting in root.GetComponentsInChildren<IncDecSetting>(true)){
            var picker=setting.transform.Find("ValuePicker") as RectTransform;
            if(picker)picker!.sizeDelta=new Vector2(24,picker.sizeDelta.y);
            setting.TextMesh.fontSize=2.6f;setting.TextMesh.fontSizeMax=2.6f;setting.TextMesh.fontSizeMin=2.3f;
            LabelWidth(setting.transform.Find("NameText") as RectTransform,26);
        }
        foreach(var setting in root.GetComponentsInChildren<StringSetting>(true)){
            if(setting.BoundingBox)setting.BoundingBox.sizeDelta=new Vector2(28,setting.BoundingBox.sizeDelta.y);
            LabelWidth(setting.transform.Find("NameText") as RectTransform,30);
        }
        foreach(var setting in root.GetComponentsInChildren<GenericSliderSetting>(true)){
            var slider=setting.Slider.transform as RectTransform;if(slider)slider!.sizeDelta=new Vector2(25,0);
            LabelWidth(setting.transform.Find("Title") as RectTransform,27);
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
