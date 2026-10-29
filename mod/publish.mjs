import fs from 'node:fs';
import {spawnSync} from 'node:child_process';
const c=spawnSync('git',['credential','fill'],{input:'protocol=https\nhost=github.com\n\n',encoding:'utf8',windowsHide:true,env:{...process.env,GCM_INTERACTIVE:'never',GIT_TERMINAL_PROMPT:'0'}});
const auth=Object.fromEntries(c.stdout.split(/\r?\n/).filter(x=>x.includes('=')).map(x=>[x.slice(0,x.indexOf('=')),x.slice(x.indexOf('=')+1)]));
if(!auth.password)throw Error('No GitHub credential');
const headers={Authorization:`Bearer ${auth.password}`,Accept:'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28','User-Agent':'SaberWorkshop-Publisher'};
async function request(path,method='GET',body){const r=await fetch('https://api.github.com'+path,{headers,method,body:body?JSON.stringify(body):undefined});const j=await r.json();if(!r.ok)throw Error(`${r.status}: ${j.message}`);return j;}
const user=await request('/user');if(user.login!=='asdodod')throw Error('Wrong GitHub account');
const root='/repos/asdodod/csl-editor';
const version=JSON.parse(fs.readFileSync('mod/SaberWorkshop/manifest.json','utf8')).version;
if(process.argv[2]==='release'){
  await request(root,'PATCH',{description:'Saber Workshop — standalone in-game saber modeller, shader property and texture editor for Beat Saber',homepage:''});
  let release;try{release=await request(root+`/releases/tags/v${version}-alpha`);}catch(e){if(!e.message.startsWith('404'))throw e;release=await request(root+'/releases','POST',{tag_name:`v${version}-alpha`,target_commitish:'main',name:`Saber Workshop ${version} alpha · Beat Saber 1.40.8`,prerelease:true,body:"Recovery release for Beat Saber 1.40.8.\n\nA reported native crash occurred while CSL was loading a binary-repacked Flux export. The binary repacker has been removed. New procedural sabers now export through Unity Editor BuildPipeline.BuildAssetBundles with an independent shader, geometry and SaberDescriptor. No original reference saber or game assemblies are included in the release.\n\nCurrent native export supports newly created profiles/rings, basic glow/colour materials and Main texture. Exporting edited imported .saber files is paused; their Workshop projects can still be saved and used through Workshop. Unity Editor 2022.3, a valid Unity license and Python are required for export. Run ExporterSetup/install-local.ps1 with BeatSaberDir, PythonExecutable and UnityEditor paths to configure the helper. Setup copies the game's Newtonsoft dependency only into your local Editor project.\n\nThe replacement Flux built from the affected project passes descriptor, component and mesh checks and a GPU render test in Unity. This is not yet a complete in-game CSL verification. Seven contracts pass and installed BSML parses 508 markup values.\n\nREADME files have been removed from the repository and package. Licenses remain included. Before restarting after an old export crash, move the affected Flux.saber files out of CustomSabers; source projects should be kept."});}
  const name=`SaberWorkshop-${version}-BeatSaber-1.40.8.zip`;
  const existing=release.assets.find(a=>a.name===name);
  if(!existing){const bytes=fs.readFileSync(name);const r=await fetch(`https://uploads.github.com/repos/asdodod/csl-editor/releases/${release.id}/assets?name=${encodeURIComponent(name)}`,{method:'POST',headers:{...headers,'Content-Type':'application/zip'},body:bytes});if(!r.ok)throw Error('Upload failed: '+r.status);const asset=await r.json();console.log(JSON.stringify({release:release.html_url,download:asset.browser_download_url,size:asset.size}));}
  else console.log(JSON.stringify({release:release.html_url,download:existing.browser_download_url,size:existing.size}));
}else if(process.argv[2]==='status'){
  const runs=await request(root+'/actions/runs?per_page=2');console.log(JSON.stringify(runs.workflow_runs.map(r=>({status:r.status,conclusion:r.conclusion,url:r.html_url}))));
}else throw Error('Use release or status');
