#!/usr/bin/env node
// Writes docs/TRIPO_PROMPTS.md: a copy-paste sheet for generating every model on the
// Tripo website by hand. File names match what the Unity build loads.
//   npm run tripo:prompts
import { writeFile } from 'node:fs/promises';
import path from 'node:path';
import { MODELS, CHARACTERS } from '../src/assets/catalog.js';
import { ROOT } from './lib/common.mjs';

// Props for the 2000s-nostalgia chapter (planned; not placed in the world yet).
const NOSTALGIA = [
  ['chungi', 'Nepali chungi: a small kick-ball made of bundled rubber bands, colourful red yellow and green rubber, single object'],
  ['wrestling_cards', 'Stack of 2000s WWE wrestling trading cards held by a rubber band, a few cards fanned out, shiny printed cards, single object'],
  ['football_itta', 'Two piles of red bricks used as a street football goal with a worn leather football, Nepali street game, single object'],
  ['ping_swing', 'Tall Dashain ping swing made from four long bamboo poles tied at the top with a rope seat, decorated with marigolds, single object'],
  ['tihar_diyo', 'Row of small clay oil lamps (diyo) burning with flames, on a stone step with marigold flowers, Tihar festival, single object'],
  ['juju_dhau_stall', 'Bhaktapur street stall selling juju dhau yogurt in clay pots, wooden table, tarpaulin roof, single object'],
  ['tempo', 'Electric safa tempo three-wheeler from Kathmandu, green and white, passengers bench in back, single vehicle'],
  ['firefly_jar', 'Glass jar with glowing fireflies (junkiri) inside, cloth lid tied with string, single object'],
  ['river_rocks', 'Cluster of large smooth river boulders from a Himalayan stream with moss, single object, no water']
];

// Recommended Tripo face limits (triangles). Residents repeat 48 times, so keep them lean.
const CHAR_FACES = { player: 25000, resident_m: 10000, resident_f: 10000, car: 20000 };
const charFaces = id => CHAR_FACES[id] ?? 20000;
const NOSTALGIA_FACES = { salon_shop: 40000, cyber_cafe: 40000, ping_swing: 15000, tempo: 25000, juju_dhau_stall: 20000, river_rocks: 15000 };
const nostalgiaFaces = id => NOSTALGIA_FACES[id] ?? 8000;
const CHAR_NOTE = 'In Tripo: Generate → then **Rig** (humanoid) → **Animate**, adding an *idle* and a *walk* animation → Export **GLB** (with animation). Unrigged models still work; they just do not animate.';
const lines = [
  '# Tripo prompts for Phewa Valley', '',
  'Copy each prompt into [Tripo](https://www.tripo3d.ai) (Text to 3D), download as **GLB**, and save it with the exact file name shown.', '',
  '**Where the files go (Unity):** `unity/PhewaValley/Assets/StreamingAssets/PhewaValley/models/<file>.glb`. In the editor, use **Phewa Valley ▸ Open Tripo models folder**. Press Play; the game picks them up automatically and anything missing keeps its built-in look.', '',
  '**Settings that work well:** Text to 3D, latest model, Texture **on**, PBR **on**, Download format **GLB**. Do not choose Draco or mesh compression.', '',
  '**Polycount:** set Tripo’s *Face limit* (in the advanced generation settings) to the number shown for each model. All numbers are **triangles**: keep Tripo’s topology on *Triangle*. Quad mode counts quads (2 triangles each) and exports FBX, which the game cannot load. Smart Low Poly caps out at about 20,000 triangles, so use the normal generation for the 40k–60k landmarks (pagoda, stupa, chautari, salon, cyber cafe). If a model comes out heavier, run Tripo’s *Smart Low Poly* or *Retopology* before downloading. Keep textures at 2K; detail mostly comes from the texture, not the triangles. With every model installed, the worst case (all 48 residents in view) is about 1.4 M triangles, which a mid-range desktop GPU handles comfortably.', '',
  '## 1. Landmarks and props (placed in the world now)', '',
  '| File | Face limit | In-game height | Where it appears |', '|---|---|---|---|',
  ...MODELS.map(m => `| \`${m.id}.glb\` | ${m.faceLimit.toLocaleString('en-US')} | ${m.height} m | ${m.placements.length} placement${m.placements.length > 1 ? 's' : ''}${m.replaces ? ` · replaces the built-in ${m.replaces}` : ''} |`), '',
  ...MODELS.flatMap(m => [`### ${m.id}.glb`, '', `Face limit: **${m.faceLimit.toLocaleString('en-US')}**`, '', '```text', m.prompt, '```', '']),
  '## 2. Characters and the car', '', CHAR_NOTE, '',
  '| File | Face limit | Who |', '|---|---|---|',
  ...CHARACTERS.map(c => `| \`${c.id}.glb\` | ${charFaces(c.id).toLocaleString('en-US')} | ${c.id === 'player' ? 'You' : c.id === 'car' ? 'Your Maruti (not rigged)' : c.id.startsWith('resident') ? `Wandering residents (${c.id.endsWith('_f') ? 'women' : 'men'})` : `Named character · id ${c.id}`} |`), '',
  ...CHARACTERS.flatMap(c => [`### ${c.id}.glb`, '', `Face limit: **${charFaces(c.id).toLocaleString('en-US')}**`, '', '```text', c.prompt, '```', '']),
  '## 3. 2000s nostalgia set (next chapter)', '',
  'These are for the upcoming street games, salon, cyber cafe and festivals. Generate them now if you like and keep them in the same folder; the next update will place them.', '',
  ...NOSTALGIA.flatMap(([id, prompt]) => [`### ${id}.glb`, '', `Face limit: **${nostalgiaFaces(id).toLocaleString('en-US')}**`, '', '```text', `${prompt}. Nepal, early 2000s, realistic materials, game-ready prop, no background`, '```', ''])
];
const file = path.join(ROOT, 'docs', 'TRIPO_PROMPTS.md');
await writeFile(file, lines.join('\n'));
console.log(`Wrote ${path.relative(ROOT, file)} (${MODELS.length + CHARACTERS.length + NOSTALGIA.length} prompts).`);
