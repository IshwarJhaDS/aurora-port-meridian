// Loads whatever the generator scripts have produced. A missing manifest simply
// means "nothing generated yet" and the game runs with its procedural content.
import { MANIFEST_URL, MODELS } from './catalog.js';
import { loadGLB } from './glb.js';

export const EMPTY_LIBRARY = Object.freeze({ models: new Map(), audio: { ambience: {}, effects: {}, music: {}, voices: {} }, errors: [] });

export async function loadAssetLibrary({ onProgress = () => {} } = {}) {
  let manifest;
  try {
    const response = await fetch(MANIFEST_URL, { cache: 'no-store' });
    if (!response.ok) return EMPTY_LIBRARY;
    manifest = await response.json();
  } catch { return EMPTY_LIBRARY; }
  const models = new Map(), errors = [], wanted = MODELS.filter(m => manifest.models?.[m.id]?.file);
  let done = 0;
  await Promise.all(wanted.map(async def => {
    try { models.set(def.id, await loadGLB(`./assets/generated/${manifest.models[def.id].file}`)); }
    catch (error) { errors.push(`${def.id}: ${error.message}`); console.warn(`[assets] ${def.id} model skipped:`, error); }
    onProgress(++done, wanted.length);
  }));
  const audio = { ambience: {}, effects: {}, music: {}, voices: {}, ...(manifest.audio || {}) };
  return { models, audio, errors };
}
