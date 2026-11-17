import { clamp } from './math.js';

export class FixedStepClock {
  constructor({
    hz=120,maxSubsteps=12,maxFrame=.1
  } = {}) {
    if(hz<=0||maxSubsteps<1)throw new RangeError('Invalid clock configuration');
    this.dt=1/hz;
    this.maxSubsteps=maxSubsteps;
    this.maxFrame=maxFrame;
    this.accumulator=0;
    this.time=0;
    this.dropped=0;
  }
  advance(frameSeconds,step) {
    if(!Number.isFinite(frameSeconds)||frameSeconds<0)throw new RangeError('Invalid frame duration');
    const accepted=Math.min(frameSeconds,this.maxFrame);
    this.dropped+=frameSeconds-accepted;
    this.accumulator+=accepted;
    let n=0;
    while(this.accumulator+1e-12>=this.dt&&n<this.maxSubsteps){
      step(this.dt);
      this.accumulator-=this.dt;
      this.time+=this.dt;
      n++;
    }
    if(this.accumulator>=this.dt){
      const lost=Math.floor(this.accumulator/this.dt)*this.dt;
      this.dropped+=lost;
      this.accumulator-=lost;
    }
    return {
      steps:n,alpha:clamp(this.accumulator/this.dt,0,1)
    };
  }
}

export class ObjectPool {
  constructor(capacity,factory,reset=()=>{
  }) {
    if(!Number.isInteger(capacity)||capacity<1)throw new RangeError('Invalid pool capacity');
    this.capacity=capacity;
    this.values=Array.from({
      length:capacity
    },(_,i)=>factory(i));
    this.generations=new Float64Array(capacity);
    this.live=new Uint8Array(capacity);
    this.free=Array.from({
      length:capacity
    },(_,i)=>capacity-1-i);
    this.reset=reset;
    this.highWater=0;
  }
  acquire() {
    if(!this.free.length)throw new RangeError('Pool exhausted');
    const i=this.free.pop();
    this.live[i]=1;
    this.highWater=Math.max(this.highWater,this.capacity-this.free.length);
    return Object.freeze({
      index:i,generation:this.generations[i]
    });
  }
  get(h) {
    if(!h||h.index<0||h.index>=this.capacity||!this.live[h.index]||this.generations[h.index]!==h.generation)throw new Error('Stale pool handle');
    return this.values[h.index];
  }
  release(h) {
    const v=this.get(h);
    this.reset(v);
    this.live[h.index]=0;
    this.generations[h.index]++;
    this.free.push(h.index);
  }
  get used() {
    return this.capacity-this.free.length;
  }
}

export class FrameArena {
  constructor(bytes=1024*1024) {
    this.buffer=new ArrayBuffer(bytes);
    this.offset=0;
    this.highWater=0;
  }
  allocate(Type,count) {
    if(!Number.isInteger(count)||count<0)throw new RangeError('Invalid arena allocation');
    const alignment=Type.BYTES_PER_ELEMENT;
    const start=Math.ceil(this.offset/alignment)*alignment,end=start+alignment*count;
    if(end>this.buffer.byteLength)throw new RangeError('Frame arena exhausted');
    this.offset=end;
    this.highWater=Math.max(this.highWater,end);
    return new Type(this.buffer,start,count);
  }
  reset() {
    this.offset=0;
  }
}

export class EventBus {
  constructor(){
    this.listeners=new Map();
  }
  on(type,fn){
    if(!this.listeners.has(type))this.listeners.set(type,new Set());
    this.listeners.get(type).add(fn);
    return ()=>this.listeners.get(type)?.delete(fn);
  }
  emit(type,event){
    for(const fn of [...(this.listeners.get(type)||[])])fn(event);
  }
  clear(){
    this.listeners.clear();
  }
}
