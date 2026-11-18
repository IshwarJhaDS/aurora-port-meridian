import { POIS, clockText } from '../world/util.js';

// Fixed lines are exported so tools/generate-audio.mjs can voice every one of them.
export const VOICES = {
  rosa: 'Those ghat lamps went dark right after the monsoon storm. Sanjay dai said he could fix them if someone carried his tools.',
  eli: 'The lights matter to people walking home after a late shift. A practical kindness goes a long way.',
  juno: 'I photographed a courier van near Godam 08. The health post labels were swapped with a routine ghat order.',
  sanjay: 'That health post consignment never left the ghat. The crate number is right, but the destination label is wrong. Tell Tashi to check manifest 08.',
  tess: 'Godam 08 logged the medicine consignment under the ghat repair order. Check the paperwork by the service entrance.',
  mira: 'Start with Laxman dai at the chiya pasal. Then meet the people in Newa Tole, at the stupa garden, and along Phewa Ghat. They know what this valley needs.'
};
export const GREETINGS = Object.freeze({ again: 'Namaste! Good to see you again.', morning: 'Namaste, good morning. Welcome to the tole.', afternoon: 'Namaste! Good to meet you.', evening: 'Namaste. A nice evening for a walk.' });
export const LINES = Object.freeze({
  fleeing: 'Aama! That was a little too close for comfort.',
  idle: 'I am heading between work and home. There is always something happening around here.',
  later: 'Come back when you have a little more experience in the tole.',
  thanks: 'Thank you for helping.',
  rain: 'The monsoon makes the roads slippery. I would keep to the footpaths and wait under a shop awning until it eases.',
  evening: 'People are finishing their shifts. The ghat looks beautiful under the lights.',
  day: 'The chiya pasal, the health post, the courier office and the ghat all keep this valley running.',
  jobsFallback: 'Tashi has delivery routes, Laxman dai needs help at the chiya pasal, and Sanjay dai has repairs along the ghat.',
  weatherRain: 'Monsoon showers move through the valley quickly. Take it slowly on the wet roads.',
  weatherClear: 'Clear skies for now. You can see Machhapuchhre from Sarangkot.',
  askOthers: 'Ask Tashi at the courier office or Sanjay dai at the ghat workshop. They know what happened.',
  welcome: 'Dhanyabad. Nice to have another friendly face around here.',
  topics: 'You can ask me about work, directions, the weather, or the ghat.',
  unknown: 'I mostly know this tole and the places I work.'
});
const JOBS = { mira: ['arrival', 'voices'], leo: ['cafe'], tess: ['courier', 'shipment'], sanjay: ['repair'], juno: ['lookout'] };
export const jobsFor = id => JOBS[id] || [];

export class DialogueSystem {
  constructor(citizens, missions, store, { navigate = () => {}, changed = () => {} } = {}) {
    this.citizens = citizens; this.missions = missions; this.store = store; this.navigate = navigate; this.changed = changed; this.current = null;
  }
  open(agent, minutes, weather) {
    if (!agent) return null;
    this.close(); this.citizens.setTalking(agent.id, true);
    const count = this.store.state.conversations[agent.id] || 0;
    this.store.state.conversations[agent.id] = count + 1;
    const greeting = count ? GREETINGS.again : minutes < 720 ? GREETINGS.morning : minutes < 1080 ? GREETINGS.afternoon : GREETINGS.evening;
    this.current = { agent, minutes, weather, text: `${greeting} ${agent.line || (agent.state === 'Fleeing' ? LINES.fleeing : LINES.idle)}`, options: [] };
    this.missions.event('talk', agent.id, { place: agent.place }); this.menu(); this.changed(this.current); return this.current;
  }
  menu() {
    if (!this.current) return;
    const a = this.current.agent, options = [];
    for (const id of jobsFor(a.id)) {
      const def = this.missions.definition(id), state = this.store.state.missions[id];
      if (state?.status === 'completed' && !def.repeatable) continue;
      options.push({ id: `mission:${id}`, text: state?.status === 'active' ? `About “${def.title}”…` : `${def.category === 'WORK' ? 'Take a job' : 'Help out'} · ${def.title}`, disabled: !this.missions.unlocked(id) });
    }
    options.push({ id: 'local', text: 'Ke chha? What is happening around here?' }, { id: 'directions', text: 'Point me toward somewhere useful.' }, { id: 'goodbye', text: 'Pheri bhetaula · see you around.' });
    this.current.options = options.slice(0, 6);
  }
  choose(id) {
    const current = this.current; if (!current) return;
    const option = current.options.find(x => x.id === id); if (!option || option.disabled) return;
    if (id === 'goodbye') { this.close(); return; }
    if (id.startsWith('mission:')) {
      const mission = id.slice(8), accepted = this.missions.accept(mission);
      current.text = accepted ? `${this.missions.definition(mission).description} ${this.missions.steps(mission)[this.store.state.missions[mission].step]?.text || LINES.thanks}` : LINES.later;
      if (accepted) this.missions.event('talk', current.agent.id, { place: current.agent.place });
    } else if (id === 'local') {
      current.text = VOICES[current.agent.id] || (current.weather.rain > .3 ? LINES.rain : `It is ${clockText(current.minutes)}. ${current.minutes > 1080 ? LINES.evening : LINES.day}`);
      this.missions.event('talk', current.agent.id, { place: current.agent.place });
    } else if (id === 'directions') {
      const p = POIS.find(p => p.id === (current.agent.place || 'plaza'));
      this.navigate(p.id); current.text = `${p.name} is marked on your map. ${p.description}`;
    }
    this.menu(); this.changed(current);
  }
  say(text) {
    if (!this.current) return;
    const input = text.trim().toLowerCase().slice(0, 200), a = this.current.agent;
    if (/\b(jobs?|work|money|shifts?|missions?|help)\b/.test(input)) {
      const id = JOBS[a.id]?.find(id => this.missions.unlocked(id));
      this.current.text = id ? `I can help you with “${this.missions.definition(id).title}”. Choose the job below and I will explain the next step.` : LINES.jobsFallback;
    } else if (/\b(weather|rain|storm|sun)\b/.test(input)) this.current.text = this.current.weather.rain > .3 ? LINES.weatherRain : LINES.weatherClear;
    else if (/\b(shipment|manifest|light|harbor|ghat|lake)\b/.test(input)) { this.current.text = VOICES[a.id] || LINES.askOthers; this.missions.event('talk', a.id, { place: a.place }); }
    else if (/\b(where|direction|map|cafe|chiya|tea|clinic|stupa|temple)\b/.test(input)) { this.choose('directions'); return; }
    else if (/\b(bye|goodbye|bhetaula)\b/.test(input)) { this.close(); return; }
    else if (/\b(hi|hello|hey|namaste|namaskar|thanks|thank|dhanyabad|dhanyavad)\b/.test(input)) this.current.text = LINES.welcome;
    else this.current.text = `${a.line || LINES.unknown} ${LINES.topics}`;
    this.changed(this.current);
  }
  close() { if (this.current) this.citizens.setTalking(this.current.agent.id, false); this.current = null; this.changed(null); }
}
