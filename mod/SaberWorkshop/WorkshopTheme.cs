using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
namespace SaberWorkshop;
public sealed class WorkshopTheme {
 public float[] Background={.025f,.035f,.05f,1},Button={.12f,.17f,.23f,1},Hover={.24f,.38f,.48f,1},Pressed={.1f,.28f,.37f,1},Accent={.38f,.86f,.92f,1},Text={.88f,.92f,.96f,1};
 public string FontFamily="Arial";public float FontSize=3.4f;
 private static WorkshopTheme? current;
 public static WorkshopTheme Current {get{if(current==null){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("SaberWorkshop.workshop-theme.json"))using(var reader=new StreamReader(stream!))current=JsonConvert.DeserializeObject<WorkshopTheme>(reader.ReadToEnd())!;}return current;}}
 public static Color ColorOf(float[] c)=>new Color(c[0],c[1],c[2],c[3]);
}
