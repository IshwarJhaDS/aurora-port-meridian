// Procedural Nepali landmarks. These always ship with the game; when a matching
// Tripo model has been generated, the generated model replaces them.
import { Vec3 } from '../math.js';
import { material, PRAYER_COLORS } from './geometry.js';
import { Random } from './util.js';

const BRICK = [.42, .14, .075], BRICK_DARK = [.3, .1, .055], WOOD = [.11, .055, .028], STONE = [.48, .45, .4];
const GOLD = [.72, .5, .14], GILT = material(.28, .9), COPPER = [.36, .2, .09], WHITE = [.86, .85, .8];

/** Three-tier Newari pagoda standing on a nine-step plinth, centred at (x, y, z). About 15 m tall. */
export function pagoda(g, x, y, z) {
  let top = y;
  for (let i = 0; i < 5; i++) { const h = .42, half = 6 - i * .55; g.box(x, top + h / 2, z, half, h / 2, half, i % 2 ? BRICK_DARK : STONE, material(.9)); top += h; }
  // Ground floor: brick sanctum wrapped by a carved wooden colonnade.
  g.box(x, top + 1.7, z, 2.7, 1.7, 2.7, BRICK, material(.85));
  for (const side of [-1, 1]) for (let k = -2; k <= 2; k++) {
    g.box(x + side * 3.25, top + 1.5, z + k * 1.3, .14, 1.5, .14, WOOD); g.box(x + k * 1.3, top + 1.5, z + side * 3.25, .14, 1.5, .14, WOOD);
  }
  for (const [dx, dz] of [[0, 2.72], [0, -2.72], [2.72, 0], [-2.72, 0]]) g.box(x + dx, top + 1.5, z + dz, dx ? .06 : .8, 1.1, dz ? .06 : .8, WOOD, material(.6));
  top += 3.4;
  const tiers = [[4.6, 1.9, 2.1, 2.2], [3.5, 1.6, 1.6, 1.8], [2.5, 1.5, 0, 0]];
  for (const [roof, rise, body, bodyHeight] of tiers) {
    g.box(x, top + .12, z, roof * .78, .12, roof * .78, WOOD);
    g.pyramid(x, top + .2, z, roof, rise, roof, COPPER, material(.45, .55), roof * .32, roof * .32);
    // Struts under the eaves and small bells at the corners.
    for (const [sx, sz] of [[-1, -1], [1, -1], [1, 1], [-1, 1]]) g.sphere(x + sx * roof * .95, top - .05, z + sz * roof * .95, .12, .16, .12, GOLD, GILT, 6);
    top += .2 + rise;
    if (body) { g.box(x, top + bodyHeight / 2, z, body, bodyHeight / 2, body, BRICK, material(.85)); for (const s of [-1, 1]) { g.box(x + s * (body + .02), top + bodyHeight / 2, z, .05, .45, .55, WOOD); g.box(x, top + bodyHeight / 2, z + s * (body + .02), .55, .45, .05, WOOD); } top += bodyHeight; }
  }
  g.pyramid(x, top, z, .8, .5, .8, GOLD, GILT, .3, .3);
  g.sphere(x, top + 1.0, z, .38, .55, .38, GOLD, GILT, 8); g.cylinder(x, top + 1.9, z, .07, .5, GOLD, GILT, 6); g.sphere(x, top + 2.45, z, .16, .2, .16, GOLD, GILT, 6);
  // Flags strung from the finial to the square, as at every Durbar Square festival.
  const crown = new Vec3(x, top + 1.6, z);
  for (const [dx, dz] of [[-15, -15], [15, -15], [-15, 15], [15, 15]]) g.prayerFlags(crown, new Vec3(x + dx, y + 4.5, z + dz), 1.6, .55);
  return top + 2.6 - y;
}

/** Boudhanath-style stupa with Buddha eyes, centred at (x, y, z). About 16 m tall. */
export function stupa(g, x, y, z) {
  let top = y;
  for (let i = 0; i < 3; i++) { const half = 9 - i * 1.4, h = .75; g.box(x, top + h / 2, z, half, h / 2, half, WHITE, material(.85)); g.box(x, top + h - .04, z, half + .05, .05, half + .05, [.62, .2, .1]); top += h; }
  g.sphere(x, top, z, 5.6, 4.4, 5.6, WHITE, material(.85), 20);
  top += 4.2;
  g.box(x, top + 1.3, z, 1.6, 1.3, 1.6, GOLD, GILT);
  for (const [dx, dz, yaw] of [[0, 1, 0], [0, -1, 0], [1, 0, Math.PI / 2], [-1, 0, Math.PI / 2]]) {
    const fx = x + dx * 1.62, fz = z + dz * 1.62, side = (u, w) => [fx + (dz ? u : 0), fz + (dx ? u : 0)];
    for (const u of [-.55, .55]) { const [ex, ez] = side(u); g.box(ex, top + 1.55, ez, .34, .13, .03, [.95, .95, .92], material(.6), yaw); g.box(ex, top + 1.55, ez, .12, .12, .04, [.03, .03, .05], material(.6), yaw); g.box(ex, top + 1.85, ez, .38, .05, .04, [.03, .03, .05], material(.6), yaw); }
    const [nx, nz] = side(0); g.box(nx, top + 1.05, nz, .1, .22, .04, [.03, .03, .05], material(.6), yaw);
  }
  top += 2.6;
  for (let i = 0; i < 13; i++) { const r = 1.25 - i * .07; g.cylinder(x, top + .17, z, r, .17, GOLD, GILT, 12); top += .36; }
  g.cylinder(x, top + .1, z, 1.0, .1, GOLD, GILT, 12); g.sphere(x, top + .55, z, .32, .5, .32, GOLD, GILT, 8);
  const crown = new Vec3(x, top - 1, z);
  for (let i = 0; i < 8; i++) { const a = i * Math.PI / 4 + Math.PI / 8; g.prayerFlags(crown, new Vec3(x + Math.cos(a) * 15, y + 2.2, z + Math.sin(a) * 15), 2.2, .55); }
  return top + 1 - y;
}

