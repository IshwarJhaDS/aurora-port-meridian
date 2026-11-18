// Turns loaded Tripo models into static render objects and colliders using the
// placements in the asset catalog. Meshes are shared between placements.
import { Vec3, Quat, Mat4, AABB } from '../math.js';
import { GeometryBuilder, renderObject } from '../world/geometry.js';
import { terrainHeight } from '../world/util.js';
import { MODELS } from '../assets/catalog.js';

const WATER_LEVEL = -1.2;

export function placeGeneratedProps(library) {
  const objects = [], colliders = [];
  for (const def of MODELS) {
    const loaded = library?.models?.get(def.id); if (!loaded) continue;
    const { mesh, image } = loaded, size = mesh.bounds.extent(), scale = def.height / Math.max(.001, size.y);
    // A cheap box stands in for the model in the shadow-trace scene.
    const c = mesh.bounds.center(), proxy = new GeometryBuilder().box(c.x, c.y, c.z, size.x / 2, size.y / 2, size.z / 2, [1, 1, 1]).build();
    for (const [i, place] of def.placements.entries()) {
      const y = def.water ? WATER_LEVEL : place.y ?? terrainHeight(place.x, place.z), position = new Vec3(place.x, y, place.z);
      const model = Mat4.compose(position, Quat.axisAngle(new Vec3(0, 1, 0), place.yaw || 0), new Vec3(scale, scale, scale));
      const bounds = new AABB();
      for (const sx of [0, 1]) for (const sy of [0, 1]) for (const sz of [0, 1]) bounds.include(Mat4.point(model, new Vec3(sx ? mesh.bounds.max.x : mesh.bounds.min.x, sy ? mesh.bounds.max.y : mesh.bounds.min.y, sz ? mesh.bounds.max.z : mesh.bounds.min.z)));
      const object = renderObject(mesh, { castShadow: !def.water });
      object.model = model; object.bounds = bounds; object.shadowMesh = proxy; object.material.image = image || null; object.generated = def.id;
      objects.push(object);
      if (def.solid) {
        const half = Math.max(size.x, size.z) * scale * .36;
        colliders.push({ id: `generated:${def.id}:${i}`, x: place.x, y: y + def.height * .25, z: place.z, half: [half, def.height * .25, half] });
      }
    }
  }
  return { objects, colliders, replaced: new Set(MODELS.filter(m => m.replaces && library?.models?.has(m.id)).map(m => m.replaces)) };
}
