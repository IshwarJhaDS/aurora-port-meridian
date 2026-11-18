// Minimal, dependency-free glTF 2.0 binary (.glb) reader for generated props.
// It flattens every triangle primitive in the default scene into one MeshData in
// model space, keeps TEXCOORD_0 for the first base-color texture, and normalizes
// the result so the model stands on y = 0, centered on x/z.
import { MeshData } from '../mesh.js';
import { Mat4, Quat, Vec3 } from '../math.js';

const GLB_MAGIC = 0x46546c67, CHUNK_JSON = 0x4e4f534a, CHUNK_BIN = 0x004e4942;
const COMPONENTS = { SCALAR: 1, VEC2: 2, VEC3: 3, VEC4: 4, MAT4: 16 };
const ARRAYS = { 5120: Int8Array, 5121: Uint8Array, 5122: Int16Array, 5123: Uint16Array, 5125: Uint32Array, 5126: Float32Array };
const NORMALIZE = { 5120: 127, 5121: 255, 5122: 32767, 5123: 65535 };
const SUPPORTED_REQUIRED = new Set(['KHR_materials_emissive_strength', 'KHR_texture_transform', 'KHR_materials_unlit']);

export function parseGLB(buffer) {
  const bytes = buffer instanceof Uint8Array ? buffer : new Uint8Array(buffer), view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  if (bytes.byteLength < 20 || view.getUint32(0, true) !== GLB_MAGIC) throw new Error('Not a binary glTF (.glb) file');
  if (view.getUint32(4, true) !== 2) throw new Error('Only glTF 2.0 is supported');
  let offset = 12, json = null, bin = null;
  while (offset + 8 <= bytes.byteLength) {
    const length = view.getUint32(offset, true), type = view.getUint32(offset + 4, true), start = offset + 8;
    if (type === CHUNK_JSON) json = JSON.parse(new TextDecoder().decode(bytes.subarray(start, start + length)));
    else if (type === CHUNK_BIN && !bin) bin = bytes.subarray(start, start + length);
    offset = start + length + ((4 - length % 4) % 4);
  }
  if (!json) throw new Error('GLB has no JSON chunk');
  const unsupported = (json.extensionsRequired || []).filter(x => !SUPPORTED_REQUIRED.has(x));
  if (unsupported.length) throw new Error(`GLB requires unsupported extensions: ${unsupported.join(', ')} (regenerate without compression)`);
  return { json, bin };
}

function bufferViewBytes(gltf, index) {
  const viewDef = gltf.json.bufferViews[index];
  if ((viewDef.buffer ?? 0) !== 0 || !gltf.bin) throw new Error('Only embedded GLB buffers are supported');
  return { bytes: gltf.bin, offset: viewDef.byteOffset || 0, length: viewDef.byteLength, stride: viewDef.byteStride || 0 };
}

export function readAccessor(gltf, index) {
  const a = gltf.json.accessors[index], size = COMPONENTS[a.type], Type = ARRAYS[a.componentType];
  if (!size || !Type) throw new Error(`Unsupported accessor ${a.type}/${a.componentType}`);
  const out = new Float32Array(a.count * size);
  if (a.bufferView == null) return out;
  const v = bufferViewBytes(gltf, a.bufferView), elementBytes = Type.BYTES_PER_ELEMENT, stride = v.stride || size * elementBytes;
  const base = v.bytes.byteOffset + v.offset + (a.byteOffset || 0), dv = new DataView(v.bytes.buffer);
  const scale = a.normalized ? NORMALIZE[a.componentType] : 0;
  const get = { 5120: 'getInt8', 5121: 'getUint8', 5122: 'getInt16', 5123: 'getUint16', 5125: 'getUint32', 5126: 'getFloat32' }[a.componentType];
  for (let i = 0; i < a.count; i++) for (let c = 0; c < size; c++) {
    let value = dv[get](base + i * stride + c * elementBytes, true);
    if (scale) value = Math.max(value / scale, -1);
    out[i * size + c] = value;
  }
  return out;
}

