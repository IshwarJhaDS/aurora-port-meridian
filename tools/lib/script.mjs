// Builds the full list of fixed sentences each speaker can say, straight from the
// game's dialogue and mission data, so voice clips always match the on-screen text.
import { NAMED_CITIZENS } from '../../src/game/citizens.js';
import { VOICES, GREETINGS, LINES, jobsFor } from '../../src/game/dialogue.js';
import { MissionSystem } from '../../src/game/missions.js';
import { ProgressStore } from '../../src/game/progress.js';
import { POIS } from '../../src/world/util.js';
import { sentences, isUnvoiced } from '../../src/assets/catalog.js';

export function spokenScript() {
  const store = new ProgressStore(null), missions = new MissionSystem(store), script = new Map();
  const add = (speaker, ...texts) => { const set = script.get(speaker) || new Set(); for (const t of texts) for (const s of sentences(t)) if (!isUnvoiced(s)) set.add(s); script.set(speaker, set); };
  const directions = id => { const p = POIS.find(p => p.id === id); return `${p.name} is marked on your map. ${p.description}`; };
  const missionLines = id => {
    const def = missions.definition(id), out = [def.description, `I can help you with “${def.title}”. Choose the job below and I will explain the next step.`];
    for (let variant = 0; variant < 3; variant++) { store.state.missions[id] = { status: 'active', step: 0, flags: [], remaining: 150, variant }; for (const step of missions.steps(id)) out.push(step.text); }
    return out;
  };
  const shared = [...Object.values(GREETINGS), LINES.later, LINES.thanks, LINES.rain, LINES.evening, LINES.day, LINES.jobsFallback, LINES.weatherRain, LINES.weatherClear, LINES.askOthers, LINES.welcome, LINES.topics, LINES.unknown];
  for (const person of NAMED_CITIZENS) {
    add(person.id, ...shared, person.line, VOICES[person.id] || '', directions(person.place || 'plaza'));
    for (const id of jobsFor(person.id)) add(person.id, ...missionLines(id));
  }
  for (const speaker of ['resident-f', 'resident-m']) add(speaker, ...shared, LINES.fleeing, LINES.idle, ...['plaza', 'cafe', 'clinic', 'depot', 'market', 'harbor', 'park'].map(directions));
  return script;
}
