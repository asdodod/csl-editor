using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using BeatSaberMarkupLanguage.Attributes;

namespace SaberWorkshop;

public sealed partial class EditorController
{
    [UIAction("ExportCSL")]
    public async void ExportCSL(){await Run(async()=>{
        if(!state.Ready)throw new InvalidOperationException("Create or open a saber first.");
        if(!state.HasGeometry)throw new InvalidOperationException("Add a blade or handle before exporting.");
        WorkshopState.Validate(state.Project);
        var configPath=Path.Combine(WorkshopState.DataRoot,"exporter.json");if(!File.Exists(configPath))throw new InvalidOperationException("CSL exporter is not installed.");
        var config=JObject.Parse(File.ReadAllText(configPath));var python=(string?)config["PythonExecutable"];var script=(string?)config["Script"];
        if(string.IsNullOrEmpty(python)||string.IsNullOrEmpty(script)||!File.Exists(python)||!File.Exists(script))throw new InvalidOperationException("CSL exporter configuration is incomplete.");
        string folder=Path.Combine(WorkshopState.DataRoot,"ExportJobs");Directory.CreateDirectory(folder);string input=Path.Combine(folder,Guid.NewGuid().ToString("N")+".json");
        var meshes=state.Project.Parts.Select(p=>new{Part=p,Mesh=p.Particles!=null||p.Trail!=null?null:ExportMesh(p)}).ToArray();
        File.WriteAllText(input,JsonConvert.SerializeObject(new{Project=state.Project,Parts=meshes}),new UTF8Encoding(false));SetStatus("Exporting CSL .saber...");
        var start=new System.Diagnostics.ProcessStartInfo{FileName=python,Arguments=Quote(script!)+" --config "+Quote(configPath)+" --input "+Quote(input),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
        using(var process=new System.Diagnostics.Process{StartInfo=start}){
            process.Start();var outputTask=process.StandardOutput.ReadToEndAsync();var errorsTask=process.StandardError.ReadToEndAsync();await Task.Run(()=>process.WaitForExit());string output=await outputTask;string errors=await errorsTask;
            var line=output.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).LastOrDefault()??"{}";var result=JObject.Parse(line);
            if(process.ExitCode!=0||(bool?)result["ok"]!=true){Plugin.Log.Error("CSL export: "+output+errors);throw new InvalidOperationException((string?)result["error"]??"CSL export failed. Check the game log.");}
            SetStatus("CSL exported: Workshop/"+Path.GetFileName((string?)result["file"]));Plugin.Log.Info("CSL export verified: "+line);
        }
    });}
    private static MeshData ExportMesh(ProfilePart p){var mesh=WorkshopState.BuildMesh(p);try{return MeshData.Capture(mesh);}finally{OwnedAssets.Release(mesh);}}
    private static string Quote(string value){if(value.Contains('"'))throw new InvalidDataException("Invalid exporter path");return "\""+value.Replace("\\","/")+"\"";}
}
