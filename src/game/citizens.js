import { Vec3, Quat, clamp } from '../math.js';
import { Random, hash, poiPosition, POIS, distanceXZ, dampAngle, terrainHeight } from '../world/util.js';
import { material } from '../world/geometry.js';

// Ids stay stable for saves; voice selects the ElevenLabs cast in src/assets/catalog.js.
export const NAMED_CITIZENS = Object.freeze([
  { id: 'mira', name: 'Maya Gurung', role: 'Tole organizer', place: 'plaza', x: 34, z: 26, outfit: [.55, .08, .12], line: 'A tole is made by the people who show up for each other.' },
  { id: 'leo', topi: [.42, .1, .09], name: 'Laxman Thapa', role: 'Chiya pasal owner', place: 'cafe', x: 104, z: 38, outfit: [.1, .26, .22], line: 'My chiya is strong, but my regulars are stronger.' },
  { id: 'anika', name: 'Dr. Anjali Shrestha', role: 'Health post doctor', place: 'clinic', x: -56, z: 106, outfit: [.7, .74, .68], line: 'One small delivery can make a very big difference up here.' },
  { id: 'tess', name: 'Tashi Sherpa', role: 'Courier dispatcher', place: 'depot', x: 184, z: 106, outfit: [.2, .31, .4], line: 'A good courier is someone the whole tole can count on.' },
  { id: 'sanjay', name: 'Sanjay Rai', role: 'Ghat electrician', place: 'harbor', x: 344, z: 106, outfit: [.6, .36, .09], line: 'Monsoon damp gets into every wire. It keeps me employed.' },
  { id: 'rosa', name: 'Rasmila Maharjan', role: 'Bazaar shopkeeper', place: 'market', x: -216, z: 40, outfit: [.37, .16, .23], line: 'Newa Tole has its own rhythm. Take the time to notice it.' },
  { id: 'eli', name: 'Bikash Tamang', role: 'Stupa gardener', place: 'park', x: -120, z: 196, outfit: [.5, .14, .06], line: 'Walk around the stupa clockwise. A quiet mind is a useful thing to have.' },
  { id: 'juno', name: 'Junu Limbu', role: 'Photographer', place: 'waterfront', x: 410, z: 37, outfit: [.13, .22, .34], line: 'Watch the mountains turn gold when the sun gets low.' }
]);

