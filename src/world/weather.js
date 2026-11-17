import { clamp, lerp, Vec3 } from '../math.js';
import { WetnessField } from '../resources.js';
import { Random } from './util.js';

export const WEATHER_TYPES = Object.freeze({
  clear: { label: 'Clear skies', rain: 0, clouds: .12, fog: .003, wind: .15 },
  cloudy: { label: 'Overcast', rain: 0, clouds: .75, fog: .005, wind: .35 },
  rain: { label: 'Passing rain', rain: .55, clouds: .9, fog: .008, wind: .55 },
  storm: { label: 'Harbor storm', rain: 1, clouds: 1, fog: .012, wind: 1 }
});

export class WeatherSystem {
  constructor(seed = 9203) { this.random = new Random(seed); this.state = 'clear'; this.remaining = 110; this.rain = 0; this.clouds = .12; this.fog = .003; this.wind = .15; this.flash = 0; this.elapsed = 0; }
  update(dt) {
    this.elapsed += dt; this.remaining -= dt;
    if (this.remaining <= 0) {
      const next = { clear: ['cloudy', 'clear'], cloudy: ['rain', 'clear'], rain: ['storm', 'cloudy'], storm: ['rain', 'cloudy'] };
      this.state = this.random.pick(next[this.state]); this.remaining = this.random.range(70, 145);
    }
    const target = WEATHER_TYPES[this.state], t = 1 - Math.exp(-dt / 12);
    for (const k of ['rain', 'clouds', 'fog', 'wind']) this[k] = lerp(this[k], target[k], t);
    this.flash = Math.max(0, this.flash - dt * 5);
    if (this.state === 'storm' && this.random.next() < dt * .055) this.flash = 1;
  }
  get label() { return WEATHER_TYPES[this.state].label; }
  snapshot() { return { state: this.state, remaining: this.remaining, rain: this.rain, clouds: this.clouds, elapsed: this.elapsed, random: this.random.state >>> 0 }; }
  restore(data) { if (!data || !WEATHER_TYPES[data.state]) return; this.state = data.state; this.remaining = clamp(Number(data.remaining) || 80, 5, 150); this.rain = clamp(Number(data.rain) || 0, 0, 1); this.clouds = clamp(Number(data.clouds) || .1, 0, 1); this.elapsed = Math.max(0, Number(data.elapsed) || 0); this.random.state = Number(data.random) >>> 0 || 9203; }
}

export class MovingWetnessField extends WetnessField {
  constructor() { super({ size: 128, extent: 200 }); this.origin = new Vec3(); this.background = 0; }
  update(dt, position, rain, sunlight) {
    this.rain = rain; this.background = clamp(this.background + (rain * .18 - .012 * Math.max(.25, sunlight)) * dt, 0, 1);
    const cell = this.extent * 2 / this.size, x = Math.round(position.x / (cell * 16)) * cell * 16, z = Math.round(position.z / (cell * 16)) * cell * 16;
    if (x !== this.origin.x || z !== this.origin.z) {
      const dx = Math.round((x - this.origin.x) / cell), dz = Math.round((z - this.origin.z) / cell), next = new Float32Array(this.water.length);
      for (let j = 0; j < this.size; j++) for (let i = 0; i < this.size; i++) next[j * this.size + i] = i + dx >= 0 && i + dx < this.size && j + dz >= 0 && j + dz < this.size ? this.water[(j + dz) * this.size + i + dx] : this.background;
      this.water = next; this.origin.set(x, 0, z);
    }
    super.step(dt, sunlight);
  }
  sample(x, z) { return super.sample(x - this.origin.x, z - this.origin.z); }
}
