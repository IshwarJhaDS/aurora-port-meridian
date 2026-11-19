#!/usr/bin/env node
// Generates voices, ambience, effects and music with ElevenLabs and records them in
// assets/generated/manifest.json. Re-running only creates what is missing.
//
//   npm run assets:audio                          # everything missing
//   npm run assets:audio -- --only ambience,music # sections: voices, ambience, effects, music
//   npm run assets:audio -- --only voices:mira    # one speaker
//   npm run assets:audio -- --dry-run             # list lines and character count, no API calls
import path from 'node:path';
import { AMBIENCE, EFFECTS, MUSIC, VOICE_CAST, voiceKey } from '../src/assets/catalog.js';
import { spokenScript } from './lib/script.mjs';
import { loadEnv, requireEnv, flags, readManifest, updateManifest, exists, writeBinary, request, pool, OUT } from './lib/common.mjs';

const opts = flags();
if (opts.help) { console.log('Usage: node tools/generate-audio.mjs [--only voices,ambience,effects,music,voices:<speaker>] [--force] [--dry-run]'); process.exit(0); }
await loadEnv();
const API = (process.env.ELEVENLABS_BASE_URL || 'https://api.elevenlabs.io').replace(/\/$/, '');

const sections = opts.only ? new Set([...opts.only].map(s => s.split(':')[0])) : new Set(['voices', 'ambience', 'effects', 'music']);
const speakersOnly = opts.only ? new Set([...opts.only].filter(s => s.startsWith('voices:')).map(s => s.slice(7))) : new Set();
const manifest = await readManifest(), have = async (group, id) => !opts.force && manifest.audio?.[group]?.[id] && await exists(path.join(OUT, manifest.audio[group][id]));

// Voice ids come from .env so you can cast Nepali / South Asian voices from your ElevenLabs library.
// The fallbacks are ElevenLabs' default premade voices, which work on every account.
const DEFAULT_VOICES = { FEMALE: '21m00Tcm4TlvDq8ikWAM', MALE: 'pNInz6obpgDQGcFmaJgB' };
const voiceFor = speaker => {
  const name = VOICE_CAST[speaker], base = name.includes('FEMALE') ? 'ELEVENLABS_VOICE_FEMALE' : 'ELEVENLABS_VOICE_MALE';
  return process.env[name] || process.env[base] || DEFAULT_VOICES[base.endsWith('FEMALE') ? 'FEMALE' : 'MALE'];
};

const jobs = [];
if (sections.has('voices')) for (const [speaker, lines] of spokenScript()) {
  if (speakersOnly.size && !speakersOnly.has(speaker)) continue;
  for (const text of lines) { const key = voiceKey(speaker, text); if (!await have('voices', key)) jobs.push({ kind: 'voices', id: key, speaker, text, file: `voices/${speaker}/${key}.mp3` }); }
}
if (sections.has('ambience')) for (const a of AMBIENCE) if (!await have('ambience', a.id)) jobs.push({ kind: 'ambience', id: a.id, text: a.prompt, def: a, file: `ambience/${a.id}.mp3` });
if (sections.has('effects')) for (const e of EFFECTS) if (!await have('effects', e.id)) jobs.push({ kind: 'effects', id: e.id, text: e.prompt, def: e, file: `effects/${e.id}.mp3` });
if (sections.has('music')) for (const m of MUSIC) if (!await have('music', m.id)) jobs.push({ kind: 'music', id: m.id, text: m.prompt, def: m, file: `music/${m.id}.mp3` });

if (!jobs.length) { console.log('All requested audio already exists. Use --force to regenerate.'); process.exit(0); }
const voiceJobs = jobs.filter(j => j.kind === 'voices'), characters = voiceJobs.reduce((n, j) => n + j.text.length, 0);
console.log(`${voiceJobs.length} voice line(s) (${characters.toLocaleString()} characters), ${jobs.filter(j => j.kind === 'ambience').length} ambience loop(s), ${jobs.filter(j => j.kind === 'effects').length} effect(s), ${jobs.filter(j => j.kind === 'music').length} music track(s).`);
if (opts.dryRun) { for (const j of jobs) console.log(`[${j.kind}${j.speaker ? ':' + j.speaker : ''}] ${j.text}`); process.exit(0); }
const usingDefaults = new Set(voiceJobs.map(j => j.speaker).filter(s => !process.env[VOICE_CAST[s]] && !process.env[VOICE_CAST[s].includes('FEMALE') ? 'ELEVENLABS_VOICE_FEMALE' : 'ELEVENLABS_VOICE_MALE']));
if (usingDefaults.size) console.warn(`! No voice id set for: ${[...usingDefaults].join(', ')}. Using ElevenLabs default voices. Set ELEVENLABS_VOICE_* in .env for Nepali-accented voices.`);

const key = requireEnv('ELEVENLABS_API_KEY', 'Create one at https://elevenlabs.io/app/settings/api-keys.');
const headers = { 'xi-api-key': key, 'Content-Type': 'application/json', Accept: 'audio/mpeg' };
const format = process.env.ELEVENLABS_OUTPUT_FORMAT || 'mp3_44100_128';

async function run(job) {
  let url, body;
  if (job.kind === 'voices') {
    url = `${API}/v1/text-to-speech/${encodeURIComponent(voiceFor(job.speaker))}?output_format=${format}`;
    body = { text: job.text, model_id: process.env.ELEVENLABS_TTS_MODEL || 'eleven_multilingual_v2', voice_settings: { stability: .5, similarity_boost: .8, style: .15, use_speaker_boost: true } };
  } else if (job.kind === 'music') {
    url = `${API}/v1/music?output_format=${format}`;
    body = { prompt: job.text, music_length_ms: job.def.lengthMs, force_instrumental: true, model_id: process.env.ELEVENLABS_MUSIC_MODEL || 'music_v2_5' };
  } else {
    url = `${API}/v1/sound-generation?output_format=${format}`;
    body = { text: job.text, duration_seconds: job.def.duration, loop: Boolean(job.def.loop), prompt_influence: .45, ...(process.env.ELEVENLABS_SFX_MODEL ? { model_id: process.env.ELEVENLABS_SFX_MODEL } : {}) };
  }
  const response = await request(url, { method: 'POST', headers, body: JSON.stringify(body) }, { label: `${job.kind}:${job.speaker || job.id}` });
  const bytes = new Uint8Array(await response.arrayBuffer()); if (bytes.length < 512) throw new Error(`${job.kind}:${job.id}: suspiciously small response`);
  await writeBinary(job.file, bytes);
  await updateManifest(m => { m.audio[job.kind][job.id] = job.file; });
  console.log(`✓ ${job.kind}${job.speaker ? ` · ${job.speaker}` : ` · ${job.id}`}: ${job.text.slice(0, 70)}${job.text.length > 70 ? '…' : ''}`);
}

// Long-running music and effects first, then voices in parallel.
const failures = await pool([...jobs.filter(j => j.kind !== 'voices'), ...voiceJobs], Number(process.env.ELEVENLABS_CONCURRENCY) || 3, run);
console.log(failures.length ? `\nDone with ${failures.length} failure(s). Re-run to retry only what is missing.` : '\nAudio ready. Reload the game; turn on "Spoken conversations" in the menu.');
process.exit(failures.length ? 1 : 0);
