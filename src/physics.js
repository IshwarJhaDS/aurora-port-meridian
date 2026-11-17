import { Vec3, Quat, AABB, EPS, clamp } from './math.js';
import { BVH } from './spatial.js';
let nextBodyId=1;
const UP=new Vec3(0,1,0),AXES=[new Vec3(1,0,0),UP,new Vec3(0,0,1)];

export class RigidBody {
  constructor({
    shape='box',half=new Vec3(.5,.5,.5),radius=.5,mass=1,position=new Vec3(),rotation=new Quat(),friction=.65,restitution=.12
  } = {}) {
    if(!['box','sphere'].includes(shape)||mass<0||radius<=0||Math.min(half.x,half.y,half.z)<=0)throw new RangeError('Invalid rigid body');
    this.id=nextBodyId++;
    this.shape=shape;
    this.half=half.clone();
    this.radius=radius;
    this.mass=mass;
    this.invMass=mass>0?1/mass:0;
    this.position=position.clone();
    this.rotation=rotation.clone().normalize();
    this.previousPosition=this.position.clone();
    this.previousRotation=this.rotation.clone();
    this.velocity=new Vec3();
    this.angularVelocity=new Vec3();
    this.force=new Vec3();
    this.torque=new Vec3();
    this.friction=friction;
    this.restitution=restitution;
    this.linearDamping=.04;
    this.angularDamping=.18;
    this.inverseInertia=new Vec3();
    if(mass>0){
      if(shape==='sphere')this.inverseInertia.set(2.5/(mass*radius*radius),2.5/(mass*radius*radius),2.5/(mass*radius*radius));
      else this.inverseInertia.set(3/(mass*(half.y**2+half.z**2)),3/(mass*(half.x**2+half.z**2)),3/(mass*(half.x**2+half.y**2)));
    }
    this.bounds=new AABB();
    this.updateBounds();
  }
  axes(){
    return AXES.map(a=>this.rotation.rotate(a));
  }
  localToWorld(v){
    return this.rotation.rotate(v).add(this.position);
  }
  worldToLocal(v){
    return this.rotation.conjugate().rotate(Vec3.sub(v,this.position));
  }
  inertiaMul(v){
    const local=this.rotation.conjugate().rotate(v);
    local.x*=this.inverseInertia.x;
    local.y*=this.inverseInertia.y;
    local.z*=this.inverseInertia.z;
    return this.rotation.rotate(local);
  }
  inertiaTensorMul(v){
    if(!this.invMass)return new Vec3();
    const local=this.rotation.conjugate().rotate(v);
    local.x/=this.inverseInertia.x;
    local.y/=this.inverseInertia.y;
    local.z/=this.inverseInertia.z;
    return this.rotation.rotate(local);
  }
  pointVelocity(p){
    return this.velocity.clone().add(Vec3.cross(this.angularVelocity,Vec3.sub(p,this.position)));
  }
  applyForce(f,p=this.position){
    if(!this.invMass)return;
    this.force.add(f);
    this.torque.add(Vec3.cross(Vec3.sub(p,this.position),f));
  }
  impulse(j,p=this.position){
    if(!this.invMass)return;
    this.velocity.addScaled(j,this.invMass);
    this.angularVelocity.add(this.inertiaMul(Vec3.cross(Vec3.sub(p,this.position),j)));
  }
  vertices(){
    const out=[];
    for(const x of [-1,1])for(const y of [-1,1])for(const z of [-1,1])out.push(this.localToWorld(new Vec3(x*this.half.x,y*this.half.y,z*this.half.z)));
    return out;
  }
  support(n){
    if(this.shape==='sphere')return this.position.clone().addScaled(n,this.radius);
    const l=this.rotation.conjugate().rotate(n),h=this.half;
    return this.localToWorld(new Vec3(Math.abs(l.x)<EPS?0:Math.sign(l.x)*h.x,Math.abs(l.y)<EPS?0:Math.sign(l.y)*h.y,Math.abs(l.z)<EPS?0:Math.sign(l.z)*h.z));
  }
  updateBounds(){
    if(this.shape==='sphere'){
      const r=new Vec3(this.radius,this.radius,this.radius);
      this.bounds=new AABB(Vec3.sub(this.position,r),Vec3.add(this.position,r));
    }else{
      this.bounds=new AABB();
      for(const p of this.vertices())this.bounds.include(p);
    }
    return this.bounds;
  }
  raycast(origin,dir,max=Infinity){
    if(this.shape==='sphere'){
      const o=Vec3.sub(origin,this.position),a=dir.dot(dir),b=o.dot(dir),c=o.dot(o)-this.radius*this.radius,disc=b*b-a*c;
      if(disc<0||a<EPS)return null;
      let t=(-b-Math.sqrt(disc))/a;
      if(t<1e-5)t=(-b+Math.sqrt(disc))/a;
      if(t<1e-5||t>max)return null;
      const point=origin.clone().addScaled(dir,t);
      return {
        distance:t,point,normal:Vec3.sub(point,this.position).normalize(),body:this
      };
    }
    const o=this.worldToLocal(origin),d=this.rotation.conjugate().rotate(dir);
    let near=-Infinity,far=Infinity,nearN=new Vec3(),farN=new Vec3();
    for(const k of ['x','y','z']){
      if(Math.abs(d[k])<EPS){
        if(Math.abs(o[k])>this.half[k])return null;
        continue;
      }
      let a=(-this.half[k]-o[k])/d[k],b=(this.half[k]-o[k])/d[k],na=new Vec3(),nb=new Vec3();
      na[k]=-1;
      nb[k]=1;
      if(a>b){
        [a,b]=[b,a];
        [na,nb]=[nb,na];
      }
      if(a>near){
        near=a;
        nearN=na;
      }
      if(b<far){
        far=b;
        farN=nb;
      }
      if(near>far)return null;
    }
    const inside=near<1e-5,t=inside?far:near;
    if(t<1e-5||t>max)return null;
    return {
      distance:t,point:origin.clone().addScaled(dir,t),normal:this.rotation.rotate(inside?farN:nearN),body:this
    };
  }
}
function clipPolygon(poly,center,normal,limit){
  const out=[];
  for(let i=0;i<poly.length;i++){
    const a=poly[i],b=poly[(i+1)%poly.length],da=Vec3.sub(a,center).dot(normal)-limit,db=Vec3.sub(b,center).dot(normal)-limit;
    if(da<=0)out.push(a);
    if((da<0&&db>0)||(da>0&&db<0))out.push(a.clone().addScaled(Vec3.sub(b,a),da/(da-db)));
  }
  return out;
}
function faceVertices(body,axis,sign){
  const h=[body.half.x,body.half.y,body.half.z],other=[0,1,2].filter(i=>i!==axis),out=[];
  for(const [u,v]of [[-1,-1],[1,-1],[1,1],[-1,1]]){
    const p=[0,0,0];
    p[axis]=sign*h[axis];
    p[other[0]]=u*h[other[0]];
    p[other[1]]=v*h[other[1]];
    out.push(body.localToWorld(Vec3.from(p)));
  }
  return out;
}
export function collide(a,b){
  const contact=(normal,point,depth)=>({
    a,b,normal,point,depth,jn:0,jt1:0,jt2:0
  });
  if(a.shape==='sphere'&&b.shape==='sphere'){
    const n=Vec3.sub(b.position,a.position),d=n.length(),depth=a.radius+b.radius-d;
    if(depth<=0)return [];
    if(d<EPS)n.copy(UP);
    else n.scale(1/d);
    return [contact(n,a.position.clone().addScaled(n,a.radius-depth*.5),depth)];
  }
  if(a.shape==='sphere'||b.shape==='sphere'){
    const sphere=a.shape==='sphere'?a:b,box=sphere===a?b:a,l=box.worldToLocal(sphere.position),q=new Vec3(clamp(l.x,-box.half.x,box.half.x),clamp(l.y,-box.half.y,box.half.y),clamp(l.z,-box.half.z,box.half.z));
    const delta=Vec3.sub(l,q);
    let dist=delta.length(),depth=sphere.radius-dist,n;
    if(dist>EPS){
      if(depth<=0)return [];
      n=box.rotation.rotate(delta.scale(1/dist));
    } else {
      const gaps=['x','y','z'].map(k=>({
        k,gap:box.half[k]-Math.abs(l[k])
      })).sort((x,y)=>x.gap-y.gap),k=gaps[0].k;
      delta.set(0,0,0);
      delta[k]=l[k]>=0?1:-1;
      q[k]=delta[k]*box.half[k];
      n=box.rotation.rotate(delta);
      depth=sphere.radius+gaps[0].gap;
    }
    const point=box.localToWorld(q);
    return [contact(sphere===a?n.scale(-1):n,point,depth)];
  }
  const aa=a.axes(),ba=b.axes(),ha=[a.half.x,a.half.y,a.half.z],hb=[b.half.x,b.half.y,b.half.z],delta=Vec3.sub(b.position,a.position),candidates=[];
  aa.forEach((n,i)=>candidates.push({
    n,type:'a',axis:i
  }));
  ba.forEach((n,i)=>candidates.push({
    n,type:'b',axis:i
  }));
  for(let i=0;i<3;i++)for(let j=0;j<3;j++){
    const n=Vec3.cross(aa[i],ba[j]);
    if(n.lengthSq()>1e-10)candidates.push({
      n:n.normalize(),type:'edge'
    });
  }
  let best=null,depth=Infinity;
  for(const c of candidates){
    const ra=aa.reduce((s,n,i)=>s+Math.abs(n.dot(c.n))*ha[i],0),rb=ba.reduce((s,n,i)=>s+Math.abs(n.dot(c.n))*hb[i],0),overlap=ra+rb-Math.abs(delta.dot(c.n));
    if(overlap<=0)return [];
    if(overlap<depth){
      depth=overlap;
      best=c;
    }
  }
  const normal=best.n.clone().scale(delta.dot(best.n)>=0?1:-1);
  if(best.type==='edge')return [contact(normal,Vec3.add(a.support(normal),b.support(normal.clone().scale(-1))).scale(.5),depth)];
  const ref=best.type==='a'?a:b,inc=ref===a?b:a,rn=normal.clone().scale(ref===a?1:-1),refAxes=ref===a?aa:ba,incAxes=ref===a?ba:aa,axis=best.axis,refHalf=[ref.half.x,ref.half.y,ref.half.z],faceCenter=ref.position.clone().addScaled(rn,refHalf[axis]);
  let incAxis=0;
  for(let i=1;i<3;i++)if(Math.abs(incAxes[i].dot(rn))>Math.abs(incAxes[incAxis].dot(rn)))incAxis=i;
  let poly=faceVertices(inc,incAxis,incAxes[incAxis].dot(rn)>0?-1:1);
  for(let i=0;i<3;i++)if(i!==axis){
    poly=clipPolygon(poly,faceCenter,refAxes[i],refHalf[i]);
    poly=clipPolygon(poly,faceCenter,refAxes[i].clone().scale(-1),refHalf[i]);
  }
  const points=poly.map(p=>({
    p,d:Vec3.sub(p,faceCenter).dot(rn)
  })).filter(x=>x.d<=.002).sort((x,y)=>x.d-y.d).slice(0,4);
  if(!points.length)return [contact(normal,Vec3.add(a.support(normal),b.support(normal.clone().scale(-1))).scale(.5),depth)];
  return points.map(({
    p,d
  })=>contact(normal.clone(),p.clone().addScaled(rn,-d*.5),Math.max(0,-d)));
}
function inverseEffective(a,b,p,n){
  const ra=Vec3.sub(p,a.position),rb=Vec3.sub(p,b.position);
  return a.invMass+b.invMass+n.dot(Vec3.cross(a.inertiaMul(Vec3.cross(ra,n)),ra).add(Vec3.cross(b.inertiaMul(Vec3.cross(rb,n)),rb)));
}
function pairImpulse(a,b,p,j){
  a.impulse(j.clone().scale(-1),p);
  b.impulse(j,p);
}