export class CitizenSystem {
  constructor(graph, seed = 811) {
    this.graph = graph; this.agents = []; this.time = 0; this.planningBudget = 0;
    const random = new Random(seed), skins = [[.58, .36, .23], [.45, .27, .16], [.66, .46, .31], [.52, .33, .21], [.72, .52, .37]], outfits = [[.12, .22, .3], [.35, .19, .12], [.26, .29, .14], [.25, .17, .3], [.16, .32, .31], [.57, .44, .25]];
    const make = data => ({ ...data, position: new Vec3(data.x, terrainHeight(data.x, data.z) + .1, data.z), yaw: random.range(-Math.PI, Math.PI), speed: 0, walkSpeed: random.range(1, 1.45), phase: random.range(0, 6.28), skin: random.pick(skins), hair: random.pick([[.02, .018, .015], [.045, .035, .025], [.03, .025, .02], [.18, .16, .13]]), state: data.named ? 'Working' : 'Idle', path: [], pathIndex: 0, targetKey: '', destination: null, wait: random.range(1, 4), panic: 0, talking: false, relations: 0, thought: '', lastUpdate: 0 });
    for (const data of NAMED_CITIZENS) this.agents.push(make({ ...data, named: true }));
    const femaleNames = ['Sita', 'Sarita', 'Pema', 'Anita', 'Kabita', 'Dolma', 'Sunita', 'Pratima'], maleNames = ['Ram', 'Hari', 'Bishal', 'Suman', 'Prakash', 'Dawa', 'Rajesh', 'Nabin'];
    const firstNames = femaleNames.flatMap((f, i) => [f, maleNames[i]]);
    const lastNames = ['Shrestha', 'Gurung', 'Tamang', 'Magar', 'Adhikari', 'Rai', 'Karki', 'Bajracharya'];
    const jobs = ['Office worker', 'Shopkeeper', 'Nurse', 'Courier', 'Boatman', 'Student', 'Trekking guide', 'Tailor'];
    const destinations = ['cafe', 'clinic', 'depot', 'market', 'harbor', 'park'];
    const all = [...graph.nodes.values()].filter(n => n.cx >= -3 && n.cx <= 4 && n.cz >= -2 && n.cz <= 3);
    for (let i = 0; i < 72; i++) {
      const home = i < 12 ? graph.nodes.get(`0:0:${i % 8}`) : random.pick(all), job = random.pick(destinations), shift = i % 5 === 0 ? [18, 24] : [8, 17];
      const a = make({ id: `resident-${i}`, name: `${random.pick(firstNames)} ${random.pick(lastNames)}`, role: random.pick(jobs), place: job, x: home.position.x, z: home.position.z, outfit: random.pick(outfits), home: home.id, shift, named: false });
      a.voice = femaleNames.includes(a.name.split(' ')[0]) ? 'resident-f' : 'resident-m'; if (a.voice === 'resident-m' && random.next() < .55) a.topi = random.pick([[.42, .1, .09], [.04, .04, .045], [.22, .14, .3]]); a.leisure = random.pick(['park', 'plaza', 'waterfront', 'cafe']); a.localLoop = i < 12 ? i : null; this.agents.push(a);
    }
    this.visible = []; this.random = random;
  }
  get(id) { return this.agents.find(a => a.id === id); }
  setTalking(id, value) { const a = this.get(id); if (a) { a.talking = value; a.speed = 0; if (value) a.state = 'Talking'; } }
  panicAt(point, radius = 28) {
    for (const a of this.agents) if (distanceXZ(a.position, point) < radius) {
      a.panic = 7; a.targetKey = ''; a.path = []; a.fleeFrom = point.clone(); a.thought = 'Careful!';
    }
  }
  plan(a, target, key) {
    if (a.targetKey === key) return;
    if (this.planningBudget <= 0) return;
    this.planningBudget--; a.targetKey = key; a.destination = target.clone();
    a.path = this.graph.route(a.position, target); a.path.push(target.clone()); a.pathIndex = 0;
  }
  update(dt, minutes, weather, playerPosition, playerCar = null, traffic = []) {
    this.time += dt; this.planningBudget = 4; const hour = minutes / 60;
    const near = this.agents.map(a => ({ a, distance: distanceXZ(a.position, playerPosition) })).sort((a, b) => a.distance - b.distance);
    this.visible = near.filter(x => x.distance < 160).slice(0, 44).map(x => x.a);
    const active = new Set(this.visible);
    for (const a of this.agents) {
      if (!active.has(a) && this.time - a.lastUpdate < .6) continue;
      const step = Math.min(.7, Math.max(dt, this.time - a.lastUpdate)); a.lastUpdate = this.time;
      if (a.talking) { a.state = 'Talking'; a.speed = 0; a.yaw = dampAngle(a.yaw, Math.atan2(-(playerPosition.x - a.position.x), -(playerPosition.z - a.position.z)), step * 4); continue; }
      if (playerCar && playerCar.velocity.length() > 4 && distanceXZ(a.position, playerCar.position) < 7) { a.panic = Math.max(a.panic, 2); a.fleeFrom = playerCar.position.clone(); }
      if (active.has(a)) for (const car of traffic) if (car.speed > 3 && distanceXZ(car.position, a.position) < 3.5) { a.panic = Math.max(a.panic, 2); a.fleeFrom = car.position.clone(); }
      a.panic = Math.max(0, a.panic - step);
      if (a.panic > 0) {
        a.state = 'Fleeing'; const away = Vec3.sub(a.position, a.fleeFrom || playerPosition).normalize();
        const target = this.graph.nearest(a.position.clone().addScaled(away, 24)).position;
        this.plan(a, target, `flee:${Math.floor(this.time / 4)}`);
      } else if (a.named) { const open = { cafe: [7,22], clinic: [6,23], harbor: [6,22], market: [8,22], park: [6,20], plaza: [7,22] }[a.place] || [0,24]; a.state = weather.rain > .55 ? 'Sheltering' : hour >= open[0] && hour < open[1] ? 'Working' : 'Idle'; a.speed = 0; a.phase += step; continue; }
      else if (weather.rain > .55) { a.state = 'Sheltering'; const p = poiPosition(a.place); this.plan(a, p, `rain:${a.place}`); }
      else if (a.localLoop !== null && hour > 8 && hour < 21) { const i = (a.localLoop + Math.floor(this.time / 28) + 1) % 8; a.state = 'Walking'; this.plan(a, this.graph.nodes.get(`0:0:${i}`).position, `square:${i}`); }
      else if (hour >= a.shift[0] && hour < a.shift[1]) { a.state = 'Commuting'; this.plan(a, poiPosition(a.place), `work:${a.place}`); }
      else if (hour >= 17 && hour < 21) { a.state = 'Walking'; this.plan(a, poiPosition(a.leisure), `leisure:${a.leisure}`); }
      else { a.state = 'Walking'; this.plan(a, this.graph.nodes.get(a.home).position, `home:${a.home}`); }
      let target = a.path[a.pathIndex];
      while (target && distanceXZ(a.position, target) < .35) { a.pathIndex++; target = a.path[a.pathIndex]; }
      if (!target) { a.speed = 0; a.phase += step; a.state = a.panic > 0 ? 'Idle' : a.targetKey.startsWith('work:') ? 'Working' : a.targetKey.startsWith('rain:') ? 'Sheltering' : 'Idle'; continue; }
      const desired = Vec3.sub(target, a.position); desired.y = 0; const distance = desired.length(); desired.normalize();
      if (active.has(a) && a.state !== 'Fleeing') {
        const roadDistance = v => { const m = (v % 80 + 80) % 80; return Math.min(m, 80 - m); };
        const current = Math.min(roadDistance(a.position.x), roadDistance(a.position.z)), next = a.position.clone().addScaled(desired, 1);
        if (current > 7 && Math.min(roadDistance(next.x), roadDistance(next.z)) < current && traffic.some(car => car.speed > 2 && distanceXZ(car.position, a.position) < 7)) { a.state = 'Waiting'; a.speed = 0; continue; }
      }
      const separation = new Vec3();
      if (active.has(a)) for (const b of this.visible) {
        if (a === b) continue; const offset = Vec3.sub(a.position, b.position); offset.y = 0; const d = offset.length(); if (d > .01 && d < 1.05) separation.addScaled(offset, (1.05 - d) / d * 1.5);
      }
      const away = Vec3.sub(a.position, playerPosition); away.y = 0; const pd = away.length(); if (pd > .01 && pd < 1.0) separation.addScaled(away, (1 - pd) / pd * 2.5);
      const move = desired.clone().add(separation).normalize(), speed = a.walkSpeed * (a.state === 'Fleeing' ? 2.9 : 1);
      const amount = Math.min(distance, speed * step); a.position.addScaled(move, amount); a.position.y = terrainHeight(a.position.x, a.position.z) + .15;
      a.yaw = dampAngle(a.yaw, Math.atan2(-move.x, -move.z), step * 8); a.speed = amount / step; a.phase += amount * 5.7;
    }
  }
  nearest(position, radius = 3.2) { let best = null, distance = radius; for (const a of this.visible) { const d = distanceXZ(a.position, position); if (d < distance) { best = a; distance = d; } } return best; }
}

