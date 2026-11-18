import { Vec3, Quat, Mat4, AABB, Frustum } from './math.js';
import { BVH } from './spatial.js';
import { GPUMesh, MeshData } from './mesh.js';
import { TextureStreamer, LODSelector } from './resources.js';
import * as shader from './shaders.js';
class Program {
  constructor(gl,vs,fs){
    this.gl=gl;
    this.uniforms=new Map();
    const stages=[];
    try{
      for(const [type,source]of [[gl.VERTEX_SHADER,vs],[gl.FRAGMENT_SHADER,fs]]){
        const s=gl.createShader(type);
        stages.push(s);
        gl.shaderSource(s,source);
        gl.compileShader(s);
        if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw new Error(gl.getShaderInfoLog(s));
      }
      this.handle=gl.createProgram();
      for(const s of stages)gl.attachShader(this.handle,s);
      gl.linkProgram(this.handle);
      if(!gl.getProgramParameter(this.handle,gl.LINK_STATUS))throw new Error(gl.getProgramInfoLog(this.handle));
    }catch(error){
      if(this.handle)gl.deleteProgram(this.handle);
      throw error;
    }finally{
      for(const s of stages)gl.deleteShader(s);
    }
  }
  use(){
    this.gl.useProgram(this.handle);
    return this;
  }
  location(n){
    if(!this.uniforms.has(n))this.uniforms.set(n,this.gl.getUniformLocation(this.handle,n));
    return this.uniforms.get(n);
  }
  f(n,v){
    this.gl.uniform1f(this.location(n),v);
    return this;
  }
  i(n,v){
    this.gl.uniform1i(this.location(n),v);
    return this;
  }
  v2(n,x,y){
    this.gl.uniform2f(this.location(n),x,y);
    return this;
  }
  v3(n,v){
    this.gl.uniform3f(this.location(n),v.x??v[0],v.y??v[1],v.z??v[2]);
    return this;
  }
  m4(n,v){
    this.gl.uniformMatrix4fv(this.location(n),false,v);
    return this;
  }
  v4array(n, v) { this.gl.uniform4fv(this.location(n), v); return this; }
  tex(n,texture,unit){
    const g=this.gl;
    g.activeTexture(g.TEXTURE0+unit);
    g.bindTexture(g.TEXTURE_2D,texture);
    return this.i(n,unit);
  }
  dispose(){
    this.gl.deleteProgram(this.handle);
  }
}

