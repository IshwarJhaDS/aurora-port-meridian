import { Vec3, Quat, Mat4, AABB } from '../math.js';
import { MeshData } from '../mesh.js';

const BOX = MeshData.box();
const sphereCache = new Map();
export const material = (roughness = .8, metallic = 0, emission = 0, wettable = 1) => [roughness, metallic, emission, wettable];
export const PRAYER_COLORS = Object.freeze([[.04, .18, .62], [.85, .85, .82], [.72, .06, .04], [.05, .42, .12], [.88, .7, .05]]);

export class GeometryBuilder {
  constructor() { this.positions = []; this.normals = []; this.uvs = []; this.colors = []; this.materials = []; this.indices = []; }
  append(mesh, transform, color, surface = material()) {
    const offset = this.positions.length / 3;
    // All builder scales are positive; inverse-transpose handles nonuniform shapes.
    const sx2 = transform[0] ** 2 + transform[1] ** 2 + transform[2] ** 2, sy2 = transform[4] ** 2 + transform[5] ** 2 + transform[6] ** 2, sz2 = transform[8] ** 2 + transform[9] ** 2 + transform[10] ** 2;
    for (let i = 0; i < mesh.positions.length; i += 3) {
      const p = Mat4.point(transform, Vec3.from(mesh.positions, i));
      const n = Vec3.from(mesh.normals, i);
      const worldNormal = new Vec3(transform[0] * n.x / sx2 + transform[4] * n.y / sy2 + transform[8] * n.z / sz2, transform[1] * n.x / sx2 + transform[5] * n.y / sy2 + transform[9] * n.z / sz2, transform[2] * n.x / sx2 + transform[6] * n.y / sy2 + transform[10] * n.z / sz2).normalize();
      p.toArray(this.positions, this.positions.length); worldNormal.toArray(this.normals, this.normals.length);
      this.colors.push(...color); this.materials.push(...surface);
    }
    this.uvs.push(...mesh.uvs);
    for (const index of mesh.indices) this.indices.push(index + offset);
    return this;
  }
  box(x, y, z, hx, hy, hz, color, surface = material(), yaw = 0) {
    return this.append(BOX, Mat4.compose(new Vec3(x, y, z), Quat.axisAngle(new Vec3(0, 1, 0), yaw), new Vec3(hx, hy, hz)), color, surface);
  }
  sphere(x, y, z, sx, sy, sz, color, surface = material(), segments = 8) {
    if (!sphereCache.has(segments)) sphereCache.set(segments, MeshData.sphere(segments, Math.max(4, segments / 2)));
    return this.append(sphereCache.get(segments), Mat4.compose(new Vec3(x, y, z), new Quat(), new Vec3(sx, sy, sz)), color, surface);
  }
  quad(a, b, c, d, color, surface = material()) {
    const start = this.positions.length / 3, n = Vec3.cross(Vec3.sub(b, a), Vec3.sub(c, a)).normalize();
    for (const [i, p] of [a, b, c, d].entries()) { p.toArray(this.positions, this.positions.length); n.toArray(this.normals, this.normals.length); this.uvs.push(i === 1 || i === 2 ? 1 : 0, i >= 2 ? 1 : 0); this.colors.push(...color); this.materials.push(...surface); }
    this.indices.push(start, start + 1, start + 2, start, start + 2, start + 3); return this;
  }
  gable(x, y, z, hx, height, hz, color) {
    const a = new Vec3(x - hx, y, z - hz), b = new Vec3(x + hx, y, z - hz), c = new Vec3(x, y + height, z - hz), d = new Vec3(x - hx, y, z + hz), e = new Vec3(x + hx, y, z + hz), f = new Vec3(x, y + height, z + hz);
    this.quad(a, c, f, d, color); this.quad(c, b, e, f, color);
    this.quad(b, c, a, a, color); this.quad(d, f, e, e, color); return this;
  }
  cylinder(x, y, z, radius, halfHeight, color, surface = material(), segments = 8) {
    for (let i = 0; i < segments; i++) {
      const a = i * 2 * Math.PI / segments, b = (i + 1) * 2 * Math.PI / segments;
      const p = new Vec3(x + Math.cos(a) * radius, y - halfHeight, z + Math.sin(a) * radius), q = new Vec3(x + Math.cos(b) * radius, y - halfHeight, z + Math.sin(b) * radius), r = new Vec3(q.x, y + halfHeight, q.z), s = new Vec3(p.x, y + halfHeight, p.z);
      this.quad(q, p, s, r, color, surface); this.quad(new Vec3(x, y + halfHeight, z), r, s, s, color, surface);
    }
    return this;
  }
  // Quad whose winding is corrected to face away from `inside`, so culling is reliable.
  facing(inside, a, b, c, d, color, surface = material()) {
    const n = Vec3.cross(Vec3.sub(b, a), Vec3.sub(c, a)), centre = Vec3.add(Vec3.add(a, b), Vec3.add(c, d)).scale(.25);
    return n.dot(Vec3.sub(centre, inside)) >= 0 ? this.quad(a, b, c, d, color, surface) : this.quad(d, c, b, a, color, surface);
  }
  /** Four-sided roof or frustum: base half extents (hx, hz) shrink to (tx, tz) at y + height. */
  pyramid(x, y, z, hx, height, hz, color, surface = material(), tx = 0, tz = 0, yaw = 0) {
    const c = Math.cos(yaw), s = Math.sin(yaw), at = (dx, dy, dz) => new Vec3(x + dx * c + dz * s, y + dy, z - dx * s + dz * c);
    const base = [at(-hx, 0, -hz), at(hx, 0, -hz), at(hx, 0, hz), at(-hx, 0, hz)], top = [at(-tx, height, -tz), at(tx, height, -tz), at(tx, height, tz), at(-tx, height, tz)];
    const inside = new Vec3(x, y + height * .3, z);
    for (let i = 0; i < 4; i++) { const j = (i + 1) % 4; this.facing(inside, base[i], base[j], top[j], top[i], color, surface); }
    if (tx > 0 || tz > 0) this.facing(new Vec3(x, y, z), top[0], top[1], top[2], top[3], color, surface);
    this.facing(new Vec3(x, y + 1, z), base[0], base[1], base[2], base[3], color, surface);
    return this;
  }
  /** A sagging string of five-colour prayer flags (blue, white, red, green, yellow) from a to b. */
  prayerFlags(a, b, sag = .8, spacing = .5) {
    const length = Vec3.sub(b, a).length(), count = Math.max(2, Math.floor(length / spacing)), yaw = Math.atan2(b.x - a.x, b.z - a.z) + Math.PI / 2;
    for (let i = 1; i < count; i++) {
      const t = i / count, p = Vec3.add(a, Vec3.sub(b, a).scale(t)); p.y -= sag * 4 * t * (1 - t);
      this.box(p.x, p.y - .2, p.z, .17, .19, .012, PRAYER_COLORS[i % 5], material(.9, 0, 0, .6), yaw);
    }
    return this;
  }
  build() {
    const mesh = new MeshData(this.positions, this.normals, this.uvs, this.indices);
    mesh.colors = new Float32Array(this.colors); mesh.materials = new Float32Array(this.materials); return mesh;
  }
}