/** Stone chautari platform around a peepal tree. */
export function chautari(g, x, y, z, seed = 1) {
  const r = new Random(seed);
  g.box(x, y + .35, z, 3.6, .35, 3.6, STONE, material(.95)); g.box(x, y + .9, z, 2.6, .2, 2.6, [.44, .41, .36], material(.95));
  g.box(x, y + 2.6, z, .45, 2, .45, [.28, .2, .13]);
  for (let i = 0; i < 5; i++) g.sphere(x + r.range(-2, 2), y + 5.6 + r.range(-.5, 1.2), z + r.range(-2, 2), r.range(2.2, 3), r.range(1.6, 2.2), r.range(2.2, 3), r.pick([[.13, .27, .1], [.17, .31, .11], [.11, .23, .09]]), material(.95), 8);
  g.prayerFlags(new Vec3(x, y + 4.5, z), new Vec3(x + 6, y + 2.6, z + 4), .6);
}

/** Painted wooden doonga as on Phewa Lake. */
export function doonga(g, x, y, z, yaw, seed = 1) {
  const colors = [[.6, .06, .04], [.04, .2, .55], [.8, .55, .04], [.05, .38, .2]], r = new Random(seed), hull = r.pick(colors), trim = r.pick(colors);
  g.box(x, y, z, .75, .3, 3.4, hull, material(.55), yaw); g.box(x, y + .32, z, .8, .05, 3.45, trim, material(.55), yaw);
  g.box(x, y + .05, z, .6, .26, 3.2, [.3, .19, .1], material(.8), yaw);
  for (const t of [-1.2, 0, 1.2]) g.box(x + Math.sin(yaw) * t, y + .25, z + Math.cos(yaw) * t, .62, .04, .2, [.36, .23, .12], material(.8), yaw);
}

/**
 * Himalaya backdrop and the forested hills that ring the valley. Distances are
 * beyond the playable area and inside the camera far plane.
 */
export function landscape(g) {
  const ROCK = [.27, .26, .26], FOREST = [.1, .17, .09], SNOW = [.9, .92, .96], SNOW_SURFACE = material(.55, 0, 0, 0), ROCK_SURFACE = material(.95, 0, 0, 0);
  const peak = (x, z, radius, height, seed, snowLine = .55) => {
    const r = new Random(seed), sides = 7, jag = Array.from({ length: sides }, () => r.range(.75, 1.2)), twist = r.range(0, 6.28);
    const ring = (t, y) => Array.from({ length: sides }, (_, i) => { const a = twist + i * Math.PI * 2 / sides, k = radius * t * jag[i]; return new Vec3(x + Math.cos(a) * k, y, z + Math.sin(a) * k); });
    const levels = [[1, -2], [.62, height * .32], [.4, height * Math.min(snowLine, .6)], [.2, height * .8], [0, height]], centre = new Vec3(x, height * .3, z), snowy = snowLine < 1;
    for (let l = 0; l < levels.length - 1; l++) {
      const a = ring(...levels[l]), b = ring(...levels[l + 1]), color = l === 0 ? FOREST : !snowy ? (l === 1 ? FOREST : [.16, .22, .1]) : l === 1 ? ROCK : SNOW, surface = snowy && l >= 2 ? SNOW_SURFACE : ROCK_SURFACE;
      for (let i = 0; i < sides; i++) { const j = (i + 1) % sides; g.facing(centre, a[i], a[j], b[j], b[i], color, surface); }
    }
  };
  // Annapurna-like range to the north, with one sharp "fishtail" summit.
  const r = new Random(4242);
  for (let x = -1500; x <= 1500; x += 170) peak(x + r.range(-50, 50), -1350 + r.range(-120, 80), r.range(170, 260), r.range(330, 520), 900 + x);
  peak(260, -1180, 150, 720, 77, .45); peak(-420, -1250, 230, 610, 78, .5); peak(780, -1300, 260, 640, 79, .5);
  // The range continues behind the lake to the east.
  for (let z = -1100; z <= 900; z += 200) peak(1420 + r.range(-60, 60), z, r.range(200, 280), r.range(320, 560), 1300 + z);
  // Green foothills to the west and south, closer and without snow.
  const hill = (x, z, radius, height, seed) => peak(x, z, radius, height, seed, 2);
  for (let z = -900; z <= 1000; z += 160) hill(-900 + r.range(-80, 80), z, r.range(180, 260), r.range(70, 150), 2000 + z);
  for (let x = -800; x <= 900; x += 170) hill(x, 1050 + r.range(-80, 80), r.range(180, 260), r.range(80, 170), 3000 + x);
  // Ground that carries the eye from the city edge to the mountains.
  const flat = [.2, .25, .14];
  for (const [x0, z0, x1, z1] of [[-1600, -1600, 436, -480], [-1600, -480, -480, 1300], [-1600, 560, 436, 1300], [436, -1600, 1046, -660], [436, 740, 1046, 1300], [1046, -1600, 1800, 1300]]) {
    const p = (x, z) => new Vec3(x, -.25, z);
    g.facing(new Vec3((x0 + x1) / 2, -10, (z0 + z1) / 2), p(x0, z0), p(x1, z0), p(x1, z1), p(x0, z1), flat, ROCK_SURFACE);
  }
}

export { PRAYER_COLORS };
