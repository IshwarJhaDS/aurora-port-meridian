import { Vec3 } from '../math.js';
import { GeometryBuilder, renderObject, material } from './geometry.js';
import { pagoda, stupa, chautari } from './landmarks.js';
import { CHUNK_SIZE, WORLD_MIN, WORLD_MAX, Random, hash, districtAt, terrainHeight, POIS, keyOf } from './util.js';

const STONE = [.42, .43, .4], PAVING = [.46, .4, .34], ASPHALT = [.075, .075, .078];
// Kathmandu valley palette: exposed Newari brick, lime plaster, ochre and pastel concrete.
const FACADES = [[.42, .15, .08], [.48, .19, .1], [.36, .13, .075], [.62, .58, .5], [.6, .43, .2], [.45, .5, .52], [.58, .36, .38], [.5, .52, .36]];
const WOOD = [.11, .055, .028], TANK = [.03, .03, .035], TIN = [[.42, .1, .07], [.13, .27, .2], [.15, .2, .35], [.4, .4, .38]], AWNINGS = [[.6, .06, .05], [.06, .22, .55], [.75, .52, .05], [.08, .4, .2]];
const GLASS = [.055, .135, .17], METAL = material(.32, .65), WINDOW = material(.16, .45, -.65, .1);

export function describeChunk(cx, cz) {
  const random = new Random(hash(cx, cz, 183)), ox = cx * 80, oz = cz * 80;
  const district = districtAt(ox + 40, oz + 40), buildings = [], specials = POIS.filter(p => Math.floor(p.x / 80) === cx && Math.floor(p.z / 80) === cz);
  const publicSpace = cx === 0 && cz === 0 || cx === -2 && cz === 2 || cx === 0 && cz === 4;
  for (const p of specials) {
    if (['community', 'park', 'vista'].includes(p.kind)) continue;
    const x = p.x - ox + 16, z = p.z - oz;
    buildings.push({ id: `${cx}:${cz}:poi:${p.id}`, x, z, hx: 12, hz: 11, height: p.id === 'warehouse' ? 13 : p.id === 'clinic' ? 12 : 7, color: p.id === 'clinic' ? [.62, .64, .58] : p.id === 'cafe' ? [.49, .31, .2] : [.36, .4, .39], special: p.id, industrial: ['warehouse', 'harbor', 'depot'].includes(p.id) });
  }
  if (!publicSpace && ox < 400) for (const [x, z] of [[25, 25], [55, 25], [25, 55], [55, 55]]) {
    if (buildings.some(b => Math.abs(b.x - x) < b.hx + 11 && Math.abs(b.z - z) < b.hz + 11)) continue;
    if (district.id === 'rural' && (x !== 25 || z !== 25)) continue;
    const suburb = district.id === 'suburbs' || district.id === 'rural', industrial = district.id === 'industrial' || district.id === 'highway';
    const height = suburb ? random.range(4.5, 7) : industrial ? random.range(6, 10) : random.range(10, 21) * (cx === 0 || cz === 0 ? 1.15 : 1);
    buildings.push({ id: `${cx}:${cz}:lot:${x}:${z}`, x, z, hx: suburb ? 8.2 : random.range(8.5, 11), hz: suburb ? 8 : random.range(8, 10.5), height, color: random.pick(FACADES), suburb, industrial, roof: random.pick(TIN), awning: random.pick(AWNINGS), flags: random.next() < .65 });
  }
  const props = [], lights = [];
  for (const [i, [x, z]] of [[14, 11], [66, 69]].entries()) {
    if (ox + x > 430) continue;
    const id = `${cx}:${cz}:lamp:${i}`, y = terrainHeight(ox + x, oz + z);
    lights.push({ id, x: ox + x, y: y + 5.8, z: oz + z, color: [1, .72, .38], radius: 17, power: 18 });
    props.push({ id, type: 'lamp', x: ox + x, y, z: oz + z, breakable: false });
  }
  for (let i = 0; i < 2; i++) {
    const x = i ? 68 : 12, z = i ? 51 : 27;
    if (ox + x < 430) props.push({ id: `${cx}:${cz}:bin:${i}`, type: 'bin', x: ox + x, y: terrainHeight(ox + x, oz + z), z: oz + z, breakable: true });
  }
  if (district.id === 'industrial') for (let i = 0; i < 3; i++) props.push({ id: `${cx}:${cz}:crate:${i}`, type: 'crate', x: ox + 12, y: terrainHeight(ox + 12, oz + 43 + i * 1.5), z: oz + 43 + i * 1.5, breakable: true });
  return { id: keyOf(cx, cz), cx, cz, ox, oz, district, buildings, props, lights, specials, publicSpace };
}

