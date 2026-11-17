import { POIS, WORLD_MIN, WORLD_MAX, distanceXZ, terrainHeight } from '../world/util.js';
import { describeChunk } from '../world/city.js';

export function drawCityMap(context, width, height, simulation, { full = false, range = 105, yaw = 0, detailed = false } = {}) {
  const player = simulation.player.position, center = full ? { x: 40, z: 40 } : player, radius = full ? 550 : range;
  const scale = Math.min(width, height) / (radius * 2), c = Math.cos(yaw), s = Math.sin(yaw);
  const project = (x, z) => { const dx = x - center.x, dz = z - center.z; return [width / 2 + (dx * c - dz * s) * scale, height / 2 + (dx * s + dz * c) * scale]; };
  const polygon = (points, color, stroke = null) => { context.beginPath(); points.forEach(([x, z], i) => { const p = project(x, z); if (i) context.lineTo(...p); else context.moveTo(...p); }); context.closePath(); context.fillStyle = color; context.fill(); if (stroke) { context.strokeStyle = stroke; context.stroke(); } };
  context.clearRect(0, 0, width, height); context.fillStyle = '#202e2b'; context.fillRect(0, 0, width, height);
  polygon([[436, -600], [1200, -600], [1200, 800], [436, 800]], '#234552');
  polygon([[-500, 230], [436, 230], [436, 600], [-500, 600]], '#344630');
  for (let i = WORLD_MIN; i <= WORLD_MAX + 1; i++) {
    const x = i * 80; polygon([[x - 7, -500], [x + 7, -500], [x + 7, 560], [x - 7, 560]], '#151f23');
    polygon([[-500, x - 7], [436, x - 7], [436, x + 7], [-500, x + 7]], '#151f23');
  }
  for (let z = WORLD_MIN; z <= WORLD_MAX; z++) for (let x = WORLD_MIN; x <= 5; x++) {
    if (!full && Math.hypot(x * 80 + 40 - player.x, z * 80 + 40 - player.z) > range * 1.8) continue;
    const plan = describeChunk(x, z);
    if (plan.publicSpace) polygon([[plan.ox + 14, plan.oz + 14], [plan.ox + 66, plan.oz + 14], [plan.ox + 66, plan.oz + 66], [plan.ox + 14, plan.oz + 66]], '#4b5546');
    for (const b of plan.buildings) {
      const px = plan.ox + b.x, pz = plan.oz + b.z;
      if (detailed) polygon([[px - b.hx + b.height * .1, pz - b.hz + b.height * .13], [px + b.hx + b.height * .1, pz - b.hz + b.height * .13], [px + b.hx + b.height * .1, pz + b.hz + b.height * .13], [px - b.hx + b.height * .1, pz + b.hz + b.height * .13]], '#101b2160');
      const color = b.color.map(v => Math.round(Math.pow(v, .65) * 170));
      polygon([[px - b.hx, pz - b.hz], [px + b.hx, pz - b.hz], [px + b.hx, pz + b.hz], [px - b.hx, pz + b.hz]], `rgb(${color.join(',')})`, '#80908240');
    }
  }
  if (simulation.route.length > 1) {
    context.beginPath(); simulation.route.forEach((p, i) => { const q = project(p.x, p.z); if (i) context.lineTo(...q); else context.moveTo(...q); });
    context.strokeStyle = '#e7bb75'; context.lineWidth = full ? 3 : 2.5; context.lineCap = 'round'; context.lineJoin = 'round'; context.stroke();
  }
  for (const poi of POIS) {
    const p = project(poi.x, poi.z); if (p[0] < -50 || p[0] > width + 50 || p[1] < -50 || p[1] > height + 50) continue;
    context.beginPath(); context.arc(...p, full ? 5 : 3.2, 0, Math.PI * 2); context.fillStyle = poi.kind === 'work' ? '#d2ad73' : '#8cbfba'; context.fill();
    if (full) { context.font = `${Math.max(10, width / 90)}px system-ui`; context.fillStyle = '#e0e5dc'; context.fillText(poi.name, p[0] + 9, p[1] + 4); }
  }
  for (const agent of simulation.citizens.visible) { const p = project(agent.position.x, agent.position.z); context.beginPath(); context.arc(...p, detailed ? 3 : 1.5, 0, Math.PI * 2); context.fillStyle = agent.named ? '#a1d8d0' : '#d3d6c5'; context.fill(); }
  for (const car of [...simulation.traffic.visible, { position: simulation.vehicle.body.position, yaw: 0, own: true }]) {
    const p = project(car.position.x, car.position.z); context.save(); context.translate(...p); context.rotate((car.yaw || 0) + yaw); context.fillStyle = car.own ? '#6fc0d2' : '#b2b7ae'; context.fillRect(-Math.max(1.5, .9 * scale), -Math.max(2.2, 1.8 * scale), Math.max(3, 1.8 * scale), Math.max(4.4, 3.6 * scale)); context.restore();
  }
  const p = project(player.x, player.z); context.save(); context.translate(...p); context.rotate(simulation.player.yaw + yaw); context.beginPath(); context.moveTo(0, -8); context.lineTo(-5, 5); context.lineTo(0, 2); context.lineTo(5, 5); context.closePath(); context.fillStyle = '#fff4d9'; context.fill(); context.restore();
  const goal = simulation.goal(); if (goal?.position) { const q = project(goal.position.x, goal.position.z); context.strokeStyle = '#f3c27a'; context.lineWidth = 2; context.beginPath(); context.arc(...q, 8, 0, Math.PI * 2); context.stroke(); }
  return { project, unproject: (px, py) => { const dx = (px - width / 2) / scale, dz = (py - height / 2) / scale; return { x: center.x + dx * c + dz * s, z: center.z - dx * s + dz * c }; } };
}

export class MapRenderer {
  constructor(canvas, simulation) { this.canvas = canvas; this.simulation = simulation; this.context = canvas.getContext('2d'); if (!this.context) throw new Error('Canvas rendering is unavailable.'); this.settings = { scale: .8, gi: false, fog: true, ssr: false, exposure: 1, fogDensity: .004 }; this.stats = {}; this.mode = 'map'; }
  render() {
    const ratio = Math.min(window.devicePixelRatio || 1, 1.5), w = Math.round(this.canvas.clientWidth * ratio), h = Math.round(this.canvas.clientHeight * ratio);
    if (this.canvas.width !== w || this.canvas.height !== h) { this.canvas.width = w; this.canvas.height = h; }
    this.mapping = drawCityMap(this.context, w, h, this.simulation, { range: 72 + this.simulation.camera.distance * 4, yaw: this.simulation.camera.yaw, detailed: true });
    this.stats = { width: w, height: h, triangles: 0, draws: 0, cpuMs: 0, gpuMs: null }; this.width = w; this.height = h;
  }
  project(position, width, height) { if (!this.mapping) return null; const [x, y] = this.mapping.project(position.x, position.z); return { x: x * width / this.canvas.width, y: y * height / this.canvas.height }; }
  dispose() { this.mapping = null; this.context.clearRect(0, 0, this.canvas.width, this.canvas.height); }
}
