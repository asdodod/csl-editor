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
  let release;try{release=await request(root+`/releases/tags/v${version}-alpha`);}catch(e){if(!e.message.startsWith('404'))throw e;release=await request(root+'/releases','POST',{tag_name:`v${version}-alpha`,target_commitish:'main',name:`Saber Workshop ${version} alpha · Beat Saber 1.40.8`,prerelease:true,body:"Saber Workshop 0.2.3 for Beat Saber 1.40.8.\n\nFixed the FPFC workbench appearing at the world origin/floor when no stereo or MainCamera-tagged camera existed. The editor now uses a static menu-space pose (0, 1.2, 2), a 250 × 126 canvas at scale 0.018, and curved canvas radius 120. No camera is required for placement. The white primitive handle is removed. Project/parts, shape, preview, material and texture columns remain visible together. Reset panel restores the static menu pose.\n\nThe root flat RectMask2D is disabled; individual scroll view masks still clip their content. The workbench is placed above the menu canvas sorting order, with dimensions and fields adjusted for its columns.\n\nValidation: native Unity test places the workbench without camera lookup, checks its world corners against the floor and verifies the preview position. Seven contracts pass; installed BSML parses 510 values. The new curved presentation still needs an in-game FPFC visual check.\n\nNative export from 0.2.2 remains unchanged: Unity Editor builds newly created profiles/rings with basic glow materials and Main texture. Exporting edited imported .saber is paused. Python and Unity Editor 2022.3 with a valid license are required for export; configure ExporterSetup/install-local.ps1 with BeatSaberDir, PythonExecutable and UnityEditor. No README, reference sabers or game DLLs are included."});}
  const name=`SaberWorkshop-${version}-BeatSaber-1.40.8.zip`;
  const existing=release.assets.find(a=>a.name===name);
  if(!existing){const bytes=fs.readFileSync(name);const r=await fetch(`https://uploads.github.com/repos/asdodod/csl-editor/releases/${release.id}/assets?name=${encodeURIComponent(name)}`,{method:'POST',headers:{...headers,'Content-Type':'application/zip'},body:bytes});if(!r.ok)throw Error('Upload failed: '+r.status);const asset=await r.json();console.log(JSON.stringify({release:release.html_url,download:asset.browser_download_url,size:asset.size}));}
  else console.log(JSON.stringify({release:release.html_url,download:existing.browser_download_url,size:existing.size}));
}else if(process.argv[2]==='status'){
  const runs=await request(root+'/actions/runs?per_page=2');console.log(JSON.stringify(runs.workflow_runs.map(r=>({status:r.status,conclusion:r.conclusion,url:r.html_url}))));
}else throw Error('Use release or status');
