import { Vec3, Quat, Mat4, AABB, clamp, lerp } from '../math.js';
import { terrainHeight, dampAngle, distanceXZ } from '../world/util.js';

export class CityPlayer {
  constructor(world, position = new Vec3(34, 0, 42)) {
    this.world = world; this.position = position.clone(); this.position.y = terrainHeight(position.x, position.z) + .1;
    this.yaw = Math.PI; this.speed = 0; this.phase = 0; this.verticalSpeed = 0; this.grounded = true; this.inVehicle = false; this.jumpLatch = false;
    this.outfit = [.14, .2, .28]; this.skin = [.62, .38, .27]; this.hair = [.13, .1, .08]; this.state = 'Idle';
  }
  moveAxis(delta, axis) {
    if (!delta) return;
    const desired = this.position.clone(); desired[axis] += delta;
    const bounds = new AABB(new Vec3(desired.x - .32, desired.y + .15, desired.z - .32), new Vec3(desired.x + .32, desired.y + 1.72, desired.z + .32));
    this.world.bvh.query(bounds, body => {
      if (body.shape !== 'box') return;
      const b = body.bounds;
      if (this.position.y >= b.max.y - .28 && b.max.y - this.position.y <= .38) { desired.y = Math.max(desired.y, b.max.y + .02); return; }
      if (b.max.y <= desired.y + .15 || b.min.y >= desired.y + 1.72) return;
      const low = b.min[axis] - .33, high = b.max[axis] + .33;
      const other = axis === 'x' ? 'z' : 'x';
      if (desired[other] <= b.min[other] - .29 || desired[other] >= b.max[other] + .29) return;
      if (desired[axis] > low && desired[axis] < high) desired[axis] = delta > 0 ? low : high;
    });
    this.position.copy(desired);
  }
  update(dt, keys, cameraYaw, enabled = true) {
    if (this.inVehicle) return;
    const input = enabled ? new Vec3((keys.has('KeyD') || keys.has('ArrowRight') ? 1 : 0) - (keys.has('KeyA') || keys.has('ArrowLeft') ? 1 : 0), 0, (keys.has('KeyS') || keys.has('ArrowDown') ? 1 : 0) - (keys.has('KeyW') || keys.has('ArrowUp') ? 1 : 0)) : new Vec3();
    input.normalize(); const direction = Quat.axisAngle(new Vec3(0, 1, 0), cameraYaw).rotate(input), sprint = keys.has('ShiftLeft') || keys.has('ShiftRight'), crouch = keys.has('KeyC');
    const desiredSpeed = input.lengthSq() ? crouch ? 1.25 : sprint ? 6.2 : 3 : 0;
    this.speed = lerp(this.speed, desiredSpeed, 1 - Math.exp(-dt * 12));
    this.moveAxis(direction.x * this.speed * dt, 'x'); this.moveAxis(direction.z * this.speed * dt, 'z');
    this.position.x = clamp(this.position.x, -468, 431); this.position.z = clamp(this.position.z, -468, 548);
    if (input.lengthSq()) this.yaw = dampAngle(this.yaw, Math.atan2(-direction.x, -direction.z), dt * 12);
    const jump = enabled && keys.has('Space');
    if (jump && !this.jumpLatch && this.grounded) { this.verticalSpeed = 4.4; this.grounded = false; }
    this.jumpLatch = jump;
    this.verticalSpeed -= 9.81 * dt; this.position.y += this.verticalSpeed * dt;
    let floor = terrainHeight(this.position.x, this.position.z) + .1;
    const support = new AABB(this.position.clone().add(new Vec3(-.25, -.45, -.25)), this.position.clone().add(new Vec3(.25, .38, .25)));
    if (this.verticalSpeed <= 0) this.world.bvh.query(support, body => { const b = body.bounds; if (b.max.y < this.position.y + .38 && b.max.y > floor && this.position.x > b.min.x - .1 && this.position.x < b.max.x + .1 && this.position.z > b.min.z - .1 && this.position.z < b.max.z + .1) floor = b.max.y + .02; });
    if (this.position.y <= floor) { this.position.y = floor; this.verticalSpeed = 0; this.grounded = true; }
    this.state = this.speed > .2 ? 'Walking' : 'Idle'; this.phase += this.speed * dt * 5;
  }
  enter(vehicle) { if (distanceXZ(this.position, vehicle.body.position) > 4.5 || vehicle.body.velocity.length() > 2) return false; this.inVehicle = true; return true; }
  exit(vehicle) {
    if (vehicle.body.velocity.length() > 4) return false;
    const body = vehicle.body, right = body.rotation.rotate(new Vec3(1, 0, 0));
    for (const side of [-1, 1]) {
      const p = body.position.clone().addScaled(right, side * 2.1); p.y = terrainHeight(p.x, p.z) + .1;
      const b = new AABB(p.clone().add(new Vec3(-.35, .1, -.35)), p.clone().add(new Vec3(.35, 1.8, .35))); let blocked = false;
      this.world.bvh.query(b, other => { if (other !== body && other.bounds.overlaps(b)) blocked = true; });
      if (!blocked) { this.position.copy(p); this.inVehicle = false; this.verticalSpeed = 0; return true; }
    }
    return false;
  }
}

