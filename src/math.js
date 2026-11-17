export const EPS = 1e-8;
export const clamp = (x, lo, hi) => Math.max(lo, Math.min(hi, x));
export const lerp = (a, b, t) => a + (b - a) * t;
export const smoothstep = (a, b, x) => {
  const t = clamp((x-a)/(b-a),0,1);
  return t*t*(3-2*t);
};

export class Vec3 {
  constructor(x=0,y=0,z=0) {
    this.x=x;
    this.y=y;
    this.z=z;
  }
  set(x,y,z) {
    this.x=x;
    this.y=y;
    this.z=z;
    return this;
  }
  copy(v) {
    return this.set(v.x,v.y,v.z);
  }
  clone() {
    return new Vec3(this.x,this.y,this.z);
  }
  add(v) {
    this.x+=v.x;
    this.y+=v.y;
    this.z+=v.z;
    return this;
  }
  sub(v) {
    this.x-=v.x;
    this.y-=v.y;
    this.z-=v.z;
    return this;
  }
  scale(s) {
    this.x*=s;
    this.y*=s;
    this.z*=s;
    return this;
  }
  addScaled(v,s) {
    this.x+=v.x*s;
    this.y+=v.y*s;
    this.z+=v.z*s;
    return this;
  }
  dot(v) {
    return this.x*v.x+this.y*v.y+this.z*v.z;
  }
  cross(v) {
    return this.copy(Vec3.cross(this,v));
  }
  lengthSq() {
    return this.dot(this);
  }
  length() {
    return Math.sqrt(this.lengthSq());
  }
  normalize() {
    const n=this.length();
    return n>EPS?this.scale(1/n):this.set(0,0,0);
  }
  distance(v) {
    return Vec3.sub(this,v).length();
  }
  finite() {
    return Number.isFinite(this.x)&&Number.isFinite(this.y)&&Number.isFinite(this.z);
  }
  toArray(a=[],i=0) {
    a[i]=this.x;
    a[i+1]=this.y;
    a[i+2]=this.z;
    return a;
  }
  static from(a,i=0) {
    return new Vec3(a[i],a[i+1],a[i+2]);
  }
  static add(a,b) {
    return new Vec3(a.x+b.x,a.y+b.y,a.z+b.z);
  }
  static sub(a,b) {
    return new Vec3(a.x-b.x,a.y-b.y,a.z-b.z);
  }
  static cross(a,b) {
    return new Vec3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
  }
  static min(a,b) {
    return new Vec3(Math.min(a.x,b.x),Math.min(a.y,b.y),Math.min(a.z,b.z));
  }
  static max(a,b) {
    return new Vec3(Math.max(a.x,b.x),Math.max(a.y,b.y),Math.max(a.z,b.z));
  }
}

export class Quat {
  constructor(x=0,y=0,z=0,w=1) {
    this.x=x;
    this.y=y;
    this.z=z;
    this.w=w;
  }
  clone() {
    return new Quat(this.x,this.y,this.z,this.w);
  }
  copy(q) {
    this.x=q.x;
    this.y=q.y;
    this.z=q.z;
    this.w=q.w;
    return this;
  }
  normalize() {
    const n=Math.hypot(this.x,this.y,this.z,this.w);
    if(n<EPS)return this.copy(new Quat());
    this.x/=n;
    this.y/=n;
    this.z/=n;
    this.w/=n;
    return this;
  }
  conjugate() {
    return new Quat(-this.x,-this.y,-this.z,this.w);
  }
  multiply(q) {
    return this.copy(Quat.mul(this,q));
  }
  rotate(v) {
    const qv=new Vec3(this.x,this.y,this.z), t=Vec3.cross(qv,v).scale(2);
    return v.clone().addScaled(t,this.w).add(Vec3.cross(qv,t));
  }
  integrate(omega,dt) {
    const speed=omega.length();
    if(speed<EPS)return this;
    return this.copy(Quat.mul(Quat.axisAngle(omega.clone().scale(1/speed),speed*dt),this)).normalize();
  }
  static mul(a,b) {
    return new Quat(a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y,a.w*b.y-a.x*b.z+a.y*b.w+a.z*b.x,a.w*b.z+a.x*b.y-a.y*b.x+a.z*b.w,a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z);
  }
  static axisAngle(axis,angle) {
    const s=Math.sin(angle/2);
    return new Quat(axis.x*s,axis.y*s,axis.z*s,Math.cos(angle/2));
  }
  static slerp(a,b,t) {
    let d=a.x*b.x+a.y*b.y+a.z*b.z+a.w*b.w, s=d<0?-1:1;
    d=Math.abs(d);
    if(d>.9995)return new Quat(lerp(a.x,b.x*s,t),lerp(a.y,b.y*s,t),lerp(a.z,b.z*s,t),lerp(a.w,b.w*s,t)).normalize();
    const th=Math.acos(clamp(d,-1,1)), st=Math.sin(th), u=Math.sin((1-t)*th)/st,v=Math.sin(t*th)/st*s;
    return new Quat(a.x*u+b.x*v,a.y*u+b.y*v,a.z*u+b.z*v,a.w*u+b.w*v);
  }
}

