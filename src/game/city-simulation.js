import { Vec3, Quat, Mat4, clamp } from '../math.js';
import { FixedStepClock, FrameArena } from '../core.js';
import { PhysicsWorld, RigidBody } from '../physics.js';
import { RaycastVehicle } from '../vehicle.js';
import { MeshData } from '../mesh.js';
import { DayNightCycle } from '../engine.js';
import { CityGenerator } from '../world/city.js';
import { ChunkStreamer } from '../world/streaming.js';
import { SidewalkGraph } from '../world/navigation.js';
import { WeatherSystem, MovingWetnessField } from '../world/weather.js';
import { GeometryBuilder, InstanceBatch, renderObject, material } from '../world/geometry.js';
import { DestructionSystem } from '../world/destruction.js';
import { POIS, poiById, poiPosition, terrainHeight, distanceXZ, districtAt, isOpen, WORLD_MIN, WORLD_MAX } from '../world/util.js';
import { CitizenSystem, drawPerson } from './citizens.js';
import { TrafficSystem, drawCar, trafficSignal } from './traffic.js';
import { CityPlayer, FollowCamera } from './player.js';
import { ProgressStore } from './progress.js';
import { MissionSystem, REPAIR_LAMPS } from './missions.js';
import { DialogueSystem } from './dialogue.js';

