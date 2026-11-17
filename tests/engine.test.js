import test from 'node:test';
import assert from 'node:assert/strict';
import { Vec3, Quat, Mat4, AABB, Frustum } from '../src/math.js';
import { FixedStepClock, ObjectPool, FrameArena, EventBus } from '../src/core.js';
import { BVH, rayTriangle, TriangleMeshCollider } from '../src/spatial.js';
import { RigidBody, PhysicsWorld, collide, Ragdoll } from '../src/physics.js';
import { RaycastVehicle, TorqueCurve } from '../src/vehicle.js';
import { DeformableSurface } from '../src/deformable.js';
import { BudgetedCache, WetnessField, LODSelector } from '../src/resources.js';
import { MeshData } from '../src/mesh.js';
import { DayNightCycle } from '../src/engine.js';
const near=(a,b,e=1e-5)=>assert.ok(Math.abs(a-b)<e,`${a} != ${b} (tolerance ${e})`);
const vectorNear=(a,b,e=1e-5)=>{
  near(a.x,b.x,e);
  near(a.y,b.y,e);
  near(a.z,b.z,e);
};
const advance=(world,seconds)=>{
  for(let i=0;i<Math.round(seconds*120);i++)world.step(1/120);
};
let seed=42;
const random=()=>{
  seed=(1664525*seed+1013904223)>>>0;
  return seed/4294967296;
};
test('vector cross, dot, zero normalization and quaternion rotation',()=>{
  const a=new Vec3(1,0,0),b=new Vec3(0,1,0);
  vectorNear(Vec3.cross(a,b),new Vec3(0,0,1));
  near(a.dot(b),0);
  vectorNear(new Vec3().normalize(),new Vec3());
  vectorNear(Quat.axisAngle(new Vec3(0,1,0),Math.PI/2).rotate(a),new Vec3(0,0,-1));
});
test('quaternion integration is normalized and frame independent at constant omega',()=>{
  const a=new Quat(),b=new Quat(),w=new Vec3(.2,1,-.4);
  a.integrate(w,1);
  for(let i=0;i<120;i++)b.integrate(w,1/120);
  vectorNear(a.rotate(new Vec3(1,2,3)),b.rotate(new Vec3(1,2,3)));
  near(Math.hypot(b.x,b.y,b.z,b.w),1);
});
test('matrix inverse, composition and homogeneous projection round-trip',()=>{
  const m=Mat4.compose(new Vec3(3,-4,6),Quat.axisAngle(new Vec3(0,1,0),.8),new Vec3(2,3,4)),v=new Vec3(.2,1,-.8);
  vectorNear(Mat4.point(Mat4.invert(m),Mat4.point(m,v)),v);
  const vp=Mat4.mul(Mat4.perspective(1,1.4,.1,100),Mat4.lookAt(new Vec3(2,4,6),new Vec3()));
  vectorNear(Mat4.point(Mat4.invert(vp),Mat4.point(vp,v)),v,5e-5);
  assert.throws(()=>Mat4.invert(new Float32Array(16)),/Singular/);
});
test('AABB slab handles parallel rays on faces without NaN',()=>{
  const b=new AABB(new Vec3(-1,-1,-1),new Vec3(1,1,1));
  near(b.ray(new Vec3(-1,0,4),new Vec3(0,0,-1)),3);
  assert.equal(b.ray(new Vec3(-2,0,4),new Vec3(0,0,-1)),null);
  near(b.ray(new Vec3(),new Vec3(0,1,0)),0);
});
test('frustum rejects behind-camera and far objects',()=>{
  const f=new Frustum(Mat4.perspective(Math.PI/2,1,.1,20));
  assert.equal(f.intersects(new AABB(new Vec3(-1,-1,-4),new Vec3(1,1,-2))),true);
  assert.equal(f.intersects(new AABB(new Vec3(-1,-1,2),new Vec3(1,1,4))),false);
});
test('fixed step caps stalls, conserves accepted time and bounds interpolation',()=>{
  const clock=new FixedStepClock({
    hz:120,maxSubsteps:8,maxFrame:.1
  });
  let count=0;
  const first=clock.advance(.016,()=>count++);
  assert.equal(first.steps,1);
  const next=clock.advance(1,()=>count++);
  assert.equal(next.steps,8);
  assert.ok(clock.dropped>.9);
  assert.ok(next.alpha>=0&&next.alpha<=1);
  near(clock.time,count/120);
  assert.throws(()=>clock.advance(-1,()=>{
  }));
});
test('pool rejects stale handles, double releases and exhaustion',()=>{
  const p=new ObjectPool(1,()=>({
    count:0
  }),x=>x.count=0),h=p.acquire();
  p.get(h).count=7;
  assert.throws(()=>p.acquire(),/exhausted/);
  p.release(h);
  assert.throws(()=>p.release(h),/Stale/);
  const next=p.acquire();
  assert.equal(p.get(next).count,0);
  assert.throws(()=>p.get(h),/Stale/);
  assert.equal(p.highWater,1);
});
test('frame arena aligns typed allocations and detects overflow',()=>{
  const a=new FrameArena(32);
  a.allocate(Uint8Array,3);
  assert.equal(a.allocate(Float64Array,1).byteOffset,8);
  assert.throws(()=>a.allocate(Float64Array,3),/exhausted/);
  a.reset();
  assert.equal(a.allocate(Float32Array,2).byteOffset,0);
});
test('event unsubscribe is effective while dispatch uses a stable snapshot',()=>{
  const bus=new EventBus();
  let total=0;
  const off=bus.on('x',()=>total++);
  bus.emit('x',{
  });
  off();
  bus.emit('x',{
  });
  assert.equal(total,1);
});
test('ray triangle is double-sided and respects closest distance',()=>{
  const t=[new Vec3(-1,0,-1),new Vec3(1,0,-1),new Vec3(0,0,1)];
  near(rayTriangle(new Vec3(0,2,0),new Vec3(0,-1,0),...t).distance,2);
  assert.equal(rayTriangle(new Vec3(0,2,0),new Vec3(0,-1,0),...t,1),null);
  assert.ok(rayTriangle(new Vec3(0,-2,0),new Vec3(0,1,0),...t));
});
test('SAH BVH overlap queries match brute force on seeded randomized bounds',()=>{
  const items=Array.from({
    length:100
  },(_,id)=>{
    const p=new Vec3(random()*20-10,random()*20-10,random()*20-10);
    return {
      id,bounds:new AABB(p,p.clone().add(new Vec3(1,1,1)))
    };
  }),bvh=new BVH(items);
  for(let i=0;i<60;i++){
    const p=new Vec3(random()*16-8,random()*16-8,random()*16-8),q=new AABB(p,p.clone().add(new Vec3(4,4,4))),got=[];
    bvh.query(q,x=>got.push(x.id));
    assert.deepEqual(got.sort((a,b)=>a-b),items.filter(x=>q.overlaps(x.bounds)).map(x=>x.id).sort((a,b)=>a-b));
  }
});
test('BVH nearest-ray result matches brute force and refits moving bodies',()=>{
  const bodies=Array.from({
    length:40
  },()=>new RigidBody({
    shape:'sphere',position:new Vec3(random()*8-4,random()*8-4,random()*8-4)
  })),bvh=new BVH(bodies);
  for(let i=0;i<100;i++){
    const o=new Vec3(random()*20-10,random()*20-10,10),d=Vec3.sub(new Vec3(),o).normalize(),hit=bvh.raycast(o,d,(b,t)=>b.raycast(o,d,t)),brute=bodies.map(b=>b.raycast(o,d)).filter(Boolean).sort((a,b)=>a.distance-b.distance)[0];
    if(brute)near(hit.distance,brute.distance);
    else assert.equal(hit,null);
  }
  for(const b of bodies){
    b.position.x+=100;
    b.updateBounds();
  }
  bvh.refit();
  assert.equal(bvh.raycast(new Vec3(0,0,10),new Vec3(0,0,-1),(b,t)=>b.raycast(new Vec3(0,0,10),new Vec3(0,0,-1),t)),null);
});
test('GPU BVH flatten escape links terminate and all leaves cover their triangles',()=>{
  const items=Array.from({
    length:31
  },(_,id)=>({
    id,bounds:new AABB(new Vec3(id,0,0),new Vec3(id+1,1,1))
  })),bvh=new BVH(items),pack=bvh.gpuPack();
  assert.equal(bvh.nodes[0].skip,bvh.nodes.length);
  assert.equal(new Set(pack.ordered.map(x=>x.id)).size,31);
  for(let i=0;i<bvh.nodes.length;i++){
    const n=bvh.nodes[i];
    assert.ok(n.skip>i&&n.skip<=bvh.nodes.length);
    assert.ok(n.count<=4);
    near(pack.nodes[i*12+3],n.skip);
  }
});
test('triangle collider refit changes raycast surface height',()=>{
  const p=new Float32Array([-1,0,-1,1,0,-1,0,0,1]),mesh=new TriangleMeshCollider(p,new Uint32Array([0,1,2]));
  near(mesh.raycast(new Vec3(0,3,0),new Vec3(0,-1,0)).distance,3);
  for(let i=1;i<p.length;i+=3)p[i]=1;
  mesh.updateBounds();
  near(mesh.raycast(new Vec3(0,3,0),new Vec3(0,-1,0)).distance,2);
});
test('OBB SAT emits a face manifold and rejects a rotated separated box',()=>{
  const a=new RigidBody(),b=new RigidBody({
    position:new Vec3(.8,0,0)
  }),c=collide(a,b);
  assert.equal(c.length,4);
  for(const p of c){
    near(p.depth,.2);
    vectorNear(p.normal,new Vec3(1,0,0));
  }
  b.position.set(3,0,0);
  b.rotation=Quat.axisAngle(new Vec3(0,1,0),.7);
  assert.equal(collide(a,b).length,0);
});
test('sphere inside an OBB has a finite outward resolving contact',()=>{
  const box=new RigidBody({
    mass:0
  }),sphere=new RigidBody({
    shape:'sphere',radius:.2,position:new Vec3(.4,0,0)
  }),c=collide(sphere,box)[0];
  assert.ok(c.normal.finite());
  near(c.depth,.3);
  vectorNear(c.normal,new Vec3(-1,0,0));
});
test('falling sphere settles on ground and does not gain energy at rest',()=>{
  const w=new PhysicsWorld(),b=w.add(new RigidBody({
    shape:'sphere',radius:.5,position:new Vec3(0,3,0)
  }));
  advance(w,5);
  near(b.position.y,.5,.015);
  assert.ok(b.velocity.length()<.05);
  assert.ok(b.angularVelocity.length()<.05);
});
test('stacked boxes remain separated and finite after six seconds',()=>{
  const w=new PhysicsWorld({
    iterations:18
  }),bs=[];
  for(let i=0;i<3;i++)bs.push(w.add(new RigidBody({
    position:new Vec3(0,.52+i*1.02,0),restitution:0
  })));
  advance(w,6);
  for(let i=0;i<3;i++){
    assert.ok(bs[i].position.finite());
    near(bs[i].position.y,.5+i,.08);
    assert.ok(bs[i].velocity.length()<.25);
  }
});
test('equal-mass elastic impact preserves linear momentum',()=>{
  const w=new PhysicsWorld({
    ground:false,gravity:new Vec3()
  }),a=w.add(new RigidBody({
    shape:'sphere',position:new Vec3(-.49,0,0),restitution:1,friction:0
  })),b=w.add(new RigidBody({
    shape:'sphere',position:new Vec3(.49,0,0),restitution:1,friction:0
  }));
  a.linearDamping=b.linearDamping=0;
  a.velocity.x=2;
  b.velocity.x=-2;
  w.step(1/120);
  near(a.velocity.x+b.velocity.x,0);
  near(a.velocity.x,-2);
  near(b.velocity.x,2);
});
test('rigid body off-center impulse produces angular velocity',()=>{
  const b=new RigidBody({
    mass:2
  });
  b.impulse(new Vec3(1,0,0),new Vec3(0,1,0));
  near(b.velocity.x,.5);
  assert.ok(b.angularVelocity.z<0);
});
test('torque-free asymmetric rigid body approximately conserves rotational energy',()=>{
  const w=new PhysicsWorld({
    ground:false,gravity:new Vec3()
  }),b=w.add(new RigidBody({
    half:new Vec3(.5,1,1.5)
  }));
  b.angularDamping=0;
  b.angularVelocity.set(.4,.7,.2);
  const energy=()=>b.angularVelocity.dot(b.inertiaTensorMul(b.angularVelocity))*.5,initial=energy();
  advance(w,2);
  assert.ok(Math.abs(energy()/initial-1)<.01);
  assert.ok(b.angularVelocity.finite());
});
test('raycast world returns nearest body and supports ignored chassis',()=>{
  const w=new PhysicsWorld(),b=w.add(new RigidBody({
    position:new Vec3(0,2,0)
  }));
  w.updateBroadphase();
  near(w.raycast(new Vec3(0,5,0),new Vec3(0,-1,0)).distance,2.5);
  near(w.raycast(new Vec3(0,5,0),new Vec3(0,-1,0),10,b).distance,5);
});
test('articulated ragdoll falls with bounded anchor separation',()=>{
  const w=new PhysicsWorld({
    iterations:18
  }),r=new Ragdoll(w,new Vec3(0,4,0));
  advance(w,4);
  for(const b of r.bodies){
    assert.ok(b.position.finite()&&b.velocity.finite());
    assert.ok(b.position.y>-.15&&b.position.y<6);
  }
  const max=Math.max(...r.joints.map(j=>j.a.localToWorld(j.localA).distance(j.b.localToWorld(j.localB))));
  assert.ok(max<.2,`joint error ${max}`);
  r.dispose();
  assert.equal(w.bodies.length,0);
});
test('torque curve interpolates and rejects unordered input',()=>{
  const c=new TorqueCurve([[1000,100],[2000,200],[6000,0]]);
  near(c.at(1500),150);
  near(c.at(7000),0);
  assert.throws(()=>new TorqueCurve([[2,1],[1,2]]));
});
test('four suspension rays support chassis and throttle moves it forward',()=>{
  const w=new PhysicsWorld(),v=new RaycastVehicle(w);
  advance(w,2);
  assert.ok(v.wheels.every(w=>w.load>0));
  near(v.body.position.y,.717,.07);
  v.setControls({
    throttle:1
  });
  advance(w,2);
  assert.ok(v.body.velocity.z< -1,`velocity ${v.body.velocity.z}`);
  assert.ok(v.body.position.finite());
  assert.ok(v.rpm>=850&&v.rpm<=6800);
  v.dispose();
  assert.equal(w.bodies.length,0);
});
test('surface pinning, XPBD distance constraints and raycast refit work',()=>{
  const s=new DeformableSurface({
    resolution:6
  }),w=new PhysicsWorld(),original=Vec3.from(s.positions);
  for(let i=0;i<120;i++)s.step(1/120,w);
  vectorNear(Vec3.from(s.positions),original);
  assert.ok([...s.positions].every(Number.isFinite));
  const stretch=Math.max(...s.edges.map(e=>Math.abs(Vec3.from(s.positions,e.a*3).distance(Vec3.from(s.positions,e.b*3))-e.rest)));
  assert.ok(stretch<.1,`stretch ${stretch}`);
  assert.ok(s.collider.raycast(new Vec3(0,10,0),new Vec3(0,-1,0)));
  assert.ok([...s.normals()].every(Number.isFinite));
});
test('deformable particles collide with a rigid sphere and the floor',()=>{
  const s=new DeformableSurface({
    origin:new Vec3(0,.8,0),resolution:4,pinCorners:false
  }),w=new PhysicsWorld();
  w.add(new RigidBody({
    shape:'sphere',mass:0,radius:1,position:new Vec3()
  }));
  for(let i=0;i<120;i++)s.step(1/120,w);
  for(let i=0;i<s.count;i++){
    const p=Vec3.from(s.positions,i*3);
    assert.ok(p.y>=.024);
    assert.ok(p.length()>=1.02-.002);
  }
});
test('wetness grows in rain, dries without rain and remains bounded',()=>{
  const f=new WetnessField({
    size:16
  });
  f.rain=1;
  for(let i=0;i<600;i++)f.step(1/60,1);
  assert.ok(f.mean>.7);
  const wet=f.mean;
  f.rain=0;
  for(let i=0;i<600;i++)f.step(1/60,1);
  assert.ok(f.mean<wet);
  assert.ok([...f.water].every(v=>v>=0&&v<=1));
  assert.ok(f.sample(0,0)>0);
});
test('LOD hysteresis coarsens with distance without threshold chatter',()=>{
  const l=new LODSelector(),levels=[{
    error:0
  },{
    error:.05
  },{
    error:.3
  }];
  assert.equal(l.select('a',levels,2,800,1),0);
  assert.equal(l.select('a',levels,500,800,1),2);
  const v=l.select('a',levels,100,800,1);
  assert.ok(v>=1);
  assert.equal(l.select('a',levels,2,800,1),0);
});
test('day-night directions are normalized and sunlight vanishes at midnight',()=>{
  const d=new DayNightCycle(0);
  near(d.direction.length(),1);
  near(d.intensity,0);
  d.hour=12;
  d.update(0);
  near(d.direction.y,1);
  assert.ok(d.intensity>5);
  d.hour=23.99;
  d.running=true;
  d.update(1);
  assert.ok(d.hour<1);
});
test('mesh topology and winding agree with outward normals',()=>{
  for(const m of [MeshData.box(),MeshData.sphere(12,6)]){
    assert.equal(m.normals.length,m.positions.length);
    for(let i=0;i<m.indices.length;i+=3){
      const [a,b,c]=Array.from(m.indices.slice(i,i+3)),n=Vec3.cross(Vec3.sub(Vec3.from(m.positions,b*3),Vec3.from(m.positions,a*3)),Vec3.sub(Vec3.from(m.positions,c*3),Vec3.from(m.positions,a*3)));
      if(n.length()>1e-7)assert.ok(n.dot(Vec3.from(m.normals,a*3))>=0);
    }
  }
});
test('streaming cache evicts unpinned LRU, protects pins and enforces byte reservations',async()=>{
  const disposed=[],c=new BudgetedCache({
    budget:20,concurrency:1,dispose:v=>disposed.push(v)
  }),a=c.request('a',{
    bytes:10,pin:true,load:async()=>1
  });
  await a.promise;
  await c.request('b',{
    bytes:10,load:async()=>2
  }).promise;
  await c.request('c',{
    bytes:10,load:async()=>3
  }).promise;
  assert.deepEqual(disposed,[2]);
  assert.equal(c.get('a'),1);
  assert.equal(c.get('c'),3);
  assert.ok(c.used+c.reserved<=20);
  assert.throws(()=>c.request('huge',{
    bytes:21,load:async()=>0
  }));
  a.release();
  c.disposeAll();
  assert.equal(c.used,0);
});
test('streaming cancellation rejects queued requests and late uploads are disposed',async()=>{
  let complete;
  const disposed=[],c=new BudgetedCache({
    budget:20,concurrency:1,dispose:v=>disposed.push(v)
  });
  const a=c.request('a',{
    bytes:10,load:()=>new Promise(resolve=>complete=resolve)
  });
  await Promise.resolve();
  const b=c.request('b',{
    bytes:10,load:async()=>2
  });
  const rejected=assert.rejects(b.promise,/Cancelled/);
  c.cancel('b');
  await rejected;
  const rejectedA=assert.rejects(a.promise,/Cancelled/);
  c.cancel('a');
  complete(1);
  await rejectedA;
  await new Promise(resolve=>setTimeout(resolve,0));
  assert.deepEqual(disposed,[1]);
  assert.equal(c.used,0);
  assert.equal(c.reserved,0);
  c.disposeAll();
});
test('streaming loader failures release reservations and allow retry',async()=>{
  const c=new BudgetedCache({
    budget:10
  }),h=c.request('x',{
    bytes:10,load:async()=>{
      throw new Error('decode');
    }
  });
  await assert.rejects(h.promise,/decode/);
  await new Promise(resolve=>setTimeout(resolve,0));
  assert.equal(c.reserved,0);
  await c.request('x',{
    bytes:10,load:async()=>8
  }).promise;
  assert.equal(c.get('x'),8);
  c.disposeAll();
});
test('shared streaming leases release idempotently without unpinning another owner',async()=>{
  const c=new BudgetedCache({
    budget:20
  }),a=c.request('a',{
    bytes:10,pin:true,load:async()=>1
  }),b=c.request('a',{
    bytes:10,pin:true,load:async()=>1
  }),unpinned=c.request('a',{
    bytes:10,load:async()=>1
  });
  await a.promise;
  a.release();
  a.release();
  unpinned.release();
  assert.equal(c.entries.get('a').pins,1);
  b.release();
  assert.equal(c.entries.get('a').pins,0);
  c.disposeAll();
});
