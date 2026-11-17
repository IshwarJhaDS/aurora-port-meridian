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