function nodeMatrix(node) {
  if (node.matrix) return new Float32Array(node.matrix);
  const t = node.translation || [0, 0, 0], r = node.rotation || [0, 0, 0, 1], s = node.scale || [1, 1, 1];
  return Mat4.compose(new Vec3(...t), new Quat(...r), new Vec3(...s));
}

/**
 * Converts a parsed GLB into one MeshData. Returns { mesh, imageIndex, material }.
 * Vertex colors carry baseColorFactor (and COLOR_0 when present) in linear space;
 * the shader multiplies them with the albedo texture.
 */
export function buildMesh(gltf) {
  const { json } = gltf, positions = [], normals = [], uvs = [], colors = [], materials = [], indices = [];
  let imageIndex = null, primaryMaterial = null;
  const visit = (nodeIndex, parent) => {
    const node = json.nodes[nodeIndex], world = Mat4.mul(parent, nodeMatrix(node));
    if (node.mesh != null) for (const prim of json.meshes[node.mesh].primitives) {
      if ((prim.mode ?? 4) !== 4 || prim.attributes.POSITION == null) continue;
      if (prim.extensions?.KHR_draco_mesh_compression) throw new Error('Draco-compressed GLB is not supported');
      const p = readAccessor(gltf, prim.attributes.POSITION), count = p.length / 3;
      const n = prim.attributes.NORMAL != null ? readAccessor(gltf, prim.attributes.NORMAL) : null;
      const uv = prim.attributes.TEXCOORD_0 != null ? readAccessor(gltf, prim.attributes.TEXCOORD_0) : new Float32Array(count * 2);
      const col = prim.attributes.COLOR_0 != null ? readAccessor(gltf, prim.attributes.COLOR_0) : null, colSize = col ? col.length / count : 0;
      const mat = json.materials?.[prim.material] || {}, pbr = mat.pbrMetallicRoughness || {}, factor = pbr.baseColorFactor || [1, 1, 1, 1];
      const roughness = pbr.roughnessFactor ?? (pbr.metallicRoughnessTexture ? .75 : 1), metallic = pbr.metallicFactor ?? (pbr.metallicRoughnessTexture ? .1 : 1);
      const texture = pbr.baseColorTexture ? json.textures?.[pbr.baseColorTexture.index] : null;
      const source = texture ? (texture.source ?? texture.extensions?.EXT_texture_webp?.source ?? texture.extensions?.KHR_texture_basisu?.source) : null;
      if (source != null && imageIndex == null) { imageIndex = source; primaryMaterial = mat; }
      // A second textured material cannot share the single albedo slot; fall back to its flat factor.
      const textured = source != null && source === imageIndex;
      const offset = positions.length / 3, normalMatrix = world;
      for (let i = 0; i < count; i++) {
        const q = Mat4.point(world, new Vec3(p[i * 3], p[i * 3 + 1], p[i * 3 + 2])); positions.push(q.x, q.y, q.z);
        if (n) { const v = new Vec3(normalMatrix[0] * n[i * 3] + normalMatrix[4] * n[i * 3 + 1] + normalMatrix[8] * n[i * 3 + 2], normalMatrix[1] * n[i * 3] + normalMatrix[5] * n[i * 3 + 1] + normalMatrix[9] * n[i * 3 + 2], normalMatrix[2] * n[i * 3] + normalMatrix[6] * n[i * 3 + 1] + normalMatrix[10] * n[i * 3 + 2]).normalize(); normals.push(v.x, v.y, v.z); }
        else normals.push(0, 0, 0);
        uvs.push(textured ? uv[i * 2] : .5, textured ? uv[i * 2 + 1] : .5);
        const c = [0, 1, 2].map(k => factor[k] * (col ? col[i * colSize + k] : 1));
        colors.push(...(textured ? c : c.map(x => Math.min(1, x))));
        materials.push(Math.max(.05, Math.min(1, roughness)), Math.max(0, Math.min(1, metallic)), 0, 1);
      }
      const idx = prim.indices != null ? readAccessor(gltf, prim.indices) : Float32Array.from({ length: count }, (_, i) => i);
      for (let i = 0; i + 2 < idx.length; i += 3) indices.push(offset + idx[i], offset + idx[i + 1], offset + idx[i + 2]);
      if (!n) for (let i = 0; i + 2 < idx.length; i += 3) {
        const [a, b, c] = [idx[i], idx[i + 1], idx[i + 2]].map(k => offset + k), pa = Vec3.from(positions, a * 3), face = Vec3.cross(Vec3.sub(Vec3.from(positions, b * 3), pa), Vec3.sub(Vec3.from(positions, c * 3), pa));
        for (const k of [a, b, c]) { normals[k * 3] += face.x; normals[k * 3 + 1] += face.y; normals[k * 3 + 2] += face.z; }
      }
    }
    for (const child of node.children || []) visit(child, world);
  };
  const scene = json.scenes?.[json.scene ?? 0];
  const roots = scene ? scene.nodes : (json.nodes || []).map((_, i) => i).filter(i => !(json.nodes || []).some(n => n.children?.includes(i)));
  for (const root of roots) visit(root, Mat4.identity());
  if (!indices.length) throw new Error('GLB contains no triangle geometry');
  for (let i = 0; i < normals.length; i += 3) { const v = new Vec3(normals[i], normals[i + 1], normals[i + 2]).normalize(); if (!Number.isFinite(v.x)) v.set(0, 1, 0); normals[i] = v.x; normals[i + 1] = v.y; normals[i + 2] = v.z; }
  // Rest the model on y = 0 and center its footprint on the origin.
  let min = [Infinity, Infinity, Infinity], max = [-Infinity, -Infinity, -Infinity];
  for (let i = 0; i < positions.length; i += 3) for (let k = 0; k < 3; k++) { min[k] = Math.min(min[k], positions[i + k]); max[k] = Math.max(max[k], positions[i + k]); }
  const cx = (min[0] + max[0]) / 2, cz = (min[2] + max[2]) / 2;
  for (let i = 0; i < positions.length; i += 3) { positions[i] -= cx; positions[i + 1] -= min[1]; positions[i + 2] -= cz; }
  const mesh = new MeshData(positions, normals, uvs, indices);
  mesh.colors = new Float32Array(colors); mesh.materials = new Float32Array(materials);
  return { mesh, imageIndex, material: primaryMaterial };
}

