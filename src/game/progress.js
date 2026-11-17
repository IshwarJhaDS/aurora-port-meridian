import { clamp } from '../math.js';

export const SAVE_KEY = 'aurora.port-meridian.v2';
export function defaultProgress() {
  return { version: 2, cash: 250, reputation: 0, minutes: 16 * 60 + 20, missions: {}, tracked: 'arrival', receipts: [], conversations: {}, destroyed: [], repaired: [], jobCount: 0, courierCycle: 0, player: [34, 0, 29], car: [15, .75, 49], weather: null, settings: { quality: 'cinematic', sound: false } };
}
const finiteVector = (value, fallback) => Array.isArray(value) && value.length === 3 && value.every(x => Number.isFinite(x) && Math.abs(x) < 2000) ? value : fallback;
export function validateProgress(data) {
  const result = defaultProgress(); if (!data || data.version !== 2) return result;
  result.cash = clamp(Number(data.cash) || 0, 0, 10000000); result.reputation = clamp(Number(data.reputation) || 0, 0, 10000);
  result.minutes = ((Number(data.minutes) || result.minutes) % 1440 + 1440) % 1440;
  result.player = finiteVector(data.player, result.player); result.car = finiteVector(data.car, result.car);
  for (const key of ['destroyed', 'repaired', 'receipts']) result[key] = Array.isArray(data[key]) ? [...new Set(data[key].filter(x => typeof x === 'string' && x.length < 150))].slice(-2000) : [];
  result.jobCount = clamp(Math.floor(Number(data.jobCount) || 0), 0, 100000); result.courierCycle = clamp(Math.floor(Number(data.courierCycle) || 0), 0, 100000);
  if (data.missions && typeof data.missions === 'object' && !Array.isArray(data.missions)) for (const [id, state] of Object.entries(data.missions)) {
    if (['__proto__','constructor','prototype'].includes(id) || id.length > 64 || !state || !['active', 'completed'].includes(state.status)) continue;
    result.missions[id] = { status: state.status, step: clamp(Math.floor(Number(state.step) || 0), 0, 20), flags: Array.isArray(state.flags) ? state.flags.filter(x => typeof x === 'string').slice(0, 30) : [], remaining: clamp(Number.isFinite(state.remaining) ? state.remaining : 150, 0, 300), variant: clamp(Math.floor(Number(state.variant) || 0), 0, 2) };
  }
  if (data.conversations && typeof data.conversations === 'object') for (const [id, n] of Object.entries(data.conversations).slice(0, 100)) result.conversations[id] = clamp(Number(n) || 0, 0, 10000);
  if (typeof data.tracked === 'string') result.tracked = data.tracked;
  if (data.settings) result.settings = { quality: ['performance', 'balanced', 'cinematic'].includes(data.settings.quality) ? data.settings.quality : 'cinematic', sound: Boolean(data.settings.sound) };
  result.weather = data.weather; return result;
}

export class ProgressStore {
  constructor(storage = null) { this.storage = storage; this.state = defaultProgress(); this.available = !!storage; this.lastError = ''; }
  load() { try { const raw = this.storage?.getItem(SAVE_KEY); if (raw) this.state = validateProgress(JSON.parse(raw)); } catch (error) { this.lastError = error.message; } return this.state; }
  save() { try { if (!this.storage) return false; this.storage.setItem(SAVE_KEY, JSON.stringify(this.state)); this.available = true; return true; } catch (error) { this.available = false; this.lastError = error.message; return false; } }
  reward(receipt, cash, reputation = 0) {
    if (this.state.receipts.includes(receipt)) return false;
    this.state.receipts.push(receipt); this.state.cash += cash; this.state.reputation += reputation; this.save(); return true;
  }
  reset() { this.state = defaultProgress(); this.save(); return this.state; }
}