let nextObjectId = 100000;
export function renderObject(mesh, { position = new Vec3(), color = [1, 1, 1], roughness = .8, metallic = 0, vertexMaterials = true, castShadow = true, static: fixed = true } = {}) {
  const model = Mat4.compose(position, new Quat()), bounds = new AABB(Vec3.add(mesh.bounds.min, position), Vec3.add(mesh.bounds.max, position));
  return { id: nextObjectId++, lods: [{ mesh, error: 0 }], material: { color, roughness, metallic, vertexMaterials }, model, bounds, static: fixed, castShadow };
}

export class InstanceBatch {
  constructor(mesh, capacity = 2048) {
    this.object = renderObject(mesh, { static: false, vertexMaterials: true });
    this.data = new Float32Array(capacity * 24); this.count = 0; this.object.instances = this.data; this.object.instanceCount = 0;
  }
  reset() { this.count = 0; this.object.bounds = new AABB(); }
  add(position, rotation, scale, color, surface = material()) {
    if ((this.count + 1) * 24 > this.data.length) { const larger = new Float32Array(this.data.length * 2); larger.set(this.data); this.data = larger; this.object.instances = larger; }
    const offset = this.count++ * 24, matrix = Mat4.compose(position, rotation, scale);
    this.data.set(matrix, offset); this.data.set([...color, 1], offset + 16); this.data.set(surface, offset + 20);
    const radius = Math.max(scale.x, scale.y, scale.z) * 1.75;
    this.object.bounds.include(position.clone().add(new Vec3(-radius, -radius, -radius))).include(position.clone().add(new Vec3(radius, radius, radius)));
  }
  box(x, y, z, hx, hy, hz, color, surface = material(), rotation = new Quat()) { this.add(new Vec3(x, y, z), rotation, new Vec3(hx, hy, hz), color, surface); }
  finish() { this.object.instanceCount = this.count; this.object.instanceVersion = (this.object.instanceVersion || 0) + 1; return this.object; }
}
