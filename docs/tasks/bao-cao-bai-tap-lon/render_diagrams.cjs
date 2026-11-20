const fs=require('node:fs');const path=require('node:path');
const deps='C:/Program Files/WindowsApps/OpenAI.CodexPrimaryRuntime.v26-1105-1297-0_26.1105.1297.0_x64__3k8sg7r9htsxt/dependencies/node/node_modules';
const {instance}=require(deps+'/@viz-js/viz/dist/viz.cjs');const sharp=require(deps+'/sharp');
(async()=>{const viz=await instance();const out=path.join(__dirname,'revisions/20261110-content-models/diagrams');const manifest=JSON.parse(fs.readFileSync(path.join(out,'manifest.json'),'utf8'));let summary=[];
for(const [name,info] of Object.entries(manifest)){
 let svg=viz.renderString(fs.readFileSync(path.join(out,name+'.dot'),'utf8'),{format:'svg',engine:info.engine});
 fs.writeFileSync(path.join(out,name+'.svg'),svg);
 const s=svg.match(/viewBox="([\d.]+) ([\d.]+) ([\d.]+) ([\d.]+)"/);const w=+s[3],h=+s[4];
 await sharp(Buffer.from(svg)).resize({width:2200}).png().toFile(path.join(out,name+'.png'));
 summary.push({name,w,h,fontAt15cm:Math.round(20*439.4/w*10)/10,heightCm:Math.round(h/w*15.5*10)/10});
}fs.writeFileSync(path.join(out,'render-metrics.json'),JSON.stringify(summary,null,2));console.log(JSON.stringify(summary));})();
