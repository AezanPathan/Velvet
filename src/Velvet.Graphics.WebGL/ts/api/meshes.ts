import { GLBuffer } from "../webgl/GLBuffer";
import { GLMesh } from "../webgl/GLMesh";
import { BufferManager, MeshManager } from "../core/resource/Managers";
import { ensureFloat32Array, ensureUint32Array, getContext } from "./runtime";

export function createMesh(
  vertices: Float32Array,
  indices?: Uint32Array,
  vertexStrideFloats?: number
): number {
  const context = getContext();
  const gl = context.gl;

  const vertexData = ensureFloat32Array(vertices);
  const vb = new GLBuffer(gl, BufferManager.generateId(), gl.ARRAY_BUFFER);
  vb.setData(vertexData);
  BufferManager.add(vb);

  let ib: GLBuffer | undefined;
  let count = 0;

  if (indices && indices.length > 0) {
    const indexData = ensureUint32Array(indices);
    ib = new GLBuffer(gl, BufferManager.generateId(), gl.ELEMENT_ARRAY_BUFFER);
    ib.setData(indexData);
    BufferManager.add(ib);
    count = indexData.length;
  }

  const mesh = new GLMesh(gl, MeshManager.generateId(), vb, ib);
  const effectiveStride = vertexStrideFloats && vertexStrideFloats > 0 ? vertexStrideFloats : 0;

  if (effectiveStride === 16 || (effectiveStride === 0 && vertexData.length % 16 === 0)) {
    count = indices ? count : vertexData.length / 16;
    mesh.setAttributes([
      { location: 0, size: 3, type: gl.FLOAT, stride: 64, offset: 0 },
      { location: 1, size: 3, type: gl.FLOAT, stride: 64, offset: 12 },
      { location: 2, size: 2, type: gl.FLOAT, stride: 64, offset: 24 },
      { location: 3, size: 4, type: gl.FLOAT, stride: 64, offset: 32 },
      { location: 4, size: 4, type: gl.FLOAT, stride: 64, offset: 48 }
    ]);
  } else if (effectiveStride === 11 || (effectiveStride === 0 && vertexData.length % 11 === 0)) {
    count = indices ? count : vertexData.length / 11;
    mesh.setAttributes([
      { location: 0, size: 3, type: gl.FLOAT, stride: 44, offset: 0 },
      { location: 1, size: 3, type: gl.FLOAT, stride: 44, offset: 12 },
      { location: 2, size: 3, type: gl.FLOAT, stride: 44, offset: 24 },
      { location: 3, size: 2, type: gl.FLOAT, stride: 44, offset: 36 }
    ]);
  } else if (effectiveStride === 9 || (effectiveStride === 0 && vertexData.length % 9 === 0)) {
    count = indices ? count : vertexData.length / 9;
    mesh.setAttributes([
      { location: 0, size: 3, type: gl.FLOAT, stride: 36, offset: 0 },
      { location: 1, size: 3, type: gl.FLOAT, stride: 36, offset: 12 },
      { location: 2, size: 3, type: gl.FLOAT, stride: 36, offset: 24 }
    ]);
  } else if (effectiveStride === 8 || (effectiveStride === 0 && vertexData.length % 8 === 0)) {
    count = indices ? count : vertexData.length / 8;
    mesh.setAttributes([
      { location: 0, size: 3, type: gl.FLOAT, stride: 32, offset: 0 },
      { location: 1, size: 3, type: gl.FLOAT, stride: 32, offset: 12 },
      { location: 2, size: 2, type: gl.FLOAT, stride: 32, offset: 24 }
    ]);
  } else if (effectiveStride === 6 || (effectiveStride === 0 && vertexData.length % 6 === 0)) {
    count = indices ? count : vertexData.length / 6;
    mesh.setAttributes([
      { location: 0, size: 3, type: gl.FLOAT, stride: 24, offset: 0 },
      { location: 1, size: 3, type: gl.FLOAT, stride: 24, offset: 12 }
    ]);
  } else if (effectiveStride === 3 || (effectiveStride === 0 && vertexData.length % 3 === 0)) {
    count = indices ? count : vertexData.length / 3;
    mesh.setAttributes([{ location: 0, size: 3, type: gl.FLOAT, stride: 12, offset: 0 }]);
  } else {
    throw new Error(`Unsupported vertex layout: ${vertexData.length} floats (expected stride 3, 6, 8, 9, 11, or 16)`);
  }

  mesh.setCount(count);
  return MeshManager.add(mesh);
}

export function createParticleMesh(capacity: number): number {
  const context = getContext();
  if (capacity <= 0) throw new Error("createParticleMesh: capacity must be > 0");

  const gl = context.gl;
  const vertexData = new Float32Array(capacity * 8);
  const vb = new GLBuffer(gl, BufferManager.generateId(), gl.ARRAY_BUFFER);
  vb.setData(vertexData, gl.DYNAMIC_DRAW);
  BufferManager.add(vb);

  const mesh = new GLMesh(gl, MeshManager.generateId(), vb);
  mesh.setAttributes([
    { location: 0, size: 3, type: gl.FLOAT, stride: 32, offset: 0 },
    { location: 1, size: 1, type: gl.FLOAT, stride: 32, offset: 12 },
    { location: 2, size: 4, type: gl.FLOAT, stride: 32, offset: 16 }
  ]);
  mesh.setPrimitiveType(gl.POINTS);
  mesh.setCount(0);
  return MeshManager.add(mesh);
}

export function updateMeshVertices(meshId: number, vertices: Float32Array, vertexCount: number): void {
  const mesh = MeshManager.get(meshId) as GLMesh;
  const vertexData = ensureFloat32Array(vertices);
  const count = Math.max(0, vertexCount | 0);

  if (count === 0) {
    mesh.setCount(0);
    return;
  }

  const required = count * 8;
  const data = vertexData.length > required ? vertexData.subarray(0, required) : vertexData;
  mesh.updateVertexData(data, count);
}
