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
  let release;try{release=await request(root+`/releases/tags/v${version}-alpha`);}catch(e){if(!e.message.startsWith('404'))throw e;release=await request(root+'/releases','POST',{tag_name:`v${version}-alpha`,target_commitish:'main',name:`Saber Workshop ${version} alpha · Beat Saber 1.40.8`,prerelease:true,body:"Standalone in-game saber editor for Beat Saber 1.40.8. Solo → Mods → Saber Workshop → Open editor.\n\n0.2.1 replaces the separate menu screen with a movable world-space workbench. Project/parts, shape, preview, materials and textures are visible side by side. Opening the editor keeps Solo in place. Grab the top handle to move the wall; Recenter returns it in front of the VR camera; Close hides it. The preview follows the wall and ignores hidden reserve meshes when centering. On a first launch a starter saber is created automatically.\n\nBuilt with no compiler warnings. Six BSML contracts pass and the installed BSML library parses 508 markup values. An in-game VR check of the new wall is still required.\n\nExport CSL requires the separately configured local Python helper included in ExporterSetup. No reference saber, game assemblies or Python runtime are bundled. See README for setup and current alpha limits."});}
  const name=`SaberWorkshop-${version}-BeatSaber-1.40.8.zip`;
  const existing=release.assets.find(a=>a.name===name);
  if(!existing){const bytes=fs.readFileSync(name);const r=await fetch(`https://uploads.github.com/repos/asdodod/csl-editor/releases/${release.id}/assets?name=${encodeURIComponent(name)}`,{method:'POST',headers:{...headers,'Content-Type':'application/zip'},body:bytes});if(!r.ok)throw Error('Upload failed: '+r.status);const asset=await r.json();console.log(JSON.stringify({release:release.html_url,download:asset.browser_download_url,size:asset.size}));}
  else console.log(JSON.stringify({release:release.html_url,download:existing.browser_download_url,size:existing.size}));
}else if(process.argv[2]==='status'){
  const runs=await request(root+'/actions/runs?per_page=2');console.log(JSON.stringify(runs.workflow_runs.map(r=>({status:r.status,conclusion:r.conclusion,url:r.html_url}))));
}else throw Error('Use release or status');
