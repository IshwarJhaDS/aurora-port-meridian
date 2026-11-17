import { Vec3 } from '../math.js';
import { MinHeap, WORLD_MIN, WORLD_MAX, terrainHeight, distanceXZ, keyOf } from './util.js';

const RING = [[10, 10], [40, 10], [70, 10], [70, 40], [70, 70], [40, 70], [10, 70], [10, 40]];

export class SidewalkGraph {
  constructor() {
    this.nodes = new Map();
    for (let z = WORLD_MIN; z <= WORLD_MAX; z++) for (let x = WORLD_MIN; x <= 5; x++) for (let i = 0; i < 8; i++) {
      const [dx, dz] = RING[i], px = x * 80 + dx, pz = z * 80 + dz;
      if (px > 430) continue;
      const id = `${x}:${z}:${i}`;
      this.nodes.set(id, { id, cx: x, cz: z, ring: i, position: new Vec3(px, terrainHeight(px, pz) + .18, pz), edges: [] });
    }
    const connect = (a, b, crossing = false) => {
      const x = this.nodes.get(a), y = this.nodes.get(b); if (!x || !y) return;
      if (!x.edges.some(e => e.id === b)) x.edges.push({ id: b, cost: distanceXZ(x.position, y.position), crossing });
      if (!y.edges.some(e => e.id === a)) y.edges.push({ id: a, cost: distanceXZ(x.position, y.position), crossing });
    };
    for (const n of this.nodes.values()) {
      connect(n.id, `${n.cx}:${n.cz}:${(n.ring + 1) % 8}`);
      if (n.ring === 3) connect(n.id, `${n.cx + 1}:${n.cz}:7`, true);
      if (n.ring === 5) connect(n.id, `${n.cx}:${n.cz + 1}:1`, true);
      if (n.ring === 2) connect(n.id, `${n.cx + 1}:${n.cz}:0`, true);
      if (n.ring === 4) connect(n.id, `${n.cx + 1}:${n.cz}:6`, true);
      if (n.ring === 6) connect(n.id, `${n.cx}:${n.cz + 1}:0`, true);
      if (n.ring === 4) connect(n.id, `${n.cx}:${n.cz + 1}:2`, true);
    }
  }
  nearest(position) {
    const cx = Math.floor(position.x / 80), cz = Math.floor(position.z / 80); let best = null, distance = Infinity;
    for (let z = cz - 1; z <= cz + 1; z++) for (let x = cx - 1; x <= cx + 1; x++) for (let i = 0; i < 8; i++) {
      const n = this.nodes.get(`${x}:${z}:${i}`); if (!n) continue; const d = distanceXZ(n.position, position); if (d < distance) { distance = d; best = n; }
    }
    if (!best) for (const n of this.nodes.values()) { const d = distanceXZ(n.position, position); if (d < distance) { distance = d; best = n; } }
    return best;
  }
  route(from, to, { crossingPenalty = 4 } = {}) {
    const start = typeof from === 'string' ? this.nodes.get(from) : this.nearest(from), goal = typeof to === 'string' ? this.nodes.get(to) : this.nearest(to);
    if (!start || !goal) return [];
    const open = new MinHeap(), cost = new Map([[start.id, 0]]), previous = new Map();
    open.push(start.id, distanceXZ(start.position, goal.position)); let iterations = 0;
    while (open.size && iterations++ < 12000) {
      const id = open.pop().value, node = this.nodes.get(id);
      if (id === goal.id) {
        const path = [node]; let cursor = id;
        while (previous.has(cursor)) { cursor = previous.get(cursor); path.push(this.nodes.get(cursor)); }
        return path.reverse().map(n => n.position.clone());
      }
      for (const edge of node.edges) {
        const next = this.nodes.get(edge.id), candidate = cost.get(id) + edge.cost + (edge.crossing ? crossingPenalty : 0);
        if (candidate >= (cost.get(edge.id) ?? Infinity)) continue;
        cost.set(edge.id, candidate); previous.set(edge.id, id); open.push(edge.id, candidate + distanceXZ(next.position, goal.position));
      }
    }
    return [];
  }
}

export class CubicBezier {
  constructor(a, b, c, d, samples = 32) {
    this.a = a; this.b = b; this.c = c; this.d = d; this.arc = [0]; let previous = a;
    for (let i = 1; i <= samples; i++) { const p = this.at(i / samples); this.arc.push(this.arc.at(-1) + previous.distance(p)); previous = p; }
    this.length = this.arc.at(-1); this.samples = samples;
  }
  at(t) { const u = 1 - t; return this.a.clone().scale(u ** 3).addScaled(this.b, 3 * u * u * t).addScaled(this.c, 3 * u * t * t).addScaled(this.d, t ** 3); }
  tangent(t) { const u = 1 - t; return Vec3.sub(this.b, this.a).scale(3 * u * u).addScaled(Vec3.sub(this.c, this.b), 6 * u * t).addScaled(Vec3.sub(this.d, this.c), 3 * t * t).normalize(); }
  parameterAtDistance(distance) {
    const d = Math.max(0, Math.min(this.length, distance)); let lo = 0, hi = this.samples;
    while (lo + 1 < hi) { const mid = (lo + hi) >> 1; if (this.arc[mid] < d) lo = mid; else hi = mid; }
    return (lo + (d - this.arc[lo]) / Math.max(1e-8, this.arc[hi] - this.arc[lo])) / this.samples;
  }
}
