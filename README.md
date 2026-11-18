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

## Phewa Valley assets (Tripo + ElevenLabs)

The valley is fully playable with its procedural art (pagoda, stupa, prayer flags, Himalaya) and browser speech. Generated assets are optional upgrades:

1. Copy `.env.example` to `.env` and add `TRIPO_API_KEY` and `ELEVENLABS_API_KEY`. For a Nepali accent, set the `ELEVENLABS_VOICE_*` ids from your ElevenLabs Voice Library.
2. Preview without spending credits: `npm run assets:models -- --dry-run` and `npm run assets:audio -- --dry-run`.
3. Generate: `npm run assets:models` (7 landmark models) and `npm run assets:audio` (about 390 voice lines, 6 ambience loops, 3 effects, 1 music track).
4. Reload the game. Generated models replace the procedural landmarks. Turn on **Spoken conversations** in the menu.

Both scripts skip anything that already exists, so re-running only fills gaps. `--only pagoda,stupa` or `--only ambience,voices:mira` limits the run, and `--force` regenerates. Prompts, placements and voice casting live in `src/assets/catalog.js`. Output goes to `assets/generated/` with a `manifest.json`. Keys are read only by these Node scripts, and the dev server refuses to serve dotfiles.

## Unity version

A native Unity 6 build lives in `unity/PhewaValley` (built-in render pipeline, glTFast for models). It reads the same places, people, dialogue and missions as the web game.

1. Run `npm run unity:data` after changing game content or generating ElevenLabs audio. It exports `world.json` and copies `assets/generated` into Unity's StreamingAssets.
2. Open `unity/PhewaValley` in Unity Hub (6000.6.6f1) and open **Assets/PhewaValley/Scenes/Main.unity**, then press **Play**. The scene is empty on purpose: the game builds itself at runtime.
3. For Tripo models downloaded by hand, follow `docs/TRIPO_PROMPTS.md`. Save each `.glb` in `Assets/StreamingAssets/PhewaValley/models/` using the file name given there.
4. To ship a Windows build, use **Phewa Valley ▸ Build for Windows**. The output goes to `unity/PhewaValley/Builds/Windows/PhewaValley.exe`.
5. If you change any Devanagari text in the C# scripts, run `npm run unity:devanagari`. It uses Windows' text shaping to pre-render the phrases, because Unity cannot shape Devanagari itself.

**Unity controls:**
- **On foot:** mouse looks around; WASD walk; Shift run; C stroll; Space jump, or climb onto a ledge 0.45–2.2 m high; **Ctrl** dive-roll while moving; E interact; **G** emotes (namaste, wave, bow, nod, clap…); **V** cycles the camera (over-the-shoulder, medium, far, first-person); **Tab** switches between Kiran, Dawa and Sunita; M map; J journal; P postcard; **F1** hides the HUD; Esc menu.
- **Menu ▸ Guidance ▸ Explorer:** no radar or distances. A red kite flies over the current objective instead.
- **In a vehicle:** W/S drive; A/D steer; Space handbrake; H horn; **Q** cycles radio stations; **T** starts or ends a passenger shift; E gets out.

**Characters:**
- Rigged Tripo exports work with either Mixamo or Unreal-mannequin bone names. Their bones are driven by the procedural walk, run, idle and talk animator.
- Unrigged T-pose models are given a 13-bone skeleton automatically.
- Models that include their own animation clips play those clips instead. Kiran (`player.glb`) is a Tripo Animation export with 26 clips:
  - Walk and run blend by speed, with their root motion cancelled. The run's loop seam (a 19.5° arm pop) is smoothed.
  - Idle uses `standing_relax`, with an occasional `look_around`.
  - In conversation he turns to face the person, does a real namaste (procedural arm IK: palms together, small bow), looks them in the eye and gestures now and then.
  - Space plays a hop from the `jump` clip, timed to the jump.
  - His feet are planted on the real ground each frame.
  - Tripo's clip named "Nepali namaste" is actually a wave, so emotes are labelled by what each clip shows.
  - All 26 clips are used:
    - **Movement:** steps and slopes switch to `run_upstairs`; `climb` is the ledge vault; `dive` is the dive-roll.
    - **Traffic and bumps:** a vehicle that hits Kiran knocks him down (`fall`), and he gets up (the end of `dive`) and complains at the driver (`complain_02`). Near misses play `hit_to_side`, running into people plays `hit_to_body_02`, and long falls land hard.
    - **Pastimes:** the lattu throw uses `chop`; chungi kicks use `front_kick_01/02`.
    - **Greetings:** walking past a named person plays `greet_01`.
    - **Emotes:** G covers the rest (cartwheel `flip`, shadow-box `box_01/02/03`, collapse, and more).
  - On foot he has momentum: it takes time to speed up and stop, and turns are wider at a run.
- Solid landmarks collide with their real mesh shape, so plinths, steps and platforms can be climbed.
- At night a soft fill light from the camera side keeps characters readable. Street lamps cast stronger pools, and the night grade is clearer.
- To inspect a rigged model's clips: `PhewaValley.exe -phewa-clips <file.glb> <folder>`. It writes a filmstrip per clip and a report (root drift, loop seams, foot contact, bone pops).

**Lok Katha:** stone tablets under tall prayer-flag poles at the valley's heritage sites tell eight Nepali folk tales, collected in the journal.

**Driving:**
- Four wheel raycasts keep the Maruti on the real road, tilted to slopes and kerbs.
- Spring suspension: the nose dives under braking, squats under power, and the body rolls in corners.
- The tyres carry momentum and can slide. The handbrake (Space) swings the rear into a drift, and monsoon rain cuts grip.
- Steering is tight at low speed and calm at high speed. The engine has a torque curve with gear-change dips, hills slow you going up, and impacts bounce the car back and jolt the camera.

**Camera:**
- **V** includes a *Kurosawa* mode: a long telephoto lens from about 20 m, a low camera, the figure on a third, and slow rail-like pans.
- Conversations cut to a side-on telephoto two-shot.
- Camera changes use a hard-edged wipe.

**Graphics:**
- Menu ▸ Film look: *2000s Kodak* (default), *Natural* or *Kurosawa B&W*.
- The colour grade follows the time of day: violet dawn, orange-and-teal day, amber golden hour, cobalt blue hour, moonlit night. Monsoon rain and load-shedding candlelight have their own grades.
- Linear colour, ACES tone mapping, bloom, colour grading and grain, using the Post Processing Stack v2.
- Procedural brick, grime and asphalt detail in the shader, and wet roads in the monsoon.
- Menu ▸ Graphics offers Performance, Balanced and Cinematic. Cinematic adds ambient occlusion and MSAA.

## Unity gameplay repair pass

Kiran movement, climbing, relocation, vehicle exits, menu pausing, tea-shop input, save recovery and weather have been repaired. Rendering has cleaner film effects and corrected texture processing. See [repair details and validation](docs/GAMEPLAY_REPAIRS.md) for the 93 Unity checks, preview instructions and graphics-build limitation. The updated preview also includes smoother controls, surface footsteps, layered mountain ridges and optional first-visit guidance (F2). Rain remains disabled.

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
