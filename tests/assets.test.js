import test from 'node:test';
import assert from 'node:assert/strict';
import { parseGLB, buildMesh } from '../src/assets/glb.js';
import { MODELS, voiceKey, sentences, isUnvoiced } from '../src/assets/catalog.js';
import { placeGeneratedProps } from '../src/game/generated-props.js';
import { MeshData } from '../src/mesh.js';
import { CitySimulation } from '../src/game/city-simulation.js';
import { CityGenerator } from '../src/world/city.js';
import { CitizenSystem, NAMED_CITIZENS } from '../src/game/citizens.js';
import { DialogueSystem } from '../src/game/dialogue.js';
import { MissionSystem } from '../src/game/missions.js';
import { ProgressStore } from '../src/game/progress.js';
import { SidewalkGraph } from '../src/world/navigation.js';
import { spokenScript } from '../tools/lib/script.mjs';

// Builds a tiny GLB: one textured-material triangle under a translated node.
function makeGLB({ required = [] } = {}) {
  const positions = new Float32Array([0, 0, 0, 2, 0, 0, 0, 4, 0]), normals = new Float32Array([0, 0, 1, 0, 0, 1, 0, 0, 1]), uvs = new Float32Array([0, 0, 1, 0, 0, 1]), indices = new Uint16Array([0, 1, 2, 0]);
  const parts = [positions, normals, uvs, indices].map(a => new Uint8Array(a.buffer)), bin = new Uint8Array(parts.reduce((n, p) => n + p.length, 0));
  let offset = 0; const views = parts.map(p => { bin.set(p, offset); const v = { buffer: 0, byteOffset: offset, byteLength: p.length }; offset += p.length; return v; });
  const json = {
    asset: { version: '2.0' }, scene: 0, scenes: [{ nodes: [0] }], nodes: [{ mesh: 0, translation: [10, 5, 0] }],
    meshes: [{ primitives: [{ attributes: { POSITION: 0, NORMAL: 1, TEXCOORD_0: 2 }, indices: 3, material: 0 }] }],
    materials: [{ pbrMetallicRoughness: { baseColorFactor: [.5, .25, 1, 1], metallicFactor: 0, roughnessFactor: .6 } }],
    buffers: [{ byteLength: bin.length }], bufferViews: views,
    accessors: [{ bufferView: 0, componentType: 5126, count: 3, type: 'VEC3' }, { bufferView: 1, componentType: 5126, count: 3, type: 'VEC3' }, { bufferView: 2, componentType: 5126, count: 3, type: 'VEC2' }, { bufferView: 3, componentType: 5123, count: 3, type: 'SCALAR' }],
    ...(required.length ? { extensionsRequired: required, extensionsUsed: required } : {})
  };
  let text = JSON.stringify(json); while (text.length % 4) text += ' ';
  const jsonBytes = new TextEncoder().encode(text), total = 12 + 8 + jsonBytes.length + 8 + bin.length, out = new Uint8Array(total), view = new DataView(out.buffer);
  view.setUint32(0, 0x46546c67, true); view.setUint32(4, 2, true); view.setUint32(8, total, true);
  view.setUint32(12, jsonBytes.length, true); view.setUint32(16, 0x4e4f534a, true); out.set(jsonBytes, 20);
  const b = 20 + jsonBytes.length; view.setUint32(b, bin.length, true); view.setUint32(b + 4, 0x004e4942, true); out.set(bin, b + 8);
  return out;
}

test('GLB reader flattens nodes, keeps material factors, and grounds the model at the origin', () => {
  const { mesh, imageIndex } = buildMesh(parseGLB(makeGLB()));
  assert.equal(mesh.indices.length, 3); assert.equal(imageIndex, null);
  assert.equal(mesh.colors.length, mesh.positions.length); assert.equal(mesh.materials.length, mesh.positions.length / 3 * 4);
  assert.ok(Math.abs(mesh.bounds.min.y) < 1e-6 && Math.abs(mesh.bounds.max.y - 4) < 1e-6, 'model rests on y = 0');
  assert.ok(Math.abs(mesh.bounds.min.x + 1) < 1e-6 && Math.abs(mesh.bounds.max.x - 1) < 1e-6, 'footprint is centred');
  assert.deepEqual([...mesh.colors.slice(0, 3)], [.5, .25, 1]); assert.ok(Math.abs(mesh.materials[0] - .6) < 1e-6);
});

test('GLB reader rejects compressed or malformed files with a clear message', () => {
  assert.throws(() => parseGLB(makeGLB({ required: ['KHR_draco_mesh_compression'] })), /unsupported extensions/);
  assert.throws(() => parseGLB(new Uint8Array(32)), /Not a binary glTF/);
});

test('generated models are scaled to catalog height, share meshes and replace procedural landmarks', () => {
  const mesh = MeshData.box(), models = new Map([['pagoda', { mesh, image: null }], ['doonga', { mesh, image: null }]]);
  const placed = placeGeneratedProps({ models });
  const pagodas = placed.objects.filter(o => o.generated === 'pagoda'), def = MODELS.find(m => m.id === 'pagoda');
  assert.equal(pagodas.length, def.placements.length); assert.ok(Math.abs(pagodas[0].bounds.extent().y - def.height) < 1e-3);
  assert.ok(placed.objects.every(o => o.lods[0].mesh === mesh && o.bounds.min.finite()));
  assert.ok(placed.replaced.has('pagoda') && placed.replaced.has('boats') && !placed.replaced.has('stupa'));
  assert.ok(placed.colliders.some(c => c.id.startsWith('generated:pagoda')) && !placed.colliders.some(c => c.id.includes('doonga')));
  const procedural = new CityGenerator().buildSync(0, 0), replaced = new CityGenerator({ replaced: placed.replaced }).buildSync(0, 0);
  assert.ok(procedural.colliders.some(c => c.id === 'pagoda')); assert.ok(!replaced.colliders.some(c => c.id === 'pagoda'));
  const sim = new CitySimulation({ assets: { models } }); assert.ok(sim.environment.some(o => o.generated === 'pagoda')); sim.dispose();
});

test('voice keys are stable and sentence splitting keeps punctuation', () => {
  assert.equal(voiceKey('mira', 'Namaste!'), voiceKey('mira', '  namaste!  ')); assert.notEqual(voiceKey('mira', 'Namaste!'), voiceKey('leo', 'Namaste!'));
  assert.deepEqual(sentences('Namaste! Good to see you again. Ke chha?'), ['Namaste!', 'Good to see you again.', 'Ke chha?']);
  assert.ok(isUnvoiced('It is 14:05.') && !isUnvoiced('It is lovely.'));
});

test('the ElevenLabs script covers every fixed line a named character can say', () => {
  const script = spokenScript(), store = new ProgressStore(null), missions = new MissionSystem(store), citizens = new CitizenSystem(new SidewalkGraph()), dialogue = new DialogueSystem(citizens, missions, store);
  const covered = (speaker, text) => { for (const s of sentences(text)) if (!isUnvoiced(s)) assert.ok(script.get(speaker)?.has(s), `${speaker} is missing a clip for "${s}"`); };
  for (const person of NAMED_CITIZENS) for (const minutes of [500, 900, 1200]) {
    dialogue.open(citizens.get(person.id), minutes, { rain: 0 }); covered(person.id, dialogue.current.text);
    for (const option of [...dialogue.current.options]) { if (option.disabled || option.id === 'goodbye') continue; dialogue.choose(option.id); if (dialogue.current) covered(person.id, dialogue.current.text); }
    dialogue.close();
  }
  assert.ok(script.get('resident-f').size > 10 && script.get('resident-m').size > 10);
});
