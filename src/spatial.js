import { Vec3, AABB, EPS } from './math.js';
export function rayTriangle(origin,dir,a,b,c,tMax=Infinity) {
  const e1=Vec3.sub(b,a),e2=Vec3.sub(c,a),p=Vec3.cross(dir,e2),det=e1.dot(p);
  if(Math.abs(det)<EPS)return null;
  const inv=1/det,s=Vec3.sub(origin,a),u=s.dot(p)*inv;
  if(u<0||u>1)return null;
  const q=Vec3.cross(s,e1),v=dir.dot(q)*inv;
  if(v<0||u+v>1)return null;
  const distance=e2.dot(q)*inv;
  if(distance<1e-6||distance>tMax)return null;
  return {
    distance,u,v,point:origin.clone().addScaled(dir,distance),normal:Vec3.cross(e1,e2).normalize()
  };
}

export class BVH {
  constructor(items=[],{
    leafSize=4,bins=12
  } = {}) {
    this.leafSize=leafSize;
    this.bins=bins;
    this.nodes=[];
    this.ordered=[];
    this.rebuild(items);
  }
  rebuild(items) {
    this.nodes=[];
    this.ordered=[];
    const build=(group)=>{
      const index=this.nodes.length,node={
        bounds:new AABB(),left:-1,right:-1,first:0,count:0,skip:0
      };
      this.nodes.push(node);
      for(const v of group)node.bounds.union(v.bounds);
      if(group.length<=this.leafSize){
        node.first=this.ordered.length;
        node.count=group.length;
        this.ordered.push(...group);
      } else {
        const centers=new AABB();
        for(const v of group)centers.include(v.bounds.center());
        let cost=Infinity,axis=null,cut=0;
        for(const k of ['x','y','z']){
          const span=centers.max[k]-centers.min[k];
          if(span<1e-9)continue;
          const buckets=Array.from({
            length:this.bins
          },()=>({
            bounds:new AABB(),count:0
          }));
          for(const v of group){
            const b=Math.min(this.bins-1,Math.floor((v.bounds.center()[k]-centers.min[k])/span*this.bins));
            buckets[b].count++;
            buckets[b].bounds.union(v.bounds);
          }
          for(let s=1;s<this.bins;s++){
            const l=new AABB(),r=new AABB();
            let nl=0,nr=0;
            for(let b=0;b<this.bins;b++)if(buckets[b].count){
              if(b<s){
                l.union(buckets[b].bounds);
                nl+=buckets[b].count;
              }else{
                r.union(buckets[b].bounds);
                nr+=buckets[b].count;
              }
            }
            const candidate=l.area()*nl+r.area()*nr;
            if(nl&&nr&&candidate<cost){
              cost=candidate;
              axis=k;
              cut=centers.min[k]+span*s/this.bins;
            }
          }
        }
        let l=axis?group.filter(v=>v.bounds.center()[axis]<cut):[],r=axis?group.filter(v=>v.bounds.center()[axis]>=cut):[];
        if(!l.length||!r.length){
          const sorted=[...group].sort((a,b)=>a.bounds.center().x-b.bounds.center().x);
          const mid=sorted.length>>1;
          l=sorted.slice(0,mid);
          r=sorted.slice(mid);
        }
        node.left=build(l);
        node.right=build(r);
      }
      node.skip=this.nodes.length;
      return index;
    };
    if(items.length)build(items);
    return this;
  }
  refit() {
    for(let i=this.nodes.length-1;i>=0;i--){
      const n=this.nodes[i];
      n.bounds=new AABB();
      if(n.count){
        for(let j=0;j<n.count;j++)n.bounds.union(this.ordered[n.first+j].bounds);
      }else n.bounds.union(this.nodes[n.left].bounds).union(this.nodes[n.right].bounds);
    }
  }
  query(bounds,visit) {
    let i=0;
    while(i<this.nodes.length){
      const n=this.nodes[i];
      if(!n.bounds.overlaps(bounds)){
        i=n.skip;
        continue;
      }
      if(n.count)for(let j=0;j<n.count;j++){
        const v=this.ordered[n.first+j];
        if(v.bounds.overlaps(bounds))visit(v);
      }
      i++;
    }
  }
  raycast(origin,dir,intersect,tMax=Infinity) {
    let best=null,i=0;
    while(i<this.nodes.length){
      const n=this.nodes[i];
      if(n.bounds.ray(origin,dir,tMax)===null){
        i=n.skip;
        continue;
      }
      if(n.count)for(let j=0;j<n.count;j++){
        const item=this.ordered[n.first+j],hit=intersect(item,tMax);
        if(hit&&hit.distance<tMax){
          tMax=hit.distance;
          best={
            ...hit,item
          };
        }
      }
      i++;
    }
    return best;
  }
  gpuPack() {
    const nodes=new Float32Array(Math.max(1,this.nodes.length)*12);
    this.nodes.forEach((n,i)=>{
      n.bounds.min.toArray(nodes,i*12);
      nodes[i*12+3]=n.skip;
      n.bounds.max.toArray(nodes,i*12+4);
      nodes[i*12+7]=n.count;
      nodes[i*12+8]=n.first;
    });
    return {
      nodes,ordered:this.ordered
    };
  }
}

export class TriangleMeshCollider {
  constructor(positions,indices) {
    this.positions=positions;
    this.indices=indices;
    this.triangles=[];
    for(let i=0;i<indices.length;i+=3)this.triangles.push({
      index:i,bounds:new AABB()
    });
    this.updateBounds();
    this.bvh=new BVH(this.triangles);
  }
  triangle(item) {
    const i=item.index;
    return [Vec3.from(this.positions,this.indices[i]*3),Vec3.from(this.positions,this.indices[i+1]*3),Vec3.from(this.positions,this.indices[i+2]*3)];
  }
  updateBounds() {
    for(const t of this.triangles){
      t.bounds=new AABB();
      for(const p of this.triangle(t))t.bounds.include(p);
    }
    this.bvh?.refit();
  }
  raycast(origin,dir,max=Infinity) {
    return this.bvh.raycast(origin,dir,(item,limit)=>rayTriangle(origin,dir,...this.triangle(item),limit),max);
  }
}
