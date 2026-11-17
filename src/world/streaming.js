import { CHUNK_SIZE, WORLD_MIN, WORLD_MAX, keyOf } from './util.js';

export const CHUNK_TRANSITIONS = Object.freeze({
  queued: ['generating', 'cancelled'], generating: ['resident', 'cancelled', 'failed'],
  resident: ['evicting'], evicting: ['unloaded'], cancelled: [], failed: []
});

export class ChunkStreamer {
  constructor(generator, { radius = 2, retainRadius = 3, maxResident = 36, onResident = () => {}, onEvict = () => {} } = {}) {
    this.generator = generator; this.radius = radius; this.retainRadius = retainRadius; this.maxResident = maxResident;
    this.onResident = onResident; this.onEvict = onEvict;
    this.resident = new Map(); this.pending = new Map(); this.queue = []; this.current = null;
    this.center = { x: 0, z: 0 }; this.revision = 0; this.log = []; this.errors = []; this.disposed = false;
  }
  transition(job, next) {
    if (!CHUNK_TRANSITIONS[job.state]?.includes(next)) throw new Error(`Invalid chunk transition ${job.state} -> ${next}`);
    this.log.push({ id: job.id, from: job.state, to: next }); if (this.log.length > 64) this.log.shift(); job.state = next;
  }
  update(position) {
    const cx = Math.max(WORLD_MIN, Math.min(WORLD_MAX, Math.floor(position.x / CHUNK_SIZE))), cz = Math.max(WORLD_MIN, Math.min(WORLD_MAX, Math.floor(position.z / CHUNK_SIZE)));
    this.center = { x: cx, z: cz };
    for (const job of this.pending.values()) if (Math.max(Math.abs(job.cx - cx), Math.abs(job.cz - cz)) > this.retainRadius) this.cancel(job.id);
    for (let z = cz - this.radius; z <= cz + this.radius; z++) for (let x = cx - this.radius; x <= cx + this.radius; x++) {
      if (x < WORLD_MIN || x > WORLD_MAX || z < WORLD_MIN || z > WORLD_MAX || x * 80 > 440) continue;
      const d = Math.max(Math.abs(x - cx), Math.abs(z - cz)), level = d <= 1 ? 0 : 1, id = keyOf(x, z);
      const existing = this.resident.get(id), pending = this.pending.get(id);
      if (existing && existing.level <= level || pending) continue;
      const job = { id, cx: x, cz: z, level, priority: Math.hypot(x - cx, z - cz), state: 'queued', controller: new AbortController(), iterator: null };
      this.pending.set(id, job); this.queue.push(job);
    }
    this.queue.sort((a, b) => Math.hypot(a.cx - cx, a.cz - cz) - Math.hypot(b.cx - cx, b.cz - cz));
    const evict = [...this.resident.values()].sort((a, b) => Math.hypot(b.cx - cx, b.cz - cz) - Math.hypot(a.cx - cx, a.cz - cz));
    for (const chunk of evict) if (Math.max(Math.abs(chunk.cx - cx), Math.abs(chunk.cz - cz)) > this.retainRadius || this.resident.size > this.maxResident) this.evict(chunk.id);
  }
  pump(budgetMs = 4) {
    if (this.disposed) return;
    const start = performance.now(); let iterations = 0;
    while (performance.now() - start < budgetMs && iterations++ < 128) {
      if (!this.current) {
        const job = this.queue.shift(); if (!job) return;
        if (job.state !== 'queued') continue;
        this.transition(job, 'generating'); job.iterator = this.generator.build(job.cx, job.cz, job.level, job.controller.signal); this.current = job;
      }
      const job = this.current;
      if (job.controller.signal.aborted) { this.current = null; continue; }
      try {
        const step = job.iterator.next();
        if (step.done) {
          this.current = null; this.pending.delete(job.id);
          if (!step.value || job.controller.signal.aborted) continue;
          const previous = this.resident.get(job.id);
          if (!previous && this.resident.size >= this.maxResident) {
            const victim = [...this.resident.values()].sort((a, b) => Math.hypot(b.cx - this.center.x, b.cz - this.center.z) - Math.hypot(a.cx - this.center.x, a.cz - this.center.z))[0];
            if (victim) this.evict(victim.id);
          }
          this.transition(job, 'resident'); const chunk = { ...step.value, state: 'resident', generation: ++this.revision };
          this.resident.set(job.id, chunk); this.onResident(chunk, previous);
          if (previous) { this.transition(previous, 'evicting'); this.onEvict(previous, true); this.transition(previous, 'unloaded'); }
        }
      } catch (error) {
        this.transition(job, 'failed'); this.errors.push({ id: job.id, error: error.message }); this.pending.delete(job.id); this.current = null;
        if (this.errors.length > 12) this.errors.shift();
      }
    }
  }
  cancel(id) {
    const job = this.pending.get(id); if (!job) return;
    job.controller.abort(); if (job.state === 'queued' || job.state === 'generating') this.transition(job, 'cancelled');
    job.iterator?.return?.(); this.pending.delete(id); this.queue = this.queue.filter(x => x !== job); if (this.current === job) this.current = null;
  }
  evict(id) {
    const chunk = this.resident.get(id); if (!chunk) return;
    this.transition(chunk, 'evicting'); this.onEvict(chunk, false); this.transition(chunk, 'unloaded'); this.resident.delete(id); this.revision++;
  }
  dispose() { this.disposed = true; for (const id of [...this.pending.keys()]) this.cancel(id); for (const id of [...this.resident.keys()]) this.evict(id); }
}
