import { readdir } from 'node:fs/promises';
import { spawnSync } from 'node:child_process';
const paths=[];
async function walk(root){
  for(const e of await readdir(root,{
    withFileTypes:true
  })){
    const p=`${root}/${e.name}`;
    if(e.isDirectory()&&!e.name.startsWith('.'))await walk(p);
    else if(e.isFile()&&/\.(m?js)$/.test(e.name))paths.push(p);
  }
}
await walk('src');
await walk('tests');
await walk('tools');
for(const p of paths){
  const r=spawnSync(process.execPath,['--check',p],{
    encoding:'utf8'
  });
  if(r.status!==0){
    process.stderr.write(r.stderr);
    process.exit(r.status||1);
  }
}
console.log(`Syntax checked ${paths.length} JavaScript modules.`);
