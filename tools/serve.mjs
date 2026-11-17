import http from 'node:http';
import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import path from 'node:path';
const args=process.argv.slice(2),value=(flag,fallback)=>{
  const i=args.indexOf(flag);
  return i<0?fallback:args[i+1];
},port=Number(value('--port',process.env.PORT||3001)),host=value('--host','127.0.0.1'),root=process.cwd();
const types={
  '.html':'text/html; charset=utf-8','.js':'text/javascript; charset=utf-8','.mjs':'text/javascript; charset=utf-8','.css':'text/css; charset=utf-8','.json':'application/json; charset=utf-8','.png':'image/png','.md':'text/plain; charset=utf-8','.zip':'application/zip'
};
const server=http.createServer(async(req,res)=>{
  try{
    if(!['GET','HEAD'].includes(req.method)){
      res.writeHead(405);
      res.end();
      return;
    }
    const url=new URL(req.url,'http://localhost'),pathname=decodeURIComponent(url.pathname),file=path.resolve(root,`.${pathname==='/'?'/index.html':pathname}`);
    if(!file.startsWith(`${root}${path.sep}`)||pathname.split('/').some(s=>s.startsWith('.'))){
      res.writeHead(403);
      res.end('Forbidden');
      return;
    }
    const info=await stat(file);
    if(!info.isFile()){
      res.writeHead(404);
      res.end('Not found');
      return;
    }
    res.writeHead(200,{
      'Content-Type':types[path.extname(file)]||'application/octet-stream','Content-Length':info.size,'Cache-Control':'no-store','X-Content-Type-Options':'nosniff'
    });
    if(req.method==='HEAD')res.end();
    else createReadStream(file).on('error',()=>res.destroy()).pipe(res);
  }catch{
    res.writeHead(404);
    res.end('Not found');
  }
});
server.on('error',error=>{
  console.error(error.message);
  process.exitCode=1;
});
server.listen(port,host,()=>console.log(`Aurora / Port Meridian: http://${host}:${port}`));
for(const signal of ['SIGTERM','SIGINT'])process.on(signal,()=>server.close(()=>process.exit(0)));
