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
        foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)){if(text.name!="BSMLText"){text.fontSize=3;text.fontSizeMin=2.6f;text.fontSizeMax=3;}text.enableWordWrapping=false;text.margin=Vector4.zero;}
        foreach(var setting in root.GetComponentsInChildren<IncDecSetting>(true))FillControl(setting.transform,setting.transform.Find("NameText") as RectTransform,setting.transform.Find("ValuePicker") as RectTransform);
        foreach(var setting in root.GetComponentsInChildren<StringSetting>(true))FillControl(setting.transform,setting.transform.Find("NameText") as RectTransform,setting.BoundingBox);
        foreach(var setting in root.GetComponentsInChildren<ColorSetting>(true))FillControl(setting.transform,setting.transform.Find("NameText") as RectTransform,setting.transform.Find("ValuePicker") as RectTransform);
        foreach(var setting in root.GetComponentsInChildren<GenericSliderSetting>(true))FillControl(setting.transform,setting.transform.Find("Title") as RectTransform,setting.Slider.transform as RectTransform);
        foreach(var setting in root.GetComponentsInChildren<DropDownListSetting>(true)){var parent=setting.transform.parent;var layout=parent.GetComponent<HorizontalOrVerticalLayoutGroup>();if(layout)layout!.enabled=false;FillControl(parent,parent.Find("Label") as RectTransform,setting.transform as RectTransform);}
        foreach(var setting in root.GetComponentsInChildren<ToggleSetting>(true))FillControl(setting.transform,setting.transform.Find("NameText") as RectTransform,setting.transform.Find("SwitchView") as RectTransform);
    }
    private static void FillControl(Transform parent,RectTransform? oldLabel,RectTransform? value){
        if(oldLabel)oldLabel!.gameObject.SetActive(false);if(!value)return;
        var element=parent.GetComponent<LayoutElement>();if(element){element!.minHeight=0;element.preferredHeight=6.4f;}
        value!.anchorMin=Vector2.zero;value.anchorMax=Vector2.one;value.offsetMin=new Vector2(1,0);value.offsetMax=new Vector2(-1,0);
    }
    private void SceneLoaded(Scene scene,LoadSceneMode mode){if(scene.name=="GameCore"||scene.name=="EmptyTransition")editor.CloseEditor();}
    public void Dispose(){SceneManager.sceneLoaded-=SceneLoaded;if(screen)Object.Destroy(screen!.gameObject);screen=null;}
}
