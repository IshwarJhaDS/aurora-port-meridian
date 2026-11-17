import { Vec3, Quat, Mat4, AABB, clamp, lerp, smoothstep } from './math.js';
import { FixedStepClock, ObjectPool, FrameArena } from './core.js';
import { RigidBody, PhysicsWorld, Ragdoll } from './physics.js';
import { RaycastVehicle } from './vehicle.js';
import { DeformableSurface } from './deformable.js';
import { WetnessField } from './resources.js';
import { MeshData } from './mesh.js';
import { Renderer } from './renderer.js';

export class DayNightCycle {
  constructor(hour=16.4){
    this.hour=hour;
    this.hoursPerSecond=.035;
    this.running=false;
    this.direction=new Vec3();
    this.color=new Vec3();
    this.intensity=0;
    this.update(0);
  }
  update(dt){
    if(this.running)this.hour=(this.hour+dt*this.hoursPerSecond)%24;
    const theta=(this.hour-6)/24*Math.PI*2;
    this.direction.set(Math.cos(theta)*.8,Math.sin(theta),Math.cos(theta)*.6).normalize();
    const day=smoothstep(-.08,.16,this.direction.y),warm=1-smoothstep(0,.5,this.direction.y);
    this.color.set(1,lerp(.96,.55,warm),lerp(.9,.3,warm));
    this.intensity=day*5.5;
  }
}

export class OrbitCamera {
  constructor(){
    this.target=new Vec3(0,1.3,0);
    this.distance=24;
    this.yaw=.65;
    this.pitch=.39;
    this.fov=Math.PI/3;
    this.eye=new Vec3();
    this.view=Mat4.identity();
    this.vp=Mat4.identity();
    this.inverseVP=Mat4.identity();
  }
  update(aspect){
    this.pitch=clamp(this.pitch,.08,1.35);
    this.distance=clamp(this.distance,4,80);
    this.eye.set(this.target.x+Math.sin(this.yaw)*Math.cos(this.pitch)*this.distance,this.target.y+Math.sin(this.pitch)*this.distance,this.target.z+Math.cos(this.yaw)*Math.cos(this.pitch)*this.distance);
    this.view=Mat4.lookAt(this.eye,this.target);
    this.vp=Mat4.mul(Mat4.perspective(this.fov,aspect,.15,250),this.view);
    this.inverseVP=Mat4.invert(this.vp);
  }
}