export class GPUOcclusion {
  constructor(gl,boxMesh){
    this.gl=gl;
    this.mesh=boxMesh;
    this.entries=new Map();
    this.lastVP=null;
    this.lastStatic='';
    this.epoch=0;
    this.stable=false;
    this.frame=0;
  }
  begin(vp,staticSignature){
    this.frame++;
    this.stable=this.lastVP!==null&&vp.every((v,i)=>Math.abs(v-this.lastVP[i])<1e-6)&&this.lastStatic===staticSignature;
    if(!this.stable)this.epoch++;
    this.lastVP=vp.slice();
    this.lastStatic=staticSignature;
    const gl=this.gl;
    for(const e of this.entries.values())if(e.pending&&gl.getQueryParameter(e.query,gl.QUERY_RESULT_AVAILABLE)){
      const visible=gl.getQueryParameter(e.query,gl.QUERY_RESULT);
      e.visible=e.epoch===this.epoch?visible:true;
      e.pending=false;
      e.resultFrame=this.frame;
    }
  }
  visible(object){
    if(!object.static||!this.stable)return true;
    const e=this.entries.get(object.id);
    return !e||e.epoch!==this.epoch||e.pending&&this.frame-e.resultFrame>2?true:e.visible;
  }
  issue(object,program,eye){
    if(!object.static)return;
    const gl=this.gl;
    let e=this.entries.get(object.id);
    if(!e){
      e={
        query:gl.createQuery(),pending:false,visible:true,resultFrame:0
      };
      this.entries.set(object.id,e);
    }
    if(e.pending)return;
    const b=object.bounds.clone().expand(.04);
    if(eye.x>=b.min.x&&eye.x<=b.max.x&&eye.y>=b.min.y&&eye.y<=b.max.y&&eye.z>=b.min.z&&eye.z<=b.max.z){
      e.visible=true;
      return;
    }
    program.i('uInstanced', 0).m4('uModel',Mat4.compose(b.center(),new Quat(),b.extent().scale(.5)));
    gl.beginQuery(gl.ANY_SAMPLES_PASSED_CONSERVATIVE,e.query);
    this.mesh.draw();
    gl.endQuery(gl.ANY_SAMPLES_PASSED_CONSERVATIVE);
    e.pending=true;
    e.epoch=this.epoch;
  }
  dispose(){
    for(const e of this.entries.values())this.gl.deleteQuery(e.query);
    this.entries.clear();
  }
}
class GPUFrameTimer {
  constructor(gl){
    this.gl=gl;
    this.ext=gl.getExtension('EXT_disjoint_timer_query_webgl2');
    this.pending=[];
    this.current=null;
    this.ms=null;
  }
  begin(){
    if(!this.ext||this.pending.length>=4)return;
    this.current=this.gl.createQuery();
    this.gl.beginQuery(this.ext.TIME_ELAPSED_EXT,this.current);
  }
  end(){
    if(!this.current)return;
    this.gl.endQuery(this.ext.TIME_ELAPSED_EXT);
    this.pending.push(this.current);
    this.current=null;
  }
  poll(){
    const g=this.gl;
    if(!this.ext)return;
    if(g.getParameter(this.ext.GPU_DISJOINT_EXT)){
      for(const q of this.pending)g.deleteQuery(q);
      this.pending=[];
      this.ms=null;
      return;
    }
    while(this.pending.length&&g.getQueryParameter(this.pending[0],g.QUERY_RESULT_AVAILABLE)){
      const q=this.pending.shift();
      this.ms=g.getQueryParameter(q,g.QUERY_RESULT)/1e6;
      g.deleteQuery(q);
    }
  }
  dispose(){
    if(this.current)this.gl.endQuery(this.ext.TIME_ELAPSED_EXT);
    for(const q of [...this.pending,...(this.current?[this.current]:[])])this.gl.deleteQuery(q);
    this.pending=[];
    this.current=null;
  }
}

