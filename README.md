# Aurora: Port Meridian — Phase 2

A playable, single-player city built on the Aurora WebGL 2 engine. Walk or drive through six districts, meet residents, work café and courier shifts, follow seven mission arcs, and watch the city change with the weather and time of day.

The art is procedural and stylized. This is a working city prototype with finite missions and authored dialogue; it is not a photorealistic AAA game or an unrestricted real-world simulation.

## Start playing

Extract the ZIP. Open a terminal in the **aurora-city** folder and run:

```bash
npm run dev
```

Open **http://127.0.0.1:3001/** and select **Enter the city**. Start by pressing **E** beside **Mira** in Founders Square. Follow “A place to begin.”

Requires Node.js 20 or newer. There are no npm dependencies to install. Keep the terminal running while playing; Ctrl+C stops the server. Port 3001 lets this chapter run alongside the older Phase 1 server on port 3000.

If Python 3 is already available, this also works from the same folder:

```bash
python3 -m http.server 3001 --bind 127.0.0.1
```

Use the HTTP address above. Opening `index.html` directly does not support the local module and texture requests.

## Controls

| Input | On foot | In the Aster GT |
| --- | --- | --- |
| W A S D / arrows | Move relative to the camera | Throttle, reverse, steer |
| Shift | Sprint | — |
| C | Slow walk | — |
| Space | Jump | Handbrake |
| B | — | Service brake |
| E | Talk, inspect, enter car; hold for repairs | Exit when slow and clear |
| Drag the scene | Turn camera | Turn camera |
| Mouse wheel | Camera distance | Camera distance |
| M | Map, destinations, shuttle | Map |
| J | Mission journal | Mission journal |
| V | Photo framing; capture button saves a PNG | Photo framing |
| R | Bring your car to a nearby road | Recover car |
| X | Break a nearby bin, crate, or the square’s glass pane | — |
| Escape | Close conversation/panel or pause | Pause |
| F3 | Runtime counters | Runtime counters |

On-screen movement buttons are available on touch layouts. Keyboard and mouse receive the most complete control coverage.

## Things to do

- Talk to eight named contacts and nearby residents. Choose replies or type questions about work, directions, weather, and the harbor.
- Work a three-order café shift, take repeatable delivery routes, or restore three streetlights.
- Follow the neighborhood stories and missing-shipment investigation; earn money and reputation.
- Visit Westhaven, East Harbor, Northline, Cedar Hills, Old Meridian, and the waterfront.
- Drive the four-wheel suspension vehicle. Rain lowers tire grip; pedestrians react to fast cars and broken props.
- Use the map to select a place. Near a named stop, the city shuttle costs **$10 of game money** and advances the clock eight minutes.

Businesses use street-side activity points. Buildings are exterior scenery; they do not have enterable interiors. Conversations are authored topic responses, not an online AI service. Optional spoken dialogue uses the browser’s speech feature.

Progress saves on this browser and address, including missions, cash, reputation, conversation familiarity, repairs, damaged props, position, time, and weather state. Use the same host and port to resume. If browser storage is blocked, the HUD shows **SESSION ONLY**. **Start a new visit** in the menu clears this chapter’s saved visit after its confirmation.

## Graphics

The 3D view needs WebGL 2, `EXT_color_buffer_float`, and at least 11 fragment texture units. If these capabilities are unavailable, the game opens a clearly labeled **MAP VIEW** using the same simulation, conversations, work, and missions. Shader/runtime errors are shown as errors rather than silently changing renderers.

Menu presets: Performance uses 60% render scale; Balanced uses 80% with wet reflections; Cinematic uses full scale with screen-space GI. The city uses raster sun shadow maps. The original small physics/rendering laboratory is preserved at **http://127.0.0.1:3001/lab.html**.

## Source and verification

```bash
npm test
npm run check
```

The delivered build passes 71 automated tests and syntax checks for 38 JavaScript modules. Native ES graphics validation links all seven shaders and renders the actual city in daylight and at night. Browser UI testing exercised the map fallback because the test browser has no WebGL 2; complete 3D browser playback and hardware frame rates remain unverified. See [validation](docs/VALIDATION.md).

| Document | Contents |
| --- | --- |
| [Playing guide](docs/PLAYER_GUIDE.md) | First steps, people, jobs, mission rewards, troubleshooting |
| [Phase 2 architecture](docs/PHASE2_ARCHITECTURE.md) | State machines, spatial algorithms, data, equations, limits |
| [Complete source](docs/FULL_SOURCE.md) | Every source file, shader, test, and validation tool |
| [Phase 1 architecture](docs/PHASE1_ARCHITECTURE.md) | Retained mathematical and engine foundation |

Runtime code and assets are included locally. No CDN, API key, account, package installation, or paid service is required.
