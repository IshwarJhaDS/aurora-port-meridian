import { Vec3, AABB } from './math.js';

export class MeshData {
  constructor(positions,normals,uvs,indices){
    this.positions=new Float32Array(positions);
    this.normals=new Float32Array(normals);
    this.uvs=new Float32Array(uvs);
    this.indices=new Uint32Array(indices);
    this.version=0;
    this.bounds=new AABB();
    for(let i=0;i<this.positions.length;i+=3)this.bounds.include(Vec3.from(this.positions,i));
  }
  static box(){
    const p=[],n=[],uv=[],idx=[];
    const faces=[[[1,0,0],[0,0,-1],[0,1,0]],[[-1,0,0],[0,0,1],[0,1,0]],[[0,1,0],[1,0,0],[0,0,-1]],[[0,-1,0],[1,0,0],[0,0,1]],[[0,0,1],[1,0,0],[0,1,0]],[[0,0,-1],[-1,0,0],[0,1,0]]];
    for(const [normal,u,v]of faces){
      const start=p.length/3;
      for(const [x,y]of [[-1,-1],[1,-1],[1,1],[-1,1]]){
        const pos=Vec3.from(normal).addScaled(Vec3.from(u),x).addScaled(Vec3.from(v),y);
        pos.toArray(p,p.length);
        n.push(...normal);
        uv.push((x+1)/2,(y+1)/2);
      }
      idx.push(start,start+1,start+2,start,start+2,start+3);
    }
    return new MeshData(p,n,uv,idx);
  }
  static sphere(segments=24,rings=12){
    const p=[],n=[],uv=[],idx=[];
    for(let j=0;j<=rings;j++)for(let i=0;i<=segments;i++){
      const phi=j/rings*Math.PI,theta=i/segments*Math.PI*2,v=new Vec3(-Math.sin(phi)*Math.cos(theta),Math.cos(phi),Math.sin(phi)*Math.sin(theta));
      v.toArray(p,p.length);
      v.toArray(n,n.length);
      uv.push(i/segments,j/rings);
    }
    for(let j=0;j<rings;j++)for(let i=0;i<segments;i++){
      const a=j*(segments+1)+i,b=a+segments+1;
      idx.push(a,b,a+1,a+1,b,b+1);
    }
    return new MeshData(p,n,uv,idx);
  }
  static surface(surface){
    const uv=[];
    for(let z=0;z<=surface.n;z++)for(let x=0;x<=surface.n;x++)uv.push(x/surface.n,z/surface.n);
    const mesh=new MeshData(surface.positions,surface.normals(),uv,surface.indices);
    mesh.surface=surface;
    return mesh;
  }
  updateSurface(){
    if(!this.surface)return;
    this.positions.set(this.surface.positions);
    this.normals=this.surface.normals();
    this.version++;
  }
}

export class GPUMesh {
  constructor(gl,data){
    this.gl=gl;
    this.data=data;
    this.version=-1;
    this.vao=gl.createVertexArray();
    this.buffers=[];
    gl.bindVertexArray(this.vao);
    const colors = data.colors || new Float32Array(data.positions.length).fill(1);
    const materials = data.materials || new Float32Array(data.positions.length / 3 * 4);
    for(const [location,size,values]of [[0,3,data.positions],[1,3,data.normals],[2,2,data.uvs],[3,3,colors],[4,4,materials]]){
      const b=gl.createBuffer();
      this.buffers.push(b);
      gl.bindBuffer(gl.ARRAY_BUFFER,b);
      gl.bufferData(gl.ARRAY_BUFFER,values,data.surface?gl.DYNAMIC_DRAW:gl.STATIC_DRAW);
      gl.enableVertexAttribArray(location);
      gl.vertexAttribPointer(location,size,gl.FLOAT,false,0,0);
    }
    this.index=gl.createBuffer();
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,this.index);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER,data.indices,gl.STATIC_DRAW);
    gl.bindVertexArray(null);
    this.version=data.version;
    this.instanceBuffer = null;
    this.instanceBytes = 0;
    this.instanceVersion = null;
  }
  update(){
    if(this.version===this.data.version)return;
    const g=this.gl;
    for(const [i,v]of [[0,this.data.positions],[1,this.data.normals]]){
      g.bindBuffer(g.ARRAY_BUFFER,this.buffers[i]);
      g.bufferSubData(g.ARRAY_BUFFER,0,v);
    }
    this.version=this.data.version;
  }
  draw(instances = null, count = 0, version = ''){
    this.update();
    const g=this.gl;
    g.bindVertexArray(this.vao);
    if (instances) {
      if (!count) return;
      if (!this.instanceBuffer) {
        this.instanceBuffer = g.createBuffer(); g.bindBuffer(g.ARRAY_BUFFER, this.instanceBuffer);
        for (let i = 0; i < 4; i++) { g.enableVertexAttribArray(5 + i); g.vertexAttribPointer(5 + i, 4, g.FLOAT, false, 96, i * 16); g.vertexAttribDivisor(5 + i, 1); }
        for (const [location, offset] of [[9, 64], [10, 80]]) { g.enableVertexAttribArray(location); g.vertexAttribPointer(location, 4, g.FLOAT, false, 96, offset); g.vertexAttribDivisor(location, 1); }
      }
      if (this.instanceVersion !== version) {
        g.bindBuffer(g.ARRAY_BUFFER, this.instanceBuffer); const values = instances.subarray(0, count * 24);
        if (values.byteLength > this.instanceBytes) { g.bufferData(g.ARRAY_BUFFER, values, g.DYNAMIC_DRAW); this.instanceBytes = values.byteLength; }
        else g.bufferSubData(g.ARRAY_BUFFER, 0, values);
        this.instanceVersion = version;
      }
      g.drawElementsInstanced(g.TRIANGLES, this.data.indices.length, g.UNSIGNED_INT, 0, count);
    } else g.drawElements(g.TRIANGLES,this.data.indices.length,g.UNSIGNED_INT,0);
  }
  dispose(){
    for(const b of this.buffers)this.gl.deleteBuffer(b);
    this.gl.deleteBuffer(this.index);
    if (this.instanceBuffer) this.gl.deleteBuffer(this.instanceBuffer);
    this.gl.deleteVertexArray(this.vao);
  }
}
