import { Vec3, clamp } from '../math.js';

export const CHUNK_SIZE = 80;
export const WORLD_MIN = -6;
export const WORLD_MAX = 6;
export const keyOf = (x, z) => `${x}:${z}`;
export const distanceXZ = (a, b) => Math.hypot(a.x - b.x, a.z - b.z);
export const wrapAngle = a => Math.atan2(Math.sin(a), Math.cos(a));
export const dampAngle = (a, b, t) => a + wrapAngle(b - a) * clamp(t, 0, 1);
export const clockText = minutes => `${String(Math.floor(minutes / 60) % 24).padStart(2, '0')}:${String(Math.floor(minutes % 60)).padStart(2, '0')}`;

export function hash(x, z = 0, salt = 0) {
  let h = Math.imul(x ^ 0x9e3779b9, 0x85ebca6b) ^ Math.imul(z ^ salt, 0xc2b2ae35);
  h = Math.imul(h ^ h >>> 16, 0x7feb352d);
  h = Math.imul(h ^ h >>> 15, 0x846ca68b);
  return (h ^ h >>> 16) >>> 0;
}

export class Random {
  constructor(seed = 1) { this.state = seed >>> 0 || 1; }
  next() {
    let t = this.state += 0x6D2B79F5;
    t = Math.imul(t ^ t >>> 15, t | 1);
    t ^= t + Math.imul(t ^ t >>> 7, t | 61);
    return ((t ^ t >>> 14) >>> 0) / 4294967296;
  }
  range(a, b) { return a + (b - a) * this.next(); }
  integer(a, b) { return Math.floor(this.range(a, b + 1)); }
  pick(values) { return values[Math.floor(this.next() * values.length)]; }
}

export class MinHeap {
  constructor() { this.values = []; }
  push(value, priority) {
    const item = { value, priority }, a = this.values;
    a.push(item);
    let i = a.length - 1;
    while (i > 0) {
      const parent = (i - 1) >> 1;
      if (a[parent].priority <= priority) break;
      a[i] = a[parent]; i = parent;
    }
    a[i] = item;
  }
  pop() {
    const a = this.values;
    if (!a.length) return null;
    const result = a[0], tail = a.pop();
    if (a.length) {
      let i = 0;
      while (true) {
        let child = i * 2 + 1;
        if (child >= a.length) break;
        if (child + 1 < a.length && a[child + 1].priority < a[child].priority) child++;
        if (a[child].priority >= tail.priority) break;
        a[i] = a[child]; i = child;
      }
      a[i] = tail;
    }
    return result;
  }
  get size() { return this.values.length; }
}

export function districtAt(x, z) {
  if (x > 420) return { id: 'coast', name: 'Phewa Lakeside', color: '#5ea6b7' };
  if (z > 230) return { id: 'rural', name: 'Sarangkot Hills', color: '#8bac72' };
  if (x > 180) return { id: 'industrial', name: 'Ghat Bazaar', color: '#d5ac79' };
  if (x < -150) return { id: 'suburbs', name: 'Newa Tole', color: '#c9b8a0' };
  if (z < -190) return { id: 'highway', name: 'Prithvi Highway', color: '#92a2b8' };
  return { id: 'downtown', name: 'Durbar Tole', color: '#d29a7a' };
}

export function terrainHeight(x, z) {
  if (x > 436) return -3.5;
  const field = Math.max(0, Math.min(1, (z - 230) / 100));
  if (!field) return 0;
  const roadDistance = n => Math.min(((n % 80) + 80) % 80, 80 - ((n % 80) + 80) % 80);
  const roadMask = clamp((Math.min(roadDistance(x), roadDistance(z)) - 12) / 13, 0, 1);
  const hills = 2.2 + 2.1 * Math.sin(x * .018) * Math.cos(z * .021);
  return hills * field * roadMask;
}

// Place ids are stable save-game keys; names and descriptions carry the setting.
export const POIS = Object.freeze([
  { id: 'plaza', name: 'Durbar Square', kind: 'community', x: 40, z: 40, description: 'The old palace square, its pagoda, and the heart of the tole.' },
  { id: 'cafe', name: 'Himalayan Chiya Pasal', kind: 'work', x: 104, z: 40, description: 'A busy tea shop. Brew milk chiya, pack momos and chat with Laxman dai.', open: [7, 22] },
  { id: 'clinic', name: 'Valley Health Post', kind: 'clinic', x: -56, z: 106, description: 'Dr. Anjali is waiting for essential medicine deliveries.', open: [6, 23] },
  { id: 'depot', name: 'Sajha Courier', kind: 'work', x: 184, z: 106, description: 'Pick up a parcel and earn an honest delivery fee.', open: [0, 24] },
  { id: 'harbor', name: 'Ghat Workshop', kind: 'work', x: 344, z: 106, description: 'Sanjay dai fixes boats, wiring and anything else by the ghats.', open: [6, 22] },
  { id: 'warehouse', name: 'Godam 08', kind: 'story', x: 344, z: -56, description: 'There is more to a missing shipment than a misplaced label.' },
  { id: 'market', name: 'Asan Haat Bazaar', kind: 'shop', x: -216, z: 40, description: 'Spices, brass pots, fresh vegetables and every bit of neighborhood gossip.' },
  { id: 'park', name: 'Shanti Stupa Garden', kind: 'park', x: -120, z: 200, description: 'Walk clockwise around the stupa and spin the prayer wheels.' },
  { id: 'lookout', name: 'Sarangkot Viewpoint', kind: 'vista', x: 40, z: 360, description: 'Climb the south road for the Himalaya at sunrise and the valley below.' },
  { id: 'waterfront', name: 'Phewa Ghat', kind: 'vista', x: 410, z: 40, description: 'Painted doongas, still water and the mountains reflected at dusk.' },
  { id: 'garage', name: 'Valley Motorworks', kind: 'garage', x: 104, z: -56, description: 'Your car is parked near Durbar Square.' }
]);
export const poiById = id => POIS.find(p => p.id === id);
export const poiPosition = id => { const p = poiById(id); if (!p) throw new Error(`Unknown place: ${id}`); return new Vec3(p.x, terrainHeight(p.x, p.z), p.z); };
export const isOpen = (poi, hour) => !poi.open || (hour >= poi.open[0] && hour < poi.open[1]);
