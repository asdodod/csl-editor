using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IPA;
using SiraUtil.Zenject;
using SiraUtil.Sabers;
using SiraUtil.Interfaces;
using BeatSaberMarkupLanguage.Settings;
using UnityEngine;
using Zenject;

namespace SaberWorkshop;

[Plugin(RuntimeOptions.SingleStartInit), NoEnableDisable]
public class Plugin
{
    internal static IPA.Logging.Logger Log = null!;
    [Init]
    public Plugin(IPA.Logging.Logger logger, Zenjector zenjector)
    {
        Log=logger;
        // Match the common legacy data assembly only if no other mod supplied it.
        if(!AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name=="CustomSaber")){
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("SaberWorkshop.CustomSaber.dll"))
            using(var memory=new MemoryStream()){stream!.CopyTo(memory);Assembly.Load(memory.ToArray());}
        }
        WorkshopState.EnsureFolders();
        zenjector.UseLogger(logger);
        zenjector.Install<WorkshopMenuInstaller>(Location.Menu);
        zenjector.Install<WorkshopPlayerInstaller>(Location.StandardPlayer);
    }
}

public class WorkshopMenuInstaller : Installer
{
    public override void InstallBindings()=>Container.BindInterfacesAndSelfTo<EditorController>().AsSingle();
}
public class WorkshopPlayerInstaller : Installer
{
    public override void InstallBindings(){
        if(WorkshopState.Instance.PlayEnabled&&WorkshopState.Instance.Ready)
            Container.Bind<SaberModelRegistration>().FromInstance(SaberModelRegistration.Create<WorkshopSaberController>(1000)).AsCached();
    }
}
public class WorkshopSaberController : SaberModelController, IPreSaberModelInit, IColorable
{
    [Inject] private ColorManager colorManager=null!;
    private GameObject? model;
    private Color color;
    public Color Color {get=>color;set{color=value;if(model)WorkshopState.ApplyPlayerColor(model!,value);}}
    public bool PreInit(Transform parent,Saber saber){
        transform.SetParent(parent,false);transform.localPosition=Vector3.zero;transform.localRotation=Quaternion.identity;
        model=WorkshopState.Instance.InstantiateHand(saber.saberType==SaberType.SaberA,transform);
        Color=colorManager.ColorForSaberType(saber.saberType);return false;
    }
}