export class Renderer {
  constructor(canvas){
    this.canvas=canvas;
    const gl=canvas.getContext('webgl2',{
      antialias:false,alpha:false,powerPreference:'high-performance'
    });
    if(!gl)throw new Error('WebGL 2 is required. Enable browser hardware acceleration.');
    this.gl=gl;
    if(!gl.getExtension('EXT_color_buffer_float'))throw new Error('EXT_color_buffer_float is required for the HDR G-buffer.');
    if(gl.getParameter(gl.MAX_TEXTURE_IMAGE_UNITS)<11)throw new Error('At least 11 fragment texture units are required.');
    this.programs={
      geometry:new Program(gl,shader.meshVS,shader.geometryFS),shadow:new Program(gl,shader.shadowVS,shader.shadowFS),query:new Program(gl,shader.shadowVS,shader.queryFS),gi:new Program(gl,shader.fullscreenVS,shader.giFS),lighting:new Program(gl,shader.fullscreenVS,shader.lightingFS),rain:new Program(gl,shader.rainVS,shader.rainFS),aa:new Program(gl,shader.fullscreenVS,shader.aaFS)
    };
    this.meshes=new Map();
    this.textures=[];
    this.framebuffers=[];
    this.fullscreenVAO=gl.createVertexArray();
    this.streamer=new TextureStreamer(gl);
    this.textureLeases=[];
    this.lod=new LODSelector({
      pixelThreshold:5
    });
    this.timer=new GPUFrameTimer(gl);
    this.occlusion=new GPUOcclusion(gl,this.mesh(MeshData.box()));
    this.enableOcclusion=true;
    this.white=this.texture({
      width:1,height:1,internal:gl.RGBA8,format:gl.RGBA,type:gl.UNSIGNED_BYTE,data:new Uint8Array([255,255,255,255])
    });
    this.wetTexture=this.texture({
      width:128,height:128,internal:gl.R8,format:gl.RED,type:gl.UNSIGNED_BYTE,data:new Uint8Array(128*128),linear:true
    });
    this.dataWidth=Math.min(1024,gl.getParameter(gl.MAX_TEXTURE_SIZE));
    this.bvhTexture=this.texture({
      width:this.dataWidth,height:1,internal:gl.RGBA32F,format:gl.RGBA,type:gl.FLOAT,data:null
    });
    this.triangleTexture=this.texture({
      width:this.dataWidth,height:1,internal:gl.RGBA32F,format:gl.RGBA,type:gl.FLOAT,data:null
    });
    this.traceBVH=new BVH();
    this.traceItems=[];
    this.traceSignature='';
    this.shadowSize=1536;
    this.shadowDepth=this.texture({
      width:this.shadowSize,height:this.shadowSize,internal:gl.DEPTH_COMPONENT24,format:gl.DEPTH_COMPONENT,type:gl.UNSIGNED_INT
    });
    this.shadowFBO=this.framebuffer([],this.shadowDepth);
    this.width=0;
    this.height=0;
    this.targets=[];
    this.settings={
      scale:1.1,gi:true,fog:true,ssr:true,shadowMode:1,debug:0,exposure:1.35,fogDensity:.0045
    };
    this.stats={
    };
    this.requestedTiers=new Set();
    this.ensureTexture(32);
  }
  // Albedo textures that arrive with generated (Tripo) models, uploaded once per image.
  imageTexture(image){
    this.imageTextures??=new Map();
    let texture=this.imageTextures.get(image);
    if(texture)return texture;
    const gl=this.gl;texture=gl.createTexture();
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D,texture);
    gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,false);
    gl.texImage2D(gl.TEXTURE_2D,0,gl.SRGB8_ALPHA8,gl.RGBA,gl.UNSIGNED_BYTE,image);
    gl.generateMipmap(gl.TEXTURE_2D);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR_MIPMAP_LINEAR);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);
    for(const wrap of [gl.TEXTURE_WRAP_S,gl.TEXTURE_WRAP_T])gl.texParameteri(gl.TEXTURE_2D,wrap,gl.REPEAT);
    const aniso=gl.getExtension('EXT_texture_filter_anisotropic');
    if(aniso)gl.texParameterf(gl.TEXTURE_2D,aniso.TEXTURE_MAX_ANISOTROPY_EXT,Math.min(8,gl.getParameter(aniso.MAX_TEXTURE_MAX_ANISOTROPY_EXT)));
    this.imageTextures.set(image,texture);
    return texture;
  }
  mesh(data){
    if(!this.meshes.has(data))this.meshes.set(data,new GPUMesh(this.gl,data));
    return this.meshes.get(data);
  }
  releaseObject(object) {
    for (const level of object.lods) { const mesh = this.meshes.get(level.mesh); if (mesh) { mesh.dispose(); this.meshes.delete(level.mesh); } }
    this.lod.remove(object.id);
    const entry = this.occlusion.entries.get(object.id); if (entry) { this.gl.deleteQuery(entry.query); this.occlusion.entries.delete(object.id); }
  }
  ensureTexture(size){
    if(this.requestedTiers.has(size))return;
    this.requestedTiers.add(size);
    const lease=this.streamer.request(`./assets/asphalt-${size}.png`,size,{
      priority:size===32?3:1,pin:size===32
    });
    lease.promise.catch(error=>this.streamer.failures.push(error.message));
    this.textureLeases.push(lease);
  }
  texture({
    width,height,internal,format,type,data=null,linear=false
  }){
    const g=this.gl,t=g.createTexture();
    g.bindTexture(g.TEXTURE_2D,t);
    g.texImage2D(g.TEXTURE_2D,0,internal,width,height,0,format,type,data);
    g.texParameteri(g.TEXTURE_2D,g.TEXTURE_MIN_FILTER,linear?g.LINEAR:g.NEAREST);
    g.texParameteri(g.TEXTURE_2D,g.TEXTURE_MAG_FILTER,linear?g.LINEAR:g.NEAREST);
    g.texParameteri(g.TEXTURE_2D,g.TEXTURE_WRAP_S,g.CLAMP_TO_EDGE);
    g.texParameteri(g.TEXTURE_2D,g.TEXTURE_WRAP_T,g.CLAMP_TO_EDGE);
    this.textures.push(t);
    return t;
  }
  framebuffer(colors,depth=null){
    const g=this.gl,f=g.createFramebuffer();
    g.bindFramebuffer(g.FRAMEBUFFER,f);
    colors.forEach((t,i)=>g.framebufferTexture2D(g.FRAMEBUFFER,g.COLOR_ATTACHMENT0+i,g.TEXTURE_2D,t,0));
    if(depth)g.framebufferTexture2D(g.FRAMEBUFFER,g.DEPTH_ATTACHMENT,g.TEXTURE_2D,depth,0);
    g.drawBuffers(colors.length?colors.map((_,i)=>g.COLOR_ATTACHMENT0+i):[g.NONE]);
    if(!colors.length)g.readBuffer(g.NONE);
    const status=g.checkFramebufferStatus(g.FRAMEBUFFER);
    if(status!==g.FRAMEBUFFER_COMPLETE){
      g.deleteFramebuffer(f);
      throw new Error(`Framebuffer incomplete: 0x${status.toString(16)}`);
    }
    this.framebuffers.push(f);
    return f;
  }
  resize(){
    const g=this.gl,ratio=Math.min(window.devicePixelRatio||1,2)*this.settings.scale,w=Math.max(64,Math.min(1600,Math.round(this.canvas.clientWidth*ratio))),h=Math.max(64,Math.min(1120,Math.round(this.canvas.clientHeight*ratio)));
    if(w===this.width&&h===this.height)return;
    for(const {
      kind,handle
    }
    of this.targets){
      if(kind==='texture'){
        g.deleteTexture(handle);
        this.textures=this.textures.filter(t=>t!==handle);
      }else{
        g.deleteFramebuffer(handle);
        this.framebuffers=this.framebuffers.filter(f=>f!==handle);
      }
    }
    this.targets=[];
    this.width=w;
    this.height=h;
    this.canvas.width=w;
    this.canvas.height=h;
    const tex=(internal,format,type,width=w,height=h)=>{
      const t=this.texture({
        width,height,internal,format,type
      });
      this.targets.push({
        kind:'texture',handle:t
      });
      return t;
    };
    this.albedo=tex(g.RGBA8,g.RGBA,g.UNSIGNED_BYTE);
    this.normal=tex(g.RGBA16F,g.RGBA,g.HALF_FLOAT);
    this.emission=tex(g.RGBA16F,g.RGBA,g.HALF_FLOAT);
    this.depth=tex(g.DEPTH_COMPONENT24,g.DEPTH_COMPONENT,g.UNSIGNED_INT);
    this.gbuffer=this.framebuffer([this.albedo,this.normal,this.emission],this.depth);
    this.targets.push({
      kind:'fbo',handle:this.gbuffer
    });
    this.gi=tex(g.RGBA16F,g.RGBA,g.HALF_FLOAT,Math.ceil(w/2),Math.ceil(h/2));
    this.giFBO=this.framebuffer([this.gi]);
    this.targets.push({
      kind:'fbo',handle:this.giFBO
    });
    this.display = this.texture({width:w,height:h,internal:g.RGBA8,format:g.RGBA,type:g.UNSIGNED_BYTE,linear:true});
    this.targets.push({kind:'texture',handle:this.display}); this.displayFBO=this.framebuffer([this.display]);this.targets.push({kind:'fbo',handle:this.displayFBO});
  }
  uploadData(texture,array){
    const g=this.gl,height=Math.max(1,Math.ceil(array.length/4/this.dataWidth));
    if(height>g.getParameter(g.MAX_TEXTURE_SIZE))throw new RangeError('BVH texture exceeds hardware limits');
    const padded=new Float32Array(this.dataWidth*height*4);
    padded.set(array);
    g.bindTexture(g.TEXTURE_2D,texture);
    g.texImage2D(g.TEXTURE_2D,0,g.RGBA32F,this.dataWidth,height,0,g.RGBA,g.FLOAT,padded);
  }
  updateTraceScene(objects){
    const signature=objects.filter(o=>o.castShadow!==false && !o.instances).map(o=>`${o.id}:${(o.shadowMesh || o.lods[0].mesh).indices.length}`).join('|'),rebuild=signature!==this.traceSignature;
    let count=0;
    for(const object of objects){
      if(object.castShadow===false || object.instances)continue;
      const mesh=object.shadowMesh || object.lods[0].mesh;
      for(let i=0;i<mesh.indices.length;i+=3){
        let item=this.traceItems[count];
        if(!item){
          item={
            bounds:new AABB(),a:new Vec3(),b:new Vec3(),c:new Vec3()
          };
          this.traceItems[count]=item;
        }
        item.a.copy(Mat4.point(object.model,Vec3.from(mesh.positions,mesh.indices[i]*3)));
        item.b.copy(Mat4.point(object.model,Vec3.from(mesh.positions,mesh.indices[i+1]*3)));
        item.c.copy(Mat4.point(object.model,Vec3.from(mesh.positions,mesh.indices[i+2]*3)));
        item.bounds=new AABB().include(item.a).include(item.b).include(item.c);
        count++;
      }
    }
    this.traceItems.length=count;
    if(rebuild){
      this.traceBVH.rebuild(this.traceItems);
      this.traceSignature=signature;
    }else this.traceBVH.refit();
    if(this.traceBVH.nodes.length>16384)throw new RangeError('Software ray traversal supports up to 16384 BVH nodes; reduce shadow-casting geometry.');
    const pack=this.traceBVH.gpuPack(),triangles=new Float32Array(Math.max(1,pack.ordered.length)*12);
    pack.ordered.forEach((t,i)=>{
      t.a.toArray(triangles,i*12);
      t.b.toArray(triangles,i*12+4);
      t.c.toArray(triangles,i*12+8);
    });
    this.uploadData(this.bvhTexture,pack.nodes);
    this.uploadData(this.triangleTexture,triangles);
  }
  common(program,camera,sun,lightVP){
    return program.use().tex('uAlbedo',this.albedo,0).tex('uNormal',this.normal,1).tex('uEmission',this.emission,2).tex('uDepth',this.depth,3).tex('uShadow',this.shadowDepth,4).m4('uInvVP',camera.inverseVP).m4('uVP',camera.vp).m4('uView',camera.view).m4('uLightVP',lightVP).v3('uEye',camera.eye).v3('uSunDirection',sun.direction).v3('uSunColor',sun.color).f('uSunIntensity',sun.intensity);
  }
  render(objects,camera,sun,wet,time,arena=null){
    const start=performance.now(),g=this.gl;
    this.resize();
    camera.update(this.width/this.height);
    this.timer.poll();
    this.timer.begin();
    const frustum=new Frustum(camera.vp);
    this.occlusion.begin(camera.vp,objects.filter(o=>o.static).map(o=>`${o.id}:${Array.from(o.model).join(',')}`).join('|'));
    g.bindTexture(g.TEXTURE_2D,this.wetTexture);
    g.texSubImage2D(g.TEXTURE_2D,0,0,0,wet.size,wet.size,g.RED,g.UNSIGNED_BYTE,wet.bytes);
    const extent = camera.shadowExtent || 48, target = camera.shadowExtent ? camera.target.clone() : new Vec3(0, 3, 0), texel = extent * 2 / this.shadowSize;
    target.x = Math.round(target.x / texel) * texel; target.z = Math.round(target.z / texel) * texel;
    const lightEye=sun.direction.clone().scale(extent * 2).add(target),lightVP=Mat4.mul(Mat4.ortho(-extent,extent,-extent,extent,1,extent * 4.5),Mat4.lookAt(lightEye,target)),lightFrustum=new Frustum(lightVP);
    let draws=0,triangles=0,culled=0;
    g.enable(g.DEPTH_TEST);
    g.depthFunc(g.LESS);
    g.depthMask(true);
    g.disable(g.BLEND);
    g.enable(g.CULL_FACE);
    g.cullFace(g.BACK);
    g.bindFramebuffer(g.FRAMEBUFFER,this.shadowFBO);
    g.viewport(0,0,this.shadowSize,this.shadowSize);
    g.clear(g.DEPTH_BUFFER_BIT);
    g.enable(g.POLYGON_OFFSET_FILL);
    g.polygonOffset(1.5,2.5);
    const sp=this.programs.shadow.use().m4('uVP',lightVP);
    for(const o of objects){
      if(o.castShadow===false || o.instances && !o.instanceCount || !lightFrustum.intersects(o.bounds))continue;
      sp.i('uInstanced', o.instances ? 1 : 0).m4('uModel',o.model);
      if(o.doubleSided)g.disable(g.CULL_FACE);
      this.mesh(o.lods[0].mesh).draw(o.instances, o.instanceCount, `${o.id}:${o.instanceVersion}`);
      if(o.doubleSided)g.enable(g.CULL_FACE);
      draws++;
    }
    g.disable(g.POLYGON_OFFSET_FILL);
    if(this.settings.shadowMode===1)this.updateTraceScene(objects);
    g.bindFramebuffer(g.FRAMEBUFFER,this.gbuffer);
    g.viewport(0,0,this.width,this.height);
    g.clearColor(0,0,0,0);
    g.clear(g.COLOR_BUFFER_BIT|g.DEPTH_BUFFER_BIT);
    const p=this.programs.geometry.use().m4('uVP',camera.vp).tex('uWet',this.wetTexture,1).f('uWetExtent',wet.extent).v2('uWetOrigin', wet.origin?.x || 0, wet.origin?.z || 0).f('uNightFactor',Math.max(0,Math.min(1,(.15-sun.direction.y)/.28))).f('uTime',time);
    const distances=arena?arena.allocate(Float32Array,objects.length):new Float32Array(objects.length),ordered=arena?arena.allocate(Uint32Array,objects.length):new Uint32Array(objects.length);
    objects.forEach((o,i)=>{
      distances[i]=o.bounds.center().distance(camera.eye);
      ordered[i]=i;
    });
    ordered.sort((a,b)=>distances[a]-distances[b]);
    const drawObject=index=>{
      const o=objects[index];
      if (o.instances && !o.instanceCount) return;
      if(!frustum.intersects(o.bounds)||(this.enableOcclusion&&!this.occlusion.visible(o))){
        culled++;
        return;
      }
      const distance=distances[index],level=this.lod.select(o.id,o.lods,distance,this.height,camera.fov),mesh=o.lods[level].mesh,material=o.material;
      if(material.textured)this.ensureTexture(distance<15?512:distance<35?128:32);
      const tier=distance<15?512:distance<35?128:32,texture=material.image?this.imageTexture(material.image):material.textured?(this.streamer.get(`./assets/asphalt-${tier}.png`)||this.streamer.get('./assets/asphalt-32.png')||this.white):this.white;
      p.i('uInstanced',o.instances?1:0).i('uVertexMaterials', material.vertexMaterials ? 1 : 0).i('uWater',o.water?1:0).m4('uModel',o.model).v3('uColor',material.color).v3('uEmissive',material.emissive||[0,0,0]).f('uRoughness',material.roughness??.65).f('uMetal',material.metallic??0).f('uWettable',material.wettable??1).f('uUVScale',material.uvScale??1).tex('uAlbedoTex',texture,0);
      if(o.doubleSided)g.disable(g.CULL_FACE);
      this.mesh(mesh).draw(o.instances, o.instanceCount, `${o.id}:${o.instanceVersion}`);
      if(o.doubleSided)g.enable(g.CULL_FACE);
      draws++;
      triangles+=mesh.indices.length/3*(o.instances?o.instanceCount:1);
    };
    for(const index of ordered)if(objects[index].static)drawObject(index);
    if(this.enableOcclusion){
      g.colorMask(false,false,false,false);
      g.depthMask(false);
      g.disable(g.CULL_FACE);
      g.depthFunc(g.LEQUAL);
      const q=this.programs.query.use().m4('uVP',camera.vp);
      for(const o of objects)this.occlusion.issue(o,q,camera.eye);
      g.colorMask(true,true,true,true);
      g.depthMask(true);
      g.depthFunc(g.LESS);
      g.enable(g.CULL_FACE);
      p.use();
    }
    for(const index of ordered)if(!objects[index].static)drawObject(index);
    g.disable(g.DEPTH_TEST);
    g.disable(g.CULL_FACE);
    g.bindVertexArray(this.fullscreenVAO);
    g.bindFramebuffer(g.FRAMEBUFFER,this.giFBO);
    g.viewport(0,0,Math.ceil(this.width/2),Math.ceil(this.height/2));
    if(this.settings.gi){
      this.common(this.programs.gi,camera,sun,lightVP);
      g.drawArrays(g.TRIANGLES,0,3);
    }else{
      g.clearColor(0,0,0,0);
      g.clear(g.COLOR_BUFFER_BIT);
    }
    g.bindFramebuffer(g.FRAMEBUFFER,this.displayFBO);
    g.viewport(0,0,this.width,this.height);
    const lights = (sun.lights || []).slice(0,12), positions = new Float32Array(48), colors = new Float32Array(48);
    lights.forEach((l,i)=>{positions.set([l.x,l.y,l.z,l.radius],i*4); colors.set([...l.color,l.power],i*4);});
    this.common(this.programs.lighting,camera,sun,lightVP).tex('uGI',this.gi,5).tex('uBVH',this.bvhTexture,6).tex('uTriangles',this.triangleTexture,7).i('uDataWidth',this.dataWidth).i('uNodeCount',this.traceBVH.nodes.length).i('uShadowMode',this.settings.shadowMode).i('uEnableGI',this.settings.gi?1:0).i('uEnableFog',this.settings.fog?1:0).i('uEnableSSR',this.settings.ssr?1:0).i('uDebug',this.settings.debug).f('uRain',wet.rain).f('uExposure',this.settings.exposure + (sun.flash||0)).f('uFogDensity',this.settings.fogDensity).f('uClouds',sun.clouds||0).f('uGameTime',time).i('uPointCount',lights.length).v4array('uLightPositions[0]',positions).v4array('uLightColors[0]',colors);
    g.drawArrays(g.TRIANGLES,0,3);
    if(wet.rain>.01&&this.settings.debug===0){
      g.enable(g.BLEND);
      g.blendFunc(g.SRC_ALPHA,g.ONE_MINUS_SRC_ALPHA);
      this.programs.rain.use().m4('uVP',camera.vp).v3('uEye',camera.eye).f('uTime',time).f('uRain',wet.rain).tex('uDepth',this.depth,0).v2('uResolution',this.width,this.height);
      g.drawArrays(g.LINES,0,Math.floor(1800*wet.rain)*2);
      g.disable(g.BLEND);
    }
    g.bindFramebuffer(g.FRAMEBUFFER,null);g.bindVertexArray(this.fullscreenVAO);this.programs.aa.use().tex('uSource',this.display,0).v2('uResolution',this.width,this.height);g.drawArrays(g.TRIANGLES,0,3);
    this.timer.end();
    this.stats={
      draws,triangles,culled,objects:objects.length,cpuMs:performance.now()-start,gpuMs:this.timer.ms,width:this.width,height:this.height,textureBytes:this.streamer.cache.used,bvhNodes:this.traceBVH.nodes.length
    };
  }
  readback(){
    const g=this.gl,bytes=new Uint8Array(this.width*this.height*4);
    g.readPixels(0,0,this.width,this.height,g.RGBA,g.UNSIGNED_BYTE,bytes);
    return bytes;
  }
  dispose(){
    const g=this.gl;
    this.timer.dispose();
    this.occlusion.dispose();
    for(const lease of this.textureLeases)lease.release();
    this.streamer.dispose();
    for(const t of this.imageTextures?.values()||[])this.gl.deleteTexture(t);
    this.imageTextures?.clear();
    for(const m of this.meshes.values())m.dispose();
    for(const p of Object.values(this.programs))p.dispose();
    for(const t of this.textures)g.deleteTexture(t);
    for(const f of this.framebuffers)g.deleteFramebuffer(f);
    g.deleteVertexArray(this.fullscreenVAO);
    this.meshes.clear();
    this.textures=[];
    this.framebuffers=[];
  }
}
