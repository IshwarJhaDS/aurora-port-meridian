import { Vec3, clamp } from './math.js';
import { TriangleMeshCollider } from './spatial.js';

export class DeformableSurface {
  constructor({
    width=3,depth=3,resolution=10,origin=new Vec3(0,4,0),compliance=2e-7,mass=3,pinCorners=true
  } = {}){
    if(resolution<2||mass<=0||compliance<0)throw new RangeError('Invalid surface');
    this.n=resolution;
    this.count=(resolution+1)**2;
    this.positions=new Float32Array(this.count*3);
    this.previous=new Float32Array(this.count*3);
    this.velocity=new Float32Array(this.count*3);
    this.invMass=new Float32Array(this.count).fill(this.count/mass);
    this.edges=[];
    this.compliance=compliance;
    this.damping=1.2;
    this.iterations=10;
    for(let z=0;z<=resolution;z++)for(let x=0;x<=resolution;x++){
      const i=z*(resolution+1)+x;
      new Vec3(origin.x+width*(x/resolution-.5),origin.y,origin.z+depth*(z/resolution-.5)).toArray(this.positions,i*3);
    }
    if(pinCorners)for(const i of [0,resolution,resolution*(resolution+1),this.count-1])this.invMass[i]=0;
    const indices=[];
    for(let z=0;z<resolution;z++)for(let x=0;x<resolution;x++){
      const a=z*(resolution+1)+x,b=a+1,c=a+resolution+1,d=c+1;
      indices.push(a,c,b,b,c,d);
    }
    this.indices=new Uint32Array(indices);
    const edge=(a,b,stiffness=1)=>this.edges.push({
      a,b,rest:Vec3.from(this.positions,a*3).distance(Vec3.from(this.positions,b*3)),lambda:0,compliance:compliance*stiffness
    });
    for(let z=0;z<=resolution;z++)for(let x=0;x<=resolution;x++){
      const i=z*(resolution+1)+x;
      if(x<resolution)edge(i,i+1);
      if(z<resolution)edge(i,i+resolution+1);
      if(x<resolution&&z<resolution){
        edge(i,i+resolution+2);
        edge(i+1,i+resolution+1);
      }
      if(x+2<=resolution)edge(i,i+2,10);
      if(z+2<=resolution)edge(i,i+2*(resolution+1),10);
    }
    this.previous.set(this.positions);
    this.collider=new TriangleMeshCollider(this.positions,this.indices);
  }
  collideParticle(i,b){
    const p=Vec3.from(this.positions,i*3),skin=.025;
    let n,depth;
    if(b.shape==='sphere'){
      const d=Vec3.sub(p,b.position),length=d.length();
      depth=b.radius+skin-length;
      if(depth<=0)return;
      n=length>1e-8?d.scale(1/length):new Vec3(0,1,0);
    } else{
      const q=b.worldToLocal(p),h=b.half;
      if(Math.abs(q.x)>h.x+skin||Math.abs(q.y)>h.y+skin||Math.abs(q.z)>h.z+skin)return;
      const gaps=['x','y','z'].map(k=>({
        k,d:h[k]+skin-Math.abs(q[k])
      })).sort((a,b)=>a.d-b.d),g=gaps[0],l=new Vec3();
      l[g.k]=q[g.k]>=0?1:-1;
      n=b.rotation.rotate(l);
      depth=g.d;
    }
    const wp=this.invMass[i],wr=b.invMass,lambda=depth/(wp+wr),correction=n.clone().scale(wp*lambda);
    p.add(correction).toArray(this.positions,i*3);
    if(wr)b.position.addScaled(n,-wr*lambda);
  }
  step(dt,world){
    this.previous.set(this.positions);
    const g=world?.gravity||new Vec3(0,-9.81,0),rigidBefore=new Map((world?.bodies||[]).filter(b=>b.invMass).map(b=>[b,b.position.clone()]));
    for(let i=0;i<this.count;i++){
      if(!this.invMass[i])continue;
      const v=Vec3.from(this.velocity,i*3).addScaled(g,dt).scale(Math.exp(-this.damping*dt)),p=Vec3.from(this.positions,i*3).addScaled(v,dt);
      v.toArray(this.velocity,i*3);
      p.toArray(this.positions,i*3);
    }
    for(const e of this.edges)e.lambda=0;
    for(let it=0;it<this.iterations;it++){
      for(const e of this.edges){
        const a=Vec3.from(this.positions,e.a*3),b=Vec3.from(this.positions,e.b*3),n=Vec3.sub(a,b),length=n.length(),alpha=e.compliance/(dt*dt),sum=this.invMass[e.a]+this.invMass[e.b];
        if(length<1e-9||sum===0)continue;
        n.scale(1/length);
        const dl=(-(length-e.rest)-alpha*e.lambda)/(sum+alpha);
        e.lambda+=dl;
        a.addScaled(n,dl*this.invMass[e.a]).toArray(this.positions,e.a*3);
        b.addScaled(n,-dl*this.invMass[e.b]).toArray(this.positions,e.b*3);
      }
      for(let i=0;i<this.count;i++)if(this.invMass[i]){
        this.positions[i*3+1]=Math.max(.025,this.positions[i*3+1]);
        for(const b of world?.bodies||[])this.collideParticle(i,b);
      }
    }
    for(let i=0;i<this.count;i++){
      const v=Vec3.sub(Vec3.from(this.positions,i*3),Vec3.from(this.previous,i*3)).scale(1/dt);
      v.toArray(this.velocity,i*3);
    }
    for(const [b,before]of rigidBefore)b.velocity.addScaled(Vec3.sub(b.position,before),1/dt);
    this.collider.updateBounds();
    if(world)world.updateBroadphase();
  }
  dent(point,normal,impulse,radius=.7){
    const magnitude=clamp(impulse*.001,0,.35);
    for(let i=0;i<this.count;i++){
      if(!this.invMass[i])continue;
      const p=Vec3.from(this.positions,i*3),d=p.distance(point);
      if(d<radius)p.addScaled(normal,-magnitude*(1-d/radius)**2).toArray(this.positions,i*3);
    }
    this.collider.updateBounds();
  }
  normals(){
    const out=new Float32Array(this.positions.length);
    for(let i=0;i<this.indices.length;i+=3){
      const a=this.indices[i],b=this.indices[i+1],c=this.indices[i+2],n=Vec3.cross(Vec3.sub(Vec3.from(this.positions,b*3),Vec3.from(this.positions,a*3)),Vec3.sub(Vec3.from(this.positions,c*3),Vec3.from(this.positions,a*3)));
      for(const j of [a,b,c]){
        out[j*3]+=n.x;
        out[j*3+1]+=n.y;
        out[j*3+2]+=n.z;
      }
    }
    for(let i=0;i<this.count;i++)Vec3.from(out,i*3).normalize().toArray(out,i*3);
    return out;
  }
}
