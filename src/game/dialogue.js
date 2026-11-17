import { POIS, clockText } from '../world/util.js';

const VOICES = {
  rosa: 'Those harbor lamps went out just after the storm. Sanjay said he could fix them if someone brought the tools along.',
  eli: 'The lights matter to the people who walk home after a late shift. A practical kindness goes a long way.',
  juno: 'I photographed a dispatch van near Warehouse 08. The clinic parcel labels were swapped with a routine harbor order.',
  sanjay: 'That clinic consignment never left the harbor. The crate number is right; the destination label is wrong. Tell Tess to check manifest 08.',
  tess: 'Warehouse 08 logged a clinic consignment under the harbor repair order. Check the paperwork by the service entrance.',
  mira: 'Start with Leo at Harbor Coffee. Then meet the people in Westhaven, Cedar Gardens, and along the promenade. They know what this city needs.'
};
const JOBS = { mira: ['arrival', 'voices'], leo: ['cafe'], tess: ['courier', 'shipment'], sanjay: ['repair'], juno: ['lookout'] };

export class DialogueSystem {
  constructor(citizens, missions, store, { navigate = () => {}, changed = () => {} } = {}) {
    this.citizens = citizens; this.missions = missions; this.store = store; this.navigate = navigate; this.changed = changed; this.current = null;
  }
  open(agent, minutes, weather) {
    if (!agent) return null;
    this.close(); this.citizens.setTalking(agent.id, true);
    const count = this.store.state.conversations[agent.id] || 0;
    this.store.state.conversations[agent.id] = count + 1;
    const greeting = count ? 'Good to see you again.' : minutes < 720 ? 'Morning. Welcome to the neighborhood.' : minutes < 1080 ? 'Afternoon. Good to meet you.' : 'Evening. Nice time for a walk.';
    this.current = { agent, minutes, weather, text: `${greeting} ${agent.line || (agent.state === 'Fleeing' ? 'That was a little too close for comfort.' : 'I am heading between work and home. There is always something happening around here.')}`, options: [] };
    this.missions.event('talk', agent.id, { place: agent.place }); this.menu(); this.changed(this.current); return this.current;
  }
  menu() {
    if (!this.current) return;
    const a = this.current.agent, options = [];
    for (const id of JOBS[a.id] || []) {
      const def = this.missions.definition(id), state = this.store.state.missions[id];
      if (state?.status === 'completed' && !def.repeatable) continue;
      options.push({ id: `mission:${id}`, text: state?.status === 'active' ? `About “${def.title}”…` : `${def.category === 'WORK' ? 'Take a job' : 'Help out'} · ${def.title}`, disabled: !this.missions.unlocked(id) });
    }
    options.push({ id: 'local', text: 'What is happening around here?' }, { id: 'directions', text: 'Point me toward somewhere useful.' }, { id: 'goodbye', text: 'See you around.' });
    this.current.options = options.slice(0, 6);
  }
  choose(id) {
    const current = this.current; if (!current) return;
    const option = current.options.find(x => x.id === id); if (!option || option.disabled) return;
    if (id === 'goodbye') { this.close(); return; }
    if (id.startsWith('mission:')) {
      const mission = id.slice(8), accepted = this.missions.accept(mission);
      current.text = accepted ? `${this.missions.definition(mission).description} ${this.missions.steps(mission)[this.store.state.missions[mission].step]?.text || 'Thank you for helping.'}` : 'Come back when you have a little more experience in the neighborhood.';
      if (accepted) this.missions.event('talk', current.agent.id, { place: current.agent.place });
    } else if (id === 'local') {
      current.text = VOICES[current.agent.id] || (current.weather.rain > .3 ? 'The rain makes the roads slippery. I would take the sidewalks and find an awning until it eases.' : `It is ${clockText(current.minutes)}. ${current.minutes > 1080 ? 'People are finishing their shifts. The harbor looks beautiful under the lights.' : 'The café, clinic, dispatch office, and harbor all keep this neighborhood running.'}`);
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
      this.current.text = id ? `I can help you with “${this.missions.definition(id).title}”. Choose the job below and I will explain the next step.` : 'Tess has delivery routes, Leo needs help at Harbor Coffee, and Sanjay has repairs along the harbor.';
    } else if (/\b(weather|rain|storm|sun)\b/.test(input)) this.current.text = this.current.weather.rain > .3 ? 'Rain moves through the harbor quickly. Take it slowly on the wet roads.' : 'Clear skies for the moment. The light is especially good from Cedar Hill.';
    else if (/\b(shipment|manifest|light|harbor)\b/.test(input)) { this.current.text = VOICES[a.id] || 'Ask Tess at dispatch or Sanjay at the harbor workshop. They know what happened.'; this.missions.event('talk', a.id, { place: a.place }); }
    else if (/\b(where|direction|map|cafe|clinic)\b/.test(input)) { this.choose('directions'); return; }
    else if (/\b(bye|goodbye)\b/.test(input)) { this.close(); return; }
    else if (/\b(hi|hello|hey|thanks|thank)\b/.test(input)) this.current.text = 'You are welcome. Nice to have another friendly face around here.';
    else this.current.text = `${a.line || 'I mostly know this neighborhood and the places I work.'} You can ask me about work, directions, the weather, or the harbor.`;
    this.changed(this.current);
  }
  close() { if (this.current) this.citizens.setTalking(this.current.agent.id, false); this.current = null; this.changed(null); }
}
