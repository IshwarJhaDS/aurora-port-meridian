import { clamp, lerp } from './math.js';

export class BudgetedCache {
  constructor({
    budget=64*1024*1024,concurrency=2,dispose=v=>v?.close?.()
  } = {}) {
    if(budget<=0||concurrency<1)throw new RangeError('Invalid streaming configuration');
    this.budget=budget;
    this.concurrency=concurrency;
    this.dispose=dispose;
    this.entries=new Map();
    this.queue=[];
    this.used=0;
    this.reserved=0;
    this.active=0;
    this.stamp=0;
    this.closed=false;
  }
  request(key,{
    bytes,load,priority=0,pin=false
  }) {
    if(this.closed)throw new Error('Cache disposed');
    let e=this.entries.get(key);
    if(e){
      e.stamp=++this.stamp;
      e.priority=Math.max(e.priority,priority);
      if(pin)e.pins++;
      let released=false;
      return {
        promise:e.promise,release:()=>{
          if(!released){
            released=true;
            if(pin)this.unpin(e);
          }
        }
      };
    }
    if(!Number.isFinite(bytes)||bytes<=0||bytes>this.budget)throw new RangeError('Resource exceeds streaming budget');
    let resolve,reject;
    const promise=new Promise((a,b)=>{
      resolve=a;
      reject=b;
    });
    e={
      key,bytes,load,priority,pins:pin?1:0,stamp:++this.stamp,state:'queued',controller:new AbortController(),promise,resolve,reject,value:null
    };
    this.entries.set(key,e);
    this.queue.push(e);
    this.pump();
    let released=false;
    return {
      promise,release:()=>{
        if(!released){
          released=true;
          if(pin)this.unpin(e);
        }
      }
    };
  }
  unpin(e){
    e.pins=Math.max(0,e.pins-1);
    this.pump();
  }
  get(key){
    const e=this.entries.get(key);
    if(e?.state!=='resident')return null;
    e.stamp=++this.stamp;
    return e.value;
  }
  makeRoom(bytes){
    const candidates=[...this.entries.values()].filter(e=>e.state==='resident'&&!e.pins).sort((a,b)=>a.stamp-b.stamp);
    while(this.used+this.reserved+bytes>this.budget&&candidates.length){
      const e=candidates.shift();
      this.dispose(e.value);
      this.used-=e.bytes;
      this.entries.delete(e.key);
      e.state='evicted';
    }
    return this.used+this.reserved+bytes<=this.budget;
  }
  pump(){
    if(this.closed)return;
    this.queue.sort((a,b)=>b.priority-a.priority||a.stamp-b.stamp);
    while(this.active<this.concurrency&&this.queue.length){
      const index=this.queue.findIndex(e=>this.makeRoom(e.bytes));
      if(index<0)break;
      const e=this.queue.splice(index,1)[0];
      this.active++;
      this.reserved+=e.bytes;
      e.reserved=true;
      e.state='loading';
      Promise.resolve().then(()=>e.load(e.controller.signal)).then(value=>{
        if(this.closed||e.controller.signal.aborted||this.entries.get(e.key)!==e){
          this.dispose(value);
          throw new DOMException('Cancelled','AbortError');
        }
        this.reserved-=e.bytes;
        e.reserved=false;
        e.value=value;
        e.state='resident';
        this.used+=e.bytes;
        e.resolve(value);
      }).catch(error=>{
        if(this.entries.get(e.key)===e)this.entries.delete(e.key);
        e.state='failed';
        e.reject(error);
      }).finally(()=>{
        this.active--;
        if(e.reserved){
          this.reserved-=e.bytes;
          e.reserved=false;
        }
        this.pump();
      });
    }
  }
  cancel(key){
    const e=this.entries.get(key);
    if(!e)return;
    e.controller.abort();
    this.entries.delete(key);
    if(e.state==='queued'){
      this.queue=this.queue.filter(v=>v!==e);
      e.reject(new DOMException('Cancelled','AbortError'));
    }
    if(e.state==='resident'){
      this.used-=e.bytes;
      this.dispose(e.value);
    }
    e.state='cancelled';
    this.pump();
  }
  disposeAll(){
    this.closed=true;
    for(const key of [...this.entries.keys()])this.cancel(key);
    this.queue=[];
  }
}