function tree(g, x, z, seed, y = 0, detailed = true) {
  const r = new Random(seed), h = r.range(2.8, 4.4);
  g.box(x, y + h * .5, z, .16, h * .5, .16, [.18, .12, .075]);
  g.sphere(x, y + h + .5, z, 1.55, 2.0, 1.5, r.pick([[.12, .24, .13], [.2, .3, .13], [.14, .26, .1], [.55, .1, .12]]), material(.95), detailed ? 8 : 6); // the last is a rhododendron in bloom
}

function building(g, b, level, y = 0) {
  const { x, z, hx, hz, height: h, color } = b;
  g.box(x, y + h / 2, z, hx, h / 2, hz, color);
  g.box(x, y + h + .18, z, hx + .22, .18, hz + .22, [.19, .21, .21], material(.78));
  const roof = b.roof || TIN[0];
  if (b.suburb) {
    g.pyramid(x, y + h + .3, z, hx + .6, 2.6, hz + .6, roof, material(.5, .45), 1.2, hz * .25);
    g.box(x + hx * .45, y + h + 1.5, z - hz * .5, .6, 1.6, .65, [.38, .2, .12]);
  } else if (!b.industrial) {
    // Flat roof terrace: parapet, stair room with a tin canopy and the ubiquitous black water tank.
    for (const s of [-1, 1]) { g.box(x + s * hx, y + h + .7, z, .14, .5, hz, color); g.box(x, y + h + .7, z + s * hz, hx, .5, .14, color); }
    g.box(x - hx * .45, y + h + 1.4, z - hz * .4, hx * .35, 1.2, hz * .35, color);
    g.pyramid(x - hx * .45, y + h + 2.6, z - hz * .4, hx * .42, .9, hz * .42, roof, material(.5, .45), hx * .1, hz * .4);
    g.cylinder(x + hx * .5, y + h + 1.2, z + hz * .45, .75, .62, TANK, material(.4), 10);
    if (b.flags) g.prayerFlags(new Vec3(x - hx * .45, y + h + 4.2, z - hz * .4), new Vec3(x + hx * .95, y + h + 1.2, z + hz * .95), .9);
  } else g.pyramid(x, y + h + .3, z, hx + .3, 1.6, hz + .3, roof, material(.5, .5), hx * .1, hz + .3);
  if (level > 0) return;
  const floors = Math.max(1, Math.min(9, Math.floor(h / 3.5))), cols = b.suburb ? 2 : 4;
  for (let floor = 0; floor < floors; floor++) {
    const wy = y + 2.0 + floor * (h - 2.8) / floors;
    if (!b.suburb && floor > 0) g.box(x, wy - .75, z, hx + .1, .07, hz + .1, b.industrial ? color.map(v => v * .82) : WOOD);
    for (let col = 0; col < cols; col++) {
      const wx = x - hx + (col + .5) * hx * 2 / cols, wz = z - hz + (col + .5) * hz * 2 / cols;
      const lit = hash(Math.floor(x + col), floor, Math.floor(z)) % 5 !== 0;
      const surf = lit ? WINDOW : material(.17, .5, 0, .1);
      const w = b.suburb ? 1.0 : .75;
      g.box(wx, wy, z + hz + .035, w, .8, .045, GLASS, surf); g.box(wx, wy, z - hz - .035, w, .8, .045, GLASS, surf);
      g.box(x + hx + .035, wy, wz, .045, .8, w, GLASS, surf); g.box(x - hx - .035, wy, wz, .045, .8, w, GLASS, surf);
      if (!b.industrial) { // Carved sal-wood frames with a heavy lintel, as on Newari houses.
        g.box(wx, wy, z + hz + .02, w + .22, 1.0, .03, WOOD); g.box(wx, wy, z - hz - .02, w + .22, 1.0, .03, WOOD);
        g.box(x + hx + .02, wy, wz, .03, 1.0, w + .22, WOOD); g.box(x - hx - .02, wy, wz, .03, 1.0, w + .22, WOOD);
        g.box(wx, wy + 1.06, z + hz + .07, w + .4, .08, .09, WOOD); g.box(wx, wy + 1.06, z - hz - .07, w + .4, .08, .09, WOOD);
      }
    }
  }
  g.box(x - hx - .05, y + 1.2, z, .08, 1.2, .8, [.055, .07, .075], material(.3, .4));
  if (b.special || (!b.suburb && !b.industrial)) {
    const awning = b.special === 'cafe' ? [.55, .08, .05] : b.special === 'clinic' ? [.62, .69, .63] : b.awning || AWNINGS[1];
    g.box(x - hx - 1.8, y + 3.4, z, 1.9, .18, Math.min(hz, 7), awning);
    g.box(x - hx - .12, y + 4.1, z, .12, .4, Math.min(hz, 6), [.075, .13, .15], material(.55, .15, -.4));
    // Entry path and an outside service counter keep every mission location accessible.
    g.box(x - hx - 3, y + .055, z, 3.5, .055, 2, [.49, .48, .43]);
    if (b.special === 'cafe') {
      g.box(x - hx - 1.4, y + .55, z + 3, .5, .55, 1.4, [.24, .16, .09]);
      for (const dz of [-4, 5]) { g.cylinder(x - hx - 3.5, y + .9, z + dz, .8, .06, [.37, .28, .17]); g.box(x - hx - 3.5, y + .45, z + dz, .08, .45, .08, [.1, .12, .13]); }
    }
    if (b.special === 'clinic') { g.box(x - hx - .15, y + 4.1, z, .15, .6, .2, [.7, .2, .15], material(.5, 0, .12)); g.box(x - hx - .18, y + 4.1, z, .15, .2, .6, [.7, .2, .15], material(.5, 0, .12)); }
  }
  if (b.industrial) {
    g.box(x - hx - .08, y + 2, z + hz * .4, .1, 2, 3, [.14, .2, .22], METAL);
    for (let i = 0; i < 6; i++) g.box(x - hx - .2, y + .5 + i * .55, z + hz * .4, .05, .035, 3, [.32, .37, .37]);
    g.box(x + hx * .3, y + h + 1.5, z, 1.6, 1.4, 1.8, [.18, .22, .23], METAL);
  }
}

