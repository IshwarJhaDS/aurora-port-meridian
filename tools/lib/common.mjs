// Shared helpers for the asset generators: .env loading, the generated-asset
// manifest, CLI flags and polite HTTP retries. No dependencies.
import { readFile, writeFile, mkdir, rename, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', '..');
export const OUT = path.join(ROOT, 'assets', 'generated');
const MANIFEST = path.join(OUT, 'manifest.json');

/** Loads KEY=VALUE pairs from .env (and .env.local) without overriding real environment variables. */
export async function loadEnv() {
  for (const name of ['.env', '.env.local']) {
    let text; try { text = await readFile(path.join(ROOT, name), 'utf8'); } catch { continue; }
    for (const raw of text.split(/\r?\n/)) {
      const line = raw.trim(); if (!line || line.startsWith('#')) continue;
      const match = line.match(/^(?:export\s+)?([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$/); if (!match) continue;
      let value = match[2].trim();
      if (/^(['"]).*\1$/.test(value)) value = value.slice(1, -1); else value = value.replace(/(^|\s+)#.*$/, '').trim();
      if (process.env[match[1]] === undefined) process.env[match[1]] = value;
    }
  }
}

export function requireEnv(name, hint) {
  const value = process.env[name];
  if (!value) { console.error(`Missing ${name}. ${hint}\nCopy .env.example to .env and fill it in.`); process.exit(1); }
  return value;
}

export function flags(argv = process.argv.slice(2)) {
  const out = { force: false, dryRun: false, only: null };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--force') out.force = true;
    else if (a === '--dry-run') out.dryRun = true;
    else if (a === '--only') out.only = new Set(argv[++i].split(',').map(s => s.trim()).filter(Boolean));
    else if (a.startsWith('--only=')) out.only = new Set(a.slice(7).split(',').map(s => s.trim()).filter(Boolean));
    else if (a === '--help' || a === '-h') out.help = true;
    else { console.error(`Unknown option ${a}`); process.exit(2); }
  }
  return out;
}

let queue = Promise.resolve();
export async function readManifest() {
  try { return JSON.parse(await readFile(MANIFEST, 'utf8')); } catch { return { version: 1, models: {}, audio: { ambience: {}, effects: {}, music: {}, voices: {} } }; }
}
/** Serialized read-modify-write so concurrent jobs never lose each other's entries. */
export function updateManifest(mutate) {
  queue = queue.then(async () => {
    const manifest = await readManifest();
    manifest.models ??= {}; manifest.audio ??= {}; for (const k of ['ambience', 'effects', 'music', 'voices']) manifest.audio[k] ??= {};
    mutate(manifest); manifest.updated = new Date().toISOString();
    await mkdir(OUT, { recursive: true });
    const temp = `${MANIFEST}.tmp`; await writeFile(temp, `${JSON.stringify(manifest, null, 2)}\n`); await rename(temp, MANIFEST);
  });
  return queue;
}

export async function exists(file) { try { return (await stat(file)).size > 0; } catch { return false; } }

export async function writeBinary(relative, bytes) {
  const file = path.join(OUT, relative); await mkdir(path.dirname(file), { recursive: true });
  await writeFile(`${file}.part`, bytes); await rename(`${file}.part`, file); return file;
}

export const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

/** fetch with retries on 429/5xx and network errors. Never logs request headers. */
export async function request(url, options = {}, { retries = 4, label = url } = {}) {
  for (let attempt = 0; ; attempt++) {
    let response;
    try { response = await fetch(url, options); }
    catch (error) { if (attempt >= retries) throw new Error(`${label}: ${error.message}`); await sleep(1500 * 2 ** attempt); continue; }
    if (response.ok) return response;
    const body = await response.text().catch(() => '');
    if ((response.status === 429 || response.status >= 500) && attempt < retries) {
      const wait = Number(response.headers.get('retry-after')) * 1000 || 2000 * 2 ** attempt;
      console.warn(`  ${label}: HTTP ${response.status}, retrying in ${Math.round(wait / 1000)}s`); await sleep(wait); continue;
    }
    throw new Error(`${label}: HTTP ${response.status} ${body.slice(0, 400)}`);
  }
}

/** Runs async jobs with bounded concurrency; collects failures instead of aborting. */
export async function pool(items, limit, worker) {
  const failures = []; let next = 0;
  await Promise.all(Array.from({ length: Math.max(1, Math.min(limit, items.length)) }, async () => {
    while (next < items.length) { const item = items[next++]; try { await worker(item); } catch (error) { failures.push({ item, error }); console.error(`  ✗ ${error.message}`); } }
  }));
  return failures;
}