export class BallSocketJoint {
  constructor(a,b,worldAnchor,{
    maxSwing=Math.PI*.65,maxTwist=Math.PI*.5,axis=new Vec3(0,1,0),bias=.18
  } = {}){
    this.a=a;
    this.b=b;
    this.localA=a.worldToLocal(worldAnchor);
    this.localB=b.worldToLocal(worldAnchor);
    this.axisA=a.rotation.conjugate().rotate(axis);
    this.axisB=b.rotation.conjugate().rotate(axis);
    this.reference=Quat.mul(a.rotation.conjugate(),b.rotation);
    this.maxSwing=maxSwing;
    this.maxTwist=maxTwist;
    this.bias=bias;
  }
  solve(dt){
    const a=this.a,b=this.b,pa=a.localToWorld(this.localA),pb=b.localToWorld(this.localB),p=Vec3.add(pa,pb).scale(.5),error=Vec3.sub(pb,pa);
    for(const n of AXES){
      const k=inverseEffective(a,b,p,n);
      if(k<EPS)continue;
      const rel=Vec3.sub(b.pointVelocity(pb),a.pointVelocity(pa)).dot(n),j=clamp(-(rel+this.bias*error.dot(n)/dt)/k,-100,100);
      pairImpulse(a,b,p,n.clone().scale(j));
    }
    const ax=a.rotation.rotate(this.axisA),bx=b.rotation.rotate(this.axisB),cross=Vec3.cross(ax,bx),angle=Math.acos(clamp(ax.dot(bx),-1,1));
    if(angle>this.maxSwing){
      let n=cross.normalize();
      if(n.lengthSq()<EPS)n=Vec3.cross(ax,Math.abs(ax.y)<.9?UP:AXES[0]).normalize();
      this.angularLimit(n,angle-this.maxSwing,dt);
    }
    const rel=Quat.mul(Quat.mul(a.rotation.conjugate(),b.rotation),this.reference.conjugate()).normalize(),projection=rel.x*this.axisA.x+rel.y*this.axisA.y+rel.z*this.axisA.z;
    let twist=2*Math.atan2(projection,rel.w);
    twist=((twist+Math.PI)%(2*Math.PI)+2*Math.PI)%(2*Math.PI)-Math.PI;
    if(Math.abs(twist)>this.maxTwist)this.angularLimit(ax.clone().scale(Math.sign(twist)),Math.abs(twist)-this.maxTwist,dt);
  }
  angularLimit(n,error,dt){
    const a=this.a,b=this.b,k=n.dot(a.inertiaMul(n).add(b.inertiaMul(n)));
    if(k<EPS)return;
    const rel=Vec3.sub(b.angularVelocity,a.angularVelocity).dot(n),j=Math.min(0,-(rel+.16*error/dt)/k);
    a.angularVelocity.addScaled(a.inertiaMul(n),-j);
    b.angularVelocity.addScaled(b.inertiaMul(n),j);
  }
}

