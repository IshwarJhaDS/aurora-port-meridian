# Aurora Phase 2 — complete source listing

This is the complete textual source, including shaders, runtime integration, tests and validation tools. Copy files using the exact paths shown, or use the supplied ZIP, which also contains all PNG texture assets. The architecture and mathematical derivations are in `PHASE2_ARCHITECTURE.md`.

## package.json

```json
{
  "name": "aurora-port-meridian",
  "version": "0.2.0",
  "private": true,
  "type": "module",
  "description": "A dependency-free single-player procedural city with streaming, citizens, conversations, work and story missions.",
  "scripts": {
    "test": "node --test tests/*.test.js",
    "dev": "node tools/serve.mjs",
    "check": "node tools/check.mjs"
  },
  "engines": {
    "node": ">=20"
  }
}
```

## index.html

```html
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover"><meta name="theme-color" content="#14282b"><title>Aurora · Port Meridian</title><meta name="description" content="Explore Port Meridian, meet its people, find honest work, and follow the stories of a living coastal city."><link rel="stylesheet" href="style.css"></head>
<body><main id="game-shell">
<canvas id="game-view" tabindex="0" aria-label="Port Meridian game. WASD to move, E to interact, M for map, J for missions, Escape for menu."></canvas><div id="world-labels" aria-hidden="true"></div>
<div id="hud">
  <header class="city-header"><div class="brand"><span class="brand-mark">A</span><div><strong>AURORA</strong><span>PORT MERIDIAN</span></div></div><div class="location"><span class="location-dot"></span><div><strong id="district-name">Old Meridian</strong><span><span id="city-clock">16:20</span> <i>·</i> <span id="weather-name">Clear skies</span></span></div></div><nav class="top-actions" aria-label="Game tools"><span id="renderer-mode" class="mode-chip">3D CITY</span><div class="wallet"><strong id="cash">$250</strong><span><b id="reputation">0</b> REPUTATION</span></div><button id="map-button" title="Map (M)" aria-label="Open map">⌖<kbd>M</kbd></button><button id="journal-button" title="Stories & work (J)" aria-label="Open missions">▤<kbd>J</kbd></button><button id="menu-button" title="Menu (Escape)" aria-label="Open menu">☰</button></nav></header>
  <section class="objective-card" aria-label="Tracked objective"><div class="objective-eyebrow"><span>◇ YOUR NEXT CHAPTER</span><strong id="objective-distance">16 m</strong></div><h2 id="objective-title">A place to begin</h2><p id="objective-text">Introduce yourself to Mira in Founders Square.</p><div class="objective-rule"></div><span class="objective-hint">Stories unfold through the people you meet.</span></section>
  <div id="toasts" role="status" aria-live="polite"></div>
  <section class="map-widget" aria-label="Neighborhood minimap"><div class="map-topline"><span>MERIDIAN</span><span>N ↑</span></div><canvas id="minimap" width="250" height="190" aria-label="Minimap showing your location, nearby people, roads, and route"></canvas><div class="map-bottomline"><span id="mode-label">ON FOOT</span><strong id="speed">EXPLORING</strong></div></section>
  <div id="interaction" class="interaction" hidden><kbd>E</kbd><div><strong id="interaction-label">Talk to Mira</strong><div class="action-track"><span id="action-progress"></span></div></div></div>
  <aside id="work-panel" class="work-panel" hidden><div class="work-heading"><span>HARBOR COFFEE</span><strong id="work-time">150s</strong></div><h3 id="work-order">ORDER 1 OF 3</h3><p id="work-status">Complete each order: brew → pack → serve.</p><div class="work-actions"><button data-work="brew">☕ <span>Brew</span></button><button data-work="pack">▣ <span>Pack</span></button><button data-work="serve">↗ <span>Serve</span></button></div></aside>
  <div id="control-help" class="control-help"><button id="help-button" aria-label="Show game controls">?</button><span id="controls-mode">WASD move · SHIFT run · SPACE jump · E interact</span><span class="extra-controls">M map · J stories · V postcard view · X break nearby props · R recover car</span></div><div class="save-indicator"><span class="save-dot"></span><span id="save-status">LOCAL PROGRESS</span></div>
</div>
<div id="photo-controls" hidden><div><span class="eyebrow">A POSTCARD FROM MERIDIAN</span><h2>Find your view.</h2><p>Drag to frame. Scroll to zoom.</p></div><div><button id="photo-capture">Capture postcard</button><button id="photo-close">Return <kbd>V</kbd></button></div></div><div id="telemetry" hidden></div><div id="overlay-scrim" hidden></div>
<section id="dialogue" class="dialogue" aria-label="Conversation" hidden><button id="dialogue-close" class="dialogue-close" aria-label="End conversation">×</button><div class="dialogue-intro"><div id="speaker-avatar" class="speaker-avatar">MP</div><div><span id="speaker-role" class="eyebrow">Community organizer</span><h2 id="speaker-name">Mira Patel</h2></div><span class="conversation-tag">CONVERSATION</span></div><p id="speaker-text" class="speaker-text"></p><div id="dialogue-choices" class="dialogue-choices"></div><form id="say-form"><label class="sr-only" for="say-input">Ask about work, directions, weather, or the harbor</label><input id="say-input" maxlength="200" placeholder="Ask about work, directions, weather…" autocomplete="off"><button type="submit" aria-label="Say this to the character">↵</button></form><p class="dialogue-note">Type a topic or choose a reply. Optional spoken dialogue is in the menu.</p></section>
<section id="map-panel" class="game-panel map-panel" aria-label="City map" hidden><div class="panel-header"><div><span class="eyebrow">GET TO KNOW THE CITY</span><h1>Port Meridian</h1></div><button data-close aria-label="Close map">×</button></div><div class="map-layout"><div class="full-map-wrap"><canvas id="world-map" aria-label="Full city map. Click to set a custom waypoint."></canvas><div class="full-map-caption"><span>Click anywhere to set a waypoint.</span><button id="shuttle-waypoint">City shuttle · $10</button><button id="clear-waypoint">Clear waypoint</button></div></div><aside><span class="eyebrow">PLACES & PEOPLE</span><div id="place-list"></div></aside></div><div class="panel-footnote">Old Meridian · East Harbor · Westhaven · Northline · Cedar Hills · Waterfront</div></section>
<section id="journal-panel" class="game-panel journal-panel" aria-label="Stories and work" hidden><div class="panel-header"><div><span class="eyebrow">MAKE YOUR OWN WAY</span><h1>Stories & work</h1></div><button data-close aria-label="Close missions">×</button></div><div class="journal-intro"><p>Meet the neighborhood. Earn your place in it.</p><strong id="journal-money">$250 · 0 reputation</strong></div><div id="mission-list" class="mission-list"></div></section>
<section id="menu-panel" class="game-panel menu-panel" aria-label="Pause menu" hidden><div class="panel-header"><div><span class="eyebrow">AURORA / PORT MERIDIAN</span><h1>A moment to pause.</h1></div><button data-close aria-label="Close menu">×</button></div><p class="menu-blurb">A coastal city, a new beginning, and a neighborhood full of stories.</p><button id="continue-game" class="primary-button">Return to the city <span>↗</span></button><div class="menu-buttons"><button id="save-game">Save progress</button><button id="recover-car">Recover your car</button></div><label class="setting-label" for="quality">Graphics quality</label><select id="quality"><option value="performance">Performance · lighter effects</option><option value="balanced" selected>Balanced · recommended</option><option value="cinematic">Cinematic · richer lighting</option></select><label class="voice-setting"><span><strong>Spoken conversations</strong><small>Use the voices available in your browser.</small></span><input id="voice-toggle" type="checkbox"></label><div class="menu-controls"><span><kbd>WASD</kbd> Move or drive</span><span><kbd>SHIFT</kbd> Sprint</span><span><kbd>E</kbd> Talk, interact, enter / exit</span><span><kbd>SPACE</kbd> Jump / handbrake</span><span><kbd>M</kbd> City map</span><span><kbd>J</kbd> Stories & work</span><span><kbd>V</kbd> Postcard view</span><span><kbd>R</kbd> Recover your car</span><span><kbd>X</kbd> Break a nearby prop</span><span><kbd>F3</kbd> Performance</span></div><div class="menu-footer"><button id="new-game">Start a new visit</button><a href="lab.html">Engine laboratory ↗</a></div><p class="local-note">Progress saves on this browser. This is an expanding single-player city prototype.</p></section>
<section id="welcome" class="welcome"><div class="welcome-top"><span>AURORA</span><span>CHAPTER TWO / PORT MERIDIAN</span></div><div class="welcome-copy"><span class="eyebrow">SOMEWHERE WORTH GETTING TO KNOW</span><h1>A city.<br>A thousand<br><em>small stories.</em></h1><p>Find your way through Port Meridian. Meet its people,<br>make a living, and see where the neighborhood takes you.</p><button id="enter-city">Enter the city <span>↗</span></button><div class="welcome-pills"><span>EXPLORE</span><i>·</i><span>TALK</span><i>·</i><span>WORK</span><i>·</i><span>DISCOVER</span></div></div><div class="welcome-bottom"><span>WASD to move · E to interact · M for your map</span><span>A place to begin.</span></div></section>
<section id="reset-confirm" class="confirmation" role="dialog" aria-label="Start a new visit" hidden><div><span class="eyebrow">START FRESH</span><h2>Begin a new visit?</h2><p>Your current local progress will be replaced.</p><button id="confirm-reset">Start a new visit</button><button id="cancel-reset">Keep exploring</button></div></section>
<section id="fatal-error" class="confirmation" role="alert" hidden><div><span class="eyebrow">AURORA</span><h2>The city could not start.</h2><p id="fatal-message"></p><p>Run this project through the included local server.</p><button onclick="location.reload()">Try again</button></div></section>
<div id="touch-controls" aria-label="Touch controls"><div><button data-key="KeyW">↑</button><div><button data-key="KeyA">←</button><button data-key="KeyS">↓</button><button data-key="KeyD">→</button></div></div><button id="touch-interact" data-key="KeyE">E</button></div>
</main><script type="module" src="src/city-main.js"></script></body></html>
```

## style.css

```css
:root{font-family:Inter,ui-sans-serif,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;color:#edf0e4;background:#14282b;font-synthesis:none;--gold:#e8bc7f;--teal:#9cc5bd;--line:#bdcfc126}*{box-sizing:border-box}body{margin:0;overflow:hidden}button,input,select{font:inherit}button{cursor:pointer;color:inherit;border:0}button:disabled{opacity:.45;cursor:default}button:focus-visible,a:focus-visible,input:focus-visible,select:focus-visible{outline:2px solid var(--gold);outline-offset:4px}[hidden]{display:none!important}#game-shell{position:relative;width:100vw;height:100dvh;min-height:420px;overflow:hidden}#game-view{display:block;width:100%;height:100%;outline:none;touch-action:none}#hud{position:absolute;inset:0;pointer-events:none;background:linear-gradient(#0e22294d,transparent 25%,transparent 72%,#0b222b35)}#hud button,#hud nav{pointer-events:auto}.city-header{display:flex;align-items:center;padding:27px 32px;gap:35px}.brand{display:flex;align-items:center;gap:12px}.brand-mark{display:grid;place-items:center;border:1px solid #dae1c17a;height:36px;width:36px;transform:rotate(-3deg);border-radius:8px;color:var(--gold);font-size:24px;font-weight:300}.brand strong{display:block;font-size:19px;letter-spacing:4px;font-weight:650}.brand div>span{display:block;font:8px ui-monospace,monospace;letter-spacing:2px;margin-top:4px;color:#cad6ca}.location{display:flex;gap:11px;align-items:center;border-left:1px solid #cad5cb3d;padding-left:28px}.location-dot{width:5px;height:5px;border-radius:50%;background:var(--gold)}.location strong{display:block;font-size:12px;font-weight:500}.location div>span{display:block;color:#c0cec3;font-size:9px;margin-top:6px}.location i{padding:0 7px;font-style:normal}.top-actions{margin-left:auto;display:flex;align-items:center;gap:8px}.top-actions button{display:flex;align-items:center;justify-content:center;gap:6px;background:#122b306b;backdrop-filter:blur(14px);height:34px;min-width:35px;border:1px solid #d9e5d42b;border-radius:6px;font-size:17px}.top-actions kbd{font-size:7px;padding:2px 3px}.wallet{text-align:right;padding:0 17px 0 12px}.wallet strong{display:block;font:20px ui-monospace,monospace;color:#f4e5be}.wallet>span{display:block;font:7px ui-monospace,monospace;letter-spacing:1.2px;color:#cad4c3;margin-top:5px}.wallet b{font-weight:400;color:var(--gold)}.mode-chip{font:7px ui-monospace,monospace;letter-spacing:1px;border:1px solid #d7e2d333;border-radius:3px;padding:5px;color:#c6d2c1}.objective-card{position:absolute;top:111px;right:32px;width:292px;border:1px solid #e2dec331;border-top:2px solid #d9b47a99;padding:18px 19px 15px;background:linear-gradient(120deg,#10282cdd,#173035c2);backdrop-filter:blur(15px);border-radius:3px 3px 10px 10px;box-shadow:0 14px 35px #071c2422}.objective-eyebrow{display:flex;justify-content:space-between;font:8px ui-monospace,monospace;letter-spacing:1.25px;color:var(--gold)}.objective-eyebrow strong{font-weight:400;letter-spacing:0}.objective-card h2{font-size:19px;font-weight:450;letter-spacing:-.4px;margin:11px 0 8px}.objective-card p{font-size:11px;line-height:1.7;color:#c3d1c6;margin:0;min-height:35px}.objective-rule{height:1px;background:#dbdec31f;margin:14px 0 10px}.objective-hint{font-size:8px;color:#91a9a1}.map-widget{position:absolute;left:28px;bottom:63px;width:244px;border:1px solid #d5ddbf38;border-radius:10px;overflow:hidden;background:#172c2de8;box-shadow:0 8px 36px #0a19264d;backdrop-filter:blur(14px)}#minimap{display:block;width:100%;height:171px;opacity:.92}.map-topline,.map-bottomline{display:flex;justify-content:space-between;padding:10px 12px;font:8px ui-monospace,monospace;letter-spacing:1.5px;color:#cad8c8}.map-topline{border-bottom:1px solid #c7d8cb1c}.map-topline>span:last-child{color:var(--gold)}.map-bottomline{padding:11px 12px;color:#92b2ab}.map-bottomline strong{font-size:9px;letter-spacing:.7px;color:#e6d5af;font-weight:400}.interaction{position:absolute;bottom:95px;left:50%;transform:translateX(-50%);display:flex;align-items:center;gap:14px;background:#10292edd;backdrop-filter:blur(16px);border:1px solid #e1d6aa45;border-radius:8px;padding:13px 20px;min-width:230px;max-width:440px}.interaction>kbd{display:grid;place-items:center;width:31px;height:32px;background:#e5d4ac;color:#173135;border:0;border-radius:4px;font:15px ui-monospace,monospace}.interaction strong{font-size:12px;line-height:1.5;font-weight:500}.action-track{height:2px;background:#d2d8c328;margin-top:7px;overflow:hidden}.action-track span{display:block;width:0%;height:2px;background:var(--gold)}.control-help{position:absolute;bottom:23px;left:50%;transform:translateX(-50%);display:flex;align-items:center;gap:12px;color:#d0d9cb;font-size:9px;white-space:nowrap;text-shadow:0 1px 5px #05141a}.control-help button{display:grid;place-items:center;width:22px;height:22px;border:1px solid #cdd6bf4d;border-radius:50%;font:10px ui-monospace,monospace;background:#132a2e7a}.extra-controls{display:none}.control-help.expanded{white-space:normal;flex-wrap:wrap;justify-content:center;width:70%;padding:10px 15px;background:#11292ee8;border-radius:7px;bottom:15px}.control-help.expanded .extra-controls{display:block;width:100%;text-align:center;color:var(--gold);font-size:9px}.save-indicator{position:absolute;bottom:25px;right:31px;display:flex;align-items:center;gap:6px;font:7px ui-monospace,monospace;color:#afc5b7;letter-spacing:1.2px}.save-dot{width:4px;height:4px;background:#9cc5ad;border-radius:50%}.world-label{position:absolute;transform:translate(-50%,-100%);font:9px ui-monospace,monospace;letter-spacing:.4px;border-radius:4px;white-space:nowrap;padding:6px 8px;background:#112b30af;border:1px solid #c3d4c535;color:#cddccd;box-shadow:0 2px 8px #071b2633}.world-label.person{background:#152e31d9;color:#f1e4c4;border-color:#e3d2a75c}.world-label.objective{border-radius:20px;color:var(--gold);border-color:#e2c18f7f;background:#20322bdc}.world-label.objective:before{content:"◇";margin-right:5px}#world-labels{position:absolute;inset:0;pointer-events:none}#toasts{position:absolute;left:50%;top:100px;transform:translateX(-50%);display:flex;flex-direction:column;gap:7px;align-items:center;max-width:510px;width:55%}.toast{font-size:10px;line-height:1.6;background:#122a2feb;border:1px solid #bacdb738;border-radius:7px;padding:11px 16px;color:#d5e1d0;box-shadow:0 6px 24px #071a251f;animation:appear .3s ease}.toast>span{color:var(--gold);margin-right:9px}.toast.success{border-color:#d4bc7477;background:#26372ef2}.work-panel{position:absolute;right:30px;bottom:82px;width:280px;padding:18px;background:#17302fee;border:1px solid #d6bc7b5c;border-radius:10px;pointer-events:auto;backdrop-filter:blur(15px)}.work-heading{display:flex;justify-content:space-between;font:8px ui-monospace,monospace;letter-spacing:1.5px;color:var(--gold)}.work-panel h3{font:14px ui-monospace,monospace;margin:13px 0 9px}.work-panel p{font-size:10px;line-height:1.6;color:#bacebd}.work-actions{display:flex;gap:6px;margin-top:15px}.work-actions button{flex:1;display:flex;flex-direction:column;align-items:center;gap:7px;background:#26423d;border:1px solid #c2d6bc2b;border-radius:6px;padding:11px;font-size:17px}.work-actions button span{font-size:10px}.work-actions button.next{border-color:var(--gold);color:var(--gold);background:#3b4937}#overlay-scrim{position:absolute;inset:0;background:#071e28a8;backdrop-filter:blur(8px);z-index:15}.game-panel{position:absolute;z-index:20;left:50%;top:50%;transform:translate(-50%,-50%);background:linear-gradient(135deg,#153035fa,#142c2efb);border:1px solid #c6d6bd38;border-radius:14px;box-shadow:0 24px 90px #00162177;max-height:90dvh;overflow:auto;scrollbar-width:thin;scrollbar-color:#5d7765 #173031}.panel-header{display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid var(--line);padding:25px 28px 22px}.eyebrow{font:8px ui-monospace,monospace;letter-spacing:2px;color:#a6bcb0}.panel-header h1{font-size:30px;letter-spacing:-.9px;font-weight:430;margin:9px 0 0}.panel-header>button{width:31px;height:31px;font-size:25px;background:#2a4641;border-radius:50%;color:#d2dfd0}.map-panel{width:min(1080px,94vw)}.map-layout{display:grid;grid-template-columns:minmax(0,1fr) 285px}.full-map-wrap{background:#203733;display:flex;flex-direction:column;min-height:500px}.full-map-wrap canvas{width:100%;height:min(62dvh,610px);display:block;cursor:crosshair}.full-map-caption{display:flex;align-items:center;justify-content:space-between;padding:15px 20px;font-size:9px;color:#a3baab;gap:12px}.full-map-caption button{font-size:9px;background:#314d43;padding:7px 11px;border:1px solid #cddbc125;border-radius:4px}.map-layout aside{padding:21px 18px;border-left:1px solid var(--line);max-height:68dvh;overflow-y:auto;scrollbar-width:thin}.place-item{display:flex;align-items:center;gap:10px;width:100%;background:transparent;padding:13px 0;border-bottom:1px solid var(--line);text-align:left}.place-item>span:last-child{font-size:12px;color:var(--gold)}.place-icon{color:var(--gold);font-size:17px}.place-item strong{display:block;font-size:11px;font-weight:500}.place-item small{display:block;font-size:9px;color:#91ada0;line-height:1.6;margin-top:5px}.panel-footnote{padding:14px 23px;border-top:1px solid var(--line);font:8px ui-monospace,monospace;color:#8da69a}.journal-panel{width:min(950px,93vw)}.journal-intro{display:flex;justify-content:space-between;align-items:center;padding:10px 28px;gap:15px}.journal-intro p{font-size:11px;color:#adbfaf}.journal-intro strong{font:10px ui-monospace,monospace;color:var(--gold);font-weight:400}.mission-list{display:grid;grid-template-columns:1fr 1fr;gap:13px;padding:10px 26px 28px}.mission-card{padding:20px;border:1px solid #a9c6ac26;background:#10292c74;border-radius:9px}.mission-card.active{border-color:#d5b57873;background:#2a3d32a6}.mission-meta{display:flex;justify-content:space-between;gap:8px;font:7px ui-monospace,monospace;letter-spacing:1.2px;color:#a6beab}.mission-meta>span:first-child{color:var(--gold)}.mission-card h3{font-size:18px;font-weight:450;letter-spacing:-.4px;margin:14px 0 10px}.mission-card p{font-size:10px;line-height:1.8;color:#b4c8b7;min-height:53px;margin:0}.mission-footer{display:flex;justify-content:space-between;align-items:center;gap:10px;margin-top:17px}.mission-footer>span{font:14px ui-monospace,monospace;color:#eadab5}.mission-footer small{font-size:8px;color:#a1bba4;margin-left:7px}.mission-footer button{font-size:9px;border:1px solid #b9cfb23d;border-radius:4px;background:#335244;padding:8px 12px}.menu-panel{width:min(520px,92vw);padding-bottom:20px}.menu-blurb{padding:3px 28px;color:#afc3b0;font-size:11px;line-height:1.8}.primary-button{display:flex;justify-content:space-between;align-items:center;margin:16px 28px;width:calc(100% - 56px);padding:15px 18px;background:#dfc28e;color:#1b342f;border-radius:5px;font-size:12px;font-weight:600}.menu-buttons{display:flex;gap:9px;margin:0 28px 22px}.menu-buttons button{flex:1;background:#27463f;border:1px solid #c6d9bf2b;border-radius:5px;padding:12px;font-size:10px}.setting-label{display:block;margin:0 28px 8px;font-size:10px;color:#aec5b5}.menu-panel select{margin:0 28px;width:calc(100% - 56px);padding:11px;border:1px solid #c1d5b735;border-radius:5px;background:#18372f;color:#d5e3cb;font-size:11px}.voice-setting{display:flex;align-items:center;justify-content:space-between;margin:19px 28px;padding-bottom:20px;border-bottom:1px solid var(--line)}.voice-setting strong{display:block;font-size:10px;font-weight:500}.voice-setting small{display:block;font-size:9px;color:#92afa0;margin-top:6px}.voice-setting input{accent-color:var(--gold);width:17px;height:17px}.menu-controls{display:grid;grid-template-columns:1fr 1fr;gap:13px 8px;margin:20px 28px;font-size:9px;color:#b2c7b7}.menu-controls span{display:flex;align-items:center;gap:7px}kbd{font:8px ui-monospace,monospace;border:1px solid #b7cbb53d;border-radius:3px;padding:3px 5px;color:#d2dec9;background:#122b2e66}.menu-footer{display:flex;justify-content:space-between;margin:24px 28px 14px;padding-top:18px;border-top:1px solid var(--line)}.menu-footer button,.menu-footer a{font-size:9px;color:#b3bda9;background:none;text-decoration:none;padding:0}.local-note{font-size:8px;color:#789b8a;line-height:1.6;margin:0 28px}.dialogue{position:absolute;z-index:12;width:min(640px,90vw);bottom:35px;left:50%;transform:translateX(-50%);padding:24px 28px 17px;background:linear-gradient(135deg,#152f33f5,#18372ff5);backdrop-filter:blur(25px);border:1px solid #d2cfaa57;border-radius:12px;box-shadow:0 22px 70px #001c2773;max-height:80dvh;overflow:auto;scrollbar-width:thin}.dialogue-close{position:absolute;right:13px;top:10px;font-size:20px;background:transparent;color:#abc4b2}.dialogue-intro{display:flex;align-items:center;gap:14px}.speaker-avatar{display:grid;place-items:center;height:47px;width:47px;border-radius:9px;border:1px solid #ded4ad44;color:#ead8b1;font:16px ui-monospace,monospace}.dialogue-intro h2{font-size:20px;letter-spacing:-.4px;font-weight:450;margin:5px 0 0}.dialogue-intro .eyebrow{font-size:8px;letter-spacing:1px;color:#b4c4b0}.conversation-tag{margin-left:auto;font:7px ui-monospace,monospace;letter-spacing:1.3px;color:#95b09e}.speaker-text{font-size:13px;line-height:1.9;color:#e0e5d2;margin:21px 0;padding-bottom:18px;border-bottom:1px solid #c5d3b826}.dialogue-choices{display:flex;flex-direction:column;gap:6px}.dialogue-choices button{display:flex;align-items:center;gap:10px;text-align:left;background:#2a463a9c;border:1px solid #bbcbb220;border-radius:5px;padding:9px 10px;font-size:10px;color:#d1dec4}.dialogue-choices button:hover{background:#3a5541;border-color:#d4bd8288}.dialogue-choices button>span:last-child,.dialogue-choices button small{margin-left:auto;color:var(--gold)}.dialogue-choices button kbd{color:#e5c58d;font-size:8px;border:0;background:#122e2d77;width:21px;text-align:center}.dialogue form{display:flex;align-items:center;margin-top:14px;border:1px solid #c2d0b52e;border-radius:5px;overflow:hidden}.dialogue input{min-width:0;flex:1;background:#102c2e8c;color:#e3e7d2;border:0;padding:12px;font-size:10px;outline:none}.dialogue input::placeholder{color:#86a494}.dialogue form button{padding:10px 15px;background:#2c483c;font-size:16px;color:var(--gold)}.dialogue-note{font-size:8px;line-height:1.7;color:#7f9f8c;margin:10px 0 0}.welcome{position:absolute;inset:0;z-index:30;padding:30px 4.5vw;background:linear-gradient(90deg,#0c242df8 2%,#102c34dd 33%,#15333377 60%,#1b39322b 100%);display:flex;flex-direction:column;justify-content:space-between;transition:opacity .65s,transform .65s}.welcome.departing{opacity:0;pointer-events:none;transform:scale(1.015)}.welcome-top,.welcome-bottom{display:flex;justify-content:space-between;align-items:center;color:#acbda8;font:8px ui-monospace,monospace;letter-spacing:1.8px}.welcome-top>span:first-child{font-size:20px;letter-spacing:5px;color:#e9dfbf;font-weight:600}.welcome-copy{max-width:650px}.welcome-copy .eyebrow{color:#a1b6a5;font-size:8px;letter-spacing:2px}.welcome-copy h1{font-family:Georgia,'Times New Roman',serif;font-size:clamp(52px,7.6vw,105px);font-weight:400;line-height:.99;letter-spacing:-3px;margin:25px 0 24px;color:#efead7}.welcome-copy h1 em{color:#d8b87f;font-weight:400}.welcome-copy p{font-size:12px;line-height:1.9;color:#b6c8b2;margin:0 0 30px}.welcome-copy>button{display:flex;justify-content:space-between;align-items:center;gap:65px;padding:16px 22px;background:#d7bd86;color:#1a3434;border-radius:4px;font-size:12px;font-weight:600;border:1px solid #e5d0a744;transition:.2s}.welcome-copy>button:hover{background:#ead3a0;transform:translateY(-2px)}.welcome-copy>button span{font-size:20px;font-weight:400}.welcome-pills{display:flex;align-items:center;gap:13px;margin-top:23px;font:7px ui-monospace,monospace;letter-spacing:1.5px;color:#91ad9d}.welcome-pills i{font-style:normal;color:#d2b980}.welcome-bottom{font-size:7px;letter-spacing:1px;color:#91ab9b}#photo-controls{position:absolute;inset:0;padding:35px;pointer-events:none;display:flex;justify-content:space-between;align-items:flex-start;background:linear-gradient(#10252b44,transparent 30%,transparent 70%,#10252b44)}#photo-controls h2{font-family:Georgia,serif;font-size:38px;font-weight:400;margin:9px 0;color:#ece5c9}#photo-controls p{font-size:11px;color:#c4d0bc}#photo-controls button{pointer-events:auto;border:1px solid #d1d4b64f;background:#203b35d1;border-radius:5px;padding:12px 15px;margin-left:9px;font-size:10px;color:#e4d9b4}#photo-controls button:first-child{background:#d6bc84;color:#19372f}body.photo-mode #hud,body.photo-mode #world-labels{display:none}.confirmation{position:absolute;inset:0;z-index:50;display:grid;place-items:center;background:#0c232ce8;backdrop-filter:blur(10px)}.confirmation>div{width:min(420px,90vw);padding:30px;border:1px solid #d0cfaa4d;background:#1b3632;border-radius:12px}.confirmation h2{font-size:26px;font-weight:400;margin:15px 0}.confirmation p{font-size:12px;line-height:1.7;color:#b9c9b5}.confirmation button{border:1px solid #d3c99c55;background:#314e3d;border-radius:5px;padding:11px 14px;font-size:10px;margin:10px 7px 0 0}#telemetry{position:absolute;bottom:7px;left:10px;font:9px ui-monospace,monospace;color:#e8d5a8;background:#0c2528cf;padding:5px 8px;border-radius:3px;z-index:40}.sr-only{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap}#touch-controls{display:none}button:hover:not(:disabled){filter:brightness(1.1)}@keyframes appear{from{opacity:0;transform:translateY(-8px)}to{opacity:1;transform:none}}
@media(max-width:1100px){.city-header{gap:20px;padding:22px}.location{padding-left:20px}.objective-card{right:23px;top:100px;width:260px}.map-widget{width:211px;left:22px}.map-widget canvas{height:145px}.mode-chip{display:none}.control-help{font-size:8px}.save-indicator{font-size:6px;right:22px}.welcome-copy h1{font-size:clamp(48px,7vw,85px)}}
@media(max-width:760px){.city-header{padding:16px;gap:14px}.brand strong{font-size:15px;letter-spacing:3px}.brand div>span{font-size:6px}.brand-mark{width:29px;height:29px;font-size:19px}.location{padding-left:12px}.location strong{font-size:10px}.location div>span{font-size:7px}.wallet{padding:0 6px}.wallet strong{font-size:16px}.wallet>span{font-size:6px;letter-spacing:.5px}.top-actions button{height:29px;min-width:29px;font-size:14px}.top-actions button kbd{display:none}.objective-card{top:85px;right:15px;width:220px;padding:13px 14px}.objective-card h2{font-size:16px}.objective-card p{font-size:10px;min-height:0}.objective-hint{display:none}.objective-rule{margin-bottom:0}.objective-eyebrow{font-size:6px}.map-widget{width:172px;left:14px;bottom:55px}.map-widget canvas{height:114px}.map-topline,.map-bottomline{font-size:6px;padding:7px 9px}.map-bottomline strong{font-size:7px}.control-help{bottom:18px;max-width:96vw;width:96vw;justify-content:center;font-size:7px}.save-indicator{bottom:47px;right:15px;font-size:6px}.interaction{bottom:91px;max-width:calc(100vw - 30px);min-width:180px;padding:11px 15px}.interaction strong{font-size:10px}.work-panel{width:236px;right:15px;bottom:63px;padding:14px}.welcome{padding:23px}.welcome-top{font-size:6px;letter-spacing:1px}.welcome-top>span:first-child{font-size:17px}.welcome-copy h1{font-size:62px;letter-spacing:-2px;margin:20px 0}.welcome-copy p{font-size:10px}.welcome-copy .eyebrow{font-size:7px;letter-spacing:1.4px}.welcome-copy>button{padding:14px 19px;font-size:11px}.welcome-bottom>span:last-child{display:none}.map-layout{grid-template-columns:1fr}.map-layout aside{max-height:190px;border-left:0;border-top:1px solid var(--line)}.full-map-wrap{min-height:330px}.full-map-wrap canvas{height:46dvh}.panel-header{padding:20px}.panel-header h1{font-size:24px}.journal-intro{padding:10px 20px;align-items:flex-start;flex-direction:column;gap:0}.mission-list{padding:10px 18px 22px;gap:10px}.mission-card{padding:15px}.mission-card h3{font-size:16px}.mission-card p{font-size:9px}.mission-footer small{font-size:7px;display:block;margin:5px 0 0}.mission-footer button{font-size:8px;padding:8px}.dialogue{bottom:20px;padding:20px;max-height:84dvh}.conversation-tag{display:none}.speaker-text{font-size:12px;line-height:1.7;margin:16px 0}.dialogue-intro h2{font-size:18px}.dialogue-choices button{font-size:9px}#toasts{top:80px;left:15px;transform:none;width:45%;align-items:flex-start}.toast{font-size:8px;padding:9px 11px}.world-label{font-size:7px}#photo-controls{padding:20px}#photo-controls h2{font-size:28px}#photo-controls button{font-size:8px;padding:10px;margin:0 0 6px 8px}#photo-controls>div:last-child{display:flex;flex-direction:column}}
@media(max-width:480px){.location{display:none}.top-actions{gap:5px}.objective-card{width:205px}.welcome-copy h1{font-size:55px}.mission-list{grid-template-columns:1fr}.map-widget{width:150px}.map-widget canvas{height:100px}.interaction{left:auto;right:15px;transform:none;bottom:90px;max-width:52vw;min-width:150px;padding:10px}.interaction strong{font-size:9px}.interaction>kbd{width:23px;height:25px;font-size:11px}.work-panel{width:210px}.menu-controls{font-size:8px}.welcome-copy p br{display:none}}
@media(pointer:coarse){#touch-controls{display:flex;position:absolute;left:15px;right:15px;bottom:73px;justify-content:space-between;align-items:flex-end;z-index:9;pointer-events:none}#touch-controls>div{pointer-events:auto;display:flex;flex-direction:column;align-items:center;gap:5px}#touch-controls button{pointer-events:auto;width:38px;height:38px;margin:0 2px;border:1px solid #d2d4b949;border-radius:8px;color:#ebdebc;background:#153332ad;touch-action:none}.map-widget{bottom:210px}.interaction{bottom:148px}.control-help{bottom:18px}.work-panel{bottom:130px}}
```

## lab.html

```html
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Aurora · Phase One Engine Laboratory</title><meta name="description" content="A custom WebGL 2 research engine with deferred PBR, ray-traced shadows, volumetric atmosphere and rigid body physics."><link rel="stylesheet" href="lab.css"></head>
<body>
  <header class="topbar"><a class="wordmark" href="./" aria-label="Aurora engine home"><span class="mark">A</span>AURORA<span class="wordmark-sub">ENGINE LABORATORY</span></a><div class="top-status"><span class="live-dot"></span><span id="status">PHASE 01 / CORE SYSTEMS</span><span class="version">v0.1.0</span></div></header>
  <main class="workspace">
    <section class="scene" aria-label="Interactive 3D engine test scene">
      <canvas id="viewport" tabindex="0" aria-label="3D scene. Drag to orbit, scroll to zoom. WASD drive the test vehicle, space for handbrake, F to launch a sphere."></canvas>
      <div class="scene-title"><span class="eyebrow">REAL-TIME SYSTEMS TESTBED</span><h1>Light. Motion.<br><span>Material.</span></h1><p>A rendering & physics foundation,<br>built from first principles.</p></div>
      <div class="time-chip"><span class="sun-icon">◒</span><div><span class="small-label">SOLAR TIME</span><strong id="clock">16:24</strong></div></div>
      <div class="scene-badges"><span>WEBGL 2</span><span>HDR / PBR</span><span>120 Hz PHYSICS</span></div>
      <div class="scene-bottom"><div class="vehicle-readout"><span class="small-label">RAYCAST VEHICLE</span><strong id="speed">0 km/h</strong><span id="rpm">900 rpm · 1</span></div><button id="reset-view" class="quiet-button">↺ Reset view</button></div>
      <div class="help-strip"><span><kbd>DRAG</kbd> orbit</span><span><kbd>SCROLL</kbd> zoom</span><span><kbd>W A S D</kbd> drive</span><span><kbd>SPACE</kbd> handbrake</span><span><kbd>B</kbd> brake</span></div>
    </section>
    <aside class="controls" aria-label="Engine controls">
      <div class="panel-heading"><span class="eyebrow">LIVE CONTROLS</span><h2>Engine inspector</h2><span class="panel-note">Every control changes the running simulation.</span></div>
      <section class="control-group"><div class="group-title"><span>01</span><h3>Atmosphere</h3></div>
        <label class="range-label" for="hour">Sun position <output id="hour-value">16:24</output></label><input id="hour" type="range" min="0" max="23.99" step=".05" value="16.4"><div class="range-hints"><span>Midnight</span><span>Noon</span><span>Midnight</span></div>
        <label class="toggle"><span>Advance day / night</span><input id="cycle" type="checkbox"><span class="switch"></span></label>
        <label class="range-label" for="rain">Rain intensity <output id="rain-value">0%</output></label><input id="rain" type="range" min="0" max="1" step=".05" value="0"><div class="mini-metric"><span>Surface saturation</span><strong id="wetness">0%</strong></div>
      </section>
      <section class="control-group"><div class="group-title"><span>02</span><h3>Render pipeline</h3></div>
        <label class="select-label" for="shadows">Sun shadows</label><select id="shadows"><option value="0">Raster map · 3 × 3 PCF</option><option value="1">Ray traced · triangle BVH</option></select>
        <label class="toggle"><span>Screen-space indirect light</span><input id="gi" type="checkbox" checked><span class="switch"></span></label>
        <label class="toggle"><span>Volumetric fog</span><input id="fog" type="checkbox" checked><span class="switch"></span></label>
        <label class="toggle"><span>Wet surface reflections</span><input id="ssr" type="checkbox" checked><span class="switch"></span></label>
        <label class="toggle"><span>GPU occlusion queries</span><input id="occlusion" type="checkbox" checked><span class="switch"></span></label>
        <label class="range-label" for="exposure">Exposure</label><input id="exposure" type="range" min=".3" max="2.5" step=".05" value="1.1">
        <label class="range-label" for="resolution">Render scale <output id="resolution-label">80%</output></label><input id="resolution" type="range" min=".35" max="1" step=".05" value=".8">
        <label class="select-label" for="debug">Buffer inspection</label><select id="debug"><option value="0">Final image</option><option value="1">Linear albedo</option><option value="2">World normals</option><option value="3">Sun visibility</option><option value="4">Wetness field</option><option value="5">Indirect illumination</option></select>
      </section>
      <section class="control-group"><div class="group-title"><span>03</span><h3>Physics bench</h3></div><div class="action-grid"><button id="ragdoll">Drop ragdoll <span>↘</span></button><button id="launch">Launch sphere <span>↗</span></button></div><button id="reset-car" class="full-button">Reset vehicle <kbd>R</kbd></button><button id="pause" class="full-button" aria-pressed="false">Pause simulation</button><p class="physics-note">Sequential impulses · articulated joints · XPBD surface · tire friction circle</p></section>
      <p id="notice" role="status" class="notice">Experimental foundation. Phase 1 only.</p>
    </aside>
  </main>
  <footer class="telemetry" aria-label="Performance telemetry"><div class="fps-stat"><strong id="fps">—</strong><span>FPS</span></div><div><span>CPU / FRAME</span><strong id="cpu">—</strong></div><div><span>GPU / FRAME</span><strong id="gpu">—</strong></div><div><span>TRIANGLES</span><strong id="triangles">—</strong></div><div><span>RIGID BODIES</span><strong id="bodies">—</strong></div><div><span>CONTACTS</span><strong id="contact-count">—</strong></div><div><span>CULLED / TOTAL</span><strong id="culling">—</strong></div><div><span>STREAMED TEXTURES</span><strong id="memory">—</strong></div></footer>
  <div id="error" class="error-overlay" role="alert" hidden><div><span class="eyebrow">ENGINE DIAGNOSTIC</span><h2>Renderer could not continue</h2><pre id="error-message"></pre><p>Use a browser with WebGL 2, floating-point render targets, and hardware acceleration enabled.</p><button id="dismiss-error">Dismiss</button></div></div>
  <script type="module" src="src/main.js"></script>
</body></html>
```

## lab.css

```css
:root{font-family:Inter,ui-sans-serif,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;color:#e3e9ed;background:#0c1014;font-synthesis:none;--cyan:#64d9d0;--muted:#77858e;--border:#232d34;--panel:#12191f}*{box-sizing:border-box}body{margin:0}button,input,select{font:inherit}button,select{cursor:pointer}button:focus-visible,input:focus-visible,select:focus-visible,canvas:focus-visible{outline:2px solid var(--cyan);outline-offset:3px}.topbar{height:78px;display:flex;align-items:center;justify-content:space-between;padding:0 32px;border-bottom:1px solid var(--border)}.wordmark{font-size:20px;font-weight:750;letter-spacing:3px;text-decoration:none;color:inherit;display:flex;align-items:center;gap:12px}.mark{height:32px;width:32px;display:grid;place-items:center;border:1px solid #5fa39f;border-radius:8px;font-size:20px;color:var(--cyan);font-weight:400}.wordmark-sub{font-size:9px;letter-spacing:2px;color:var(--muted);margin-left:12px;padding-left:24px;border-left:1px solid var(--border);font-weight:500}.top-status{font:10px ui-monospace,monospace;letter-spacing:1px;display:flex;align-items:center;gap:10px;color:#acb8c0}.live-dot{width:5px;height:5px;border-radius:50%;background:var(--cyan);box-shadow:0 0 12px #64d9d055}.version{color:#64717a;margin-left:16px}.workspace{display:grid;grid-template-columns:minmax(0,1fr) 320px;min-height:680px;height:calc(100dvh - 165px)}.scene{position:relative;min-width:0;overflow:hidden;background:#16212c}#viewport{width:100%;height:100%;display:block;touch-action:none}.scene-title{position:absolute;left:36px;top:35px;pointer-events:none;text-shadow:0 2px 20px #081019aa}.eyebrow{font:9px ui-monospace,monospace;letter-spacing:2px;color:#8babae}.scene-title h1{font-size:clamp(28px,3.4vw,51px);line-height:1.08;font-weight:450;letter-spacing:-2px;margin:14px 0 16px}.scene-title h1 span{color:var(--cyan)}.scene-title p{font-size:11px;line-height:1.8;color:#c3d1d7;margin:0}.time-chip{position:absolute;right:28px;top:36px;display:flex;align-items:center;gap:10px;padding:10px 14px;background:#10192099;border:1px solid #6b81852a;border-radius:8px;backdrop-filter:blur(12px)}.time-chip .small-label{font-size:8px;letter-spacing:1px}.time-chip strong{display:block;font:19px ui-monospace,monospace;margin-top:3px}.sun-icon{font-size:26px;color:#dbb98a}.small-label{color:#8b9da8;font:9px ui-monospace,monospace;letter-spacing:1.5px}.scene-badges{position:absolute;left:36px;top:260px;display:flex;gap:6px;pointer-events:none}.scene-badges span{padding:5px 7px;font:8px ui-monospace,monospace;letter-spacing:1px;color:#c2d3d9;border:1px solid #95c1c333;background:#12212e66;border-radius:3px}.scene-bottom{position:absolute;left:34px;right:28px;bottom:80px;display:flex;align-items:flex-end;justify-content:space-between;pointer-events:none}.scene-bottom button{pointer-events:auto}.vehicle-readout{display:flex;flex-direction:column;text-shadow:0 2px 10px #000}.vehicle-readout strong{font-size:30px;font-weight:400;letter-spacing:-1px;margin:7px 0}.vehicle-readout>span:last-child{font:10px ui-monospace,monospace;color:#a7b6bf}.quiet-button{background:#11212d88;color:#c2d1d6;border:1px solid #90b4bc44;border-radius:5px;padding:9px 12px;font-size:10px}.help-strip{position:absolute;left:0;bottom:0;right:0;display:flex;flex-wrap:wrap;align-items:center;justify-content:center;gap:18px;padding:17px 12px;background:linear-gradient(0deg,#08131ee6,#08131e88);font-size:9px;color:#9bacb7}kbd{font:9px ui-monospace,monospace;border:1px solid #4f626e66;border-radius:3px;padding:3px 5px;margin-right:5px;color:#c0cfd5}.controls{background:var(--panel);border-left:1px solid var(--border);padding:28px 24px 20px;overflow-y:auto;scrollbar-width:thin;scrollbar-color:#334550 transparent}.panel-heading h2{font-size:21px;font-weight:500;letter-spacing:-.6px;margin:9px 0}.panel-note{color:#7c8b94;font-size:10px;line-height:1.6}.control-group{border-top:1px solid var(--border);padding:22px 0 20px;margin-top:21px}.control-group+.control-group{margin-top:0}.group-title{display:flex;align-items:center;gap:10px;margin-bottom:19px}.group-title>span{font:9px ui-monospace,monospace;color:var(--cyan);border:1px solid #64d9d033;border-radius:4px;padding:4px}.group-title h3{font-size:12px;font-weight:550;letter-spacing:.2px;margin:0}.range-label{display:flex;justify-content:space-between;font-size:10px;color:#adbac3;margin:15px 0 8px}.range-label output{font:10px ui-monospace,monospace;color:#a4ddd8}input[type=range]{appearance:none;display:block;width:100%;height:3px;background:#2d3e48;border-radius:5px;margin:15px 0 12px;accent-color:var(--cyan)}input[type=range]::-webkit-slider-thumb{appearance:none;width:10px;height:10px;border-radius:50%;background:#8adfd7;box-shadow:0 0 0 4px #64d9d00d}.range-hints{display:flex;justify-content:space-between;color:#5e707c;font-size:8px;margin-bottom:16px}.toggle{position:relative;display:flex;align-items:center;justify-content:space-between;gap:8px;min-height:33px;font-size:10px;color:#aab7bf;cursor:pointer}.toggle input{position:absolute;right:0;opacity:0;width:30px;height:25px}.switch{width:26px;height:14px;border-radius:20px;border:1px solid #46545e;position:relative;background:#26343e;transition:.2s;pointer-events:none}.switch:before{content:"";position:absolute;top:3px;left:3px;width:6px;height:6px;border-radius:50%;background:#8799a5;transition:.2s}.toggle input:checked+.switch{background:#285250;border-color:#49877e}.toggle input:checked+.switch:before{left:15px;background:#8de4d8}.toggle input:focus-visible+.switch{outline:2px solid var(--cyan);outline-offset:4px}.mini-metric{display:flex;justify-content:space-between;font-size:9px;color:#647c88;margin-top:15px}.mini-metric strong{color:#9fb6bd;font-weight:400}.select-label{font-size:9px;color:#7f959f;display:block;margin:13px 0 7px}select{width:100%;border:1px solid #2b3b45;border-radius:5px;background:#18242d;color:#bbccd3;padding:9px 8px;font-size:10px;margin-bottom:9px}.action-grid{display:grid;grid-template-columns:1fr 1fr;gap:7px}.action-grid button,.full-button{background:#1a2a33;border:1px solid #2e424d;border-radius:5px;color:#b7cdd3;padding:10px 8px;text-align:left;font-size:9px;transition:.2s}.action-grid button span{float:right;color:var(--cyan)}button:hover{background:#28424b;border-color:#4f7c7a}.full-button{width:100%;margin-top:7px;padding:9px 10px;display:flex;align-items:center;justify-content:space-between;background:#15212a}.physics-note{font-size:8px;line-height:1.8;color:#5d7885;margin:13px 0 0}.notice{font:8px/1.8 ui-monospace,monospace;color:#596b76;margin-top:0}.telemetry{min-height:87px;display:flex;align-items:center;justify-content:space-around;gap:18px;border-top:1px solid var(--border);padding:16px 28px}.telemetry>div{display:flex;flex-direction:column;gap:8px}.telemetry span{font:8px ui-monospace,monospace;letter-spacing:1px;color:#607580}.telemetry strong{font:13px ui-monospace,monospace;color:#b5c5cd;font-weight:400}.telemetry .fps-stat{display:flex;flex-direction:row;gap:7px;align-items:baseline;border-right:1px solid var(--border);padding-right:28px}.fps-stat strong{font-size:27px;color:var(--cyan)}.error-overlay{position:fixed;inset:0;background:#070d13dc;display:grid;place-items:center;z-index:10;padding:30px}.error-overlay[hidden]{display:none}.error-overlay>div{max-width:650px;background:#14212b;border:1px solid #526871;border-radius:15px;padding:30px}.error-overlay h2{font-weight:400}.error-overlay pre{white-space:pre-wrap;color:#efb899;font:11px/1.6 ui-monospace,monospace}.error-overlay p{font-size:12px;line-height:1.6;color:#adbfca}.error-overlay button{border:1px solid #5e928a;background:#25473f;color:#c9eee4;padding:9px 18px;border-radius:4px}@media(min-height:950px){.workspace{min-height:760px}}@media(max-width:1000px){.wordmark-sub{display:none}.workspace{grid-template-columns:minmax(0,1fr) 280px}.controls{padding:24px 18px}.topbar{padding:0 20px}.scene-title{left:24px}.scene-badges{left:24px}.telemetry{gap:12px;padding:16px}.telemetry>div:nth-last-child(2),.telemetry>div:nth-last-child(1){display:none}.help-strip{gap:8px}.help-strip span:last-child{display:none}}@media(max-width:700px){.topbar{height:65px;padding:0 15px}.wordmark{font-size:15px;gap:8px}.top-status{font-size:7px;letter-spacing:0}.version{display:none}.workspace{height:auto;display:block;min-height:0}.scene{height:68dvh;min-height:460px}.scene-title{top:24px;left:20px}.scene-title h1{font-size:34px}.scene-title p{font-size:9px}.scene-badges{top:218px;left:20px}.time-chip{right:16px;top:25px;padding:8px}.time-chip strong{font-size:15px}.sun-icon{font-size:20px}.controls{border-left:0;border-top:1px solid var(--border);overflow:visible;padding:25px}.control-group{padding:20px 0}.telemetry{flex-wrap:wrap;justify-content:flex-start;gap:22px}.telemetry>div{min-width:65px}.help-strip{font-size:8px}.scene-bottom{left:20px;bottom:65px}.vehicle-readout strong{font-size:24px}}
```

## src/city-main.js

```javascript
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
    Object.assign(this.renderer.settings, quality === 'performance' ? { scale: .6, gi: false, ssr: false } : quality === 'cinematic' ? { scale: 1, gi: true, ssr: true } : { scale: .8, gi: false, ssr: true });
    this.renderer.settings.fog = true; this.renderer.settings.exposure = 1.05;
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
```

## src/core.js

```javascript
import { clamp } from './math.js';

export class FixedStepClock {
  constructor({
    hz=120,maxSubsteps=12,maxFrame=.1
  } = {}) {
    if(hz<=0||maxSubsteps<1)throw new RangeError('Invalid clock configuration');
    this.dt=1/hz;
    this.maxSubsteps=maxSubsteps;
    this.maxFrame=maxFrame;
    this.accumulator=0;
    this.time=0;
    this.dropped=0;
  }
  advance(frameSeconds,step) {
    if(!Number.isFinite(frameSeconds)||frameSeconds<0)throw new RangeError('Invalid frame duration');
    const accepted=Math.min(frameSeconds,this.maxFrame);
    this.dropped+=frameSeconds-accepted;
    this.accumulator+=accepted;
    let n=0;
    while(this.accumulator+1e-12>=this.dt&&n<this.maxSubsteps){
      step(this.dt);
      this.accumulator-=this.dt;
      this.time+=this.dt;
      n++;
    }
    if(this.accumulator>=this.dt){
      const lost=Math.floor(this.accumulator/this.dt)*this.dt;
      this.dropped+=lost;
      this.accumulator-=lost;
    }
    return {
      steps:n,alpha:clamp(this.accumulator/this.dt,0,1)
    };
  }
}

export class ObjectPool {
  constructor(capacity,factory,reset=()=>{
  }) {
    if(!Number.isInteger(capacity)||capacity<1)throw new RangeError('Invalid pool capacity');
    this.capacity=capacity;
    this.values=Array.from({
      length:capacity
    },(_,i)=>factory(i));
    this.generations=new Float64Array(capacity);
    this.live=new Uint8Array(capacity);
    this.free=Array.from({
      length:capacity
    },(_,i)=>capacity-1-i);
    this.reset=reset;
    this.highWater=0;
  }
  acquire() {
    if(!this.free.length)throw new RangeError('Pool exhausted');
    const i=this.free.pop();
    this.live[i]=1;
    this.highWater=Math.max(this.highWater,this.capacity-this.free.length);
    return Object.freeze({
      index:i,generation:this.generations[i]
    });
  }
  get(h) {
    if(!h||h.index<0||h.index>=this.capacity||!this.live[h.index]||this.generations[h.index]!==h.generation)throw new Error('Stale pool handle');
    return this.values[h.index];
  }
  release(h) {
    const v=this.get(h);
    this.reset(v);
    this.live[h.index]=0;
    this.generations[h.index]++;
    this.free.push(h.index);
  }
  get used() {
    return this.capacity-this.free.length;
  }
}

export class FrameArena {
  constructor(bytes=1024*1024) {
    this.buffer=new ArrayBuffer(bytes);
    this.offset=0;
    this.highWater=0;
  }
  allocate(Type,count) {
    if(!Number.isInteger(count)||count<0)throw new RangeError('Invalid arena allocation');
    const alignment=Type.BYTES_PER_ELEMENT;
    const start=Math.ceil(this.offset/alignment)*alignment,end=start+alignment*count;
    if(end>this.buffer.byteLength)throw new RangeError('Frame arena exhausted');
    this.offset=end;
    this.highWater=Math.max(this.highWater,end);
    return new Type(this.buffer,start,count);
  }
  reset() {
    this.offset=0;
  }
}

export class EventBus {
  constructor(){
    this.listeners=new Map();
  }
  on(type,fn){
    if(!this.listeners.has(type))this.listeners.set(type,new Set());
    this.listeners.get(type).add(fn);
    return ()=>this.listeners.get(type)?.delete(fn);
  }
  emit(type,event){
    for(const fn of [...(this.listeners.get(type)||[])])fn(event);
  }
  clear(){
    this.listeners.clear();
  }
}
```

## src/deformable.js

```javascript
import { Vec3, clamp } from './math.js';
import { TriangleMeshCollider } from './spatial.js';

export class DeformableSurface {
  constructor({
    width=3,depth=3,resolution=10,origin=new Vec3(0,4,0),compliance=2e-7,mass=3,pinCorners=true
  } = {}){
    if(resolution<2||mass<=0||compliance<0)throw new RangeError('Invalid surface');
    this.n=resolution;
    this.count=(resolution+1)**2;
    this.positions=new Float32Array(this.count*3);
    this.previous=new Float32Array(this.count*3);
    this.velocity=new Float32Array(this.count*3);
    this.invMass=new Float32Array(this.count).fill(this.count/mass);
    this.edges=[];
    this.compliance=compliance;
    this.damping=1.2;
    this.iterations=10;
    for(let z=0;z<=resolution;z++)for(let x=0;x<=resolution;x++){
      const i=z*(resolution+1)+x;
      new Vec3(origin.x+width*(x/resolution-.5),origin.y,origin.z+depth*(z/resolution-.5)).toArray(this.positions,i*3);
    }
    if(pinCorners)for(const i of [0,resolution,resolution*(resolution+1),this.count-1])this.invMass[i]=0;
    const indices=[];
    for(let z=0;z<resolution;z++)for(let x=0;x<resolution;x++){
      const a=z*(resolution+1)+x,b=a+1,c=a+resolution+1,d=c+1;
      indices.push(a,c,b,b,c,d);
    }
    this.indices=new Uint32Array(indices);
    const edge=(a,b,stiffness=1)=>this.edges.push({
      a,b,rest:Vec3.from(this.positions,a*3).distance(Vec3.from(this.positions,b*3)),lambda:0,compliance:compliance*stiffness
    });
    for(let z=0;z<=resolution;z++)for(let x=0;x<=resolution;x++){
      const i=z*(resolution+1)+x;
      if(x<resolution)edge(i,i+1);
      if(z<resolution)edge(i,i+resolution+1);
      if(x<resolution&&z<resolution){
        edge(i,i+resolution+2);
        edge(i+1,i+resolution+1);
      }
      if(x+2<=resolution)edge(i,i+2,10);
      if(z+2<=resolution)edge(i,i+2*(resolution+1),10);
    }
    this.previous.set(this.positions);
    this.collider=new TriangleMeshCollider(this.positions,this.indices);
  }
  collideParticle(i,b){
    const p=Vec3.from(this.positions,i*3),skin=.025;
    let n,depth;
    if(b.shape==='sphere'){
      const d=Vec3.sub(p,b.position),length=d.length();
      depth=b.radius+skin-length;
      if(depth<=0)return;
      n=length>1e-8?d.scale(1/length):new Vec3(0,1,0);
    } else{
      const q=b.worldToLocal(p),h=b.half;
      if(Math.abs(q.x)>h.x+skin||Math.abs(q.y)>h.y+skin||Math.abs(q.z)>h.z+skin)return;
      const gaps=['x','y','z'].map(k=>({
        k,d:h[k]+skin-Math.abs(q[k])
      })).sort((a,b)=>a.d-b.d),g=gaps[0],l=new Vec3();
      l[g.k]=q[g.k]>=0?1:-1;
      n=b.rotation.rotate(l);
      depth=g.d;
    }
    const wp=this.invMass[i],wr=b.invMass,lambda=depth/(wp+wr),correction=n.clone().scale(wp*lambda);
    p.add(correction).toArray(this.positions,i*3);
    if(wr)b.position.addScaled(n,-wr*lambda);
  }
  step(dt,world){
    this.previous.set(this.positions);
    const g=world?.gravity||new Vec3(0,-9.81,0),rigidBefore=new Map((world?.bodies||[]).filter(b=>b.invMass).map(b=>[b,b.position.clone()]));
    for(let i=0;i<this.count;i++){
      if(!this.invMass[i])continue;
      const v=Vec3.from(this.velocity,i*3).addScaled(g,dt).scale(Math.exp(-this.damping*dt)),p=Vec3.from(this.positions,i*3).addScaled(v,dt);
      v.toArray(this.velocity,i*3);
      p.toArray(this.positions,i*3);
    }
    for(const e of this.edges)e.lambda=0;
    for(let it=0;it<this.iterations;it++){
      for(const e of this.edges){
        const a=Vec3.from(this.positions,e.a*3),b=Vec3.from(this.positions,e.b*3),n=Vec3.sub(a,b),length=n.length(),alpha=e.compliance/(dt*dt),sum=this.invMass[e.a]+this.invMass[e.b];
        if(length<1e-9||sum===0)continue;
        n.scale(1/length);
        const dl=(-(length-e.rest)-alpha*e.lambda)/(sum+alpha);
        e.lambda+=dl;
        a.addScaled(n,dl*this.invMass[e.a]).toArray(this.positions,e.a*3);
        b.addScaled(n,-dl*this.invMass[e.b]).toArray(this.positions,e.b*3);
      }
      for(let i=0;i<this.count;i++)if(this.invMass[i]){
        this.positions[i*3+1]=Math.max(.025,this.positions[i*3+1]);
        for(const b of world?.bodies||[])this.collideParticle(i,b);
      }
    }
    for(let i=0;i<this.count;i++){
      const v=Vec3.sub(Vec3.from(this.positions,i*3),Vec3.from(this.previous,i*3)).scale(1/dt);
      v.toArray(this.velocity,i*3);
    }
    for(const [b,before]of rigidBefore)b.velocity.addScaled(Vec3.sub(b.position,before),1/dt);
    this.collider.updateBounds();
    if(world)world.updateBroadphase();
  }
  dent(point,normal,impulse,radius=.7){
    const magnitude=clamp(impulse*.001,0,.35);
    for(let i=0;i<this.count;i++){
      if(!this.invMass[i])continue;
      const p=Vec3.from(this.positions,i*3),d=p.distance(point);
      if(d<radius)p.addScaled(normal,-magnitude*(1-d/radius)**2).toArray(this.positions,i*3);
    }
    this.collider.updateBounds();
  }
  normals(){
    const out=new Float32Array(this.positions.length);
    for(let i=0;i<this.indices.length;i+=3){
      const a=this.indices[i],b=this.indices[i+1],c=this.indices[i+2],n=Vec3.cross(Vec3.sub(Vec3.from(this.positions,b*3),Vec3.from(this.positions,a*3)),Vec3.sub(Vec3.from(this.positions,c*3),Vec3.from(this.positions,a*3)));
      for(const j of [a,b,c]){
        out[j*3]+=n.x;
        out[j*3+1]+=n.y;
        out[j*3+2]+=n.z;
      }
    }
    for(let i=0;i<this.count;i++)Vec3.from(out,i*3).normalize().toArray(out,i*3);
    return out;
  }
}
```

## src/engine.js

```javascript
import { Vec3, Quat, Mat4, AABB, clamp, lerp, smoothstep } from './math.js';
import { FixedStepClock, ObjectPool, FrameArena } from './core.js';
import { RigidBody, PhysicsWorld, Ragdoll } from './physics.js';
import { RaycastVehicle } from './vehicle.js';
import { DeformableSurface } from './deformable.js';
import { WetnessField } from './resources.js';
import { MeshData } from './mesh.js';
import { Renderer } from './renderer.js';

export class DayNightCycle {
  constructor(hour=16.4){
    this.hour=hour;
    this.hoursPerSecond=.035;
    this.running=false;
    this.direction=new Vec3();
    this.color=new Vec3();
    this.intensity=0;
    this.update(0);
  }
  update(dt){
    if(this.running)this.hour=(this.hour+dt*this.hoursPerSecond)%24;
    const theta=(this.hour-6)/24*Math.PI*2;
    this.direction.set(Math.cos(theta)*.8,Math.sin(theta),Math.cos(theta)*.6).normalize();
    const day=smoothstep(-.08,.16,this.direction.y),warm=1-smoothstep(0,.5,this.direction.y);
    this.color.set(1,lerp(.96,.55,warm),lerp(.9,.3,warm));
    this.intensity=day*5.5;
  }
}

export class OrbitCamera {
  constructor(){
    this.target=new Vec3(0,1.3,0);
    this.distance=24;
    this.yaw=.65;
    this.pitch=.39;
    this.fov=Math.PI/3;
    this.eye=new Vec3();
    this.view=Mat4.identity();
    this.vp=Mat4.identity();
    this.inverseVP=Mat4.identity();
  }
  update(aspect){
    this.pitch=clamp(this.pitch,.08,1.35);
    this.distance=clamp(this.distance,4,80);
    this.eye.set(this.target.x+Math.sin(this.yaw)*Math.cos(this.pitch)*this.distance,this.target.y+Math.sin(this.pitch)*this.distance,this.target.z+Math.cos(this.yaw)*Math.cos(this.pitch)*this.distance);
    this.view=Mat4.lookAt(this.eye,this.target);
    this.vp=Mat4.mul(Mat4.perspective(this.fov,aspect,.15,250),this.view);
    this.inverseVP=Mat4.invert(this.vp);
  }
}

export class Engine {
  constructor(canvas){
    this.canvas=canvas;
    this.renderer=new Renderer(canvas);
    this.camera=new OrbitCamera();
    this.clock=new FixedStepClock();
    this.world=new PhysicsWorld();
    this.sun=new DayNightCycle();
    this.wet=new WetnessField();
    this.objects=[];
    this.nextObject=1;
    this.ragdolls=[];
    this.keys=new Set();
    this.paused=false;
    this.contextLost=false;
    this.running=false;
    this.disposers=[];
    this.physicsMs=0;
    this.frames=0;
    this.arena=new FrameArena(64*1024);
    this.projectiles=new ObjectPool(24,()=>({
      body:null,object:null,remaining:0
    }),v=>{
      v.body=null;
      v.object=null;
      v.remaining=0;
    });
    this.projectileHandles=[];
    this.box=MeshData.box();
    this.spheres=[{
      mesh:MeshData.sphere(24,12),error:0
    },{
      mesh:MeshData.sphere(12,6),error:.07
    },{
      mesh:MeshData.sphere(6,3),error:.24
    }];
    this.smallSpheres=[{
      mesh:MeshData.sphere(12,6),error:0
    },{
      mesh:MeshData.sphere(6,3),error:.1
    }];
    this.createLab();
    this.bindInput();
    this.onLost=e=>{
      e.preventDefault();
      this.contextLost=true;
      this.onStatus?.('Graphics context lost. Waiting for the browser to restore it.');
    };
    this.onRestored=()=>{
      try{
        this.renderer.dispose();
        this.renderer=new Renderer(canvas);
        this.contextLost=false;
        this.onStatus?.('Graphics context restored.');
      }catch(error){
        this.onError?.(error);
      }
    };
    canvas.addEventListener('webglcontextlost',this.onLost);
    canvas.addEventListener('webglcontextrestored',this.onRestored);
  }
  object({
    body=null,position=new Vec3(),rotation=new Quat(),scale=new Vec3(1,1,1),offset=new Vec3(),material={
    },lods=null,doubleSided=false,castShadow=true
  } = {}){
    const o={
      id:this.nextObject++,body,position,rotation,scale,offset,material:{
        color:[.3,.3,.3],...material
      },lods:lods||[{
        mesh:this.box,error:0
      }],doubleSided,castShadow,static:!body||!body.invMass,model:Mat4.identity(),bounds:new AABB()
    };
    this.objects.push(o);
    return o;
  }
  bodyVisual(body,color=[.2,.34,.45],material={
  }){
    return this.object({
      body,scale:body.shape==='sphere'?new Vec3(body.radius,body.radius,body.radius):body.half.clone(),lods:body.shape==='sphere'?this.spheres:null,material:{
        color,roughness:.45,...material
      }
    });
  }
  createLab(){
    this.object({
      position:new Vec3(0,-.25,0),scale:new Vec3(45,.25,45),material:{
        color:[.22,.25,.28],roughness:.88,textured:true,uvScale:30
      }
    });
    for(const x of [-7,7])for(const z of [-7,7]){
      const p=this.world.add(new RigidBody({
        mass:0,position:new Vec3(x,1.5,z),half:new Vec3(.35,1.5,.35)
      }));
      this.bodyVisual(p,[.14,.18,.22],{
        roughness:.3,metallic:.35
      });
      this.object({
        position:new Vec3(x,3.05,z),scale:new Vec3(.42,.035,.42),material:{
          color:[.12,.5,.6],emissive:[.4,1.8,2.4],metallic:.2
        }
      });
    }
    for(let i=0;i<4;i++)this.object({
      position:new Vec3(-4.8+i*3.2,.018,0),scale:new Vec3(.018,.012,12),material:{
        color:[.33,.4,.41],roughness:.75
      },castShadow:false
    });
    for(let i=0;i<3;i++){
      const pedestal=this.world.add(new RigidBody({
        mass:0,position:new Vec3(-4.6+i*2.25,.28,-5),half:new Vec3(.85,.28,.85)
      }));
      this.bodyVisual(pedestal,[.08,.1,.13],{
        roughness:.7
      });
      const sphere=this.world.add(new RigidBody({
        mass:0,shape:'sphere',radius:.67,position:new Vec3(-4.6+i*2.25,1.25,-5)
      }));
      this.bodyVisual(sphere,i===1?[.66,.24,.07]:i===2?[.08,.37,.5]:[.58,.62,.67],{
        metallic:i===0?1:i===1?.9:.1,roughness:.08+i*.22
      });
    }
    for(let j=0;j<3;j++)for(let i=0;i<2;i++){
      const b=this.world.add(new RigidBody({
        position:new Vec3(-5+i*1.05,.55+j*1.02,3),half:new Vec3(.47,.47,.47),mass:8
      }));
      this.bodyVisual(b,j===2?[.58,.22,.07]:[.24,.3,.37],{
        roughness:.5,metallic:.12
      });
    }
    this.vehicle=new RaycastVehicle(this.world,{
      position:new Vec3(3,1,3)
    });
    this.bodyVisual(this.vehicle.body,[.015,.09,.15],{
      roughness:.18,metallic:.78
    });
    this.object({
      body:this.vehicle.body,offset:new Vec3(0,.43,.28),scale:new Vec3(.68,.26,.85),material:{
        color:[.025,.055,.08],roughness:.08,metallic:.65
      }
    });
    for(const x of [-.63,.63])this.object({
      body:this.vehicle.body,offset:new Vec3(x,.03,-1.805),scale:new Vec3(.15,.07,.015),material:{
        color:[.7,.6,.35],emissive:[2.5,2,.9],roughness:.15
      }
    });
    for(const w of this.vehicle.wheels){
      const o=this.object({
        body:this.vehicle.body,scale:new Vec3(.16,w.radius,w.radius),lods:this.smallSpheres,material:{
          color:[.025,.03,.035],roughness:.8
        }
      });
      o.wheel=w;
    }
    this.surface=new DeformableSurface({
      width:3.4,depth:2.6,resolution:10,origin:new Vec3(5,3,-3)
    });
    this.surfaceMesh=MeshData.surface(this.surface);
    this.object({
      lods:[{
        mesh:this.surfaceMesh,error:0
      }],material:{
        color:[.055,.4,.42],roughness:.42,metallic:.15
      },doubleSided:true
    });
    for(const x of [3.3,6.7])for(const z of [-4.3,-1.7])this.object({
      position:new Vec3(x,1.5,z),scale:new Vec3(.035,1.5,.035),material:{
        color:[.2,.25,.28],metallic:.8,roughness:.3
      }
    });
    const ball=this.world.add(new RigidBody({
      shape:'sphere',radius:.42,mass:4,position:new Vec3(5,5,-3),restitution:.25
    }));
    this.bodyVisual(ball,[.7,.2,.04],{
      roughness:.22,metallic:.3
    });
    this.world.afterStep.add(dt=>this.surface.step(dt,this.world));
  }
  spawnRagdoll(){
    if(this.ragdolls.length>=3){
      const r=this.ragdolls.shift(),ids=new Set(r.bodies.map(b=>b.id));
      this.objects=this.objects.filter(o=>{
        if(!ids.has(o.body?.id))return true;
        this.renderer.lod.remove(o.id);
        return false;
      });
      r.dispose();
    }
    const r=new Ragdoll(this.world,new Vec3(0,5.5,1));
    this.ragdolls.push(r);
    for(const b of r.bodies)this.bodyVisual(b,[.54,.18,.065],{
      roughness:.55,metallic:.05
    });
    return r;
  }
  launch(){
    if(this.projectiles.used>=this.projectiles.capacity)return;
    this.camera.update(this.renderer.width/Math.max(1,this.renderer.height));
    const h=this.projectiles.acquire(),p=this.projectiles.get(h),direction=Vec3.sub(this.camera.target,this.camera.eye).normalize();
    p.body=this.world.add(new RigidBody({
      shape:'sphere',radius:.3,mass:8,position:this.camera.eye.clone().addScaled(direction,1.2),restitution:.3
    }));
    p.body.velocity.copy(direction.scale(22));
    p.object=this.bodyVisual(p.body,[.7,.33,.05],{
      roughness:.2,metallic:.6
    });
    p.remaining=9;
    this.projectileHandles.push(h);
  }
  resetVehicle(){
    const b=this.vehicle.body;
    b.position.set(3,1,3);
    b.previousPosition.copy(b.position);
    b.rotation.copy(new Quat());
    b.previousRotation.copy(b.rotation);
    b.velocity.set(0,0,0);
    b.angularVelocity.set(0,0,0);
    for(const w of this.vehicle.wheels){
      w.omega=0;
      w.compression=0;
    }
    this.world.updateBroadphase();
  }
  bindInput(){
    const canvas=this.canvas;
    let drag=null;
    const listen=(target,event,fn,options)=>{
      target.addEventListener(event,fn,options);
      this.disposers.push(()=>target.removeEventListener(event,fn,options));
    };
    listen(canvas,'pointerdown',e=>{
      canvas.focus();
      canvas.setPointerCapture(e.pointerId);
      drag={
        x:e.clientX,y:e.clientY,pan:e.button===2||e.shiftKey
      };
    });
    listen(canvas,'pointermove',e=>{
      if(!drag)return;
      const dx=e.clientX-drag.x,dy=e.clientY-drag.y;
      drag.x=e.clientX;
      drag.y=e.clientY;
      if(drag.pan){
        this.camera.target.x-=dx*.015;
        this.camera.target.z+=dy*.015;
      }else{
        this.camera.yaw-=dx*.006;
        this.camera.pitch+=dy*.005;
      }
    });
    for(const event of ['pointerup','pointercancel','lostpointercapture'])listen(canvas,event,()=>drag=null);
    listen(canvas,'contextmenu',e=>e.preventDefault());
    listen(canvas,'wheel',e=>{
      e.preventDefault();
      this.camera.distance*=Math.exp(e.deltaY*.001);
    },{
      passive:false
    });
    listen(window,'keydown',e=>{
      if(e.target instanceof HTMLInputElement||e.target instanceof HTMLSelectElement)return;
      if(['ArrowUp','ArrowDown','ArrowLeft','ArrowRight','Space'].includes(e.code))e.preventDefault();
      this.keys.add(e.code);
      if(e.code==='KeyF'&&!e.repeat)this.launch();
      if(e.code==='KeyR'&&!e.repeat)this.resetVehicle();
    });
    listen(window,'keyup',e=>this.keys.delete(e.code));
    listen(window,'blur',()=>this.keys.clear());
    listen(document,'visibilitychange',()=>{
      this.keys.clear();
      this.lastTime=null;
    });
  }
  fixedStep(dt){
    const k=this.keys;
    this.vehicle.setControls({
      throttle:(k.has('KeyW')||k.has('ArrowUp')?1:0)-(k.has('KeyS')||k.has('ArrowDown')?1:0),brake:k.has('KeyB')?1:0,steer:(k.has('KeyA')||k.has('ArrowLeft')?1:0)-(k.has('KeyD')||k.has('ArrowRight')?1:0),handbrake:k.has('Space')?1:0
    });
    this.vehicle.wetness=this.wet.sample(this.vehicle.body.position.x,this.vehicle.body.position.z);
    this.world.step(dt);
    for(let i=this.projectileHandles.length-1;i>=0;i--){
      const h=this.projectileHandles[i],p=this.projectiles.get(h);
      p.remaining-=dt;
      if(p.remaining<=0){
        this.world.remove(p.body);
        this.objects=this.objects.filter(o=>o!==p.object);
        this.renderer.lod.remove(p.object.id);
        this.projectiles.release(h);
        this.projectileHandles.splice(i,1);
      }
    }
  }
  updateObjects(alpha){
    this.surfaceMesh.updateSurface();
    for(const o of this.objects){
      let position=o.position,rotation=o.rotation;
      if(o.body){
        const b=o.body;
        position=b.previousPosition.clone().addScaled(Vec3.sub(b.position,b.previousPosition),alpha);
        rotation=Quat.slerp(b.previousRotation,b.rotation,alpha);
        position.add(rotation.rotate(o.offset));
      }
      if(o.wheel){
        position=this.vehicle.wheelPosition(o.wheel);
        const steering=o.wheel.steering?this.vehicle.steer*.5:0;
        rotation=Quat.mul(this.vehicle.body.rotation,Quat.mul(Quat.axisAngle(new Vec3(0,1,0),steering),Quat.axisAngle(new Vec3(1,0,0),o.wheel.angle)));
      }
      o.model=Mat4.compose(position,rotation,o.scale);
      o.bounds=new AABB();
      const local=o.lods[0].mesh.surface?new AABB():o.lods[0].mesh.bounds;
      if(o.lods[0].mesh.surface){
        const p=o.lods[0].mesh.positions;
        for(let i=0;i<p.length;i+=3)o.bounds.include(Vec3.from(p,i));
      } else for(const x of [local.min.x,local.max.x])for(const y of [local.min.y,local.max.y])for(const z of [local.min.z,local.max.z])o.bounds.include(Mat4.point(o.model,new Vec3(x,y,z)));
    }
  }
  frame(milliseconds){
    if(!this.running)return;
    const seconds=this.lastTime==null?0:Math.max(0,(milliseconds-this.lastTime)/1000);
    this.lastTime=milliseconds;
    try{
      if(!this.contextLost){
        this.arena.reset();
        const before=performance.now();
        const result=this.paused?{
          alpha:1,steps:0
        }
        :this.clock.advance(seconds,dt=>this.fixedStep(dt));
        this.physicsMs=performance.now()-before;
        if(!this.paused){
          this.sun.update(Math.min(seconds,.1));
          this.wet.step(Math.min(seconds,.1),Math.max(0,this.sun.direction.y));
        }
        this.updateObjects(result.alpha);
        this.renderer.render(this.objects,this.camera,this.sun,this.wet,this.clock.time,this.arena);
        this.frames++;
        this.onFrame?.(this,seconds);
      }
    } catch(error){
      this.running=false;
      this.onError?.(error);
      return;
    }
    this.raf=requestAnimationFrame(t=>this.frame(t));
  }
  start(){
    if(this.running)return;
    this.running=true;
    this.lastTime=null;
    this.raf=requestAnimationFrame(t=>this.frame(t));
  }
  stop(){
    this.running=false;
    cancelAnimationFrame(this.raf);
  }
  selfTest(){
    this.updateObjects(1);
    this.renderer.render(this.objects,this.camera,this.sun,this.wet,this.clock.time);
    const bytes=this.renderer.readback();
    let min=255,max=0,sum=0;
    for(let i=0;i<bytes.length;i+=4){
      const v=bytes[i]+bytes[i+1]+bytes[i+2];
      min=Math.min(min,v);
      max=Math.max(max,v);
      sum+=v;
    }
    const error=this.renderer.gl.getError();
    return {
      glError:error,renderedPixels:bytes.length/4,range:[min,max],mean:sum/(bytes.length/4*3),finitePhysics:this.world.bodies.every(b=>b.position.finite()&&b.velocity.finite()),...this.renderer.stats
    };
  }
  dispose(){
    this.stop();
    for(const fn of this.disposers)fn();
    this.canvas.removeEventListener('webglcontextlost',this.onLost);
    this.canvas.removeEventListener('webglcontextrestored',this.onRestored);
    this.vehicle.dispose();
    for(const r of this.ragdolls)r.dispose();
    this.renderer.dispose();
    this.world.beforeStep.clear();
    this.world.afterStep.clear();
  }
}
```

## src/game/citizens.js

```javascript
import { Vec3, Quat, clamp } from '../math.js';
import { Random, hash, poiPosition, POIS, distanceXZ, dampAngle, terrainHeight } from '../world/util.js';
import { material } from '../world/geometry.js';

export const NAMED_CITIZENS = Object.freeze([
  { id: 'mira', name: 'Mira Patel', role: 'Community organizer', place: 'plaza', x: 34, z: 26, outfit: [.32, .16, .1], line: 'A city is made by the people who show up for each other.' },
  { id: 'leo', name: 'Leo Santos', role: 'Barista', place: 'cafe', x: 104, z: 38, outfit: [.1, .26, .22], line: 'The coffee is strong. The regulars are stronger.' },
  { id: 'anika', name: 'Dr. Anika Shah', role: 'Clinic physician', place: 'clinic', x: -56, z: 106, outfit: [.7, .74, .68], line: 'A small delivery can make a very big difference here.' },
  { id: 'tess', name: 'Tess Morgan', role: 'Dispatch coordinator', place: 'depot', x: 184, z: 106, outfit: [.2, .31, .4], line: 'A good courier is someone the neighborhood can count on.' },
  { id: 'sanjay', name: 'Sanjay Rai', role: 'Harbor electrician', place: 'harbor', x: 344, z: 106, outfit: [.6, .36, .09], line: 'Salt air gets into everything. Keeps me employed.' },
  { id: 'rosa', name: 'Rosa Bennett', role: 'Market owner', place: 'market', x: -216, z: 40, outfit: [.37, .16, .23], line: 'Westhaven has its own rhythm. Take the time to notice it.' },
  { id: 'eli', name: 'Eli Brooks', role: 'Gardener', place: 'park', x: -120, z: 196, outfit: [.24, .31, .13], line: 'A quiet place is a useful thing to have in a busy city.' },
  { id: 'juno', name: 'Juno Park', role: 'Photographer', place: 'waterfront', x: 410, z: 37, outfit: [.13, .22, .34], line: 'Watch what the harbor does when the sun gets low.' }
]);

export class CitizenSystem {
  constructor(graph, seed = 811) {
    this.graph = graph; this.agents = []; this.time = 0; this.planningBudget = 0;
    const random = new Random(seed), skins = [[.58, .36, .23], [.36, .21, .14], [.73, .52, .36], [.82, .65, .5]], outfits = [[.12, .22, .3], [.35, .19, .12], [.26, .29, .14], [.25, .17, .3], [.16, .32, .31], [.57, .44, .25]];
    const make = data => ({ ...data, position: new Vec3(data.x, terrainHeight(data.x, data.z) + .1, data.z), yaw: random.range(-Math.PI, Math.PI), speed: 0, walkSpeed: random.range(1, 1.45), phase: random.range(0, 6.28), skin: random.pick(skins), hair: random.pick([[.045, .035, .025], [.12, .065, .03], [.18, .16, .13]]), state: data.named ? 'Working' : 'Idle', path: [], pathIndex: 0, targetKey: '', destination: null, wait: random.range(1, 4), panic: 0, talking: false, relations: 0, thought: '', lastUpdate: 0 });
    for (const data of NAMED_CITIZENS) this.agents.push(make({ ...data, named: true }));
    const firstNames = ['Alex', 'Noor', 'Maya', 'Owen', 'Nadia', 'Ben', 'Sofia', 'Ishan', 'Grace', 'Luca', 'Ava', 'Amir', 'Sam', 'Hazel', 'Kai', 'Priya'];
    const lastNames = ['Chen', 'Wilson', 'Das', 'Rivera', 'Adams', 'Khan', 'Lee', 'Walker'];
    const jobs = ['Office worker', 'Shop assistant', 'Nurse', 'Courier', 'Dock worker', 'Student'];
    const destinations = ['cafe', 'clinic', 'depot', 'market', 'harbor', 'park'];
    const all = [...graph.nodes.values()].filter(n => n.cx >= -3 && n.cx <= 4 && n.cz >= -2 && n.cz <= 3);
    for (let i = 0; i < 72; i++) {
      const home = i < 12 ? graph.nodes.get(`0:0:${i % 8}`) : random.pick(all), job = random.pick(destinations), shift = i % 5 === 0 ? [18, 24] : [8, 17];
      const a = make({ id: `resident-${i}`, name: `${random.pick(firstNames)} ${random.pick(lastNames)}`, role: random.pick(jobs), place: job, x: home.position.x, z: home.position.z, outfit: random.pick(outfits), home: home.id, shift, named: false });
      a.leisure = random.pick(['park', 'plaza', 'waterfront', 'cafe']); a.localLoop = i < 12 ? i : null; this.agents.push(a);
    }
    this.visible = []; this.random = random;
  }
  get(id) { return this.agents.find(a => a.id === id); }
  setTalking(id, value) { const a = this.get(id); if (a) { a.talking = value; a.speed = 0; if (value) a.state = 'Talking'; } }
  panicAt(point, radius = 28) {
    for (const a of this.agents) if (distanceXZ(a.position, point) < radius) {
      a.panic = 7; a.targetKey = ''; a.path = []; a.fleeFrom = point.clone(); a.thought = 'Careful!';
    }
  }
  plan(a, target, key) {
    if (a.targetKey === key) return;
    if (this.planningBudget <= 0) return;
    this.planningBudget--; a.targetKey = key; a.destination = target.clone();
    a.path = this.graph.route(a.position, target); a.path.push(target.clone()); a.pathIndex = 0;
  }
  update(dt, minutes, weather, playerPosition, playerCar = null, traffic = []) {
    this.time += dt; this.planningBudget = 4; const hour = minutes / 60;
    const near = this.agents.map(a => ({ a, distance: distanceXZ(a.position, playerPosition) })).sort((a, b) => a.distance - b.distance);
    this.visible = near.filter(x => x.distance < 160).slice(0, 44).map(x => x.a);
    const active = new Set(this.visible);
    for (const a of this.agents) {
      if (!active.has(a) && this.time - a.lastUpdate < .6) continue;
      const step = Math.min(.7, Math.max(dt, this.time - a.lastUpdate)); a.lastUpdate = this.time;
      if (a.talking) { a.state = 'Talking'; a.speed = 0; a.yaw = dampAngle(a.yaw, Math.atan2(-(playerPosition.x - a.position.x), -(playerPosition.z - a.position.z)), step * 4); continue; }
      if (playerCar && playerCar.velocity.length() > 4 && distanceXZ(a.position, playerCar.position) < 7) { a.panic = Math.max(a.panic, 2); a.fleeFrom = playerCar.position.clone(); }
      if (active.has(a)) for (const car of traffic) if (car.speed > 3 && distanceXZ(car.position, a.position) < 3.5) { a.panic = Math.max(a.panic, 2); a.fleeFrom = car.position.clone(); }
      a.panic = Math.max(0, a.panic - step);
      if (a.panic > 0) {
        a.state = 'Fleeing'; const away = Vec3.sub(a.position, a.fleeFrom || playerPosition).normalize();
        const target = this.graph.nearest(a.position.clone().addScaled(away, 24)).position;
        this.plan(a, target, `flee:${Math.floor(this.time / 4)}`);
      } else if (a.named) { const open = { cafe: [7,22], clinic: [6,23], harbor: [6,22], market: [8,22], park: [6,20], plaza: [7,22] }[a.place] || [0,24]; a.state = weather.rain > .55 ? 'Sheltering' : hour >= open[0] && hour < open[1] ? 'Working' : 'Idle'; a.speed = 0; a.phase += step; continue; }
      else if (weather.rain > .55) { a.state = 'Sheltering'; const p = poiPosition(a.place); this.plan(a, p, `rain:${a.place}`); }
      else if (a.localLoop !== null && hour > 8 && hour < 21) { const i = (a.localLoop + Math.floor(this.time / 28) + 1) % 8; a.state = 'Walking'; this.plan(a, this.graph.nodes.get(`0:0:${i}`).position, `square:${i}`); }
      else if (hour >= a.shift[0] && hour < a.shift[1]) { a.state = 'Commuting'; this.plan(a, poiPosition(a.place), `work:${a.place}`); }
      else if (hour >= 17 && hour < 21) { a.state = 'Walking'; this.plan(a, poiPosition(a.leisure), `leisure:${a.leisure}`); }
      else { a.state = 'Walking'; this.plan(a, this.graph.nodes.get(a.home).position, `home:${a.home}`); }
      let target = a.path[a.pathIndex];
      while (target && distanceXZ(a.position, target) < .35) { a.pathIndex++; target = a.path[a.pathIndex]; }
      if (!target) { a.speed = 0; a.phase += step; a.state = a.panic > 0 ? 'Idle' : a.targetKey.startsWith('work:') ? 'Working' : a.targetKey.startsWith('rain:') ? 'Sheltering' : 'Idle'; continue; }
      const desired = Vec3.sub(target, a.position); desired.y = 0; const distance = desired.length(); desired.normalize();
      if (active.has(a) && a.state !== 'Fleeing') {
        const roadDistance = v => { const m = (v % 80 + 80) % 80; return Math.min(m, 80 - m); };
        const current = Math.min(roadDistance(a.position.x), roadDistance(a.position.z)), next = a.position.clone().addScaled(desired, 1);
        if (current > 7 && Math.min(roadDistance(next.x), roadDistance(next.z)) < current && traffic.some(car => car.speed > 2 && distanceXZ(car.position, a.position) < 7)) { a.state = 'Waiting'; a.speed = 0; continue; }
      }
      const separation = new Vec3();
      if (active.has(a)) for (const b of this.visible) {
        if (a === b) continue; const offset = Vec3.sub(a.position, b.position); offset.y = 0; const d = offset.length(); if (d > .01 && d < 1.05) separation.addScaled(offset, (1.05 - d) / d * 1.5);
      }
      const away = Vec3.sub(a.position, playerPosition); away.y = 0; const pd = away.length(); if (pd > .01 && pd < 1.0) separation.addScaled(away, (1 - pd) / pd * 2.5);
      const move = desired.clone().add(separation).normalize(), speed = a.walkSpeed * (a.state === 'Fleeing' ? 2.9 : 1);
      const amount = Math.min(distance, speed * step); a.position.addScaled(move, amount); a.position.y = terrainHeight(a.position.x, a.position.z) + .15;
      a.yaw = dampAngle(a.yaw, Math.atan2(-move.x, -move.z), step * 8); a.speed = amount / step; a.phase += amount * 5.7;
    }
  }
  nearest(position, radius = 3.2) { let best = null, distance = radius; for (const a of this.visible) { const d = distanceXZ(a.position, position); if (d < distance) { best = a; distance = d; } } return best; }
}

export function drawPerson(boxes, spheres, person, time = 0, player = false) {
  const p = person.position, root = Quat.axisAngle(new Vec3(0, 1, 0), person.yaw), moving = person.speed > .1, phase = person.phase || 0;
  const bob = moving ? Math.abs(Math.sin(phase)) * .035 : Math.sin(time * 1.2 + p.x) * .008;
  const outfit = person.outfit || [.65, .24, .075], skin = person.skin || [.58, .36, .23], pants = player ? [.055, .095, .14] : [.07, .09, .105];
  const part = (x, y, z, hx, hy, hz, color, local = new Quat(), surf = material(.85)) => {
    const position = root.rotate(new Vec3(x, y + bob, z)).add(p); boxes.add(position, Quat.mul(root, local), new Vec3(hx, hy, hz), color, surf);
  };
  part(0, 1.12, 0, .225, .29, .14, outfit); part(0, .77, 0, .2, .09, .13, pants);
  spheres.add(root.rotate(new Vec3(0, 1.66 + bob, 0)).add(p), root, new Vec3(.16, .2, .155), skin, material(.86));
  part(0, 1.81, .015, .158, .065, .15, person.hair || [.045, .035, .025]);
  part(-.06, 1.69, -.151, .018, .014, .013, [.02, .027, .025]); part(.06, 1.69, -.151, .018, .014, .013, [.02, .027, .025]);
  part(0, 1.63, -.168, .035, .045, .035, skin);
  for (const side of [-1, 1]) {
    const swing = moving ? Math.sin(phase + (side < 0 ? Math.PI : 0)) * .65 : 0;
    const leg = Quat.axisAngle(new Vec3(1, 0, 0), swing), knee = Quat.axisAngle(new Vec3(1, 0, 0), Math.max(0, -swing) * .8);
    const hip = new Vec3(side * .11, .75, 0), upper = leg.rotate(new Vec3(0, -.18, 0)).add(hip), footJoint = leg.rotate(new Vec3(0, -.36, 0)).add(hip), lowerRotation = Quat.mul(leg, knee), lower = lowerRotation.rotate(new Vec3(0, -.17, 0)).add(footJoint), shoe = lowerRotation.rotate(new Vec3(0, -.32, -.035)).add(footJoint);
    part(upper.x, upper.y, upper.z, .083, .18, .09, pants, leg); part(lower.x, lower.y, lower.z, .073, .17, .075, pants, lowerRotation); part(shoe.x, Math.max(.07, shoe.y), shoe.z, .085, .065, .135, [.025, .03, .028], lowerRotation);
    const working = person.state === 'Working' && !player, armAngle = moving ? -swing * .7 : working ? -.45 - Math.sin(time * 1.6 + side) * .12 : .05;
    const arm = Quat.axisAngle(new Vec3(1, 0, 0), armAngle), shoulder = new Vec3(side * .28, 1.32, 0), upperArm = arm.rotate(new Vec3(0, -.16, 0)).add(shoulder), hand = arm.rotate(new Vec3(0, -.41, -.015)).add(shoulder);
    part(upperArm.x, upperArm.y, upperArm.z, .07, .17, .075, outfit, arm); part(hand.x, hand.y + .08, hand.z, .06, .13, .065, skin, arm);
    if (working && side === 1) part(hand.x, hand.y - .02, hand.z - .07, .065, .09, .065, [.58, .53, .4], arm);
  }
  if (player) { part(0, 1.12, .2, .16, .22, .075, [.045, .14, .18]); part(0, 1.42, -.145, .13, .045, .015, [.75, .57, .3]); }
}
```

## src/game/city-simulation.js

```javascript
import { Vec3, Quat, Mat4, clamp } from '../math.js';
import { FixedStepClock, FrameArena } from '../core.js';
import { PhysicsWorld, RigidBody } from '../physics.js';
import { RaycastVehicle } from '../vehicle.js';
import { MeshData } from '../mesh.js';
import { DayNightCycle } from '../engine.js';
import { CityGenerator } from '../world/city.js';
import { ChunkStreamer } from '../world/streaming.js';
import { SidewalkGraph } from '../world/navigation.js';
import { WeatherSystem, MovingWetnessField } from '../world/weather.js';
import { GeometryBuilder, InstanceBatch, renderObject, material } from '../world/geometry.js';
import { DestructionSystem } from '../world/destruction.js';
import { POIS, poiById, poiPosition, terrainHeight, distanceXZ, districtAt, isOpen, WORLD_MIN, WORLD_MAX } from '../world/util.js';
import { CitizenSystem, drawPerson } from './citizens.js';
import { TrafficSystem, drawCar, trafficSignal } from './traffic.js';
import { CityPlayer, FollowCamera } from './player.js';
import { ProgressStore } from './progress.js';
import { MissionSystem, REPAIR_LAMPS } from './missions.js';
import { DialogueSystem } from './dialogue.js';

export class CitySimulation {
  constructor({ storage = null, notify = () => {}, changed = () => {}, release = () => {} } = {}) {
    this.notify = notify; this.changed = changed; this.release = release;
    this.store = new ProgressStore(storage); this.progress = this.store.load(); this.clock = new FixedStepClock({ hz: 120, maxSubsteps: 12 }); this.elapsed = 0; this.saveTimer = 0; this.routeTimer = 0; this.paused = false; this.keys = new Set(); this.hold = null; this.work = null; this.kickCooldown = 0; this.navTarget = null; this.route = [];
    this.world = new PhysicsWorld({ iterations: 10 }); this.world.groundHeight = terrainHeight;
    this.player = new CityPlayer(this.world, Vec3.from(this.progress.player));
    this.vehicle = new RaycastVehicle(this.world, { position: Vec3.from(this.progress.car) });
    this.vehicle.body.rotation.copy(Quat.axisAngle(new Vec3(0, 1, 0), Math.PI)); this.vehicle.body.previousRotation.copy(this.vehicle.body.rotation);
    this.vehicle.body.name = 'Your Aster GT'; this.sun = new DayNightCycle(this.progress.minutes / 60); this.weather = new WeatherSystem(); this.weather.restore(this.progress.weather); this.wet = new MovingWetnessField();
    this.camera = new FollowCamera(this.world, this.player, this.vehicle); this.camera.yaw = .2;
    this.graph = new SidewalkGraph(); this.citizens = new CitizenSystem(this.graph); this.traffic = new TrafficSystem();
    this.missions = new MissionSystem(this.store, { notify, onChange: () => { this.routeTimer = 0; this.changed('missions'); } });
    this.dialogue = new DialogueSystem(this.citizens, this.missions, this.store, { navigate: id => this.navigate(id), changed: data => this.changed('dialogue', data) });
    this.generator = new CityGenerator(); this.chunkObjects = new Map(); this.chunkBodies = new Map(); this.propBodies = new Map(); this.props = new Map(); this.lights = new Map();
    this.streamer = new ChunkStreamer(this.generator, { onResident: (c, previous) => this.installChunk(c, previous), onEvict: (c, replaced) => this.removeChunk(c, replaced) });
    this.destruction = new DestructionSystem(this.world, this.progress, { panic: p => this.citizens.panicAt(p), release: object => this.release(object) });
    this.boxMesh = MeshData.box(); this.sphereMesh = MeshData.sphere(10, 6); this.boxes = new InstanceBatch(this.boxMesh, 4096); this.spheres = new InstanceBatch(this.sphereMesh, 1024); this.arena = new FrameArena(128 * 1024);
    this.environment = this.buildEnvironment(); this.horizon = this.generator.createHorizon();
    this.streamer.update(this.player.position);
    // One small initial chunk is prepared before play; subsequent chunks are incremental.
    while (!this.streamer.resident.has(`${Math.floor(this.player.position.x / 80)}:${Math.floor(this.player.position.z / 80)}`) && this.streamer.pending.size) this.streamer.pump(12);
    this.world.updateBroadphase(); this.objects = []; this.interaction = null;
  }
  buildEnvironment() {
    const land = new GeometryBuilder(), sea = new GeometryBuilder(), boats = new GeometryBuilder();
    for (let z = -480; z < 560; z += 40) for (let x = -480; x < 436; x += 40) {
      const right = Math.min(x + 40, 436), point = (a, b) => new Vec3(a, terrainHeight(a, b) - .1, b);
      land.quad(point(x, z), point(x, z + 40), point(right, z + 40), point(right, z), z > 230 ? [.23, .29, .16] : [.24, .28, .22]);
    }
    for (let n = -480; n <= 560; n += 80) { if (n < 430) land.box(n, -.02, 40, 7, .009, 520, [.065, .077, .085]); land.box(-22, -.02, n, 458, .009, 7, [.065, .077, .085]); }
    sea.box(740, -1.05, 40, 305, .035, 700, [.025, .12, .16], material(.12, .55, 0, 0));
    for (const z of [-120, 65, 215]) {
      boats.box(484, -.42, z, 3.4, .55, 7, [.22, .28, .29], material(.35, .35)); boats.box(484, .45, z + 1, 2.2, .5, 3, [.63, .62, .52]); boats.box(484, 3.1, z, .09, 2.7, .09, [.35, .37, .33], material(.4, .7));
    }
    const water = renderObject(sea.build(), { castShadow: false }); water.water = true;
    return [renderObject(land.build(), { castShadow: false }), water, renderObject(boats.build(), { castShadow: false })];
  }
  installChunk(chunk) {
    const position = new Vec3(chunk.ox, 0, chunk.oz), object = renderObject(chunk.mesh, { position }), ground = renderObject(chunk.groundMesh, { position, castShadow: false });
    object.shadowMesh = chunk.shadowMesh; chunk.renderObjects = [object, ground]; chunk.physicsBodies = [];
    this.chunkObjects.set(chunk.id, chunk.renderObjects);
    for (const data of chunk.colliders) {
      const body = this.world.add(new RigidBody({ mass: 0, position: new Vec3(data.x, data.y, data.z), half: Vec3.from(data.half) })); body.name = data.id; chunk.physicsBodies.push(body);
    }
    if (chunk.cx === 0 && chunk.cz === 0) chunk.props.push({ id: 'plaza-glass', type: 'glass', x: 15, y: 0, z: 62, breakable: true });
    for (const prop of chunk.props) {
      this.props.set(prop.id, prop);
      if (!prop.breakable || this.progress.destroyed.includes(prop.id)) continue;
      const half = prop.type === 'crate' ? new Vec3(.5, .5, .5) : prop.type === 'glass' ? new Vec3(1.3, .8, .05) : new Vec3(.32, .52, .32), body = this.world.add(new RigidBody({ mass: 0, position: new Vec3(prop.x, prop.y + (prop.type === 'glass' ? 1.2 : half.y), prop.z), half }));
      body.propId = prop.id; body.chunkGeneration = chunk.generation; chunk.physicsBodies.push(body); this.propBodies.set(prop.id, body);
    }
    for (const light of chunk.lights) this.lights.set(light.id, light);
    this.chunkBodies.set(chunk.id, chunk.physicsBodies);
  }
  removeChunk(chunk, replaced) {
    for (const object of chunk.renderObjects || []) this.release(object);
    for (const body of chunk.physicsBodies || []) { this.world.remove(body); if (body.propId && this.propBodies.get(body.propId) === body) this.propBodies.delete(body.propId); }
    if (!replaced) { this.chunkObjects.delete(chunk.id); this.chunkBodies.delete(chunk.id); for (const p of chunk.props) this.props.delete(p.id); for (const l of chunk.lights) this.lights.delete(l.id); }
  }
  navigate(id) { const p = poiById(id); if (!p) return; this.navTarget = { position: poiPosition(id), label: p.name, id }; this.routeTimer = 0; this.notify(`Waypoint set · ${p.name}`); this.changed('map'); }
  navigatePosition(position) { this.navTarget = { position: position.clone(), label: 'Custom waypoint', id: null }; this.routeTimer = 0; this.changed('map'); }
  takeShuttle(id) {
    const destination = poiById(id);
    if (!destination) { this.notify('Select a named place on the map first.'); return false; }
    if (this.player.inVehicle || !POIS.some(p => distanceXZ(this.player.position, p) < 16)) { this.notify('Board the city shuttle near a named stop, on foot.'); return false; }
    if (this.progress.cash < 10) { this.notify('A city shuttle trip costs $10 in game money.'); return false; }
    this.progress.cash -= 10;
    const p = poiPosition(id); if (id === 'plaza') p.set(34, .1, 29); else p.y += .1;
    this.player.position.copy(p); this.player.verticalSpeed = 0; this.progress.minutes = (this.progress.minutes + 8) % 1440;
    this.dialogue.close(); this.navTarget = null; this.routeTimer = 0; this.camera.initialized = false; this.streamer.update(p); this.streamer.pump(12);
    this.missions.update(0, this.player.position, false); this.save(); this.notify(`City shuttle · ${destination.name}   −$10`); return true;
  }
  goal() { const objective = this.missions.tracked(); return this.navTarget || (objective ? { position: this.missions.objectivePosition(this.citizens), label: objective.step?.text || objective.definition.title, id: objective.step?.place } : null); }
  getInteraction() {
    if (this.player.inVehicle) return { kind: 'exit', label: 'Exit Aster GT', key: 'E' };
    const repair = this.missions.repairNear(this.player.position); if (repair) return { kind: 'repair', label: 'Repair streetlight · hold', target: repair.target, key: 'E' };
    const cafe = this.progress.missions.cafe;
    if (cafe?.status === 'active' && distanceXZ(this.player.position, poiPosition('cafe')) < 7) return { kind: 'work', label: this.missions.steps('cafe')[cafe.step]?.text || 'Café counter', key: 'E' };
    const citizen = this.citizens.nearest(this.player.position); if (citizen) return { kind: 'talk', label: `Talk to ${citizen.name}`, target: citizen.id, key: 'E' };
    if (distanceXZ(this.player.position, this.vehicle.body.position) < 4) return { kind: 'enter', label: 'Drive your Aster GT', key: 'E' };
    const poi = POIS.find(p => distanceXZ(this.player.position, p) < 3.8); if (poi) return { kind: 'place', label: `${poi.id === 'warehouse' ? 'Inspect' : 'Visit'} ${poi.name}`, target: poi.id, key: 'E' };
    return null;
  }
  interact() {
    if (this.dialogue.current) return;
    const interaction = this.getInteraction(); if (!interaction) return;
    if (interaction.kind === 'talk') this.dialogue.open(this.citizens.get(interaction.target), this.progress.minutes, this.weather);
    else if (interaction.kind === 'enter') { if (this.player.enter(this.vehicle)) { this.notify('Aster GT · WASD to drive, Space handbrake, E to exit.'); this.camera.distance = 7.5; } }
    else if (interaction.kind === 'exit') { if (!this.player.exit(this.vehicle)) this.notify('Slow down and find a clear space to get out.'); }
    else if (interaction.kind === 'repair') this.hold = { target: interaction.target, time: 0, duration: 2.2 };
    else if (interaction.kind === 'work') this.changed('work');
    else if (interaction.kind === 'place') { this.missions.event('interact', interaction.target); this.notify(poiById(interaction.target).description); }
  }
  startWork(action) {
    const state = this.progress.missions.cafe; if (this.work || state?.status !== 'active') return;
    if (distanceXZ(this.player.position, poiPosition('cafe')) > 7 || !isOpen(poiById('cafe'), this.progress.minutes / 60)) { this.notify('Go to Harbor Coffee while the café is open.'); return; }
    const step = this.missions.steps('cafe')[state.step]; if (step.target !== action) { this.notify(`Next, ${step.text.split('·')[1]?.trim() || step.target}`); return; }
    this.work = { action, duration: action === 'brew' ? 2.6 : 1.4, time: 0 }; this.changed('work');
  }
  destroy(prop) {
    if (!this.destruction.break(prop, this.vehicle.body.rotation.rotate(new Vec3(0, .4, -1)))) return;
    const body = this.propBodies.get(prop.id); if (body) { this.world.remove(body); this.propBodies.delete(prop.id); }
    this.notify(prop.type === 'glass' ? 'Glass shattered · nearby people step away.' : 'Street prop damaged · watch your surroundings.'); this.store.save();
  }
  kick() { if (this.kickCooldown || this.player.inVehicle) return; const prop = [...this.props.values()].find(p => p.breakable && !this.progress.destroyed.includes(p.id) && distanceXZ(this.player.position, p) < 2.7); if (prop) { this.destroy(prop); this.kickCooldown = .8; } }
  resetVehicle() {
    const p = this.player.position; const x = Math.round(p.x / 80) * 80 + 3.1, z = clamp(p.z + 8, -460, 540), b = this.vehicle.body;
    b.position.set(Math.min(403, x), terrainHeight(x, z) + .8, z); b.rotation.copy(new Quat()); b.velocity.set(0, 0, 0); b.angularVelocity.set(0, 0, 0); b.previousPosition.copy(b.position); b.previousRotation.copy(b.rotation);
    for (const w of this.vehicle.wheels) { w.omega = 0; w.compression = 0; } this.world.updateBroadphase(); this.notify('Your Aster GT is ready on the nearest road.');
  }
  fixedStep(dt, enabled) {
    const drive = this.player.inVehicle && enabled;
    const forward = drive && (this.keys.has('KeyW') || this.keys.has('ArrowUp')), backward = drive && (this.keys.has('KeyS') || this.keys.has('ArrowDown'));
    const heading = this.vehicle.body.rotation.rotate(new Vec3(0, 0, -1)), speed = this.vehicle.body.velocity.dot(heading);
    const reversingDirection = forward && this.vehicle.gear === 0 && speed < -.8 || backward && this.vehicle.gear !== 0 && speed > .8;
    this.vehicle.setControls({ throttle: reversingDirection ? 0 : forward ? 1 : backward ? -1 : 0, brake: !this.player.inVehicle || reversingDirection || drive && this.keys.has('KeyB') ? 1 : 0, steer: drive ? (this.keys.has('KeyA') || this.keys.has('ArrowLeft') ? 1 : 0) - (this.keys.has('KeyD') || this.keys.has('ArrowRight') ? 1 : 0) : 0, handbrake: !this.player.inVehicle || drive && this.keys.has('Space') ? 1 : 0 });
    this.vehicle.wetness = this.wet.sample(this.vehicle.body.position.x, this.vehicle.body.position.z);
    this.world.step(dt); this.player.update(dt, this.keys, this.camera.yaw, enabled && !this.work);
    if (this.player.inVehicle) this.player.position.copy(this.vehicle.body.position).add(new Vec3(0, -.6, 0));
    for (const contact of this.world.contacts) {
      const id = contact.a.propId || contact.b.propId; if (id && contact.jn > 180 && this.props.has(id)) this.destroy(this.props.get(id));
    }
  }
  update(dt) {
    this.streamer.update(this.player.position); this.streamer.pump(4);
    if (this.paused) { this.camera.tick(Math.min(dt, .05)); return; }
    dt = Math.min(.1, dt); this.elapsed += dt; this.kickCooldown = Math.max(0, this.kickCooldown - dt);
    this.progress.minutes = (this.progress.minutes + dt * .55) % 1440; this.weather.update(dt); this.sun.hour = this.progress.minutes / 60; this.sun.update(0); this.sun.intensity *= 1 - this.weather.clouds * .42; this.sun.clouds = this.weather.clouds; this.sun.flash = this.weather.flash * .7;
    this.wet.update(dt, this.player.position, this.weather.rain, Math.max(0, this.sun.direction.y));
    const enabled = !this.dialogue.current; this.clock.advance(dt, h => this.fixedStep(h, enabled));
    this.citizens.update(dt, this.progress.minutes, this.weather, this.player.position, this.vehicle.body, this.traffic.visible); this.traffic.update(dt, this.player.position, this.vehicle.body, this.citizens.visible);
    this.destruction.update(dt); this.missions.update(dt, this.player.position, enabled);
    if (this.hold) {
      const step = this.missions.repairNear(this.player.position);
      if (!this.keys.has('KeyE') || !step || step.target !== this.hold.target) this.hold = null;
      else { this.hold.time += dt; if (this.hold.time >= this.hold.duration) { this.missions.event('repair', this.hold.target); this.hold = null; } }
    }
    if (this.work) {
      if (distanceXZ(this.player.position, poiPosition('cafe')) > 8) { this.work = null; this.notify('Return to the counter to continue your order.'); }
      else { this.work.time += dt; if (this.work.time >= this.work.duration) { this.missions.event('work', this.work.action, { place: 'cafe' }); this.work = null; this.changed('work'); } }
    }
    if (this.vehicle.body.position.y < -2 || this.vehicle.body.position.x > 432 || Math.abs(this.vehicle.body.position.x) > 470 || this.vehicle.body.position.z < -470 || this.vehicle.body.position.z > 550) this.resetVehicle();
    this.interaction = this.getInteraction(); this.camera.tick(dt);
    this.routeTimer -= dt; const goal = this.goal();
    if (this.navTarget && distanceXZ(this.player.position, this.navTarget.position) < 5) { this.notify(`Arrived · ${this.navTarget.label}`); this.navTarget = null; this.routeTimer = 0; }
    if (this.routeTimer <= 0 && goal?.position) {
      const from = this.player.position.clone().add(new Vec3(0, 1, 0)), to = goal.position.clone().add(new Vec3(0, 1, 0)), d = Vec3.sub(to, from), length = d.length();
      const clear = length < 45 && !this.world.raycast(from, d.normalize(), Math.max(0, length - .2), this.vehicle.body);
      this.route = clear ? [this.player.position.clone(), goal.position.clone()] : [this.player.position.clone(), ...this.graph.route(this.player.position, goal.position), goal.position.clone()]; this.routeTimer = 1.2;
    }
    this.saveTimer += dt; if (this.saveTimer > 10) { this.save(); this.saveTimer = 0; }
  }
  save() { this.progress.player = this.player.position.toArray(); this.progress.car = this.vehicle.body.position.toArray(); this.progress.weather = this.weather.snapshot(); const saved = this.store.save(); this.changed('saved', saved); return saved; }
  buildRenderObjects() {
    const b = this.boxes, s = this.spheres; b.reset(); s.reset();
    if (!this.player.inVehicle) drawPerson(b, s, this.player, this.elapsed, true);
    for (const a of this.citizens.visible) drawPerson(b, s, a, this.elapsed);
    const carBody = this.vehicle.body, car = { position: carBody.position, rotation: carBody.rotation, color: [.018, .12, .17], brake: this.vehicle.brake > .1, wheel: 0 };
    const wheels = this.vehicle.wheels.map(w => ({ position: this.vehicle.wheelPosition(w), rotation: Quat.mul(carBody.rotation, Quat.axisAngle(new Vec3(1, 0, 0), w.angle)) }));
    drawCar(b, s, car, { wheels, headlights: this.sun.direction.y < .2 });
    for (const [i, traffic] of this.traffic.visible.entries()) drawCar(b, s, traffic, { taxi: i % 7 === 0, headlights: this.sun.direction.y < .2 });
    const night = clamp((.15 - this.sun.direction.y) / .28, 0, 1), damaged = new Set(REPAIR_LAMPS.filter(l => !this.progress.repaired.includes(l.id)).map(l => l.id));
    for (const prop of this.props.values()) {
      if (distanceXZ(prop, this.player.position) > 180) continue;
      const { x, y, z } = prop;
      if (prop.type === 'lamp') {
        b.box(x, y + 2.9, z, .065, 2.9, .065, [.15, .19, .19], material(.4, .7)); b.box(x + .55, y + 5.7, z, .6, .06, .06, [.17, .2, .19], material(.4, .7));
        b.box(x + 1, y + 5.62, z, .3, .055, .16, damaged.has(prop.id) ? [.17, .15, .1] : [.8, .58, .28], material(.4, .1, damaged.has(prop.id) ? 0 : night * 3));
      } else if (prop.type === 'glass') {
        b.box(x - 1.4, y + 1.25, z, .055, 1.25, .06, [.15, .19, .19], material(.5, .7)); b.box(x + 1.4, y + 1.25, z, .055, 1.25, .06, [.15, .19, .19], material(.5, .7)); b.box(x, y + 2.5, z, 1.45, .06, .6, [.18, .24, .25]);
        if (!this.progress.destroyed.includes(prop.id)) b.box(x, y + 1.2, z, 1.3, .8, .035, [.08, .2, .22], material(.1, .65));
      } else if (!this.progress.destroyed.includes(prop.id)) {
        const crate = prop.type === 'crate'; b.box(x, y + (crate ? .5 : .52), z, crate ? .5 : .31, crate ? .5 : .52, crate ? .5 : .31, crate ? [.37, .24, .12] : [.1, .17, .15], material(.85));
        if (crate) for (const d of [-.3, .3]) b.box(x + d, y + .51, z, .035, .52, .515, [.22, .15, .09]); else b.box(x, y + 1.05, z, .34, .045, .34, [.15, .2, .18], material(.5, .4));
      }
    }
    for (let ix = Math.floor(this.player.position.x / 80) - 1; ix <= Math.floor(this.player.position.x / 80) + 1; ix++) for (let iz = Math.floor(this.player.position.z / 80) - 1; iz <= Math.floor(this.player.position.z / 80) + 1; iz++) {
      const x = ix * 80 + 8, z = iz * 80 + 8, axis = 'x', color = trafficSignal(ix * 80, iz * 80, this.traffic.elapsed, axis);
      b.box(x, 1.9, z, .065, 1.9, .065, [.12, .15, .15], material(.4, .7)); b.box(x, 3.9, z, .14, .4, .15, [.045, .055, .05]);
      for (const [i, c] of ['red', 'amber', 'green'].entries()) s.box(x, 4.12 - i * .22, z - .15, .075, .075, .055, c === 'red' ? [.55, .025, .015] : c === 'amber' ? [.75, .4, .035] : [.04, .48, .2], material(.4, 0, c === color ? 2.7 : .02));
    }
    const pieces = [];
    for (const piece of this.destruction.debris) {
      if (piece.object) { piece.object.model = Mat4.compose(piece.body.position, piece.body.rotation); piece.object.bounds = piece.body.bounds.clone().expand(.8); pieces.push(piece.object); }
      else b.add(piece.body.position, piece.body.rotation, piece.body.half, piece.color, material(.85));
    }
    this.sun.lights = [...this.lights.values()].filter(l => !damaged.has(l.id) && night > .02).sort((a, b) => distanceXZ(a, this.player.position) - distanceXZ(b, this.player.position)).slice(0, 10).map(l => ({ ...l, power: l.power * night }));
    if (night > .1) for (const side of [-.6, .6]) { const p = carBody.localToWorld(new Vec3(side, .1, -2)); this.sun.lights.push({ x: p.x, y: p.y, z: p.z, radius: 14, power: 17, color: [1, .9, .68] }); }
    this.objects = [...this.environment, this.horizon, ...[...this.chunkObjects.values()].flat(), b.finish(), s.finish(), ...pieces]; return this.objects;
  }
  dispose() { this.save(); this.dialogue.close(); this.streamer.dispose(); this.destruction.dispose(); this.vehicle.dispose(); }
}
```

## src/game/city-ui.js

```javascript
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
    $('enter-city').addEventListener('click', () => { this.started = true; this.sim.paused = false; $('welcome').classList.add('departing'); setTimeout(() => $('welcome').hidden = true, 650); game.canvas.focus(); this.toast('Welcome to Port Meridian. Mira is nearby in Founders Square.'); });
    for (const [id, name] of [['map-button', 'map'], ['journal-button', 'journal'], ['menu-button', 'menu']]) $(id).addEventListener('click', () => this.toggle(name));
    for (const button of document.querySelectorAll('[data-close]')) button.addEventListener('click', () => this.close());
    $('continue-game').addEventListener('click', () => this.close()); $('save-game').addEventListener('click', () => this.toast(this.sim.save() ? 'Your visit has been saved on this browser.' : 'Saving is unavailable in this browser. Your current session continues.'));
    $('recover-car').addEventListener('click', () => { this.sim.resetVehicle(); this.close(); });
    $('quality').value = this.sim.progress.settings.quality;
    $('quality').addEventListener('change', e => { this.sim.progress.settings.quality = e.target.value; this.game.applyQuality(); this.sim.save(); });
    $('voice-toggle').checked = this.sim.progress.settings.sound;
    $('voice-toggle').addEventListener('change', e => { this.sim.progress.settings.sound = e.target.checked; if (!e.target.checked) window.speechSynthesis?.cancel(); this.sim.save(); });
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
    $('dialogue').hidden = !data; if (!data) { window.speechSynthesis?.cancel(); return; }
    $('speaker-name').textContent = data.agent.name; $('speaker-role').textContent = data.agent.role; $('speaker-text').textContent = data.text;
    const color = data.agent.outfit.map(x => Math.round(Math.pow(x, .6) * 220)); $('speaker-avatar').style.background = `linear-gradient(145deg,rgb(${color.join(',')}),#172b32)`; $('speaker-avatar').textContent = data.agent.name.split(' ').map(n => n[0]).slice(0, 2).join('');
    $('dialogue-choices').innerHTML = data.options.map((o, i) => `<button data-choice="${o.id}" ${o.disabled ? 'disabled' : ''}><kbd>${i + 1}</kbd><span>${escape(o.text)}</span>${o.disabled ? '<small>4 REP</small>' : '<span>↗</span>'}</button>`).join('');
    if (this.sim.progress.settings.sound && window.speechSynthesis && window.SpeechSynthesisUtterance) { window.speechSynthesis.cancel(); const utterance = new SpeechSynthesisUtterance(data.text); utterance.rate = .98; utterance.lang = 'en-US'; window.speechSynthesis.speak(utterance); }
  }
  chooseNumber(number) { const option = this.sim.dialogue.current?.options[number - 1]; if (option) this.sim.dialogue.choose(option.id); }
  renderJournal() {
    $('mission-list').innerHTML = MISSION_DEFINITIONS.map(def => {
      const state = this.sim.progress.missions[def.id], locked = !this.sim.missions.unlocked(def.id), completed = state?.status === 'completed' && !def.repeatable, active = state?.status === 'active', step = active ? this.sim.missions.steps(def.id)[state.step]?.text : def.description;
      return `<article class="mission-card ${active ? 'active' : ''}"><div class="mission-meta"><span>${def.category}</span><span>${completed ? 'COMPLETED ✓' : locked ? 'LOCKED' : active ? 'IN PROGRESS' : def.repeatable ? 'REPEATABLE' : 'AVAILABLE'}</span></div><h3>${escape(def.title)}</h3><p>${escape(locked ? def.requirement : step || def.description)}</p><div class="mission-footer"><span>$${def.reward} <small>+ ${def.reputation} REP</small></span><button data-mission="${def.id}" ${locked || completed ? 'disabled' : ''}>${completed ? 'Complete' : active ? this.sim.progress.tracked === def.id ? 'Tracking ✓' : 'Track' : 'Start'} ↗</button></div></article>`;
    }).join('');
    $('journal-money').textContent = `$${Math.floor(this.sim.progress.cash).toLocaleString()} · ${this.sim.progress.reputation} reputation`;
  }
  renderMap() { const canvas = $('world-map'), rect = canvas.getBoundingClientRect(); canvas.width = Math.max(500, Math.round(rect.width * Math.min(devicePixelRatio || 1, 1.5))); canvas.height = Math.max(440, Math.round(rect.height * Math.min(devicePixelRatio || 1, 1.5))); this.fullMapping = drawCityMap(this.map, canvas.width, canvas.height, this.sim, { full: true }); }
  update(dt) {
    this.timer += dt; if (this.timer < .08) return; this.timer = 0;
    const sim = this.sim, p = sim.player.position, place = districtAt(p.x, p.z), active = sim.missions.tracked(), goal = sim.goal();
    $('district-name').textContent = place.name; $('city-clock').textContent = clockText(sim.progress.minutes); $('weather-name').textContent = sim.weather.label;
    $('cash').textContent = `$${Math.floor(sim.progress.cash).toLocaleString()}`; $('reputation').textContent = sim.progress.reputation; $('mode-label').textContent = sim.player.inVehicle ? 'ASTER GT' : 'ON FOOT';
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
```

## src/game/dialogue.js

```javascript
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
```

## src/game/map-renderer.js

```javascript
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
```

## src/game/missions.js

```javascript
import { Vec3 } from '../math.js';
import { poiPosition, POIS, terrainHeight, distanceXZ } from '../world/util.js';

const talk = (target, text, place) => ({ kind: 'talk', target, text, place });
const place = (target, text) => ({ kind: 'place', target, text, place: target });
export const REPAIR_LAMPS = Object.freeze([
  { id: '4:1:lamp:0', x: 334, z: 91 }, { id: '4:0:lamp:1', x: 386, z: 69 }, { id: '3:1:lamp:0', x: 254, z: 91 }
]);

export const MISSION_DEFINITIONS = Object.freeze([
  { id: 'arrival', title: 'A place to begin', category: 'STORY', description: 'Meet the people who keep Old Meridian moving. A small favor is a good first step.', reward: 120, reputation: 4, giver: 'mira', steps: [talk('mira', 'Introduce yourself to Mira in Founders Square.', 'plaza'), { kind: 'reach', target: 'cafe', place: 'cafe', text: 'Walk to Harbor Coffee.' }, talk('leo', 'Say hello to Leo at the café.', 'cafe'), talk('mira', 'Return to Mira with news from the neighborhood.', 'plaza')] },
  { id: 'courier', title: 'The neighborhood route', category: 'WORK', description: 'Collect a parcel at Eastside Dispatch and deliver it safely. Routes change between shifts.', reward: 95, reputation: 2, giver: 'tess', repeatable: true },
  { id: 'cafe', title: 'The afternoon rush', category: 'WORK', description: 'Work a three-order café shift: brew, pack, and serve each order before the shift ends.', reward: 85, reputation: 2, giver: 'leo', repeatable: true },
  { id: 'voices', title: 'Neighborhood voices', category: 'STORY', description: 'Listen to Rosa, Eli, and Juno. What happened to the harbor lights?', reward: 150, reputation: 4, giver: 'mira', steps: [{ kind: 'voices', targets: ['rosa', 'eli', 'juno'], place: 'market', text: 'Speak with Rosa, Eli, and Juno about the harbor.' }, talk('mira', 'Tell Mira what you learned.', 'plaza')] },
  { id: 'repair', title: 'Keep the lights on', category: 'WORK', description: 'Collect Sanjay’s tools, repair three damaged streetlights, and restore the evening promenade.', reward: 180, reputation: 5, giver: 'sanjay' },
  { id: 'shipment', title: 'The missing manifest', category: 'STORY', description: 'Help Tess trace a misrouted clinic shipment through Warehouse 08.', reward: 250, reputation: 6, giver: 'tess', requirement: 'Build 4 reputation to earn Tess’s trust.', steps: [talk('tess', 'Ask Tess about the missing shipment.', 'depot'), place('warehouse', 'Inspect the shipping manifest at Warehouse 08.'), talk('sanjay', 'Ask Sanjay what arrived at the harbor.', 'harbor'), talk('tess', 'Bring the corrected manifest back to Tess.', 'depot')] },
  { id: 'lookout', title: 'The city from above', category: 'EXPLORATION', description: 'Take a break in Cedar Hills and frame a postcard of the city.', reward: 80, reputation: 1, giver: 'juno', steps: [{ kind: 'reach', target: 'lookout', place: 'lookout', text: 'Reach Cedar Hill Lookout.' }, { kind: 'photo', target: 'lookout', place: 'lookout', text: 'Press V at the lookout to frame a postcard.' }] }
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
    if (id === 'courier') { const target = ['clinic', 'market', 'harbor'][state?.variant || 0]; return [place('depot', 'Collect your parcel at Eastside Dispatch.'), place(target, `Deliver the parcel to ${POIS.find(p => p.id === target).name}.`)]; }
    if (id === 'cafe') return Array.from({ length: 9 }, (_, i) => ({ kind: 'work', target: ['brew', 'pack', 'serve'][i % 3], place: 'cafe', text: `Order ${Math.floor(i / 3) + 1} of 3 · ${['brew the coffee', 'pack the order', 'serve the customer'][i % 3]}.` }));
    if (id === 'repair') return [place('harbor', 'Collect Sanjay’s tool kit at the harbor workshop.'), ...REPAIR_LAMPS.map((lamp, i) => ({ kind: 'repair', target: lamp.id, place: 'harbor', position: new Vec3(lamp.x, terrainHeight(lamp.x, lamp.z), lamp.z), text: `Repair streetlight ${i + 1} of 3. Hold E beside the lamp.` })), talk('sanjay', 'Return the tool kit to Sanjay.', 'harbor')];
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
        if (!state.flags.includes(target)) { state.flags.push(target); this.notify(`Neighborhood voices · ${state.flags.length}/3`); progressed = true; }
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
    this.notify(`Completed · ${def.title}   +$${def.reward}   +${def.reputation} reputation`, 'success');
  }
  update(dt, playerPosition, canAct = true) {
    for (const [id, state] of Object.entries(this.progress.missions)) {
      if (state.status !== 'active') continue;
      const step = this.steps(id)[state.step]; if (!step) continue;
      if (step.kind === 'reach' && distanceXZ(playerPosition, poiPosition(step.target)) < 8) this.event('reach', step.target);
      if (id === 'cafe' && canAct) {
        state.remaining -= dt;
        if (state.remaining <= 0) { delete this.progress.missions.cafe; this.notify('The café shift ended. Talk to Leo to try another shift.'); this.store.save(); this.onChange(); }
      }
    }
  }
  repairNear(position) {
    const state = this.progress.missions.repair; if (state?.status !== 'active') return null;
    const step = this.steps('repair')[state.step]; return step?.kind === 'repair' && distanceXZ(position, step.position) < 2.8 ? step : null;
  }
}
```

## src/game/player.js

```javascript
import { Vec3, Quat, Mat4, AABB, clamp, lerp } from '../math.js';
import { terrainHeight, dampAngle, distanceXZ } from '../world/util.js';

export class CityPlayer {
  constructor(world, position = new Vec3(34, 0, 42)) {
    this.world = world; this.position = position.clone(); this.position.y = terrainHeight(position.x, position.z) + .1;
    this.yaw = Math.PI; this.speed = 0; this.phase = 0; this.verticalSpeed = 0; this.grounded = true; this.inVehicle = false; this.jumpLatch = false;
    this.outfit = [.66, .27, .09]; this.skin = [.58, .37, .24]; this.hair = [.045, .035, .025]; this.state = 'Idle';
  }
  moveAxis(delta, axis) {
    if (!delta) return;
    const desired = this.position.clone(); desired[axis] += delta;
    const bounds = new AABB(new Vec3(desired.x - .32, desired.y + .15, desired.z - .32), new Vec3(desired.x + .32, desired.y + 1.72, desired.z + .32));
    this.world.bvh.query(bounds, body => {
      if (body.shape !== 'box') return;
      const b = body.bounds;
      if (this.position.y >= b.max.y - .28 && b.max.y - this.position.y <= .38) { desired.y = Math.max(desired.y, b.max.y + .02); return; }
      if (b.max.y <= desired.y + .15 || b.min.y >= desired.y + 1.72) return;
      const low = b.min[axis] - .33, high = b.max[axis] + .33;
      const other = axis === 'x' ? 'z' : 'x';
      if (desired[other] <= b.min[other] - .29 || desired[other] >= b.max[other] + .29) return;
      if (desired[axis] > low && desired[axis] < high) desired[axis] = delta > 0 ? low : high;
    });
    this.position.copy(desired);
  }
  update(dt, keys, cameraYaw, enabled = true) {
    if (this.inVehicle) return;
    const input = enabled ? new Vec3((keys.has('KeyD') || keys.has('ArrowRight') ? 1 : 0) - (keys.has('KeyA') || keys.has('ArrowLeft') ? 1 : 0), 0, (keys.has('KeyS') || keys.has('ArrowDown') ? 1 : 0) - (keys.has('KeyW') || keys.has('ArrowUp') ? 1 : 0)) : new Vec3();
    input.normalize(); const direction = Quat.axisAngle(new Vec3(0, 1, 0), cameraYaw).rotate(input), sprint = keys.has('ShiftLeft') || keys.has('ShiftRight'), crouch = keys.has('KeyC');
    const desiredSpeed = input.lengthSq() ? crouch ? 1.25 : sprint ? 6.2 : 3 : 0;
    this.speed = lerp(this.speed, desiredSpeed, 1 - Math.exp(-dt * 12));
    this.moveAxis(direction.x * this.speed * dt, 'x'); this.moveAxis(direction.z * this.speed * dt, 'z');
    this.position.x = clamp(this.position.x, -468, 431); this.position.z = clamp(this.position.z, -468, 548);
    if (input.lengthSq()) this.yaw = dampAngle(this.yaw, Math.atan2(-direction.x, -direction.z), dt * 12);
    const jump = enabled && keys.has('Space');
    if (jump && !this.jumpLatch && this.grounded) { this.verticalSpeed = 4.4; this.grounded = false; }
    this.jumpLatch = jump;
    this.verticalSpeed -= 9.81 * dt; this.position.y += this.verticalSpeed * dt;
    let floor = terrainHeight(this.position.x, this.position.z) + .1;
    const support = new AABB(this.position.clone().add(new Vec3(-.25, -.45, -.25)), this.position.clone().add(new Vec3(.25, .38, .25)));
    if (this.verticalSpeed <= 0) this.world.bvh.query(support, body => { const b = body.bounds; if (b.max.y < this.position.y + .38 && b.max.y > floor && this.position.x > b.min.x - .1 && this.position.x < b.max.x + .1 && this.position.z > b.min.z - .1 && this.position.z < b.max.z + .1) floor = b.max.y + .02; });
    if (this.position.y <= floor) { this.position.y = floor; this.verticalSpeed = 0; this.grounded = true; }
    this.state = this.speed > .2 ? 'Walking' : 'Idle'; this.phase += this.speed * dt * 5;
  }
  enter(vehicle) { if (distanceXZ(this.position, vehicle.body.position) > 4.5 || vehicle.body.velocity.length() > 2) return false; this.inVehicle = true; return true; }
  exit(vehicle) {
    if (vehicle.body.velocity.length() > 4) return false;
    const body = vehicle.body, right = body.rotation.rotate(new Vec3(1, 0, 0));
    for (const side of [-1, 1]) {
      const p = body.position.clone().addScaled(right, side * 2.1); p.y = terrainHeight(p.x, p.z) + .1;
      const b = new AABB(p.clone().add(new Vec3(-.35, .1, -.35)), p.clone().add(new Vec3(.35, 1.8, .35))); let blocked = false;
      this.world.bvh.query(b, other => { if (other !== body && other.bounds.overlaps(b)) blocked = true; });
      if (!blocked) { this.position.copy(p); this.inVehicle = false; this.verticalSpeed = 0; return true; }
    }
    return false;
  }
}

export class FollowCamera {
  constructor(world, player, vehicle) {
    this.world = world; this.player = player; this.vehicle = vehicle; this.target = player.position.clone().add(new Vec3(0, 1.2, 0));
    this.eye = this.target.clone().add(new Vec3(7, 4, 10)); this.yaw = .35; this.pitch = .27; this.distance = 7.5; this.fov = Math.PI / 3;
    this.shadowExtent = 100; this.photo = false; this.view = Mat4.identity(); this.vp = Mat4.identity(); this.inverseVP = Mat4.identity(); this.initialized = false;
  }
  tick(dt) {
    const driving = this.player.inVehicle, focus = driving ? this.vehicle.body.position.clone().add(new Vec3(0, .65, 0)) : this.player.position.clone().add(new Vec3(0, 1.2, 0));
    const t = this.initialized ? 1 - Math.exp(-dt * 10) : 1; this.initialized = true;
    this.target.addScaled(Vec3.sub(focus, this.target), t);
    this.pitch = clamp(this.pitch, .06, 1.25); this.distance = clamp(this.distance, 3.5, this.photo ? 120 : 18);
    const d = this.distance + (driving ? 3 + Math.min(4, this.vehicle.body.velocity.length() * .12) : 0);
    const desired = this.target.clone().add(new Vec3(Math.sin(this.yaw) * Math.cos(this.pitch) * d, Math.sin(this.pitch) * d, Math.cos(this.yaw) * Math.cos(this.pitch) * d));
    const ray = Vec3.sub(desired, this.target), length = ray.length(); ray.normalize();
    const hit = this.photo ? null : this.world.raycast(this.target, ray, length, this.vehicle.body);
    if (hit) desired.copy(this.target).addScaled(ray, Math.max(1, hit.distance - .3));
    if (this.photo) this.world.bvh.query(new AABB(desired.clone().add(new Vec3(-.2,-.2,-.2)), desired.clone().add(new Vec3(.2,.2,.2))), body => { if(body !== this.vehicle.body && body.bounds.min.y < desired.y && body.bounds.max.y > desired.y) desired.y = body.bounds.max.y + 1; });
    desired.y = Math.max(terrainHeight(desired.x, desired.z) + .6, desired.y);
    this.eye.addScaled(Vec3.sub(desired, this.eye), 1 - Math.exp(-dt * 14));
    this.fov = lerp(this.fov, Math.PI / 3 + (driving ? Math.min(.18, this.vehicle.body.velocity.length() * .004) : 0), 1 - Math.exp(-dt * 4));
  }
  update(aspect) { this.view = Mat4.lookAt(this.eye, this.target); this.vp = Mat4.mul(Mat4.perspective(this.fov, aspect, .12, 1200), this.view); this.inverseVP = Mat4.invert(this.vp); }
  project(position, width, height) {
    const p = Mat4.point(this.vp, position), forward = Vec3.sub(position, this.eye).dot(Vec3.sub(this.target, this.eye));
    if (forward <= 0 || p.z > 1 || p.z < -1) return null;
    return { x: (p.x * .5 + .5) * width, y: (-p.y * .5 + .5) * height, depth: p.z };
  }
}
```

## src/game/progress.js

```javascript
import { clamp } from '../math.js';

export const SAVE_KEY = 'aurora.port-meridian.v2';
export function defaultProgress() {
  return { version: 2, cash: 250, reputation: 0, minutes: 16 * 60 + 20, missions: {}, tracked: 'arrival', receipts: [], conversations: {}, destroyed: [], repaired: [], jobCount: 0, courierCycle: 0, player: [34, 0, 29], car: [15, .75, 49], weather: null, settings: { quality: 'balanced', sound: false } };
}
const finiteVector = (value, fallback) => Array.isArray(value) && value.length === 3 && value.every(x => Number.isFinite(x) && Math.abs(x) < 2000) ? value : fallback;
export function validateProgress(data) {
  const result = defaultProgress(); if (!data || data.version !== 2) return result;
  result.cash = clamp(Number(data.cash) || 0, 0, 10000000); result.reputation = clamp(Number(data.reputation) || 0, 0, 10000);
  result.minutes = ((Number(data.minutes) || result.minutes) % 1440 + 1440) % 1440;
  result.player = finiteVector(data.player, result.player); result.car = finiteVector(data.car, result.car);
  for (const key of ['destroyed', 'repaired', 'receipts']) result[key] = Array.isArray(data[key]) ? [...new Set(data[key].filter(x => typeof x === 'string' && x.length < 150))].slice(-2000) : [];
  result.jobCount = clamp(Math.floor(Number(data.jobCount) || 0), 0, 100000); result.courierCycle = clamp(Math.floor(Number(data.courierCycle) || 0), 0, 100000);
  if (data.missions && typeof data.missions === 'object' && !Array.isArray(data.missions)) for (const [id, state] of Object.entries(data.missions)) {
    if (['__proto__','constructor','prototype'].includes(id) || id.length > 64 || !state || !['active', 'completed'].includes(state.status)) continue;
    result.missions[id] = { status: state.status, step: clamp(Math.floor(Number(state.step) || 0), 0, 20), flags: Array.isArray(state.flags) ? state.flags.filter(x => typeof x === 'string').slice(0, 30) : [], remaining: clamp(Number.isFinite(state.remaining) ? state.remaining : 150, 0, 300), variant: clamp(Math.floor(Number(state.variant) || 0), 0, 2) };
  }
  if (data.conversations && typeof data.conversations === 'object') for (const [id, n] of Object.entries(data.conversations).slice(0, 100)) result.conversations[id] = clamp(Number(n) || 0, 0, 10000);
  if (typeof data.tracked === 'string') result.tracked = data.tracked;
  if (data.settings) result.settings = { quality: ['performance', 'balanced', 'cinematic'].includes(data.settings.quality) ? data.settings.quality : 'balanced', sound: Boolean(data.settings.sound) };
  result.weather = data.weather; return result;
}

export class ProgressStore {
  constructor(storage = null) { this.storage = storage; this.state = defaultProgress(); this.available = !!storage; this.lastError = ''; }
  load() { try { const raw = this.storage?.getItem(SAVE_KEY); if (raw) this.state = validateProgress(JSON.parse(raw)); } catch (error) { this.lastError = error.message; } return this.state; }
  save() { try { if (!this.storage) return false; this.storage.setItem(SAVE_KEY, JSON.stringify(this.state)); this.available = true; return true; } catch (error) { this.available = false; this.lastError = error.message; return false; } }
  reward(receipt, cash, reputation = 0) {
    if (this.state.receipts.includes(receipt)) return false;
    this.state.receipts.push(receipt); this.state.cash += cash; this.state.reputation += reputation; this.save(); return true;
  }
  reset() { this.state = defaultProgress(); this.save(); return this.state; }
}
```

## src/game/traffic.js

```javascript
import { Vec3, Quat, clamp } from '../math.js';
import { CubicBezier } from '../world/navigation.js';
import { Random, distanceXZ } from '../world/util.js';
import { material } from '../world/geometry.js';

export function trafficSignal(x, z, seconds, axis = 'x') {
  const offset = ((Math.round(x / 80) * 3 + Math.round(z / 80) * 5) % 7 + 7) % 7;
  const phase = (seconds + offset) % 28;
  if (axis === 'x') return phase < 11 ? 'green' : phase < 13 ? 'amber' : 'red';
  return phase >= 14 && phase < 25 ? 'green' : phase >= 25 && phase < 27 ? 'amber' : 'red';
}

export class RoadLoop {
  constructor(x0, z0, x1, z1, reverse = false) {
    const lane = reverse ? -3.1 : 3.1;
    const corners = reverse ? [[x0 + lane, z0 + lane], [x0 + lane, z1 - lane], [x1 - lane, z1 - lane], [x1 - lane, z0 + lane]] : [[x0 + lane, z0 + lane], [x1 - lane, z0 + lane], [x1 - lane, z1 - lane], [x0 + lane, z1 - lane]];
    this.segments = []; this.starts = []; this.length = 0;
    for (let i = 0; i < 4; i++) {
      const prev = Vec3.from([corners[(i + 3) % 4][0], .72, corners[(i + 3) % 4][1]]), corner = Vec3.from([corners[i][0], .72, corners[i][1]]), next = Vec3.from([corners[(i + 1) % 4][0], .72, corners[(i + 1) % 4][1]]);
      const inDirection = Vec3.sub(corner, prev).normalize(), outDirection = Vec3.sub(next, corner).normalize(), entry = corner.clone().addScaled(inDirection, -5), exit = corner.clone().addScaled(outDirection, 5), nextEntry = next.clone().addScaled(outDirection, -5);
      const curve = new CubicBezier(entry, entry.clone().addScaled(inDirection, 2.76), exit.clone().addScaled(outDirection, -2.76), exit);
      const line = new CubicBezier(exit, exit.clone().addScaled(Vec3.sub(nextEntry, exit), 1 / 3), exit.clone().addScaled(Vec3.sub(nextEntry, exit), 2 / 3), nextEntry, 4);
      for (const segment of [curve, line]) { this.starts.push(this.length); this.segments.push(segment); this.length += segment.length; }
    }
  }
  sample(distance) {
    const d = ((distance % this.length) + this.length) % this.length; let i = this.segments.length - 1;
    for (let j = 0; j < this.starts.length - 1; j++) if (d < this.starts[j + 1]) { i = j; break; }
    const segment = this.segments[i], t = segment.parameterAtDistance(d - this.starts[i]);
    return { position: segment.at(t), direction: segment.tangent(t), turning: i % 2 === 0 };
  }
}

export class TrafficSystem {
  constructor(seed = 990) {
    this.random = new Random(seed); this.elapsed = 0; this.cars = [];
    this.routes = [[-160, -160, 160, 160], [-240, 0, 80, 240], [0, -80, 320, 160], [-80, -240, 240, 0]].flatMap(rect => [new RoadLoop(...rect), new RoadLoop(...rect, true)]);
    const colors = [[.08, .18, .23], [.55, .51, .41], [.26, .36, .26], [.48, .13, .07], [.12, .14, .16], [.6, .62, .56], [.2, .28, .35]];
    for (let i = 0; i < 32; i++) {
      const route = this.routes[i % this.routes.length], distance = route.length * ((Math.floor(i / 8) + .25 + (i % 3) * .08) / 4), sampled = route.sample(distance);
      this.cars.push({ id: `traffic-${i}`, route, distance, position: sampled.position, direction: sampled.direction, speed: this.random.range(3, 7), desiredSpeed: this.random.range(7, 11), color: this.random.pick(colors), brake: false, yaw: 0, wheel: 0 });
    }
    this.visible = [];
  }
  update(dt, player, playerCar = null, pedestrians = []) {
    this.elapsed += dt;
    for (const car of this.cars) {
      const sample = car.route.sample(car.distance); car.position = sample.position; car.direction = sample.direction;
      let target = car.desiredSpeed * (sample.turning ? .6 : 1);
      const axis = Math.abs(car.direction.x) > Math.abs(car.direction.z) ? 'x' : 'z', direction = car.direction[axis] >= 0 ? 1 : -1;
      const coordinate = car.position[axis], intersection = direction > 0 ? Math.ceil((coordinate + .001) / 80) * 80 : Math.floor((coordinate - .001) / 80) * 80;
      const distanceToLine = (intersection - coordinate) * direction - 10;
      const sx = axis === 'x' ? intersection : Math.round(car.position.x / 80) * 80, sz = axis === 'z' ? intersection : Math.round(car.position.z / 80) * 80;
      if (!sample.turning && distanceToLine > -1 && distanceToLine < 30 && trafficSignal(sx, sz, this.elapsed, axis) !== 'green') target = Math.min(target, Math.sqrt(2 * 4 * Math.max(0, distanceToLine)));
      for (const other of this.cars) {
        if (other === car) continue;
        const offset = Vec3.sub(other.position, car.position), ahead = offset.dot(car.direction), side = Math.abs(offset.x * car.direction.z - offset.z * car.direction.x);
        if (ahead > 0 && ahead < 24 && side < 2.5) target = Math.min(target, Math.sqrt(Math.max(0, ahead - 5) * 4));
      }
      for (const obstacle of [player, playerCar?.position, ...pedestrians.map(a => a.position)].filter(Boolean)) {
        const offset = Vec3.sub(obstacle, car.position), ahead = offset.dot(car.direction), side = Math.abs(offset.x * car.direction.z - offset.z * car.direction.x);
        if (ahead > -1 && ahead < 19 && side < 2.4) target = Math.min(target, Math.sqrt(Math.max(0, ahead - 4) * 4));
      }
      const before = car.speed; car.speed = clamp(car.speed + clamp(target - car.speed, -5 * dt, 2.1 * dt), 0, 14); car.brake = car.speed < before - .02 || car.speed < .25;
      car.distance = (car.distance + car.speed * dt) % car.route.length;
      const after = car.route.sample(car.distance); car.position = after.position; car.direction = after.direction; car.yaw = Math.atan2(-after.direction.x, -after.direction.z); car.wheel += car.speed * dt / .34;
    }
    this.visible = this.cars.filter(c => distanceXZ(c.position, player) < 220);
  }
}

export function drawCar(boxes, spheres, car, { taxi = false, wheels = null, headlights = false } = {}) {
  const position = car.position, rotation = car.rotation || Quat.axisAngle(new Vec3(0, 1, 0), car.yaw), color = car.color || [.025, .12, .17];
  const part = (x, y, z, hx, hy, hz, c, surface = material(.24, .65)) => boxes.add(rotation.rotate(new Vec3(x, y, z)).add(position), rotation, new Vec3(hx, hy, hz), c, surface);
  part(0, 0, 0, .86, .27, 1.8, color); part(0, .35, .14, .71, .26, .92, [.035, .075, .09], material(.12, .65));
  part(0, .63, .21, .7, .035, .79, color); part(0, .15, -1.29, .82, .09, .45, color);
  part(0, -.17, -1.81, .74, .05, .04, [.06, .08, .08], material(.35, .85));
  for (const x of [-.6, .6]) {
    part(x, .04, -1.815, .17, .065, .025, [.78, .66, .39], material(.18, .2, headlights ? 3 : .15));
    part(x, .04, 1.815, .17, .065, .025, [.55, .035, .02], material(.2, .2, car.brake ? 3 : .15));
    part(x * 1.48, .33, -.45, .08, .07, .15, color);
  }
  if (taxi) part(0, .78, .1, .29, .1, .16, [.85, .61, .18], material(.6, .1, .2));
  if (wheels) for (const w of wheels) spheres.add(w.position, w.rotation || rotation, new Vec3(.16, .34, .34), [.025, .03, .033], material(.9));
  else for (const x of [-.86, .86]) for (const z of [-1.23, 1.23]) {
    const r = Quat.mul(rotation, Quat.axisAngle(new Vec3(1, 0, 0), car.wheel || 0));
    spheres.add(rotation.rotate(new Vec3(x, -.29, z)).add(position), r, new Vec3(.17, .34, .34), [.025, .03, .033], material(.9));
  }
}
```

## src/main.js

```javascript
import { Engine } from './engine.js';
const $=id=>document.getElementById(id);
function fail(error){
  console.error(error);
  $('error').hidden=false;
  $('error-message').textContent=error.message;
  $('status').textContent='ENGINE STOPPED';
  document.body.classList.add('failed');
}
try{
  const engine=new Engine($('viewport'));
  window.engine=engine;
  engine.onError=fail;
  engine.onStatus=message=>$('notice').textContent=message;
  const set=(id,callback)=>$(id).addEventListener('input',event=>callback(event.target));
  set('hour',el=>{
    engine.sun.hour=Number(el.value);
    engine.sun.update(0);
  });
  set('cycle',el=>engine.sun.running=el.checked);
  set('rain',el=>{
    engine.wet.rain=Number(el.value);
    $('rain-value').textContent=`${Math.round(engine.wet.rain*100)}%`;
  });
  set('resolution',el=>engine.renderer.settings.scale=Number(el.value));
  set('exposure',el=>engine.renderer.settings.exposure=Number(el.value));
  set('shadows',el=>engine.renderer.settings.shadowMode=Number(el.value));
  set('debug',el=>engine.renderer.settings.debug=Number(el.value));
  for(const name of ['gi','fog','ssr'])set(name,el=>engine.renderer.settings[name]=el.checked);
  set('occlusion',el=>engine.renderer.enableOcclusion=el.checked);
  $('ragdoll').addEventListener('click',()=>engine.spawnRagdoll());
  $('launch').addEventListener('click',()=>engine.launch());
  $('reset-car').addEventListener('click',()=>engine.resetVehicle());
  $('pause').addEventListener('click',()=>{
    engine.paused=!engine.paused;
    $('pause').textContent=engine.paused?'Resume simulation':'Pause simulation';
    $('pause').setAttribute('aria-pressed',String(engine.paused));
  });
  $('reset-view').addEventListener('click',()=>{
    engine.camera.target.set(0,1.3,0);
    engine.camera.distance=24;
    engine.camera.yaw=.65;
    engine.camera.pitch=.39;
  });
  $('dismiss-error').addEventListener('click',()=>$('error').hidden=true);
  let frames=0,elapsed=0;
  engine.onFrame=(e,dt)=>{
    frames++;
    elapsed+=dt;
    if(elapsed<.4)return;
    const s=e.renderer.stats;
    $('fps').textContent=(frames/elapsed).toFixed(0);
    $('cpu').textContent=`${(s.cpuMs+e.physicsMs).toFixed(1)} ms`;
    $('gpu').textContent=s.gpuMs==null?'unavailable':`${s.gpuMs.toFixed(1)} ms`;
    $('triangles').textContent=s.triangles.toLocaleString();
    $('bodies').textContent=e.world.bodies.length;
    $('culling').textContent=`${s.culled} / ${s.objects}`;
    $('memory').textContent=`${(s.textureBytes/1024).toFixed(0)} KB`;
    $('wetness').textContent=`${Math.round(e.wet.mean*100)}%`;
    $('speed').textContent=`${Math.round(e.vehicle.body.velocity.length()*3.6)} km/h`;
    $('rpm').textContent=`${Math.round(e.vehicle.rpm)} rpm · ${e.vehicle.gear===0?'R':e.vehicle.gear-1}`;
    $('clock').textContent=`${String(Math.floor(e.sun.hour)).padStart(2,'0')}:${String(Math.floor(e.sun.hour%1*60)).padStart(2,'0')}`;
    $('hour-value').textContent=$('clock').textContent;
    if(e.sun.running)$('hour').value=e.sun.hour;
    $('resolution-label').textContent=`${s.width} × ${s.height}`;
    $('contact-count').textContent=e.world.contacts.length;
    frames=0;
    elapsed=0;
  };
  window.addEventListener('pagehide',()=>engine.dispose(),{
    once:true
  });
  engine.start();
}catch(error){
  fail(error);
}
```

## src/math.js

```javascript
export const EPS = 1e-8;
export const clamp = (x, lo, hi) => Math.max(lo, Math.min(hi, x));
export const lerp = (a, b, t) => a + (b - a) * t;
export const smoothstep = (a, b, x) => {
  const t = clamp((x-a)/(b-a),0,1);
  return t*t*(3-2*t);
};

export class Vec3 {
  constructor(x=0,y=0,z=0) {
    this.x=x;
    this.y=y;
    this.z=z;
  }
  set(x,y,z) {
    this.x=x;
    this.y=y;
    this.z=z;
    return this;
  }
  copy(v) {
    return this.set(v.x,v.y,v.z);
  }
  clone() {
    return new Vec3(this.x,this.y,this.z);
  }
  add(v) {
    this.x+=v.x;
    this.y+=v.y;
    this.z+=v.z;
    return this;
  }
  sub(v) {
    this.x-=v.x;
    this.y-=v.y;
    this.z-=v.z;
    return this;
  }
  scale(s) {
    this.x*=s;
    this.y*=s;
    this.z*=s;
    return this;
  }
  addScaled(v,s) {
    this.x+=v.x*s;
    this.y+=v.y*s;
    this.z+=v.z*s;
    return this;
  }
  dot(v) {
    return this.x*v.x+this.y*v.y+this.z*v.z;
  }
  cross(v) {
    return this.copy(Vec3.cross(this,v));
  }
  lengthSq() {
    return this.dot(this);
  }
  length() {
    return Math.sqrt(this.lengthSq());
  }
  normalize() {
    const n=this.length();
    return n>EPS?this.scale(1/n):this.set(0,0,0);
  }
  distance(v) {
    return Vec3.sub(this,v).length();
  }
  finite() {
    return Number.isFinite(this.x)&&Number.isFinite(this.y)&&Number.isFinite(this.z);
  }
  toArray(a=[],i=0) {
    a[i]=this.x;
    a[i+1]=this.y;
    a[i+2]=this.z;
    return a;
  }
  static from(a,i=0) {
    return new Vec3(a[i],a[i+1],a[i+2]);
  }
  static add(a,b) {
    return new Vec3(a.x+b.x,a.y+b.y,a.z+b.z);
  }
  static sub(a,b) {
    return new Vec3(a.x-b.x,a.y-b.y,a.z-b.z);
  }
  static cross(a,b) {
    return new Vec3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
  }
  static min(a,b) {
    return new Vec3(Math.min(a.x,b.x),Math.min(a.y,b.y),Math.min(a.z,b.z));
  }
  static max(a,b) {
    return new Vec3(Math.max(a.x,b.x),Math.max(a.y,b.y),Math.max(a.z,b.z));
  }
}

export class Quat {
  constructor(x=0,y=0,z=0,w=1) {
    this.x=x;
    this.y=y;
    this.z=z;
    this.w=w;
  }
  clone() {
    return new Quat(this.x,this.y,this.z,this.w);
  }
  copy(q) {
    this.x=q.x;
    this.y=q.y;
    this.z=q.z;
    this.w=q.w;
    return this;
  }
  normalize() {
    const n=Math.hypot(this.x,this.y,this.z,this.w);
    if(n<EPS)return this.copy(new Quat());
    this.x/=n;
    this.y/=n;
    this.z/=n;
    this.w/=n;
    return this;
  }
  conjugate() {
    return new Quat(-this.x,-this.y,-this.z,this.w);
  }
  multiply(q) {
    return this.copy(Quat.mul(this,q));
  }
  rotate(v) {
    const qv=new Vec3(this.x,this.y,this.z), t=Vec3.cross(qv,v).scale(2);
    return v.clone().addScaled(t,this.w).add(Vec3.cross(qv,t));
  }
  integrate(omega,dt) {
    const speed=omega.length();
    if(speed<EPS)return this;
    return this.copy(Quat.mul(Quat.axisAngle(omega.clone().scale(1/speed),speed*dt),this)).normalize();
  }
  static mul(a,b) {
    return new Quat(a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y,a.w*b.y-a.x*b.z+a.y*b.w+a.z*b.x,a.w*b.z+a.x*b.y-a.y*b.x+a.z*b.w,a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z);
  }
  static axisAngle(axis,angle) {
    const s=Math.sin(angle/2);
    return new Quat(axis.x*s,axis.y*s,axis.z*s,Math.cos(angle/2));
  }
  static slerp(a,b,t) {
    let d=a.x*b.x+a.y*b.y+a.z*b.z+a.w*b.w, s=d<0?-1:1;
    d=Math.abs(d);
    if(d>.9995)return new Quat(lerp(a.x,b.x*s,t),lerp(a.y,b.y*s,t),lerp(a.z,b.z*s,t),lerp(a.w,b.w*s,t)).normalize();
    const th=Math.acos(clamp(d,-1,1)), st=Math.sin(th), u=Math.sin((1-t)*th)/st,v=Math.sin(t*th)/st*s;
    return new Quat(a.x*u+b.x*v,a.y*u+b.y*v,a.z*u+b.z*v,a.w*u+b.w*v);
  }
}

export class Mat4 {
  static identity() {
    return new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]);
  }
  static mul(a,b) {
    const o=new Float32Array(16);
    for(let c=0;c<4;c++)for(let r=0;r<4;r++)for(let k=0;k<4;k++)o[c*4+r]+=a[k*4+r]*b[c*4+k];
    return o;
  }
  static compose(p,q,s=new Vec3(1,1,1)) {
    const x=q.x,y=q.y,z=q.z,w=q.w,xx=x*x,yy=y*y,zz=z*z,xy=x*y,xz=x*z,yz=y*z,wx=w*x,wy=w*y,wz=w*z;
    return new Float32Array([(1-2*(yy+zz))*s.x,2*(xy+wz)*s.x,2*(xz-wy)*s.x,0,2*(xy-wz)*s.y,(1-2*(xx+zz))*s.y,2*(yz+wx)*s.y,0,2*(xz+wy)*s.z,2*(yz-wx)*s.z,(1-2*(xx+yy))*s.z,0,p.x,p.y,p.z,1]);
  }
  static perspective(fov,aspect,near,far) {
    const f=1/Math.tan(fov/2),nf=1/(near-far);
    return new Float32Array([f/aspect,0,0,0,0,f,0,0,0,0,(far+near)*nf,-1,0,0,2*far*near*nf,0]);
  }
  static ortho(l,r,b,t,n,f) {
    return new Float32Array([2/(r-l),0,0,0,0,2/(t-b),0,0,0,0,-2/(f-n),0,-(r+l)/(r-l),-(t+b)/(t-b),-(f+n)/(f-n),1]);
  }
  static lookAt(eye,target,up=new Vec3(0,1,0)) {
    const z=Vec3.sub(eye,target).normalize();
    let x=Vec3.cross(up,z).normalize();
    if(x.lengthSq()<EPS)x=Vec3.cross(new Vec3(0,0,1),z).normalize();
    const y=Vec3.cross(z,x);
    return new Float32Array([x.x,y.x,z.x,0,x.y,y.y,z.y,0,x.z,y.z,z.z,0,-x.dot(eye),-y.dot(eye),-z.dot(eye),1]);
  }
  static invert(a) {
    const m=Array.from({
      length:4
    },(_,r)=>[a[r],a[4+r],a[8+r],a[12+r],...Array.from({
      length:4
    },(_,c)=>r===c?1:0)]);
    for(let c=0;c<4;c++) {
      let pivot=c;
      for(let r=c+1;r<4;r++)if(Math.abs(m[r][c])>Math.abs(m[pivot][c]))pivot=r;
      if(Math.abs(m[pivot][c])<1e-12)throw new Error('Singular matrix');
      [m[pivot],m[c]]=[m[c],m[pivot]];
      const v=m[c][c];
      for(let k=0;k<8;k++)m[c][k]/=v;
      for(let r=0;r<4;r++)if(r!==c){
        const f=m[r][c];
        for(let k=0;k<8;k++)m[r][k]-=f*m[c][k];
      }
    }
    const out=new Float32Array(16);
    for(let r=0;r<4;r++)for(let c=0;c<4;c++)out[c*4+r]=m[r][c+4];
    return out;
  }
  static point(m,v) {
    const w=m[3]*v.x+m[7]*v.y+m[11]*v.z+m[15];
    return new Vec3((m[0]*v.x+m[4]*v.y+m[8]*v.z+m[12])/w,(m[1]*v.x+m[5]*v.y+m[9]*v.z+m[13])/w,(m[2]*v.x+m[6]*v.y+m[10]*v.z+m[14])/w);
  }
}

export class AABB {
  constructor(min=new Vec3(Infinity,Infinity,Infinity),max=new Vec3(-Infinity,-Infinity,-Infinity)) {
    this.min=min;
    this.max=max;
  }
  clone() {
    return new AABB(this.min.clone(),this.max.clone());
  }
  include(p) {
    this.min=Vec3.min(this.min,p);
    this.max=Vec3.max(this.max,p);
    return this;
  }
  union(b) {
    return this.include(b.min).include(b.max);
  }
  center() {
    return Vec3.add(this.min,this.max).scale(.5);
  }
  extent() {
    return Vec3.sub(this.max,this.min);
  }
  area() {
    const d=this.extent();
    return 2*(d.x*d.y+d.y*d.z+d.z*d.x);
  }
  overlaps(b) {
    return this.min.x<=b.max.x&&this.max.x>=b.min.x&&this.min.y<=b.max.y&&this.max.y>=b.min.y&&this.min.z<=b.max.z&&this.max.z>=b.min.z;
  }
  expand(s) {
    this.min.add(new Vec3(-s,-s,-s));
    this.max.add(new Vec3(s,s,s));
    return this;
  }
  ray(origin,dir,tMax=Infinity) {
    let near=0,far=tMax;
    for(const k of ['x','y','z']) {
      if(Math.abs(dir[k])<1e-12){
        if(origin[k]<this.min[k]||origin[k]>this.max[k])return null;
        continue;
      }
      let a=(this.min[k]-origin[k])/dir[k],b=(this.max[k]-origin[k])/dir[k];
      if(a>b)[a,b]=[b,a];
      near=Math.max(near,a);
      far=Math.min(far,b);
      if(near>far)return null;
    }
    return near;
  }
}

export class Frustum {
  constructor(m) {
    this.planes=[];
    for(const [r,s] of [[0,1],[0,-1],[1,1],[1,-1],[2,1],[2,-1]]){
      const p=[m[3]+s*m[r],m[7]+s*m[4+r],m[11]+s*m[8+r],m[15]+s*m[12+r]],l=Math.hypot(...p.slice(0,3));
      this.planes.push(p.map(v=>v/l));
    }
  }
  intersects(b) {
    return this.planes.every(p=>p[0]*(p[0]>=0?b.max.x:b.min.x)+p[1]*(p[1]>=0?b.max.y:b.min.y)+p[2]*(p[2]>=0?b.max.z:b.min.z)+p[3]>=0);
  }
}
```

## src/mesh.js

```javascript
import { Vec3, AABB } from './math.js';

export class MeshData {
  constructor(positions,normals,uvs,indices){
    this.positions=new Float32Array(positions);
    this.normals=new Float32Array(normals);
    this.uvs=new Float32Array(uvs);
    this.indices=new Uint32Array(indices);
    this.version=0;
    this.bounds=new AABB();
    for(let i=0;i<this.positions.length;i+=3)this.bounds.include(Vec3.from(this.positions,i));
  }
  static box(){
    const p=[],n=[],uv=[],idx=[];
    const faces=[[[1,0,0],[0,0,-1],[0,1,0]],[[-1,0,0],[0,0,1],[0,1,0]],[[0,1,0],[1,0,0],[0,0,-1]],[[0,-1,0],[1,0,0],[0,0,1]],[[0,0,1],[1,0,0],[0,1,0]],[[0,0,-1],[-1,0,0],[0,1,0]]];
    for(const [normal,u,v]of faces){
      const start=p.length/3;
      for(const [x,y]of [[-1,-1],[1,-1],[1,1],[-1,1]]){
        const pos=Vec3.from(normal).addScaled(Vec3.from(u),x).addScaled(Vec3.from(v),y);
        pos.toArray(p,p.length);
        n.push(...normal);
        uv.push((x+1)/2,(y+1)/2);
      }
      idx.push(start,start+1,start+2,start,start+2,start+3);
    }
    return new MeshData(p,n,uv,idx);
  }
  static sphere(segments=24,rings=12){
    const p=[],n=[],uv=[],idx=[];
    for(let j=0;j<=rings;j++)for(let i=0;i<=segments;i++){
      const phi=j/rings*Math.PI,theta=i/segments*Math.PI*2,v=new Vec3(-Math.sin(phi)*Math.cos(theta),Math.cos(phi),Math.sin(phi)*Math.sin(theta));
      v.toArray(p,p.length);
      v.toArray(n,n.length);
      uv.push(i/segments,j/rings);
    }
    for(let j=0;j<rings;j++)for(let i=0;i<segments;i++){
      const a=j*(segments+1)+i,b=a+segments+1;
      idx.push(a,b,a+1,a+1,b,b+1);
    }
    return new MeshData(p,n,uv,idx);
  }
  static surface(surface){
    const uv=[];
    for(let z=0;z<=surface.n;z++)for(let x=0;x<=surface.n;x++)uv.push(x/surface.n,z/surface.n);
    const mesh=new MeshData(surface.positions,surface.normals(),uv,surface.indices);
    mesh.surface=surface;
    return mesh;
  }
  updateSurface(){
    if(!this.surface)return;
    this.positions.set(this.surface.positions);
    this.normals=this.surface.normals();
    this.version++;
  }
}

export class GPUMesh {
  constructor(gl,data){
    this.gl=gl;
    this.data=data;
    this.version=-1;
    this.vao=gl.createVertexArray();
    this.buffers=[];
    gl.bindVertexArray(this.vao);
    const colors = data.colors || new Float32Array(data.positions.length).fill(1);
    const materials = data.materials || new Float32Array(data.positions.length / 3 * 4);
    for(const [location,size,values]of [[0,3,data.positions],[1,3,data.normals],[2,2,data.uvs],[3,3,colors],[4,4,materials]]){
      const b=gl.createBuffer();
      this.buffers.push(b);
      gl.bindBuffer(gl.ARRAY_BUFFER,b);
      gl.bufferData(gl.ARRAY_BUFFER,values,data.surface?gl.DYNAMIC_DRAW:gl.STATIC_DRAW);
      gl.enableVertexAttribArray(location);
      gl.vertexAttribPointer(location,size,gl.FLOAT,false,0,0);
    }
    this.index=gl.createBuffer();
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,this.index);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER,data.indices,gl.STATIC_DRAW);
    gl.bindVertexArray(null);
    this.version=data.version;
    this.instanceBuffer = null;
    this.instanceBytes = 0;
    this.instanceVersion = null;
  }
  update(){
    if(this.version===this.data.version)return;
    const g=this.gl;
    for(const [i,v]of [[0,this.data.positions],[1,this.data.normals]]){
      g.bindBuffer(g.ARRAY_BUFFER,this.buffers[i]);
      g.bufferSubData(g.ARRAY_BUFFER,0,v);
    }
    this.version=this.data.version;
  }
  draw(instances = null, count = 0, version = ''){
    this.update();
    const g=this.gl;
    g.bindVertexArray(this.vao);
    if (instances) {
      if (!count) return;
      if (!this.instanceBuffer) {
        this.instanceBuffer = g.createBuffer(); g.bindBuffer(g.ARRAY_BUFFER, this.instanceBuffer);
        for (let i = 0; i < 4; i++) { g.enableVertexAttribArray(5 + i); g.vertexAttribPointer(5 + i, 4, g.FLOAT, false, 96, i * 16); g.vertexAttribDivisor(5 + i, 1); }
        for (const [location, offset] of [[9, 64], [10, 80]]) { g.enableVertexAttribArray(location); g.vertexAttribPointer(location, 4, g.FLOAT, false, 96, offset); g.vertexAttribDivisor(location, 1); }
      }
      if (this.instanceVersion !== version) {
        g.bindBuffer(g.ARRAY_BUFFER, this.instanceBuffer); const values = instances.subarray(0, count * 24);
        if (values.byteLength > this.instanceBytes) { g.bufferData(g.ARRAY_BUFFER, values, g.DYNAMIC_DRAW); this.instanceBytes = values.byteLength; }
        else g.bufferSubData(g.ARRAY_BUFFER, 0, values);
        this.instanceVersion = version;
      }
      g.drawElementsInstanced(g.TRIANGLES, this.data.indices.length, g.UNSIGNED_INT, 0, count);
    } else g.drawElements(g.TRIANGLES,this.data.indices.length,g.UNSIGNED_INT,0);
  }
  dispose(){
    for(const b of this.buffers)this.gl.deleteBuffer(b);
    this.gl.deleteBuffer(this.index);
    if (this.instanceBuffer) this.gl.deleteBuffer(this.instanceBuffer);
    this.gl.deleteVertexArray(this.vao);
  }
}
```

## src/physics.js

```javascript
import { Vec3, Quat, AABB, EPS, clamp } from './math.js';
import { BVH } from './spatial.js';
let nextBodyId=1;
const UP=new Vec3(0,1,0),AXES=[new Vec3(1,0,0),UP,new Vec3(0,0,1)];

export class RigidBody {
  constructor({
    shape='box',half=new Vec3(.5,.5,.5),radius=.5,mass=1,position=new Vec3(),rotation=new Quat(),friction=.65,restitution=.12
  } = {}) {
    if(!['box','sphere'].includes(shape)||mass<0||radius<=0||Math.min(half.x,half.y,half.z)<=0)throw new RangeError('Invalid rigid body');
    this.id=nextBodyId++;
    this.shape=shape;
    this.half=half.clone();
    this.radius=radius;
    this.mass=mass;
    this.invMass=mass>0?1/mass:0;
    this.position=position.clone();
    this.rotation=rotation.clone().normalize();
    this.previousPosition=this.position.clone();
    this.previousRotation=this.rotation.clone();
    this.velocity=new Vec3();
    this.angularVelocity=new Vec3();
    this.force=new Vec3();
    this.torque=new Vec3();
    this.friction=friction;
    this.restitution=restitution;
    this.linearDamping=.04;
    this.angularDamping=.18;
    this.inverseInertia=new Vec3();
    if(mass>0){
      if(shape==='sphere')this.inverseInertia.set(2.5/(mass*radius*radius),2.5/(mass*radius*radius),2.5/(mass*radius*radius));
      else this.inverseInertia.set(3/(mass*(half.y**2+half.z**2)),3/(mass*(half.x**2+half.z**2)),3/(mass*(half.x**2+half.y**2)));
    }
    this.bounds=new AABB();
    this.updateBounds();
  }
  axes(){
    return AXES.map(a=>this.rotation.rotate(a));
  }
  localToWorld(v){
    return this.rotation.rotate(v).add(this.position);
  }
  worldToLocal(v){
    return this.rotation.conjugate().rotate(Vec3.sub(v,this.position));
  }
  inertiaMul(v){
    const local=this.rotation.conjugate().rotate(v);
    local.x*=this.inverseInertia.x;
    local.y*=this.inverseInertia.y;
    local.z*=this.inverseInertia.z;
    return this.rotation.rotate(local);
  }
  inertiaTensorMul(v){
    if(!this.invMass)return new Vec3();
    const local=this.rotation.conjugate().rotate(v);
    local.x/=this.inverseInertia.x;
    local.y/=this.inverseInertia.y;
    local.z/=this.inverseInertia.z;
    return this.rotation.rotate(local);
  }
  pointVelocity(p){
    return this.velocity.clone().add(Vec3.cross(this.angularVelocity,Vec3.sub(p,this.position)));
  }
  applyForce(f,p=this.position){
    if(!this.invMass)return;
    this.force.add(f);
    this.torque.add(Vec3.cross(Vec3.sub(p,this.position),f));
  }
  impulse(j,p=this.position){
    if(!this.invMass)return;
    this.velocity.addScaled(j,this.invMass);
    this.angularVelocity.add(this.inertiaMul(Vec3.cross(Vec3.sub(p,this.position),j)));
  }
  vertices(){
    const out=[];
    for(const x of [-1,1])for(const y of [-1,1])for(const z of [-1,1])out.push(this.localToWorld(new Vec3(x*this.half.x,y*this.half.y,z*this.half.z)));
    return out;
  }
  support(n){
    if(this.shape==='sphere')return this.position.clone().addScaled(n,this.radius);
    const l=this.rotation.conjugate().rotate(n),h=this.half;
    return this.localToWorld(new Vec3(Math.abs(l.x)<EPS?0:Math.sign(l.x)*h.x,Math.abs(l.y)<EPS?0:Math.sign(l.y)*h.y,Math.abs(l.z)<EPS?0:Math.sign(l.z)*h.z));
  }
  updateBounds(){
    if(this.shape==='sphere'){
      const r=new Vec3(this.radius,this.radius,this.radius);
      this.bounds=new AABB(Vec3.sub(this.position,r),Vec3.add(this.position,r));
    }else{
      this.bounds=new AABB();
      for(const p of this.vertices())this.bounds.include(p);
    }
    return this.bounds;
  }
  raycast(origin,dir,max=Infinity){
    if(this.shape==='sphere'){
      const o=Vec3.sub(origin,this.position),a=dir.dot(dir),b=o.dot(dir),c=o.dot(o)-this.radius*this.radius,disc=b*b-a*c;
      if(disc<0||a<EPS)return null;
      let t=(-b-Math.sqrt(disc))/a;
      if(t<1e-5)t=(-b+Math.sqrt(disc))/a;
      if(t<1e-5||t>max)return null;
      const point=origin.clone().addScaled(dir,t);
      return {
        distance:t,point,normal:Vec3.sub(point,this.position).normalize(),body:this
      };
    }
    const o=this.worldToLocal(origin),d=this.rotation.conjugate().rotate(dir);
    let near=-Infinity,far=Infinity,nearN=new Vec3(),farN=new Vec3();
    for(const k of ['x','y','z']){
      if(Math.abs(d[k])<EPS){
        if(Math.abs(o[k])>this.half[k])return null;
        continue;
      }
      let a=(-this.half[k]-o[k])/d[k],b=(this.half[k]-o[k])/d[k],na=new Vec3(),nb=new Vec3();
      na[k]=-1;
      nb[k]=1;
      if(a>b){
        [a,b]=[b,a];
        [na,nb]=[nb,na];
      }
      if(a>near){
        near=a;
        nearN=na;
      }
      if(b<far){
        far=b;
        farN=nb;
      }
      if(near>far)return null;
    }
    const inside=near<1e-5,t=inside?far:near;
    if(t<1e-5||t>max)return null;
    return {
      distance:t,point:origin.clone().addScaled(dir,t),normal:this.rotation.rotate(inside?farN:nearN),body:this
    };
  }
}
function clipPolygon(poly,center,normal,limit){
  const out=[];
  for(let i=0;i<poly.length;i++){
    const a=poly[i],b=poly[(i+1)%poly.length],da=Vec3.sub(a,center).dot(normal)-limit,db=Vec3.sub(b,center).dot(normal)-limit;
    if(da<=0)out.push(a);
    if((da<0&&db>0)||(da>0&&db<0))out.push(a.clone().addScaled(Vec3.sub(b,a),da/(da-db)));
  }
  return out;
}
function faceVertices(body,axis,sign){
  const h=[body.half.x,body.half.y,body.half.z],other=[0,1,2].filter(i=>i!==axis),out=[];
  for(const [u,v]of [[-1,-1],[1,-1],[1,1],[-1,1]]){
    const p=[0,0,0];
    p[axis]=sign*h[axis];
    p[other[0]]=u*h[other[0]];
    p[other[1]]=v*h[other[1]];
    out.push(body.localToWorld(Vec3.from(p)));
  }
  return out;
}
export function collide(a,b){
  const contact=(normal,point,depth)=>({
    a,b,normal,point,depth,jn:0,jt1:0,jt2:0
  });
  if(a.shape==='sphere'&&b.shape==='sphere'){
    const n=Vec3.sub(b.position,a.position),d=n.length(),depth=a.radius+b.radius-d;
    if(depth<=0)return [];
    if(d<EPS)n.copy(UP);
    else n.scale(1/d);
    return [contact(n,a.position.clone().addScaled(n,a.radius-depth*.5),depth)];
  }
  if(a.shape==='sphere'||b.shape==='sphere'){
    const sphere=a.shape==='sphere'?a:b,box=sphere===a?b:a,l=box.worldToLocal(sphere.position),q=new Vec3(clamp(l.x,-box.half.x,box.half.x),clamp(l.y,-box.half.y,box.half.y),clamp(l.z,-box.half.z,box.half.z));
    const delta=Vec3.sub(l,q);
    let dist=delta.length(),depth=sphere.radius-dist,n;
    if(dist>EPS){
      if(depth<=0)return [];
      n=box.rotation.rotate(delta.scale(1/dist));
    } else {
      const gaps=['x','y','z'].map(k=>({
        k,gap:box.half[k]-Math.abs(l[k])
      })).sort((x,y)=>x.gap-y.gap),k=gaps[0].k;
      delta.set(0,0,0);
      delta[k]=l[k]>=0?1:-1;
      q[k]=delta[k]*box.half[k];
      n=box.rotation.rotate(delta);
      depth=sphere.radius+gaps[0].gap;
    }
    const point=box.localToWorld(q);
    return [contact(sphere===a?n.scale(-1):n,point,depth)];
  }
  const aa=a.axes(),ba=b.axes(),ha=[a.half.x,a.half.y,a.half.z],hb=[b.half.x,b.half.y,b.half.z],delta=Vec3.sub(b.position,a.position),candidates=[];
  aa.forEach((n,i)=>candidates.push({
    n,type:'a',axis:i
  }));
  ba.forEach((n,i)=>candidates.push({
    n,type:'b',axis:i
  }));
  for(let i=0;i<3;i++)for(let j=0;j<3;j++){
    const n=Vec3.cross(aa[i],ba[j]);
    if(n.lengthSq()>1e-10)candidates.push({
      n:n.normalize(),type:'edge'
    });
  }
  let best=null,depth=Infinity;
  for(const c of candidates){
    const ra=aa.reduce((s,n,i)=>s+Math.abs(n.dot(c.n))*ha[i],0),rb=ba.reduce((s,n,i)=>s+Math.abs(n.dot(c.n))*hb[i],0),overlap=ra+rb-Math.abs(delta.dot(c.n));
    if(overlap<=0)return [];
    if(overlap<depth){
      depth=overlap;
      best=c;
    }
  }
  const normal=best.n.clone().scale(delta.dot(best.n)>=0?1:-1);
  if(best.type==='edge')return [contact(normal,Vec3.add(a.support(normal),b.support(normal.clone().scale(-1))).scale(.5),depth)];
  const ref=best.type==='a'?a:b,inc=ref===a?b:a,rn=normal.clone().scale(ref===a?1:-1),refAxes=ref===a?aa:ba,incAxes=ref===a?ba:aa,axis=best.axis,refHalf=[ref.half.x,ref.half.y,ref.half.z],faceCenter=ref.position.clone().addScaled(rn,refHalf[axis]);
  let incAxis=0;
  for(let i=1;i<3;i++)if(Math.abs(incAxes[i].dot(rn))>Math.abs(incAxes[incAxis].dot(rn)))incAxis=i;
  let poly=faceVertices(inc,incAxis,incAxes[incAxis].dot(rn)>0?-1:1);
  for(let i=0;i<3;i++)if(i!==axis){
    poly=clipPolygon(poly,faceCenter,refAxes[i],refHalf[i]);
    poly=clipPolygon(poly,faceCenter,refAxes[i].clone().scale(-1),refHalf[i]);
  }
  const points=poly.map(p=>({
    p,d:Vec3.sub(p,faceCenter).dot(rn)
  })).filter(x=>x.d<=.002).sort((x,y)=>x.d-y.d).slice(0,4);
  if(!points.length)return [contact(normal,Vec3.add(a.support(normal),b.support(normal.clone().scale(-1))).scale(.5),depth)];
  return points.map(({
    p,d
  })=>contact(normal.clone(),p.clone().addScaled(rn,-d*.5),Math.max(0,-d)));
}
function inverseEffective(a,b,p,n){
  const ra=Vec3.sub(p,a.position),rb=Vec3.sub(p,b.position);
  return a.invMass+b.invMass+n.dot(Vec3.cross(a.inertiaMul(Vec3.cross(ra,n)),ra).add(Vec3.cross(b.inertiaMul(Vec3.cross(rb,n)),rb)));
}
function pairImpulse(a,b,p,j){
  a.impulse(j.clone().scale(-1),p);
  b.impulse(j,p);
}

export class BallSocketJoint {
  constructor(a,b,worldAnchor,{
    maxSwing=Math.PI*.65,maxTwist=Math.PI*.5,axis=new Vec3(0,1,0),bias=.18
  } = {}){
    this.a=a;
    this.b=b;
    this.localA=a.worldToLocal(worldAnchor);
    this.localB=b.worldToLocal(worldAnchor);
    this.axisA=a.rotation.conjugate().rotate(axis);
    this.axisB=b.rotation.conjugate().rotate(axis);
    this.reference=Quat.mul(a.rotation.conjugate(),b.rotation);
    this.maxSwing=maxSwing;
    this.maxTwist=maxTwist;
    this.bias=bias;
  }
  solve(dt){
    const a=this.a,b=this.b,pa=a.localToWorld(this.localA),pb=b.localToWorld(this.localB),p=Vec3.add(pa,pb).scale(.5),error=Vec3.sub(pb,pa);
    for(const n of AXES){
      const k=inverseEffective(a,b,p,n);
      if(k<EPS)continue;
      const rel=Vec3.sub(b.pointVelocity(pb),a.pointVelocity(pa)).dot(n),j=clamp(-(rel+this.bias*error.dot(n)/dt)/k,-100,100);
      pairImpulse(a,b,p,n.clone().scale(j));
    }
    const ax=a.rotation.rotate(this.axisA),bx=b.rotation.rotate(this.axisB),cross=Vec3.cross(ax,bx),angle=Math.acos(clamp(ax.dot(bx),-1,1));
    if(angle>this.maxSwing){
      let n=cross.normalize();
      if(n.lengthSq()<EPS)n=Vec3.cross(ax,Math.abs(ax.y)<.9?UP:AXES[0]).normalize();
      this.angularLimit(n,angle-this.maxSwing,dt);
    }
    const rel=Quat.mul(Quat.mul(a.rotation.conjugate(),b.rotation),this.reference.conjugate()).normalize(),projection=rel.x*this.axisA.x+rel.y*this.axisA.y+rel.z*this.axisA.z;
    let twist=2*Math.atan2(projection,rel.w);
    twist=((twist+Math.PI)%(2*Math.PI)+2*Math.PI)%(2*Math.PI)-Math.PI;
    if(Math.abs(twist)>this.maxTwist)this.angularLimit(ax.clone().scale(Math.sign(twist)),Math.abs(twist)-this.maxTwist,dt);
  }
  angularLimit(n,error,dt){
    const a=this.a,b=this.b,k=n.dot(a.inertiaMul(n).add(b.inertiaMul(n)));
    if(k<EPS)return;
    const rel=Vec3.sub(b.angularVelocity,a.angularVelocity).dot(n),j=Math.min(0,-(rel+.16*error/dt)/k);
    a.angularVelocity.addScaled(a.inertiaMul(n),-j);
    b.angularVelocity.addScaled(b.inertiaMul(n),j);
  }
}

export class PhysicsWorld {
  constructor({
    gravity=new Vec3(0,-9.81,0),iterations=14,ground=true
  } = {}){
    this.gravity=gravity;
    this.iterations=iterations;
    this.bodies=[];
    this.joints=[];
    this.contacts=[];
    this.disabledPairs=new Set();
    this.beforeStep=new Set();
    this.afterStep=new Set();
    this.bvh=new BVH();
    this.ground=ground;
    this.groundBody=new RigidBody({
      mass:0,half:new Vec3(1000,.5,1000),position:new Vec3(0,-.5,0),restitution:.05
    });
    this.topology=0;
    this.broadVersion=-1;
    this.cache=new Map();
  }
  add(body){
    this.bodies.push(body);
    this.topology++;
    return body;
  }
  remove(body){
    const i=this.bodies.indexOf(body);
    if(i>=0){
      this.bodies.splice(i,1);
      this.joints=this.joints.filter(j=>j.a!==body&&j.b!==body);
      for(const key of this.disabledPairs)if(key.split(':').some(id=>Number(id)===body.id))this.disabledPairs.delete(key);
      this.topology++;
    }
  }
  addJoint(joint){
    this.joints.push(joint);
    this.disabledPairs.add(this.pairKey(joint.a,joint.b));
    return joint;
  }
  pairKey(a,b){
    return a.id<b.id?`${a.id}:${b.id}`:`${b.id}:${a.id}`;
  }
  updateBroadphase(){
    for(const b of this.bodies)if(b.invMass || b.kinematic)b.updateBounds();
    if(this.broadVersion!==this.topology){
      this.bvh.rebuild(this.bodies);
      this.broadVersion=this.topology;
    }else this.bvh.refit();
  }
  generateContacts(){
    const out=[];
    for(const a of this.bodies){
      if(!a.invMass)continue;
      if(this.ground){
        const n=new Vec3(0,-1,0);
        if(a.shape==='sphere'){
          const depth=a.radius-a.position.y+(this.groundHeight?.(a.position.x,a.position.z)||0);
          if(depth>0)out.push({
            a,b:this.groundBody,normal:n,point:new Vec3(a.position.x,a.position.y-a.radius,a.position.z),depth,jn:0,jt1:0,jt2:0
          });
        }else for(const p of a.vertices()){
          const height=this.groundHeight?.(p.x,p.z)||0;
          if(p.y<height)out.push({a,b:this.groundBody,normal:this.groundHeight?this.terrainNormal(p.x,p.z).scale(-1):n.clone(),point:p,depth:height-p.y,jn:0,jt1:0,jt2:0});
        }
      }
      this.bvh.query(a.bounds,b=>{
        if(a===b||(b.invMass&&b.id<a.id)||this.disabledPairs.has(this.pairKey(a,b)))return;
        out.push(...collide(a,b));
      });
    }
    return out;
  }
  prepareContact(c,dt){
    const {
      a,b,point:p,normal:n
    }
    =c;
    c.t1=Vec3.cross(n,Math.abs(n.x)<.8?AXES[0]:UP).normalize();
    c.t2=Vec3.cross(n,c.t1);
    c.kn=inverseEffective(a,b,p,n);
    c.k1=inverseEffective(a,b,p,c.t1);
    c.k2=inverseEffective(a,b,p,c.t2);
    c.mu=Math.sqrt(a.friction*b.friction);
    const vn=Vec3.sub(b.pointVelocity(p),a.pointVelocity(p)).dot(n);
    c.target=Math.max(Math.min(2,.15*Math.max(0,c.depth-.004)/dt),vn<-.8?-Math.min(a.restitution,b.restitution)*vn:0);
    const local=a.worldToLocal(p);
    c.key=`${a.id}:${b.id}:${Math.round(local.x*12)}:${Math.round(local.y*12)}:${Math.round(local.z*12)}`;
    const old=this.cache.get(c.key);
    if(old&&old.n.dot(n)>.95){
      c.jn=old.jn*.8;
      c.jt1=old.jt1*.8;
      c.jt2=old.jt2*.8;
      pairImpulse(a,b,p,n.clone().scale(c.jn).addScaled(c.t1,c.jt1).addScaled(c.t2,c.jt2));
    }
  }
  solveContact(c){
    const {
      a,b,point:p,normal:n
    }
    =c;
    let v=Vec3.sub(b.pointVelocity(p),a.pointVelocity(p));
    const old=c.jn;
    c.jn=Math.max(0,old+(c.target-v.dot(n))/Math.max(EPS,c.kn));
    pairImpulse(a,b,p,n.clone().scale(c.jn-old));
    v=Vec3.sub(b.pointVelocity(p),a.pointVelocity(p));
    let u=c.jt1-v.dot(c.t1)/Math.max(EPS,c.k1),w=c.jt2-v.dot(c.t2)/Math.max(EPS,c.k2),mag=Math.hypot(u,w),limit=c.mu*c.jn;
    if(mag>limit&&mag>EPS){
      u*=limit/mag;
      w*=limit/mag;
    }
    pairImpulse(a,b,p,c.t1.clone().scale(u-c.jt1).addScaled(c.t2,w-c.jt2));
    c.jt1=u;
    c.jt2=w;
  }
  step(dt){
    if(!(dt>0&&dt<=.05))throw new RangeError('Physics dt must be in (0, .05] seconds');
    for(const b of this.bodies){
      b.previousPosition.copy(b.position);
      b.previousRotation.copy(b.rotation);
    }
    this.updateBroadphase();
    for(const f of this.beforeStep)f(dt);
    for(const b of this.bodies){
      if(!b.invMass)continue;
      b.velocity.addScaled(this.gravity,dt).addScaled(b.force,b.invMass*dt);
      const gyro=Vec3.cross(b.angularVelocity,b.inertiaTensorMul(b.angularVelocity));
      b.angularVelocity.addScaled(b.inertiaMul(Vec3.sub(b.torque,gyro)),dt);
      b.velocity.scale(Math.exp(-b.linearDamping*dt));
      b.angularVelocity.scale(Math.exp(-b.angularDamping*dt));
      b.force.set(0,0,0);
      b.torque.set(0,0,0);
    }
    this.contacts=this.generateContacts();
    for(const c of this.contacts)this.prepareContact(c,dt);
    for(let i=0;i<this.iterations;i++){
      for(const c of this.contacts)this.solveContact(c);
      for(const j of this.joints)j.solve(dt);
    }
    for(const b of this.bodies){
      if(!b.invMass)continue;
      b.position.addScaled(b.velocity,dt);
      b.rotation.integrate(b.angularVelocity,dt);
      b.updateBounds();
    }
    this.cache=new Map(this.contacts.map(c=>[c.key,{
      jn:c.jn,jt1:c.jt1,jt2:c.jt2,n:c.normal.clone()
    }]));
    this.bvh.refit();
    for(const f of this.afterStep)f(dt);
  }
  terrainNormal(x,z){const h=.1,fn=this.groundHeight||(()=>0);return new Vec3(fn(x-h,z)-fn(x+h,z),2*h,fn(x,z-h)-fn(x,z+h)).normalize();}
  raycast(origin,dir,max=Infinity,ignore=null){
    let best=this.bvh.raycast(origin,dir,(b,limit)=>b===ignore?null:b.raycast(origin,dir,limit),max);
    if(this.groundHeight && this.ground){
      const limit=Math.min(max,best?.distance??max,500);let previous=origin.y-this.groundHeight(origin.x,origin.z),left=0;
      for(let i=1;i<=64;i++){const t=limit*i/64,p=origin.clone().addScaled(dir,t),above=p.y-this.groundHeight(p.x,p.z);
        if(previous>=0&&above<=0){let low=left,high=t;for(let j=0;j<14;j++){const mid=(low+high)/2,q=origin.clone().addScaled(dir,mid);if(q.y>this.groundHeight(q.x,q.z))low=mid;else high=mid;}const distance=(low+high)/2,point=origin.clone().addScaled(dir,distance);if(distance>1e-5)best={distance,point,normal:this.terrainNormal(point.x,point.z),body:this.groundBody};break;}previous=above;left=t;
      }
    }else if(this.ground&&dir.y< -EPS){
      const t=-origin.y/dir.y;
      if(t>1e-5&&t<max&&(!best||t<best.distance))best={
        distance:t,point:origin.clone().addScaled(dir,t),normal:UP.clone(),body:this.groundBody
      };
    }
    return best;
  }
}

export class Ragdoll {
  constructor(world,origin=new Vec3(0,4,0)){
    this.world=world;
    this.bodies=[];
    this.joints=[];
    const part=(name,offset,half,mass)=>{
      const body=world.add(new RigidBody({
        position:Vec3.add(origin,Vec3.from(offset)),half:Vec3.from(half),mass,restitution:.05
      }));
      body.name=name;
      this.bodies.push(body);
      return body;
    };
    const pelvis=part('pelvis',[0,0,0],[.22,.16,.14],9),torso=part('torso',[0,.44,0],[.27,.28,.14],17),head=part('head',[0,.95,0],[.14,.16,.14],5);
    const joint=(a,b,p,opts={
    })=>{
      const j=world.addJoint(new BallSocketJoint(a,b,Vec3.add(origin,Vec3.from(p)),opts));
      this.joints.push(j);
    };
    joint(pelvis,torso,[0,.17,0],{
      maxSwing:.5,maxTwist:.45
    });
    joint(torso,head,[0,.76,0],{
      maxSwing:.65,maxTwist:.8
    });
    for(const side of [-1,1]){
      const upper=part('upper arm',[side*.42,.44,0],[.12,.25,.12],3),lower=part('forearm',[side*.42,-.07,0],[.1,.24,.1],2),thigh=part('thigh',[side*.14,-.49,0],[.11,.3,.12],7),shin=part('shin',[side*.14,-1.11,0],[.1,.29,.1],4);
      joint(torso,upper,[side*.29,.67,0],{
        maxSwing:1.4,maxTwist:1
      });
      joint(upper,lower,[side*.42,.18,0],{
        maxSwing:1.65,maxTwist:.12
      });
      joint(pelvis,thigh,[side*.14,-.15,0],{
        maxSwing:1.1,maxTwist:.35
      });
      joint(thigh,shin,[side*.14,-.8,0],{
        maxSwing:1.6,maxTwist:.1
      });
    }
  }
  dispose(){
    for(const b of this.bodies)this.world.remove(b);
    this.bodies=[];
    this.joints=[];
  }
}
```

## src/renderer.js

```javascript
import { Vec3, Quat, Mat4, AABB, Frustum } from './math.js';
import { BVH } from './spatial.js';
import { GPUMesh, MeshData } from './mesh.js';
import { TextureStreamer, LODSelector } from './resources.js';
import * as shader from './shaders.js';
class Program {
  constructor(gl,vs,fs){
    this.gl=gl;
    this.uniforms=new Map();
    const stages=[];
    try{
      for(const [type,source]of [[gl.VERTEX_SHADER,vs],[gl.FRAGMENT_SHADER,fs]]){
        const s=gl.createShader(type);
        stages.push(s);
        gl.shaderSource(s,source);
        gl.compileShader(s);
        if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw new Error(gl.getShaderInfoLog(s));
      }
      this.handle=gl.createProgram();
      for(const s of stages)gl.attachShader(this.handle,s);
      gl.linkProgram(this.handle);
      if(!gl.getProgramParameter(this.handle,gl.LINK_STATUS))throw new Error(gl.getProgramInfoLog(this.handle));
    }catch(error){
      if(this.handle)gl.deleteProgram(this.handle);
      throw error;
    }finally{
      for(const s of stages)gl.deleteShader(s);
    }
  }
  use(){
    this.gl.useProgram(this.handle);
    return this;
  }
  location(n){
    if(!this.uniforms.has(n))this.uniforms.set(n,this.gl.getUniformLocation(this.handle,n));
    return this.uniforms.get(n);
  }
  f(n,v){
    this.gl.uniform1f(this.location(n),v);
    return this;
  }
  i(n,v){
    this.gl.uniform1i(this.location(n),v);
    return this;
  }
  v2(n,x,y){
    this.gl.uniform2f(this.location(n),x,y);
    return this;
  }
  v3(n,v){
    this.gl.uniform3f(this.location(n),v.x??v[0],v.y??v[1],v.z??v[2]);
    return this;
  }
  m4(n,v){
    this.gl.uniformMatrix4fv(this.location(n),false,v);
    return this;
  }
  v4array(n, v) { this.gl.uniform4fv(this.location(n), v); return this; }
  tex(n,texture,unit){
    const g=this.gl;
    g.activeTexture(g.TEXTURE0+unit);
    g.bindTexture(g.TEXTURE_2D,texture);
    return this.i(n,unit);
  }
  dispose(){
    this.gl.deleteProgram(this.handle);
  }
}

export class GPUOcclusion {
  constructor(gl,boxMesh){
    this.gl=gl;
    this.mesh=boxMesh;
    this.entries=new Map();
    this.lastVP=null;
    this.lastStatic='';
    this.epoch=0;
    this.stable=false;
    this.frame=0;
  }
  begin(vp,staticSignature){
    this.frame++;
    this.stable=this.lastVP!==null&&vp.every((v,i)=>Math.abs(v-this.lastVP[i])<1e-6)&&this.lastStatic===staticSignature;
    if(!this.stable)this.epoch++;
    this.lastVP=vp.slice();
    this.lastStatic=staticSignature;
    const gl=this.gl;
    for(const e of this.entries.values())if(e.pending&&gl.getQueryParameter(e.query,gl.QUERY_RESULT_AVAILABLE)){
      const visible=gl.getQueryParameter(e.query,gl.QUERY_RESULT);
      e.visible=e.epoch===this.epoch?visible:true;
      e.pending=false;
      e.resultFrame=this.frame;
    }
  }
  visible(object){
    if(!object.static||!this.stable)return true;
    const e=this.entries.get(object.id);
    return !e||e.epoch!==this.epoch||e.pending&&this.frame-e.resultFrame>2?true:e.visible;
  }
  issue(object,program,eye){
    if(!object.static)return;
    const gl=this.gl;
    let e=this.entries.get(object.id);
    if(!e){
      e={
        query:gl.createQuery(),pending:false,visible:true,resultFrame:0
      };
      this.entries.set(object.id,e);
    }
    if(e.pending)return;
    const b=object.bounds.clone().expand(.04);
    if(eye.x>=b.min.x&&eye.x<=b.max.x&&eye.y>=b.min.y&&eye.y<=b.max.y&&eye.z>=b.min.z&&eye.z<=b.max.z){
      e.visible=true;
      return;
    }
    program.i('uInstanced', 0).m4('uModel',Mat4.compose(b.center(),new Quat(),b.extent().scale(.5)));
    gl.beginQuery(gl.ANY_SAMPLES_PASSED_CONSERVATIVE,e.query);
    this.mesh.draw();
    gl.endQuery(gl.ANY_SAMPLES_PASSED_CONSERVATIVE);
    e.pending=true;
    e.epoch=this.epoch;
  }
  dispose(){
    for(const e of this.entries.values())this.gl.deleteQuery(e.query);
    this.entries.clear();
  }
}
class GPUFrameTimer {
  constructor(gl){
    this.gl=gl;
    this.ext=gl.getExtension('EXT_disjoint_timer_query_webgl2');
    this.pending=[];
    this.current=null;
    this.ms=null;
  }
  begin(){
    if(!this.ext||this.pending.length>=4)return;
    this.current=this.gl.createQuery();
    this.gl.beginQuery(this.ext.TIME_ELAPSED_EXT,this.current);
  }
  end(){
    if(!this.current)return;
    this.gl.endQuery(this.ext.TIME_ELAPSED_EXT);
    this.pending.push(this.current);
    this.current=null;
  }
  poll(){
    const g=this.gl;
    if(!this.ext)return;
    if(g.getParameter(this.ext.GPU_DISJOINT_EXT)){
      for(const q of this.pending)g.deleteQuery(q);
      this.pending=[];
      this.ms=null;
      return;
    }
    while(this.pending.length&&g.getQueryParameter(this.pending[0],g.QUERY_RESULT_AVAILABLE)){
      const q=this.pending.shift();
      this.ms=g.getQueryParameter(q,g.QUERY_RESULT)/1e6;
      g.deleteQuery(q);
    }
  }
  dispose(){
    if(this.current)this.gl.endQuery(this.ext.TIME_ELAPSED_EXT);
    for(const q of [...this.pending,...(this.current?[this.current]:[])])this.gl.deleteQuery(q);
    this.pending=[];
    this.current=null;
  }
}

export class Renderer {
  constructor(canvas){
    this.canvas=canvas;
    const gl=canvas.getContext('webgl2',{
      antialias:false,alpha:false,powerPreference:'high-performance'
    });
    if(!gl)throw new Error('WebGL 2 is required. Enable browser hardware acceleration.');
    this.gl=gl;
    if(!gl.getExtension('EXT_color_buffer_float'))throw new Error('EXT_color_buffer_float is required for the HDR G-buffer.');
    if(gl.getParameter(gl.MAX_TEXTURE_IMAGE_UNITS)<11)throw new Error('At least 11 fragment texture units are required.');
    this.programs={
      geometry:new Program(gl,shader.meshVS,shader.geometryFS),shadow:new Program(gl,shader.shadowVS,shader.shadowFS),query:new Program(gl,shader.shadowVS,shader.queryFS),gi:new Program(gl,shader.fullscreenVS,shader.giFS),lighting:new Program(gl,shader.fullscreenVS,shader.lightingFS),rain:new Program(gl,shader.rainVS,shader.rainFS),aa:new Program(gl,shader.fullscreenVS,shader.aaFS)
    };
    this.meshes=new Map();
    this.textures=[];
    this.framebuffers=[];
    this.fullscreenVAO=gl.createVertexArray();
    this.streamer=new TextureStreamer(gl);
    this.textureLeases=[];
    this.lod=new LODSelector({
      pixelThreshold:5
    });
    this.timer=new GPUFrameTimer(gl);
    this.occlusion=new GPUOcclusion(gl,this.mesh(MeshData.box()));
    this.enableOcclusion=true;
    this.white=this.texture({
      width:1,height:1,internal:gl.RGBA8,format:gl.RGBA,type:gl.UNSIGNED_BYTE,data:new Uint8Array([255,255,255,255])
    });
    this.wetTexture=this.texture({
      width:128,height:128,internal:gl.R8,format:gl.RED,type:gl.UNSIGNED_BYTE,data:new Uint8Array(128*128),linear:true
    });
    this.dataWidth=Math.min(1024,gl.getParameter(gl.MAX_TEXTURE_SIZE));
    this.bvhTexture=this.texture({
      width:this.dataWidth,height:1,internal:gl.RGBA32F,format:gl.RGBA,type:gl.FLOAT,data:null
    });
    this.triangleTexture=this.texture({
      width:this.dataWidth,height:1,internal:gl.RGBA32F,format:gl.RGBA,type:gl.FLOAT,data:null
    });
    this.traceBVH=new BVH();
    this.traceItems=[];
    this.traceSignature='';
    this.shadowSize=1536;
    this.shadowDepth=this.texture({
      width:this.shadowSize,height:this.shadowSize,internal:gl.DEPTH_COMPONENT24,format:gl.DEPTH_COMPONENT,type:gl.UNSIGNED_INT
    });
    this.shadowFBO=this.framebuffer([],this.shadowDepth);
    this.width=0;
    this.height=0;
    this.targets=[];
    this.settings={
      scale:.8,gi:true,fog:true,ssr:true,shadowMode:0,debug:0,exposure:1.1,fogDensity:.005
    };
    this.stats={
    };
    this.requestedTiers=new Set();
    this.ensureTexture(32);
  }
  mesh(data){
    if(!this.meshes.has(data))this.meshes.set(data,new GPUMesh(this.gl,data));
    return this.meshes.get(data);
  }
  releaseObject(object) {
    for (const level of object.lods) { const mesh = this.meshes.get(level.mesh); if (mesh) { mesh.dispose(); this.meshes.delete(level.mesh); } }
    this.lod.remove(object.id);
    const entry = this.occlusion.entries.get(object.id); if (entry) { this.gl.deleteQuery(entry.query); this.occlusion.entries.delete(object.id); }
  }
  ensureTexture(size){
    if(this.requestedTiers.has(size))return;
    this.requestedTiers.add(size);
    const lease=this.streamer.request(`./assets/asphalt-${size}.png`,size,{
      priority:size===32?3:1,pin:size===32
    });
    lease.promise.catch(error=>this.streamer.failures.push(error.message));
    this.textureLeases.push(lease);
  }
  texture({
    width,height,internal,format,type,data=null,linear=false
  }){
    const g=this.gl,t=g.createTexture();
    g.bindTexture(g.TEXTURE_2D,t);
    g.texImage2D(g.TEXTURE_2D,0,internal,width,height,0,format,type,data);
    g.texParameteri(g.TEXTURE_2D,g.TEXTURE_MIN_FILTER,linear?g.LINEAR:g.NEAREST);
    g.texParameteri(g.TEXTURE_2D,g.TEXTURE_MAG_FILTER,linear?g.LINEAR:g.NEAREST);
    g.texParameteri(g.TEXTURE_2D,g.TEXTURE_WRAP_S,g.CLAMP_TO_EDGE);
    g.texParameteri(g.TEXTURE_2D,g.TEXTURE_WRAP_T,g.CLAMP_TO_EDGE);
    this.textures.push(t);
    return t;
  }
  framebuffer(colors,depth=null){
    const g=this.gl,f=g.createFramebuffer();
    g.bindFramebuffer(g.FRAMEBUFFER,f);
    colors.forEach((t,i)=>g.framebufferTexture2D(g.FRAMEBUFFER,g.COLOR_ATTACHMENT0+i,g.TEXTURE_2D,t,0));
    if(depth)g.framebufferTexture2D(g.FRAMEBUFFER,g.DEPTH_ATTACHMENT,g.TEXTURE_2D,depth,0);
    g.drawBuffers(colors.length?colors.map((_,i)=>g.COLOR_ATTACHMENT0+i):[g.NONE]);
    if(!colors.length)g.readBuffer(g.NONE);
    const status=g.checkFramebufferStatus(g.FRAMEBUFFER);
    if(status!==g.FRAMEBUFFER_COMPLETE){
      g.deleteFramebuffer(f);
      throw new Error(`Framebuffer incomplete: 0x${status.toString(16)}`);
    }
    this.framebuffers.push(f);
    return f;
  }
  resize(){
    const g=this.gl,ratio=Math.min(window.devicePixelRatio||1,1.5)*this.settings.scale,w=Math.max(64,Math.min(1280,Math.round(this.canvas.clientWidth*ratio))),h=Math.max(64,Math.min(900,Math.round(this.canvas.clientHeight*ratio)));
    if(w===this.width&&h===this.height)return;
    for(const {
      kind,handle
    }
    of this.targets){
      if(kind==='texture'){
        g.deleteTexture(handle);
        this.textures=this.textures.filter(t=>t!==handle);
      }else{
        g.deleteFramebuffer(handle);
        this.framebuffers=this.framebuffers.filter(f=>f!==handle);
      }
    }
    this.targets=[];
    this.width=w;
    this.height=h;
    this.canvas.width=w;
    this.canvas.height=h;
    const tex=(internal,format,type,width=w,height=h)=>{
      const t=this.texture({
        width,height,internal,format,type
      });
      this.targets.push({
        kind:'texture',handle:t
      });
      return t;
    };
    this.albedo=tex(g.RGBA8,g.RGBA,g.UNSIGNED_BYTE);
    this.normal=tex(g.RGBA16F,g.RGBA,g.HALF_FLOAT);
    this.emission=tex(g.RGBA16F,g.RGBA,g.HALF_FLOAT);
    this.depth=tex(g.DEPTH_COMPONENT24,g.DEPTH_COMPONENT,g.UNSIGNED_INT);
    this.gbuffer=this.framebuffer([this.albedo,this.normal,this.emission],this.depth);
    this.targets.push({
      kind:'fbo',handle:this.gbuffer
    });
    this.gi=tex(g.RGBA16F,g.RGBA,g.HALF_FLOAT,Math.ceil(w/2),Math.ceil(h/2));
    this.giFBO=this.framebuffer([this.gi]);
    this.targets.push({
      kind:'fbo',handle:this.giFBO
    });
    this.display = this.texture({width:w,height:h,internal:g.RGBA8,format:g.RGBA,type:g.UNSIGNED_BYTE,linear:true});
    this.targets.push({kind:'texture',handle:this.display}); this.displayFBO=this.framebuffer([this.display]);this.targets.push({kind:'fbo',handle:this.displayFBO});
  }
  uploadData(texture,array){
    const g=this.gl,height=Math.max(1,Math.ceil(array.length/4/this.dataWidth));
    if(height>g.getParameter(g.MAX_TEXTURE_SIZE))throw new RangeError('BVH texture exceeds hardware limits');
    const padded=new Float32Array(this.dataWidth*height*4);
    padded.set(array);
    g.bindTexture(g.TEXTURE_2D,texture);
    g.texImage2D(g.TEXTURE_2D,0,g.RGBA32F,this.dataWidth,height,0,g.RGBA,g.FLOAT,padded);
  }
  updateTraceScene(objects){
    const signature=objects.filter(o=>o.castShadow!==false && !o.instances).map(o=>`${o.id}:${(o.shadowMesh || o.lods[0].mesh).indices.length}`).join('|'),rebuild=signature!==this.traceSignature;
    let count=0;
    for(const object of objects){
      if(object.castShadow===false || object.instances)continue;
      const mesh=object.shadowMesh || object.lods[0].mesh;
      for(let i=0;i<mesh.indices.length;i+=3){
        let item=this.traceItems[count];
        if(!item){
          item={
            bounds:new AABB(),a:new Vec3(),b:new Vec3(),c:new Vec3()
          };
          this.traceItems[count]=item;
        }
        item.a.copy(Mat4.point(object.model,Vec3.from(mesh.positions,mesh.indices[i]*3)));
        item.b.copy(Mat4.point(object.model,Vec3.from(mesh.positions,mesh.indices[i+1]*3)));
        item.c.copy(Mat4.point(object.model,Vec3.from(mesh.positions,mesh.indices[i+2]*3)));
        item.bounds=new AABB().include(item.a).include(item.b).include(item.c);
        count++;
      }
    }
    this.traceItems.length=count;
    if(rebuild){
      this.traceBVH.rebuild(this.traceItems);
      this.traceSignature=signature;
    }else this.traceBVH.refit();
    if(this.traceBVH.nodes.length>16384)throw new RangeError('Software ray traversal supports up to 16384 BVH nodes; reduce shadow-casting geometry.');
    const pack=this.traceBVH.gpuPack(),triangles=new Float32Array(Math.max(1,pack.ordered.length)*12);
    pack.ordered.forEach((t,i)=>{
      t.a.toArray(triangles,i*12);
      t.b.toArray(triangles,i*12+4);
      t.c.toArray(triangles,i*12+8);
    });
    this.uploadData(this.bvhTexture,pack.nodes);
    this.uploadData(this.triangleTexture,triangles);
  }
  common(program,camera,sun,lightVP){
    return program.use().tex('uAlbedo',this.albedo,0).tex('uNormal',this.normal,1).tex('uEmission',this.emission,2).tex('uDepth',this.depth,3).tex('uShadow',this.shadowDepth,4).m4('uInvVP',camera.inverseVP).m4('uVP',camera.vp).m4('uView',camera.view).m4('uLightVP',lightVP).v3('uEye',camera.eye).v3('uSunDirection',sun.direction).v3('uSunColor',sun.color).f('uSunIntensity',sun.intensity);
  }
  render(objects,camera,sun,wet,time,arena=null){
    const start=performance.now(),g=this.gl;
    this.resize();
    camera.update(this.width/this.height);
    this.timer.poll();
    this.timer.begin();
    const frustum=new Frustum(camera.vp);
    this.occlusion.begin(camera.vp,objects.filter(o=>o.static).map(o=>`${o.id}:${Array.from(o.model).join(',')}`).join('|'));
    g.bindTexture(g.TEXTURE_2D,this.wetTexture);
    g.texSubImage2D(g.TEXTURE_2D,0,0,0,wet.size,wet.size,g.RED,g.UNSIGNED_BYTE,wet.bytes);
    const extent = camera.shadowExtent || 48, target = camera.shadowExtent ? camera.target.clone() : new Vec3(0, 3, 0), texel = extent * 2 / this.shadowSize;
    target.x = Math.round(target.x / texel) * texel; target.z = Math.round(target.z / texel) * texel;
    const lightEye=sun.direction.clone().scale(extent * 2).add(target),lightVP=Mat4.mul(Mat4.ortho(-extent,extent,-extent,extent,1,extent * 4.5),Mat4.lookAt(lightEye,target)),lightFrustum=new Frustum(lightVP);
    let draws=0,triangles=0,culled=0;
    g.enable(g.DEPTH_TEST);
    g.depthFunc(g.LESS);
    g.depthMask(true);
    g.disable(g.BLEND);
    g.enable(g.CULL_FACE);
    g.cullFace(g.BACK);
    g.bindFramebuffer(g.FRAMEBUFFER,this.shadowFBO);
    g.viewport(0,0,this.shadowSize,this.shadowSize);
    g.clear(g.DEPTH_BUFFER_BIT);
    g.enable(g.POLYGON_OFFSET_FILL);
    g.polygonOffset(1.5,2.5);
    const sp=this.programs.shadow.use().m4('uVP',lightVP);
    for(const o of objects){
      if(o.castShadow===false || o.instances && !o.instanceCount || !lightFrustum.intersects(o.bounds))continue;
      sp.i('uInstanced', o.instances ? 1 : 0).m4('uModel',o.model);
      if(o.doubleSided)g.disable(g.CULL_FACE);
      this.mesh(o.lods[0].mesh).draw(o.instances, o.instanceCount, `${o.id}:${o.instanceVersion}`);
      if(o.doubleSided)g.enable(g.CULL_FACE);
      draws++;
    }
    g.disable(g.POLYGON_OFFSET_FILL);
    if(this.settings.shadowMode===1)this.updateTraceScene(objects);
    g.bindFramebuffer(g.FRAMEBUFFER,this.gbuffer);
    g.viewport(0,0,this.width,this.height);
    g.clearColor(0,0,0,0);
    g.clear(g.COLOR_BUFFER_BIT|g.DEPTH_BUFFER_BIT);
    const p=this.programs.geometry.use().m4('uVP',camera.vp).tex('uWet',this.wetTexture,1).f('uWetExtent',wet.extent).v2('uWetOrigin', wet.origin?.x || 0, wet.origin?.z || 0).f('uNightFactor',Math.max(0,Math.min(1,(.15-sun.direction.y)/.28))).f('uTime',time);
    const distances=arena?arena.allocate(Float32Array,objects.length):new Float32Array(objects.length),ordered=arena?arena.allocate(Uint32Array,objects.length):new Uint32Array(objects.length);
    objects.forEach((o,i)=>{
      distances[i]=o.bounds.center().distance(camera.eye);
      ordered[i]=i;
    });
    ordered.sort((a,b)=>distances[a]-distances[b]);
    const drawObject=index=>{
      const o=objects[index];
      if (o.instances && !o.instanceCount) return;
      if(!frustum.intersects(o.bounds)||(this.enableOcclusion&&!this.occlusion.visible(o))){
        culled++;
        return;
      }
      const distance=distances[index],level=this.lod.select(o.id,o.lods,distance,this.height,camera.fov),mesh=o.lods[level].mesh,material=o.material;
      if(material.textured)this.ensureTexture(distance<15?512:distance<35?128:32);
      const tier=distance<15?512:distance<35?128:32,texture=material.textured?(this.streamer.get(`./assets/asphalt-${tier}.png`)||this.streamer.get('./assets/asphalt-32.png')||this.white):this.white;
      p.i('uInstanced',o.instances?1:0).i('uVertexMaterials', material.vertexMaterials ? 1 : 0).i('uWater',o.water?1:0).m4('uModel',o.model).v3('uColor',material.color).v3('uEmissive',material.emissive||[0,0,0]).f('uRoughness',material.roughness??.65).f('uMetal',material.metallic??0).f('uWettable',material.wettable??1).f('uUVScale',material.uvScale??1).tex('uAlbedoTex',texture,0);
      if(o.doubleSided)g.disable(g.CULL_FACE);
      this.mesh(mesh).draw(o.instances, o.instanceCount, `${o.id}:${o.instanceVersion}`);
      if(o.doubleSided)g.enable(g.CULL_FACE);
      draws++;
      triangles+=mesh.indices.length/3*(o.instances?o.instanceCount:1);
    };
    for(const index of ordered)if(objects[index].static)drawObject(index);
    if(this.enableOcclusion){
      g.colorMask(false,false,false,false);
      g.depthMask(false);
      g.disable(g.CULL_FACE);
      g.depthFunc(g.LEQUAL);
      const q=this.programs.query.use().m4('uVP',camera.vp);
      for(const o of objects)this.occlusion.issue(o,q,camera.eye);
      g.colorMask(true,true,true,true);
      g.depthMask(true);
      g.depthFunc(g.LESS);
      g.enable(g.CULL_FACE);
      p.use();
    }
    for(const index of ordered)if(!objects[index].static)drawObject(index);
    g.disable(g.DEPTH_TEST);
    g.disable(g.CULL_FACE);
    g.bindVertexArray(this.fullscreenVAO);
    g.bindFramebuffer(g.FRAMEBUFFER,this.giFBO);
    g.viewport(0,0,Math.ceil(this.width/2),Math.ceil(this.height/2));
    if(this.settings.gi){
      this.common(this.programs.gi,camera,sun,lightVP);
      g.drawArrays(g.TRIANGLES,0,3);
    }else{
      g.clearColor(0,0,0,0);
      g.clear(g.COLOR_BUFFER_BIT);
    }
    g.bindFramebuffer(g.FRAMEBUFFER,this.displayFBO);
    g.viewport(0,0,this.width,this.height);
    const lights = (sun.lights || []).slice(0,12), positions = new Float32Array(48), colors = new Float32Array(48);
    lights.forEach((l,i)=>{positions.set([l.x,l.y,l.z,l.radius],i*4); colors.set([...l.color,l.power],i*4);});
    this.common(this.programs.lighting,camera,sun,lightVP).tex('uGI',this.gi,5).tex('uBVH',this.bvhTexture,6).tex('uTriangles',this.triangleTexture,7).i('uDataWidth',this.dataWidth).i('uNodeCount',this.traceBVH.nodes.length).i('uShadowMode',this.settings.shadowMode).i('uEnableGI',this.settings.gi?1:0).i('uEnableFog',this.settings.fog?1:0).i('uEnableSSR',this.settings.ssr?1:0).i('uDebug',this.settings.debug).f('uRain',wet.rain).f('uExposure',this.settings.exposure + (sun.flash||0)).f('uFogDensity',this.settings.fogDensity).f('uClouds',sun.clouds||0).f('uGameTime',time).i('uPointCount',lights.length).v4array('uLightPositions[0]',positions).v4array('uLightColors[0]',colors);
    g.drawArrays(g.TRIANGLES,0,3);
    if(wet.rain>.01&&this.settings.debug===0){
      g.enable(g.BLEND);
      g.blendFunc(g.SRC_ALPHA,g.ONE_MINUS_SRC_ALPHA);
      this.programs.rain.use().m4('uVP',camera.vp).v3('uEye',camera.eye).f('uTime',time).f('uRain',wet.rain).tex('uDepth',this.depth,0).v2('uResolution',this.width,this.height);
      g.drawArrays(g.LINES,0,Math.floor(1800*wet.rain)*2);
      g.disable(g.BLEND);
    }
    g.bindFramebuffer(g.FRAMEBUFFER,null);g.bindVertexArray(this.fullscreenVAO);this.programs.aa.use().tex('uSource',this.display,0).v2('uResolution',this.width,this.height);g.drawArrays(g.TRIANGLES,0,3);
    this.timer.end();
    this.stats={
      draws,triangles,culled,objects:objects.length,cpuMs:performance.now()-start,gpuMs:this.timer.ms,width:this.width,height:this.height,textureBytes:this.streamer.cache.used,bvhNodes:this.traceBVH.nodes.length
    };
  }
  readback(){
    const g=this.gl,bytes=new Uint8Array(this.width*this.height*4);
    g.readPixels(0,0,this.width,this.height,g.RGBA,g.UNSIGNED_BYTE,bytes);
    return bytes;
  }
  dispose(){
    const g=this.gl;
    this.timer.dispose();
    this.occlusion.dispose();
    for(const lease of this.textureLeases)lease.release();
    this.streamer.dispose();
    for(const m of this.meshes.values())m.dispose();
    for(const p of Object.values(this.programs))p.dispose();
    for(const t of this.textures)g.deleteTexture(t);
    for(const f of this.framebuffers)g.deleteFramebuffer(f);
    g.deleteVertexArray(this.fullscreenVAO);
    this.meshes.clear();
    this.textures=[];
    this.framebuffers=[];
  }
}
```

## src/resources.js

```javascript
import { clamp, lerp } from './math.js';

export class BudgetedCache {
  constructor({
    budget=64*1024*1024,concurrency=2,dispose=v=>v?.close?.()
  } = {}) {
    if(budget<=0||concurrency<1)throw new RangeError('Invalid streaming configuration');
    this.budget=budget;
    this.concurrency=concurrency;
    this.dispose=dispose;
    this.entries=new Map();
    this.queue=[];
    this.used=0;
    this.reserved=0;
    this.active=0;
    this.stamp=0;
    this.closed=false;
  }
  request(key,{
    bytes,load,priority=0,pin=false
  }) {
    if(this.closed)throw new Error('Cache disposed');
    let e=this.entries.get(key);
    if(e){
      e.stamp=++this.stamp;
      e.priority=Math.max(e.priority,priority);
      if(pin)e.pins++;
      let released=false;
      return {
        promise:e.promise,release:()=>{
          if(!released){
            released=true;
            if(pin)this.unpin(e);
          }
        }
      };
    }
    if(!Number.isFinite(bytes)||bytes<=0||bytes>this.budget)throw new RangeError('Resource exceeds streaming budget');
    let resolve,reject;
    const promise=new Promise((a,b)=>{
      resolve=a;
      reject=b;
    });
    e={
      key,bytes,load,priority,pins:pin?1:0,stamp:++this.stamp,state:'queued',controller:new AbortController(),promise,resolve,reject,value:null
    };
    this.entries.set(key,e);
    this.queue.push(e);
    this.pump();
    let released=false;
    return {
      promise,release:()=>{
        if(!released){
          released=true;
          if(pin)this.unpin(e);
        }
      }
    };
  }
  unpin(e){
    e.pins=Math.max(0,e.pins-1);
    this.pump();
  }
  get(key){
    const e=this.entries.get(key);
    if(e?.state!=='resident')return null;
    e.stamp=++this.stamp;
    return e.value;
  }
  makeRoom(bytes){
    const candidates=[...this.entries.values()].filter(e=>e.state==='resident'&&!e.pins).sort((a,b)=>a.stamp-b.stamp);
    while(this.used+this.reserved+bytes>this.budget&&candidates.length){
      const e=candidates.shift();
      this.dispose(e.value);
      this.used-=e.bytes;
      this.entries.delete(e.key);
      e.state='evicted';
    }
    return this.used+this.reserved+bytes<=this.budget;
  }
  pump(){
    if(this.closed)return;
    this.queue.sort((a,b)=>b.priority-a.priority||a.stamp-b.stamp);
    while(this.active<this.concurrency&&this.queue.length){
      const index=this.queue.findIndex(e=>this.makeRoom(e.bytes));
      if(index<0)break;
      const e=this.queue.splice(index,1)[0];
      this.active++;
      this.reserved+=e.bytes;
      e.reserved=true;
      e.state='loading';
      Promise.resolve().then(()=>e.load(e.controller.signal)).then(value=>{
        if(this.closed||e.controller.signal.aborted||this.entries.get(e.key)!==e){
          this.dispose(value);
          throw new DOMException('Cancelled','AbortError');
        }
        this.reserved-=e.bytes;
        e.reserved=false;
        e.value=value;
        e.state='resident';
        this.used+=e.bytes;
        e.resolve(value);
      }).catch(error=>{
        if(this.entries.get(e.key)===e)this.entries.delete(e.key);
        e.state='failed';
        e.reject(error);
      }).finally(()=>{
        this.active--;
        if(e.reserved){
          this.reserved-=e.bytes;
          e.reserved=false;
        }
        this.pump();
      });
    }
  }
  cancel(key){
    const e=this.entries.get(key);
    if(!e)return;
    e.controller.abort();
    this.entries.delete(key);
    if(e.state==='queued'){
      this.queue=this.queue.filter(v=>v!==e);
      e.reject(new DOMException('Cancelled','AbortError'));
    }
    if(e.state==='resident'){
      this.used-=e.bytes;
      this.dispose(e.value);
    }
    e.state='cancelled';
    this.pump();
  }
  disposeAll(){
    this.closed=true;
    for(const key of [...this.entries.keys()])this.cancel(key);
    this.queue=[];
  }
}

export class TextureStreamer {
  constructor(gl,{
    budget=32*1024*1024,concurrency=2
  } = {}){
    this.gl=gl;
    this.cache=new BudgetedCache({
      budget,concurrency,dispose:texture=>gl.deleteTexture(texture)
    });
    this.failures=[];
  }
  request(url,size,{
    priority=0,pin=false
  } = {}){
    const gl=this.gl;
    return this.cache.request(url,{
      bytes:Math.ceil(size*size*4*4/3),priority,pin,load:async signal=>{
        const response=await fetch(url,{
          signal
        });
        if(!response.ok)throw new Error(`Texture HTTP ${response.status}: ${url}`);
        const image=await createImageBitmap(await response.blob(),{
          colorSpaceConversion:'none'
        });
        if(signal.aborted){
          image.close();
          throw new DOMException('Cancelled','AbortError');
        }
        if(image.width!==size||image.height!==size){
          image.close();
          throw new Error(`Texture dimensions do not match reservation for ${url}`);
        }
        const texture=gl.createTexture();
        try{
          gl.bindTexture(gl.TEXTURE_2D,texture);
          gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,false);
          gl.texImage2D(gl.TEXTURE_2D,0,gl.SRGB8_ALPHA8,gl.RGBA,gl.UNSIGNED_BYTE,image);
          gl.generateMipmap(gl.TEXTURE_2D);
          gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR_MIPMAP_LINEAR);
          gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);
          gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_S,gl.REPEAT);
          gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_WRAP_T,gl.REPEAT);
          return texture;
        }catch(error){
          gl.deleteTexture(texture);
          throw error;
        }finally{
          image.close();
        }
      }
    });
  }
  get(url){
    return this.cache.get(url);
  }
  dispose(){
    this.cache.disposeAll();
  }
}

export class LODSelector {
  constructor({
    pixelThreshold=2,hysteresis=.18
  } = {}){
    this.threshold=pixelThreshold;
    this.hysteresis=hysteresis;
    this.levels=new Map();
  }
  select(id,levels,distance,viewportHeight,fov){
    if(!levels.length)throw new Error('LOD list cannot be empty');
    const scale=viewportHeight/(2*Math.tan(fov/2)*Math.max(.01,distance)),current=this.levels.get(id)??0;
    let selected=current;
    while(selected+1<levels.length&&levels[selected+1].error*scale<this.threshold*(1-this.hysteresis))selected++;
    while(selected>0&&levels[selected].error*scale>this.threshold*(1+this.hysteresis))selected--;
    this.levels.set(id,selected);
    return selected;
  }
  remove(id){
    this.levels.delete(id);
  }
}

export class WetnessField {
  constructor({
    size=128,extent=45
  } = {}){
    this.size=size;
    this.extent=extent;
    this.water=new Float32Array(size*size);
    this.next=new Float32Array(size*size);
    this.bytes=new Uint8Array(size*size);
    this.rain=0;
    this.evaporation=.012;
    this.diffusion=.1;
    this.mean=0;
  }
  step(dt,sun=1){
    const n=this.size,a=Math.min(.24,this.diffusion*dt),dry=this.evaporation*lerp(.25,1,sun);
    let total=0;
    for(let z=0;z<n;z++)for(let x=0;x<n;x++){
      const i=z*n+x,c=this.water[i],l=this.water[z*n+Math.max(0,x-1)],r=this.water[z*n+Math.min(n-1,x+1)],u=this.water[Math.max(0,z-1)*n+x],d=this.water[Math.min(n-1,z+1)*n+x],permeability=.6+.4*Math.sin(x*.17)*Math.cos(z*.23),value=clamp(c+(this.rain*.22*permeability-dry)*dt+a*(l+r+u+d-4*c),0,1);
      this.next[i]=value;
      this.bytes[i]=Math.round(value*255);
      total+=value;
    }
    [this.water,this.next]=[this.next,this.water];
    this.mean=total/this.water.length;
  }
  sample(x,z){
    const u=clamp(Math.floor((x/(this.extent*2)+.5)*this.size),0,this.size-1),v=clamp(Math.floor((z/(this.extent*2)+.5)*this.size),0,this.size-1);
    return this.water[v*this.size+u];
  }
}
```

## src/shaders.js

```javascript
export const fullscreenVS=`#version 300 es
precision highp float;
out vec2 vUV;
void main(){vec2 p=vec2(float((gl_VertexID<<1)&2),float(gl_VertexID&2));vUV=p;gl_Position=vec4(p*2.0-1.0,0.0,1.0);}`;
export const meshVS=`#version 300 es
precision highp float;
layout(location=0) in vec3 aPosition;
layout(location=1) in vec3 aNormal;
layout(location=2) in vec2 aUV;
layout(location=3) in vec3 aColor;
layout(location=4) in vec4 aMaterial;
layout(location=5) in mat4 aInstanceModel;
layout(location=9) in vec4 aInstanceColor;
layout(location=10) in vec4 aInstanceMaterial;
uniform mat4 uModel,uVP;
uniform int uInstanced,uVertexMaterials,uWater;
uniform float uTime;
out vec3 vWorld,vNormal,vColor;
out vec4 vMaterial;
out vec2 vUV;
void main(){
  mat4 model=uInstanced==1?aInstanceModel:uModel;
  vec4 p=model*vec4(aPosition,1.0);vec3 normal=transpose(inverse(mat3(model)))*aNormal;
  if(uWater==1){float a=p.x*.12+uTime*.7,b=p.z*.19-uTime*.5;p.y+=sin(a)*.045+sin(b)*.035;normal=normalize(vec3(-cos(a)*.0054,1.0,-cos(b)*.00665));}
  vWorld=p.xyz;vNormal=normal;vUV=aUV;vColor=uVertexMaterials==1?aColor:vec3(1.0);vMaterial=aMaterial;
  if(uInstanced==1){vColor=aInstanceColor.rgb;vMaterial=aInstanceMaterial;}
  gl_Position=uVP*p;
}`;
export const geometryFS=`#version 300 es
precision highp float;precision highp int;
in vec3 vWorld,vNormal,vColor;in vec2 vUV;in vec4 vMaterial;
uniform vec3 uColor,uEmissive;
uniform float uRoughness,uMetal,uWettable,uUVScale,uWetExtent,uNightFactor;
uniform vec2 uWetOrigin;
uniform int uVertexMaterials;
uniform sampler2D uAlbedoTex,uWet;
layout(location=0) out vec4 gAlbedo;
layout(location=1) out vec4 gNormal;
layout(location=2) out vec4 gEmission;
void main(){
  vec3 normal=normalize(vNormal);if(!gl_FrontFacing)normal=-normal;
  float rough=uVertexMaterials==1?vMaterial.x:uRoughness,metal=uVertexMaterials==1?vMaterial.y:uMetal,wettable=uVertexMaterials==1?vMaterial.w:uWettable;
  float wet=texture(uWet,(vWorld.xz-uWetOrigin)/(uWetExtent*2.0)+.5).r*wettable*max(normal.y,0.0);
  vec3 color=uColor*vColor*texture(uAlbedoTex,vUV*uUVScale).rgb;
  float grain=fract(sin(dot(floor(vWorld*5.0),vec3(127.1,311.7,74.7)))*43758.5453);
  color*=mix(1.0,.62,wet)*(.965+.035*grain);
  vec3 emission=uEmissive;
  if(uVertexMaterials==1)emission+=(vMaterial.z<0.0?mix(vColor,vec3(1.0,.55,.19),.85):vColor)*abs(vMaterial.z)*(vMaterial.z<0.0?uNightFactor:1.0);
  gAlbedo=vec4(color,metal);gNormal=vec4(normal*.5+.5,mix(max(.045,rough),max(.055,rough*.24),wet));gEmission=vec4(emission,wet);
}`;
export const shadowVS=`#version 300 es
precision highp float;
layout(location=0)in vec3 aPosition;layout(location=5)in mat4 aInstanceModel;
uniform mat4 uModel,uVP;uniform int uInstanced;
void main(){gl_Position=uVP*(uInstanced==1?aInstanceModel:uModel)*vec4(aPosition,1.0);}`;
export const shadowFS=`#version 300 es
precision highp float;void main(){}`;
export const queryFS=`#version 300 es
precision highp float;out vec4 color;void main(){color=vec4(0.0);}`;
const sceneGLSL=`
const float PI=3.141592653589793;
uniform sampler2D uDepth,uAlbedo,uNormal,uEmission,uShadow;
uniform mat4 uInvVP,uVP,uView,uLightVP;
uniform vec3 uEye,uSunDirection,uSunColor;
uniform float uSunIntensity;
vec3 worldAt(vec2 uv){float d=texture(uDepth,uv).r;vec4 p=uInvVP*vec4(uv*2.0-1.0,d*2.0-1.0,1.0);return p.xyz/p.w;}
float mapShadow(vec3 p,vec3 n){
  vec4 l=uLightVP*vec4(p+n*.018,1.0);vec3 uv=l.xyz/l.w*.5+.5;
  if(any(lessThan(uv,vec3(0.0)))||any(greaterThan(uv,vec3(1.0))))return 1.0;
  vec2 px=1.0/vec2(textureSize(uShadow,0));float lit=0.0,bias=max(.00007,.0004*(1.0-dot(n,uSunDirection)));
  for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)lit+=uv.z-bias<=texture(uShadow,uv.xy+vec2(x,y)*px).r?1.0:0.0;
  return lit/9.0;
}
vec3 simpleLight(vec2 uv){vec3 n=normalize(texture(uNormal,uv).xyz*2.0-1.0);vec3 albedo=texture(uAlbedo,uv).rgb;return albedo*(vec3(.025,.035,.055)+uSunColor*uSunIntensity*max(0.0,dot(n,uSunDirection))/PI)+texture(uEmission,uv).rgb;}
bool screenRay(vec3 origin,vec3 dir,float stride,int steps,out vec2 hitUV){
  for(int i=1;i<=32;i++){if(i>steps)break;vec3 p=origin+dir*(float(i)*stride+.06*float(i*i));vec4 clip=uVP*vec4(p,1.0);if(clip.w<=0.0)break;vec2 uv=clip.xy/clip.w*.5+.5;if(any(lessThanEqual(uv,vec2(.003)))||any(greaterThanEqual(uv,vec2(.997))))break;
    if(texture(uDepth,uv).r>.99999)continue;vec3 scene=worldAt(uv);float delta=-(uView*vec4(p,1.0)).z+(uView*vec4(scene,1.0)).z;
    if(delta>0.0&&delta<.5+stride*.4&&distance(origin,scene)>.18){hitUV=uv;return true;}
  }return false;
}
`;
export const giFS=`#version 300 es
precision highp float;precision highp int;
in vec2 vUV;out vec4 outGI;
${sceneGLSL}
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
void main(){
  if(texture(uDepth,vUV).r>.99999){outGI=vec4(0.0);return;}
  vec3 p=worldAt(vUV),n=normalize(texture(uNormal,vUV).xyz*2.0-1.0),t=normalize(cross(abs(n.y)<.95?vec3(0,1,0):vec3(1,0,0),n)),b=cross(n,t),sum=vec3(0.0);float angle=hash(floor(gl_FragCoord.xy))*2.0*PI;
  for(int i=0;i<4;i++){float u=(float(i)+.5)/4.0,phi=angle+float(i)*2.39996323;vec3 d=t*cos(phi)*sqrt(u)+b*sin(phi)*sqrt(u)+n*sqrt(1.0-u);vec2 hit;
    if(screenRay(p+n*.12,d,.3,12,hit)){vec3 hn=normalize(texture(uNormal,hit).xyz*2.0-1.0);float valid=max(0.0,dot(hn,-d));sum+=simpleLight(hit)*valid;}
  }outGI=vec4(sum/4.0,1.0);
}`;
const traceGLSL=`
uniform sampler2D uBVH,uTriangles;
uniform int uNodeCount,uDataWidth,uShadowMode;
vec4 dataAt(sampler2D tex,int index){return texelFetch(tex,ivec2(index%uDataWidth,index/uDataWidth),0);}
bool rayBox(vec3 origin,vec3 dir,vec3 lo,vec3 hi){
  float nearT=0.0,farT=300.0;
  for(int j=0;j<3;j++){if(abs(dir[j])<1e-8){if(origin[j]<lo[j]||origin[j]>hi[j])return false;}else{float a=(lo[j]-origin[j])/dir[j],b=(hi[j]-origin[j])/dir[j];nearT=max(nearT,min(a,b));farT=min(farT,max(a,b));if(nearT>farT)return false;}}
  return true;
}
bool rayTriangle(vec3 o,vec3 d,int index){
  vec3 a=dataAt(uTriangles,index*3).xyz,b=dataAt(uTriangles,index*3+1).xyz,c=dataAt(uTriangles,index*3+2).xyz,e1=b-a,e2=c-a,p=cross(d,e2);float det=dot(e1,p);if(abs(det)<1e-8)return false;
  vec3 s=o-a;float u=dot(s,p)/det;if(u<0.0||u>1.0)return false;vec3 q=cross(s,e1);float v=dot(d,q)/det;if(v<0.0||u+v>1.0)return false;float t=dot(e2,q)/det;return t>.008&&t<300.0;
}
float tracedShadow(vec3 p,vec3 n){
  vec3 o=p+n*.035;int node=0;
  for(int it=0;it<16384;it++){if(node>=uNodeCount)break;vec4 lo=dataAt(uBVH,node*3),hi=dataAt(uBVH,node*3+1),meta=dataAt(uBVH,node*3+2);
    if(!rayBox(o,uSunDirection,lo.xyz,hi.xyz)){node=int(lo.w);continue;}
    int count=int(hi.w),first=int(meta.x);for(int j=0;j<4;j++){if(j>=count)break;if(rayTriangle(o,uSunDirection,first+j))return 0.0;}node++;
  }return 1.0;
}
`;
const atmosphereGLSL=`
uniform float uClouds,uGameTime;
float cloudHash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float cloudNoise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);return mix(mix(cloudHash(i),cloudHash(i+vec2(1,0)),f.x),mix(cloudHash(i+vec2(0,1)),cloudHash(i+vec2(1,1)),f.x),f.y);}
vec2 sphereInterval(vec3 o,vec3 d,float r){float b=dot(o,d),c=dot(o,o)-r*r,h=b*b-c;if(h<0.0)return vec2(-1.0);return vec2(-b-sqrt(h),-b+sqrt(h));}
vec3 atmosphere(vec3 direction){
  const float earth=6360000.0,outer=6460000.0;const vec3 betaR=vec3(5.8e-6,13.5e-6,33.1e-6);const float betaM=21e-6;
  vec3 o=vec3(0.0,earth+max(uEye.y,2.0),0.0);vec2 air=sphereInterval(o,direction,outer),ground=sphereInterval(o,direction,earth);float end=air.y;
  if(ground.x>0.0)end=min(end,ground.x);end=max(0.0,end);float ds=end/8.0,odR=0.0,odM=0.0;vec3 sumR=vec3(0.0),sumM=vec3(0.0);
  float mu=dot(direction,uSunDirection),phaseR=3.0/(16.0*PI)*(1.0+mu*mu),g=.76,phaseM=(1.0-g*g)/(4.0*PI*pow(max(.01,1.0+g*g-2.0*g*mu),1.5));
  for(int i=0;i<8;i++){vec3 p=o+direction*((float(i)+.5)*ds);float h=max(0.0,length(p)-earth),r=exp(-h/8000.0)*ds,m=exp(-h/1200.0)*ds;odR+=r*.5;odM+=m*.5;
    vec2 light=sphereInterval(p,uSunDirection,outer),planet=sphereInterval(p,uSunDirection,earth);float stepL=max(0.0,light.y)/4.0,lr=0.0,lm=0.0;
    if(planet.x<0.0){for(int j=0;j<4;j++){float lh=max(0.0,length(p+uSunDirection*((float(j)+.5)*stepL))-earth);lr+=exp(-lh/8000.0)*stepL;lm+=exp(-lh/1200.0)*stepL;}
      vec3 trans=exp(-(betaR*(odR+lr)+vec3(betaM)*(odM+lm)));sumR+=trans*r;sumM+=trans*m;}
    odR+=r*.5;odM+=m*.5;
  }
  vec3 sky=22.0*(sumR*betaR*phaseR+sumM*betaM*phaseM)+vec3(.001,.002,.006);
  if(ground.x<0.0&&dot(direction,uSunDirection)>.9998&&uSunDirection.y>-.03)sky+=uSunColor*8.0;
  float stars=pow(fract(sin(dot(floor(direction.xz/max(.08,abs(direction.y))*700.0),vec2(127.1,311.7)))*43758.5453),150.0)*(1.0-smoothstep(-.25,.0,uSunDirection.y))*max(0.0,direction.y);
  vec3 result=sky+vec3(stars*.3);
  if(direction.y>.02&&uClouds>0.0){vec2 q=direction.xz/direction.y*2.8+vec2(uGameTime*.006,uGameTime*.003);float noise=cloudNoise(q)*.58+cloudNoise(q*2.0)*.28+cloudNoise(q*4.0)*.14;float cover=smoothstep(.74-uClouds*.42,.88-uClouds*.28,noise);vec3 cloudColor=mix(vec3(.07,.09,.13),vec3(.55,.58,.58),max(0.0,uSunDirection.y));result=mix(result,cloudColor,cover*min(1.0,direction.y*12.0));}
  return result;
}
`;
export const lightingFS=`#version 300 es
precision highp float;precision highp int;
in vec2 vUV;out vec4 outColor;
${sceneGLSL}
${traceGLSL}
${atmosphereGLSL}
uniform sampler2D uGI;
uniform float uRain,uExposure,uFogDensity;
uniform int uEnableGI,uEnableFog,uEnableSSR,uDebug;
uniform int uPointCount;
uniform vec4 uLightPositions[12];
uniform vec4 uLightColors[12];
vec3 fresnel(float cosTheta,vec3 f0){return f0+(1.0-f0)*pow(1.0-cosTheta,5.0);}
vec3 brdf(vec3 n,vec3 v,vec3 l,vec3 albedo,float metallic,float roughness){
  if(dot(n,l)<=0.0)return vec3(0.0);
  vec3 h=normalize(v+l),f0=mix(vec3(.04),albedo,metallic),F=fresnel(max(0.0,dot(v,h)),f0);float nl=max(0.0,dot(n,l)),nv=max(.0001,dot(n,v)),nh=max(0.0,dot(n,h)),a=roughness*roughness,a2=a*a,denom=nh*nh*(a2-1.0)+1.0,D=a2/(PI*denom*denom),Gv=2.0*nv/(nv+sqrt(a2+(1.0-a2)*nv*nv)),Gl=2.0*nl/max(.0001,nl+sqrt(a2+(1.0-a2)*nl*nl));
  return ((1.0-F)*(1.0-metallic)*albedo/PI+D*Gv*Gl*F/max(.0001,4.0*nv*nl))*nl;
}
vec3 bilateralGI(vec2 uv,vec3 p,vec3 n){vec2 px=1.0/vec2(textureSize(uGI,0));vec3 sum=vec3(0.0);float total=0.0;
  for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++){vec2 q=uv+vec2(x,y)*px;vec3 qn=normalize(texture(uNormal,q).xyz*2.0-1.0);float w=exp(-distance(p,worldAt(q))*3.0)*pow(max(0.0,dot(n,qn)),8.0);sum+=texture(uGI,q).rgb*w;total+=w;}
  return sum/max(.0001,total);
}
vec3 tone(vec3 x){x=max(vec3(0.0),x*uExposure);x=clamp((x*(2.51*x+.03))/(x*(2.43*x+.59)+.14),0.0,1.0);return mix(x*12.92,1.055*pow(x,vec3(1.0/2.4))-.055,step(vec3(.0031308),x));}
void main(){
  float depth=texture(uDepth,vUV).r;vec3 p=worldAt(vUV),ray=normalize(p-uEye),color;
  if(depth>.99999){color=atmosphere(ray);}
  else{
    vec4 am=texture(uAlbedo,vUV),nr=texture(uNormal,vUV),em=texture(uEmission,vUV);vec3 n=normalize(nr.xyz*2.0-1.0),v=normalize(uEye-p);float roughness=nr.w,metallic=am.w;
    float visibility=uSunDirection.y>0.0?(uShadowMode==1?tracedShadow(p,n):mapShadow(p,n)):0.0;
    vec3 f0=mix(vec3(.04),am.rgb,metallic),F=fresnel(max(0.0,dot(n,v)),f0),skyAmbient=vec3(.025,.04,.065)+vec3(.045,.055,.065)*max(0.0,uSunDirection.y);
    color=brdf(n,v,uSunDirection,am.rgb,metallic,roughness)*uSunColor*uSunIntensity*visibility+skyAmbient*am.rgb*(1.0-metallic)+em.rgb;
    for(int li=0;li<12;li++){if(li>=uPointCount)break;vec3 delta=uLightPositions[li].xyz-p;float dist=length(delta),range=uLightPositions[li].w;float falloff=pow(clamp(1.0-pow(dist/range,4.0),0.0,1.0),2.0)/max(1.0,dist*dist);color+=brdf(n,v,delta/max(.001,dist),am.rgb,metallic,roughness)*uLightColors[li].rgb*uLightColors[li].a*falloff;}
    if(uEnableGI==1)color+=bilateralGI(vUV,p,n)*am.rgb*(1.0-metallic)*.8;
    vec3 reflectionDir=reflect(-v,n),reflection=skyAmbient*1.8+pow(max(0.0,dot(reflectionDir,uSunDirection)),mix(800.0,5.0,roughness))*uSunColor*uSunIntensity;
    if(uEnableSSR==1&&(em.a>.02||metallic>.2)){vec2 hit;if(screenRay(p+n*.1,reflectionDir,.3,24,hit)){float edge=min(min(hit.x,hit.y),min(1.0-hit.x,1.0-hit.y));reflection=mix(reflection,simpleLight(hit),smoothstep(0.0,.08,edge));}}
    color+=reflection*F*(1.0-roughness*.8);
    if(uDebug==1)color=am.rgb;if(uDebug==2)color=n*.5+.5;if(uDebug==3)color=vec3(visibility);if(uDebug==4)color=vec3(em.a);if(uDebug==5)color=texture(uGI,vUV).rgb;
  }
  if(uEnableFog==1&&uDebug==0){float distanceToSurface=depth>.99999?170.0:min(170.0,distance(p,uEye)),ds=distanceToSurface/12.0,trans=1.0;vec3 fog=vec3(0.0);float g=.52,mu=dot(ray,uSunDirection),phase=(1.0-g*g)/(4.0*PI*pow(1.0+g*g-2.0*g*mu,1.5));
    for(int i=0;i<12;i++){vec3 q=uEye+ray*((float(i)+.5)*ds);float density=uFogDensity*(1.0+uRain*1.5)*exp(-max(0.0,q.y)*.055),segment=exp(-density*ds),lit=mapShadow(q,vec3(0,1,0));vec3 source=vec3(.12,.17,.23)+uSunColor*uSunIntensity*phase*lit;fog+=trans*(1.0-segment)*source;trans*=segment;}
    color=color*trans+fog;
  }outColor=vec4(tone(color),1.0);
}`;
export const rainVS=`#version 300 es
precision highp float;uniform mat4 uVP;uniform vec3 uEye;uniform float uTime,uRain;out float vAlpha;
float hash(float n){return fract(sin(n*127.1)*43758.5453);}
void main(){float id=float(gl_VertexID/2),tip=float(gl_VertexID%2);vec3 p=vec3((hash(id+1.0)-.5)*55.0,mod(hash(id+2.0)*28.0-uTime*15.0,28.0),(hash(id+3.0)-.5)*55.0);p.xz+=uEye.xz;p+=vec3(.07,-.55,.03)*tip;gl_Position=uVP*vec4(p,1.0);vAlpha=.18*uRain;}`;
export const rainFS=`#version 300 es
precision highp float;uniform sampler2D uDepth;uniform vec2 uResolution;in float vAlpha;out vec4 outColor;void main(){float scene=texture(uDepth,gl_FragCoord.xy/uResolution).r;if(gl_FragCoord.z>scene+.0001)discard;outColor=vec4(.62,.78,.94,vAlpha);}`;
export const aaFS=`#version 300 es
precision highp float;
in vec2 vUV;out vec4 outColor;uniform sampler2D uSource;uniform vec2 uResolution;
float luma(vec3 c){return dot(c,vec3(.2126,.7152,.0722));}
void main(){
  vec2 px=1.0/uResolution;vec3 m=texture(uSource,vUV).rgb,n=texture(uSource,vUV+vec2(0,px.y)).rgb,s=texture(uSource,vUV-vec2(0,px.y)).rgb,e=texture(uSource,vUV+vec2(px.x,0)).rgb,w=texture(uSource,vUV-vec2(px.x,0)).rgb;
  float lm=luma(m),ln=luma(n),ls=luma(s),le=luma(e),lw=luma(w),hi=max(lm,max(max(ln,ls),max(le,lw))),lo=min(lm,min(min(ln,ls),min(le,lw))),range=hi-lo;
  if(range<max(.025,hi*.12)){outColor=vec4(m,1.0);return;}
  vec2 direction=abs(le-lw)>abs(ln-ls)?vec2(px.x,0):vec2(0,px.y);
  vec3 filtered=(texture(uSource,vUV+direction*.5).rgb+texture(uSource,vUV-direction*.5).rgb)*.5;
  outColor=vec4(mix(m,filtered,min(.8,range/max(.04,hi))),1.0);
}`;
```

## src/spatial.js

```javascript
import { Vec3, AABB, EPS } from './math.js';
export function rayTriangle(origin,dir,a,b,c,tMax=Infinity) {
  const e1=Vec3.sub(b,a),e2=Vec3.sub(c,a),p=Vec3.cross(dir,e2),det=e1.dot(p);
  if(Math.abs(det)<EPS)return null;
  const inv=1/det,s=Vec3.sub(origin,a),u=s.dot(p)*inv;
  if(u<0||u>1)return null;
  const q=Vec3.cross(s,e1),v=dir.dot(q)*inv;
  if(v<0||u+v>1)return null;
  const distance=e2.dot(q)*inv;
  if(distance<1e-6||distance>tMax)return null;
  return {
    distance,u,v,point:origin.clone().addScaled(dir,distance),normal:Vec3.cross(e1,e2).normalize()
  };
}

export class BVH {
  constructor(items=[],{
    leafSize=4,bins=12
  } = {}) {
    this.leafSize=leafSize;
    this.bins=bins;
    this.nodes=[];
    this.ordered=[];
    this.rebuild(items);
  }
  rebuild(items) {
    this.nodes=[];
    this.ordered=[];
    const build=(group)=>{
      const index=this.nodes.length,node={
        bounds:new AABB(),left:-1,right:-1,first:0,count:0,skip:0
      };
      this.nodes.push(node);
      for(const v of group)node.bounds.union(v.bounds);
      if(group.length<=this.leafSize){
        node.first=this.ordered.length;
        node.count=group.length;
        this.ordered.push(...group);
      } else {
        const centers=new AABB();
        for(const v of group)centers.include(v.bounds.center());
        let cost=Infinity,axis=null,cut=0;
        for(const k of ['x','y','z']){
          const span=centers.max[k]-centers.min[k];
          if(span<1e-9)continue;
          const buckets=Array.from({
            length:this.bins
          },()=>({
            bounds:new AABB(),count:0
          }));
          for(const v of group){
            const b=Math.min(this.bins-1,Math.floor((v.bounds.center()[k]-centers.min[k])/span*this.bins));
            buckets[b].count++;
            buckets[b].bounds.union(v.bounds);
          }
          for(let s=1;s<this.bins;s++){
            const l=new AABB(),r=new AABB();
            let nl=0,nr=0;
            for(let b=0;b<this.bins;b++)if(buckets[b].count){
              if(b<s){
                l.union(buckets[b].bounds);
                nl+=buckets[b].count;
              }else{
                r.union(buckets[b].bounds);
                nr+=buckets[b].count;
              }
            }
            const candidate=l.area()*nl+r.area()*nr;
            if(nl&&nr&&candidate<cost){
              cost=candidate;
              axis=k;
              cut=centers.min[k]+span*s/this.bins;
            }
          }
        }
        let l=axis?group.filter(v=>v.bounds.center()[axis]<cut):[],r=axis?group.filter(v=>v.bounds.center()[axis]>=cut):[];
        if(!l.length||!r.length){
          const sorted=[...group].sort((a,b)=>a.bounds.center().x-b.bounds.center().x);
          const mid=sorted.length>>1;
          l=sorted.slice(0,mid);
          r=sorted.slice(mid);
        }
        node.left=build(l);
        node.right=build(r);
      }
      node.skip=this.nodes.length;
      return index;
    };
    if(items.length)build(items);
    return this;
  }
  refit() {
    for(let i=this.nodes.length-1;i>=0;i--){
      const n=this.nodes[i];
      n.bounds=new AABB();
      if(n.count){
        for(let j=0;j<n.count;j++)n.bounds.union(this.ordered[n.first+j].bounds);
      }else n.bounds.union(this.nodes[n.left].bounds).union(this.nodes[n.right].bounds);
    }
  }
  query(bounds,visit) {
    let i=0;
    while(i<this.nodes.length){
      const n=this.nodes[i];
      if(!n.bounds.overlaps(bounds)){
        i=n.skip;
        continue;
      }
      if(n.count)for(let j=0;j<n.count;j++){
        const v=this.ordered[n.first+j];
        if(v.bounds.overlaps(bounds))visit(v);
      }
      i++;
    }
  }
  raycast(origin,dir,intersect,tMax=Infinity) {
    let best=null,i=0;
    while(i<this.nodes.length){
      const n=this.nodes[i];
      if(n.bounds.ray(origin,dir,tMax)===null){
        i=n.skip;
        continue;
      }
      if(n.count)for(let j=0;j<n.count;j++){
        const item=this.ordered[n.first+j],hit=intersect(item,tMax);
        if(hit&&hit.distance<tMax){
          tMax=hit.distance;
          best={
            ...hit,item
          };
        }
      }
      i++;
    }
    return best;
  }
  gpuPack() {
    const nodes=new Float32Array(Math.max(1,this.nodes.length)*12);
    this.nodes.forEach((n,i)=>{
      n.bounds.min.toArray(nodes,i*12);
      nodes[i*12+3]=n.skip;
      n.bounds.max.toArray(nodes,i*12+4);
      nodes[i*12+7]=n.count;
      nodes[i*12+8]=n.first;
    });
    return {
      nodes,ordered:this.ordered
    };
  }
}

export class TriangleMeshCollider {
  constructor(positions,indices) {
    this.positions=positions;
    this.indices=indices;
    this.triangles=[];
    for(let i=0;i<indices.length;i+=3)this.triangles.push({
      index:i,bounds:new AABB()
    });
    this.updateBounds();
    this.bvh=new BVH(this.triangles);
  }
  triangle(item) {
    const i=item.index;
    return [Vec3.from(this.positions,this.indices[i]*3),Vec3.from(this.positions,this.indices[i+1]*3),Vec3.from(this.positions,this.indices[i+2]*3)];
  }
  updateBounds() {
    for(const t of this.triangles){
      t.bounds=new AABB();
      for(const p of this.triangle(t))t.bounds.include(p);
    }
    this.bvh?.refit();
  }
  raycast(origin,dir,max=Infinity) {
    return this.bvh.raycast(origin,dir,(item,limit)=>rayTriangle(origin,dir,...this.triangle(item),limit),max);
  }
}
```

## src/vehicle.js

```javascript
import { Vec3, clamp, lerp } from './math.js';
import { RigidBody } from './physics.js';

export class TorqueCurve {
  constructor(points=[[800,160],[2000,270],[4000,335],[5600,300],[6800,0]]){
    if(points.length<2||points.some((p,i)=>p.length!==2||p[0]<0||p[1]<0||(i>0&&p[0]<=points[i-1][0])))throw new RangeError('Torque points must be increasing');
    this.points=points;
  }
  at(rpm){
    if(rpm<=this.points[0][0])return this.points[0][1];
    for(let i=1;i<this.points.length;i++){
      const [r,t]=this.points[i],[r0,t0]=this.points[i-1];
      if(rpm<=r)return lerp(t0,t,(rpm-r0)/(r-r0));
    }
    return 0;
  }
}

export class RaycastVehicle {
  constructor(world,{
    position=new Vec3(0,1,0),mass=1250
  } = {}){
    this.world=world;
    this.body=world.add(new RigidBody({
      mass,half:new Vec3(.85,.26,1.8),position,friction:.4,restitution:.05
    }));
    this.body.angularDamping=.32;
    this.curve=new TorqueCurve();
    this.gears=[-3.2,0,3.5,2.2,1.5,1.1,.85];
    this.gear=2;
    this.finalDrive=3.4;
    this.rpm=900;
    this.shiftTimer=0;
    this.throttle=0;
    this.brake=0;
    this.steer=0;
    this.handbrake=0;
    this.wetness=0;
    this.wheels=[];
    for(const z of [-1.24,1.24])for(const x of [-.82,.82])this.wheels.push({
      anchor:new Vec3(x,-.05,z),radius:.34,rest:.42,travel:.2,spring:33000,damper:4200,inertia:1.4,omega:0,compression:0,load:0,steering:z<0,driven:z>0,hit:null,angle:0
    });
    this.stepCallback=dt=>this.step(dt);
    world.beforeStep.add(this.stepCallback);
  }
  setControls({
    throttle=0,brake=0,steer=0,handbrake=0
  }){
    this.throttle=clamp(throttle,-1,1);
    this.brake=clamp(brake,0,1);
    this.steer=clamp(steer,-1,1);
    this.handbrake=clamp(handbrake,0,1);
  }
  step(dt){
    const b=this.body,up=b.rotation.rotate(new Vec3(0,1,0)),down=up.clone().scale(-1),forward=b.rotation.rotate(new Vec3(0,0,-1)),speed=b.velocity.dot(forward),wheelRPM=this.wheels.filter(w=>w.driven).reduce((s,w)=>s+Math.abs(w.omega),0)/2*60/(2*Math.PI);
    this.shiftTimer=Math.max(0,this.shiftTimer-dt);
    if(this.throttle<-.05&&Math.abs(speed)<1)this.gear=0;
    else if(this.throttle>.05&&this.gear===0&&Math.abs(speed)<1)this.gear=2;
    this.rpm=clamp(lerp(this.rpm,Math.max(850,wheelRPM*Math.abs(this.gears[this.gear])*this.finalDrive),1-Math.exp(-dt*12)),850,6800);
    if(this.gear>=2&&!this.shiftTimer){
      if(this.rpm>5600&&this.gear<this.gears.length-1){
        this.gear++;
        this.shiftTimer=.22;
      }else if(this.rpm<1700&&this.gear>2){
        this.gear--;
        this.shiftTimer=.22;
      }
    }
    const drive=this.shiftTimer?0:this.curve.at(this.rpm)*Math.abs(this.throttle)*this.gears[this.gear]*this.finalDrive*.88/2;
    for(const w of this.wheels){
      const origin=b.localToWorld(w.anchor),hit=this.world.raycast(origin,down,w.rest+w.travel+w.radius,b);
      w.hit=hit;
      let fx=0,fy=0;
      const steer=w.steering?this.steer*.5/(1+Math.abs(speed)*.035):0,c=Math.cos(steer),s=Math.sin(steer),f=b.rotation.rotate(new Vec3(-s,0,-c)),right=Vec3.cross(f,up).normalize();
      if(hit){
        const length=clamp(hit.distance-w.radius,w.rest-w.travel,w.rest+w.travel),compression=w.rest-length,groundV=hit.body?hit.body.pointVelocity(hit.point):new Vec3(),rel=Vec3.sub(b.pointVelocity(hit.point),groundV),compressionSpeed=-rel.dot(up);
        w.compression=compression;
        w.load=clamp(w.spring*compression+w.damper*compressionSpeed,0,b.mass*9.81*1.6);
        const vx=rel.dot(f),vy=rel.dot(right),slip=(w.omega*w.radius-vx)/Math.max(2,Math.abs(vx)),angle=Math.atan2(vy,Math.max(2,Math.abs(vx))),mu=lerp(1.08,.53,this.wetness)*(w.driven?lerp(1,.48,this.handbrake):1),limit=mu*w.load;
        fx=limit*Math.tanh(7*slip);
        fy=-limit*Math.tanh(5*angle);
        const total=Math.hypot(fx,fy);
        if(total>limit&&total>0){
          fx*=limit/total;
          fy*=limit/total;
        }
        const force=up.clone().scale(w.load).addScaled(f,fx).addScaled(right,fy);
        b.applyForce(force,hit.point);
        if(hit.body?.invMass)hit.body.applyForce(force.clone().scale(-1),hit.point);
      }else{
        w.compression=-w.travel;
        w.load=0;
      }
      const engineTorque=w.driven?drive:0,brakeTorque=2400*this.brake+(w.driven?3400*this.handbrake:0),net=engineTorque-fx*w.radius-w.omega*.12;
      w.omega+=net/w.inertia*dt;
      const stop=brakeTorque/w.inertia*dt;
      w.omega=Math.sign(w.omega)*Math.max(0,Math.abs(w.omega)-stop);
      w.omega=clamp(w.omega,-240,240);
      w.angle+=w.omega*dt;
    }
    const v=b.velocity.length();
    if(v>1e-5)b.applyForce(b.velocity.clone().scale(-.5*1.225*.32*2.2*v));
    b.applyForce(up.clone().scale(-.5*1.225*.8*2.2*speed*speed));
  }
  wheelPosition(w){
    return this.body.localToWorld(w.anchor).addScaled(this.body.rotation.rotate(new Vec3(0,-1,0)),w.rest-w.compression);
  }
  dispose(){
    this.world.beforeStep.delete(this.stepCallback);
    this.world.remove(this.body);
  }
}
```

## src/world/city.js

```javascript
import { Vec3 } from '../math.js';
import { GeometryBuilder, renderObject, material } from './geometry.js';
import { CHUNK_SIZE, WORLD_MIN, WORLD_MAX, Random, hash, districtAt, terrainHeight, POIS, keyOf } from './util.js';

const STONE = [.42, .43, .4], PAVING = [.44, .46, .43], ASPHALT = [.065, .077, .085];
const FACADES = [[.47, .43, .36], [.53, .51, .45], [.29, .37, .39], [.56, .46, .36], [.35, .36, .36], [.55, .57, .53]];
const GLASS = [.055, .135, .17], METAL = material(.32, .65), WINDOW = material(.16, .45, -.65, .1);

export function describeChunk(cx, cz) {
  const random = new Random(hash(cx, cz, 183)), ox = cx * 80, oz = cz * 80;
  const district = districtAt(ox + 40, oz + 40), buildings = [], specials = POIS.filter(p => Math.floor(p.x / 80) === cx && Math.floor(p.z / 80) === cz);
  const publicSpace = cx === 0 && cz === 0 || cx === -2 && cz === 2 || cx === 0 && cz === 4;
  for (const p of specials) {
    if (['community', 'park', 'vista'].includes(p.kind)) continue;
    const x = p.x - ox + 16, z = p.z - oz;
    buildings.push({ id: `${cx}:${cz}:poi:${p.id}`, x, z, hx: 12, hz: 11, height: p.id === 'warehouse' ? 13 : p.id === 'clinic' ? 12 : 7, color: p.id === 'clinic' ? [.62, .64, .58] : p.id === 'cafe' ? [.49, .31, .2] : [.36, .4, .39], special: p.id, industrial: ['warehouse', 'harbor', 'depot'].includes(p.id) });
  }
  if (!publicSpace && ox < 400) for (const [x, z] of [[25, 25], [55, 25], [25, 55], [55, 55]]) {
    if (buildings.some(b => Math.abs(b.x - x) < b.hx + 11 && Math.abs(b.z - z) < b.hz + 11)) continue;
    if (district.id === 'rural' && (x !== 25 || z !== 25)) continue;
    const suburb = district.id === 'suburbs' || district.id === 'rural', industrial = district.id === 'industrial' || district.id === 'highway';
    const height = suburb ? random.range(4.5, 7) : industrial ? random.range(7, 13) : random.range(13, 38) * (cx === 0 || cz === 0 ? 1.25 : 1);
    buildings.push({ id: `${cx}:${cz}:lot:${x}:${z}`, x, z, hx: suburb ? 8.2 : random.range(8.5, 11), hz: suburb ? 8 : random.range(8, 10.5), height, color: random.pick(FACADES), suburb, industrial });
  }
  const props = [], lights = [];
  for (const [i, [x, z]] of [[14, 11], [66, 69]].entries()) {
    if (ox + x > 430) continue;
    const id = `${cx}:${cz}:lamp:${i}`, y = terrainHeight(ox + x, oz + z);
    lights.push({ id, x: ox + x, y: y + 5.8, z: oz + z, color: [1, .72, .38], radius: 17, power: 18 });
    props.push({ id, type: 'lamp', x: ox + x, y, z: oz + z, breakable: false });
  }
  for (let i = 0; i < 2; i++) {
    const x = i ? 68 : 12, z = i ? 51 : 27;
    if (ox + x < 430) props.push({ id: `${cx}:${cz}:bin:${i}`, type: 'bin', x: ox + x, y: terrainHeight(ox + x, oz + z), z: oz + z, breakable: true });
  }
  if (district.id === 'industrial') for (let i = 0; i < 3; i++) props.push({ id: `${cx}:${cz}:crate:${i}`, type: 'crate', x: ox + 12, y: terrainHeight(ox + 12, oz + 43 + i * 1.5), z: oz + 43 + i * 1.5, breakable: true });
  return { id: keyOf(cx, cz), cx, cz, ox, oz, district, buildings, props, lights, specials, publicSpace };
}

function tree(g, x, z, seed, y = 0, detailed = true) {
  const r = new Random(seed), h = r.range(2.8, 4.4);
  g.box(x, y + h * .5, z, .16, h * .5, .16, [.18, .12, .075]);
  g.sphere(x, y + h + .5, z, 1.55, 2.0, 1.5, r.pick([[.12, .24, .13], [.2, .3, .13], [.18, .27, .19]]), material(.95), detailed ? 8 : 6);
}

function building(g, b, level, y = 0) {
  const { x, z, hx, hz, height: h, color } = b;
  g.box(x, y + h / 2, z, hx, h / 2, hz, color);
  g.box(x, y + h + .18, z, hx + .22, .18, hz + .22, [.19, .21, .21], material(.78));
  if (b.suburb) {
    g.gable(x, y + h + .35, z, hx + .45, 3.1, hz + .5, [.25, .13, .095]);
    g.box(x + hx * .45, y + h + 1.5, z - hz * .5, .6, 1.6, .65, [.28, .25, .22]);
  } else if (!b.industrial) {
    g.box(x - hx + .8, y + h + 1.25, z, .6, 1.2, hz * .6, [.3, .33, .32], METAL);
    if (h > 28) g.box(x + hx * .3, y + h + 1.2, z, hx * .45, 1, hz * .6, color);
  }
  if (level > 0) return;
  const floors = Math.max(1, Math.min(9, Math.floor(h / 3.5))), cols = b.suburb ? 2 : 4;
  for (let floor = 0; floor < floors; floor++) {
    const wy = y + 2.0 + floor * (h - 2.8) / floors;
    if (!b.suburb && floor > 0) g.box(x, wy - .75, z, hx + .04, .055, hz + .04, color.map(v => v * .82));
    for (let col = 0; col < cols; col++) {
      const wx = x - hx + (col + .5) * hx * 2 / cols, wz = z - hz + (col + .5) * hz * 2 / cols;
      const lit = hash(Math.floor(x + col), floor, Math.floor(z)) % 5 !== 0;
      const surf = lit ? WINDOW : material(.17, .5, 0, .1);
      g.box(wx, wy, z + hz + .035, b.suburb ? 1.0 : .75, .8, .045, GLASS, surf);
      g.box(wx, wy, z - hz - .035, b.suburb ? 1.0 : .75, .8, .045, GLASS, surf);
      g.box(x + hx + .035, wy, wz, .045, .8, .75, GLASS, surf);
      g.box(x - hx - .035, wy, wz, .045, .8, .75, GLASS, surf);
    }
  }
  g.box(x - hx - .05, y + 1.2, z, .08, 1.2, .8, [.055, .07, .075], material(.3, .4));
  if (b.special || (!b.suburb && !b.industrial)) {
    const awning = b.special === 'cafe' ? [.12, .28, .24] : b.special === 'clinic' ? [.62, .69, .63] : [.18, .23, .27];
    g.box(x - hx - 1.8, y + 3.4, z, 1.9, .18, Math.min(hz, 7), awning);
    g.box(x - hx - .12, y + 4.1, z, .12, .4, Math.min(hz, 6), [.075, .13, .15], material(.55, .15, -.4));
    // Entry path and an outside service counter keep every mission location accessible.
    g.box(x - hx - 3, y + .055, z, 3.5, .055, 2, [.49, .48, .43]);
    if (b.special === 'cafe') {
      g.box(x - hx - 1.4, y + .55, z + 3, .5, .55, 1.4, [.24, .16, .09]);
      for (const dz of [-4, 5]) { g.cylinder(x - hx - 3.5, y + .9, z + dz, .8, .06, [.37, .28, .17]); g.box(x - hx - 3.5, y + .45, z + dz, .08, .45, .08, [.1, .12, .13]); }
    }
    if (b.special === 'clinic') { g.box(x - hx - .15, y + 4.1, z, .15, .6, .2, [.7, .2, .15], material(.5, 0, .12)); g.box(x - hx - .18, y + 4.1, z, .15, .2, .6, [.7, .2, .15], material(.5, 0, .12)); }
  }
  if (b.industrial) {
    g.box(x - hx - .08, y + 2, z + hz * .4, .1, 2, 3, [.14, .2, .22], METAL);
    for (let i = 0; i < 6; i++) g.box(x - hx - .2, y + .5 + i * .55, z + hz * .4, .05, .035, 3, [.32, .37, .37]);
    g.box(x + hx * .3, y + h + 1.5, z, 1.6, 1.4, 1.8, [.18, .22, .23], METAL);
  }
}

function roads(g, plan, level) {
  const { ox, oz } = plan;
  // Shared boundaries are half-road strips, so neighboring chunks join exactly.
  for (const x of [3.5, 76.5]) g.box(x, .012, 40, 3.5, .012, 40, ASPHALT, material(.94));
  for (const z of [3.5, 76.5]) g.box(40, .016, z, 40, .016, 3.5, ASPHALT, material(.94));
  for (const x of [8.6, 71.4]) g.box(x, .09, 40, 1.6, .09, 33, PAVING);
  for (const z of [8.6, 71.4]) g.box(40, .09, z, 33, .09, 1.6, PAVING);
  for (let t = 15; t < 70; t += 12) {
    g.box(.07, .035, t, .07, .012, 2.8, [.65, .57, .35]); g.box(t, .035, .07, 2.8, .012, .07, [.65, .57, .35]);
  }
  if (!level) for (let t = 1; t < 6; t++) {
    const s = 1 + t * 1.2;
    for (const z of [10.7, 40, 69.3]) { g.box(s, .045, z, .35, .014, 1.1, [.75, .75, .65]); g.box(80 - s, .045, z, .35, .014, 1.1, [.75, .75, .65]); }
    for (const x of [10.7, 40, 69.3]) { g.box(x, .045, s, 1.1, .014, .35, [.75, .75, .65]); g.box(x, .045, 80 - s, 1.1, .014, .35, [.75, .75, .65]); }
  }
  if (oz < -190) for (let t = 15; t < 70; t += 10) g.box(t, .04, 3.5, 2.2, .01, .07, [.65, .64, .52]);
}

export class CityGenerator {
  describe(cx, cz) { return describeChunk(cx, cz); }
  *build(cx, cz, level = 0, signal = null) {
    const plan = describeChunk(cx, cz), g = new GeometryBuilder(), proxy = new GeometryBuilder(), bodies = [], ground = new GeometryBuilder();
    const random = new Random(hash(cx, cz, 771));
    const green = plan.district.id === 'rural' ? [.23, .29, .16] : [.25, .29, .23];
    const steps = plan.district.id === 'rural' ? 8 : 1;
    for (let z = 0; z < steps; z++) for (let x = 0; x < steps; x++) {
      const a = x * 80 / steps, b = z * 80 / steps, c = (x + 1) * 80 / steps, d = (z + 1) * 80 / steps;
      const point = (px, pz) => new Vec3(px, terrainHeight(plan.ox + px, plan.oz + pz) - .03, pz);
      if (plan.ox + a < 436) ground.quad(point(a, b), point(a, d), point(c, d), point(c, b), green);
    }
    roads(g, plan, level); yield { phase: 'roads' };
    for (const b of plan.buildings) {
      if (signal?.aborted) return null;
      const y = terrainHeight(plan.ox + b.x, plan.oz + b.z);
      building(g, b, level, y);
      proxy.box(b.x, y + b.height * .5, b.z, b.hx, b.height * .5, b.hz, b.color);
      bodies.push({ id: b.id, x: plan.ox + b.x, y: y + b.height * .5, z: plan.oz + b.z, half: [b.hx, b.height * .5, b.hz] });
      yield { phase: 'building', id: b.id };
    }
    if (plan.publicSpace) {
      g.box(40, .04, 40, 26, .04, 26, [.49, .47, .4]);
      for (const t of [20, 40, 60]) { g.box(t, .085, 40, .04, .01, 26, [.31, .32, .29]); g.box(40, .085, t, 26, .01, .04, [.31, .32, .29]); }
      if (cx === 0 && cz === 0) {
        g.cylinder(43, .24, 43, 4.5, .24, [.47, .47, .4], material(.55), 16);
        g.cylinder(43, .5, 43, 3.9, .04, [.025, .22, .28], material(.08, .45), 16);
        g.cylinder(43, 1.1, 43, .8, .6, [.6, .57, .46], material(.4), 10);
        g.sphere(43, 2.1, 43, .75, .75, .75, [.57, .43, .21], material(.16, .9));
        bodies.push({ id: 'fountain', x: plan.ox + 43, y: .4, z: plan.oz + 43, half: [4, .4, 4] });
        g.box(26, 1.2, 25, .12, 1.2, 1.7, [.13, .17, .18]);
        g.box(26.14, 1.9, 25, .05, .42, 1.4, [.6, .49, .29]);
      }
      for (const [x, z] of [[19, 22], [61, 22], [19, 60], [61, 60]]) {
        const y = terrainHeight(plan.ox + x, plan.oz + z);
        g.box(x, y + .5, z, 1.8, .12, .48, [.25, .16, .09]); g.box(x, y + .95, z + .43, 1.8, .45, .08, [.25, .16, .09]);
        g.box(x - 1.3, y + .2, z, .08, .25, .35, [.08, .1, .1]); g.box(x + 1.3, y + .2, z, .08, .25, .35, [.08, .1, .1]);
      }
    }
    const count = plan.publicSpace ? 8 : plan.district.id === 'rural' ? 7 : 3;
    for (let i = 0; i < count; i++) {
      const x = plan.publicSpace ? (i % 2 ? 62 : 18) : random.pick([12.5, 67.5]), z = 15 + (i * 13 % 52);
      if (plan.ox + x < 430) tree(g, x, z, hash(cx, cz, i + 5), terrainHeight(plan.ox + x, plan.oz + z), !level);
    }
    if (plan.ox >= 400 && plan.ox < 440) {
      g.box(20, .09, 40, 14, .09, 33, [.5, .48, .4]);
      for (let z = 10; z < 80; z += 10) { g.box(34, .6, z, .08, .6, .08, [.13, .2, .22], METAL); g.box(34, 1.13, z + 5, .05, .05, 5, [.22, .28, .27], METAL); }
      bodies.push({ id: `seawall:${cz}`, x: plan.ox + 34, y: 1, z: plan.oz + 40, half: [.3, 1, 40] });
      if (cz % 2 === 0) { g.box(49, -.12, 40, 15, .18, 3.5, [.33, .25, .17]); for (const z of [37, 43]) for (const x of [38, 49, 60]) g.box(x, -1, z, .2, 1, .2, [.18, .16, .12]); }
    }
    yield { phase: 'details' };
    return { ...plan, mesh: g.build(), groundMesh: ground.build(), shadowMesh: proxy.build(), colliders: bodies, level };
  }
  buildSync(cx, cz, level = 0) { const it = this.build(cx, cz, level); let step; do { step = it.next(); } while (!step.done); return step.value; }
  createHorizon() {
    const g = new GeometryBuilder();
    for (let cz = WORLD_MIN; cz <= WORLD_MAX; cz++) for (let cx = WORLD_MIN; cx <= WORLD_MAX; cx++) {
      const p = describeChunk(cx, cz);
      for (const b of p.buildings) {
        const y = terrainHeight(p.ox + b.x, p.oz + b.z);
        g.box(p.ox + b.x, y + b.height * .5 - .1, p.oz + b.z, Math.max(.2, b.hx - .22), b.height * .5 - .1, Math.max(.2, b.hz - .22), b.color);
      }
    }
    return renderObject(g.build(), { castShadow: false });
  }
}
```

## src/world/destruction.js

```javascript
import { Vec3, Quat } from '../math.js';
import { RigidBody } from '../physics.js';
import { GeometryBuilder, renderObject, material } from './geometry.js';
import { Random, hash } from './util.js';

function clipHalfPlane(polygon, normal, constant) {
  const result = [];
  for (let i = 0; i < polygon.length; i++) {
    const a = polygon[i], b = polygon[(i + 1) % polygon.length], da = a[0] * normal[0] + a[1] * normal[1] - constant, db = b[0] * normal[0] + b[1] * normal[1] - constant;
    if (da <= 1e-8) result.push(a);
    if ((da < 0 && db > 0) || (da > 0 && db < 0)) { const t = da / (da - db); result.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]); }
  }
  return result;
}

export class GlassFracture {
  static cells(width, height, count = 9, seed = 53) {
    const random = new Random(seed), seeds = Array.from({ length: count }, () => [random.range(-width / 2, width / 2), random.range(-height / 2, height / 2)]), cells = [];
    for (let i = 0; i < seeds.length; i++) {
      let polygon = [[-width / 2, -height / 2], [width / 2, -height / 2], [width / 2, height / 2], [-width / 2, height / 2]];
      for (let j = 0; j < seeds.length && polygon.length; j++) if (i !== j) {
        const a = seeds[i], b = seeds[j], n = [b[0] - a[0], b[1] - a[1]], c = (b[0] ** 2 + b[1] ** 2 - a[0] ** 2 - a[1] ** 2) / 2;
        polygon = clipHalfPlane(polygon, n, c);
      }
      if (polygon.length >= 3) cells.push(polygon);
    }
    return cells;
  }
  static mesh(polygon) {
    const center = polygon.reduce((p, v) => [p[0] + v[0] / polygon.length, p[1] + v[1] / polygon.length], [0, 0]), g = new GeometryBuilder();
    for (let i = 0; i < polygon.length; i++) {
      const a = polygon[i], b = polygon[(i + 1) % polygon.length];
      const v = q => new Vec3(q[0] - center[0], q[1] - center[1], 0);
      g.quad(new Vec3(), v(a), v(b), v(b), [.13, .29, .32], material(.13, .65));
    }
    return { mesh: g.build(), center };
  }
}

export class DestructionSystem {
  constructor(world, progress, { panic = () => {}, release = () => {} } = {}) { this.world = world; this.progress = progress; this.panic = panic; this.release = release; this.debris = []; this.timer = 0; }
  break(prop, direction = new Vec3(0, 0, -1)) {
    if (!prop.breakable || this.progress.destroyed.includes(prop.id)) return false;
    this.progress.destroyed.push(prop.id); this.panic(new Vec3(prop.x, prop.y, prop.z));
    const random = new Random(hash(Math.round(prop.x), Math.round(prop.z), 123));
    const glass = prop.type === 'glass', pieces = glass ? GlassFracture.cells(2.6, 1.6, 9) : Array.from({ length: 5 }, () => null);
    for (const piece of pieces) {
      const center = new Vec3(prop.x + random.range(-.2, .2), prop.y + .65 + random.range(0, .6), prop.z + random.range(-.2, .2));
      const body = this.world.add(new RigidBody({ half: glass ? new Vec3(.13, .13, .035) : new Vec3(.13, .16, .13), mass: glass ? .15 : 1, position: center, restitution: .16 }));
      body.velocity.copy(direction).scale(random.range(1, 3)).add(new Vec3(random.range(-1.2, 1.2), random.range(1, 3), random.range(-1.2, 1.2))); body.angularVelocity.set(random.range(-5, 5), random.range(-5, 5), random.range(-5, 5));
      let object = null;
      if (glass) { const fragment = GlassFracture.mesh(piece); object = renderObject(fragment.mesh, { static: false }); object.doubleSided = true; body.position.x = prop.x + fragment.center[0]; body.position.y = prop.y + 1.2 + fragment.center[1]; body.previousPosition.copy(body.position); body.updateBounds(); }
      this.debris.push({ body, object, remaining: glass ? 7 : 10, color: prop.type === 'crate' ? [.35, .22, .11] : [.1, .17, .16] });
    }
    while (this.debris.length > 36) this.remove(this.debris.shift());
    return true;
  }
  remove(piece) { this.world.remove(piece.body); if (piece.object) this.release(piece.object); }
  update(dt) { for (let i = this.debris.length - 1; i >= 0; i--) { const d = this.debris[i]; d.remaining -= dt; if (d.remaining <= 0) { this.remove(d); this.debris.splice(i, 1); } } }
  dispose() { for (const d of this.debris) this.remove(d); this.debris = []; }
}
```

## src/world/geometry.js

```javascript
import { Vec3, Quat, Mat4, AABB } from '../math.js';
import { MeshData } from '../mesh.js';

const BOX = MeshData.box();
const sphereCache = new Map();
export const material = (roughness = .8, metallic = 0, emission = 0, wettable = 1) => [roughness, metallic, emission, wettable];

export class GeometryBuilder {
  constructor() { this.positions = []; this.normals = []; this.uvs = []; this.colors = []; this.materials = []; this.indices = []; }
  append(mesh, transform, color, surface = material()) {
    const offset = this.positions.length / 3;
    // All builder scales are positive; inverse-transpose handles nonuniform shapes.
    const sx2 = transform[0] ** 2 + transform[1] ** 2 + transform[2] ** 2, sy2 = transform[4] ** 2 + transform[5] ** 2 + transform[6] ** 2, sz2 = transform[8] ** 2 + transform[9] ** 2 + transform[10] ** 2;
    for (let i = 0; i < mesh.positions.length; i += 3) {
      const p = Mat4.point(transform, Vec3.from(mesh.positions, i));
      const n = Vec3.from(mesh.normals, i);
      const worldNormal = new Vec3(transform[0] * n.x / sx2 + transform[4] * n.y / sy2 + transform[8] * n.z / sz2, transform[1] * n.x / sx2 + transform[5] * n.y / sy2 + transform[9] * n.z / sz2, transform[2] * n.x / sx2 + transform[6] * n.y / sy2 + transform[10] * n.z / sz2).normalize();
      p.toArray(this.positions, this.positions.length); worldNormal.toArray(this.normals, this.normals.length);
      this.colors.push(...color); this.materials.push(...surface);
    }
    this.uvs.push(...mesh.uvs);
    for (const index of mesh.indices) this.indices.push(index + offset);
    return this;
  }
  box(x, y, z, hx, hy, hz, color, surface = material(), yaw = 0) {
    return this.append(BOX, Mat4.compose(new Vec3(x, y, z), Quat.axisAngle(new Vec3(0, 1, 0), yaw), new Vec3(hx, hy, hz)), color, surface);
  }
  sphere(x, y, z, sx, sy, sz, color, surface = material(), segments = 8) {
    if (!sphereCache.has(segments)) sphereCache.set(segments, MeshData.sphere(segments, Math.max(4, segments / 2)));
    return this.append(sphereCache.get(segments), Mat4.compose(new Vec3(x, y, z), new Quat(), new Vec3(sx, sy, sz)), color, surface);
  }
  quad(a, b, c, d, color, surface = material()) {
    const start = this.positions.length / 3, n = Vec3.cross(Vec3.sub(b, a), Vec3.sub(c, a)).normalize();
    for (const [i, p] of [a, b, c, d].entries()) { p.toArray(this.positions, this.positions.length); n.toArray(this.normals, this.normals.length); this.uvs.push(i === 1 || i === 2 ? 1 : 0, i >= 2 ? 1 : 0); this.colors.push(...color); this.materials.push(...surface); }
    this.indices.push(start, start + 1, start + 2, start, start + 2, start + 3); return this;
  }
  gable(x, y, z, hx, height, hz, color) {
    const a = new Vec3(x - hx, y, z - hz), b = new Vec3(x + hx, y, z - hz), c = new Vec3(x, y + height, z - hz), d = new Vec3(x - hx, y, z + hz), e = new Vec3(x + hx, y, z + hz), f = new Vec3(x, y + height, z + hz);
    this.quad(a, c, f, d, color); this.quad(c, b, e, f, color);
    this.quad(b, c, a, a, color); this.quad(d, f, e, e, color); return this;
  }
  cylinder(x, y, z, radius, halfHeight, color, surface = material(), segments = 8) {
    for (let i = 0; i < segments; i++) {
      const a = i * 2 * Math.PI / segments, b = (i + 1) * 2 * Math.PI / segments;
      const p = new Vec3(x + Math.cos(a) * radius, y - halfHeight, z + Math.sin(a) * radius), q = new Vec3(x + Math.cos(b) * radius, y - halfHeight, z + Math.sin(b) * radius), r = new Vec3(q.x, y + halfHeight, q.z), s = new Vec3(p.x, y + halfHeight, p.z);
      this.quad(q, p, s, r, color, surface); this.quad(new Vec3(x, y + halfHeight, z), r, s, s, color, surface);
    }
    return this;
  }
  build() {
    const mesh = new MeshData(this.positions, this.normals, this.uvs, this.indices);
    mesh.colors = new Float32Array(this.colors); mesh.materials = new Float32Array(this.materials); return mesh;
  }
}

let nextObjectId = 100000;
export function renderObject(mesh, { position = new Vec3(), color = [1, 1, 1], roughness = .8, metallic = 0, vertexMaterials = true, castShadow = true, static: fixed = true } = {}) {
  const model = Mat4.compose(position, new Quat()), bounds = new AABB(Vec3.add(mesh.bounds.min, position), Vec3.add(mesh.bounds.max, position));
  return { id: nextObjectId++, lods: [{ mesh, error: 0 }], material: { color, roughness, metallic, vertexMaterials }, model, bounds, static: fixed, castShadow };
}

export class InstanceBatch {
  constructor(mesh, capacity = 2048) {
    this.object = renderObject(mesh, { static: false, vertexMaterials: true });
    this.data = new Float32Array(capacity * 24); this.count = 0; this.object.instances = this.data; this.object.instanceCount = 0;
  }
  reset() { this.count = 0; this.object.bounds = new AABB(); }
  add(position, rotation, scale, color, surface = material()) {
    if ((this.count + 1) * 24 > this.data.length) { const larger = new Float32Array(this.data.length * 2); larger.set(this.data); this.data = larger; this.object.instances = larger; }
    const offset = this.count++ * 24, matrix = Mat4.compose(position, rotation, scale);
    this.data.set(matrix, offset); this.data.set([...color, 1], offset + 16); this.data.set(surface, offset + 20);
    const radius = Math.max(scale.x, scale.y, scale.z) * 1.75;
    this.object.bounds.include(position.clone().add(new Vec3(-radius, -radius, -radius))).include(position.clone().add(new Vec3(radius, radius, radius)));
  }
  box(x, y, z, hx, hy, hz, color, surface = material(), rotation = new Quat()) { this.add(new Vec3(x, y, z), rotation, new Vec3(hx, hy, hz), color, surface); }
  finish() { this.object.instanceCount = this.count; this.object.instanceVersion = (this.object.instanceVersion || 0) + 1; return this.object; }
}
```

## src/world/navigation.js

```javascript
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
```

## src/world/streaming.js

```javascript
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
```

## src/world/util.js

```javascript
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
  if (x > 420) return { id: 'coast', name: 'Meridian Waterfront', color: '#5ea6b7' };
  if (z > 230) return { id: 'rural', name: 'Cedar Hills', color: '#8bac72' };
  if (x > 180) return { id: 'industrial', name: 'East Harbor', color: '#d5ac79' };
  if (x < -150) return { id: 'suburbs', name: 'Westhaven', color: '#c9b8a0' };
  if (z < -190) return { id: 'highway', name: 'Northline', color: '#92a2b8' };
  return { id: 'downtown', name: 'Old Meridian', color: '#9dbaca' };
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

export const POIS = Object.freeze([
  { id: 'plaza', name: 'Founders Square', kind: 'community', x: 40, z: 40, description: 'Meet Mira, take on a story, or watch the city pass by.' },
  { id: 'cafe', name: 'Harbor Coffee', kind: 'work', x: 104, z: 40, description: 'A neighborhood café. Work the counter or catch up with Leo.', open: [7, 22] },
  { id: 'clinic', name: 'Meridian Clinic', kind: 'clinic', x: -56, z: 106, description: 'Dr. Anika is waiting for essential deliveries.', open: [6, 23] },
  { id: 'depot', name: 'Eastside Dispatch', kind: 'work', x: 184, z: 106, description: 'Pick up a parcel and earn an honest delivery fee.', open: [0, 24] },
  { id: 'harbor', name: 'Harbor Workshop', kind: 'work', x: 344, z: 106, description: 'Sanjay runs repairs along the waterfront.', open: [6, 22] },
  { id: 'warehouse', name: 'Warehouse 08', kind: 'story', x: 344, z: -56, description: 'There is more to a missing shipment than a misplaced label.' },
  { id: 'market', name: 'Westhaven Market', kind: 'shop', x: -216, z: 40, description: 'Groceries, neighborhood stories, and a place to rest.' },
  { id: 'park', name: 'Cedar Gardens', kind: 'park', x: -120, z: 200, description: 'A quiet garden away from the city traffic.' },
  { id: 'lookout', name: 'Cedar Hill Lookout', kind: 'vista', x: 40, z: 360, description: 'Take the south road to a view over the whole city.' },
  { id: 'waterfront', name: 'Meridian Promenade', kind: 'vista', x: 410, z: 40, description: 'Walk the harbor, watch the boats, and catch the evening light.' },
  { id: 'garage', name: 'Aster Motorworks', kind: 'garage', x: 104, z: -56, description: 'Your road car is parked near Founders Square.' }
]);
export const poiById = id => POIS.find(p => p.id === id);
export const poiPosition = id => { const p = poiById(id); if (!p) throw new Error(`Unknown place: ${id}`); return new Vec3(p.x, terrainHeight(p.x, p.z), p.z); };
export const isOpen = (poi, hour) => !poi.open || (hour >= poi.open[0] && hour < poi.open[1]);
```

## src/world/weather.js

```javascript
import { clamp, lerp, Vec3 } from '../math.js';
import { WetnessField } from '../resources.js';
import { Random } from './util.js';

export const WEATHER_TYPES = Object.freeze({
  clear: { label: 'Clear skies', rain: 0, clouds: .12, fog: .003, wind: .15 },
  cloudy: { label: 'Overcast', rain: 0, clouds: .75, fog: .005, wind: .35 },
  rain: { label: 'Passing rain', rain: .55, clouds: .9, fog: .008, wind: .55 },
  storm: { label: 'Harbor storm', rain: 1, clouds: 1, fog: .012, wind: 1 }
});

export class WeatherSystem {
  constructor(seed = 9203) { this.random = new Random(seed); this.state = 'clear'; this.remaining = 110; this.rain = 0; this.clouds = .12; this.fog = .003; this.wind = .15; this.flash = 0; this.elapsed = 0; }
  update(dt) {
    this.elapsed += dt; this.remaining -= dt;
    if (this.remaining <= 0) {
      const next = { clear: ['cloudy', 'clear'], cloudy: ['rain', 'clear'], rain: ['storm', 'cloudy'], storm: ['rain', 'cloudy'] };
      this.state = this.random.pick(next[this.state]); this.remaining = this.random.range(70, 145);
    }
    const target = WEATHER_TYPES[this.state], t = 1 - Math.exp(-dt / 12);
    for (const k of ['rain', 'clouds', 'fog', 'wind']) this[k] = lerp(this[k], target[k], t);
    this.flash = Math.max(0, this.flash - dt * 5);
    if (this.state === 'storm' && this.random.next() < dt * .055) this.flash = 1;
  }
  get label() { return WEATHER_TYPES[this.state].label; }
  snapshot() { return { state: this.state, remaining: this.remaining, rain: this.rain, clouds: this.clouds, elapsed: this.elapsed, random: this.random.state >>> 0 }; }
  restore(data) { if (!data || !WEATHER_TYPES[data.state]) return; this.state = data.state; this.remaining = clamp(Number(data.remaining) || 80, 5, 150); this.rain = clamp(Number(data.rain) || 0, 0, 1); this.clouds = clamp(Number(data.clouds) || .1, 0, 1); this.elapsed = Math.max(0, Number(data.elapsed) || 0); this.random.state = Number(data.random) >>> 0 || 9203; }
}

export class MovingWetnessField extends WetnessField {
  constructor() { super({ size: 128, extent: 200 }); this.origin = new Vec3(); this.background = 0; }
  update(dt, position, rain, sunlight) {
    this.rain = rain; this.background = clamp(this.background + (rain * .18 - .012 * Math.max(.25, sunlight)) * dt, 0, 1);
    const cell = this.extent * 2 / this.size, x = Math.round(position.x / (cell * 16)) * cell * 16, z = Math.round(position.z / (cell * 16)) * cell * 16;
    if (x !== this.origin.x || z !== this.origin.z) {
      const dx = Math.round((x - this.origin.x) / cell), dz = Math.round((z - this.origin.z) / cell), next = new Float32Array(this.water.length);
      for (let j = 0; j < this.size; j++) for (let i = 0; i < this.size; i++) next[j * this.size + i] = i + dx >= 0 && i + dx < this.size && j + dz >= 0 && j + dz < this.size ? this.water[(j + dz) * this.size + i + dx] : this.background;
      this.water = next; this.origin.set(x, 0, z);
    }
    super.step(dt, sunlight);
  }
  sample(x, z) { return super.sample(x - this.origin.x, z - this.origin.z); }
}
```

## tests/bench.test.js

```javascript
import test from 'node:test';
import assert from 'node:assert/strict';
import { Engine } from '../src/engine.js';
import { Renderer } from '../src/renderer.js';
import { Vec3 } from '../src/math.js';
import { PhysicsWorld, RigidBody } from '../src/physics.js';
import { MeshData } from '../src/mesh.js';
import { LODSelector } from '../src/resources.js';
import { BVH } from '../src/spatial.js';

test('integrated lab settles and maximum pooled content fits the software-ray BVH limit', () => {
  // Exercise the real integration methods without constructing a browser or GPU context.
  const engine = Object.create(Engine.prototype);
  Object.assign(engine, {
    world: new PhysicsWorld(), objects: [], nextObject: 1, ragdolls: [],
    box: MeshData.box(),
    spheres: [{ mesh: MeshData.sphere(24, 12), error: 0 }],
    smallSpheres: [{ mesh: MeshData.sphere(12, 6), error: 0 }],
    renderer: { lod: new LODSelector() }
  });
  engine.createLab();
  const droppedSphere = engine.world.bodies.at(-1);
  for (let i = 0; i < 600; i++) engine.world.step(1 / 120);
  assert.ok(droppedSphere.position.y > 2, 'Sphere must remain supported by the coupled surface');
  assert.ok(engine.world.bodies.every(b => b.position.finite() && b.velocity.finite()));

  for (let i = 0; i < 3; i++) engine.spawnRagdoll();
  for (let i = 0; i < 24; i++) {
    const body = engine.world.add(new RigidBody({
      shape: 'sphere', radius: .3, mass: 8,
      position: new Vec3(i % 6 - 3, 7 + Math.floor(i / 6), 7)
    }));
    engine.bodyVisual(body);
  }
  engine.updateObjects(1);
  const renderer = Object.create(Renderer.prototype);
  Object.assign(renderer, {
    traceBVH: new BVH(), traceItems: [], traceSignature: '', uploadData() {}
  });
  renderer.updateTraceScene(engine.objects);
  assert.equal(engine.world.bodies.length, 75);
  assert.ok(renderer.traceItems.length > 17000);
  assert.ok(renderer.traceBVH.nodes.length < 16384);
  for (const o of engine.objects) {
    assert.ok([...o.model].every(Number.isFinite));
    assert.ok(o.bounds.min.finite() && o.bounds.max.finite());
  }
});
```

## tests/city.test.js

```javascript
import test from 'node:test';
import assert from 'node:assert/strict';
import { Vec3, Quat, AABB } from '../src/math.js';
import { CityGenerator, describeChunk } from '../src/world/city.js';
import { ChunkStreamer } from '../src/world/streaming.js';
import { SidewalkGraph, CubicBezier } from '../src/world/navigation.js';
import { Random, hash, poiPosition, terrainHeight, POIS } from '../src/world/util.js';
import { WeatherSystem, MovingWetnessField } from '../src/world/weather.js';
import { GlassFracture } from '../src/world/destruction.js';
import { CitySimulation } from '../src/game/city-simulation.js';
import { CitizenSystem, drawPerson } from '../src/game/citizens.js';
import { ProgressStore, validateProgress, defaultProgress } from '../src/game/progress.js';
import { MissionSystem, REPAIR_LAMPS } from '../src/game/missions.js';
import { DialogueSystem } from '../src/game/dialogue.js';
import { RoadLoop, trafficSignal, TrafficSystem } from '../src/game/traffic.js';
import { CityPlayer } from '../src/game/player.js';
import { PhysicsWorld, RigidBody } from '../src/physics.js';
import { InstanceBatch } from '../src/world/geometry.js';
import { MeshData } from '../src/mesh.js';

const near = (a, b, e = 1e-5) => assert.ok(Math.abs(a - b) < e, `${a} differs from ${b}`);
const memory = () => { const data = new Map(); return { getItem: key => data.get(key) || null, setItem: (key, value) => data.set(key, value) }; };
const missions = () => { const store = new ProgressStore(memory()); return { store, system: new MissionSystem(store) }; };
const drain = s => { let guard = 0; while (s.pending.size && guard++ < 100) s.pump(100); assert.ok(guard < 100); };

test('seeded generation is stable across positive and negative chunk coordinates', () => {
  assert.deepEqual(describeChunk(-2, 1), describeChunk(-2, 1)); assert.notDeepEqual(describeChunk(2, 1), describeChunk(1, 2));
  const a = new Random(hash(-1, 2)), b = new Random(hash(-1, 2)); for (let i = 0; i < 100; i++) near(a.next(), b.next());
});
test('all mission locations are outside building collider footprints', () => {
  for (const poi of POIS) {
    const plan = describeChunk(Math.floor(poi.x / 80), Math.floor(poi.z / 80));
    for (const b of plan.buildings) assert.ok(Math.abs(poi.x - plan.ox - b.x) >= b.hx + .3 || Math.abs(poi.z - plan.oz - b.z) >= b.hz + .3, `${poi.id} is inside ${b.id}`);
  }
});
test('generated meshes have matching color/material attributes and complete triangle indices', () => {
  const c = new CityGenerator().buildSync(1, 0);
  assert.equal(c.mesh.colors.length, c.mesh.positions.length); assert.equal(c.mesh.materials.length, c.mesh.positions.length / 3 * 4); assert.equal(c.mesh.indices.length % 3, 0);
  assert.ok([...c.mesh.positions].every(Number.isFinite)); assert.ok(Math.max(...c.mesh.indices) < c.mesh.positions.length / 3); assert.ok(c.colliders.length > 0);
});
test('rural terrain meets the road at zero and is deterministic on chunk seams', () => {
  near(terrainHeight(0, 360), 0); near(terrainHeight(80, 360), 0); assert.ok(terrainHeight(40, 360) > 0);
  near(terrainHeight(79.999999, 400), terrainHeight(80.000001, 400), 1e-4);
});
test('chunk commits are atomic and generation can be cancelled without an install', () => {
  const generator = new CityGenerator(), installed = [], s = new ChunkStreamer(generator, { radius: 0, onResident: c => installed.push(c.id) });
  s.update(new Vec3(40, 0, 40)); assert.equal(s.resident.size, 0); s.cancel('0:0'); s.pump(50); assert.equal(installed.length, 0);
  s.update(new Vec3(40, 0, 40)); drain(s); assert.equal(installed.length, 1); assert.equal(s.resident.get('0:0').state, 'resident'); s.dispose();
});
test('chunk streaming respects residency capacity and evicts across district travel', () => {
  const removed = [], s = new ChunkStreamer(new CityGenerator(), { radius: 1, retainRadius: 1, maxResident: 9, onEvict: c => removed.push(c.id) });
  s.update(new Vec3(40, 0, 40)); drain(s); assert.equal(s.resident.size, 9); s.update(new Vec3(344, 0, 106)); drain(s); assert.ok(s.resident.size <= 9); assert.ok(removed.includes('0:0')); assert.equal(s.errors.length, 0); s.dispose(); assert.equal(s.resident.size, 0);
});
test('sidewalk A* connects all named destinations and avoids unconnected water nodes', () => {
  const graph = new SidewalkGraph();
  for (const poi of POIS) { const route = graph.route(poiPosition('plaza'), poiPosition(poi.id)); assert.ok(route.length > 0, `No route to ${poi.id}`); assert.ok(route.every(p => p.finite() && p.x <= 430)); }
  for (const node of graph.nodes.values()) for (const edge of node.edges) assert.ok(graph.nodes.get(edge.id).edges.some(e => e.id === node.id));
});
test('Bezier arc-length lookup progresses monotonically with near-uniform speed', () => {
  const b = new CubicBezier(new Vec3(), new Vec3(0, 0, 10), new Vec3(10, 0, 10), new Vec3(10, 0, 0), 100), samples = [];
  for (let i = 0; i <= 20; i++) samples.push(b.at(b.parameterAtDistance(b.length * i / 20)));
  const distances = samples.slice(1).map((p, i) => p.distance(samples[i])); assert.ok(Math.max(...distances) / Math.min(...distances) < 1.04); near(samples.at(-1).x, 10);
});
test('traffic loops close smoothly and opposing lanes remain separated', () => {
  const a = new RoadLoop(0, 0, 160, 160), b = new RoadLoop(0, 0, 160, 160, true);
  assert.ok(a.sample(0).position.distance(a.sample(a.length).position) < 1e-5); assert.ok(a.sample(a.length - .001).position.distance(a.sample(0).position) < .01);
  assert.ok(a.sample(40).position.distance(b.sample(40).position) > 5);
});
test('traffic signals never give both crossing directions a green light', () => {
  for (let t = 0; t < 200; t += .2) assert.ok(!(trafficSignal(80, 160, t, 'x') === 'green' && trafficSignal(80, 160, t, 'z') === 'green'));
});
test('autonomous traffic remains finite and responds to an obstacle in its lane', () => {
  const system = new TrafficSystem(), car = system.cars[0]; system.cars = [car]; car.speed = 7;
  for (let i = 0; i < 120; i++) { const obstacle = car.position.clone().addScaled(car.direction, 3); system.update(1 / 60, obstacle); }
  assert.ok(car.speed < .1); assert.ok(car.position.finite());
});
test('weather transitions remain bounded and restoration preserves the random stream', () => {
  const a = new WeatherSystem(3); a.remaining = .01; a.update(.1); assert.ok(['clear', 'cloudy'].includes(a.state));
  for (let i = 0; i < 2000; i++) a.update(.1);
  assert.ok(a.rain >= 0 && a.rain <= 1 && a.clouds >= 0 && a.clouds <= 1); const b = new WeatherSystem(); b.restore(a.snapshot()); assert.equal(b.state, a.state); near(b.rain, a.rain); near(b.random.next(), a.random.next());
});
test('wetness window recenters while preserving overlapping cells', () => {
  const w = new MovingWetnessField(); w.water.fill(.6); w.background = .2; const cell = w.extent * 2 / w.size;
  w.update(0, new Vec3(cell * 16, 0, 0), 0, 1); near(w.sample(0, 0), .6, 1e-4); assert.ok([...w.water].every(v => v >= 0 && v <= 1));
});
test('Voronoi glass fragments partition the pane without losing area', () => {
  const cells = GlassFracture.cells(2.6, 1.6, 9, 51);
  const area = cells.reduce((sum, cell) => sum + Math.abs(cell.reduce((a, p, i) => { const q = cell[(i + 1) % cell.length]; return a + p[0] * q[1] - p[1] * q[0]; }, 0)) / 2, 0);
  near(area, 2.6 * 1.6); for (const cell of cells) assert.ok(GlassFracture.mesh(cell).mesh.indices.length >= 9);
});
test('save validation rejects invalid vectors, unbounded cash, and malformed missions', () => {
  const d = validateProgress({ version: 2, cash: Infinity, player: [NaN, 0, 0], car: [1, 2], missions: { courier: { status: 'active', step: -2, variant: 2, remaining: 15 }, unknown: null }, destroyed: ['a', 'a', 3] });
  assert.equal(d.cash, 10000000); assert.deepEqual(d.player, defaultProgress().player); assert.equal(d.missions.courier.step, 0); assert.equal(d.missions.courier.variant, 2); assert.deepEqual(d.destroyed, ['a']);
});
test('reward receipts prevent double payment across saved reloads', () => {
  const storage = memory(), a = new ProgressStore(storage); assert.equal(a.reward('one', 50, 2), true); const b = new ProgressStore(storage); b.load(); assert.equal(b.reward('one', 50, 2), false); assert.equal(b.state.cash, 300);
});
test('new-visit save logic resets an isolated fixture without retaining rewards or broken props', () => {
  const storage = memory(), s = new ProgressStore(storage); s.state.cash = 900; s.state.destroyed = ['fixture']; s.state.receipts = ['fixture-reward']; s.save(); s.reset();
  const fresh = new ProgressStore(storage); fresh.load(); assert.equal(fresh.state.cash, 250); assert.deepEqual(fresh.state.destroyed, []); assert.deepEqual(fresh.state.receipts, []);
});
test('introductory story requires its ordered conversations and pays once', () => {
  const { store, system } = missions(); system.event('talk', 'leo', { place: 'cafe' }); assert.equal(store.state.missions.arrival.step, 0);
  system.event('talk', 'mira', { place: 'plaza' }); system.event('reach', 'cafe'); system.event('talk', 'leo', { place: 'cafe' }); system.event('talk', 'mira', { place: 'plaza' });
  assert.equal(store.state.missions.arrival.status, 'completed'); assert.equal(store.state.cash, 370); system.event('talk', 'mira'); assert.equal(store.state.cash, 370);
});
test('courier routes rotate and the selected destination survives a reload', () => {
  const { store, system } = missions();
  for (let i = 0; i < 3; i++) { system.accept('courier'); assert.equal(store.state.missions.courier.variant, i); system.event('interact', 'depot'); system.event('interact', ['clinic', 'market', 'harbor'][i]); }
  assert.equal(store.state.cash, 535); system.accept('courier'); store.state.missions.courier.variant = 2; store.save();
  const reloaded = new ProgressStore(store.storage); reloaded.load(); const again = new MissionSystem(reloaded); assert.equal(again.steps('courier')[1].target, 'harbor');
});
test('café shift enforces action ordering and pays only after three complete orders', () => {
  const { store, system } = missions(); system.accept('cafe'); system.event('work', 'serve', { place: 'cafe' }); assert.equal(store.state.missions.cafe.step, 0);
  for (let i = 0; i < 3; i++) for (const action of ['brew', 'pack', 'serve']) system.event('work', action, { place: 'cafe' });
  assert.equal(store.state.cash, 335); assert.equal(store.state.missions.cafe.status, 'completed'); system.accept('cafe'); assert.equal(store.state.missions.cafe.step, 0);
});
test('café timeout ends the shift and keeps the job retryable', () => {
  const { store, system } = missions(); system.accept('cafe'); system.update(151, new Vec3(104, 0, 40)); assert.equal(store.state.missions.cafe, undefined); assert.equal(store.state.cash, 250); assert.ok(system.accept('cafe'));
});
test('neighborhood voices require three distinct people', () => {
  const { store, system } = missions(); system.accept('voices'); system.event('talk', 'rosa'); system.event('talk', 'rosa'); assert.equal(store.state.missions.voices.flags.length, 1);
  system.event('talk', 'eli'); system.event('talk', 'juno'); assert.equal(store.state.missions.voices.step, 1); system.event('talk', 'mira'); assert.equal(store.state.missions.voices.status, 'completed');
});
test('streetlight repairs require tools and restore the exact persistent lamp IDs', () => {
  const { store, system } = missions(); system.accept('repair'); system.event('repair', REPAIR_LAMPS[0].id); assert.equal(store.state.repaired.length, 0);
  system.event('talk', 'sanjay', { place: 'harbor' }); for (const lamp of REPAIR_LAMPS) system.event('repair', lamp.id); system.event('talk', 'sanjay'); assert.equal(store.state.repaired.length, 3); assert.equal(store.state.cash, 430);
});
test('shipment story unlocks at the earned reputation threshold', () => {
  const { store, system } = missions(); assert.equal(system.accept('shipment'), false); store.state.reputation = 4; assert.ok(system.accept('shipment'));
  system.event('talk', 'tess'); system.event('interact', 'warehouse'); system.event('talk', 'sanjay'); system.event('talk', 'tess'); assert.equal(store.state.missions.shipment.status, 'completed');
});
test('lookout mission needs a visit followed by framing a postcard', () => {
  const { store, system } = missions(); system.accept('lookout'); system.event('photo', 'lookout'); assert.equal(store.state.missions.lookout.step, 0); system.event('reach', 'lookout'); system.event('photo', 'lookout'); assert.equal(store.state.cash, 330);
});
test('conversations persist familiarity, support topic replies, and release the speaker', () => {
  const { store, system } = missions(), citizens = new CitizenSystem(new SidewalkGraph()), dialogue = new DialogueSystem(citizens, system, store);
  dialogue.open(citizens.get('mira'), 980, { rain: 0 }); assert.equal(citizens.get('mira').talking, true); dialogue.say('What jobs can I do?'); assert.match(dialogue.current.text, /job|help|choose|Choose/i); dialogue.close(); assert.equal(citizens.get('mira').talking, false); dialogue.open(citizens.get('mira'), 980, { rain: 0 }); assert.match(dialogue.current.text, /again/); dialogue.close();
});
test('citizens commute, react to a local disturbance, and produce finite animated instance transforms', () => {
  const citizens = new CitizenSystem(new SidewalkGraph()), p = new Vec3(34, 0, 29); for (let i = 0; i < 30; i++) citizens.update(1 / 30, 980, { rain: 0 }, p);
  assert.ok(citizens.visible.length > 0); citizens.panicAt(p, 50); citizens.update(.1, 980, { rain: 0 }, p); assert.ok(citizens.agents.some(a => a.state === 'Fleeing'));
  const boxes = new InstanceBatch(MeshData.box()), spheres = new InstanceBatch(MeshData.sphere(6, 4)); drawPerson(boxes, spheres, citizens.get('mira'), 2); assert.ok(boxes.count > 10 && spheres.count > 0); assert.ok([...boxes.data.subarray(0, boxes.count * 24)].every(Number.isFinite));
});
test('on-foot controller stops at a wall and supports jumping and landing', () => {
  const world = new PhysicsWorld(), body = world.add(new RigidBody({ mass: 0, position: new Vec3(0, 1, -2), half: new Vec3(2, 1, .2) })); world.updateBroadphase();
  const p = new CityPlayer(world, new Vec3(0, 0, 0)), keys = new Set(['KeyW']); for (let i = 0; i < 120; i++) p.update(1 / 120, keys, 0); assert.ok(p.position.z > -1.5);
  keys.clear(); keys.add('Space'); p.update(1 / 120, keys, 0); keys.clear(); const y = p.position.y; for (let i = 0; i < 120; i++) p.update(1 / 120, keys, 0); assert.ok(p.grounded); near(p.position.y, .1); assert.ok(y > .1);
});
test('heightfield raycasting returns the same physical surface used by rural walking', () => {
  const w = new PhysicsWorld(); w.groundHeight = terrainHeight; const ray = w.raycast(new Vec3(40, 20, 360), new Vec3(0, -1, 0), 30); assert.ok(ray); near(ray.point.y, terrainHeight(40, 360), .001); assert.ok(ray.normal.y > .8);
});
test('complete city onboarding, proximity interaction, streaming travel, save and cleanup work together', () => {
  const storage = memory(), s = new CitySimulation({ storage }); s.update(1 / 60); assert.equal(s.getInteraction().target, 'mira'); s.interact(); assert.equal(s.dialogue.current.agent.id, 'mira'); assert.equal(s.progress.missions.arrival.step, 1); s.dialogue.close();
  s.player.position.copy(poiPosition('cafe')); s.update(1 / 60); s.interact(); assert.equal(s.dialogue.current.agent.id, 'leo'); assert.equal(s.progress.missions.arrival.step, 3); s.dialogue.close();
  s.player.position.set(34, .1, 29); s.update(1 / 60); s.interact(); assert.equal(s.progress.missions.arrival.status, 'completed'); s.dialogue.close(); assert.equal(s.progress.cash, 370);
  for (const id of ['harbor', 'market', 'lookout', 'plaza']) { s.player.position.copy(poiPosition(id)); s.streamer.update(s.player.position); drain(s.streamer); s.world.updateBroadphase(); assert.ok(s.streamer.resident.size <= 36); }
  s.buildRenderObjects(); assert.ok(s.boxes.count > 100); assert.ok(s.objects.every(o => o.bounds.min.finite() && o.bounds.max.finite())); s.save(); const saved = new ProgressStore(storage); saved.load(); assert.equal(saved.state.cash, 370); s.dispose(); assert.equal(s.streamer.resident.size, 0);
});
test('café work in the integrated game takes time and cannot be performed from another district', () => {
  const s = new CitySimulation(); s.missions.accept('cafe'); s.startWork('brew'); assert.equal(s.work, null); s.player.position.copy(poiPosition('cafe')); s.startWork('brew'); assert.ok(s.work); for (let i = 0; i < 180; i++) s.update(1 / 60); assert.equal(s.progress.missions.cafe.step, 1); assert.equal(s.work, null); s.dispose();
});
test('city shuttle uses game currency, requires a stop, and advances arrival objectives', () => {
  const s = new CitySimulation(); s.missions.event('talk', 'mira'); const before = s.progress.missions.arrival.step;
  assert.equal(s.takeShuttle('cafe'), true); assert.equal(s.progress.cash, 240); near(s.player.position.x, 104); assert.equal(s.progress.missions.arrival.step, before + 1);
  s.player.position.set(230, 0, 230); assert.equal(s.takeShuttle('plaza'), false); assert.equal(s.progress.cash, 240); s.dispose();
});
```

## tests/engine.test.js

```javascript
import test from 'node:test';
import assert from 'node:assert/strict';
import { Vec3, Quat, Mat4, AABB, Frustum } from '../src/math.js';
import { FixedStepClock, ObjectPool, FrameArena, EventBus } from '../src/core.js';
import { BVH, rayTriangle, TriangleMeshCollider } from '../src/spatial.js';
import { RigidBody, PhysicsWorld, collide, Ragdoll } from '../src/physics.js';
import { RaycastVehicle, TorqueCurve } from '../src/vehicle.js';
import { DeformableSurface } from '../src/deformable.js';
import { BudgetedCache, WetnessField, LODSelector } from '../src/resources.js';
import { MeshData } from '../src/mesh.js';
import { DayNightCycle } from '../src/engine.js';
const near=(a,b,e=1e-5)=>assert.ok(Math.abs(a-b)<e,`${a} != ${b} (tolerance ${e})`);
const vectorNear=(a,b,e=1e-5)=>{
  near(a.x,b.x,e);
  near(a.y,b.y,e);
  near(a.z,b.z,e);
};
const advance=(world,seconds)=>{
  for(let i=0;i<Math.round(seconds*120);i++)world.step(1/120);
};
let seed=42;
const random=()=>{
  seed=(1664525*seed+1013904223)>>>0;
  return seed/4294967296;
};
test('vector cross, dot, zero normalization and quaternion rotation',()=>{
  const a=new Vec3(1,0,0),b=new Vec3(0,1,0);
  vectorNear(Vec3.cross(a,b),new Vec3(0,0,1));
  near(a.dot(b),0);
  vectorNear(new Vec3().normalize(),new Vec3());
  vectorNear(Quat.axisAngle(new Vec3(0,1,0),Math.PI/2).rotate(a),new Vec3(0,0,-1));
});
test('quaternion integration is normalized and frame independent at constant omega',()=>{
  const a=new Quat(),b=new Quat(),w=new Vec3(.2,1,-.4);
  a.integrate(w,1);
  for(let i=0;i<120;i++)b.integrate(w,1/120);
  vectorNear(a.rotate(new Vec3(1,2,3)),b.rotate(new Vec3(1,2,3)));
  near(Math.hypot(b.x,b.y,b.z,b.w),1);
});
test('matrix inverse, composition and homogeneous projection round-trip',()=>{
  const m=Mat4.compose(new Vec3(3,-4,6),Quat.axisAngle(new Vec3(0,1,0),.8),new Vec3(2,3,4)),v=new Vec3(.2,1,-.8);
  vectorNear(Mat4.point(Mat4.invert(m),Mat4.point(m,v)),v);
  const vp=Mat4.mul(Mat4.perspective(1,1.4,.1,100),Mat4.lookAt(new Vec3(2,4,6),new Vec3()));
  vectorNear(Mat4.point(Mat4.invert(vp),Mat4.point(vp,v)),v,5e-5);
  assert.throws(()=>Mat4.invert(new Float32Array(16)),/Singular/);
});
test('AABB slab handles parallel rays on faces without NaN',()=>{
  const b=new AABB(new Vec3(-1,-1,-1),new Vec3(1,1,1));
  near(b.ray(new Vec3(-1,0,4),new Vec3(0,0,-1)),3);
  assert.equal(b.ray(new Vec3(-2,0,4),new Vec3(0,0,-1)),null);
  near(b.ray(new Vec3(),new Vec3(0,1,0)),0);
});
test('frustum rejects behind-camera and far objects',()=>{
  const f=new Frustum(Mat4.perspective(Math.PI/2,1,.1,20));
  assert.equal(f.intersects(new AABB(new Vec3(-1,-1,-4),new Vec3(1,1,-2))),true);
  assert.equal(f.intersects(new AABB(new Vec3(-1,-1,2),new Vec3(1,1,4))),false);
});
test('fixed step caps stalls, conserves accepted time and bounds interpolation',()=>{
  const clock=new FixedStepClock({
    hz:120,maxSubsteps:8,maxFrame:.1
  });
  let count=0;
  const first=clock.advance(.016,()=>count++);
  assert.equal(first.steps,1);
  const next=clock.advance(1,()=>count++);
  assert.equal(next.steps,8);
  assert.ok(clock.dropped>.9);
  assert.ok(next.alpha>=0&&next.alpha<=1);
  near(clock.time,count/120);
  assert.throws(()=>clock.advance(-1,()=>{
  }));
});
test('pool rejects stale handles, double releases and exhaustion',()=>{
  const p=new ObjectPool(1,()=>({
    count:0
  }),x=>x.count=0),h=p.acquire();
  p.get(h).count=7;
  assert.throws(()=>p.acquire(),/exhausted/);
  p.release(h);
  assert.throws(()=>p.release(h),/Stale/);
  const next=p.acquire();
  assert.equal(p.get(next).count,0);
  assert.throws(()=>p.get(h),/Stale/);
  assert.equal(p.highWater,1);
});
test('frame arena aligns typed allocations and detects overflow',()=>{
  const a=new FrameArena(32);
  a.allocate(Uint8Array,3);
  assert.equal(a.allocate(Float64Array,1).byteOffset,8);
  assert.throws(()=>a.allocate(Float64Array,3),/exhausted/);
  a.reset();
  assert.equal(a.allocate(Float32Array,2).byteOffset,0);
});
test('event unsubscribe is effective while dispatch uses a stable snapshot',()=>{
  const bus=new EventBus();
  let total=0;
  const off=bus.on('x',()=>total++);
  bus.emit('x',{
  });
  off();
  bus.emit('x',{
  });
  assert.equal(total,1);
});
test('ray triangle is double-sided and respects closest distance',()=>{
  const t=[new Vec3(-1,0,-1),new Vec3(1,0,-1),new Vec3(0,0,1)];
  near(rayTriangle(new Vec3(0,2,0),new Vec3(0,-1,0),...t).distance,2);
  assert.equal(rayTriangle(new Vec3(0,2,0),new Vec3(0,-1,0),...t,1),null);
  assert.ok(rayTriangle(new Vec3(0,-2,0),new Vec3(0,1,0),...t));
});
test('SAH BVH overlap queries match brute force on seeded randomized bounds',()=>{
  const items=Array.from({
    length:100
  },(_,id)=>{
    const p=new Vec3(random()*20-10,random()*20-10,random()*20-10);
    return {
      id,bounds:new AABB(p,p.clone().add(new Vec3(1,1,1)))
    };
  }),bvh=new BVH(items);
  for(let i=0;i<60;i++){
    const p=new Vec3(random()*16-8,random()*16-8,random()*16-8),q=new AABB(p,p.clone().add(new Vec3(4,4,4))),got=[];
    bvh.query(q,x=>got.push(x.id));
    assert.deepEqual(got.sort((a,b)=>a-b),items.filter(x=>q.overlaps(x.bounds)).map(x=>x.id).sort((a,b)=>a-b));
  }
});
test('BVH nearest-ray result matches brute force and refits moving bodies',()=>{
  const bodies=Array.from({
    length:40
  },()=>new RigidBody({
    shape:'sphere',position:new Vec3(random()*8-4,random()*8-4,random()*8-4)
  })),bvh=new BVH(bodies);
  for(let i=0;i<100;i++){
    const o=new Vec3(random()*20-10,random()*20-10,10),d=Vec3.sub(new Vec3(),o).normalize(),hit=bvh.raycast(o,d,(b,t)=>b.raycast(o,d,t)),brute=bodies.map(b=>b.raycast(o,d)).filter(Boolean).sort((a,b)=>a.distance-b.distance)[0];
    if(brute)near(hit.distance,brute.distance);
    else assert.equal(hit,null);
  }
  for(const b of bodies){
    b.position.x+=100;
    b.updateBounds();
  }
  bvh.refit();
  assert.equal(bvh.raycast(new Vec3(0,0,10),new Vec3(0,0,-1),(b,t)=>b.raycast(new Vec3(0,0,10),new Vec3(0,0,-1),t)),null);
});
test('GPU BVH flatten escape links terminate and all leaves cover their triangles',()=>{
  const items=Array.from({
    length:31
  },(_,id)=>({
    id,bounds:new AABB(new Vec3(id,0,0),new Vec3(id+1,1,1))
  })),bvh=new BVH(items),pack=bvh.gpuPack();
  assert.equal(bvh.nodes[0].skip,bvh.nodes.length);
  assert.equal(new Set(pack.ordered.map(x=>x.id)).size,31);
  for(let i=0;i<bvh.nodes.length;i++){
    const n=bvh.nodes[i];
    assert.ok(n.skip>i&&n.skip<=bvh.nodes.length);
    assert.ok(n.count<=4);
    near(pack.nodes[i*12+3],n.skip);
  }
});
test('triangle collider refit changes raycast surface height',()=>{
  const p=new Float32Array([-1,0,-1,1,0,-1,0,0,1]),mesh=new TriangleMeshCollider(p,new Uint32Array([0,1,2]));
  near(mesh.raycast(new Vec3(0,3,0),new Vec3(0,-1,0)).distance,3);
  for(let i=1;i<p.length;i+=3)p[i]=1;
  mesh.updateBounds();
  near(mesh.raycast(new Vec3(0,3,0),new Vec3(0,-1,0)).distance,2);
});
test('OBB SAT emits a face manifold and rejects a rotated separated box',()=>{
  const a=new RigidBody(),b=new RigidBody({
    position:new Vec3(.8,0,0)
  }),c=collide(a,b);
  assert.equal(c.length,4);
  for(const p of c){
    near(p.depth,.2);
    vectorNear(p.normal,new Vec3(1,0,0));
  }
  b.position.set(3,0,0);
  b.rotation=Quat.axisAngle(new Vec3(0,1,0),.7);
  assert.equal(collide(a,b).length,0);
});
test('sphere inside an OBB has a finite outward resolving contact',()=>{
  const box=new RigidBody({
    mass:0
  }),sphere=new RigidBody({
    shape:'sphere',radius:.2,position:new Vec3(.4,0,0)
  }),c=collide(sphere,box)[0];
  assert.ok(c.normal.finite());
  near(c.depth,.3);
  vectorNear(c.normal,new Vec3(-1,0,0));
});
test('falling sphere settles on ground and does not gain energy at rest',()=>{
  const w=new PhysicsWorld(),b=w.add(new RigidBody({
    shape:'sphere',radius:.5,position:new Vec3(0,3,0)
  }));
  advance(w,5);
  near(b.position.y,.5,.015);
  assert.ok(b.velocity.length()<.05);
  assert.ok(b.angularVelocity.length()<.05);
});
test('stacked boxes remain separated and finite after six seconds',()=>{
  const w=new PhysicsWorld({
    iterations:18
  }),bs=[];
  for(let i=0;i<3;i++)bs.push(w.add(new RigidBody({
    position:new Vec3(0,.52+i*1.02,0),restitution:0
  })));
  advance(w,6);
  for(let i=0;i<3;i++){
    assert.ok(bs[i].position.finite());
    near(bs[i].position.y,.5+i,.08);
    assert.ok(bs[i].velocity.length()<.25);
  }
});
test('equal-mass elastic impact preserves linear momentum',()=>{
  const w=new PhysicsWorld({
    ground:false,gravity:new Vec3()
  }),a=w.add(new RigidBody({
    shape:'sphere',position:new Vec3(-.49,0,0),restitution:1,friction:0
  })),b=w.add(new RigidBody({
    shape:'sphere',position:new Vec3(.49,0,0),restitution:1,friction:0
  }));
  a.linearDamping=b.linearDamping=0;
  a.velocity.x=2;
  b.velocity.x=-2;
  w.step(1/120);
  near(a.velocity.x+b.velocity.x,0);
  near(a.velocity.x,-2);
  near(b.velocity.x,2);
});
test('rigid body off-center impulse produces angular velocity',()=>{
  const b=new RigidBody({
    mass:2
  });
  b.impulse(new Vec3(1,0,0),new Vec3(0,1,0));
  near(b.velocity.x,.5);
  assert.ok(b.angularVelocity.z<0);
});
test('torque-free asymmetric rigid body approximately conserves rotational energy',()=>{
  const w=new PhysicsWorld({
    ground:false,gravity:new Vec3()
  }),b=w.add(new RigidBody({
    half:new Vec3(.5,1,1.5)
  }));
  b.angularDamping=0;
  b.angularVelocity.set(.4,.7,.2);
  const energy=()=>b.angularVelocity.dot(b.inertiaTensorMul(b.angularVelocity))*.5,initial=energy();
  advance(w,2);
  assert.ok(Math.abs(energy()/initial-1)<.01);
  assert.ok(b.angularVelocity.finite());
});
test('raycast world returns nearest body and supports ignored chassis',()=>{
  const w=new PhysicsWorld(),b=w.add(new RigidBody({
    position:new Vec3(0,2,0)
  }));
  w.updateBroadphase();
  near(w.raycast(new Vec3(0,5,0),new Vec3(0,-1,0)).distance,2.5);
  near(w.raycast(new Vec3(0,5,0),new Vec3(0,-1,0),10,b).distance,5);
});
test('articulated ragdoll falls with bounded anchor separation',()=>{
  const w=new PhysicsWorld({
    iterations:18
  }),r=new Ragdoll(w,new Vec3(0,4,0));
  advance(w,4);
  for(const b of r.bodies){
    assert.ok(b.position.finite()&&b.velocity.finite());
    assert.ok(b.position.y>-.15&&b.position.y<6);
  }
  const max=Math.max(...r.joints.map(j=>j.a.localToWorld(j.localA).distance(j.b.localToWorld(j.localB))));
  assert.ok(max<.2,`joint error ${max}`);
  r.dispose();
  assert.equal(w.bodies.length,0);
});
test('torque curve interpolates and rejects unordered input',()=>{
  const c=new TorqueCurve([[1000,100],[2000,200],[6000,0]]);
  near(c.at(1500),150);
  near(c.at(7000),0);
  assert.throws(()=>new TorqueCurve([[2,1],[1,2]]));
});
test('four suspension rays support chassis and throttle moves it forward',()=>{
  const w=new PhysicsWorld(),v=new RaycastVehicle(w);
  advance(w,2);
  assert.ok(v.wheels.every(w=>w.load>0));
  near(v.body.position.y,.717,.07);
  v.setControls({
    throttle:1
  });
  advance(w,2);
  assert.ok(v.body.velocity.z< -1,`velocity ${v.body.velocity.z}`);
  assert.ok(v.body.position.finite());
  assert.ok(v.rpm>=850&&v.rpm<=6800);
  v.dispose();
  assert.equal(w.bodies.length,0);
});
test('surface pinning, XPBD distance constraints and raycast refit work',()=>{
  const s=new DeformableSurface({
    resolution:6
  }),w=new PhysicsWorld(),original=Vec3.from(s.positions);
  for(let i=0;i<120;i++)s.step(1/120,w);
  vectorNear(Vec3.from(s.positions),original);
  assert.ok([...s.positions].every(Number.isFinite));
  const stretch=Math.max(...s.edges.map(e=>Math.abs(Vec3.from(s.positions,e.a*3).distance(Vec3.from(s.positions,e.b*3))-e.rest)));
  assert.ok(stretch<.1,`stretch ${stretch}`);
  assert.ok(s.collider.raycast(new Vec3(0,10,0),new Vec3(0,-1,0)));
  assert.ok([...s.normals()].every(Number.isFinite));
});
test('deformable particles collide with a rigid sphere and the floor',()=>{
  const s=new DeformableSurface({
    origin:new Vec3(0,.8,0),resolution:4,pinCorners:false
  }),w=new PhysicsWorld();
  w.add(new RigidBody({
    shape:'sphere',mass:0,radius:1,position:new Vec3()
  }));
  for(let i=0;i<120;i++)s.step(1/120,w);
  for(let i=0;i<s.count;i++){
    const p=Vec3.from(s.positions,i*3);
    assert.ok(p.y>=.024);
    assert.ok(p.length()>=1.02-.002);
  }
});
test('wetness grows in rain, dries without rain and remains bounded',()=>{
  const f=new WetnessField({
    size:16
  });
  f.rain=1;
  for(let i=0;i<600;i++)f.step(1/60,1);
  assert.ok(f.mean>.7);
  const wet=f.mean;
  f.rain=0;
  for(let i=0;i<600;i++)f.step(1/60,1);
  assert.ok(f.mean<wet);
  assert.ok([...f.water].every(v=>v>=0&&v<=1));
  assert.ok(f.sample(0,0)>0);
});
test('LOD hysteresis coarsens with distance without threshold chatter',()=>{
  const l=new LODSelector(),levels=[{
    error:0
  },{
    error:.05
  },{
    error:.3
  }];
  assert.equal(l.select('a',levels,2,800,1),0);
  assert.equal(l.select('a',levels,500,800,1),2);
  const v=l.select('a',levels,100,800,1);
  assert.ok(v>=1);
  assert.equal(l.select('a',levels,2,800,1),0);
});
test('day-night directions are normalized and sunlight vanishes at midnight',()=>{
  const d=new DayNightCycle(0);
  near(d.direction.length(),1);
  near(d.intensity,0);
  d.hour=12;
  d.update(0);
  near(d.direction.y,1);
  assert.ok(d.intensity>5);
  d.hour=23.99;
  d.running=true;
  d.update(1);
  assert.ok(d.hour<1);
});
test('mesh topology and winding agree with outward normals',()=>{
  for(const m of [MeshData.box(),MeshData.sphere(12,6)]){
    assert.equal(m.normals.length,m.positions.length);
    for(let i=0;i<m.indices.length;i+=3){
      const [a,b,c]=Array.from(m.indices.slice(i,i+3)),n=Vec3.cross(Vec3.sub(Vec3.from(m.positions,b*3),Vec3.from(m.positions,a*3)),Vec3.sub(Vec3.from(m.positions,c*3),Vec3.from(m.positions,a*3)));
      if(n.length()>1e-7)assert.ok(n.dot(Vec3.from(m.normals,a*3))>=0);
    }
  }
});
test('streaming cache evicts unpinned LRU, protects pins and enforces byte reservations',async()=>{
  const disposed=[],c=new BudgetedCache({
    budget:20,concurrency:1,dispose:v=>disposed.push(v)
  }),a=c.request('a',{
    bytes:10,pin:true,load:async()=>1
  });
  await a.promise;
  await c.request('b',{
    bytes:10,load:async()=>2
  }).promise;
  await c.request('c',{
    bytes:10,load:async()=>3
  }).promise;
  assert.deepEqual(disposed,[2]);
  assert.equal(c.get('a'),1);
  assert.equal(c.get('c'),3);
  assert.ok(c.used+c.reserved<=20);
  assert.throws(()=>c.request('huge',{
    bytes:21,load:async()=>0
  }));
  a.release();
  c.disposeAll();
  assert.equal(c.used,0);
});
test('streaming cancellation rejects queued requests and late uploads are disposed',async()=>{
  let complete;
  const disposed=[],c=new BudgetedCache({
    budget:20,concurrency:1,dispose:v=>disposed.push(v)
  });
  const a=c.request('a',{
    bytes:10,load:()=>new Promise(resolve=>complete=resolve)
  });
  await Promise.resolve();
  const b=c.request('b',{
    bytes:10,load:async()=>2
  });
  const rejected=assert.rejects(b.promise,/Cancelled/);
  c.cancel('b');
  await rejected;
  const rejectedA=assert.rejects(a.promise,/Cancelled/);
  c.cancel('a');
  complete(1);
  await rejectedA;
  await new Promise(resolve=>setTimeout(resolve,0));
  assert.deepEqual(disposed,[1]);
  assert.equal(c.used,0);
  assert.equal(c.reserved,0);
  c.disposeAll();
});
test('streaming loader failures release reservations and allow retry',async()=>{
  const c=new BudgetedCache({
    budget:10
  }),h=c.request('x',{
    bytes:10,load:async()=>{
      throw new Error('decode');
    }
  });
  await assert.rejects(h.promise,/decode/);
  await new Promise(resolve=>setTimeout(resolve,0));
  assert.equal(c.reserved,0);
  await c.request('x',{
    bytes:10,load:async()=>8
  }).promise;
  assert.equal(c.get('x'),8);
  c.disposeAll();
});
test('shared streaming leases release idempotently without unpinning another owner',async()=>{
  const c=new BudgetedCache({
    budget:20
  }),a=c.request('a',{
    bytes:10,pin:true,load:async()=>1
  }),b=c.request('a',{
    bytes:10,pin:true,load:async()=>1
  }),unpinned=c.request('a',{
    bytes:10,load:async()=>1
  });
  await a.promise;
  a.release();
  a.release();
  unpinned.release();
  assert.equal(c.entries.get('a').pins,1);
  b.release();
  assert.equal(c.entries.get('a').pins,0);
  c.disposeAll();
});
```

## tests/instancing.test.js

```javascript
import test from 'node:test';
import assert from 'node:assert/strict';
import { MeshData, GPUMesh } from '../src/mesh.js';
import { InstanceBatch } from '../src/world/geometry.js';
import { Vec3, Quat } from '../src/math.js';

test('GPU mesh instances allocate, reuse, update and release their exact attribute buffers', () => {
  let next = 0, arrayBuffer = null, vao = null, draws = 0, uploads = 0;
  const buffers = new Map(), attributes = new Map(), arrays = new Set();
  const gl = {
    ARRAY_BUFFER: 1, ELEMENT_ARRAY_BUFFER: 2, STATIC_DRAW: 3, DYNAMIC_DRAW: 4, FLOAT: 5, TRIANGLES: 6, UNSIGNED_INT: 7,
    createVertexArray() { const id = ++next; arrays.add(id); return id; }, bindVertexArray(id) { vao = id; },
    createBuffer() { const id = ++next; buffers.set(id, 0); return id; }, bindBuffer(target, id) { arrayBuffer = id; },
    bufferData(target, data) { buffers.set(arrayBuffer, data.byteLength); uploads++; },
    bufferSubData(target, offset, data) { assert.ok(buffers.get(arrayBuffer) >= data.byteLength + offset); uploads++; },
    enableVertexAttribArray() {}, vertexAttribPointer(location, size, type, normalized, stride, offset) { attributes.set(location, { size, stride, offset, buffer: arrayBuffer }); },
    vertexAttribDivisor(location, divisor) { attributes.get(location).divisor = divisor; },
    drawElements() { draws++; },
    drawElementsInstanced(mode, count, type, offset, instances) { assert.ok(vao); assert.equal(attributes.get(5).stride, 96); assert.equal(attributes.get(9).offset, 64); assert.equal(attributes.get(10).offset, 80); assert.equal(attributes.get(5).divisor, 1); assert.ok(buffers.get(attributes.get(5).buffer) >= instances * 96); draws++; },
    deleteBuffer(id) { buffers.delete(id); }, deleteVertexArray(id) { arrays.delete(id); }
  };
  const data = MeshData.box(), batch = new InstanceBatch(data, 2);
  batch.add(new Vec3(), new Quat(), new Vec3(1, 1, 1), [.5, .2, .1]); batch.add(new Vec3(2, 0, 0), new Quat(), new Vec3(1, 1, 1), [.1, .5, .2]); batch.finish();
  const mesh = new GPUMesh(gl, data); mesh.draw(batch.data, 2); const first = uploads; mesh.draw(batch.data, 2); assert.equal(uploads, first);
  mesh.draw(batch.data, 2, 'changed'); assert.equal(uploads, first + 1); assert.equal(draws, 3); mesh.dispose(); assert.equal(buffers.size, 0); assert.equal(arrays.size, 0);
});
```

## tests/renderer.test.js

```javascript
import test from 'node:test';
import assert from 'node:assert/strict';
import { GPUOcclusion } from '../src/renderer.js';
import { AABB, Vec3, Mat4 } from '../src/math.js';
function fixture(){
  const results=new Map();
  let next=0;
  const gl={
    ANY_SAMPLES_PASSED_CONSERVATIVE:1,QUERY_RESULT_AVAILABLE:2,QUERY_RESULT:3,createQuery(){
      const id=++next;
      results.set(id,{
        available:false,visible:true
      });
      return id;
    },getQueryParameter(q,name){
      return results.get(q)[name===2?'available':'visible'];
    },beginQuery(){
    },endQuery(){
    },deleteQuery(q){
      results.delete(q);
    }
  };
  const culling=new GPUOcclusion(gl,{
    draw(){
    }
  }),object={
    id:1,static:true,bounds:new AABB(new Vec3(-1,-1,-1),new Vec3(1,1,1))
  },program={
    i(){ return this; }, m4(){
    }
  },eye=new Vec3(0,4,10);
  return {
    results,culling,object,program,eye
  };
}
test('occlusion only rejects static objects after an available, current-camera query',()=>{
  const f=fixture(),vp=Mat4.identity();
  f.culling.begin(vp,'static');
  assert.equal(f.culling.visible(f.object),true);
  f.culling.issue(f.object,f.program,f.eye);
  assert.equal(f.culling.visible(f.object),true);
  f.results.get(1).available=true;
  f.results.get(1).visible=false;
  f.culling.begin(vp,'static');
  assert.equal(f.culling.visible(f.object),false);
  assert.equal(f.culling.visible({
    ...f.object,static:false
  }),true);
  const changed=vp.slice();
  changed[12]=.1;
  f.culling.begin(changed,'static');
  assert.equal(f.culling.visible(f.object),true);
  f.culling.dispose();
  assert.equal(f.results.size,0);
});
test('late occlusion results from an obsolete camera or moved static scene cannot hide objects',()=>{
  const f=fixture(),vp=Mat4.identity();
  f.culling.begin(vp,'v1');
  f.culling.issue(f.object,f.program,f.eye);
  const changed=vp.slice();
  changed[12]=1;
  f.culling.begin(changed,'v1');
  f.results.get(1).available=true;
  f.results.get(1).visible=false;
  f.culling.begin(changed,'v1');
  assert.equal(f.culling.visible(f.object),true);
  f.culling.issue(f.object,f.program,f.eye);
  f.culling.begin(changed,'v2');
  f.results.get(1).available=true;
  f.results.get(1).visible=false;
  f.culling.begin(changed,'v2');
  assert.equal(f.culling.visible(f.object),true);
  f.culling.dispose();
});
```

## tools/check.mjs

```javascript
import { readdir } from 'node:fs/promises';
import { spawnSync } from 'node:child_process';
const paths=[];
async function walk(root){
  for(const e of await readdir(root,{
    withFileTypes:true
  })){
    const p=`${root}/${e.name}`;
    if(e.isDirectory()&&!e.name.startsWith('.'))await walk(p);
    else if(e.isFile()&&/\.(m?js)$/.test(e.name))paths.push(p);
  }
}
await walk('src');
await walk('tests');
await walk('tools');
for(const p of paths){
  const r=spawnSync(process.execPath,['--check',p],{
    encoding:'utf8'
  });
  if(r.status!==0){
    process.stderr.write(r.stderr);
    process.exit(r.status||1);
  }
}
console.log(`Syntax checked ${paths.length} JavaScript modules.`);
```

## tools/export-city-scene.mjs

```javascript
import * as shaders from '../src/shaders.js';
import { CitySimulation } from '../src/game/city-simulation.js';
import { Vec3, Mat4, Quat, AABB } from '../src/math.js';
import { BVH } from '../src/spatial.js';

const simulation = new CitySimulation();
if(process.argv.includes('--night'))simulation.progress.minutes=20*60;
while (simulation.streamer.pending.size) simulation.streamer.pump(1000);
simulation.update(1 / 60);
simulation.camera.distance = 80; simulation.camera.pitch = .47; simulation.camera.yaw = .68;
simulation.camera.initialized = false; simulation.camera.tick(1); simulation.camera.eye.copy(simulation.camera.target).add(new Vec3(Math.sin(.68) * Math.cos(.47) * 80, Math.sin(.47) * 80, Math.cos(.68) * Math.cos(.47) * 80));
simulation.camera.update(960 / 540);
const objects = simulation.buildRenderObjects(), shadowRanges = [], mesh = { positions: [], normals: [], uvs: [], indices: [], colors: [], materials: [] };
function append(data, model, color, surface, perVertex) {
  const offset = mesh.positions.length / 3, sx = model[0] ** 2 + model[1] ** 2 + model[2] ** 2, sy = model[4] ** 2 + model[5] ** 2 + model[6] ** 2, sz = model[8] ** 2 + model[9] ** 2 + model[10] ** 2;
  for (let i = 0; i < data.positions.length; i += 3) {
    const p = Mat4.point(model, Vec3.from(data.positions, i)), n = Vec3.from(data.normals, i), normal = new Vec3(model[0] * n.x / sx + model[4] * n.y / sy + model[8] * n.z / sz, model[1] * n.x / sx + model[5] * n.y / sy + model[9] * n.z / sz, model[2] * n.x / sx + model[6] * n.y / sy + model[10] * n.z / sz).normalize();
    p.toArray(mesh.positions, mesh.positions.length); normal.toArray(mesh.normals, mesh.normals.length);
    mesh.colors.push(...(perVertex && data.colors ? data.colors.subarray(i, i + 3) : color));
    const m = perVertex && data.materials ? data.materials.subarray(i / 3 * 4, i / 3 * 4 + 4) : surface; mesh.materials.push(...m);
  }
  for (const uv of data.uvs) mesh.uvs.push(uv);
  for (const index of data.indices) mesh.indices.push(offset + index);
}
for (const object of objects) {
  const start = mesh.indices.length;
  const data = object.lods[0].mesh;
  if (object.instances) for (let i = 0; i < object.instanceCount; i++) append(data, object.instances.subarray(i * 24, i * 24 + 16), [...object.instances.subarray(i * 24 + 16, i * 24 + 19)], [...object.instances.subarray(i * 24 + 20, i * 24 + 24)], false);
  else append(data, object.model, object.material.color, [object.material.roughness || .8, object.material.metallic || 0, 0, 1], object.material.vertexMaterials);
  if(object.castShadow!==false)shadowRanges.push([start,mesh.indices.length-start]);
}
const eye = simulation.camera.eye, sun = simulation.sun.direction, target = simulation.camera.target;
// The native city preview uses the raster path; the separate GPU test exercises BVH rays.
const bvh = new BVH([{ bounds: new AABB(new Vec3(-1, -2, -1), new Vec3(1, -1, 1)), a: new Vec3(-1, -1, -1), b: new Vec3(1, -1, -1), c: new Vec3(0, -1, 1) }]);
const pack = bvh.gpuPack(), triangle = new Float32Array(12); pack.ordered[0].a.toArray(triangle); pack.ordered[0].b.toArray(triangle, 4); pack.ordered[0].c.toArray(triangle, 8);
console.log(JSON.stringify({ shaders, mesh, shadowRanges, models: [[...Mat4.identity()]], eye: eye.toArray(), view: [...simulation.camera.view], vp: [...simulation.camera.vp], inverseVP: [...simulation.camera.inverseVP], sun: sun.toArray(), sunColor: simulation.sun.color.toArray(), intensity: simulation.sun.intensity, vertexMaterials: 1, nightFactor: Math.max(0,Math.min(1,(.15-simulation.sun.direction.y)/.28)), lights:simulation.sun.lights, lightVP: [...Mat4.mul(Mat4.ortho(-100, 100, -100, 100, 1, 450), Mat4.lookAt(sun.clone().scale(200).add(target), target))], nodes: [...pack.nodes], triangles: [...triangle], nodeCount: 1 }));
simulation.dispose();
```

## tools/export-source.mjs

```javascript
import { readFile, writeFile, readdir } from 'node:fs/promises';
import { createHash } from 'node:crypto';

const files = ['package.json', 'index.html', 'style.css', 'lab.html', 'lab.css'];
async function sourceFiles(directory) {
  for (const entry of (await readdir(directory, { withFileTypes: true })).sort((a,b)=>a.name.localeCompare(b.name))) {
    const path = `${directory}/${entry.name}`;
    if(entry.isDirectory()&&!entry.name.startsWith('.')) await sourceFiles(path);
    else if(entry.isFile()&&/\.(js|mjs|py)$/.test(entry.name)) files.push(path);
  }
}
for(const directory of ['src','tests','tools'])await sourceFiles(directory);
const languages = { json: 'json', html: 'html', css: 'css', js: 'javascript', mjs: 'javascript', py: 'python' };
let text = '# Aurora Phase 2 — complete source listing\n\n';
text += 'This is the complete textual source, including shaders, runtime integration, tests and validation tools. '; 
text += 'Copy files using the exact paths shown, or use the supplied ZIP, which also contains all PNG texture assets. '; 
text += 'The architecture and mathematical derivations are in `PHASE2_ARCHITECTURE.md`.\n\n';
for (const file of files) {
  const contents = await readFile(file, 'utf8');
  text += `## ${file}\n\n\`\`\`${languages[file.split('.').at(-1)]}\n${contents.trimEnd()}\n\`\`\`\n\n`;
}
text += '## Included texture assets\n\n| File | Bytes | SHA-256 |\n| --- | ---: | --- |\n';
for (const file of (await readdir('assets')).sort()) {
  const contents = await readFile(`assets/${file}`);
  text += `| assets/${file} | ${contents.length} | ${createHash('sha256').update(contents).digest('hex')} |\n`;
}
await writeFile('docs/FULL_SOURCE.md', text);
console.log(`Exported ${files.length} complete source files.`);
```

## tools/serve.mjs

```javascript
import http from 'node:http';
import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import path from 'node:path';
const args=process.argv.slice(2),value=(flag,fallback)=>{
  const i=args.indexOf(flag);
  return i<0?fallback:args[i+1];
},port=Number(value('--port',process.env.PORT||3001)),host=value('--host','127.0.0.1'),root=process.cwd();
const types={
  '.html':'text/html; charset=utf-8','.js':'text/javascript; charset=utf-8','.mjs':'text/javascript; charset=utf-8','.css':'text/css; charset=utf-8','.json':'application/json; charset=utf-8','.png':'image/png','.md':'text/plain; charset=utf-8','.zip':'application/zip'
};
const server=http.createServer(async(req,res)=>{
  try{
    if(!['GET','HEAD'].includes(req.method)){
      res.writeHead(405);
      res.end();
      return;
    }
    const url=new URL(req.url,'http://localhost'),pathname=decodeURIComponent(url.pathname),file=path.resolve(root,`.${pathname==='/'?'/index.html':pathname}`);
    if(!file.startsWith(`${root}${path.sep}`)||pathname.split('/').some(s=>s.startsWith('.'))){
      res.writeHead(403);
      res.end('Forbidden');
      return;
    }
    const info=await stat(file);
    if(!info.isFile()){
      res.writeHead(404);
      res.end('Not found');
      return;
    }
    res.writeHead(200,{
      'Content-Type':types[path.extname(file)]||'application/octet-stream','Content-Length':info.size,'Cache-Control':'no-store','X-Content-Type-Options':'nosniff'
    });
    if(req.method==='HEAD')res.end();
    else createReadStream(file).on('error',()=>res.destroy()).pipe(res);
  }catch{
    res.writeHead(404);
    res.end('Not found');
  }
});
server.on('error',error=>{
  console.error(error.message);
  process.exitCode=1;
});
server.listen(port,host,()=>console.log(`Aurora / Port Meridian: http://${host}:${port}`));
for(const signal of ['SIGTERM','SIGINT'])process.on(signal,()=>server.close(()=>process.exit(0)));
```

## tools/validate_gpu.py

```python
"""Compile the actual GLSL and execute the render passes in a native ES 3 context.

Optional Linux validation; requires system libEGL and a Mesa EGL driver.
This checks GLES behavior, not browser WebGL conformance or performance.
"""
import ctypes as C
import ctypes.util
import json
from pathlib import Path
import subprocess
import sys
import struct
import zlib

CITY = "--city" in sys.argv
W, H = (960, 540) if CITY else (64, 48)

ROOT = Path(__file__).resolve().parents[1]
node_code = """
import * as shaders from './src/shaders.js';
import {Vec3,Quat,Mat4,AABB} from './src/math.js';
import {MeshData} from './src/mesh.js';
import {BVH} from './src/spatial.js';
const mesh=MeshData.box(),eye=new Vec3(4,3,6),view=Mat4.lookAt(eye,new Vec3()),vp=Mat4.mul(Mat4.perspective(1,64/48,.1,100),view),sun=new Vec3(-.5,.8,.4).normalize();
const models=[Mat4.identity(),Mat4.compose(new Vec3(0,-1.2,0),new Quat(),new Vec3(6,.2,6))];
const triangles=[];
for(const model of models)for(let i=0;i<mesh.indices.length;i+=3){const p=[0,1,2].map(j=>Mat4.point(model,Vec3.from(mesh.positions,mesh.indices[i+j]*3)));triangles.push({a:p[0],b:p[1],c:p[2],bounds:new AABB().include(p[0]).include(p[1]).include(p[2])});}
const bvh=new BVH(triangles),pack=bvh.gpuPack(),tri=new Float32Array(pack.ordered.length*12);pack.ordered.forEach((t,i)=>{t.a.toArray(tri,i*12);t.b.toArray(tri,i*12+4);t.c.toArray(tri,i*12+8);});
console.log(JSON.stringify({shaders,mesh:{positions:[...mesh.positions],normals:[...mesh.normals],uvs:[...mesh.uvs],indices:[...mesh.indices]},models:models.map(x=>[...x]),eye:eye.toArray(),view:[...view],vp:[...vp],inverseVP:[...Mat4.invert(vp)],sun:sun.toArray(),lightVP:[...Mat4.mul(Mat4.ortho(-8,8,-8,8,1,30),Mat4.lookAt(sun.clone().scale(15),new Vec3()))],nodes:[...pack.nodes],triangles:[...tri],nodeCount:bvh.nodes.length}));
"""
scene = json.loads(subprocess.check_output((['node', 'tools/export-city-scene.mjs']+(['--night'] if '--night' in sys.argv else [])) if CITY else ['node', '--input-type=module', '-e', node_code], cwd=ROOT, text=True))

egl_path = ctypes_path = C.util.find_library('EGL')
if not egl_path:
    raise SystemExit('Native shader validation unavailable: system libEGL not found.')
egl = C.CDLL(egl_path)
def api(lib, name, result, *args):
    fn = getattr(lib, name)
    fn.restype, fn.argtypes = result, args
    return fn

voidp, integer, uint, floating = C.c_void_p, C.c_int, C.c_uint, C.c_float
platform_display = api(egl, 'eglGetPlatformDisplay', voidp, uint, voidp, C.POINTER(integer))
initialize = api(egl, 'eglInitialize', uint, voidp, C.POINTER(integer), C.POINTER(integer))
bind_api = api(egl, 'eglBindAPI', uint, uint)
choose = api(egl, 'eglChooseConfig', uint, voidp, C.POINTER(integer), C.POINTER(voidp), integer, C.POINTER(integer))
create_surface = api(egl, 'eglCreatePbufferSurface', voidp, voidp, voidp, C.POINTER(integer))
create_context = api(egl, 'eglCreateContext', voidp, voidp, voidp, voidp, C.POINTER(integer))
make_current = api(egl, 'eglMakeCurrent', uint, voidp, voidp, voidp, voidp)
get_proc = api(egl, 'eglGetProcAddress', voidp, C.c_char_p)
display = platform_display(0x31DD, None, None)
major, minor = integer(), integer()
if not display or not initialize(display, C.byref(major), C.byref(minor)):
    raise SystemExit('Native shader validation unavailable: surfaceless EGL initialization failed.')
assert bind_api(0x30A0), 'Cannot bind OpenGL ES API'
attributes = (integer * 15)(0x3033, 1, 0x3040, 0x40, 0x3024, 8, 0x3023, 8, 0x3022, 8, 0x3021, 8, 0x3025, 24, 0x3038)
config, count = voidp(), integer()
assert choose(display, attributes, C.byref(config), 1, C.byref(count)) and count.value, 'No ES 3 pbuffer configuration'
surface = create_surface(display, config, (integer * 5)(0x3057, W, 0x3056, H, 0x3038))
context = create_context(display, config, None, (integer * 3)(0x3098, 3, 0x3038))
assert surface and context and make_current(display, surface, surface, context), 'Cannot create native ES context'
def gl(name, result, *args):
    address = get_proc(name.encode())
    if not address:
        raise RuntimeError(f'GL entry point missing: {name}')
    return C.CFUNCTYPE(result, *args)(address)

get_string = gl('glGetString', C.c_char_p, uint)
get_error = gl('glGetError', uint)
create_shader = gl('glCreateShader', uint, uint)
shader_source = gl('glShaderSource', None, uint, integer, C.POINTER(C.c_char_p), C.POINTER(integer))
compile_shader = gl('glCompileShader', None, uint)
get_shader = gl('glGetShaderiv', None, uint, uint, C.POINTER(integer))
shader_log = gl('glGetShaderInfoLog', None, uint, integer, C.POINTER(integer), C.c_char_p)
create_program = gl('glCreateProgram', uint)
attach_shader = gl('glAttachShader', None, uint, uint)
link_program = gl('glLinkProgram', None, uint)
get_program = gl('glGetProgramiv', None, uint, uint, C.POINTER(integer))
program_log = gl('glGetProgramInfoLog', None, uint, integer, C.POINTER(integer), C.c_char_p)
use = gl('glUseProgram', None, uint)
uniform_location = gl('glGetUniformLocation', integer, uint, C.c_char_p)
uniform_i = gl('glUniform1i', None, integer, integer)
uniform_f = gl('glUniform1f', None, integer, floating)
uniform_2 = gl('glUniform2f', None, integer, floating, floating)
uniform_3 = gl('glUniform3f', None, integer, floating, floating, floating)
uniform_4m = gl('glUniformMatrix4fv', None, integer, integer, C.c_ubyte, C.POINTER(floating))
gen_textures = gl('glGenTextures', None, integer, C.POINTER(uint))
bind_texture = gl('glBindTexture', None, uint, uint)
active_texture = gl('glActiveTexture', None, uint)
tex_image = gl('glTexImage2D', None, uint, integer, integer, integer, integer, integer, uint, uint, voidp)
tex_param = gl('glTexParameteri', None, uint, uint, integer)
gen_fbo = gl('glGenFramebuffers', None, integer, C.POINTER(uint))
bind_fbo = gl('glBindFramebuffer', None, uint, uint)
attach_texture = gl('glFramebufferTexture2D', None, uint, uint, uint, uint, integer)
draw_buffers = gl('glDrawBuffers', None, integer, C.POINTER(uint))
read_buffer = gl('glReadBuffer', None, uint)
fbo_status = gl('glCheckFramebufferStatus', uint, uint)
gen_vao = gl('glGenVertexArrays', None, integer, C.POINTER(uint))
bind_vao = gl('glBindVertexArray', None, uint)
gen_buffers = gl('glGenBuffers', None, integer, C.POINTER(uint))
bind_buffer = gl('glBindBuffer', None, uint, uint)
buffer_data = gl('glBufferData', None, uint, C.c_ssize_t, voidp, uint)
enable_attribute = gl('glEnableVertexAttribArray', None, uint)
attribute = gl('glVertexAttribPointer', None, uint, integer, uint, C.c_ubyte, integer, voidp)
viewport = gl('glViewport', None, integer, integer, integer, integer)
clear_color = gl('glClearColor', None, floating, floating, floating, floating)
clear = gl('glClear', None, uint)
enable = gl('glEnable', None, uint)
disable = gl('glDisable', None, uint)
polygon_offset = gl('glPolygonOffset', None, floating, floating)
draw_elements = gl('glDrawElements', None, uint, integer, uint, voidp)
attribute_divisor = gl('glVertexAttribDivisor',None,uint,uint)
draw_instanced = gl('glDrawElementsInstanced',None,uint,integer,uint,voidp,integer)
uniform_4v = gl('glUniform4fv',None,integer,integer,C.POINTER(floating))
draw_arrays = gl('glDrawArrays', None, uint, integer, integer)
read_pixels = gl('glReadPixels', None, integer, integer, integer, integer, uint, uint, voidp)

def stage(kind, source):
    handle = create_shader(kind)
    text = C.c_char_p(source.encode())
    shader_source(handle, 1, C.byref(text), None)
    compile_shader(handle)
    success = integer()
    get_shader(handle, 0x8B81, C.byref(success))
    if not success.value:
        log = C.create_string_buffer(32768)
        shader_log(handle, len(log), None, log)
        raise RuntimeError(log.value.decode())
    return handle

def program(vs, fs):
    result = create_program()
    attach_shader(result, stage(0x8B31, scene['shaders'][vs]))
    attach_shader(result, stage(0x8B30, scene['shaders'][fs]))
    link_program(result)
    success = integer()
    get_program(result, 0x8B82, C.byref(success))
    if not success.value:
        log = C.create_string_buffer(32768)
        program_log(result, len(log), None, log)
        raise RuntimeError(log.value.decode())
    return result

programs = {name: program(vs, fs) for name, vs, fs in [
    ('geometry', 'meshVS', 'geometryFS'), ('shadow', 'shadowVS', 'shadowFS'),
    ('query', 'shadowVS', 'queryFS'), ('gi', 'fullscreenVS', 'giFS'),
    ('lighting', 'fullscreenVS', 'lightingFS'), ('rain', 'rainVS', 'rainFS'), ('aa', 'fullscreenVS', 'aaFS')]}

def uniform(p, name, value, kind='f'):
    loc = uniform_location(p, name.encode())
    if kind == 'i': uniform_i(loc, int(value))
    elif kind == 'm': uniform_4m(loc, 1, 0, (floating * 16)(*value))
    elif isinstance(value, list) and len(value) == 3: uniform_3(loc, *value)
    elif isinstance(value, list) and len(value) == 2: uniform_2(loc, *value)
    else: uniform_f(loc, value)

def texture(width, height, internal=0x8058, fmt=0x1908, typ=0x1401, data=None):
    handle = uint()
    gen_textures(1, C.byref(handle)); bind_texture(0x0DE1, handle)
    tex_image(0x0DE1, 0, internal, width, height, 0, fmt, typ, data)
    for k, v in [(0x2801, 0x2600), (0x2800, 0x2600), (0x2802, 0x812F), (0x2803, 0x812F)]: tex_param(0x0DE1, k, v)
    return handle.value

def fbo(colors, depth=None):
    handle = uint(); gen_fbo(1, C.byref(handle)); bind_fbo(0x8D40, handle)
    for i, t in enumerate(colors): attach_texture(0x8D40, 0x8CE0+i, 0x0DE1, t, 0)
    if depth: attach_texture(0x8D40, 0x8D00, 0x0DE1, depth, 0)
    draw_buffers(max(1, len(colors)), (uint * max(1, len(colors)))(*([0x8CE0+i for i in range(len(colors))] or [0])))
    if not colors: read_buffer(0)
    assert fbo_status(0x8D40) == 0x8CD5, 'Incomplete native framebuffer'
    return handle.value

def data_texture(data):
    height = max(1, (len(data)+1023)//1024)
    packed = (floating * (256*height*4))()
    for i, v in enumerate(data): packed[i] = v
    return texture(256, height, 0x8814, 0x1908, 0x1406, packed)

white = texture(1, 1, data=(C.c_ubyte * 4)(255, 255, 255, 255))
wet = texture(1, 1, internal=0x8229, fmt=0x1903, data=(C.c_ubyte * 1)(0 if CITY else 180))
albedo = texture(W, H)
normal = texture(W, H, 0x881A, 0x1908, 0x140B)
emission = texture(W, H, 0x881A, 0x1908, 0x140B)
depth = texture(W, H, 0x81A6, 0x1902, 0x1405)
gbuffer = fbo([albedo, normal, emission], depth)
shadow = texture(1536 if CITY else 128, 1536 if CITY else 128, 0x81A6, 0x1902, 0x1405)
shadow_fbo = fbo([], shadow)
gi = texture(W//2, H//2, 0x881A, 0x1908, 0x140B)
gi_fbo = fbo([gi])
display_texture=texture(W,H)
tex_param(0x0DE1,0x2801,0x2601);tex_param(0x0DE1,0x2800,0x2601)
display_fbo=fbo([display_texture])
bvh, triangles = data_texture(scene['nodes']), data_texture(scene['triangles'])

def bind(p, name, tex, unit):
    active_texture(0x84C0+unit); bind_texture(0x0DE1, tex); uniform(p, name, unit, 'i')

vao = uint(); gen_vao(1, C.byref(vao)); bind_vao(vao)
mesh = scene['mesh']; buffers = []
for i, key, size in [(0, 'positions', 3), (1, 'normals', 3), (2, 'uvs', 2), *([(3, 'colors', 3), (4, 'materials', 4)] if CITY else [])]:
    handle = uint(); gen_buffers(1, C.byref(handle)); buffers.append(handle)
    bind_buffer(0x8892, handle); values = (floating * len(mesh[key]))(*mesh[key])
    buffer_data(0x8892, C.sizeof(values), values, 0x88E4); enable_attribute(i); attribute(i, size, 0x1406, 0, 0, None)
indices = (uint * len(mesh['indices']))(*mesh['indices'])
index_buffer = uint(); gen_buffers(1, C.byref(index_buffer)); bind_buffer(0x8893, index_buffer); buffer_data(0x8893, C.sizeof(indices), indices, 0x88E4)
full_vao = uint(); gen_vao(1, C.byref(full_vao))

def draw_scene(p):
    bind_vao(vao)
    for model in scene['models']:
        uniform(p, 'uModel', model, 'm'); 
        if CITY and p==programs['shadow']:
            for start,count in scene['shadowRanges']:draw_elements(4,count,0x1405,C.c_void_p(start*4))
        else: draw_elements(4, len(mesh['indices']), 0x1405, None)

use(programs['shadow']); uniform(programs['shadow'], 'uVP', scene['lightVP'], 'm')
bind_fbo(0x8D40, shadow_fbo); viewport(0, 0, 1536 if CITY else 128, 1536 if CITY else 128); enable(0x0B71); clear(0x100); enable(0x8037);polygon_offset(1.5,2.5);draw_scene(programs['shadow']);disable(0x8037)
use(programs['geometry']); p = programs['geometry']; uniform(p, 'uVP', scene['vp'], 'm')
bind(p, 'uAlbedoTex', white, 0); bind(p, 'uWet', wet, 1)
uniform(p,'uVertexMaterials',1 if CITY else 0,'i');uniform(p,'uNightFactor',scene.get('nightFactor',0));uniform(p,'uWetOrigin',[0,0])
for k, v in {'uColor':([1,1,1] if CITY else [.4,.15,.08]), 'uEmissive':[0,0,0], 'uRoughness':.45, 'uMetal':.1, 'uWettable':1, 'uUVScale':1, 'uWetExtent':45}.items(): uniform(p, k, v)
bind_fbo(0x8D40, gbuffer); viewport(0, 0, W, H); clear_color(0,0,0,0); clear(0x4100); draw_scene(p); disable(0x0B71)

def common(p):
    use(p)
    for i, (name, tex) in enumerate([('uAlbedo',albedo), ('uNormal',normal), ('uEmission',emission), ('uDepth',depth), ('uShadow',shadow)]): bind(p,name,tex,i)
    for name, value in [('uInvVP',scene['inverseVP']), ('uVP',scene['vp']), ('uView',scene['view']), ('uLightVP',scene['lightVP'])]: uniform(p,name,value,'m')
    for name,value in [('uEye',scene['eye']), ('uSunDirection',scene['sun']), ('uSunColor',scene.get('sunColor',[1,.9,.7])), ('uSunIntensity',scene.get('intensity',5.5))]: uniform(p,name,value)

geometry_pixels = (C.c_ubyte * (W*H*4))()
bind_fbo(0x8D40, gbuffer)
read_buffer(0x8CE0)
read_pixels(0,0,W,H,0x1908,0x1401,geometry_pixels)
geometry_mask = [geometry_pixels[i+3]>0 for i in range(0,len(geometry_pixels),4)]
assert sum(geometry_mask)>100, 'Geometry pass did not write the expected material pixels'
bind_vao(full_vao); bind_fbo(0x8D40,gi_fbo); viewport(0,0,W//2,H//2); common(programs['gi']); draw_arrays(4,0,3)
def present():
    bind_fbo(0x8D40,0);p=programs['aa'];use(p);bind(p,'uSource',display_texture,0);uniform(p,'uResolution',[W,H]);draw_arrays(4,0,3)

results = []
for mode in ((0,) if CITY else (0, 1)):
    bind_fbo(0x8D40,display_fbo); viewport(0,0,W,H); p = programs['lighting']; common(p)
    bind(p,'uGI',gi,5); bind(p,'uBVH',bvh,6); bind(p,'uTriangles',triangles,7)
    for name,value in [('uDataWidth',256),('uNodeCount',scene['nodeCount']),('uShadowMode',mode),('uEnableGI',0 if CITY else 1),('uEnableFog',1),('uEnableSSR',1),('uDebug',0)]: uniform(p,name,value,'i')
    for name,value in [('uRain',0 if CITY else .5),('uExposure',1.05 if CITY else 1.1),('uFogDensity',.003 if CITY else .005)]: uniform(p,name,value)
    lights=scene.get('lights',[])[:12]
    uniform(p,'uPointCount',len(lights),'i')
    if lights:
        lp=(floating*48)();lc=(floating*48)()
        for li,light in enumerate(lights):
            for j,v in enumerate([light['x'],light['y'],light['z'],light['radius']]):lp[li*4+j]=v
            for j,v in enumerate(light['color']+[light['power']]):lc[li*4+j]=v
        uniform_4v(uniform_location(p,b'uLightPositions[0]'),12,lp);uniform_4v(uniform_location(p,b'uLightColors[0]'),12,lc)
    draw_arrays(4,0,3)
    present()
    pixels = (C.c_ubyte * (W*H*4))(); read_pixels(0,0,W,H,0x1908,0x1401,pixels)
    values = [pixels[i]+pixels[i+1]+pixels[i+2] for i in range(0,len(pixels),4)]
    error = get_error(); assert error == 0, f'Native GL error: {error:#x}'
    assert max(values)-min(values)>30 and sum(values)>10000, 'Render did not produce a varied, lit image'
    if CITY:
        rgba=bytes(pixels);raw=bytearray()
        for row in range(H-1,-1,-1):
            raw.append(0)
            for col in range(W):raw.extend(rgba[(row*W+col)*4:(row*W+col)*4+3])
        def png_chunk(kind,data):return struct.pack('!I',len(data))+kind+data+struct.pack('!I',zlib.crc32(kind+data)&0xffffffff)
        png=b'\x89PNG\r\n\x1a\n'+png_chunk(b'IHDR',struct.pack('!2I5B',W,H,8,2,0,0,0))+png_chunk(b'IDAT',zlib.compress(bytes(raw),8))+png_chunk(b'IEND',b'')
        output=Path(sys.argv[sys.argv.index('--output')+1]) if '--output' in sys.argv else ROOT/'docs'/'Native_City_Preview.png'
        output.parent.mkdir(parents=True,exist_ok=True);output.write_bytes(png)
    mean_channel = sum(values)/(W*H*3)
    bind_fbo(0x8D40,display_fbo);use(p);bind(p,'uAlbedo',albedo,0);uniform(p,'uDebug',3,'i')
    draw_arrays(4,0,3)
    present()
    read_pixels(0,0,W,H,0x1908,0x1401,pixels)
    dark = sum(geometry_mask[i] and pixels[i*4]<5 for i in range(W*H))
    lit = sum(geometry_mask[i] and pixels[i*4]>100 for i in range(W*H))
    if scene.get('intensity',5.5)>0:assert dark>0 and lit>0, 'Sun-visibility buffer must contain occluded and visible geometry'
    assert get_error()==0
    results.append({'shadow_mode':mode,'rgb_sum_range':[min(values),max(values)],'mean_channel':mean_channel,'shadowed_geometry_pixels':dark,'lit_geometry_pixels':lit,'gl_error':error})

instance_result = None
if not CITY:
    p=programs['geometry'];use(p);bind_fbo(0x8D40,gbuffer);bind_vao(vao);viewport(0,0,W,H);enable(0x0B71);clear_color(0,0,0,0);clear(0x4100)
    bind(p,'uAlbedoTex',white,0);bind(p,'uWet',wet,1);uniform(p,'uColor',[1,1,1]);uniform(p,'uVertexMaterials',1,'i');uniform(p,'uInstanced',1,'i');uniform(p,'uVP',scene['vp'],'m')
    values=[]
    for x,color in [(-1.25,[1,.1,.05,1]),(1.25,[.05,1,.1,1])]:
        values += [.65,0,0,0,0,.65,0,0,0,0,.65,0,x,0,0,1]+color+[.7,0,0,0]
    instance_buffer=uint();gen_buffers(1,C.byref(instance_buffer));bind_buffer(0x8892,instance_buffer);values=(floating*len(values))(*values);buffer_data(0x8892,C.sizeof(values),values,0x88E8)
    for location,offset in [(5,0),(6,16),(7,32),(8,48),(9,64),(10,80)]:
        enable_attribute(location);attribute(location,4,0x1406,0,96,C.c_void_p(offset));attribute_divisor(location,1)
    draw_instanced(4,len(mesh['indices']),0x1405,None,2)
    colors=(C.c_ubyte*(W*H*4))();read_buffer(0x8CE0);read_pixels(0,0,W,H,0x1908,0x1401,colors)
    red=sum(colors[i]>colors[i+1]*2 and colors[i]>30 for i in range(0,len(colors),4));green=sum(colors[i+1]>colors[i]*2 and colors[i+1]>30 for i in range(0,len(colors),4))
    assert red>10 and green>10,'Independent instance matrices/materials did not reach the G-buffer'
    assert get_error()==0
    instance_result={'instances':2,'red_pixels':red,'green_pixels':green,'gl_error':0}
print(json.dumps({'driver':get_string(0x1F02).decode(),'renderer':get_string(0x1F01).decode(),'programs_linked':len(programs),'framebuffers_complete':4,'renders':results,'instanced_render':instance_result},indent=2))
make_current(display,None,None,None)
api(egl,'eglDestroyContext',uint,voidp,voidp)(display,context)
api(egl,'eglDestroySurface',uint,voidp,voidp)(display,surface)
api(egl,'eglTerminate',uint,voidp)(display)
```

## Included texture assets

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| assets/asphalt-128.png | 17182 | 31e274df5ab929beba34d6273d42ed434a05b8fc3e1a4f0211e58a83ca800ec2 |
| assets/asphalt-32.png | 1455 | b2b69f466b50495e8b5ff5dee0ca768003dd3c56db387918c77f663a9555c389 |
| assets/asphalt-512.png | 255616 | 5568860dfd11303dd6990fe3257b6b000cbc61e6a93b80c4489fda1bd5c53e16 |