function roads(g, plan, level) {
  const { ox, oz } = plan;
  // Shared boundaries are half-road strips, so neighboring chunks join exactly.
  for (const x of [3.5, 76.5]) g.box(x, .012, 40, 3.5, .012, 40, ASPHALT, material(.94));
  for (const z of [3.5, 76.5]) g.box(40, .016, z, 40, .016, 3.5, ASPHALT, material(.94));
  for (const x of [8.6, 71.4]) g.box(x, .09, 40, 1.6, .09, 33, PAVING);
  for (const z of [8.6, 71.4]) g.box(40, .09, z, 33, .09, 1.6, PAVING);
  for (let t = 15; t < 70; t += 12) {
    g.box(.07, .035, t, .07, .012, 2.8, [.65, .57, .35]); g.box(t, .035, .07, 2.8, .012, .07, [.65, .57, .35]);
  }
  if (!level) for (let t = 1; t < 6; t++) {
    const s = 1 + t * 1.2;
    for (const z of [10.7, 40, 69.3]) { g.box(s, .045, z, .35, .014, 1.1, [.75, .75, .65]); g.box(80 - s, .045, z, .35, .014, 1.1, [.75, .75, .65]); }
    for (const x of [10.7, 40, 69.3]) { g.box(x, .045, s, 1.1, .014, .35, [.75, .75, .65]); g.box(x, .045, 80 - s, 1.1, .014, .35, [.75, .75, .65]); }
  }
  if (oz < -190) for (let t = 15; t < 70; t += 10) g.box(t, .04, 3.5, 2.2, .01, .07, [.65, .64, .52]);
}

