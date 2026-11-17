import test from 'node:test';
import assert from 'node:assert/strict';
import { Vec3, Quat, AABB } from '../src/math.js';
import { CityGenerator, describeChunk } from '../src/world/city.js';
import { ChunkStreamer } from '../src/world/streaming.js';
import { SidewalkGraph, CubicBezier } from '../src/world/navigation.js';
import { Random, hash, poiPosition, terrainHeight, POIS } from '../src/world/util.js';
import { WeatherSystem, MovingWetnessField } from '../src/world/weather.js';
import { GlassFracture } from '../src/world/destruction.js';
import { CitySimulation } from '../src/game/city-simulation.js';
import { CitizenSystem, drawPerson } from '../src/game/citizens.js';
import { ProgressStore, validateProgress, defaultProgress } from '../src/game/progress.js';
import { MissionSystem, REPAIR_LAMPS } from '../src/game/missions.js';
import { DialogueSystem } from '../src/game/dialogue.js';
import { RoadLoop, trafficSignal, TrafficSystem } from '../src/game/traffic.js';
import { CityPlayer } from '../src/game/player.js';
import { PhysicsWorld, RigidBody } from '../src/physics.js';
import { InstanceBatch } from '../src/world/geometry.js';
import { MeshData } from '../src/mesh.js';

const near = (a, b, e = 1e-5) => assert.ok(Math.abs(a - b) < e, `${a} differs from ${b}`);
const memory = () => { const data = new Map(); return { getItem: key => data.get(key) || null, setItem: (key, value) => data.set(key, value) }; };
const missions = () => { const store = new ProgressStore(memory()); return { store, system: new MissionSystem(store) }; };
const drain = s => { let guard = 0; while (s.pending.size && guard++ < 100) s.pump(100); assert.ok(guard < 100); };

