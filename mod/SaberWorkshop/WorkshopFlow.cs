using System;
using System.Collections;
using System.Reflection;
using BeatSaberMarkupLanguage;
using HMUI;
using UnityEngine;
using UnityEngine.UI;

namespace SaberWorkshop;

public sealed class WorkshopFlow : FlowCoordinator
{
    public EditorController Editor=null!;
    public Action? Exit;
    private WorkshopView? view;
    protected override void DidActivate(bool firstActivation,bool addedToHierarchy,bool screenSystemEnabling)
    {
        if(firstActivation){SetTitle("Saber Workshop");showBackButton=true;view=BeatSaberUI.CreateViewController<WorkshopView>();view.Editor=Editor;}
        if(addedToHierarchy)ProvideInitialViewControllers(view);
        Editor.EditorOpened();
    }
    protected override void BackButtonWasPressed(ViewController topViewController){Editor.EditorClosed();Exit?.Invoke();}
}

public sealed class WorkshopView : ViewController
{
    public EditorController Editor=null!;
    protected override void DidActivate(bool firstActivation,bool addedToHierarchy,bool screenSystemEnabling)
    {
        if(!firstActivation)return;
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("SaberWorkshop.Editor.bsml"))
        using(var reader=new System.IO.StreamReader(stream!)){
            try{BSMLParser.Instance.Parse(reader.ReadToEnd(),gameObject,Editor);StartCoroutine(ReportLayout());}
            catch(Exception e){Plugin.Log.Error("Workshop UI failed: "+e);BSMLParser.Instance.Parse("<vertical preferred-width='100'><text text='Unable to open editor. Check the game log.' font-size='4'/></vertical>",gameObject);}
        }
    }
    private IEnumerator ReportLayout()
    {
        yield return null;Canvas.ForceUpdateCanvases();
        int count=0;foreach(var button in GetComponentsInChildren<Button>(false)){var rect=button.transform as RectTransform;if(rect&&rect!.rect.width>0&&rect.rect.height>0)count++;}
        Plugin.Log.Info("Workshop editor opened: "+count+" visible buttons; panel "+((RectTransform)transform).rect.size);
    }
}
