using BeatSaberMarkupLanguage.Attributes;
namespace SaberWorkshop;

public sealed partial class EditorController {
 public bool TopTapered=>IsProfile&&ProfileEnds.Tapered(Part!,true);
 public bool BottomTapered=>IsProfile&&ProfileEnds.Tapered(Part!,false);
 public float TopEndLength {get=>IsProfile?ProfileEnds.Length(Part!,true):0;set=>ChangeEndLength(true,value);}
 public float BottomEndLength {get=>IsProfile?ProfileEnds.Length(Part!,false):0;set=>ChangeEndLength(false,value);}
 void ChangeEndLength(bool top,float value){if(refreshing||!IsProfile)return;Guard(()=>{Checkpoint(top?"top-length":"bottom-length");ProfileEnds.SetLength(Part!,top,value);RebuildShape();});}
 void ChangeEnd(bool top,bool pointed){if(!IsProfile)return;Guard(()=>{Checkpoint(top?"top-shape":"bottom-shape");if(pointed)ProfileEnds.Point(Part!,top);else ProfileEnds.Flatten(Part!,top);RebuildShape();SetStatus((top?"Top":"Bottom")+" end changed. Other parts keep their own shape.");});}
 [UIAction("FlatTop")] public void FlatTop()=>ChangeEnd(true,false);
 [UIAction("PointTop")] public void PointTop()=>ChangeEnd(true,true);
 [UIAction("FlatBottom")] public void FlatBottom()=>ChangeEnd(false,false);
 [UIAction("PointBottom")] public void PointBottom()=>ChangeEnd(false,true);
 void NotifyEnds(){foreach(string key in new[]{nameof(TopTapered),nameof(BottomTapered),nameof(TopEndLength),nameof(BottomEndLength)})NotifyPropertyChanged(key);}
}