export class Engine {
  constructor(canvas){
    this.canvas=canvas;
    this.renderer=new Renderer(canvas);
    this.camera=new OrbitCamera();
    this.clock=new FixedStepClock();
    this.world=new PhysicsWorld();
    this.sun=new DayNightCycle();
    this.wet=new WetnessField();
    this.objects=[];
    this.nextObject=1;
    this.ragdolls=[];
    this.keys=new Set();
    this.paused=false;
    this.contextLost=false;
    this.running=false;
    this.disposers=[];
    this.physicsMs=0;
    this.frames=0;
    this.arena=new FrameArena(64*1024);
    this.projectiles=new ObjectPool(24,()=>({
      body:null,object:null,remaining:0
    }),v=>{
      v.body=null;
      v.object=null;
      v.remaining=0;
    });
    this.projectileHandles=[];
    this.box=MeshData.box();
    this.spheres=[{
      mesh:MeshData.sphere(24,12),error:0
    },{
      mesh:MeshData.sphere(12,6),error:.07
    },{
      mesh:MeshData.sphere(6,3),error:.24
    }];
    this.smallSpheres=[{
      mesh:MeshData.sphere(12,6),error:0
    },{
      mesh:MeshData.sphere(6,3),error:.1
    }];
    this.createLab();
    this.bindInput();
    this.onLost=e=>{
      e.preventDefault();
      this.contextLost=true;
      this.onStatus?.('Graphics context lost. Waiting for the browser to restore it.');
    };
    this.onRestored=()=>{
      try{
        this.renderer.dispose();
        this.renderer=new Renderer(canvas);
        this.contextLost=false;
        this.onStatus?.('Graphics context restored.');
      }catch(error){
        this.onError?.(error);
      }
    };
    canvas.addEventListener('webglcontextlost',this.onLost);
    canvas.addEventListener('webglcontextrestored',this.onRestored);
  }
  object({
    body=null,position=new Vec3(),rotation=new Quat(),scale=new Vec3(1,1,1),offset=new Vec3(),material={
    },lods=null,doubleSided=false,castShadow=true
  } = {}){
    const o={
      id:this.nextObject++,body,position,rotation,scale,offset,material:{
        color:[.3,.3,.3],...material
      },lods:lods||[{
        mesh:this.box,error:0
      }],doubleSided,castShadow,static:!body||!body.invMass,model:Mat4.identity(),bounds:new AABB()
    };
    this.objects.push(o);
    return o;
  }
  bodyVisual(body,color=[.2,.34,.45],material={
  }){
    return this.object({
      body,scale:body.shape==='sphere'?new Vec3(body.radius,body.radius,body.radius):body.half.clone(),lods:body.shape==='sphere'?this.spheres:null,material:{
        color,roughness:.45,...material
      }
    });
  }
  createLab(){
    this.object({
      position:new Vec3(0,-.25,0),scale:new Vec3(45,.25,45),material:{
        color:[.22,.25,.28],roughness:.88,textured:true,uvScale:30
      }
    });
    for(const x of [-7,7])for(const z of [-7,7]){
      const p=this.world.add(new RigidBody({
        mass:0,position:new Vec3(x,1.5,z),half:new Vec3(.35,1.5,.35)
      }));
      this.bodyVisual(p,[.14,.18,.22],{
        roughness:.3,metallic:.35
      });
      this.object({
        position:new Vec3(x,3.05,z),scale:new Vec3(.42,.035,.42),material:{
          color:[.12,.5,.6],emissive:[.4,1.8,2.4],metallic:.2
        }
      });
    }
    for(let i=0;i<4;i++)this.object({
      position:new Vec3(-4.8+i*3.2,.018,0),scale:new Vec3(.018,.012,12),material:{
        color:[.33,.4,.41],roughness:.75
      },castShadow:false
    });
    for(let i=0;i<3;i++){
      const pedestal=this.world.add(new RigidBody({
        mass:0,position:new Vec3(-4.6+i*2.25,.28,-5),half:new Vec3(.85,.28,.85)
      }));
      this.bodyVisual(pedestal,[.08,.1,.13],{
        roughness:.7
      });
      const sphere=this.world.add(new RigidBody({
        mass:0,shape:'sphere',radius:.67,position:new Vec3(-4.6+i*2.25,1.25,-5)
      }));
      this.bodyVisual(sphere,i===1?[.66,.24,.07]:i===2?[.08,.37,.5]:[.58,.62,.67],{
        metallic:i===0?1:i===1?.9:.1,roughness:.08+i*.22
      });
    }
    for(let j=0;j<3;j++)for(let i=0;i<2;i++){
      const b=this.world.add(new RigidBody({
        position:new Vec3(-5+i*1.05,.55+j*1.02,3),half:new Vec3(.47,.47,.47),mass:8
      }));
      this.bodyVisual(b,j===2?[.58,.22,.07]:[.24,.3,.37],{
        roughness:.5,metallic:.12
      });
    }
    this.vehicle=new RaycastVehicle(this.world,{
      position:new Vec3(3,1,3)
    });
    this.bodyVisual(this.vehicle.body,[.015,.09,.15],{
      roughness:.18,metallic:.78
    });
    this.object({
      body:this.vehicle.body,offset:new Vec3(0,.43,.28),scale:new Vec3(.68,.26,.85),material:{
        color:[.025,.055,.08],roughness:.08,metallic:.65
      }
    });
    for(const x of [-.63,.63])this.object({
      body:this.vehicle.body,offset:new Vec3(x,.03,-1.805),scale:new Vec3(.15,.07,.015),material:{
        color:[.7,.6,.35],emissive:[2.5,2,.9],roughness:.15
      }
    });
    for(const w of this.vehicle.wheels){
      const o=this.object({
        body:this.vehicle.body,scale:new Vec3(.16,w.radius,w.radius),lods:this.smallSpheres,material:{
          color:[.025,.03,.035],roughness:.8
        }
      });
      o.wheel=w;
    }
    this.surface=new DeformableSurface({
      width:3.4,depth:2.6,resolution:10,origin:new Vec3(5,3,-3)
    });
    this.surfaceMesh=MeshData.surface(this.surface);
    this.object({
      lods:[{
        mesh:this.surfaceMesh,error:0
      }],material:{
        color:[.055,.4,.42],roughness:.42,metallic:.15
      },doubleSided:true
    });
    for(const x of [3.3,6.7])for(const z of [-4.3,-1.7])this.object({
      position:new Vec3(x,1.5,z),scale:new Vec3(.035,1.5,.035),material:{
        color:[.2,.25,.28],metallic:.8,roughness:.3
      }
    });
    const ball=this.world.add(new RigidBody({
      shape:'sphere',radius:.42,mass:4,position:new Vec3(5,5,-3),restitution:.25
    }));
    this.bodyVisual(ball,[.7,.2,.04],{
      roughness:.22,metallic:.3
    });
    this.world.afterStep.add(dt=>this.surface.step(dt,this.world));
  }
  spawnRagdoll(){
    if(this.ragdolls.length>=3){
      const r=this.ragdolls.shift(),ids=new Set(r.bodies.map(b=>b.id));
      this.objects=this.objects.filter(o=>{
        if(!ids.has(o.body?.id))return true;
        this.renderer.lod.remove(o.id);
        return false;
      });
      r.dispose();
    }
    const r=new Ragdoll(this.world,new Vec3(0,5.5,1));
    this.ragdolls.push(r);
    for(const b of r.bodies)this.bodyVisual(b,[.54,.18,.065],{
      roughness:.55,metallic:.05
    });
    return r;
  }
  launch(){
    if(this.projectiles.used>=this.projectiles.capacity)return;
    this.camera.update(this.renderer.width/Math.max(1,this.renderer.height));
    const h=this.projectiles.acquire(),p=this.projectiles.get(h),direction=Vec3.sub(this.camera.target,this.camera.eye).normalize();
    p.body=this.world.add(new RigidBody({
      shape:'sphere',radius:.3,mass:8,position:this.camera.eye.clone().addScaled(direction,1.2),restitution:.3
    }));
    p.body.velocity.copy(direction.scale(22));
    p.object=this.bodyVisual(p.body,[.7,.33,.05],{
      roughness:.2,metallic:.6
    });
    p.remaining=9;
    this.projectileHandles.push(h);
  }
  resetVehicle(){
    const b=this.vehicle.body;
    b.position.set(3,1,3);
    b.previousPosition.copy(b.position);
    b.rotation.copy(new Quat());
    b.previousRotation.copy(b.rotation);
    b.velocity.set(0,0,0);
    b.angularVelocity.set(0,0,0);
    for(const w of this.vehicle.wheels){
      w.omega=0;
      w.compression=0;
    }
    this.world.updateBroadphase();
  }
  bindInput(){
    const canvas=this.canvas;
    let drag=null;
    const listen=(target,event,fn,options)=>{
      target.addEventListener(event,fn,options);
      this.disposers.push(()=>target.removeEventListener(event,fn,options));
    };
    listen(canvas,'pointerdown',e=>{
      canvas.focus();
      canvas.setPointerCapture(e.pointerId);
      drag={
        x:e.clientX,y:e.clientY,pan:e.button===2||e.shiftKey
      };
    });
    listen(canvas,'pointermove',e=>{
      if(!drag)return;
      const dx=e.clientX-drag.x,dy=e.clientY-drag.y;
      drag.x=e.clientX;
      drag.y=e.clientY;
      if(drag.pan){
        this.camera.target.x-=dx*.015;
        this.camera.target.z+=dy*.015;
      }else{
        this.camera.yaw-=dx*.006;
        this.camera.pitch+=dy*.005;
      }
    });
    for(const event of ['pointerup','pointercancel','lostpointercapture'])listen(canvas,event,()=>drag=null);
    listen(canvas,'contextmenu',e=>e.preventDefault());
    listen(canvas,'wheel',e=>{
      e.preventDefault();
      this.camera.distance*=Math.exp(e.deltaY*.001);
    },{
      passive:false
    });
    listen(window,'keydown',e=>{
      if(e.target instanceof HTMLInputElement||e.target instanceof HTMLSelectElement)return;
      if(['ArrowUp','ArrowDown','ArrowLeft','ArrowRight','Space'].includes(e.code))e.preventDefault();
      this.keys.add(e.code);
      if(e.code==='KeyF'&&!e.repeat)this.launch();
      if(e.code==='KeyR'&&!e.repeat)this.resetVehicle();
    });
    listen(window,'keyup',e=>this.keys.delete(e.code));
    listen(window,'blur',()=>this.keys.clear());
    listen(document,'visibilitychange',()=>{
      this.keys.clear();
      this.lastTime=null;
    });
  }
  fixedStep(dt){
    const k=this.keys;
    this.vehicle.setControls({
      throttle:(k.has('KeyW')||k.has('ArrowUp')?1:0)-(k.has('KeyS')||k.has('ArrowDown')?1:0),brake:k.has('KeyB')?1:0,steer:(k.has('KeyA')||k.has('ArrowLeft')?1:0)-(k.has('KeyD')||k.has('ArrowRight')?1:0),handbrake:k.has('Space')?1:0
    });
    this.vehicle.wetness=this.wet.sample(this.vehicle.body.position.x,this.vehicle.body.position.z);
    this.world.step(dt);
    for(let i=this.projectileHandles.length-1;i>=0;i--){
      const h=this.projectileHandles[i],p=this.projectiles.get(h);
      p.remaining-=dt;
      if(p.remaining<=0){
        this.world.remove(p.body);
        this.objects=this.objects.filter(o=>o!==p.object);
        this.renderer.lod.remove(p.object.id);
        this.projectiles.release(h);
        this.projectileHandles.splice(i,1);
      }
    }
  }
  updateObjects(alpha){
    this.surfaceMesh.updateSurface();
    for(const o of this.objects){
      let position=o.position,rotation=o.rotation;
      if(o.body){
        const b=o.body;
        position=b.previousPosition.clone().addScaled(Vec3.sub(b.position,b.previousPosition),alpha);
        rotation=Quat.slerp(b.previousRotation,b.rotation,alpha);
        position.add(rotation.rotate(o.offset));
      }
      if(o.wheel){
        position=this.vehicle.wheelPosition(o.wheel);
        const steering=o.wheel.steering?this.vehicle.steer*.5:0;
        rotation=Quat.mul(this.vehicle.body.rotation,Quat.mul(Quat.axisAngle(new Vec3(0,1,0),steering),Quat.axisAngle(new Vec3(1,0,0),o.wheel.angle)));
      }
      o.model=Mat4.compose(position,rotation,o.scale);
      o.bounds=new AABB();
      const local=o.lods[0].mesh.surface?new AABB():o.lods[0].mesh.bounds;
      if(o.lods[0].mesh.surface){
        const p=o.lods[0].mesh.positions;
        for(let i=0;i<p.length;i+=3)o.bounds.include(Vec3.from(p,i));
      } else for(const x of [local.min.x,local.max.x])for(const y of [local.min.y,local.max.y])for(const z of [local.min.z,local.max.z])o.bounds.include(Mat4.point(o.model,new Vec3(x,y,z)));
    }
  }
  frame(milliseconds){
    if(!this.running)return;
    const seconds=this.lastTime==null?0:Math.max(0,(milliseconds-this.lastTime)/1000);
    this.lastTime=milliseconds;
    try{
      if(!this.contextLost){
        this.arena.reset();
        const before=performance.now();
        const result=this.paused?{
          alpha:1,steps:0
        }
        :this.clock.advance(seconds,dt=>this.fixedStep(dt));
        this.physicsMs=performance.now()-before;
        if(!this.paused){
          this.sun.update(Math.min(seconds,.1));
          this.wet.step(Math.min(seconds,.1),Math.max(0,this.sun.direction.y));
        }
        this.updateObjects(result.alpha);
        this.renderer.render(this.objects,this.camera,this.sun,this.wet,this.clock.time,this.arena);
        this.frames++;
        this.onFrame?.(this,seconds);
      }
    } catch(error){
      this.running=false;
      this.onError?.(error);
      return;
    }
    this.raf=requestAnimationFrame(t=>this.frame(t));
  }
  start(){
    if(this.running)return;
    this.running=true;
    this.lastTime=null;
    this.raf=requestAnimationFrame(t=>this.frame(t));
  }
  stop(){
    this.running=false;
    cancelAnimationFrame(this.raf);
  }
  selfTest(){
    this.updateObjects(1);
    this.renderer.render(this.objects,this.camera,this.sun,this.wet,this.clock.time);
    const bytes=this.renderer.readback();
    let min=255,max=0,sum=0;
    for(let i=0;i<bytes.length;i+=4){
      const v=bytes[i]+bytes[i+1]+bytes[i+2];
      min=Math.min(min,v);
      max=Math.max(max,v);
      sum+=v;
    }
    const error=this.renderer.gl.getError();
    return {
      glError:error,renderedPixels:bytes.length/4,range:[min,max],mean:sum/(bytes.length/4*3),finitePhysics:this.world.bodies.every(b=>b.position.finite()&&b.velocity.finite()),...this.renderer.stats
    };
  }
  dispose(){
    this.stop();
    for(const fn of this.disposers)fn();
    this.canvas.removeEventListener('webglcontextlost',this.onLost);
    this.canvas.removeEventListener('webglcontextrestored',this.onRestored);
    this.vehicle.dispose();
    for(const r of this.ragdolls)r.dispose();
    this.renderer.dispose();
    this.world.beforeStep.clear();
    this.world.afterStep.clear();
  }
}
