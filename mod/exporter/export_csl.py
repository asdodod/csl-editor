"""Use the Unity Editor's native AssetBundle builder, without patching serialized objects."""
import argparse,json,subprocess,sys,tempfile,shutil,uuid
from pathlib import Path

def main():
 p=argparse.ArgumentParser();p.add_argument('--config',required=True);p.add_argument('--input',required=True);args=p.parse_args()
 config=json.loads(Path(args.config).read_text(encoding='utf-8-sig'));snapshot=json.loads(Path(args.input).read_text(encoding='utf-8-sig'))
 if snapshot['Project'].get('SourceFile'):raise ValueError('Imported .saber export is paused after a CSL crash. Save the Workshop project.')
 editor=Path(config.get('UnityEditor',''));project=Path(config.get('UnityProject',''))
 if not editor.is_file() or not (project/'Assets/Editor/BuildWorkshopCSL.cs').is_file():raise ValueError('Native Unity exporter is not configured.')
 job=project/'ExportJobs'/uuid.uuid4().hex;job.mkdir(parents=True);log=job/'build.log'
 result=subprocess.run([str(editor),'-batchmode','-nographics','-quit','-projectPath',str(project),'-executeMethod','BuildWorkshopCSL.Run','-workshopInput',str(Path(args.input).resolve()),'-workshopOutput',str(job),'-logFile',str(log)],creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
 report=log.read_text(encoding='utf-8-sig',errors='replace') if log.exists() else ''
 if result.returncode or 'WORKSHOP_NATIVE_EXPORT_COMPLETE' not in report:
  errors=[x.strip() for x in report.splitlines() if 'WORKSHOP_NATIVE_EXPORT_FAILED' in x or 'error CS' in x]
  raise ValueError((errors[0] if errors else 'Unity build failed')+'; log: '+str(log))
 name=''.join(c for c in snapshot['Project']['Name'] if c not in '<>:"/\\|?*' and ord(c)>=32).strip(' .')[:80] or 'Workshop Saber'
 folder=Path(config['CustomSabers'])/'Workshop';folder.mkdir(parents=True,exist_ok=True);output=folder/(name+'.saber');i=2
 while output.exists():output=folder/(name+f' ({i}).saber');i+=1
 shutil.copyfile(job/'workshop.saber',output)
 print(json.dumps({'ok':True,'file':str(output),'bytes':output.stat().st_size}))
if __name__=='__main__':
 try:main()
 except Exception as e:print(json.dumps({'ok':False,'error':str(e)}));sys.exit(1)
