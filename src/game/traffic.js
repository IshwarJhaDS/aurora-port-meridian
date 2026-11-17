import { Vec3, Quat, clamp } from '../math.js';
import { CubicBezier } from '../world/navigation.js';
import { Random, distanceXZ } from '../world/util.js';
import { material } from '../world/geometry.js';

export function trafficSignal(x, z, seconds, axis = 'x') {
  const offset = ((Math.round(x / 80) * 3 + Math.round(z / 80) * 5) % 7 + 7) % 7;
  const phase = (seconds + offset) % 28;
  if (axis === 'x') return phase < 11 ? 'green' : phase < 13 ? 'amber' : 'red';
  return phase >= 14 && phase < 25 ? 'green' : phase >= 25 && phase < 27 ? 'amber' : 'red';
}

export class RoadLoop {
  constructor(x0, z0, x1, z1, reverse = false) {
    const lane = reverse ? -3.1 : 3.1;
    const corners = reverse ? [[x0 + lane, z0 + lane], [x0 + lane, z1 - lane], [x1 - lane, z1 - lane], [x1 - lane, z0 + lane]] : [[x0 + lane, z0 + lane], [x1 - lane, z0 + lane], [x1 - lane, z1 - lane], [x0 + lane, z1 - lane]];
    this.segments = []; this.starts = []; this.length = 0;
    for (let i = 0; i < 4; i++) {
      const prev = Vec3.from([corners[(i + 3) % 4][0], .72, corners[(i + 3) % 4][1]]), corner = Vec3.from([corners[i][0], .72, corners[i][1]]), next = Vec3.from([corners[(i + 1) % 4][0], .72, corners[(i + 1) % 4][1]]);
      const inDirection = Vec3.sub(corner, prev).normalize(), outDirection = Vec3.sub(next, corner).normalize(), entry = corner.clone().addScaled(inDirection, -5), exit = corner.clone().addScaled(outDirection, 5), nextEntry = next.clone().addScaled(outDirection, -5);
      const curve = new CubicBezier(entry, entry.clone().addScaled(inDirection, 2.76), exit.clone().addScaled(outDirection, -2.76), exit);
      const line = new CubicBezier(exit, exit.clone().addScaled(Vec3.sub(nextEntry, exit), 1 / 3), exit.clone().addScaled(Vec3.sub(nextEntry, exit), 2 / 3), nextEntry, 4);
      for (const segment of [curve, line]) { this.starts.push(this.length); this.segments.push(segment); this.length += segment.length; }
    }
  }
  sample(distance) {
    const d = ((distance % this.length) + this.length) % this.length; let i = this.segments.length - 1;
    for (let j = 0; j < this.starts.length - 1; j++) if (d < this.starts[j + 1]) { i = j; break; }
    const segment = this.segments[i], t = segment.parameterAtDistance(d - this.starts[i]);
    return { position: segment.at(t), direction: segment.tangent(t), turning: i % 2 === 0 };
  }
}

