import { Vec3 } from '../math.js';
import { poiPosition, POIS, terrainHeight, distanceXZ } from '../world/util.js';

const talk = (target, text, place) => ({ kind: 'talk', target, text, place });
const place = (target, text) => ({ kind: 'place', target, text, place: target });
export const REPAIR_LAMPS = Object.freeze([
  { id: '4:1:lamp:0', x: 334, z: 91 }, { id: '4:0:lamp:1', x: 386, z: 69 }, { id: '3:1:lamp:0', x: 254, z: 91 }
]);

export const MISSION_DEFINITIONS = Object.freeze([
  { id: 'arrival', title: 'First steps in the valley', category: 'STORY', description: 'Find your feet in Durbar Square, greet Maya didi, follow the footpath to the tea shop and bring Laxman dai’s news home. Then take your Maruti for a gentle first drive.', reward: 120, reputation: 4, giver: 'mira', steps: [talk('mira', 'Meet Maya didi in Durbar Square. Walk up and press E to introduce yourself.', 'plaza'), { kind: 'reach', target: 'cafe', place: 'cafe', text: 'Follow the footpath to Himalayan Chiya Pasal. M opens the map; J shows this story.' }, talk('leo', 'Press E beside Laxman dai at the tea shop and say namaste.', 'cafe'), talk('mira', 'Return to Maya didi in Durbar Square and tell her you met Laxman dai.', 'plaza')] },
  { id: 'courier', title: 'The neighborhood route', category: 'WORK', description: 'Collect a parcel at Sajha Courier and deliver it safely. Routes change between shifts.', reward: 95, reputation: 2, giver: 'tess', repeatable: true },
  { id: 'cafe', title: 'The chiya rush', category: 'WORK', description: 'Work a three-order shift at the tea shop: brew milk chiya, pack the momos, and serve each order before the shift ends.', reward: 85, reputation: 2, giver: 'leo', repeatable: true },
  { id: 'voices', title: 'Voices of the tole', category: 'STORY', description: 'Listen to Rasmila, Bikash and Junu. What happened to the ghat lights?', reward: 150, reputation: 4, giver: 'mira', steps: [{ kind: 'voices', targets: ['rosa', 'eli', 'juno'], place: 'market', text: 'Speak with Rasmila, Bikash and Junu about the ghat.' }, talk('mira', 'Tell Maya didi what you learned.', 'plaza')] },
  { id: 'repair', title: 'Keep the lights on', category: 'WORK', description: 'Collect Sanjay dai’s tools, repair three damaged streetlights, and light up the evening ghat.', reward: 180, reputation: 5, giver: 'sanjay' },
  { id: 'shipment', title: 'The missing manifest', category: 'STORY', description: 'Help Tashi trace a misrouted health post shipment through Godam 08.', reward: 250, reputation: 6, giver: 'tess', requirement: 'Build 4 reputation to earn Tashi’s trust.', steps: [talk('tess', 'Ask Tashi about the missing shipment.', 'depot'), place('warehouse', 'Inspect the shipping manifest at Godam 08.'), talk('sanjay', 'Ask Sanjay dai what arrived at the ghat.', 'harbor'), talk('tess', 'Bring the corrected manifest back to Tashi.', 'depot')] },
  { id: 'lookout', title: 'The valley from above', category: 'EXPLORATION', description: 'Climb to Sarangkot and frame a postcard of the Himalaya.', reward: 80, reputation: 1, giver: 'juno', steps: [{ kind: 'reach', target: 'lookout', place: 'lookout', text: 'Reach Sarangkot Viewpoint.' }, { kind: 'photo', target: 'lookout', place: 'lookout', text: 'Press V at the lookout to frame a postcard.' }] }
]);

