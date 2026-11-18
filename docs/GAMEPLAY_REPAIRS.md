# Gameplay and environment repairs — November 17, 2026

This pass focuses on the Unity Phewa Valley game, where Kiran and his animated controller live. Existing web-game changes were preserved.

## Character repair pass - November 18, 2026

- Animation sampling now uses an explicit clock and a clean base pose on every frame. Pausing cannot repeatedly subtract root travel or accumulate neck/arm corrections. Kiran's actual dive and namaste poses pass repeated zero-time samples.
- Action clips play to their real end instead of stopping 0.25 seconds early. Root-motion flags remain active through outgoing fades, and climbs cancel horizontal motion and vertical lift together.
- Per-clip starting offsets are measured relative to standing. Kiran's measured walk/run hip displacement from the controller dropped from 0.986 m to 0.122 m in the regression sequence. Idle soles sit approximately 9 mm below the floor, within the foot-contact tolerance.
- Foot grounding samples near the physical feet, rejects steep support surfaces and avoids lifting the model onto overhead geometry. Skinned bounds are measured through the complete transform hierarchy; Kiran's skinning updates offscreen so actions do not freeze outside the camera.
- The capsule follows protagonist height. Small descending kerbs retain contact, wall collisions discard blocked momentum, and dialogue/work freeze horizontal travel. First-person visibility also applies after swapping a body.
- Knockdowns retain a working capsule, ignore duplicate impacts, wait for the get-up sequence and grounding, and provide a short recovery interval against repeat hits.

**93 Unity checks pass**, including synthetic animation tests and direct checks of the installed Kiran GLB. The web suite remains at 77 passing tests. Test execution is isolated from the player's saved visit. The preview and repository source contain the same gameplay code; Unity build/cache output and machine-specific logs remain local.

## Controls, landscape and opening-story polish

- Tighter acceleration and stopping, frame-rate-independent turning, slightly more air control and support for either Shift key. Jump travel uses averaged gravity velocity: measured full jumps are 0.900-0.901 m at 30/60/120 FPS. Releasing Space early shortens a jump.
- Distance-driven footsteps distinguish road, paving and grassy terrain, with a separate landing sound. Foley is synthesized locally and follows the ambience audio setting; it does not require paid assets.
- Three continuous mountain belts replace the repeated individual cones: irregular ridgelines, foothills, spurs, altitude-dependent rock/snow and muted distant colours. The 19,200-vertex backdrop remains outside the playable area and uses the existing shader, so it works in the preview.
- The opening story is now **First steps in the valley**, with clearer instructions and an optional eight-stage guidance card that teaches movement, talking, navigation and driving/parking. F2 or the menu toggles hints. Progress survives reloads; players who already finished the original opening story skip the tutorial. Existing story steps and rewards are unchanged for save compatibility.
- The preview preparation script now copies current authored world content. `-phewa-fresh` creates a temporary new visit only with `-phewa-smoke`; it does not erase the player's save.

Current validation: **93 Unity checks and 77 web tests pass**. New checks cover jump consistency, the opening-to-parking sequence, tutorial reloads and veteran saves, footstep signal limits/surface selection, and backdrop bounds/seams. [Updated scene](verification/polish-day.png), [opening HUD](verification/intro-ui.png), and [Kiran jumping against the new ridges](verification/polish-jump.png) were rendered and visually inspected. The Windows UI helper reported `foreground window did not report a process id` while activating the review window; the game's own normal-window screenshot provided the HUD check. Keyboard toggles were not independently exercised through that helper.

## Follow-up: rain removed

Rain particles, storm transitions, wet-road effects and rain-driven audio/colour grading are disabled in the Unity game at the user's request. Old saved rainfall and wetness no longer reactivate them. Day/night lighting and normal atmospheric haze remain. The playable preview has been updated.

## Original repair changes