export class CityGenerator {
  constructor({ replaced = new Set() } = {}) { this.replaced = replaced; }
  describe(cx, cz) { return describeChunk(cx, cz); }
  *build(cx, cz, level = 0, signal = null) {
    const plan = describeChunk(cx, cz), g = new GeometryBuilder(), proxy = new GeometryBuilder(), bodies = [], ground = new GeometryBuilder();
    const random = new Random(hash(cx, cz, 771));
    const green = plan.district.id === 'rural' ? [.23, .29, .16] : [.25, .29, .23];
    const steps = plan.district.id === 'rural' ? 8 : 1;
    for (let z = 0; z < steps; z++) for (let x = 0; x < steps; x++) {
      const a = x * 80 / steps, b = z * 80 / steps, c = (x + 1) * 80 / steps, d = (z + 1) * 80 / steps;
      const point = (px, pz) => new Vec3(px, terrainHeight(plan.ox + px, plan.oz + pz) - .03, pz);
      if (plan.ox + a < 436) ground.quad(point(a, b), point(a, d), point(c, d), point(c, b), green);
    }
    roads(g, plan, level); yield { phase: 'roads' };
    for (const b of plan.buildings) {
      if (signal?.aborted) return null;
      const y = terrainHeight(plan.ox + b.x, plan.oz + b.z);
      building(g, b, level, y);
      proxy.box(b.x, y + b.height * .5, b.z, b.hx, b.height * .5, b.hz, b.color);
      bodies.push({ id: b.id, x: plan.ox + b.x, y: y + b.height * .5, z: plan.oz + b.z, half: [b.hx, b.height * .5, b.hz] });
      yield { phase: 'building', id: b.id };
    }
    if (plan.publicSpace) {
      g.box(40, .04, 40, 26, .04, 26, [.49, .47, .4]);
      for (const t of [20, 40, 60]) { g.box(t, .085, 40, .04, .01, 26, [.31, .32, .29]); g.box(40, .085, t, 26, .01, .04, [.31, .32, .29]); }
      if (cx === 0 && cz === 0) {
        // Durbar Square: the temple is procedural unless a generated pagoda model is installed.
        if (!this.replaced.has('pagoda')) { pagoda(g, 47, 0, 47); bodies.push({ id: 'pagoda', x: plan.ox + 47, y: 2.5, z: plan.oz + 47, half: [5.2, 2.5, 5.2] }); }
        if (!this.replaced.has('chautari')) { chautari(g, 55, 0, 64, 7); bodies.push({ id: 'chautari-square', x: plan.ox + 55, y: .5, z: plan.oz + 64, half: [3.4, .5, 3.4] }); }
        g.box(26, 1.2, 25, .12, 1.2, 1.7, [.13, .17, .18]);
        g.box(26.14, 1.9, 25, .05, .42, 1.4, [.6, .49, .29]);
      }
      if (cx === -2 && cz === 2 && !this.replaced.has('stupa')) { stupa(g, 40, 0, 54); bodies.push({ id: 'stupa', x: plan.ox + 40, y: 3, z: plan.oz + 54, half: [8.6, 3, 8.6] }); }
      if (cx === 0 && cz === 4 && !this.replaced.has('chautari')) { const y = terrainHeight(plan.ox + 54, plan.oz + 32); chautari(g, 54, y, 32, 9); bodies.push({ id: 'chautari-lookout', x: plan.ox + 54, y: y + .5, z: plan.oz + 32, half: [3.4, .5, 3.4] }); }
      for (const [x, z] of [[19, 22], [61, 22], [19, 60], [61, 60]]) {
        const y = terrainHeight(plan.ox + x, plan.oz + z);
        g.box(x, y + .5, z, 1.8, .12, .48, [.25, .16, .09]); g.box(x, y + .95, z + .43, 1.8, .45, .08, [.25, .16, .09]);
        g.box(x - 1.3, y + .2, z, .08, .25, .35, [.08, .1, .1]); g.box(x + 1.3, y + .2, z, .08, .25, .35, [.08, .1, .1]);
      }
    }
    const count = plan.publicSpace ? 8 : plan.district.id === 'rural' ? 7 : 3;
    for (let i = 0; i < count; i++) {
      const x = plan.publicSpace ? (i % 2 ? 62 : 18) : random.pick([12.5, 67.5]), z = 15 + (i * 13 % 52);
      if (plan.ox + x < 430) tree(g, x, z, hash(cx, cz, i + 5), terrainHeight(plan.ox + x, plan.oz + z), !level);
    }
    if (plan.ox >= 400 && plan.ox < 440) {
      g.box(20, .09, 40, 14, .09, 33, [.5, .48, .4]);
      for (let z = 10; z < 80; z += 10) { g.box(34, .6, z, .08, .6, .08, [.13, .2, .22], METAL); g.box(34, 1.13, z + 5, .05, .05, 5, [.22, .28, .27], METAL); }
      bodies.push({ id: `seawall:${cz}`, x: plan.ox + 34, y: 1, z: plan.oz + 40, half: [.3, 1, 40] });
      if (cz % 2 === 0) { g.box(49, -.12, 40, 15, .18, 3.5, [.33, .25, .17]); for (const z of [37, 43]) for (const x of [38, 49, 60]) g.box(x, -1, z, .2, 1, .2, [.18, .16, .12]); }
    }
    yield { phase: 'details' };
    return { ...plan, mesh: g.build(), groundMesh: ground.build(), shadowMesh: proxy.build(), colliders: bodies, level };
  }
  buildSync(cx, cz, level = 0) { const it = this.build(cx, cz, level); let step; do { step = it.next(); } while (!step.done); return step.value; }
  createHorizon() {
    const g = new GeometryBuilder();
    for (let cz = WORLD_MIN; cz <= WORLD_MAX; cz++) for (let cx = WORLD_MIN; cx <= WORLD_MAX; cx++) {
      const p = describeChunk(cx, cz);
      for (const b of p.buildings) {
        const y = terrainHeight(p.ox + b.x, p.oz + b.z);
        g.box(p.ox + b.x, y + b.height * .5 - .1, p.oz + b.z, Math.max(.2, b.hx - .22), b.height * .5 - .1, Math.max(.2, b.hz - .22), b.color);
      }
    }
    return renderObject(g.build(), { castShadow: false });
  }
}