export class CitySimulation {
  constructor({ storage = null, notify = () => {}, changed = () => {}, release = () => {} } = {}) {
    this.notify = notify; this.changed = changed; this.release = release;
    this.store = new ProgressStore(storage); this.progress = this.store.load(); this.clock = new FixedStepClock({ hz: 120, maxSubsteps: 12 }); this.elapsed = 0; this.saveTimer = 0; this.routeTimer = 0; this.paused = false; this.keys = new Set(); this.hold = null; this.work = null; this.kickCooldown = 0; this.navTarget = null; this.route = [];
    this.world = new PhysicsWorld({ iterations: 10 }); this.world.groundHeight = terrainHeight;
    this.player = new CityPlayer(this.world, Vec3.from(this.progress.player));
    this.vehicle = new RaycastVehicle(this.world, { position: Vec3.from(this.progress.car) });
    this.vehicle.body.rotation.copy(Quat.axisAngle(new Vec3(0, 1, 0), Math.PI)); this.vehicle.body.previousRotation.copy(this.vehicle.body.rotation);
    this.vehicle.body.name = 'Your Aster GT'; this.sun = new DayNightCycle(this.progress.minutes / 60); this.weather = new WeatherSystem(); this.weather.restore(this.progress.weather); this.wet = new MovingWetnessField();
    this.camera = new FollowCamera(this.world, this.player, this.vehicle); this.camera.yaw = .2;
    this.graph = new SidewalkGraph(); this.citizens = new CitizenSystem(this.graph); this.traffic = new TrafficSystem();
    this.missions = new MissionSystem(this.store, { notify, onChange: () => { this.routeTimer = 0; this.changed('missions'); } });
    this.dialogue = new DialogueSystem(this.citizens, this.missions, this.store, { navigate: id => this.navigate(id), changed: data => this.changed('dialogue', data) });
    this.generator = new CityGenerator(); this.chunkObjects = new Map(); this.chunkBodies = new Map(); this.propBodies = new Map(); this.props = new Map(); this.lights = new Map();
    this.streamer = new ChunkStreamer(this.generator, { onResident: (c, previous) => this.installChunk(c, previous), onEvict: (c, replaced) => this.removeChunk(c, replaced) });
    this.destruction = new DestructionSystem(this.world, this.progress, { panic: p => this.citizens.panicAt(p), release: object => this.release(object) });
    this.boxMesh = MeshData.box(); this.sphereMesh = MeshData.sphere(10, 6); this.boxes = new InstanceBatch(this.boxMesh, 4096); this.spheres = new InstanceBatch(this.sphereMesh, 1024); this.arena = new FrameArena(128 * 1024);
    this.environment = this.buildEnvironment(); this.horizon = this.generator.createHorizon();
    this.streamer.update(this.player.position);
    // One small initial chunk is prepared before play; subsequent chunks are incremental.
    while (!this.streamer.resident.has(`${Math.floor(this.player.position.x / 80)}:${Math.floor(this.player.position.z / 80)}`) && this.streamer.pending.size) this.streamer.pump(12);
    this.world.updateBroadphase(); this.objects = []; this.interaction = null;
  }
  buildEnvironment() {
    const land = new GeometryBuilder(), sea = new GeometryBuilder(), boats = new GeometryBuilder();
    for (let z = -480; z < 560; z += 40) for (let x = -480; x < 436; x += 40) {
      const right = Math.min(x + 40, 436), point = (a, b) => new Vec3(a, terrainHeight(a, b) - .1, b);
      land.quad(point(x, z), point(x, z + 40), point(right, z + 40), point(right, z), z > 230 ? [.23, .29, .16] : [.24, .28, .22]);
    }
    for (let n = -480; n <= 560; n += 80) { if (n < 430) land.box(n, -.02, 40, 7, .009, 520, [.065, .077, .085]); land.box(-22, -.02, n, 458, .009, 7, [.065, .077, .085]); }
    sea.box(740, -1.05, 40, 305, .035, 700, [.025, .12, .16], material(.12, .55, 0, 0));
    for (const z of [-120, 65, 215]) {
      boats.box(484, -.42, z, 3.4, .55, 7, [.22, .28, .29], material(.35, .35)); boats.box(484, .45, z + 1, 2.2, .5, 3, [.63, .62, .52]); boats.box(484, 3.1, z, .09, 2.7, .09, [.35, .37, .33], material(.4, .7));
    }
    const water = renderObject(sea.build(), { castShadow: false }); water.water = true;
    return [renderObject(land.build(), { castShadow: false }), water, renderObject(boats.build(), { castShadow: false })];
  }
  installChunk(chunk) {
    const position = new Vec3(chunk.ox, 0, chunk.oz), object = renderObject(chunk.mesh, { position }), ground = renderObject(chunk.groundMesh, { position, castShadow: false });
    object.shadowMesh = chunk.shadowMesh; chunk.renderObjects = [object, ground]; chunk.physicsBodies = [];
    this.chunkObjects.set(chunk.id, chunk.renderObjects);
    for (const data of chunk.colliders) {
      const body = this.world.add(new RigidBody({ mass: 0, position: new Vec3(data.x, data.y, data.z), half: Vec3.from(data.half) })); body.name = data.id; chunk.physicsBodies.push(body);
    }
    if (chunk.cx === 0 && chunk.cz === 0) chunk.props.push({ id: 'plaza-glass', type: 'glass', x: 15, y: 0, z: 62, breakable: true });
    for (const prop of chunk.props) {
      this.props.set(prop.id, prop);
      if (!prop.breakable || this.progress.destroyed.includes(prop.id)) continue;
      const half = prop.type === 'crate' ? new Vec3(.5, .5, .5) : prop.type === 'glass' ? new Vec3(1.3, .8, .05) : new Vec3(.32, .52, .32), body = this.world.add(new RigidBody({ mass: 0, position: new Vec3(prop.x, prop.y + (prop.type === 'glass' ? 1.2 : half.y), prop.z), half }));
      body.propId = prop.id; body.chunkGeneration = chunk.generation; chunk.physicsBodies.push(body); this.propBodies.set(prop.id, body);
    }
    for (const light of chunk.lights) this.lights.set(light.id, light);
    this.chunkBodies.set(chunk.id, chunk.physicsBodies);
  }
  removeChunk(chunk, replaced) {
    for (const object of chunk.renderObjects || []) this.release(object);
    for (const body of chunk.physicsBodies || []) { this.world.remove(body); if (body.propId && this.propBodies.get(body.propId) === body) this.propBodies.delete(body.propId); }
    if (!replaced) { this.chunkObjects.delete(chunk.id); this.chunkBodies.delete(chunk.id); for (const p of chunk.props) this.props.delete(p.id); for (const l of chunk.lights) this.lights.delete(l.id); }
  }
  navigate(id) { const p = poiById(id); if (!p) return; this.navTarget = { position: poiPosition(id), label: p.name, id }; this.routeTimer = 0; this.notify(`Waypoint set · ${p.name}`); this.changed('map'); }
  navigatePosition(position) { this.navTarget = { position: position.clone(), label: 'Custom waypoint', id: null }; this.routeTimer = 0; this.changed('map'); }
  takeShuttle(id) {
    const destination = poiById(id);
    if (!destination) { this.notify('Select a named place on the map first.'); return false; }
    if (this.player.inVehicle || !POIS.some(p => distanceXZ(this.player.position, p) < 16)) { this.notify('Board the city shuttle near a named stop, on foot.'); return false; }
    if (this.progress.cash < 10) { this.notify('A city shuttle trip costs $10 in game money.'); return false; }
    this.progress.cash -= 10;
    const p = poiPosition(id); if (id === 'plaza') p.set(34, .1, 29); else p.y += .1;
    this.player.position.copy(p); this.player.verticalSpeed = 0; this.progress.minutes = (this.progress.minutes + 8) % 1440;
    this.dialogue.close(); this.navTarget = null; this.routeTimer = 0; this.camera.initialized = false; this.streamer.update(p); this.streamer.pump(12);
    this.missions.update(0, this.player.position, false); this.save(); this.notify(`City shuttle · ${destination.name}   −$10`); return true;
  }
  goal() { const objective = this.missions.tracked(); return this.navTarget || (objective ? { position: this.missions.objectivePosition(this.citizens), label: objective.step?.text || objective.definition.title, id: objective.step?.place } : null); }
  getInteraction() {
    if (this.player.inVehicle) return { kind: 'exit', label: 'Exit Aster GT', key: 'E' };
    const repair = this.missions.repairNear(this.player.position); if (repair) return { kind: 'repair', label: 'Repair streetlight · hold', target: repair.target, key: 'E' };
    const cafe = this.progress.missions.cafe;
    if (cafe?.status === 'active' && distanceXZ(this.player.position, poiPosition('cafe')) < 7) return { kind: 'work', label: this.missions.steps('cafe')[cafe.step]?.text || 'Café counter', key: 'E' };
    const citizen = this.citizens.nearest(this.player.position); if (citizen) return { kind: 'talk', label: `Talk to ${citizen.name}`, target: citizen.id, key: 'E' };
    if (distanceXZ(this.player.position, this.vehicle.body.position) < 4) return { kind: 'enter', label: 'Drive your Aster GT', key: 'E' };
    const poi = POIS.find(p => distanceXZ(this.player.position, p) < 3.8); if (poi) return { kind: 'place', label: `${poi.id === 'warehouse' ? 'Inspect' : 'Visit'} ${poi.name}`, target: poi.id, key: 'E' };
    return null;
  }
  interact() {
    if (this.dialogue.current) return;
    const interaction = this.getInteraction(); if (!interaction) return;
    if (interaction.kind === 'talk') this.dialogue.open(this.citizens.get(interaction.target), this.progress.minutes, this.weather);
    else if (interaction.kind === 'enter') { if (this.player.enter(this.vehicle)) { this.notify('Aster GT · WASD to drive, Space handbrake, E to exit.'); this.camera.distance = 7.5; } }
    else if (interaction.kind === 'exit') { if (!this.player.exit(this.vehicle)) this.notify('Slow down and find a clear space to get out.'); }
    else if (interaction.kind === 'repair') this.hold = { target: interaction.target, time: 0, duration: 2.2 };
    else if (interaction.kind === 'work') this.changed('work');
    else if (interaction.kind === 'place') { this.missions.event('interact', interaction.target); this.notify(poiById(interaction.target).description); }
  }
  startWork(action) {
    const state = this.progress.missions.cafe; if (this.work || state?.status !== 'active') return;
    if (distanceXZ(this.player.position, poiPosition('cafe')) > 7 || !isOpen(poiById('cafe'), this.progress.minutes / 60)) { this.notify('Go to Harbor Coffee while the café is open.'); return; }
    const step = this.missions.steps('cafe')[state.step]; if (step.target !== action) { this.notify(`Next, ${step.text.split('·')[1]?.trim() || step.target}`); return; }
    this.work = { action, duration: action === 'brew' ? 2.6 : 1.4, time: 0 }; this.changed('work');
  }
  destroy(prop) {
    if (!this.destruction.break(prop, this.vehicle.body.rotation.rotate(new Vec3(0, .4, -1)))) return;
    const body = this.propBodies.get(prop.id); if (body) { this.world.remove(body); this.propBodies.delete(prop.id); }
    this.notify(prop.type === 'glass' ? 'Glass shattered · nearby people step away.' : 'Street prop damaged · watch your surroundings.'); this.store.save();
  }
  kick() { if (this.kickCooldown || this.player.inVehicle) return; const prop = [...this.props.values()].find(p => p.breakable && !this.progress.destroyed.includes(p.id) && distanceXZ(this.player.position, p) < 2.7); if (prop) { this.destroy(prop); this.kickCooldown = .8; } }
  resetVehicle() {
    const p = this.player.position; const x = Math.round(p.x / 80) * 80 + 3.1, z = clamp(p.z + 8, -460, 540), b = this.vehicle.body;
    b.position.set(Math.min(403, x), terrainHeight(x, z) + .8, z); b.rotation.copy(new Quat()); b.velocity.set(0, 0, 0); b.angularVelocity.set(0, 0, 0); b.previousPosition.copy(b.position); b.previousRotation.copy(b.rotation);
    for (const w of this.vehicle.wheels) { w.omega = 0; w.compression = 0; } this.world.updateBroadphase(); this.notify('Your Aster GT is ready on the nearest road.');
  }
  fixedStep(dt, enabled) {
    const drive = this.player.inVehicle && enabled;
    const forward = drive && (this.keys.has('KeyW') || this.keys.has('ArrowUp')), backward = drive && (this.keys.has('KeyS') || this.keys.has('ArrowDown'));
    const heading = this.vehicle.body.rotation.rotate(new Vec3(0, 0, -1)), speed = this.vehicle.body.velocity.dot(heading);
    const reversingDirection = forward && this.vehicle.gear === 0 && speed < -.8 || backward && this.vehicle.gear !== 0 && speed > .8;
    this.vehicle.setControls({ throttle: reversingDirection ? 0 : forward ? 1 : backward ? -1 : 0, brake: !this.player.inVehicle || reversingDirection || drive && this.keys.has('KeyB') ? 1 : 0, steer: drive ? (this.keys.has('KeyA') || this.keys.has('ArrowLeft') ? 1 : 0) - (this.keys.has('KeyD') || this.keys.has('ArrowRight') ? 1 : 0) : 0, handbrake: !this.player.inVehicle || drive && this.keys.has('Space') ? 1 : 0 });
    this.vehicle.wetness = this.wet.sample(this.vehicle.body.position.x, this.vehicle.body.position.z);
    this.world.step(dt); this.player.update(dt, this.keys, this.camera.yaw, enabled && !this.work);
    if (this.player.inVehicle) this.player.position.copy(this.vehicle.body.position).add(new Vec3(0, -.6, 0));
    for (const contact of this.world.contacts) {
      const id = contact.a.propId || contact.b.propId; if (id && contact.jn > 180 && this.props.has(id)) this.destroy(this.props.get(id));
    }
  }
  update(dt) {
    this.streamer.update(this.player.position); this.streamer.pump(4);
    if (this.paused) { this.camera.tick(Math.min(dt, .05)); return; }
    dt = Math.min(.1, dt); this.elapsed += dt; this.kickCooldown = Math.max(0, this.kickCooldown - dt);
    this.progress.minutes = (this.progress.minutes + dt * .55) % 1440; this.weather.update(dt); this.sun.hour = this.progress.minutes / 60; this.sun.update(0); this.sun.intensity *= 1 - this.weather.clouds * .42; this.sun.clouds = this.weather.clouds; this.sun.flash = this.weather.flash * .7;
    this.wet.update(dt, this.player.position, this.weather.rain, Math.max(0, this.sun.direction.y));
    const enabled = !this.dialogue.current; this.clock.advance(dt, h => this.fixedStep(h, enabled));
    this.citizens.update(dt, this.progress.minutes, this.weather, this.player.position, this.vehicle.body, this.traffic.visible); this.traffic.update(dt, this.player.position, this.vehicle.body, this.citizens.visible);
    this.destruction.update(dt); this.missions.update(dt, this.player.position, enabled);
    if (this.hold) {
      const step = this.missions.repairNear(this.player.position);
      if (!this.keys.has('KeyE') || !step || step.target !== this.hold.target) this.hold = null;
      else { this.hold.time += dt; if (this.hold.time >= this.hold.duration) { this.missions.event('repair', this.hold.target); this.hold = null; } }
    }
    if (this.work) {
      if (distanceXZ(this.player.position, poiPosition('cafe')) > 8) { this.work = null; this.notify('Return to the counter to continue your order.'); }
      else { this.work.time += dt; if (this.work.time >= this.work.duration) { this.missions.event('work', this.work.action, { place: 'cafe' }); this.work = null; this.changed('work'); } }
    }
    if (this.vehicle.body.position.y < -2 || this.vehicle.body.position.x > 432 || Math.abs(this.vehicle.body.position.x) > 470 || this.vehicle.body.position.z < -470 || this.vehicle.body.position.z > 550) this.resetVehicle();
    this.interaction = this.getInteraction(); this.camera.tick(dt);
    this.routeTimer -= dt; const goal = this.goal();
    if (this.navTarget && distanceXZ(this.player.position, this.navTarget.position) < 5) { this.notify(`Arrived · ${this.navTarget.label}`); this.navTarget = null; this.routeTimer = 0; }
    if (this.routeTimer <= 0 && goal?.position) {
      const from = this.player.position.clone().add(new Vec3(0, 1, 0)), to = goal.position.clone().add(new Vec3(0, 1, 0)), d = Vec3.sub(to, from), length = d.length();
      const clear = length < 45 && !this.world.raycast(from, d.normalize(), Math.max(0, length - .2), this.vehicle.body);
      this.route = clear ? [this.player.position.clone(), goal.position.clone()] : [this.player.position.clone(), ...this.graph.route(this.player.position, goal.position), goal.position.clone()]; this.routeTimer = 1.2;
    }
    this.saveTimer += dt; if (this.saveTimer > 10) { this.save(); this.saveTimer = 0; }
  }
  save() { this.progress.player = this.player.position.toArray(); this.progress.car = this.vehicle.body.position.toArray(); this.progress.weather = this.weather.snapshot(); const saved = this.store.save(); this.changed('saved', saved); return saved; }
  buildRenderObjects() {
    const b = this.boxes, s = this.spheres; b.reset(); s.reset();
    if (!this.player.inVehicle) drawPerson(b, s, this.player, this.elapsed, true);
    for (const a of this.citizens.visible) drawPerson(b, s, a, this.elapsed);
    const carBody = this.vehicle.body, car = { position: carBody.position, rotation: carBody.rotation, color: [.018, .12, .17], brake: this.vehicle.brake > .1, wheel: 0 };
    const wheels = this.vehicle.wheels.map(w => ({ position: this.vehicle.wheelPosition(w), rotation: Quat.mul(carBody.rotation, Quat.axisAngle(new Vec3(1, 0, 0), w.angle)) }));
    drawCar(b, s, car, { wheels, headlights: this.sun.direction.y < .2 });
    for (const [i, traffic] of this.traffic.visible.entries()) drawCar(b, s, traffic, { taxi: i % 7 === 0, headlights: this.sun.direction.y < .2 });
    const night = clamp((.15 - this.sun.direction.y) / .28, 0, 1), damaged = new Set(REPAIR_LAMPS.filter(l => !this.progress.repaired.includes(l.id)).map(l => l.id));
    for (const prop of this.props.values()) {
      if (distanceXZ(prop, this.player.position) > 180) continue;
      const { x, y, z } = prop;
      if (prop.type === 'lamp') {
        b.box(x, y + 2.9, z, .065, 2.9, .065, [.15, .19, .19], material(.4, .7)); b.box(x + .55, y + 5.7, z, .6, .06, .06, [.17, .2, .19], material(.4, .7));
        b.box(x + 1, y + 5.62, z, .3, .055, .16, damaged.has(prop.id) ? [.17, .15, .1] : [.8, .58, .28], material(.4, .1, damaged.has(prop.id) ? 0 : night * 3));
      } else if (prop.type === 'glass') {
        b.box(x - 1.4, y + 1.25, z, .055, 1.25, .06, [.15, .19, .19], material(.5, .7)); b.box(x + 1.4, y + 1.25, z, .055, 1.25, .06, [.15, .19, .19], material(.5, .7)); b.box(x, y + 2.5, z, 1.45, .06, .6, [.18, .24, .25]);
        if (!this.progress.destroyed.includes(prop.id)) b.box(x, y + 1.2, z, 1.3, .8, .035, [.08, .2, .22], material(.1, .65));
      } else if (!this.progress.destroyed.includes(prop.id)) {
        const crate = prop.type === 'crate'; b.box(x, y + (crate ? .5 : .52), z, crate ? .5 : .31, crate ? .5 : .52, crate ? .5 : .31, crate ? [.37, .24, .12] : [.1, .17, .15], material(.85));
        if (crate) for (const d of [-.3, .3]) b.box(x + d, y + .51, z, .035, .52, .515, [.22, .15, .09]); else b.box(x, y + 1.05, z, .34, .045, .34, [.15, .2, .18], material(.5, .4));
      }
    }
    for (let ix = Math.floor(this.player.position.x / 80) - 1; ix <= Math.floor(this.player.position.x / 80) + 1; ix++) for (let iz = Math.floor(this.player.position.z / 80) - 1; iz <= Math.floor(this.player.position.z / 80) + 1; iz++) {
      const x = ix * 80 + 8, z = iz * 80 + 8, axis = 'x', color = trafficSignal(ix * 80, iz * 80, this.traffic.elapsed, axis);
      b.box(x, 1.9, z, .065, 1.9, .065, [.12, .15, .15], material(.4, .7)); b.box(x, 3.9, z, .14, .4, .15, [.045, .055, .05]);
      for (const [i, c] of ['red', 'amber', 'green'].entries()) s.box(x, 4.12 - i * .22, z - .15, .075, .075, .055, c === 'red' ? [.55, .025, .015] : c === 'amber' ? [.75, .4, .035] : [.04, .48, .2], material(.4, 0, c === color ? 2.7 : .02));
    }
    const pieces = [];
    for (const piece of this.destruction.debris) {
      if (piece.object) { piece.object.model = Mat4.compose(piece.body.position, piece.body.rotation); piece.object.bounds = piece.body.bounds.clone().expand(.8); pieces.push(piece.object); }
      else b.add(piece.body.position, piece.body.rotation, piece.body.half, piece.color, material(.85));
    }
    this.sun.lights = [...this.lights.values()].filter(l => !damaged.has(l.id) && night > .02).sort((a, b) => distanceXZ(a, this.player.position) - distanceXZ(b, this.player.position)).slice(0, 10).map(l => ({ ...l, power: l.power * night }));
    if (night > .1) for (const side of [-.6, .6]) { const p = carBody.localToWorld(new Vec3(side, .1, -2)); this.sun.lights.push({ x: p.x, y: p.y, z: p.z, radius: 14, power: 17, color: [1, .9, .68] }); }
    this.objects = [...this.environment, this.horizon, ...[...this.chunkObjects.values()].flat(), b.finish(), s.finish(), ...pieces]; return this.objects;
  }
  dispose() { this.save(); this.dialogue.close(); this.streamer.dispose(); this.destruction.dispose(); this.vehicle.dispose(); }
}
