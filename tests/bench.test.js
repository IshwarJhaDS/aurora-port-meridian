import test from 'node:test';
import assert from 'node:assert/strict';
import { Engine } from '../src/engine.js';
import { Renderer } from '../src/renderer.js';
import { Vec3 } from '../src/math.js';
import { PhysicsWorld, RigidBody } from '../src/physics.js';
import { MeshData } from '../src/mesh.js';
import { LODSelector } from '../src/resources.js';
import { BVH } from '../src/spatial.js';

test('integrated lab settles and maximum pooled content fits the software-ray BVH limit', () => {
  // Exercise the real integration methods without constructing a browser or GPU context.
  const engine = Object.create(Engine.prototype);
  Object.assign(engine, {
    world: new PhysicsWorld(), objects: [], nextObject: 1, ragdolls: [],
    box: MeshData.box(),
    spheres: [{ mesh: MeshData.sphere(24, 12), error: 0 }],
    smallSpheres: [{ mesh: MeshData.sphere(12, 6), error: 0 }],
    renderer: { lod: new LODSelector() }
  });
  engine.createLab();
  const droppedSphere = engine.world.bodies.at(-1);
  for (let i = 0; i < 600; i++) engine.world.step(1 / 120);
  assert.ok(droppedSphere.position.y > 2, 'Sphere must remain supported by the coupled surface');
  assert.ok(engine.world.bodies.every(b => b.position.finite() && b.velocity.finite()));

  for (let i = 0; i < 3; i++) engine.spawnRagdoll();
  for (let i = 0; i < 24; i++) {
    const body = engine.world.add(new RigidBody({
      shape: 'sphere', radius: .3, mass: 8,
      position: new Vec3(i % 6 - 3, 7 + Math.floor(i / 6), 7)
    }));
    engine.bodyVisual(body);
  }
  engine.updateObjects(1);
  const renderer = Object.create(Renderer.prototype);
  Object.assign(renderer, {
    traceBVH: new BVH(), traceItems: [], traceSignature: '', uploadData() {}
  });
  renderer.updateTraceScene(engine.objects);
  assert.equal(engine.world.bodies.length, 75);
  assert.ok(renderer.traceItems.length > 17000);
  assert.ok(renderer.traceBVH.nodes.length < 16384);
  for (const o of engine.objects) {
    assert.ok([...o.model].every(Number.isFinite));
    assert.ok(o.bounds.min.finite() && o.bounds.max.finite());
  }
});
