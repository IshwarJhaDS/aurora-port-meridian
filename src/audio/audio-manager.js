// Web Audio playback for generated ElevenLabs content. Everything degrades quietly:
// a missing clip means silence (ambience) or browser speech (dialogue).
import { voiceKey, sentences, isUnvoiced } from '../assets/catalog.js';

const BASE = './assets/generated/';
const TEMPLES = [{ x: 47, z: 47 }, { x: -120, z: 214 }];

export class AudioManager {
  constructor(audio = {}, settings = {}) {
    this.audio = { ambience: {}, effects: {}, music: {}, voices: {}, ...audio };
    this.settings = settings; this.context = null; this.buffers = new Map(); this.loops = new Map(); this.voice = []; this.voiceToken = 0;
  }
  get hasVoices() { return Object.keys(this.audio.voices).length > 0; }
  /** Must be called from a user gesture (the "enter" click). */
  unlock() {
    if (this.context || typeof AudioContext === 'undefined') { this.context?.resume?.(); return; }
    const c = this.context = new AudioContext();
    this.master = c.createGain(); this.master.connect(c.destination);
    this.ambienceBus = c.createGain(); this.ambienceBus.gain.value = .55; this.ambienceBus.connect(this.master);
    this.musicBus = c.createGain(); this.musicBus.gain.value = .16; this.musicBus.connect(this.master);
    this.voiceBus = c.createGain(); this.voiceBus.gain.value = 1; this.voiceBus.connect(this.master);
    this.effectBus = c.createGain(); this.effectBus.gain.value = .7; this.effectBus.connect(this.master);
    if (this.audio.music.theme) this.loop('music:theme', BASE + this.audio.music.theme, this.musicBus, 1);
  }
  buffer(url) {
    if (!this.buffers.has(url)) this.buffers.set(url, fetch(url).then(r => { if (!r.ok) throw new Error(`${url}: ${r.status}`); return r.arrayBuffer(); }).then(data => this.context.decodeAudioData(data)).catch(error => { console.warn('[audio]', error.message); return null; }));
    return this.buffers.get(url);
  }
  loop(id, url, bus, level = 0) {
    if (this.loops.has(id)) return this.loops.get(id);
    const gain = this.context.createGain(); gain.gain.value = 0; gain.connect(bus);
    const entry = { gain, target: level, source: null }; this.loops.set(id, entry);
    this.buffer(url).then(buffer => {
      if (!buffer || !this.context) return;
      const source = this.context.createBufferSource(); source.buffer = buffer; source.loop = true; source.connect(gain);
      source.start(0, Math.random() * buffer.duration); entry.source = source;
    });
    return entry;
  }
  /** Called every frame with the player's surroundings; crossfades the ambience beds. */
  update({ district = 'downtown', position = null, rain = 0, night = 0, paused = false } = {}) {
    if (!this.context) return;
    const enabled = this.settings.ambience !== false, t = this.context.currentTime;
    const talking = t < (this.voiceUntil || 0);
    this.master.gain.setTargetAtTime(enabled || talking ? 1 : 0, t, .2);
    this.ambienceBus.gain.setTargetAtTime(enabled ? (talking ? .18 : paused ? .25 : .55) : 0, t, talking ? .15 : .4);
    this.musicBus.gain.setTargetAtTime(enabled ? (talking ? .08 : paused ? .45 : .32) : 0, t, .8);
    const desiredMusic = paused ? 'theme' : night > .5 ? 'night' : district === 'coast' || district === 'rural' ? 'exploration' : 'theme';
    const selectedMusic = this.audio.music[desiredMusic] ? desiredMusic : 'theme';
    for (const [id, file] of Object.entries(this.audio.music)) {
      const track = this.loop(`music:${id}`, BASE + file, this.musicBus);
      track.gain.gain.setTargetAtTime(id === selectedMusic ? 1 : 0, t, 1.2);
    }
    let temple = 0;
    if (position) for (const p of TEMPLES) temple = Math.max(temple, 1 - Math.hypot(position.x - p.x, position.z - p.z) / 55);
    temple = Math.max(0, temple);
    const town = district === 'coast' || district === 'rural' ? 0 : district === 'suburbs' || district === 'industrial' ? .65 : 1;
    const mix = {
      bazaar: town * (1 - night * .75) * (1 - temple * .5),
      temple: temple * .9,
      lake: district === 'coast' ? 1 : 0,
      hills: district === 'rural' ? 1 - night * .4 : 0,
      night: night * .85,
      rain: Math.min(1, rain * 1.2)
    };
    for (const [id, level] of Object.entries(mix)) {
      const file = this.audio.ambience[id]; if (!file) continue;
      if (level < .01 && !this.loops.has(`ambience:${id}`)) continue;
      const entry = this.loop(`ambience:${id}`, BASE + file, this.ambienceBus);
      entry.gain.gain.setTargetAtTime(level, t, 1.2);
    }
  }
  effect(id) {
    const file = this.audio.effects[id]; if (!this.context || !file || this.settings.ambience === false) return;
    this.buffer(BASE + file).then(buffer => { if (!buffer) return; const s = this.context.createBufferSource(); s.buffer = buffer; s.connect(this.effectBus); s.start(); });
  }
  /**
   * Plays pre-generated clips for every sentence of `text`. Returns false (and
   * plays nothing) when any sentence lacks a clip, so the caller can fall back.
   */
  speak(speaker, text) {
    this.stopVoice();
    if (!this.context) return false;
    const files = sentences(text).filter(s => !isUnvoiced(s)).map(s => this.audio.voices[voiceKey(speaker, s)]);
    if (!files.length || files.some(f => !f)) return false;
    const token = ++this.voiceToken;
    Promise.all(files.map(f => this.buffer(BASE + f))).then(buffers => {
      if (token !== this.voiceToken || buffers.some(b => !b)) return;
      let at = this.context.currentTime + .05;
      for (const buffer of buffers) {
        const s = this.context.createBufferSource(); s.buffer = buffer; s.connect(this.voiceBus); s.start(at); this.voice.push(s); at += buffer.duration + .08;
      }
      this.voiceUntil = at; // update() ducks the ambience until then
    });
    return true;
  }
  stopVoice() { this.voiceToken++; this.voiceUntil = 0; for (const s of this.voice) { try { s.stop(); } catch { /* already ended */ } } this.voice = []; }
  dispose() { this.stopVoice(); this.context?.close?.(); this.context = null; this.loops.clear(); this.buffers.clear(); }
}
