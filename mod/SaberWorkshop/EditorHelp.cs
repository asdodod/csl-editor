using BeatSaberMarkupLanguage.Attributes;
using HMUI;
namespace SaberWorkshop;
public sealed partial class EditorController {
 [UIComponent("help-dialog")] private ModalView helpDialog=null!;
 private int helpStep;
 public string HelpTitle=>$"{helpStep+1} / {TutorialContent.Titles.Length} · {TutorialContent.Titles[helpStep]}";
 public string HelpBody=>TutorialContent.English[helpStep];
 public string HelpNextText=>helpStep==TutorialContent.Titles.Length-1?"Done":"Next";
 private void NotifyHelp(){NotifyPropertyChanged(nameof(HelpTitle));NotifyPropertyChanged(nameof(HelpBody));NotifyPropertyChanged(nameof(HelpNextText));}
 [UIAction("OpenHelp")] public void OpenHelp(){NotifyHelp();if(preview)preview!.SetActive(false);if(helpDialog)helpDialog.Show(true,false);}
 [UIAction("CloseHelp")] public void CloseHelp(){if(helpDialog)helpDialog.Hide(true);if(preview)preview!.SetActive(visible&&editorOpen&&!ShaderInspector);}
 [UIAction("HelpNext")] public void HelpNext(){if(helpStep==TutorialContent.Titles.Length-1){CloseHelp();return;}helpStep++;NotifyHelp();}
 [UIAction("HelpBack")] public void HelpBack(){if(helpStep>0)helpStep--;NotifyHelp();}
 [UIAction("HelpShow")] public void HelpShow(){CloseHelp();switch(helpStep){case 0:NewPresetDialog();break;case 1:case 5:AddPartDialog();break;case 3:ShaderWorkshop();break;default:InspectShape();ShowShapeDetails=helpStep==4;ShowTransform=false;break;}}
}