- **Kiran:** process gravity before movement, detect landing after movement, cancel ascent on ceiling contact, add 100 ms of jump grace after leaving an edge and 120 ms of jump buffering. Climbing no longer falls through into normal movement with a disabled capsule. Validate clearance above both ends of a climb and recheck its destination. Use actual movement distance for locomotion animation when pushing into walls.
- **Relocation:** one teleport path clears fall velocity, dive/knockdown momentum, jump requests, animation actions and foot offsets. Car exits, bus travel, protagonist changes, swing cancellation and fall recovery use it. The swing can suspend the collider without the controller continuing to call Move.
- **Vehicles and camera:** check both doors for supported, unobstructed exits; refuse exits onto nearby roofs or drops. Refuse boarding a moving car. Recovery resets drivetrain and suspension state and stays within the valley. Wet-road grip follows retained surface wetness. Camera obstruction casts examine multiple hits and ignore the followed body; changing camera targets restores visibility of the previous body.
- **Interaction:** menu/map/journal/protagonist wheel pause simulation time; tea-shop controls release the cursor and suppress walking while the work panel is visible. Guard invalid saved tea-shop step indices. Prevent switching protagonists during physical actions or work.
- **Saves and weather:** sanitize protagonist/quality indices, nonfinite cash/time/weather and missing collections. Save rainfall, target rainfall, transition timer and wetness. Rain emits downward, drifts with wind and collides with roofs/ground. Weather probability, duration limits and wind strength are exposed on DayNight.
- **Graphics:** softer grain, bloom, fringing and color grading; darker night ambient/moon lighting with local character fill. Fix a character-texture conversion bug that restored a temporary render target after releasing it. Add a reflective world-space lake ripple shader, registered for inclusion in future Unity builds. Performance settings still apply if post-processing resources are missing.

## Verification

- **93 Unity player checks passed.** Real CharacterController/PhysX runs check jumps and landings at 30/60/120 FPS, ceilings, climbing, wall blocking, fall/dive relocation, both car-door obstruction cases, recovery, disabled rain/wet-road effects and save sanitization. All seven mission arcs complete using authored objectives; duplicate events cannot pay again; replay policies and tea-shop expiration pass.
- **77 web-game tests passed**, using `node --test --test-isolation=none tests/*.test.js` because the restricted process environment prevents the default test runner from spawning isolated subprocesses.
- **51 JavaScript modules passed syntax checks.** Runtime C# and editor helper C# compile successfully against the local Unity assemblies. Existing warnings concern unused fields and JSON-populated fields.
- Day and night scenes rendered through Direct3D 11 with all **21 installed models loaded**. Screenshots: [day](verification/valley-day.png), [night](verification/valley-night.png). These captures render the game camera without the HUD.

The full editor asset build is blocked by **“No valid Unity Editor license found.”** Therefore the new Lake shader has **not** been imported or visually verified. The isolated preview uses the existing shipped asset bundle and Standard water fallback. Shader verification is explicitly skipped during existing-player checks; a normal fresh-build verification requires it. This is a focused repair pass, not a claim that every possible game state has been exhaustively tested.

## Run

The updated gameplay preview is `unity/PhewaValley/Builds/Validation/PhewaValley.exe`. It uses the same save location as the game when played normally. Automated checks and smoke captures do not write the visit save.

Recreate the preview and run checks using the installed Mono Windows player and Unity C# compiler:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify-unity-player.ps1 -RunChecks
```

The script copies the existing player into Builds/Validation and compiles current gameplay there. It does not import new assets or replace Builds/Windows. The text results appear in `docs/verification/controller-runtime.log` and `Builds/Validation/controller-checks.txt`.

After activating the editor license, use **Phewa Valley → Build for Windows** to include the lake shader. A repeatable separate development build is also available:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Unity.exe' -batchmode -projectPath "$PWD/unity/PhewaValley" -executeMethod PhewaValley.EditorTools.PhewaSetup.BuildValidation -logFile "$PWD/docs/verification/unity-build.log"
```

Run that fresh player with `-phewa-verify` to check the complete build including shader availability. Graphics support must be enabled for that shader check; use `-phewa-skip-shader-check` only for explicit gameplay-only validation. Smoke captures support `-phewa-render-camera 1` for hidden-window rendering.
