using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SaberWorkshop;
using Object=UnityEngine.Object;

// Offline layout harness. Uses Editor.bsml and Unity's real UGUI layout engine.
// Game-specific BSML prefabs are represented by ordinary UGUI controls here.
public sealed class PreviewMenu : MonoBehaviour {
 public string Markup;
 public readonly Dictionary<string,object> Values=new Dictionary<string,object>();
 public readonly Dictionary<string,Slider> Sliders=new Dictionary<string,Slider>();
 public readonly Dictionary<string,Button> Actions=new Dictionary<string,Button>();
 public readonly Dictionary<string,GameObject> Modals=new Dictionary<string,GameObject>();
 readonly List<(Text text,string binding)> labels=new List<(Text,string)>();
 readonly List<(GameObject obj,string binding)> conditions=new List<(GameObject,string)>();
 RectTransform content;Font font;string inputTarget="";bool replaceNumber;
 GameObject sabers;Material sampleMaterial;int helpStep;readonly Color ink=new Color(.88f,.92f,.96f);
 void Start(){if(content==null&&!string.IsNullOrEmpty(Markup))Initialize(Markup);}
 public void Initialize(string markup){
  foreach(var child in transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
  Markup=markup;font=Font.CreateDynamicFontFromOSFont(WorkshopTheme.Current.FontFamily,34);
  var state=WorkshopState.Instance;state.CreateStarter();state.Project.Name="My saber";
  var defaults=new Dictionary<string,object>{{"EditorTitle","Saber Workshop"},{"ProjectName","My saber"},{"PartName","Handle"},{"PartHeight",230f},{"PartWidth",36f},{"PartDepth",36f},{"PreviewScale",1f},{"SaberPreviewArea",true},{"PreviewTitle","SABER PREVIEW"},{"MaterialPreviewForm","Sphere"},{"EffectTarget","1. Handle"},{"HasSelection",true},{"NoSelection",false},{"ShapeInspector",true},{"ColorInspector",false},{"TextureInspector",false},{"ShaderInspector",false},{"IsSolidPart",true},{"IsProfile",true},{"HasTint",true},{"HasGlow",true},{"IsCreatedPart",true},{"ColorSource","RGB"},{"CrossSection","Round"},{"ShapeTabLabel","Part"},{"ColorTabLabel","Material"},{"TextureTabLabel","Images"},{"ShaderTabLabel","Effects"},{"Status","Select a part. Drag a slider; tap it to enter a number."},{"NumberText","0"},{"NumberTitle","Width · mm"},{"NewPresetName","My saber"},{"StarterChoice","Straight"},{"NewPartType","Blade"},{"MaterialName","Workshop material"},{"MaterialChoice","Workshop material"},{"TextureSlotChoice","Main texture"},{"EffectName","My plasma"},{"Glow",1f},{"EffectScale",4f},{"EffectSpeed",.3f},{"EffectDepth",1f},{"EffectBrightness",1.5f},{"FlowDirection","Up"}};
  foreach(var pair in defaults)Values[pair.Key]=pair.Value;
  Values["EffectTargets"]=state.Project.Parts.Select((p,i)=>(i+1)+". "+p.Name).ToArray();Values["MaterialPreviewForms"]=new[]{"Sphere","Blade","Ring","Selected part"};Values["TextureFileOptions"]=new[]{"No image"};Values["PartTypes"]=new[]{"Blade","Handle","Ring","Particles","Trail"};Values["ColorSources"]=new[]{"RGB","Note colour"};Values["CrossSections"]=new[]{"Round","Flat","Square","Triangle"};Values["StarterOptions"]=new[]{"Straight","Segmented","Empty"};Values["ProjectOptions"]=new[]{"My saber","My second saber"};Values["MaterialOptions"]=new[]{"Workshop material"};Values["FlowDirections"]=new[]{"Up","Down","Across"};
  Build();
  sabers=state.InstantiatePair();sabers.transform.SetParent(transform,false);float scale=(WorkbenchLayout.Position.z/WorkbenchLayout.PreviewCenter(null).z)/WorkbenchLayout.Scale;sabers.transform.localScale=Vector3.one*scale;var meshes=sabers.GetComponentsInChildren<Renderer>();var bounds=meshes[0].bounds;foreach(var mesh in meshes.Skip(1))bounds.Encapsulate(mesh.bounds);sabers.transform.position=new Vector3(84,-5,-4)-bounds.center;
  WorkshopState.ApplyPlayerColor(sabers.transform.Find("LeftSaber").gameObject,new Color(.9f,.07f,.2f));WorkshopState.ApplyPlayerColor(sabers.transform.Find("RightSaber").gameObject,new Color(.06f,.75f,.95f));sabers.AddComponent<PreviewPresentation>().Apply();
 }
 public object Get(string key)=>Values.TryGetValue(key,out var value)?value:"";
 public float Number(string key)=>float.TryParse(Convert.ToString(Get(key),CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out var value)?value:0;
 public bool Flag(string key)=>Get(key) is bool value&&value;
 public void Set(string key,object value){Values[key]=value;Refresh();}
 void UpdateHelp(){Set("HelpTitle",(helpStep+1)+" / 7 · "+TutorialContent.Titles[helpStep]);Set("HelpBody",TutorialContent.English[helpStep]);Set("HelpNextText",helpStep==6?"Done":"Next");}
 void Build(){
  if(content)Object.DestroyImmediate(content.gameObject);labels.Clear();conditions.Clear();Sliders.Clear();Actions.Clear();Modals.Clear();
  content=Node("Editor markup",transform);content.sizeDelta=new Vector2(250,126);
  var frame=Create(XElement.Parse(Markup),content);Stretch(frame);Refresh();
 }
 public void Refresh(){
  foreach(var label in labels)if(label.text)label.text.text=Convert.ToString(Get(label.binding),CultureInfo.InvariantCulture)??"";
  foreach(var condition in conditions)if(condition.obj)condition.obj.SetActive(Flag(condition.binding));
 }
 public void Activate(string page){Values["SaberPreviewArea"]=page!="ShaderInspector";Values["PreviewTitle"]=page=="ShaderInspector"?"MATERIAL PREVIEW":"SABER PREVIEW";if(sabers)sabers.SetActive(page!="ShaderInspector");foreach(var key in new[]{"ShapeInspector","ColorInspector","TextureInspector","ShaderInspector"})Values[key]=key==page;Refresh();}
 static float N(XElement e,string key,float fallback=0)=>float.TryParse((string)e.Attribute(key),NumberStyles.Float,CultureInfo.InvariantCulture,out var n)?n:fallback;
 static bool B(XElement e,string key,bool fallback=false)=>bool.TryParse((string)e.Attribute(key),out var b)?b:fallback;
 static string A(XElement e,string key,string fallback="")=>(string)e.Attribute(key)??fallback;
 RectTransform Node(string name,Transform parent){var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;}
 void Layout(RectTransform rect,XElement e){
  var item=rect.gameObject.AddComponent<LayoutElement>();item.minWidth=0;item.minHeight=0;item.flexibleWidth=0;item.flexibleHeight=0;
  if(e.Attribute("preferred-width")!=null)item.preferredWidth=N(e,"preferred-width");else if(e.Name.LocalName=="list")item.preferredWidth=N(e,"list-width",58);else item.flexibleWidth=1;
  if(e.Attribute("preferred-height")!=null)item.preferredHeight=N(e,"preferred-height");
  var active=A(e,"active");if(active.StartsWith("~"))conditions.Add((rect.gameObject,active.Substring(1)));
 }
 Text Label(RectTransform rect,string text,float size=3.4f,TextAnchor align=TextAnchor.MiddleLeft){
  var label=rect.gameObject.AddComponent<Text>();label.font=font;label.fontSize=Mathf.RoundToInt(size*10);label.color=ink;label.alignment=align;label.supportRichText=true;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;label.raycastTarget=false;
  // Keep fractional font sizes using a ten-times text rectangle.
  var outer=rect;var child=Node("Glyphs",outer);child.anchorMin=Vector2.zero;child.anchorMax=Vector2.one;child.offsetMin=child.offsetMax=Vector2.zero;
  Object.DestroyImmediate(label);label=child.gameObject.AddComponent<Text>();label.font=font;label.fontSize=34;label.color=ink;label.alignment=align;label.raycastTarget=false;label.supportRichText=true;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
  child.localScale=Vector3.one*.1f;child.sizeDelta=new Vector2(outer.rect.width*9,outer.rect.height*9);child.pivot=Vector2.zero;child.anchorMin=child.anchorMax=Vector2.zero;child.anchoredPosition=Vector2.zero;
  var sizing=child.gameObject.AddComponent<PreviewTextSize>();sizing.Parent=outer;sizing.Label=label;sizing.FontSize=size;
  if(text.StartsWith("~")){labels.Add((label,text.Substring(1)));label.text=Convert.ToString(Get(text.Substring(1)));}else label.text=text;
  return label;
 }
 Image Fill(RectTransform rect,Color color){var image=rect.gameObject.AddComponent<Image>();image.color=color;return image;}
 Button MakeButton(RectTransform rect,string text,Action click){var image=Fill(rect,Color.white);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;var c=button.colors;c.normalColor=WorkshopTheme.ColorOf(WorkshopTheme.Current.Button);c.selectedColor=c.normalColor;c.highlightedColor=WorkshopTheme.ColorOf(WorkshopTheme.Current.Hover);c.pressedColor=WorkshopTheme.ColorOf(WorkshopTheme.Current.Pressed);button.colors=c;var inner=Node("Button label",rect);Stretch(inner,2,0);Label(inner,text,3.4f,TextAnchor.MiddleCenter);button.onClick.AddListener(()=>click());return button;}
 static void Stretch(RectTransform rect,float x=0,float y=0){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(x,y);rect.offsetMax=new Vector2(-x,-y);}
 RectTransform Create(XElement e,RectTransform parent){
  var tag=e.Name.LocalName;var rect=Node(A(e,"id",tag),parent);Layout(rect,e);
  if(tag=="vertical"||tag=="horizontal"){
   HorizontalOrVerticalLayoutGroup group=tag=="vertical"?(HorizontalOrVerticalLayoutGroup)rect.gameObject.AddComponent<VerticalLayoutGroup>():rect.gameObject.AddComponent<HorizontalLayoutGroup>();
   group.spacing=N(e,"spacing");group.childControlWidth=B(e,"child-control-width",true);group.childControlHeight=B(e,"child-control-height",true);group.childForceExpandWidth=B(e,"child-expand-width");group.childForceExpandHeight=B(e,"child-expand-height");group.childAlignment=TextAnchor.UpperLeft;
   int pad=(int)N(e,"pad");group.padding=new RectOffset(pad,pad,(int)N(e,"pad-top",pad),(int)N(e,"pad-bottom",pad));
   if(A(e,"vertical-fit")=="PreferredSize"){var fit=rect.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;}
   foreach(var child in e.Elements())Create(child,rect);
  }else if(tag=="text")Label(rect,A(e,"text"),N(e,"font-size",3.4f));
  else if(tag=="button"){string action=A(e,"on-click");var button=MakeButton(rect,A(e,"text"),()=>Action(action));Actions[action]=button;}
  else if(tag=="slider-setting"){
   string key=A(e,"value");Fill(rect,new Color(.085f,.12f,.17f));var slider=rect.gameObject.AddComponent<Slider>();slider.minValue=N(e,"min");slider.maxValue=N(e,"max",100);if(slider.maxValue<Number(key))slider.maxValue=Number(key)*1.2f;
   var bar=Node("Fill",rect);bar.anchorMin=Vector2.zero;bar.anchorMax=Vector2.one;bar.offsetMin=new Vector2(0,2.2f);bar.offsetMax=new Vector2(0,-2.2f);Fill(bar,new Color(.13f,.52f,.66f)).raycastTarget=false;slider.fillRect=bar;
   var thumb=Node("Thumb",rect);thumb.sizeDelta=new Vector2(1.2f,-2f);Fill(thumb,WorkshopTheme.ColorOf(WorkshopTheme.Current.Accent));slider.handleRect=thumb;slider.direction=Slider.Direction.LeftToRight;
   var valueRect=Node("Value",rect);Stretch(valueRect,2,0);var label=Label(valueRect,Number(key).ToString("0.##",CultureInfo.InvariantCulture),3.4f,TextAnchor.MiddleCenter);
   slider.SetValueWithoutNotify(Number(key));slider.onValueChanged.AddListener(v=>{float step=N(e,"increment",.1f);v=Mathf.Round(v/step)*step;Values[key]=v;label.text=v.ToString("0.##",CultureInfo.InvariantCulture);ResizePart(key,v);});
   rect.gameObject.AddComponent<PreviewTap>().Tap=()=>OpenNumber(key);Sliders[key]=slider;
  }else if(tag=="bool-setting"){
   string key=A(e,"value");Button button=null;button=MakeButton(rect,Flag(key)?"ON":"OFF",()=>{Set(key,!Flag(key));buttonRefresh();});
   void buttonRefresh(){button.GetComponentInChildren<Text>().text=Flag(key)?"ON":"OFF";}
  }else if(tag=="string-setting"){
   string key=A(e,"value");MakeButton(rect,Convert.ToString(Get(key))??"",()=>{Values["Status"]="Text input: "+key;Refresh();});
  }else if(tag=="dropdown-list-setting"){
   string key=A(e,"value");var choices=Get(A(e,"choices")) as string[]??new[]{"Workshop material"};int index=Math.Max(0,Array.IndexOf(choices,Convert.ToString(Get(key))));
   Button button=null;button=MakeButton(rect,choices[Math.Min(index,choices.Length-1)]+"  v",()=>{index=(index+1)%choices.Length;Set(key,choices[index]);button.GetComponentInChildren<Text>().text=choices[index]+"  v";});
  }else if(tag=="raw-image"){var image=rect.gameObject.AddComponent<RawImage>();sampleMaterial=WorkshopAssets.EffectMaterial();image.gameObject.AddComponent<MaterialThumbnail>().Show(sampleMaterial,null,"Sphere");}
  else if(tag=="color-setting")Fill(rect,new Color(.37f,.15f,.75f));
  else if(tag=="list"){
   var group=rect.gameObject.AddComponent<VerticalLayoutGroup>();group.childControlHeight=true;group.childForceExpandHeight=false;group.childControlWidth=true;group.childForceExpandWidth=true;group.spacing=1;
   foreach(var part in WorkshopState.Instance.Project.Parts){var cell=Node("Part",rect);var item=cell.gameObject.AddComponent<LayoutElement>();item.preferredHeight=5;var current=part;MakeButton(cell,part.Name,()=>{Set("PartName",current.Name);});}
  }else if(tag=="scroll-view"){
   var image=Fill(rect,new Color(.035f,.05f,.075f));rect.gameObject.AddComponent<Mask>().showMaskGraphic=true;var scroll=rect.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
   var body=Node("Content",rect);body.anchorMin=new Vector2(0,1);body.anchorMax=Vector2.one;body.pivot=new Vector2(.5f,1);body.sizeDelta=Vector2.zero;var fit=body.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;var layout=body.gameObject.AddComponent<VerticalLayoutGroup>();layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;
   foreach(var child in e.Elements())Create(child,body);scroll.content=body;scroll.viewport=rect;
   var rail=Node("Scrollbar",rect);rail.anchorMin=new Vector2(1,0);rail.anchorMax=Vector2.one;rail.sizeDelta=new Vector2(1.4f,-2);rail.anchoredPosition=new Vector2(-1.5f,0);Fill(rail,new Color(.1f,.15f,.2f));var scrollbar=rail.gameObject.AddComponent<Scrollbar>();var handle=Node("Handle",rail);Stretch(handle);Fill(handle,new Color(.32f,.58f,.7f));scrollbar.handleRect=handle;scrollbar.direction=Scrollbar.Direction.BottomToTop;scrollbar.value=1;scroll.verticalScrollbar=scrollbar;
  }else if(tag=="modal"){
   rect.GetComponent<LayoutElement>().ignoreLayout=true;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=Vector2.one*.5f;
   var sizes=A(e,"size-delta","90 80").Split(' ');rect.sizeDelta=new Vector2(float.Parse(sizes[0],CultureInfo.InvariantCulture),float.Parse(sizes[1],CultureInfo.InvariantCulture));Fill(rect,new Color(.045f,.07f,.11f));
   var body=Node("Modal body",rect);Stretch(body,2,2);var layout=body.gameObject.AddComponent<VerticalLayoutGroup>();layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;foreach(var child in e.Elements())Create(child,body);Modals[A(e,"id")]=rect.gameObject;rect.gameObject.SetActive(false);
  }
  return rect;
 }
 void ResizePart(string key,float value){var property=key=="EffectScale"?"_Scale":key=="EffectSpeed"?"_Speed":key=="EffectDepth"?"_Depth":key=="EffectBrightness"?"_Brightness":key=="EffectGlow"?"_Glow":"";if(property!=""&&sampleMaterial){sampleMaterial.SetFloat(property,value);return;}if(!sabers)return;var part=WorkshopState.Instance.Project.Parts[0];if(key=="PartHeight")part.HeightScale=value/230;else if(key=="PartWidth")part.WidthScale=value/36;else if(key=="PartDepth")part.DepthScale=value/36;else return;foreach(var id in sabers.GetComponentsInChildren<PartIdentity>()){if(id.Id!=part.Id)continue;var filter=id.GetComponent<MeshFilter>();if(filter){var old=filter.sharedMesh;filter.sharedMesh=WorkshopState.BuildMesh(part);Object.DestroyImmediate(old);}}}
 public void OpenNumber(string key){if(sabers)sabers.SetActive(false);inputTarget=key;Set("NumberTitle",key.StartsWith("Part")?key.Substring(4)+" · mm":key);Set("NumberText",Number(key).ToString("0.##",CultureInfo.InvariantCulture));replaceNumber=true;Modals["number-dialog"].SetActive(true);Modals["number-dialog"].transform.SetAsLastSibling();}
 public void Action(string action){
  if(action.StartsWith("Num")&&action.Length==4&&char.IsDigit(action[3])){var t=replaceNumber?"":Convert.ToString(Get("NumberText"));replaceNumber=false;Set("NumberText",t+action[3]);return;}
  switch(action){
   case "PlasmaEffect":sampleMaterial.SetFloat("_Mode",1);Set("HasEffect",true);break;case "SpaceEffect":sampleMaterial.SetFloat("_Mode",2);Set("HasEffect",true);break;
   case "ApplyEffectDraft":foreach(var id in sabers.GetComponentsInChildren<PartIdentity>())if(id.Id==WorkshopState.Instance.Project.Parts[0].Id)id.GetComponent<Renderer>().sharedMaterial=new Material(sampleMaterial);Activate("ShapeInspector");break;
   case "OpenView":Modals["view-dialog"].SetActive(true);break;case "CloseView":Modals["view-dialog"].SetActive(false);break;
   case "OpenHelp":if(sabers)sabers.SetActive(false);UpdateHelp();Modals["help-dialog"].SetActive(true);break;
   case "CloseHelp":Modals["help-dialog"].SetActive(false);if(sabers)sabers.SetActive(true);break;
   case "HelpNext":if(helpStep<6){helpStep++;UpdateHelp();}else Action("CloseHelp");break;
   case "HelpBack":if(helpStep>0){helpStep--;UpdateHelp();}break;
   case "HelpShow":Action("CloseHelp");Activate(helpStep==3?"ShaderInspector":"ShapeInspector");break;
   case "InspectShape":Activate("ShapeInspector");break;case "InspectColor":Activate("ColorInspector");break;case "InspectTexture":Activate("TextureInspector");break;case "ShaderWorkshop":Activate("ShaderInspector");break;
   case "NewPresetDialog":Modals["preset-dialog"].SetActive(true);break;case "OpenLibrary":Modals["library-dialog"].SetActive(true);break;case "AddPartDialog":Modals["part-dialog"].SetActive(true);break;
   case "CloseNumber":Modals["number-dialog"].SetActive(false);if(sabers)sabers.SetActive(true);break;
   case "ApplyNumber":if(float.TryParse(Convert.ToString(Get("NumberText")),NumberStyles.Float,CultureInfo.InvariantCulture,out var value)){Values[inputTarget]=value;if(Sliders.TryGetValue(inputTarget,out var slider))slider.value=value;Modals["number-dialog"].SetActive(false);if(sabers)sabers.SetActive(true);}break;
   case "NumClear":Set("NumberText","");replaceNumber=false;break;case "NumBack":var text=Convert.ToString(Get("NumberText"));Set("NumberText",text.Length>0?text.Substring(0,text.Length-1):"");break;
   case "NumSign":var number=Convert.ToString(Get("NumberText"));Set("NumberText",number.StartsWith("-")?number.Substring(1):"-"+number);replaceNumber=false;break;
   case "NumDot":Set("NumberText",(replaceNumber?"0":Get("NumberText"))+".");replaceNumber=false;break;
   case "ClosePresetDialog":Modals["preset-dialog"].SetActive(false);break;case "CloseLibrary":Modals["library-dialog"].SetActive(false);break;case "ClosePartDialog":Modals["part-dialog"].SetActive(false);break;
   default:Set("Status","Preview action: "+action);break;
  }
 }
}