export class TrafficSystem {
  constructor(seed = 990) {
    this.random = new Random(seed); this.elapsed = 0; this.cars = [];
    this.routes = [[-160, -160, 160, 160], [-240, 0, 80, 240], [0, -80, 320, 160], [-80, -240, 240, 0]].flatMap(rect => [new RoadLoop(...rect), new RoadLoop(...rect, true)]);
    const colors = [[.08, .18, .23], [.55, .51, .41], [.26, .36, .26], [.48, .13, .07], [.12, .14, .16], [.6, .62, .56], [.2, .28, .35]];
    for (let i = 0; i < 32; i++) {
      const route = this.routes[i % this.routes.length], distance = route.length * ((Math.floor(i / 8) + .25 + (i % 3) * .08) / 4), sampled = route.sample(distance);
      this.cars.push({ id: `traffic-${i}`, route, distance, position: sampled.position, direction: sampled.direction, speed: this.random.range(3, 7), desiredSpeed: this.random.range(7, 11), color: this.random.pick(colors), brake: false, yaw: 0, wheel: 0 });
    }
    this.visible = [];
  }
  update(dt, player, playerCar = null, pedestrians = []) {
    this.elapsed += dt;
    for (const car of this.cars) {
      const sample = car.route.sample(car.distance); car.position = sample.position; car.direction = sample.direction;
      let target = car.desiredSpeed * (sample.turning ? .6 : 1);
      const axis = Math.abs(car.direction.x) > Math.abs(car.direction.z) ? 'x' : 'z', direction = car.direction[axis] >= 0 ? 1 : -1;
      const coordinate = car.position[axis], intersection = direction > 0 ? Math.ceil((coordinate + .001) / 80) * 80 : Math.floor((coordinate - .001) / 80) * 80;
      const distanceToLine = (intersection - coordinate) * direction - 10;
      const sx = axis === 'x' ? intersection : Math.round(car.position.x / 80) * 80, sz = axis === 'z' ? intersection : Math.round(car.position.z / 80) * 80;
      if (!sample.turning && distanceToLine > -1 && distanceToLine < 30 && trafficSignal(sx, sz, this.elapsed, axis) !== 'green') target = Math.min(target, Math.sqrt(2 * 4 * Math.max(0, distanceToLine)));
      for (const other of this.cars) {
        if (other === car) continue;
        const offset = Vec3.sub(other.position, car.position), ahead = offset.dot(car.direction), side = Math.abs(offset.x * car.direction.z - offset.z * car.direction.x);
        if (ahead > 0 && ahead < 24 && side < 2.5) target = Math.min(target, Math.sqrt(Math.max(0, ahead - 5) * 4));
      }
      for (const obstacle of [player, playerCar?.position, ...pedestrians.map(a => a.position)].filter(Boolean)) {
        const offset = Vec3.sub(obstacle, car.position), ahead = offset.dot(car.direction), side = Math.abs(offset.x * car.direction.z - offset.z * car.direction.x);
        if (ahead > -1 && ahead < 19 && side < 2.4) target = Math.min(target, Math.sqrt(Math.max(0, ahead - 4) * 4));
      }
      const before = car.speed; car.speed = clamp(car.speed + clamp(target - car.speed, -5 * dt, 2.1 * dt), 0, 14); car.brake = car.speed < before - .02 || car.speed < .25;
      car.distance = (car.distance + car.speed * dt) % car.route.length;
      const after = car.route.sample(car.distance); car.position = after.position; car.direction = after.direction; car.yaw = Math.atan2(-after.direction.x, -after.direction.z); car.wheel += car.speed * dt / .34;
    }
    this.visible = this.cars.filter(c => distanceXZ(c.position, player) < 220);
  }
}

export function drawCar(boxes, spheres, car, { taxi = false, wheels = null, headlights = false } = {}) {
  const position = car.position, rotation = car.rotation || Quat.axisAngle(new Vec3(0, 1, 0), car.yaw), color = car.color || [.025, .12, .17];
  const part = (x, y, z, hx, hy, hz, c, surface = material(.24, .65)) => boxes.add(rotation.rotate(new Vec3(x, y, z)).add(position), rotation, new Vec3(hx, hy, hz), c, surface);
  part(0, 0, 0, .86, .27, 1.8, color); part(0, .35, .14, .71, .26, .92, [.035, .075, .09], material(.12, .65));
  part(0, .63, .21, .7, .035, .79, color); part(0, .15, -1.29, .82, .09, .45, color);
  part(0, -.17, -1.81, .74, .05, .04, [.06, .08, .08], material(.35, .85));
  for (const x of [-.6, .6]) {
    part(x, .04, -1.815, .17, .065, .025, [.78, .66, .39], material(.18, .2, headlights ? 3 : .15));
    part(x, .04, 1.815, .17, .065, .025, [.55, .035, .02], material(.2, .2, car.brake ? 3 : .15));
    part(x * 1.48, .33, -.45, .08, .07, .15, color);
  }
  if (taxi) part(0, .78, .1, .29, .1, .16, [.85, .61, .18], material(.6, .1, .2));
  if (wheels) for (const w of wheels) spheres.add(w.position, w.rotation || rotation, new Vec3(.16, .34, .34), [.025, .03, .033], material(.9));
  else for (const x of [-.86, .86]) for (const z of [-1.23, 1.23]) {
    const r = Quat.mul(rotation, Quat.axisAngle(new Vec3(1, 0, 0), car.wheel || 0));
    spheres.add(rotation.rotate(new Vec3(x, -.29, z)).add(position), r, new Vec3(.17, .34, .34), [.025, .03, .033], material(.9));
  }
}
