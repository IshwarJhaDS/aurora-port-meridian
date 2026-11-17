import { Vec3, Quat } from '../math.js';
import { RigidBody } from '../physics.js';
import { GeometryBuilder, renderObject, material } from './geometry.js';
import { Random, hash } from './util.js';

function clipHalfPlane(polygon, normal, constant) {
  const result = [];
  for (let i = 0; i < polygon.length; i++) {
    const a = polygon[i], b = polygon[(i + 1) % polygon.length], da = a[0] * normal[0] + a[1] * normal[1] - constant, db = b[0] * normal[0] + b[1] * normal[1] - constant;
    if (da <= 1e-8) result.push(a);
    if ((da < 0 && db > 0) || (da > 0 && db < 0)) { const t = da / (da - db); result.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]); }
  }
  return result;
}

export class GlassFracture {
  static cells(width, height, count = 9, seed = 53) {
    const random = new Random(seed), seeds = Array.from({ length: count }, () => [random.range(-width / 2, width / 2), random.range(-height / 2, height / 2)]), cells = [];
    for (let i = 0; i < seeds.length; i++) {
      let polygon = [[-width / 2, -height / 2], [width / 2, -height / 2], [width / 2, height / 2], [-width / 2, height / 2]];
      for (let j = 0; j < seeds.length && polygon.length; j++) if (i !== j) {
        const a = seeds[i], b = seeds[j], n = [b[0] - a[0], b[1] - a[1]], c = (b[0] ** 2 + b[1] ** 2 - a[0] ** 2 - a[1] ** 2) / 2;
        polygon = clipHalfPlane(polygon, n, c);
      }
      if (polygon.length >= 3) cells.push(polygon);
    }
    return cells;
  }
  static mesh(polygon) {
    const center = polygon.reduce((p, v) => [p[0] + v[0] / polygon.length, p[1] + v[1] / polygon.length], [0, 0]), g = new GeometryBuilder();
    for (let i = 0; i < polygon.length; i++) {
      const a = polygon[i], b = polygon[(i + 1) % polygon.length];
      const v = q => new Vec3(q[0] - center[0], q[1] - center[1], 0);
      g.quad(new Vec3(), v(a), v(b), v(b), [.13, .29, .32], material(.13, .65));
    }
    return { mesh: g.build(), center };
  }
}

export class DestructionSystem {
  constructor(world, progress, { panic = () => {}, release = () => {} } = {}) { this.world = world; this.progress = progress; this.panic = panic; this.release = release; this.debris = []; this.timer = 0; }
  break(prop, direction = new Vec3(0, 0, -1)) {
    if (!prop.breakable || this.progress.destroyed.includes(prop.id)) return false;
    this.progress.destroyed.push(prop.id); this.panic(new Vec3(prop.x, prop.y, prop.z));
    const random = new Random(hash(Math.round(prop.x), Math.round(prop.z), 123));
    const glass = prop.type === 'glass', pieces = glass ? GlassFracture.cells(2.6, 1.6, 9) : Array.from({ length: 5 }, () => null);
    for (const piece of pieces) {
      const center = new Vec3(prop.x + random.range(-.2, .2), prop.y + .65 + random.range(0, .6), prop.z + random.range(-.2, .2));
      const body = this.world.add(new RigidBody({ half: glass ? new Vec3(.13, .13, .035) : new Vec3(.13, .16, .13), mass: glass ? .15 : 1, position: center, restitution: .16 }));
      body.velocity.copy(direction).scale(random.range(1, 3)).add(new Vec3(random.range(-1.2, 1.2), random.range(1, 3), random.range(-1.2, 1.2))); body.angularVelocity.set(random.range(-5, 5), random.range(-5, 5), random.range(-5, 5));
      let object = null;
      if (glass) { const fragment = GlassFracture.mesh(piece); object = renderObject(fragment.mesh, { static: false }); object.doubleSided = true; body.position.x = prop.x + fragment.center[0]; body.position.y = prop.y + 1.2 + fragment.center[1]; body.previousPosition.copy(body.position); body.updateBounds(); }
      this.debris.push({ body, object, remaining: glass ? 7 : 10, color: prop.type === 'crate' ? [.35, .22, .11] : [.1, .17, .16] });
    }
    while (this.debris.length > 36) this.remove(this.debris.shift());
    return true;
  }
  remove(piece) { this.world.remove(piece.body); if (piece.object) this.release(piece.object); }
  update(dt) { for (let i = this.debris.length - 1; i >= 0; i--) { const d = this.debris[i]; d.remaining -= dt; if (d.remaining <= 0) { this.remove(d); this.debris.splice(i, 1); } } }
  dispose() { for (const d of this.debris) this.remove(d); this.debris = []; }
}
