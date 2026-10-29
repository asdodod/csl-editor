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
  let release;try{release=await request(root+`/releases/tags/v${version}-alpha`);}catch(e){if(!e.message.startsWith('404'))throw e;release=await request(root+'/releases','POST',{tag_name:`v${version}-alpha`,target_commitish:'main',name:`Saber Workshop ${version} alpha · Beat Saber 1.40.8`,prerelease:true,body:"Standalone in-game saber editor for Beat Saber 1.40.8. Solo → Mods (next to Base Game) → Saber Workshop → Open editor.\n\n0.2.0: part list and property inspector; ready-made blade/handle profiles; material creation and duplication with embedded textures; direct Color/Glow and Advanced properties; Undo/Redo; game note colours; preview centering using mesh bounds and VR camera; local CSL .saber exporter.\n\nExport CSL requires the separately configured Python helper in ExporterSetup. No reference saber, game assemblies or Python runtime are bundled. Exported materials must use shaders already present in the source/base bundle.\n\nValidation: six BSML contracts pass; installed BSML parses 477 markup values. Unity verifies source import, material/texture persistence, generated geometry and hand colours. Exported Flux, Zoog, MK1 TEK and Batonblade bundles load in Unity with valid active meshes. The new UI still needs an interactive VR check.\n\nThis is an alpha, not a complete Unity replacement. Shader code compilation, arbitrary vertex editing and trails/hit-event authoring are not implemented. See README for dependencies, exporter setup and limits."});}
  const name=`SaberWorkshop-${version}-BeatSaber-1.40.8.zip`;
  const existing=release.assets.find(a=>a.name===name);
  if(!existing){const bytes=fs.readFileSync(name);const r=await fetch(`https://uploads.github.com/repos/asdodod/csl-editor/releases/${release.id}/assets?name=${encodeURIComponent(name)}`,{method:'POST',headers:{...headers,'Content-Type':'application/zip'},body:bytes});if(!r.ok)throw Error('Upload failed: '+r.status);const asset=await r.json();console.log(JSON.stringify({release:release.html_url,download:asset.browser_download_url,size:asset.size}));}
  else console.log(JSON.stringify({release:release.html_url,download:existing.browser_download_url,size:existing.size}));
}else if(process.argv[2]==='status'){
  const runs=await request(root+'/actions/runs?per_page=2');console.log(JSON.stringify(runs.workflow_runs.map(r=>({status:r.status,conclusion:r.conclusion,url:r.html_url}))));
}else throw Error('Use release or status');
