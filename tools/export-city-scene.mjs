import * as shaders from '../src/shaders.js';
import { CitySimulation } from '../src/game/city-simulation.js';
import { Vec3, Mat4, Quat, AABB } from '../src/math.js';
import { BVH } from '../src/spatial.js';

const simulation = new CitySimulation();
if(process.argv.includes('--night'))simulation.progress.minutes=20*60;
while (simulation.streamer.pending.size) simulation.streamer.pump(1000);
simulation.update(1 / 60);
simulation.camera.distance = 80; simulation.camera.pitch = .47; simulation.camera.yaw = .68;
simulation.camera.initialized = false; simulation.camera.tick(1); simulation.camera.eye.copy(simulation.camera.target).add(new Vec3(Math.sin(.68) * Math.cos(.47) * 80, Math.sin(.47) * 80, Math.cos(.68) * Math.cos(.47) * 80));
simulation.camera.update(960 / 540);
const objects = simulation.buildRenderObjects(), shadowRanges = [], mesh = { positions: [], normals: [], uvs: [], indices: [], colors: [], materials: [] };
function append(data, model, color, surface, perVertex) {
  const offset = mesh.positions.length / 3, sx = model[0] ** 2 + model[1] ** 2 + model[2] ** 2, sy = model[4] ** 2 + model[5] ** 2 + model[6] ** 2, sz = model[8] ** 2 + model[9] ** 2 + model[10] ** 2;
  for (let i = 0; i < data.positions.length; i += 3) {
    const p = Mat4.point(model, Vec3.from(data.positions, i)), n = Vec3.from(data.normals, i), normal = new Vec3(model[0] * n.x / sx + model[4] * n.y / sy + model[8] * n.z / sz, model[1] * n.x / sx + model[5] * n.y / sy + model[9] * n.z / sz, model[2] * n.x / sx + model[6] * n.y / sy + model[10] * n.z / sz).normalize();
    p.toArray(mesh.positions, mesh.positions.length); normal.toArray(mesh.normals, mesh.normals.length);
    mesh.colors.push(...(perVertex && data.colors ? data.colors.subarray(i, i + 3) : color));
    const m = perVertex && data.materials ? data.materials.subarray(i / 3 * 4, i / 3 * 4 + 4) : surface; mesh.materials.push(...m);
  }
  for (const uv of data.uvs) mesh.uvs.push(uv);
  for (const index of data.indices) mesh.indices.push(offset + index);
}
for (const object of objects) {
  const start = mesh.indices.length;
  const data = object.lods[0].mesh;
  if (object.instances) for (let i = 0; i < object.instanceCount; i++) append(data, object.instances.subarray(i * 24, i * 24 + 16), [...object.instances.subarray(i * 24 + 16, i * 24 + 19)], [...object.instances.subarray(i * 24 + 20, i * 24 + 24)], false);
  else append(data, object.model, object.material.color, [object.material.roughness || .8, object.material.metallic || 0, 0, 1], object.material.vertexMaterials);
  if(object.castShadow!==false)shadowRanges.push([start,mesh.indices.length-start]);
}
const eye = simulation.camera.eye, sun = simulation.sun.direction, target = simulation.camera.target;
// The native city preview uses the raster path; the separate GPU test exercises BVH rays.
const bvh = new BVH([{ bounds: new AABB(new Vec3(-1, -2, -1), new Vec3(1, -1, 1)), a: new Vec3(-1, -1, -1), b: new Vec3(1, -1, -1), c: new Vec3(0, -1, 1) }]);
const pack = bvh.gpuPack(), triangle = new Float32Array(12); pack.ordered[0].a.toArray(triangle); pack.ordered[0].b.toArray(triangle, 4); pack.ordered[0].c.toArray(triangle, 8);
console.log(JSON.stringify({ shaders, mesh, shadowRanges, models: [[...Mat4.identity()]], eye: eye.toArray(), view: [...simulation.camera.view], vp: [...simulation.camera.vp], inverseVP: [...simulation.camera.inverseVP], sun: sun.toArray(), sunColor: simulation.sun.color.toArray(), intensity: simulation.sun.intensity, vertexMaterials: 1, nightFactor: Math.max(0,Math.min(1,(.15-simulation.sun.direction.y)/.28)), lights:simulation.sun.lights, lightVP: [...Mat4.mul(Mat4.ortho(-100, 100, -100, 100, 1, 450), Mat4.lookAt(sun.clone().scale(200).add(target), target))], nodes: [...pack.nodes], triangles: [...triangle], nodeCount: 1 }));
simulation.dispose();
