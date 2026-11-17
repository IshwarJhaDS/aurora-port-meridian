import { Renderer } from './renderer.js';
import { CitySimulation } from './game/city-simulation.js';
import { CityUI } from './game/city-ui.js';
import { MapRenderer } from './game/map-renderer.js';
import { distanceXZ, poiPosition } from './world/util.js';

class CityGame {
  constructor() {
    this.canvas = document.getElementById('game-view'); this.renderer = null; this.ui = null; this.disposers = []; this.fps = 0; this.photo = false; this.capturePending = false; this.contextLost = false; this.running = true; this.frames = 0; this.fpsTime = 0; this.last = null; this.resetting = false;
    let storage = null; try { storage = window.localStorage; } catch { storage = null; }
    this.sim = new CitySimulation({ storage, notify: (text, type) => this.ui?.toast(text, type), changed: (type, data) => this.ui?.changed(type, data), release: object => this.renderer?.releaseObject?.(object) });
    try { this.renderer = new Renderer(this.canvas); }
    catch (error) {
      if (!/WebGL 2|EXT_color_buffer_float|texture units/.test(error.message)) throw error;
      console.warn(error.message); const replacement = this.canvas.cloneNode(false); this.canvas.replaceWith(replacement); this.canvas = replacement; this.renderer = new MapRenderer(this.canvas, this.sim); this.fallbackReason = error.message;
    }
    this.sim.paused = true; this.ui = new CityUI(this); this.applyQuality(); this.bind(); this.raf = requestAnimationFrame(time => this.frame(time));
  }
  applyQuality() {
    const quality = this.sim.progress.settings.quality;
    Object.assign(this.renderer.settings, quality === 'performance' ? { scale: .7, gi: false, ssr: false, shadowMode: 0 } : quality === 'cinematic' ? { scale: 1.2, gi: true, ssr: true, shadowMode: 1 } : { scale: 1, gi: true, ssr: true, shadowMode: 1 });
    this.renderer.settings.fog = true;
    this.renderer.settings.exposure = quality === 'performance' ? .95 : quality === 'balanced' ? 1.2 : 1.45;
  }
  listen(target, type, callback, options) { target.addEventListener(type, callback, options); this.disposers.push(() => target.removeEventListener(type, callback, options)); }
  bind() {
    const canvas = this.canvas; let drag = null;
    this.listen(canvas, 'pointerdown', event => { if (!this.ui.started || this.ui.panel || this.sim.dialogue.current) return; canvas.focus(); canvas.setPointerCapture(event.pointerId); drag = { x: event.clientX, y: event.clientY }; });
    this.listen(canvas, 'pointermove', event => { if (!drag) return; this.sim.camera.yaw -= (event.clientX - drag.x) * .005; this.sim.camera.pitch += (event.clientY - drag.y) * .004; drag = { x: event.clientX, y: event.clientY }; });
    for (const type of ['pointerup', 'pointercancel', 'lostpointercapture']) this.listen(canvas, type, () => drag = null);
    this.listen(canvas, 'contextmenu', e => e.preventDefault()); this.listen(canvas, 'wheel', e => { e.preventDefault(); this.sim.camera.distance *= Math.exp(e.deltaY * .001); }, { passive: false });
    this.listen(window, 'keydown', e => {
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement || e.target instanceof HTMLSelectElement) { if (e.code === 'Escape') { e.target.blur(); this.ui.escape(); } return; }
      if (e.code === 'F3' || !this.ui.panel && !this.sim.dialogue.current && e.target === canvas && ['Space', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(e.code)) e.preventDefault();
      if (e.code === 'Escape' && !e.repeat) { this.ui.escape(); this.sim.keys.clear(); return; }
      if (!this.ui.started) return;
      if (e.code === 'KeyM' && !e.repeat) { this.ui.toggle('map'); this.sim.keys.clear(); return; }
      if (e.code === 'KeyJ' && !e.repeat) { this.ui.toggle('journal'); this.sim.keys.clear(); return; }
      if (this.sim.dialogue.current && /^Digit[1-6]$/.test(e.code) && !e.repeat) { this.ui.chooseNumber(Number(e.code.at(-1))); return; }
      if (e.code === 'F3' && !e.repeat) { const node = document.getElementById('telemetry'); node.hidden = !node.hidden; return; }
      if (this.ui.panel) return;
      this.sim.keys.add(e.code);
      if (e.code === 'KeyE' && !e.repeat) this.sim.interact();
      if (e.code === 'KeyR' && !e.repeat && !this.sim.dialogue.current) this.sim.resetVehicle();
      if (e.code === 'KeyX' && !e.repeat && !this.sim.dialogue.current) this.sim.kick();
      if (e.code === 'KeyV' && !e.repeat && !this.sim.dialogue.current) this.togglePhoto();
    });
    this.listen(window, 'keyup', e => this.sim.keys.delete(e.code)); this.listen(window, 'blur', () => { this.sim.keys.clear(); drag = null; });
    this.listen(document, 'visibilitychange', () => { this.sim.keys.clear(); this.last = null; if (document.hidden) this.sim.save(); });
    for (const button of document.querySelectorAll('[data-key]')) {
      this.listen(button, 'pointerdown', e => { e.preventDefault(); button.setPointerCapture(e.pointerId); this.sim.keys.add(button.dataset.key); if (button.dataset.key === 'KeyE') this.sim.interact(); });
      for (const type of ['pointerup', 'pointercancel', 'lostpointercapture']) this.listen(button, type, () => this.sim.keys.delete(button.dataset.key));
    }
    this.listen(canvas, 'webglcontextlost', e => { e.preventDefault(); this.contextLost = true; this.ui.toast('Graphics paused. Waiting for the browser to restore the scene.'); });
    this.listen(canvas, 'webglcontextrestored', () => { try { this.renderer.dispose(); this.renderer = new Renderer(canvas); this.applyQuality(); this.contextLost = false; this.last = null; } catch (error) { this.fail(error); } });
    this.listen(window, 'pagehide', () => this.dispose(), { once: true });
  }
  togglePhoto() {
    this.photo = !this.photo; this.sim.camera.photo = this.photo; document.body.classList.toggle('photo-mode', this.photo); document.getElementById('photo-controls').hidden = !this.photo;
    if (this.photo && distanceXZ(this.sim.player.position, poiPosition('lookout')) < 25) this.sim.missions.event('photo', 'lookout');
    if (!this.photo) this.sim.camera.distance = Math.min(this.sim.camera.distance, 15);
  }
  project(position, width, height) { return this.renderer.mode === 'map' ? this.renderer.project(position, width, height) : this.sim.camera.project(position, width, height); }
  frame(time) {
    if (!this.running) return;
    const dt = this.last == null ? 1 / 60 : Math.max(0, Math.min(.1, (time - this.last) / 1000)); this.last = time;
    try {
      if (!this.contextLost) {
        this.sim.update(dt); this.sim.arena.reset(); const objects = this.sim.buildRenderObjects(); this.renderer.settings.fogDensity = this.sim.weather.fog;
        if (this.renderer.mode === 'map') this.renderer.render(); else this.renderer.render(objects, this.sim.camera, this.sim.sun, this.sim.wet, this.sim.elapsed, this.sim.arena);
        this.frames++; this.fpsTime += dt; if (this.fpsTime > .5) { this.fps = this.frames / this.fpsTime; this.frames = 0; this.fpsTime = 0; }
        this.ui.update(dt);
        if (this.capturePending) { this.capturePending = false; const url = this.canvas.toDataURL('image/png'), anchor = document.createElement('a'); anchor.download = 'Postcard_from_Port_Meridian.png'; anchor.href = url; anchor.click(); this.ui.toast('Postcard captured.'); }
      }
    } catch (error) { this.fail(error); return; }
    this.raf = requestAnimationFrame(next => this.frame(next));
  }
  fail(error) { console.error(error); this.running = false; document.getElementById('fatal-message').textContent = error.message; document.getElementById('fatal-error').hidden = false; }
  dispose() { if (!this.running) return; this.running = false; cancelAnimationFrame(this.raf); for (const remove of this.disposers) remove(); window.speechSynthesis?.cancel(); if (!this.resetting) this.sim.dispose(); this.renderer.dispose(); }
}

try { window.cityGame = new CityGame(); } catch (error) { console.error(error); document.getElementById('fatal-message').textContent = error.message; document.getElementById('fatal-error').hidden = false; }