export class PhysicsWorld {
  constructor({
    gravity=new Vec3(0,-9.81,0),iterations=14,ground=true
  } = {}){
    this.gravity=gravity;
    this.iterations=iterations;
    this.bodies=[];
    this.joints=[];
    this.contacts=[];
    this.disabledPairs=new Set();
    this.beforeStep=new Set();
    this.afterStep=new Set();
    this.bvh=new BVH();
    this.ground=ground;
    this.groundBody=new RigidBody({
      mass:0,half:new Vec3(1000,.5,1000),position:new Vec3(0,-.5,0),restitution:.05
    });
    this.topology=0;
    this.broadVersion=-1;
    this.cache=new Map();
  }
  add(body){
    this.bodies.push(body);
    this.topology++;
    return body;
  }
  remove(body){
    const i=this.bodies.indexOf(body);
    if(i>=0){
      this.bodies.splice(i,1);
      this.joints=this.joints.filter(j=>j.a!==body&&j.b!==body);
      for(const key of this.disabledPairs)if(key.split(':').some(id=>Number(id)===body.id))this.disabledPairs.delete(key);
      this.topology++;
    }
  }
  addJoint(joint){
    this.joints.push(joint);
    this.disabledPairs.add(this.pairKey(joint.a,joint.b));
    return joint;
  }
  pairKey(a,b){
    return a.id<b.id?`${a.id}:${b.id}`:`${b.id}:${a.id}`;
  }
  updateBroadphase(){
    for(const b of this.bodies)if(b.invMass || b.kinematic)b.updateBounds();
    if(this.broadVersion!==this.topology){
      this.bvh.rebuild(this.bodies);
      this.broadVersion=this.topology;
    }else this.bvh.refit();
  }
  generateContacts(){
    const out=[];
    for(const a of this.bodies){
      if(!a.invMass)continue;
      if(this.ground){
        const n=new Vec3(0,-1,0);
        if(a.shape==='sphere'){
          const depth=a.radius-a.position.y+(this.groundHeight?.(a.position.x,a.position.z)||0);
          if(depth>0)out.push({
            a,b:this.groundBody,normal:n,point:new Vec3(a.position.x,a.position.y-a.radius,a.position.z),depth,jn:0,jt1:0,jt2:0
          });
        }else for(const p of a.vertices()){
          const height=this.groundHeight?.(p.x,p.z)||0;
          if(p.y<height)out.push({a,b:this.groundBody,normal:this.groundHeight?this.terrainNormal(p.x,p.z).scale(-1):n.clone(),point:p,depth:height-p.y,jn:0,jt1:0,jt2:0});
        }
      }
      this.bvh.query(a.bounds,b=>{
        if(a===b||(b.invMass&&b.id<a.id)||this.disabledPairs.has(this.pairKey(a,b)))return;
        out.push(...collide(a,b));
      });
    }
    return out;
  }
  prepareContact(c,dt){
    const {
      a,b,point:p,normal:n
    }
    =c;
    c.t1=Vec3.cross(n,Math.abs(n.x)<.8?AXES[0]:UP).normalize();
    c.t2=Vec3.cross(n,c.t1);
    c.kn=inverseEffective(a,b,p,n);
    c.k1=inverseEffective(a,b,p,c.t1);
    c.k2=inverseEffective(a,b,p,c.t2);
    c.mu=Math.sqrt(a.friction*b.friction);
    const vn=Vec3.sub(b.pointVelocity(p),a.pointVelocity(p)).dot(n);
    c.target=Math.max(Math.min(2,.15*Math.max(0,c.depth-.004)/dt),vn<-.8?-Math.min(a.restitution,b.restitution)*vn:0);
    const local=a.worldToLocal(p);
    c.key=`${a.id}:${b.id}:${Math.round(local.x*12)}:${Math.round(local.y*12)}:${Math.round(local.z*12)}`;
    const old=this.cache.get(c.key);
    if(old&&old.n.dot(n)>.95){
      c.jn=old.jn*.8;
      c.jt1=old.jt1*.8;
      c.jt2=old.jt2*.8;
      pairImpulse(a,b,p,n.clone().scale(c.jn).addScaled(c.t1,c.jt1).addScaled(c.t2,c.jt2));
    }
  }
  solveContact(c){
    const {
      a,b,point:p,normal:n
    }
    =c;
    let v=Vec3.sub(b.pointVelocity(p),a.pointVelocity(p));
    const old=c.jn;
    c.jn=Math.max(0,old+(c.target-v.dot(n))/Math.max(EPS,c.kn));
    pairImpulse(a,b,p,n.clone().scale(c.jn-old));
    v=Vec3.sub(b.pointVelocity(p),a.pointVelocity(p));
    let u=c.jt1-v.dot(c.t1)/Math.max(EPS,c.k1),w=c.jt2-v.dot(c.t2)/Math.max(EPS,c.k2),mag=Math.hypot(u,w),limit=c.mu*c.jn;
    if(mag>limit&&mag>EPS){
      u*=limit/mag;
      w*=limit/mag;
    }
    pairImpulse(a,b,p,c.t1.clone().scale(u-c.jt1).addScaled(c.t2,w-c.jt2));
    c.jt1=u;
    c.jt2=w;
  }
  step(dt){
    if(!(dt>0&&dt<=.05))throw new RangeError('Physics dt must be in (0, .05] seconds');
    for(const b of this.bodies){
      b.previousPosition.copy(b.position);
      b.previousRotation.copy(b.rotation);
    }
    this.updateBroadphase();
    for(const f of this.beforeStep)f(dt);
    for(const b of this.bodies){
      if(!b.invMass)continue;
      b.velocity.addScaled(this.gravity,dt).addScaled(b.force,b.invMass*dt);
      const gyro=Vec3.cross(b.angularVelocity,b.inertiaTensorMul(b.angularVelocity));
      b.angularVelocity.addScaled(b.inertiaMul(Vec3.sub(b.torque,gyro)),dt);
      b.velocity.scale(Math.exp(-b.linearDamping*dt));
      b.angularVelocity.scale(Math.exp(-b.angularDamping*dt));
      b.force.set(0,0,0);
      b.torque.set(0,0,0);
    }
    this.contacts=this.generateContacts();
    for(const c of this.contacts)this.prepareContact(c,dt);
    for(let i=0;i<this.iterations;i++){
      for(const c of this.contacts)this.solveContact(c);
      for(const j of this.joints)j.solve(dt);
    }
    for(const b of this.bodies){
      if(!b.invMass)continue;
      b.position.addScaled(b.velocity,dt);
      b.rotation.integrate(b.angularVelocity,dt);
      b.updateBounds();
    }
    this.cache=new Map(this.contacts.map(c=>[c.key,{
      jn:c.jn,jt1:c.jt1,jt2:c.jt2,n:c.normal.clone()
    }]));
    this.bvh.refit();
    for(const f of this.afterStep)f(dt);
  }
  terrainNormal(x,z){const h=.1,fn=this.groundHeight||(()=>0);return new Vec3(fn(x-h,z)-fn(x+h,z),2*h,fn(x,z-h)-fn(x,z+h)).normalize();}
  raycast(origin,dir,max=Infinity,ignore=null){
    let best=this.bvh.raycast(origin,dir,(b,limit)=>b===ignore?null:b.raycast(origin,dir,limit),max);
    if(this.groundHeight && this.ground){
      const limit=Math.min(max,best?.distance??max,500);let previous=origin.y-this.groundHeight(origin.x,origin.z),left=0;
      for(let i=1;i<=64;i++){const t=limit*i/64,p=origin.clone().addScaled(dir,t),above=p.y-this.groundHeight(p.x,p.z);
        if(previous>=0&&above<=0){let low=left,high=t;for(let j=0;j<14;j++){const mid=(low+high)/2,q=origin.clone().addScaled(dir,mid);if(q.y>this.groundHeight(q.x,q.z))low=mid;else high=mid;}const distance=(low+high)/2,point=origin.clone().addScaled(dir,distance);if(distance>1e-5)best={distance,point,normal:this.terrainNormal(point.x,point.z),body:this.groundBody};break;}previous=above;left=t;
      }
    }else if(this.ground&&dir.y< -EPS){
      const t=-origin.y/dir.y;
      if(t>1e-5&&t<max&&(!best||t<best.distance))best={
        distance:t,point:origin.clone().addScaled(dir,t),normal:UP.clone(),body:this.groundBody
      };
    }
    return best;
  }
}