export function imageBytes(gltf, index) {
  const image = gltf.json.images?.[index]; if (!image || image.bufferView == null) return null;
  const v = bufferViewBytes(gltf, image.bufferView);
  return { bytes: v.bytes.subarray(v.offset, v.offset + v.length), mimeType: image.mimeType || 'image/png' };
}

/** Browser-only: fetch, parse and decode a GLB. Resolves to { mesh, image } where image may be null. */
export async function loadGLB(url, { maxTexture = 2048 } = {}) {
  const response = await fetch(url, { cache: 'force-cache' });
  if (!response.ok) throw new Error(`${url}: HTTP ${response.status}`);
  const gltf = parseGLB(await response.arrayBuffer()), { mesh, imageIndex } = buildMesh(gltf);
  let image = null;
  if (imageIndex != null && typeof createImageBitmap === 'function') {
    const data = imageBytes(gltf, imageIndex);
    if (data) {
      const blob = new Blob([data.bytes], { type: data.mimeType });
      const probe = await createImageBitmap(blob), scale = Math.min(1, maxTexture / Math.max(probe.width, probe.height));
      image = scale < 1 ? await createImageBitmap(blob, { resizeWidth: Math.round(probe.width * scale), resizeHeight: Math.round(probe.height * scale), resizeQuality: 'high' }) : probe;
      if (image !== probe) probe.close?.();
    }
  }
  return { mesh, image };
}
