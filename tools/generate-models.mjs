#!/usr/bin/env node
// Generates the Nepali landmark models with the Tripo API (v3) and records them in
// assets/generated/manifest.json. Re-running skips models that already exist.
//
//   npm run assets:models                 # everything missing
//   npm run assets:models -- --only pagoda,stupa
//   npm run assets:models -- --force      # regenerate (spends credits again)
//   npm run assets:models -- --dry-run    # show what would be generated
import path from 'node:path';
import { MODELS } from '../src/assets/catalog.js';
import { parseGLB, buildMesh } from '../src/assets/glb.js';
import { loadEnv, requireEnv, flags, readManifest, updateManifest, exists, writeBinary, request, sleep, pool, OUT } from './lib/common.mjs';

const API = 'https://openapi.tripo3d.ai/v3';
const opts = flags();
if (opts.help) { console.log('Usage: node tools/generate-models.mjs [--only id,id] [--force] [--dry-run]\nIds: ' + MODELS.map(m => m.id).join(', ')); process.exit(0); }
await loadEnv();

const manifest = await readManifest();
const wanted = [];
for (const def of MODELS) {
  if (opts.only && !opts.only.has(def.id)) continue;
  const entry = manifest.models?.[def.id];
  if (!opts.force && entry?.file && await exists(path.join(OUT, entry.file))) { console.log(`• ${def.id}: already generated (${entry.file})`); continue; }
  wanted.push(def);
}
if (opts.only) for (const id of opts.only) if (!MODELS.some(m => m.id === id)) console.warn(`! Unknown model id "${id}"`);
if (!wanted.length) { console.log('Nothing to generate.'); process.exit(0); }

const model = process.env.TRIPO_MODEL || 'v3.1-20260211', textureQuality = process.env.TRIPO_TEXTURE_QUALITY || 'standard';
console.log(`Tripo ${model}, texture quality "${textureQuality}". ${wanted.length} model(s): ${wanted.map(m => m.id).join(', ')}`);
if (opts.dryRun) { for (const def of wanted) console.log(`\n[${def.id}] face_limit ${def.faceLimit}\n${def.prompt}`); process.exit(0); }

const key = requireEnv('TRIPO_API_KEY', 'Create one at https://platform.tripo3d.ai (API keys start with "tsk_").');
const auth = { Authorization: `Bearer ${key}` };

async function generate(def) {
  const body = {
    prompt: def.prompt, model, negative_prompt: 'ground plane, terrain, background scene, text, watermark, people, low quality',
    texture: true, pbr: true, texture_quality: textureQuality, face_limit: Number(process.env.TRIPO_FACE_LIMIT) || def.faceLimit
  };
  const created = await (await request(`${API}/generation/text-to-model`, { method: 'POST', headers: { ...auth, 'Content-Type': 'application/json' }, body: JSON.stringify(body) }, { label: `${def.id} create` })).json();
  if (created.code !== 0 || !created.data?.task_id) throw new Error(`${def.id}: Tripo refused the task (${created.code}: ${created.message || JSON.stringify(created).slice(0, 200)})`);
  const taskId = created.data.task_id; console.log(`→ ${def.id}: task ${taskId}`);
  const deadline = Date.now() + (Number(process.env.TRIPO_TIMEOUT_MINUTES) || 20) * 60000; let last = -1, task;
  while (true) {
    await sleep(5000);
    task = (await (await request(`${API}/tasks/${taskId}`, { headers: auth }, { label: `${def.id} status` })).json()).data;
    if (task.progress !== last) { console.log(`  ${def.id}: ${task.status} ${task.progress ?? 0}%`); last = task.progress; }
    if (task.status === 'success') break;
    if (['failed', 'cancelled', 'banned', 'expired'].includes(task.status)) throw new Error(`${def.id}: task ${task.status} ${task.error_message || task.error_code || ''}`);
    if (Date.now() > deadline) throw new Error(`${def.id}: timed out (task ${taskId} is still ${task.status}; re-run later)`);
  }
  const url = task.output?.model_url || task.output?.pbr_model || task.output?.model;
  if (!url) throw new Error(`${def.id}: task finished without a model URL`);
  const bytes = new Uint8Array(await (await request(url, {}, { label: `${def.id} download` })).arrayBuffer());
  // Validate before installing so the game never loads a file it cannot parse.
  const { mesh, imageIndex } = buildMesh(parseGLB(bytes));
  const file = `models/${def.id}.glb`; await writeBinary(file, bytes);
  if (task.output?.rendered_image_url) {
    try { const preview = await request(task.output.rendered_image_url, {}, { label: `${def.id} preview`, retries: 1 }); const ext = (preview.headers.get('content-type') || '').includes('png') ? 'png' : 'webp'; await writeBinary(`models/${def.id}.preview.${ext}`, new Uint8Array(await preview.arrayBuffer())); } catch { /* preview is optional */ }
  }
  await updateManifest(m => { m.models[def.id] = { file, taskId, model, triangles: mesh.indices.length / 3, textured: imageIndex != null, bytes: bytes.length, generated: new Date().toISOString() }; });
  console.log(`✓ ${def.id}: ${(bytes.length / 1048576).toFixed(1)} MB, ${(mesh.indices.length / 3).toLocaleString()} triangles${imageIndex != null ? ', textured' : ''}`);
}

const failures = await pool(wanted, Number(process.env.TRIPO_CONCURRENCY) || 2, generate);
console.log(failures.length ? `\nDone with ${failures.length} failure(s). Re-run to retry only the missing models.` : '\nAll models ready. Reload the game to see them.');
process.exit(failures.length ? 1 : 0);