export class FollowCamera {
  constructor(world, player, vehicle) {
    this.world = world; this.player = player; this.vehicle = vehicle; this.target = player.position.clone().add(new Vec3(0, 1.2, 0));
    this.eye = this.target.clone().add(new Vec3(7, 4, 10)); this.yaw = .35; this.pitch = .27; this.distance = 7.5; this.fov = Math.PI / 3;
    this.shadowExtent = 100; this.photo = false; this.view = Mat4.identity(); this.vp = Mat4.identity(); this.inverseVP = Mat4.identity(); this.initialized = false;
  }
  tick(dt) {
    const driving = this.player.inVehicle, focus = driving ? this.vehicle.body.position.clone().add(new Vec3(0, .65, 0)) : this.player.position.clone().add(new Vec3(0, 1.2, 0));
    const t = this.initialized ? 1 - Math.exp(-dt * 10) : 1; this.initialized = true;
    this.target.addScaled(Vec3.sub(focus, this.target), t);
    this.pitch = clamp(this.pitch, .06, 1.25); this.distance = clamp(this.distance, 3.5, this.photo ? 120 : 18);
    const d = this.distance + (driving ? 3 + Math.min(4, this.vehicle.body.velocity.length() * .12) : 0);
    const desired = this.target.clone().add(new Vec3(Math.sin(this.yaw) * Math.cos(this.pitch) * d, Math.sin(this.pitch) * d, Math.cos(this.yaw) * Math.cos(this.pitch) * d));
    const ray = Vec3.sub(desired, this.target), length = ray.length(); ray.normalize();
    const hit = this.photo ? null : this.world.raycast(this.target, ray, length, this.vehicle.body);
    if (hit) desired.copy(this.target).addScaled(ray, Math.max(1, hit.distance - .3));
    if (this.photo) this.world.bvh.query(new AABB(desired.clone().add(new Vec3(-.2,-.2,-.2)), desired.clone().add(new Vec3(.2,.2,.2))), body => { if(body !== this.vehicle.body && body.bounds.min.y < desired.y && body.bounds.max.y > desired.y) desired.y = body.bounds.max.y + 1; });
    desired.y = Math.max(terrainHeight(desired.x, desired.z) + .6, desired.y);
    this.eye.addScaled(Vec3.sub(desired, this.eye), 1 - Math.exp(-dt * 14));
    this.fov = lerp(this.fov, Math.PI / 3 + (driving ? Math.min(.18, this.vehicle.body.velocity.length() * .004) : 0), 1 - Math.exp(-dt * 4));
  }
  update(aspect) { this.view = Mat4.lookAt(this.eye, this.target); this.vp = Mat4.mul(Mat4.perspective(this.fov, aspect, .12, 2600), this.view); this.inverseVP = Mat4.invert(this.vp); }
  project(position, width, height) {
    const p = Mat4.point(this.vp, position), forward = Vec3.sub(position, this.eye).dot(Vec3.sub(this.target, this.eye));
    if (forward <= 0 || p.z > 1 || p.z < -1) return null;
    return { x: (p.x * .5 + .5) * width, y: (-p.y * .5 + .5) * height, depth: p.z };
  }
}
