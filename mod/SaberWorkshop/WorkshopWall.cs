using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using TMPro;
using HMUI;
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
                ConfigureCurvedMasks(created.gameObject);
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
    private static void ConfigureCurvedMasks(GameObject root){
        foreach(var list in root.GetComponentsInChildren<CustomListTableData>(true)){var canvas=list.TableView.GetComponent<Canvas>();if(canvas)canvas!.overrideSorting=false;}
        var background=root.transform.Find("Background")?.GetComponent<ImageView>();
        if(background){background!.material=Utilities.ImageResources.NoGlowMat;background.color=new Color(.025f,.035f,.05f,1);}
        foreach(var flat in root.GetComponentsInChildren<RectMask2D>(true)){
            flat.enabled=false;if(flat.gameObject==root)continue;
            var image=flat.GetComponent<ImageView>()??flat.gameObject.AddComponent<ImageView>();
            image.sprite=Utilities.ImageResources.WhitePixel;image.material=Utilities.ImageResources.NoGlowMat;image.color=Color.white;image.raycastTarget=false;
            var stencil=flat.GetComponent<Mask>()??flat.gameObject.AddComponent<Mask>();stencil.showMaskGraphic=false;
        }
    }
    private static void CompactFields(GameObject root){
        foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)){
            text.fontSize=3;text.fontSizeMin=2.6f;text.fontSizeMax=3;text.enableWordWrapping=false;text.overflowMode=TextOverflowModes.Ellipsis;
        }
        foreach(var setting in root.GetComponentsInChildren<IncDecSetting>(true))StackField(setting.transform,setting.transform.Find("NameText") as RectTransform,setting.transform.Find("ValuePicker") as RectTransform);
        foreach(var setting in root.GetComponentsInChildren<StringSetting>(true))StackField(setting.transform,setting.transform.Find("NameText") as RectTransform,setting.BoundingBox);
        foreach(var setting in root.GetComponentsInChildren<ColorSetting>(true))StackField(setting.transform,setting.transform.Find("NameText") as RectTransform,setting.transform.Find("ValuePicker") as RectTransform);
        foreach(var setting in root.GetComponentsInChildren<GenericSliderSetting>(true))StackField(setting.transform,setting.transform.Find("Title") as RectTransform,setting.Slider.transform as RectTransform);
        foreach(var setting in root.GetComponentsInChildren<DropDownListSetting>(true)){
            var parent=setting.transform.parent;var label=parent.Find("Label") as RectTransform;if(!label)continue;
            var layout=parent.GetComponent<HorizontalOrVerticalLayoutGroup>();if(layout)layout!.enabled=false;
            StackField(parent,label,setting.transform as RectTransform);
        }
        foreach(var setting in root.GetComponentsInChildren<ToggleSetting>(true)){
            var label=setting.transform.Find("NameText") as RectTransform;if(!label)continue;label!.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;label.offsetMin=new Vector2(1,0);label.offsetMax=new Vector2(-17,0);
        }
    }
    private static void StackField(Transform parent,RectTransform? label,RectTransform? value){
        if(!label||!value)return;var element=parent.GetComponent<LayoutElement>();if(element)element!.preferredHeight=10;
        label!.anchorMin=new Vector2(0,1);label.anchorMax=Vector2.one;label.pivot=new Vector2(.5f,1);label.sizeDelta=new Vector2(-2,3.2f);label.anchoredPosition=Vector2.zero;
        value!.anchorMin=Vector2.zero;value.anchorMax=new Vector2(1,0);value.pivot=new Vector2(.5f,0);value.sizeDelta=new Vector2(-2,6.4f);value.anchoredPosition=Vector2.zero;
    }
    private void SceneLoaded(Scene scene,LoadSceneMode mode){if(scene.name=="GameCore"||scene.name=="EmptyTransition")editor.CloseEditor();}
    public void Dispose(){SceneManager.sceneLoaded-=SceneLoaded;if(screen)Object.Destroy(screen!.gameObject);screen=null;}
}
