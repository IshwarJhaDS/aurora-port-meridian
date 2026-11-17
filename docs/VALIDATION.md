# Phase 2 validation record

Executed November 4, 2026 against Aurora: Port Meridian 0.2.0. The previous chapter’s record is retained as `PHASE1_VALIDATION.md`; its counts describe that earlier build only.

## Automated model and engine checks

**71 tests passed; 0 failed; 0 skipped.** `npm test` runs the numerical engine tests and the actual city simulation in Node. `npm run check` syntax-checks **38 JavaScript modules** across source, tests, and tools.

The checks include the Phase 1 vector/matrix/quaternion, BVH, rigid-body/contact, suspension/tire, ragdoll, deformable, pool/arena, texture cache, LOD, occlusion, and lab integration tests. City-specific checks cover:

- Deterministic generation for negative/positive chunk coordinates, valid indexed geometry/material attributes, terrain seam behavior, and accessibility of all named mission locations.
- Chunk commit/cancellation, eviction after district travel, resident capacity and cleanup.
- Connected, reciprocal sidewalk routes to every named destination; Bézier arc-length progression and closed opposing traffic lanes.
- Signal conflict exclusion and autonomous braking toward an obstacle.
- Bounded weather, retained weather RNG position, moving wetness overlap, and Voronoi fragment area conservation.
- Save schema bounds, malformed input handling, courier variant retention, reload reward idempotence, and new-visit reset in an isolated in-memory fixture.
- All seven mission arcs, ordered work, café timeout/retry, distinct neighborhood contacts, exact lamp IDs, reputation unlock, and lookout photo progression.
- Topic dialogue and familiarity, released conversation state, citizen schedules/panic and finite instance transforms.
- Character wall collision, jump/landing, and shared terrain raycasts.
- Integrated intro progression, proximity interactions, cross-district streaming, saved reload, cleanup, timed/proximity-guarded café actions, and shuttle cost/stop/arrival checks.
- Instance buffer stride, matrix/material offsets, divisors, upload-version reuse, growth/update and disposal.

These are implementation checks, not a certification of full-world realism, long-duration stability, or complete scenario coverage. The isolated reset test does not erase browser progress.

## Native shader and rendering checks

`python3 tools/validate_gpu.py` creates a surfaceless ES 3 context on system Mesa:

```text
OpenGL ES 3.2 Mesa 25.2.8-0ubuntu0.24.04.2
llvmpipe (LLVM 20.1.2, 256 bits)
```

**Seven actual shader programs linked:** geometry, depth shadow, conservative-query proxy, screen-space GI, deferred lighting, rain, and edge-aware antialiasing. **Four framebuffer configurations were complete:** material G-buffer, shadow depth, half-resolution GI, and display target.

The 64×48 engine scene executed both raster depth shadows and triangle-BVH software rays with zero GL errors, varied final colors, and nonempty visible/occluded material masks. The instance check separately drew two differently transformed/materialized instances into the actual G-buffer: 75 red pixels and 106 green pixels; zero GL errors.

| Diagnostic shadow mode | Occluded geometry pixels | Lit geometry pixels | Mean final channel / 255 | GL error |
| --- | ---: | ---: | ---: | ---: |
| Raster | 34 | 1,646 | 155.0184 | 0 |
| Software triangle rays | 60 | 1,611 | 154.8832 | 0 |

`tools/export-city-scene.mjs` additionally constructs the actual city simulation, drains initial chunk generation, and exports its real meshes/materials/instances and lights. The native harness renders those data using the shipped shaders at 960×540. It flattens moving instances for the city image; the independent instance draw above verifies the instanced GPU path itself.

| Actual city render | RGB-sum range | Mean final channel / 255 | GL error |
| --- | --- | ---: | ---: |
| Daylight | 103–765 | 128.3759 | 0 |
| Night | 40–705 | 39.1061 | 0 |

`gpu-daylight.json` and `gpu-night.json` contain the reported results. `Port_Meridian_Daylight.png` and `Port_Meridian_Night.png` are actual native ES renders, not concept art and not browser screenshots. Night rendering shows scheduled warm windows, street lighting, and vehicle lamps. A zero lit-sun mask at night is expected because direct sunlight is off.

The harness compiles rain/query programs but does not execute the browser’s rain overlay, query scheduling, texture decode/fetch, or complete renderer lifecycle. The city export uses a fixed aerial camera for visual inspection. Native ES is not browser WebGL conformance and llvmpipe does not establish hardware GPU frame rates.

## Browser UI and gameplay verification

The managed Chrome test browser could not create WebGL 2. The application correctly entered its explicitly labeled **MAP VIEW**, retaining the same simulation and UI. No browser graphics/security settings were altered.

The UI playtest verified welcome/resume, visible city/map/minimap, mission journal, settings/menu, named-person interaction, typed job questions, map destination selection, shuttle travel/deductions, and saved mid-shift reload. It completed the full nine-action café shift through the real controls and observed **$230 → $315**, **0 → 2 reputation**. A return shuttle deducted $10. Speaking to Mira then completed the intro and observed **$305 → $425**, **2 → 6 reputation**. The journal showed the intro completed, café work repeatable, and the missing-manifest story available at the earned reputation.

Browser movement key-hold tests were limited by the cloud browser’s slow simulation cadence; the controller’s normal fixed-step collision/jump behavior is covered by the model tests. Full 3D browser gameplay, target-device performance, context restoration, touch controls, browser photo downloads, audio voices, and long-session resource pressure remain unverified. Other mission arcs were completed in the automated shared-model tests, not claimed as full browser playthroughs.

## Reproduce

```bash
npm test
npm run check
python3 tools/validate_gpu.py
python3 tools/validate_gpu.py --city --output docs/Port_Meridian_Daylight.png
python3 tools/validate_gpu.py --city --night --output docs/Port_Meridian_Night.png
npm run dev
```

The native checks require an existing system EGL/Mesa installation; there are no downloads or package installs in the game. For an actual 3D acceptance pass, open the printed local URL on a WebGL 2-capable browser and check walking/driving, weather, shadows, streamed travel, character interactions, every mission, save/resume, photos, resize, and device performance. The current build is a playable stylized prototype with explicit bounds, not production-certified AAA technology.