export class Mat4 {
  static identity() {
    return new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]);
  }
  static mul(a,b) {
    const o=new Float32Array(16);
    for(let c=0;c<4;c++)for(let r=0;r<4;r++)for(let k=0;k<4;k++)o[c*4+r]+=a[k*4+r]*b[c*4+k];
    return o;
  }
  static compose(p,q,s=new Vec3(1,1,1)) {
    const x=q.x,y=q.y,z=q.z,w=q.w,xx=x*x,yy=y*y,zz=z*z,xy=x*y,xz=x*z,yz=y*z,wx=w*x,wy=w*y,wz=w*z;
    return new Float32Array([(1-2*(yy+zz))*s.x,2*(xy+wz)*s.x,2*(xz-wy)*s.x,0,2*(xy-wz)*s.y,(1-2*(xx+zz))*s.y,2*(yz+wx)*s.y,0,2*(xz+wy)*s.z,2*(yz-wx)*s.z,(1-2*(xx+yy))*s.z,0,p.x,p.y,p.z,1]);
  }
  static perspective(fov,aspect,near,far) {
    const f=1/Math.tan(fov/2),nf=1/(near-far);
    return new Float32Array([f/aspect,0,0,0,0,f,0,0,0,0,(far+near)*nf,-1,0,0,2*far*near*nf,0]);
  }
  static ortho(l,r,b,t,n,f) {
    return new Float32Array([2/(r-l),0,0,0,0,2/(t-b),0,0,0,0,-2/(f-n),0,-(r+l)/(r-l),-(t+b)/(t-b),-(f+n)/(f-n),1]);
  }
  static lookAt(eye,target,up=new Vec3(0,1,0)) {
    const z=Vec3.sub(eye,target).normalize();
    let x=Vec3.cross(up,z).normalize();
    if(x.lengthSq()<EPS)x=Vec3.cross(new Vec3(0,0,1),z).normalize();
    const y=Vec3.cross(z,x);
    return new Float32Array([x.x,y.x,z.x,0,x.y,y.y,z.y,0,x.z,y.z,z.z,0,-x.dot(eye),-y.dot(eye),-z.dot(eye),1]);
  }
  static invert(a) {
    const m=Array.from({
      length:4
    },(_,r)=>[a[r],a[4+r],a[8+r],a[12+r],...Array.from({
      length:4
    },(_,c)=>r===c?1:0)]);
    for(let c=0;c<4;c++) {
      let pivot=c;
      for(let r=c+1;r<4;r++)if(Math.abs(m[r][c])>Math.abs(m[pivot][c]))pivot=r;
      if(Math.abs(m[pivot][c])<1e-12)throw new Error('Singular matrix');
      [m[pivot],m[c]]=[m[c],m[pivot]];
      const v=m[c][c];
      for(let k=0;k<8;k++)m[c][k]/=v;
      for(let r=0;r<4;r++)if(r!==c){
        const f=m[r][c];
        for(let k=0;k<8;k++)m[r][k]-=f*m[c][k];
      }
    }
    const out=new Float32Array(16);
    for(let r=0;r<4;r++)for(let c=0;c<4;c++)out[c*4+r]=m[r][c+4];
    return out;
  }
  static point(m,v) {
    const w=m[3]*v.x+m[7]*v.y+m[11]*v.z+m[15];
    return new Vec3((m[0]*v.x+m[4]*v.y+m[8]*v.z+m[12])/w,(m[1]*v.x+m[5]*v.y+m[9]*v.z+m[13])/w,(m[2]*v.x+m[6]*v.y+m[10]*v.z+m[14])/w);
  }
}

export class AABB {
  constructor(min=new Vec3(Infinity,Infinity,Infinity),max=new Vec3(-Infinity,-Infinity,-Infinity)) {
    this.min=min;
    this.max=max;
  }
  clone() {
    return new AABB(this.min.clone(),this.max.clone());
  }
  include(p) {
    this.min=Vec3.min(this.min,p);
    this.max=Vec3.max(this.max,p);
    return this;
  }
  union(b) {
    return this.include(b.min).include(b.max);
  }
  center() {
    return Vec3.add(this.min,this.max).scale(.5);
  }
  extent() {
    return Vec3.sub(this.max,this.min);
  }
  area() {
    const d=this.extent();
    return 2*(d.x*d.y+d.y*d.z+d.z*d.x);
  }
  overlaps(b) {
    return this.min.x<=b.max.x&&this.max.x>=b.min.x&&this.min.y<=b.max.y&&this.max.y>=b.min.y&&this.min.z<=b.max.z&&this.max.z>=b.min.z;
  }
  expand(s) {
    this.min.add(new Vec3(-s,-s,-s));
    this.max.add(new Vec3(s,s,s));
    return this;
  }
  ray(origin,dir,tMax=Infinity) {
    let near=0,far=tMax;
    for(const k of ['x','y','z']) {
      if(Math.abs(dir[k])<1e-12){
        if(origin[k]<this.min[k]||origin[k]>this.max[k])return null;
        continue;
      }
      let a=(this.min[k]-origin[k])/dir[k],b=(this.max[k]-origin[k])/dir[k];
      if(a>b)[a,b]=[b,a];
      near=Math.max(near,a);
      far=Math.min(far,b);
      if(near>far)return null;
    }
    return near;
  }
}

export class Frustum {
  constructor(m) {
    this.planes=[];
    for(const [r,s] of [[0,1],[0,-1],[1,1],[1,-1],[2,1],[2,-1]]){
      const p=[m[3]+s*m[r],m[7]+s*m[4+r],m[11]+s*m[8+r],m[15]+s*m[12+r]],l=Math.hypot(...p.slice(0,3));
      this.planes.push(p.map(v=>v/l));
    }
  }
  intersects(b) {
    return this.planes.every(p=>p[0]*(p[0]>=0?b.max.x:b.min.x)+p[1]*(p[1]>=0?b.max.y:b.min.y)+p[2]*(p[2]>=0?b.max.z:b.min.z)+p[3]>=0);
  }
}