test('seeded generation is stable across positive and negative chunk coordinates', () => {
  assert.deepEqual(describeChunk(-2, 1), describeChunk(-2, 1)); assert.notDeepEqual(describeChunk(2, 1), describeChunk(1, 2));
  const a = new Random(hash(-1, 2)), b = new Random(hash(-1, 2)); for (let i = 0; i < 100; i++) near(a.next(), b.next());
});
test('all mission locations are outside building collider footprints', () => {
  for (const poi of POIS) {
    const plan = describeChunk(Math.floor(poi.x / 80), Math.floor(poi.z / 80));
    for (const b of plan.buildings) assert.ok(Math.abs(poi.x - plan.ox - b.x) >= b.hx + .3 || Math.abs(poi.z - plan.oz - b.z) >= b.hz + .3, `${poi.id} is inside ${b.id}`);
  }
});
test('generated meshes have matching color/material attributes and complete triangle indices', () => {
  const c = new CityGenerator().buildSync(1, 0);
  assert.equal(c.mesh.colors.length, c.mesh.positions.length); assert.equal(c.mesh.materials.length, c.mesh.positions.length / 3 * 4); assert.equal(c.mesh.indices.length % 3, 0);
  assert.ok([...c.mesh.positions].every(Number.isFinite)); assert.ok(Math.max(...c.mesh.indices) < c.mesh.positions.length / 3); assert.ok(c.colliders.length > 0);
});
test('rural terrain meets the road at zero and is deterministic on chunk seams', () => {
  near(terrainHeight(0, 360), 0); near(terrainHeight(80, 360), 0); assert.ok(terrainHeight(40, 360) > 0);
  near(terrainHeight(79.999999, 400), terrainHeight(80.000001, 400), 1e-4);
});
test('chunk commits are atomic and generation can be cancelled without an install', () => {
  const generator = new CityGenerator(), installed = [], s = new ChunkStreamer(generator, { radius: 0, onResident: c => installed.push(c.id) });
  s.update(new Vec3(40, 0, 40)); assert.equal(s.resident.size, 0); s.cancel('0:0'); s.pump(50); assert.equal(installed.length, 0);
  s.update(new Vec3(40, 0, 40)); drain(s); assert.equal(installed.length, 1); assert.equal(s.resident.get('0:0').state, 'resident'); s.dispose();
});
test('chunk streaming respects residency capacity and evicts across district travel', () => {
  const removed = [], s = new ChunkStreamer(new CityGenerator(), { radius: 1, retainRadius: 1, maxResident: 9, onEvict: c => removed.push(c.id) });
  s.update(new Vec3(40, 0, 40)); drain(s); assert.equal(s.resident.size, 9); s.update(new Vec3(344, 0, 106)); drain(s); assert.ok(s.resident.size <= 9); assert.ok(removed.includes('0:0')); assert.equal(s.errors.length, 0); s.dispose(); assert.equal(s.resident.size, 0);
});
test('sidewalk A* connects all named destinations and avoids unconnected water nodes', () => {
  const graph = new SidewalkGraph();
  for (const poi of POIS) { const route = graph.route(poiPosition('plaza'), poiPosition(poi.id)); assert.ok(route.length > 0, `No route to ${poi.id}`); assert.ok(route.every(p => p.finite() && p.x <= 430)); }
  for (const node of graph.nodes.values()) for (const edge of node.edges) assert.ok(graph.nodes.get(edge.id).edges.some(e => e.id === node.id));
});
test('Bezier arc-length lookup progresses monotonically with near-uniform speed', () => {
  const b = new CubicBezier(new Vec3(), new Vec3(0, 0, 10), new Vec3(10, 0, 10), new Vec3(10, 0, 0), 100), samples = [];
  for (let i = 0; i <= 20; i++) samples.push(b.at(b.parameterAtDistance(b.length * i / 20)));
  const distances = samples.slice(1).map((p, i) => p.distance(samples[i])); assert.ok(Math.max(...distances) / Math.min(...distances) < 1.04); near(samples.at(-1).x, 10);
});
test('traffic loops close smoothly and opposing lanes remain separated', () => {
  const a = new RoadLoop(0, 0, 160, 160), b = new RoadLoop(0, 0, 160, 160, true);
  assert.ok(a.sample(0).position.distance(a.sample(a.length).position) < 1e-5); assert.ok(a.sample(a.length - .001).position.distance(a.sample(0).position) < .01);
  assert.ok(a.sample(40).position.distance(b.sample(40).position) > 5);
});
test('traffic signals never give both crossing directions a green light', () => {
  for (let t = 0; t < 200; t += .2) assert.ok(!(trafficSignal(80, 160, t, 'x') === 'green' && trafficSignal(80, 160, t, 'z') === 'green'));
});
test('autonomous traffic remains finite and responds to an obstacle in its lane', () => {
  const system = new TrafficSystem(), car = system.cars[0]; system.cars = [car]; car.speed = 7;
  for (let i = 0; i < 120; i++) { const obstacle = car.position.clone().addScaled(car.direction, 3); system.update(1 / 60, obstacle); }
  assert.ok(car.speed < .1); assert.ok(car.position.finite());
});
test('weather transitions remain bounded and restoration preserves the random stream', () => {
  const a = new WeatherSystem(3); a.remaining = .01; a.update(.1); assert.ok(['clear', 'cloudy'].includes(a.state));
  for (let i = 0; i < 2000; i++) a.update(.1);
  assert.ok(a.rain >= 0 && a.rain <= 1 && a.clouds >= 0 && a.clouds <= 1); const b = new WeatherSystem(); b.restore(a.snapshot()); assert.equal(b.state, a.state); near(b.rain, a.rain); near(b.random.next(), a.random.next());
});
test('wetness window recenters while preserving overlapping cells', () => {
  const w = new MovingWetnessField(); w.water.fill(.6); w.background = .2; const cell = w.extent * 2 / w.size;
  w.update(0, new Vec3(cell * 16, 0, 0), 0, 1); near(w.sample(0, 0), .6, 1e-4); assert.ok([...w.water].every(v => v >= 0 && v <= 1));
});
test('Voronoi glass fragments partition the pane without losing area', () => {
  const cells = GlassFracture.cells(2.6, 1.6, 9, 51);
  const area = cells.reduce((sum, cell) => sum + Math.abs(cell.reduce((a, p, i) => { const q = cell[(i + 1) % cell.length]; return a + p[0] * q[1] - p[1] * q[0]; }, 0)) / 2, 0);
  near(area, 2.6 * 1.6); for (const cell of cells) assert.ok(GlassFracture.mesh(cell).mesh.indices.length >= 9);
});
test('save validation rejects invalid vectors, unbounded cash, and malformed missions', () => {
  const d = validateProgress({ version: 2, cash: Infinity, player: [NaN, 0, 0], car: [1, 2], missions: { courier: { status: 'active', step: -2, variant: 2, remaining: 15 }, unknown: null }, destroyed: ['a', 'a', 3] });
  assert.equal(d.cash, 10000000); assert.deepEqual(d.player, defaultProgress().player); assert.equal(d.missions.courier.step, 0); assert.equal(d.missions.courier.variant, 2); assert.deepEqual(d.destroyed, ['a']);
});
test('reward receipts prevent double payment across saved reloads', () => {
  const storage = memory(), a = new ProgressStore(storage); assert.equal(a.reward('one', 50, 2), true); const b = new ProgressStore(storage); b.load(); assert.equal(b.reward('one', 50, 2), false); assert.equal(b.state.cash, 300);
});
test('new-visit save logic resets an isolated fixture without retaining rewards or broken props', () => {
  const storage = memory(), s = new ProgressStore(storage); s.state.cash = 900; s.state.destroyed = ['fixture']; s.state.receipts = ['fixture-reward']; s.save(); s.reset();
  const fresh = new ProgressStore(storage); fresh.load(); assert.equal(fresh.state.cash, 250); assert.deepEqual(fresh.state.destroyed, []); assert.deepEqual(fresh.state.receipts, []);
});
test('introductory story requires its ordered conversations and pays once', () => {
  const { store, system } = missions(); system.event('talk', 'leo', { place: 'cafe' }); assert.equal(store.state.missions.arrival.step, 0);
  system.event('talk', 'mira', { place: 'plaza' }); system.event('reach', 'cafe'); system.event('talk', 'leo', { place: 'cafe' }); system.event('talk', 'mira', { place: 'plaza' });
  assert.equal(store.state.missions.arrival.status, 'completed'); assert.equal(store.state.cash, 370); system.event('talk', 'mira'); assert.equal(store.state.cash, 370);
});
test('courier routes rotate and the selected destination survives a reload', () => {
  const { store, system } = missions();
  for (let i = 0; i < 3; i++) { system.accept('courier'); assert.equal(store.state.missions.courier.variant, i); system.event('interact', 'depot'); system.event('interact', ['clinic', 'market', 'harbor'][i]); }
  assert.equal(store.state.cash, 535); system.accept('courier'); store.state.missions.courier.variant = 2; store.save();
  const reloaded = new ProgressStore(store.storage); reloaded.load(); const again = new MissionSystem(reloaded); assert.equal(again.steps('courier')[1].target, 'harbor');
});
test('café shift enforces action ordering and pays only after three complete orders', () => {
  const { store, system } = missions(); system.accept('cafe'); system.event('work', 'serve', { place: 'cafe' }); assert.equal(store.state.missions.cafe.step, 0);
  for (let i = 0; i < 3; i++) for (const action of ['brew', 'pack', 'serve']) system.event('work', action, { place: 'cafe' });
  assert.equal(store.state.cash, 335); assert.equal(store.state.missions.cafe.status, 'completed'); system.accept('cafe'); assert.equal(store.state.missions.cafe.step, 0);
});
test('café timeout ends the shift and keeps the job retryable', () => {
  const { store, system } = missions(); system.accept('cafe'); system.update(151, new Vec3(104, 0, 40)); assert.equal(store.state.missions.cafe, undefined); assert.equal(store.state.cash, 250); assert.ok(system.accept('cafe'));
});
test('neighborhood voices require three distinct people', () => {
  const { store, system } = missions(); system.accept('voices'); system.event('talk', 'rosa'); system.event('talk', 'rosa'); assert.equal(store.state.missions.voices.flags.length, 1);
  system.event('talk', 'eli'); system.event('talk', 'juno'); assert.equal(store.state.missions.voices.step, 1); system.event('talk', 'mira'); assert.equal(store.state.missions.voices.status, 'completed');
});
test('streetlight repairs require tools and restore the exact persistent lamp IDs', () => {
  const { store, system } = missions(); system.accept('repair'); system.event('repair', REPAIR_LAMPS[0].id); assert.equal(store.state.repaired.length, 0);
  system.event('talk', 'sanjay', { place: 'harbor' }); for (const lamp of REPAIR_LAMPS) system.event('repair', lamp.id); system.event('talk', 'sanjay'); assert.equal(store.state.repaired.length, 3); assert.equal(store.state.cash, 430);
});
test('shipment story unlocks at the earned reputation threshold', () => {
  const { store, system } = missions(); assert.equal(system.accept('shipment'), false); store.state.reputation = 4; assert.ok(system.accept('shipment'));
  system.event('talk', 'tess'); system.event('interact', 'warehouse'); system.event('talk', 'sanjay'); system.event('talk', 'tess'); assert.equal(store.state.missions.shipment.status, 'completed');
});
test('lookout mission needs a visit followed by framing a postcard', () => {
  const { store, system } = missions(); system.accept('lookout'); system.event('photo', 'lookout'); assert.equal(store.state.missions.lookout.step, 0); system.event('reach', 'lookout'); system.event('photo', 'lookout'); assert.equal(store.state.cash, 330);
});
test('conversations persist familiarity, support topic replies, and release the speaker', () => {
  const { store, system } = missions(), citizens = new CitizenSystem(new SidewalkGraph()), dialogue = new DialogueSystem(citizens, system, store);
  dialogue.open(citizens.get('mira'), 980, { rain: 0 }); assert.equal(citizens.get('mira').talking, true); dialogue.say('What jobs can I do?'); assert.match(dialogue.current.text, /job|help|choose|Choose/i); dialogue.close(); assert.equal(citizens.get('mira').talking, false); dialogue.open(citizens.get('mira'), 980, { rain: 0 }); assert.match(dialogue.current.text, /again/); dialogue.close();
});
test('citizens commute, react to a local disturbance, and produce finite animated instance transforms', () => {
  const citizens = new CitizenSystem(new SidewalkGraph()), p = new Vec3(34, 0, 29); for (let i = 0; i < 30; i++) citizens.update(1 / 30, 980, { rain: 0 }, p);
  assert.ok(citizens.visible.length > 0); citizens.panicAt(p, 50); citizens.update(.1, 980, { rain: 0 }, p); assert.ok(citizens.agents.some(a => a.state === 'Fleeing'));
  const boxes = new InstanceBatch(MeshData.box()), spheres = new InstanceBatch(MeshData.sphere(6, 4)); drawPerson(boxes, spheres, citizens.get('mira'), 2); assert.ok(boxes.count > 10 && spheres.count > 0); assert.ok([...boxes.data.subarray(0, boxes.count * 24)].every(Number.isFinite));
});
test('player silhouettes gain richer facial and body contour detail', () => {
  const citizens = new CitizenSystem(new SidewalkGraph());
  const boxes = new InstanceBatch(MeshData.box()), spheres = new InstanceBatch(MeshData.sphere(6, 4));
  drawPerson(boxes, spheres, citizens.get('mira'), 2, true);
  assert.ok(boxes.count > 40, `expected a more detailed silhouette, got ${boxes.count}`);
  assert.ok(spheres.count > 0);
});
test('on-foot controller stops at a wall and supports jumping and landing', () => {
  const world = new PhysicsWorld(), body = world.add(new RigidBody({ mass: 0, position: new Vec3(0, 1, -2), half: new Vec3(2, 1, .2) })); world.updateBroadphase();
  const p = new CityPlayer(world, new Vec3(0, 0, 0)), keys = new Set(['KeyW']); for (let i = 0; i < 120; i++) p.update(1 / 120, keys, 0); assert.ok(p.position.z > -1.5);
  keys.clear(); keys.add('Space'); p.update(1 / 120, keys, 0); keys.clear(); const y = p.position.y; for (let i = 0; i < 120; i++) p.update(1 / 120, keys, 0); assert.ok(p.grounded); near(p.position.y, .1); assert.ok(y > .1);
});
test('heightfield raycasting returns the same physical surface used by rural walking', () => {
  const w = new PhysicsWorld(); w.groundHeight = terrainHeight; const ray = w.raycast(new Vec3(40, 20, 360), new Vec3(0, -1, 0), 30); assert.ok(ray); near(ray.point.y, terrainHeight(40, 360), .001); assert.ok(ray.normal.y > .8);
});
test('complete city onboarding, proximity interaction, streaming travel, save and cleanup work together', () => {
  const storage = memory(), s = new CitySimulation({ storage }); s.update(1 / 60); assert.equal(s.getInteraction().target, 'mira'); s.interact(); assert.equal(s.dialogue.current.agent.id, 'mira'); assert.equal(s.progress.missions.arrival.step, 1); s.dialogue.close();
  s.player.position.copy(poiPosition('cafe')); s.update(1 / 60); s.interact(); assert.equal(s.dialogue.current.agent.id, 'leo'); assert.equal(s.progress.missions.arrival.step, 3); s.dialogue.close();
  s.player.position.set(34, .1, 29); s.update(1 / 60); s.interact(); assert.equal(s.progress.missions.arrival.status, 'completed'); s.dialogue.close(); assert.equal(s.progress.cash, 370);
  for (const id of ['harbor', 'market', 'lookout', 'plaza']) { s.player.position.copy(poiPosition(id)); s.streamer.update(s.player.position); drain(s.streamer); s.world.updateBroadphase(); assert.ok(s.streamer.resident.size <= 36); }
  s.buildRenderObjects(); assert.ok(s.boxes.count > 100); assert.ok(s.objects.every(o => o.bounds.min.finite() && o.bounds.max.finite())); s.save(); const saved = new ProgressStore(storage); saved.load(); assert.equal(saved.state.cash, 370); s.dispose(); assert.equal(s.streamer.resident.size, 0);
});
test('café work in the integrated game takes time and cannot be performed from another district', () => {
  const s = new CitySimulation(); s.missions.accept('cafe'); s.startWork('brew'); assert.equal(s.work, null); s.player.position.copy(poiPosition('cafe')); s.startWork('brew'); assert.ok(s.work); for (let i = 0; i < 180; i++) s.update(1 / 60); assert.equal(s.progress.missions.cafe.step, 1); assert.equal(s.work, null); s.dispose();
});
test('city shuttle uses game currency, requires a stop, and advances arrival objectives', () => {
  const s = new CitySimulation(); s.missions.event('talk', 'mira'); const before = s.progress.missions.arrival.step;
  assert.equal(s.takeShuttle('cafe'), true); assert.equal(s.progress.cash, 240); near(s.player.position.x, 104); assert.equal(s.progress.missions.arrival.step, before + 1);
  s.player.position.set(230, 0, 230); assert.equal(s.takeShuttle('plaza'), false); assert.equal(s.progress.cash, 240); s.dispose();
});