export function drawPerson(boxes, spheres, person, time = 0, player = false) {
  const p = person.position, root = Quat.axisAngle(new Vec3(0, 1, 0), person.yaw), moving = person.speed > .12, phase = person.phase || 0;
  const stride = Math.sin(phase) * (person.speed > 4 ? 1.2 : .9), runBoost = Math.min(1, Math.max(0, person.speed / 6));
  const bob = moving ? Math.abs(Math.sin(phase)) * (0.045 + runBoost * 0.04) : Math.sin(time * 1.25 + p.x) * .008;
  const outfit = person.outfit || [.65, .24, .075], skin = person.skin || [.58, .36, .23], pants = player ? [.045, .08, .12] : [.07, .09, .105];
  const jacket = player ? [.17, .23, .26] : outfit;
  const part = (x, y, z, hx, hy, hz, color, local = new Quat(), surf = material(.85)) => {
    const position = root.rotate(new Vec3(x, y + bob, z)).add(p); boxes.add(position, Quat.mul(root, local), new Vec3(hx, hy, hz), color, surf);
  };

  const torsoLean = moving ? Math.sin(phase * 1.8) * 0.14 : 0;
  const upperTorso = Quat.axisAngle(new Vec3(1, 0, 0), torsoLean * 0.9);
  part(0, 1.12, 0, .26, .38, .19, jacket, upperTorso);
  part(0, 1.18, -.08, .18, .26, .08, outfit, upperTorso);
  part(0, 1.32, .03, .12, .13, .12, [.08, .11, .16], upperTorso);
  part(0, 1.06, .11, .16, .09, .1, [.14, .18, .2], upperTorso);
  part(0, 1.38, .15, .12, .08, .05, [.12, .15, .2], upperTorso);
  part(0, 1.34, -.15, .14, .07, .05, [.04, .06, .08], upperTorso);
  part(0, .78, .02, .24, .1, .14, pants);
  part(0, .94, .1, .2, .12, .1, [.05, .06, .08]);
  part(0, 1.32, -.14, .15, .06, .05, [.03, .04, .06]);
  part(-.16, 1.18, .15, .08, .15, .06, outfit, upperTorso);
  part(.16, 1.18, .15, .08, .15, .06, outfit, upperTorso);

  const headCenter = root.rotate(new Vec3(0, 1.7 + bob, 0)).add(p);
  spheres.add(headCenter, root, new Vec3(.18, .22, .17), skin, material(.92));
  part(0, 1.52, 0, .09, .18, .1, skin);
  part(0, 1.93, .015, .17, .085, .17, person.hair || [.08, .07, .06]);
  if (person.topi) { part(0, 2.04, .015, .165, .075, .16, person.topi); part(0, 2.11, .01, .12, .03, .14, person.topi.map(c => c * 1.4)); } // dhaka / bhadgaunle topi
  part(0, 1.78, .13, .08, .06, .045, skin);
  part(0, 1.66, -.178, .05, .06, .04, skin);
  part(-.09, 1.74, -.085, .028, .05, .018, [.08, .08, .09]);
  part(.09, 1.74, -.085, .028, .05, .018, [.08, .08, .09]);
  part(0, 1.63, -.19, .032, .04, .022, [.14, .09, .08]);
  part(0, 1.59, -.19, .032, .028, .028, skin);
  part(-.17, 1.7, .02, .025, .04, .018, skin);
  part(.17, 1.7, .02, .025, .04, .018, skin);
  part(0, 1.88, .05, .11, .047, .05, [.13, .08, .05]);
  part(-.19, 1.76, .01, .02, .12, .02, skin);
  part(.19, 1.76, .01, .02, .12, .02, skin);
  part(-.06, 1.8, .12, .018, .04, .01, [.9, .94, .96]);
  part(.06, 1.8, .12, .018, .04, .01, [.9, .94, .96]);
  part(0, 1.68, -.1, .04, .07, .02, [.2, .14, .13]);
  part(0, 1.52, .12, .07, .04, .02, [.13, .08, .07]);
  part(0, 1.48, 0, .05, .12, .09, [.18, .22, .26]);

  for (const side of [-1, 1]) {
    const swing = moving ? stride + (side < 0 ? .18 : 0) : 0;
    const leg = Quat.axisAngle(new Vec3(1, 0, 0), swing * 0.82), knee = Quat.axisAngle(new Vec3(1, 0, 0), Math.max(0, -swing) * 1.2);
    const hip = new Vec3(side * .11, .75, 0), upper = leg.rotate(new Vec3(0, -.18, 0)).add(hip), footJoint = leg.rotate(new Vec3(0, -.42, 0)).add(hip), lowerRotation = Quat.mul(leg, knee), lower = lowerRotation.rotate(new Vec3(0, -.18, 0)).add(footJoint), shoe = lowerRotation.rotate(new Vec3(0, -.34, -.04)).add(footJoint);
    part(upper.x, upper.y, upper.z, .083, .18, .088, pants, leg);
    part(lower.x, lower.y, lower.z, .076, .17, .074, pants, lowerRotation);
    part(shoe.x, Math.max(.07, shoe.y), shoe.z, .09, .065, .135, [.03, .04, .05], lowerRotation);
    part(hip.x + side * .07, .58, .06, .06, .15, .07, [.06, .07, .08], leg);
    part(hip.x + side * .02, .32, .08, .06, .12, .09, [.04, .05, .06], lowerRotation);

    const working = person.state === 'Working' && !player;
    const armAngle = moving ? (side === -1 ? -0.9 : 0.9) * (0.4 + runBoost * 0.5) + Math.sin(phase * 1.6 + side) * 0.32 : working ? -.48 - Math.sin(time * 1.6 + side) * .12 : .08;
    const arm = Quat.axisAngle(new Vec3(1, 0, 0), armAngle), shoulder = new Vec3(side * .29, 1.28, 0), upperArm = arm.rotate(new Vec3(0, -.15, 0)).add(shoulder), hand = arm.rotate(new Vec3(0, -.42, -.02)).add(shoulder), forearm = arm.rotate(new Vec3(0.02, -.28, .03)).add(shoulder);
    part(upperArm.x, upperArm.y, upperArm.z, .066, .17, .077, jacket, arm);
    part(forearm.x, forearm.y + .04, forearm.z, .055, .13, .07, outfit, arm);
    part(hand.x, hand.y + .06, hand.z, .065, .11, .065, skin, arm);
    part(shoulder.x + side * .04, 1.18, .08, .05, .11, .08, [.1, .13, .18], arm);
    if (working && side === 1) part(hand.x, hand.y - .02, hand.z - .08, .065, .09, .065, [.58, .53, .4], arm);
  }

  if (player) {
    part(0, 1.2, .22, .18, .22, .08, [.08, .18, .22]);
    part(0, 1.38, -.14, .12, .04, .02, [.75, .57, .3]);
    part(0, 1.68, -.12, .09, .1, .03, skin);
    part(0, .96, .02, .2, .1, .16, [.06, .12, .17]);
    part(0, 1.22, .14, .13, .08, .04, [.14, .18, .2]);
    part(0, 1.07, -.12, .08, .07, .04, [.09, .13, .16]);
  }
}