export class Ragdoll {
  constructor(world,origin=new Vec3(0,4,0)){
    this.world=world;
    this.bodies=[];
    this.joints=[];
    const part=(name,offset,half,mass)=>{
      const body=world.add(new RigidBody({
        position:Vec3.add(origin,Vec3.from(offset)),half:Vec3.from(half),mass,restitution:.05
      }));
      body.name=name;
      this.bodies.push(body);
      return body;
    };
    const pelvis=part('pelvis',[0,0,0],[.22,.16,.14],9),torso=part('torso',[0,.44,0],[.27,.28,.14],17),head=part('head',[0,.95,0],[.14,.16,.14],5);
    const joint=(a,b,p,opts={
    })=>{
      const j=world.addJoint(new BallSocketJoint(a,b,Vec3.add(origin,Vec3.from(p)),opts));
      this.joints.push(j);
    };
    joint(pelvis,torso,[0,.17,0],{
      maxSwing:.5,maxTwist:.45
    });
    joint(torso,head,[0,.76,0],{
      maxSwing:.65,maxTwist:.8
    });
    for(const side of [-1,1]){
      const upper=part('upper arm',[side*.42,.44,0],[.12,.25,.12],3),lower=part('forearm',[side*.42,-.07,0],[.1,.24,.1],2),thigh=part('thigh',[side*.14,-.49,0],[.11,.3,.12],7),shin=part('shin',[side*.14,-1.11,0],[.1,.29,.1],4);
      joint(torso,upper,[side*.29,.67,0],{
        maxSwing:1.4,maxTwist:1
      });
      joint(upper,lower,[side*.42,.18,0],{
        maxSwing:1.65,maxTwist:.12
      });
      joint(pelvis,thigh,[side*.14,-.15,0],{
        maxSwing:1.1,maxTwist:.35
      });
      joint(thigh,shin,[side*.14,-.8,0],{
        maxSwing:1.6,maxTwist:.1
      });
    }
  }
  dispose(){
    for(const b of this.bodies)this.world.remove(b);
    this.bodies=[];
    this.joints=[];
  }
}
