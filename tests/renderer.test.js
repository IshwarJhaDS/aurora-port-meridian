import test from 'node:test';
import assert from 'node:assert/strict';
import { GPUOcclusion } from '../src/renderer.js';
import { AABB, Vec3, Mat4 } from '../src/math.js';
function fixture(){
  const results=new Map();
  let next=0;
  const gl={
    ANY_SAMPLES_PASSED_CONSERVATIVE:1,QUERY_RESULT_AVAILABLE:2,QUERY_RESULT:3,createQuery(){
      const id=++next;
      results.set(id,{
        available:false,visible:true
      });
      return id;
    },getQueryParameter(q,name){
      return results.get(q)[name===2?'available':'visible'];
    },beginQuery(){
    },endQuery(){
    },deleteQuery(q){
      results.delete(q);
    }
  };
  const culling=new GPUOcclusion(gl,{
    draw(){
    }
  }),object={
    id:1,static:true,bounds:new AABB(new Vec3(-1,-1,-1),new Vec3(1,1,1))
  },program={
    i(){ return this; }, m4(){
    }
  },eye=new Vec3(0,4,10);
  return {
    results,culling,object,program,eye
  };
}
test('occlusion only rejects static objects after an available, current-camera query',()=>{
  const f=fixture(),vp=Mat4.identity();
  f.culling.begin(vp,'static');
  assert.equal(f.culling.visible(f.object),true);
  f.culling.issue(f.object,f.program,f.eye);
  assert.equal(f.culling.visible(f.object),true);
  f.results.get(1).available=true;
  f.results.get(1).visible=false;
  f.culling.begin(vp,'static');
  assert.equal(f.culling.visible(f.object),false);
  assert.equal(f.culling.visible({
    ...f.object,static:false
  }),true);
  const changed=vp.slice();
  changed[12]=.1;
  f.culling.begin(changed,'static');
  assert.equal(f.culling.visible(f.object),true);
  f.culling.dispose();
  assert.equal(f.results.size,0);
});
test('late occlusion results from an obsolete camera or moved static scene cannot hide objects',()=>{
  const f=fixture(),vp=Mat4.identity();
  f.culling.begin(vp,'v1');
  f.culling.issue(f.object,f.program,f.eye);
  const changed=vp.slice();
  changed[12]=1;
  f.culling.begin(changed,'v1');
  f.results.get(1).available=true;
  f.results.get(1).visible=false;
  f.culling.begin(changed,'v1');
  assert.equal(f.culling.visible(f.object),true);
  f.culling.issue(f.object,f.program,f.eye);
  f.culling.begin(changed,'v2');
  f.results.get(1).available=true;
  f.results.get(1).visible=false;
  f.culling.begin(changed,'v2');
  assert.equal(f.culling.visible(f.object),true);
  f.culling.dispose();
});
