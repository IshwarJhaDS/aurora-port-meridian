import { Vec3 } from '../math.js';
import { POIS, poiPosition, distanceXZ, districtAt, clockText, terrainHeight } from '../world/util.js';
import { MISSION_DEFINITIONS } from './missions.js';
import { drawCityMap } from './map-renderer.js';

const $ = id => document.getElementById(id);
const escape = text => String(text).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

export class CityUI {
  constructor(game) {
    this.game = game; this.sim = game.sim; this.panel = null; this.started = false; this.timer = 0; this.toasts = []; this.labelNodes = [];
    this.mini = $('minimap').getContext('2d'); this.map = $('world-map').getContext('2d');
    $('enter-city').addEventListener('click', () => { game.audio.unlock(); this.started = true; this.sim.paused = false; $('welcome').classList.add('departing'); setTimeout(() => $('welcome').hidden = true, 650); game.canvas.focus(); this.toast('Swagatam! Welcome to Phewa Valley. Maya didi is waiting in Durbar Square.'); });
    for (const [id, name] of [['map-button', 'map'], ['journal-button', 'journal'], ['menu-button', 'menu']]) $(id).addEventListener('click', () => this.toggle(name));
    for (const button of document.querySelectorAll('[data-close]')) button.addEventListener('click', () => this.close());
    $('continue-game').addEventListener('click', () => this.close()); $('save-game').addEventListener('click', () => this.toast(this.sim.save() ? 'Your visit has been saved on this browser.' : 'Saving is unavailable in this browser. Your current session continues.'));
    $('recover-car').addEventListener('click', () => { this.sim.resetVehicle(); this.close(); });
    $('quality').value = this.sim.progress.settings.quality;
    $('quality').addEventListener('change', e => { this.sim.progress.settings.quality = e.target.value; this.game.applyQuality(); this.sim.save(); });
    $('voice-toggle').checked = this.sim.progress.settings.sound;
    $('voice-toggle').addEventListener('change', e => { this.sim.progress.settings.sound = e.target.checked; if (!e.target.checked) { window.speechSynthesis?.cancel(); game.audio.stopVoice(); } this.sim.save(); });
    $('ambience-toggle').checked = this.sim.progress.settings.ambience !== false;
    $('ambience-toggle').addEventListener('change', e => { this.sim.progress.settings.ambience = e.target.checked; this.sim.save(); });
    $('dialogue-choices').addEventListener('click', e => { const button = e.target.closest('[data-choice]'); if (button) this.sim.dialogue.choose(button.dataset.choice); });
    $('say-form').addEventListener('submit', e => { e.preventDefault(); const text = $('say-input').value; if (text.trim()) { this.sim.dialogue.say(text); $('say-input').value = ''; } });
    $('dialogue-close').addEventListener('click', () => this.sim.dialogue.close());
    $('mission-list').addEventListener('click', e => { const button = e.target.closest('[data-mission]'); if (!button) return; this.sim.navTarget = null; if (this.sim.progress.missions[button.dataset.mission]?.status === 'active') this.sim.missions.track(button.dataset.mission); else this.sim.missions.accept(button.dataset.mission); this.renderJournal(); });
    $('place-list').addEventListener('click', e => { const button = e.target.closest('[data-place]'); if (button) { this.sim.navigate(button.dataset.place); this.renderMap(); } });
    $('shuttle-waypoint').addEventListener('click', () => { if (this.sim.takeShuttle(this.sim.navTarget?.id)) this.close(); });
    $('world-map').addEventListener('click', e => { if (!this.fullMapping) return; const rect = e.currentTarget.getBoundingClientRect(), p = this.fullMapping.unproject((e.clientX - rect.left) / rect.width * e.currentTarget.width, (e.clientY - rect.top) / rect.height * e.currentTarget.height); p.x = Math.max(-465, Math.min(425, p.x)); p.z = Math.max(-465, Math.min(545, p.z)); this.sim.navigatePosition(new Vec3(p.x, terrainHeight(p.x, p.z), p.z)); this.toast('Custom waypoint set.'); });
    $('clear-waypoint').addEventListener('click', () => { this.sim.navTarget = null; this.sim.routeTimer = 0; this.toast('Waypoint cleared. Your tracked story is shown again.'); });
    for (const button of document.querySelectorAll('[data-work]')) button.addEventListener('click', () => this.sim.startWork(button.dataset.work));
    $('photo-capture').addEventListener('click', () => { this.game.capturePending = true; });
    $('photo-close').addEventListener('click', () => this.game.togglePhoto());
    $('help-button').addEventListener('click', () => $('control-help').classList.toggle('expanded'));
    $('new-game').addEventListener('click', () => { $('reset-confirm').hidden = false; });
    $('cancel-reset').addEventListener('click', () => $('reset-confirm').hidden = true);
    $('confirm-reset').addEventListener('click', () => { this.sim.store.reset(); this.game.resetting = true; location.reload(); });
    $('place-list').innerHTML = POIS.map(p => `<button class="place-item" data-place="${p.id}"><span class="place-icon">${p.kind === 'work' ? '◈' : p.kind === 'vista' ? '◇' : '⌖'}</span><span><strong>${escape(p.name)}</strong><small>${escape(p.description)}</small></span><span>↗</span></button>`).join('');
    $('renderer-mode').textContent = game.renderer.mode === 'map' ? 'MAP VIEW' : '3D CITY';
    this.renderJournal(); this.dialogue(null);
  }
  toast(text, type = 'info') { this.toasts.push({ text, type, until: performance.now() + 5000 }); if (this.toasts.length > 3) this.toasts.shift(); this.renderToasts(); }
  renderToasts() { $('toasts').innerHTML = this.toasts.map(t => `<div class="toast ${t.type}"><span>${t.type === 'success' ? '✓' : '◇'}</span>${escape(t.text)}</div>`).join(''); }
  toggle(name) {
    if (!this.started) return;
    if (this.sim.dialogue.current) this.sim.dialogue.close();
    if (this.panel === name) { this.close(); return; }
    this.close(); this.panel = name; $(name + '-panel').hidden = false; this.sim.paused = true; $('overlay-scrim').hidden = false;
    if (name === 'journal') this.renderJournal(); if (name === 'map') this.renderMap();
  }
  close() { for (const name of ['map', 'journal', 'menu']) $(name + '-panel').hidden = true; $('overlay-scrim').hidden = true; this.panel = null; this.sim.paused = !this.started; $('reset-confirm').hidden = true; this.game.canvas.focus(); }
  escape() { if (this.sim.dialogue.current) this.sim.dialogue.close(); else if (this.game.photo) this.game.togglePhoto(); else this.panel ? this.close() : this.toggle('menu'); }
  changed(type, data) { if (type === 'dialogue') this.dialogue(data); if (type === 'missions' && this.panel === 'journal') this.renderJournal(); if (type === 'saved') { $('save-status').textContent = data ? 'PROGRESS SAVED' : 'SESSION ONLY'; } if (['missions','dialogue','work'].includes(type)) this.update(.1); }
  dialogue(data) {
    $('dialogue').hidden = !data; if (!data) { window.speechSynthesis?.cancel(); this.game.audio?.stopVoice(); return; }
    $('speaker-name').textContent = data.agent.name; $('speaker-role').textContent = data.agent.role; $('speaker-text').textContent = data.text;
    const color = data.agent.outfit.map(x => Math.round(Math.pow(x, .6) * 220)); $('speaker-avatar').style.background = `linear-gradient(145deg,rgb(${color.join(',')}),#172b32)`; $('speaker-avatar').textContent = data.agent.name.split(' ').map(n => n[0]).slice(0, 2).join('');
    $('dialogue-choices').innerHTML = data.options.map((o, i) => `<button data-choice="${o.id}" ${o.disabled ? 'disabled' : ''}><kbd>${i + 1}</kbd><span>${escape(o.text)}</span>${o.disabled ? '<small>4 REP</small>' : '<span>↗</span>'}</button>`).join('');
    if (this.sim.progress.settings.sound) this.speak(data);
  }
  speak(data) {
    window.speechSynthesis?.cancel();
    // Prefer the ElevenLabs clips; otherwise use a South Asian browser voice when one is installed.
    if (this.game.audio?.speak(data.agent.named ? data.agent.id : data.agent.voice || 'resident-m', data.text)) return;
    if (!window.speechSynthesis || !window.SpeechSynthesisUtterance) return;
    const utterance = new SpeechSynthesisUtterance(data.text), voices = window.speechSynthesis.getVoices();
    const voice = ['ne-NP', 'en-IN', 'hi-IN'].map(lang => voices.find(v => v.lang?.replace('_', '-') === lang)).find(Boolean);
    if (voice) utterance.voice = voice; utterance.lang = voice?.lang || 'en-IN'; utterance.rate = .96; window.speechSynthesis.speak(utterance);
  }
  chooseNumber(number) { const option = this.sim.dialogue.current?.options[number - 1]; if (option) this.sim.dialogue.choose(option.id); }
  renderJournal() {
    $('mission-list').innerHTML = MISSION_DEFINITIONS.map(def => {
      const state = this.sim.progress.missions[def.id], locked = !this.sim.missions.unlocked(def.id), completed = state?.status === 'completed' && !def.repeatable, active = state?.status === 'active', step = active ? this.sim.missions.steps(def.id)[state.step]?.text : def.description;
      return `<article class="mission-card ${active ? 'active' : ''}"><div class="mission-meta"><span>${def.category}</span><span>${completed ? 'COMPLETED ✓' : locked ? 'LOCKED' : active ? 'IN PROGRESS' : def.repeatable ? 'REPEATABLE' : 'AVAILABLE'}</span></div><h3>${escape(def.title)}</h3><p>${escape(locked ? def.requirement : step || def.description)}</p><div class="mission-footer"><span>Rs ${def.reward} <small>+ ${def.reputation} REP</small></span><button data-mission="${def.id}" ${locked || completed ? 'disabled' : ''}>${completed ? 'Complete' : active ? this.sim.progress.tracked === def.id ? 'Tracking ✓' : 'Track' : 'Start'} ↗</button></div></article>`;
    }).join('');
    $('journal-money').textContent = `Rs ${Math.floor(this.sim.progress.cash).toLocaleString()} · ${this.sim.progress.reputation} reputation`;
  }
  renderMap() { const canvas = $('world-map'), rect = canvas.getBoundingClientRect(); canvas.width = Math.max(500, Math.round(rect.width * Math.min(devicePixelRatio || 1, 1.5))); canvas.height = Math.max(440, Math.round(rect.height * Math.min(devicePixelRatio || 1, 1.5))); this.fullMapping = drawCityMap(this.map, canvas.width, canvas.height, this.sim, { full: true }); }
  update(dt) {
    this.timer += dt; if (this.timer < .08) return; this.timer = 0;
    const sim = this.sim, p = sim.player.position, place = districtAt(p.x, p.z), active = sim.missions.tracked(), goal = sim.goal();
    $('district-name').textContent = place.name; $('city-clock').textContent = clockText(sim.progress.minutes); $('weather-name').textContent = sim.weather.label;
    $('cash').textContent = `Rs ${Math.floor(sim.progress.cash).toLocaleString()}`; $('reputation').textContent = sim.progress.reputation; $('mode-label').textContent = sim.player.inVehicle ? 'DRIVING' : 'ON FOOT';
    $('speed').textContent = sim.player.inVehicle ? `${Math.round(sim.vehicle.body.velocity.length() * 3.6)} km/h` : sim.player.speed > 4 ? 'RUNNING' : sim.player.speed > .3 ? 'WALKING' : 'EXPLORING';
    $('objective-title').textContent = sim.navTarget ? sim.navTarget.label : active?.definition.title || 'Make yourself at home'; $('objective-text').textContent = sim.navTarget ? 'Follow your waypoint through the neighborhood.' : active?.step?.text || 'Explore, talk to people, and discover a new story.';
    $('objective-distance').textContent = goal?.position ? `${Math.round(distanceXZ(p, goal.position))} m` : 'FREE ROAM';
    if (active?.step?.kind === 'voices') $('objective-text').textContent += ` (${active.state.flags.length}/3)`;
    const interaction = sim.interaction; $('interaction').hidden = !interaction || !!sim.dialogue.current || !!this.panel || !this.started;
    if (interaction) $('interaction-label').textContent = interaction.label;
    const progress = sim.hold ? sim.hold.time / sim.hold.duration : sim.work ? sim.work.time / sim.work.duration : 0; $('action-progress').style.width = `${Math.min(100, progress * 100)}%`;
    const cafe = sim.progress.missions.cafe, working = cafe?.status === 'active' && distanceXZ(p, poiPosition('cafe')) < 8 && !sim.dialogue.current && !this.panel;
    $('work-panel').hidden = !working; if (working) { $('work-time').textContent = `${Math.ceil(cafe.remaining)}s`; $('work-order').textContent = `ORDER ${Math.floor(cafe.step / 3) + 1} OF 3`; const next = sim.missions.steps('cafe')[cafe.step]?.target; for (const button of document.querySelectorAll('[data-work]')) { button.classList.toggle('next', button.dataset.work === next); button.disabled = !!sim.work; } $('work-status').textContent = sim.work ? `${sim.work.action === 'brew' ? 'Brewing' : sim.work.action === 'pack' ? 'Packing' : 'Serving'}…` : 'Complete each order: brew → pack → serve.'; }
    const canvas = $('minimap'); if (canvas.width !== 250) { canvas.width = 250; canvas.height = 190; } drawCityMap(this.mini, 250, 190, sim, { range: sim.player.inVehicle ? 140 : 95 });
    const now = performance.now(), before = this.toasts.length; this.toasts = this.toasts.filter(t => t.until > now); if (before !== this.toasts.length) this.renderToasts();
    this.updateLabels();
    $('controls-mode').textContent = sim.player.inVehicle ? 'WASD drive · SPACE handbrake · B brake · E exit' : 'WASD move · SHIFT run · SPACE jump · E interact';
    if (this.panel === 'map') this.renderMap();
    $('telemetry').textContent = `${Math.round(this.game.fps)} FPS · ${sim.streamer.resident.size} chunks · ${sim.citizens.visible.length} people · ${Math.round((this.game.renderer.stats.triangles || 0) / 1000)}k triangles`;
  }
  updateLabels() {
    const game = this.game, sim = this.sim, width = game.canvas.clientWidth, height = game.canvas.clientHeight;
    const labels = [];
    for (const p of POIS) if (distanceXZ(p, sim.player.position) < 95) labels.push({ position: poiPosition(p.id).add(new Vec3(0, 3.1, 0)), text: p.name, type: 'place' });
    for (const a of sim.citizens.visible) if (a.named && distanceXZ(a.position, sim.player.position) < 18) labels.push({ position: a.position.clone().add(new Vec3(0, 2.2, 0)), text: a.name.split(' ')[0], type: 'person' });
    const goal = sim.goal(); if (goal?.position) labels.push({ position: goal.position.clone().add(new Vec3(0, 1.9, 0)), text: `${Math.round(distanceXZ(goal.position, sim.player.position))} m`, type: 'objective' });
    const html = [];
    for (const label of labels) { const p = game.project(label.position, width, height); if (!p || p.x < 20 || p.x > width - 20 || p.y < 75 || p.y > height - 100) continue; html.push(`<span class="world-label ${label.type}" style="left:${p.x}px;top:${p.y}px">${escape(label.text)}</span>`); }
    $('world-labels').innerHTML = html.join('');
  }
}