export class MissionSystem {
  constructor(store, { notify = () => {}, onChange = () => {} } = {}) {
    this.store = store; this.notify = notify; this.onChange = onChange;
    if (!store.state.missions.arrival) store.state.missions.arrival = { status: 'active', step: 0, flags: [], remaining: 150, variant: 0 };
  }
  get progress() { return this.store.state; }
  definition(id) { return MISSION_DEFINITIONS.find(m => m.id === id); }
  unlocked(id) { return id !== 'shipment' || this.progress.reputation >= 4; }
  steps(id) {
    const def = this.definition(id); if (!def) return [];
    const state = this.progress.missions[id];
    if (id === 'courier') { const target = ['clinic', 'market', 'harbor'][state?.variant || 0]; return [place('depot', 'Collect your parcel at Sajha Courier.'), place(target, `Deliver the parcel to ${POIS.find(p => p.id === target).name}.`)]; }
    if (id === 'cafe') return Array.from({ length: 9 }, (_, i) => ({ kind: 'work', target: ['brew', 'pack', 'serve'][i % 3], place: 'cafe', text: `Order ${Math.floor(i / 3) + 1} of 3 · ${['brew the milk chiya', 'pack the momos', 'serve the customer'][i % 3]}.` }));
    if (id === 'repair') return [place('harbor', 'Collect Sanjay dai’s tool kit at the ghat workshop.'), ...REPAIR_LAMPS.map((lamp, i) => ({ kind: 'repair', target: lamp.id, place: 'harbor', position: new Vec3(lamp.x, terrainHeight(lamp.x, lamp.z), lamp.z), text: `Repair streetlight ${i + 1} of 3. Hold E beside the lamp.` })), talk('sanjay', 'Return the tool kit to Sanjay dai.', 'harbor')];
    return def.steps;
  }
  accept(id) {
    const def = this.definition(id), old = this.progress.missions[id];
    if (!def || !this.unlocked(id)) { this.notify('Build more neighborhood reputation first.'); return false; }
    if (id === 'cafe' && (this.progress.minutes < 420 || this.progress.minutes >= 1320)) { this.notify('Café shifts are available from 07:00 to 22:00.'); return false; }
    if (old?.status === 'active') { this.progress.tracked = id; this.onChange(); return true; }
    if (old?.status === 'completed' && !def.repeatable) { this.notify('You have already completed this story.'); return false; }
    this.progress.missions[id] = { status: 'active', step: 0, flags: [], remaining: 150, variant: id === 'courier' ? this.progress.courierCycle % 3 : 0 };
    this.progress.tracked = id; this.notify(`Started · ${def.title}`); this.store.save(); this.onChange(); return true;
  }
  track(id) { if (this.progress.missions[id]?.status !== 'active') return false; this.progress.tracked = id; this.onChange(); return true; }
  tracked() {
    let id = this.progress.tracked;
    if (this.progress.missions[id]?.status !== 'active') id = Object.keys(this.progress.missions).find(k => this.progress.missions[k].status === 'active');
    if (!id) return null;
    const state = this.progress.missions[id], def = this.definition(id); if (!def) return null;
    return { id, state, definition: def, step: this.steps(id)[state.step] };
  }
  objectivePosition(citizens = null) {
    const active = this.tracked(); if (!active?.step) return null;
    const step = active.step;
    if (step.kind === 'talk' && citizens?.get(step.target)) return citizens.get(step.target).position.clone();
    if (step.kind === 'voices') {
      const next = step.targets.find(id => !active.state.flags.includes(id)); return citizens?.get(next)?.position.clone() || poiPosition(step.place);
    }
    return step.position?.clone() || poiPosition(step.place);
  }
  event(kind, target, data = {}) {
    let progressed = false;
    for (const [id, state] of Object.entries(this.progress.missions)) {
      if (state.status !== 'active') continue;
      const steps = this.steps(id), step = steps[state.step]; if (!step) continue;
      let match = step.kind === kind && step.target === target;
      if (step.kind === 'place') match = kind === 'interact' && target === step.target || kind === 'talk' && data.place === step.target;
      if (step.kind === 'work') match = kind === 'work' && target === step.target && data.place === 'cafe';
      if (step.kind === 'voices' && kind === 'talk' && step.targets.includes(target)) {
        if (!state.flags.includes(target)) { state.flags.push(target); this.notify(`Voices of the tole · ${state.flags.length}/3`); progressed = true; }
        match = step.targets.every(x => state.flags.includes(x));
      }
      if (!match) continue;
      if (step.kind === 'repair' && !this.progress.repaired.includes(target)) this.progress.repaired.push(target);
      state.step++; progressed = true;
      if (state.step >= steps.length) this.complete(id);
      else this.notify(`Objective updated · ${steps[state.step].text}`);
    }
    if (progressed) { this.store.save(); this.onChange(); }
    return progressed;
  }
  complete(id) {
    const state = this.progress.missions[id], def = this.definition(id); if (!state || state.status !== 'active') return;
    state.status = 'completed';
    const cycle = def.repeatable ? (id === 'courier' ? this.progress.courierCycle++ : this.progress.jobCount++) : 0;
    this.store.reward(`mission:${id}:${cycle}`, def.reward, def.reputation);
    this.notify(`Completed · ${def.title}   +Rs ${def.reward}   +${def.reputation} reputation`, 'success');
  }
  update(dt, playerPosition, canAct = true) {
    for (const [id, state] of Object.entries(this.progress.missions)) {
      if (state.status !== 'active') continue;
      const step = this.steps(id)[state.step]; if (!step) continue;
      if (step.kind === 'reach' && distanceXZ(playerPosition, poiPosition(step.target)) < 8) this.event('reach', step.target);
      if (id === 'cafe' && canAct) {
        state.remaining -= dt;
        if (state.remaining <= 0) { delete this.progress.missions.cafe; this.notify('The tea shop shift ended. Talk to Laxman dai to try another shift.'); this.store.save(); this.onChange(); }
      }
    }
  }
  repairNear(position) {
    const state = this.progress.missions.repair; if (state?.status !== 'active') return null;
    const step = this.steps('repair')[state.step]; return step?.kind === 'repair' && distanceXZ(position, step.position) < 2.8 ? step : null;
  }
}
