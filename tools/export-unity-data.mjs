#!/usr/bin/env node
// Exports the game's content (places, people, dialogue, missions, model placements,
// generated audio) to the Unity project so both versions share one source of truth.
// Also copies assets/generated (Tripo + ElevenLabs output) into StreamingAssets.
//
//   npm run unity:data
import { mkdir, writeFile, cp, readdir } from 'node:fs/promises';
import path from 'node:path';
import { POIS } from '../src/world/util.js';
import { NAMED_CITIZENS } from '../src/game/citizens.js';
import { VOICES, GREETINGS, LINES, jobsFor } from '../src/game/dialogue.js';
import { MissionSystem, MISSION_DEFINITIONS } from '../src/game/missions.js';
import { ProgressStore } from '../src/game/progress.js';
import { MODELS, CHARACTERS, AMBIENCE, EFFECTS, MUSIC, voiceKey } from '../src/assets/catalog.js';
import { spokenScript } from './lib/script.mjs';
import { ROOT, OUT, readManifest, exists } from './lib/common.mjs';

const UNITY = path.join(ROOT, 'unity', 'PhewaValley', 'Assets', 'StreamingAssets', 'PhewaValley');
const manifest = await readManifest();

const store = new ProgressStore(null), missions = new MissionSystem(store);
const step = s => ({ kind: s.kind, target: s.target || '', targets: s.targets || [], place: s.place || '', text: s.text, x: s.position?.x ?? 0, z: s.position?.z ?? 0, hasPosition: !!s.position });
const missionData = MISSION_DEFINITIONS.map(def => {
  const variants = [];
  for (let variant = 0; variant < (def.id === 'courier' ? 3 : 1); variant++) {
    store.state.missions[def.id] = { status: 'active', step: 0, flags: [], remaining: 150, variant };
    variants.push({ steps: missions.steps(def.id).map(step) });
  }
  delete store.state.missions[def.id];
  return { id: def.id, title: def.title, category: def.category, description: def.description, reward: def.reward, reputation: def.reputation, giver: def.giver, repeatable: !!def.repeatable, requirement: def.requirement || '', variants };
});

// Only clips that exist on disk are exported, keyed by the exact sentence text.
const voiceClips = [];
for (const [speaker, lines] of spokenScript()) for (const sentence of lines) {
  const file = manifest.audio?.voices?.[voiceKey(speaker, sentence)];
  if (file && await exists(path.join(OUT, file))) voiceClips.push({ speaker, sentence, file });
}
const audioList = async (group, defs) => (await Promise.all(defs.map(async d => ({ id: d.id, file: manifest.audio?.[group]?.[d.id] || '' })))).filter(x => x.file);

const data = {
  version: 1,
  pois: POIS.map(p => ({ id: p.id, name: p.name, kind: p.kind, x: p.x, z: p.z, description: p.description, openFrom: p.open?.[0] ?? 0, openTo: p.open?.[1] ?? 24 })),
  citizens: NAMED_CITIZENS.map(c => ({ id: c.id, name: c.name, role: c.role, place: c.place, x: c.x, z: c.z, outfit: c.outfit, line: c.line, topi: c.topi || [] })),
  dialogue: {
    greetings: GREETINGS, lines: LINES,
    voices: Object.entries(VOICES).map(([id, text]) => ({ id, text })),
    jobs: NAMED_CITIZENS.map(c => ({ id: c.id, missions: jobsFor(c.id) }))
  },
  missions: missionData,
  models: MODELS.map(m => ({ id: m.id, height: m.height, solid: !!m.solid, replaces: m.replaces || '', building: m.building || '', water: !!m.water, file: manifest.models?.[m.id]?.file || '', placements: m.placements.map(p => ({ x: p.x, z: p.z, yaw: p.yaw || 0, y: p.y ?? -999 })) })),
  characters: CHARACTERS.map(c => ({ id: c.id, height: c.height })),
  audio: { ambience: await audioList('ambience', AMBIENCE), effects: await audioList('effects', EFFECTS), music: await audioList('music', MUSIC), voices: voiceClips }
};

await mkdir(path.join(UNITY, 'models'), { recursive: true });
await writeFile(path.join(UNITY, 'world.json'), `${JSON.stringify(data, null, 2)}\n`);
let copied = 0;
try { await readdir(OUT); await cp(OUT, path.join(UNITY, 'generated'), { recursive: true, filter: src => !src.endsWith('.part') && !src.endsWith('.tmp') }); copied = 1; } catch { /* nothing generated yet */ }
console.log(`Unity data: ${data.pois.length} places, ${data.citizens.length} people, ${data.missions.length} missions, ${voiceClips.length} voice clips${copied ? ', generated assets copied' : ''}.`);
console.log(`→ ${path.relative(ROOT, UNITY)}`);
