import { Engine } from './engine.js';
const $=id=>document.getElementById(id);
function fail(error){
  console.error(error);
  $('error').hidden=false;
  $('error-message').textContent=error.message;
  $('status').textContent='ENGINE STOPPED';
  document.body.classList.add('failed');
}
try{
  const engine=new Engine($('viewport'));
  window.engine=engine;
  engine.onError=fail;
  engine.onStatus=message=>$('notice').textContent=message;
  const set=(id,callback)=>$(id).addEventListener('input',event=>callback(event.target));
  set('hour',el=>{
    engine.sun.hour=Number(el.value);
    engine.sun.update(0);
  });
  set('cycle',el=>engine.sun.running=el.checked);
  set('rain',el=>{
    engine.wet.rain=Number(el.value);
    $('rain-value').textContent=`${Math.round(engine.wet.rain*100)}%`;
  });
  set('resolution',el=>engine.renderer.settings.scale=Number(el.value));
  set('exposure',el=>engine.renderer.settings.exposure=Number(el.value));
  set('shadows',el=>engine.renderer.settings.shadowMode=Number(el.value));
  set('debug',el=>engine.renderer.settings.debug=Number(el.value));
  for(const name of ['gi','fog','ssr'])set(name,el=>engine.renderer.settings[name]=el.checked);
  set('occlusion',el=>engine.renderer.enableOcclusion=el.checked);
  $('ragdoll').addEventListener('click',()=>engine.spawnRagdoll());
  $('launch').addEventListener('click',()=>engine.launch());
  $('reset-car').addEventListener('click',()=>engine.resetVehicle());
  $('pause').addEventListener('click',()=>{
    engine.paused=!engine.paused;
    $('pause').textContent=engine.paused?'Resume simulation':'Pause simulation';
    $('pause').setAttribute('aria-pressed',String(engine.paused));
  });
  $('reset-view').addEventListener('click',()=>{
    engine.camera.target.set(0,1.3,0);
    engine.camera.distance=24;
    engine.camera.yaw=.65;
    engine.camera.pitch=.39;
  });
  $('dismiss-error').addEventListener('click',()=>$('error').hidden=true);
  let frames=0,elapsed=0;
  engine.onFrame=(e,dt)=>{
    frames++;
    elapsed+=dt;
    if(elapsed<.4)return;
    const s=e.renderer.stats;
    $('fps').textContent=(frames/elapsed).toFixed(0);
    $('cpu').textContent=`${(s.cpuMs+e.physicsMs).toFixed(1)} ms`;
    $('gpu').textContent=s.gpuMs==null?'unavailable':`${s.gpuMs.toFixed(1)} ms`;
    $('triangles').textContent=s.triangles.toLocaleString();
    $('bodies').textContent=e.world.bodies.length;
    $('culling').textContent=`${s.culled} / ${s.objects}`;
    $('memory').textContent=`${(s.textureBytes/1024).toFixed(0)} KB`;
    $('wetness').textContent=`${Math.round(e.wet.mean*100)}%`;
    $('speed').textContent=`${Math.round(e.vehicle.body.velocity.length()*3.6)} km/h`;
    $('rpm').textContent=`${Math.round(e.vehicle.rpm)} rpm · ${e.vehicle.gear===0?'R':e.vehicle.gear-1}`;
    $('clock').textContent=`${String(Math.floor(e.sun.hour)).padStart(2,'0')}:${String(Math.floor(e.sun.hour%1*60)).padStart(2,'0')}`;
    $('hour-value').textContent=$('clock').textContent;
    if(e.sun.running)$('hour').value=e.sun.hour;
    $('resolution-label').textContent=`${s.width} × ${s.height}`;
    $('contact-count').textContent=e.world.contacts.length;
    frames=0;
    elapsed=0;
  };
  window.addEventListener('pagehide',()=>engine.dispose(),{
    once:true
  });
  engine.start();
}catch(error){
  fail(error);
}
