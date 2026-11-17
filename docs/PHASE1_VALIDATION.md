# Phase 1 validation record

Executed November 3, 2026 against the supplied source.

## Numerical and integration checks

**38 tests passed; 0 failed; 0 skipped.** Command: `node --test tests/*.test.js`.

The checks cover:

- Vector, quaternion, matrix-inverse and projection behavior.
- Quaternion constant-angular-velocity integration and approximate torque-free rotational-energy conservation.
- Parallel/boundary ray–AABB slabs and frustum rejection.
- Fixed-step time accounting, bounded catch-up and interpolation.
- Generation-checked pooling, stale handles, duplicate releases and arena alignment/overflow.
- Event unsubscription.
- Ray–triangle intersections, SAH BVH queries against seeded brute-force results, closest hits, dynamic refit, GPU layout and escape links.
- Refittable triangle-mesh collision queries.
- OBB face manifolds and separation; a sphere inside an OBB.
- A falling sphere at rest and a six-second three-box stack.
- Equal-mass elastic-impact linear momentum and off-center impulse torque.
- Nearest world raycasts and chassis exclusion.
- Four-second articulated-ragdoll anchor error and cleanup.
- Torque interpolation, four-wheel suspension support and forward acceleration.
- XPBD pinning/stretch constraints, normals, raycast updates, sphere/floor particle collision and wetness bounds.
- LOD hysteresis and day/night normalization.
- Mesh winding against outward normals.
- Texture-budget accounting, pinned-resource protection, LRU eviction, queued cancellation, cancelled late completion, loader errors and shared-lease idempotency.
- GPU occlusion result availability, moving-camera invalidation and obsolete static-scene/camera epochs.
- The actual laboratory simulated for five seconds, including rigid/deformable coupling that supports the dropped sphere.
- Maximum laboratory content construction: 75 rigid bodies, 96 render objects, 17,612 shadow triangles, 10,889 BVH nodes; below the 16,384-node software-ray limit.

The maximum-content check validates construction, finite transforms, packing and capacity. It is not a long-duration simulation or a performance benchmark of that content.

## Shader and native render checks

Command: `python3 tools/validate_gpu.py`.

Environment reported:

```text
OpenGL ES 3.2 Mesa 25.2.8-0ubuntu0.24.04.2
llvmpipe (LLVM 20.1.2, 256 bits)
```

**Six programs compiled and linked:** geometry, depth shadow, conservative-query proxy, SSGI, final lighting and rain. **Three framebuffer configurations were complete:** HDR G-buffer, depth shadow, and half-resolution GI.

The native harness executes the actual supplied shaders against a two-object scene at 64×48 pixels. It reads material pixels, checks a varied lit final image, then checks the visibility-buffer result for both lit and occluded geometry. It does not run a separate replacement renderer shader.

| Shadow mode | Material geometry pixels in shadow | Material geometry pixels lit | Mean final color channel | GL error |
| --- | ---: | ---: | ---: | ---: |
| Raster depth map | 66 | 1,646 | 149.9749 / 255 | 0 |
| Triangle-BVH software rays | 101 | 1,611 | 155.3895 / 255 | 0 |

Different masks are expected: the raster path has depth-map discretization, PCF and different biasing, while the software path tests hard triangle visibility. These numbers demonstrate executed paths and nontrivial visibility; they are not image-quality targets.

The native harness compiles the rain/query programs but does not execute their draw behavior. Browser input, streaming decode/upload, occlusion-query scheduling, context restoration and the complete interactive browser loop still require an enabled browser test.

## Browser verification limit

The provided cloud Chrome browser could not create a WebGL 2 context. The page loaded and displayed the explicit capability diagnostic:

> WebGL 2 is required. Enable browser hardware acceleration.

No browser graphics settings or network permissions were changed. Interactive 3D behavior and browser-specific GPU semantics are **unverified in that environment**. Native GLES testing is not represented as equivalent to browser WebGL conformance. No real-device FPS or AAA visual-fidelity claim is made.

## Reproduction and acceptance

Run `npm test` and `npm run check`. On a machine with a compatible browser, run `npm run dev`, open the local URL, and verify both shadow modes, all buffer inspection options, sunlight movement, rain/saturation/reflections, vehicle forces, sphere launches, ragdoll drops, resize and cleanup. For Linux systems with EGL/Mesa, the optional native shader test provides an additional automated check.

For production acceptance, define target hardware and a representative content/performance budget, then add device/browser testing, long-duration stress, continuous collision handling, robust content ingestion, resource-pressure recovery and image-quality comparisons. Those acceptance gates have not been completed by this reference implementation.