export class TextureStreamer {
  constructor(gl,{
    budget=32*1024*1024,concurrency=2
  } = {}){
    this.gl=gl;
    this.cache=new BudgetedCache({
      budget,concurrency,dispose:texture=>gl.deleteTexture(texture)
    });
    this.failures=[];
  }
  request(url,size,{
    priority=0,pin=false
  } = {}){
    const gl=this.gl;
    return this.cache.request(url,{
      bytes:Math.ceil(size*size*4*4/3),priority,pin,load:async signal=>{
        const response=await fetch(url,{
          signal
        });
        if(!response.ok)throw new Error(`Texture HTTP ${response.status}: ${url}`);
        const image=await createImageBitmap(await response.blob(),{
          colorSpaceConversion:'none'
        });
        if(signal.aborted){
          image.close();
          throw new DOMException('Cancelled','AbortError');
        }
        if(image.width!==size||image.height!==size){
          image.close();
          throw new Error(`Texture dimensions do not match reservation for ${url}`);
        }
        const texture=gl.createTexture();
        try{
          gl.bindTexture(gl.TEXTURE_2D,texture);
          gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,false);
          gl.texImage2D(gl.TEXTURE_2D,0,gl.SRGB8_ALPHA8,gl.RGBA,gl.UNSIGNED_BYTE,image);
          gl.generateMipmap(gl.TEXTURE_2D);
          gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR_MIPMAP_LINEAR);
          gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);
          gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.REPEAT);
          gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.REPEAT);
          return texture;
        }catch(error){
          gl.deleteTexture(texture);
          throw error;
        }finally{
          image.close();
        }
      }
    });
  }
  get(url){
    return this.cache.get(url);
  }
  dispose(){
    this.cache.disposeAll();
  }
}

export class LODSelector {
  constructor({
    pixelThreshold=2,hysteresis=.18
  } = {}){
    this.threshold=pixelThreshold;
    this.hysteresis=hysteresis;
    this.levels=new Map();
  }
  select(id,levels,distance,viewportHeight,fov){
    if(!levels.length)throw new Error('LOD list cannot be empty');
    const scale=viewportHeight/(2*Math.tan(fov/2)*Math.max(.01,distance)),current=this.levels.get(id)??0;
    let selected=current;
    while(selected+1<levels.length&&levels[selected+1].error*scale<this.threshold*(1-this.hysteresis))selected++;
    while(selected>0&&levels[selected].error*scale>this.threshold*(1+this.hysteresis))selected--;
    this.levels.set(id,selected);
    return selected;
  }
  remove(id){
    this.levels.delete(id);
  }
}

export class WetnessField {
  constructor({
    size=128,extent=45
  } = {}){
    this.size=size;
    this.extent=extent;
    this.water=new Float32Array(size*size);
    this.next=new Float32Array(size*size);
    this.bytes=new Uint8Array(size*size);
    this.rain=0;
    this.evaporation=.012;
    this.diffusion=.1;
    this.mean=0;
  }
  step(dt,sun=1){
    const n=this.size,a=Math.min(.24,this.diffusion*dt),dry=this.evaporation*lerp(.25,1,sun);
    let total=0;
    for(let z=0;z<n;z++)for(let x=0;x<n;x++){
      const i=z*n+x,c=this.water[i],l=this.water[z*n+Math.max(0,x-1)],r=this.water[z*n+Math.min(n-1,x+1)],u=this.water[Math.max(0,z-1)*n+x],d=this.water[Math.min(n-1,z+1)*n+x],permeability=.6+.4*Math.sin(x*.17)*Math.cos(z*.23),value=clamp(c+(this.rain*.22*permeability-dry)*dt+a*(l+r+u+d-4*c),0,1);
      this.next[i]=value;
      this.bytes[i]=Math.round(value*255);
      total+=value;
    }
    [this.water,this.next]=[this.next,this.water];
    this.mean=total/this.water.length;
  }
  sample(x,z){
    const u=clamp(Math.floor((x/(this.extent*2)+.5)*this.size),0,this.size-1),v=clamp(Math.floor((z/(this.extent*2)+.5)*this.size),0,this.size-1);
    return this.water[v*this.size+u];
  }
}
