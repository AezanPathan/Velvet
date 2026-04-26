import { GLBuffer } from "../platform/webgl/GLBuffer";
import { GLMesh, VertexAttribute } from "../platform/webgl/GLMesh";
import { BufferManager, MeshManager } from "../core/resource/Managers";
import { ensureFloat32Array, ensureUint32Array, getContext } from "./runtime";

/**
 * Mesh creation and update utilities.
 * Handles vertex buffers, index buffers, and attribute layout.
 */

type LayoutAttribute = Readonly<{
  location: number;
  size: number;
  offsetFloats: number;
}>;

type VertexLayout = Readonly<{
  strideFloats: number;
  attributes: readonly LayoutAttribute[];
}>;

const VERTEX_LAYOUTS: readonly VertexLayout[] = [
  {
    strideFloats: 16,
    attributes: [
      { location: 0, size: 3, offsetFloats: 0 },
      { location: 1, size: 3, offsetFloats: 3 },
      { location: 2, size: 2, offsetFloats: 6 },
      { location: 3, size: 4, offsetFloats: 8 },
      { location: 4, size: 4, offsetFloats: 12 }
    ]
  },
  {
    strideFloats: 11,
    attributes: [
      { location: 0, size: 3, offsetFloats: 0 },
      { location: 1, size: 3, offsetFloats: 3 },
      { location: 2, size: 3, offsetFloats: 6 },
      { location: 3, size: 2, offsetFloats: 9 }
    ]
  },
  {
    strideFloats: 9,
    attributes: [
      { location: 0, size: 3, offsetFloats: 0 },
      { location: 1, size: 3, offsetFloats: 3 },
      { location: 2, size: 3, offsetFloats: 6 }
    ]
  },
  {
    strideFloats: 8,
    attributes: [
      { location: 0, size: 3, offsetFloats: 0 },
      { location: 1, size: 3, offsetFloats: 3 },
      { location: 2, size: 2, offsetFloats: 6 }
    ]
  },
  {
    strideFloats: 6,
    attributes: [
      { location: 0, size: 3, offsetFloats: 0 },
      { location: 1, size: 3, offsetFloats: 3 }
    ]
  },
  {
    strideFloats: 3,
    attributes: [{ location: 0, size: 3, offsetFloats: 0 }]
  }
];

function resolveLayout(vertexLength: number, explicitStride: number): VertexLayout | undefined {
  if (explicitStride > 0) {
    return VERTEX_LAYOUTS.find((layout) => layout.strideFloats === explicitStride);
  }

  return VERTEX_LAYOUTS.find((layout) => vertexLength % layout.strideFloats === 0);
}

function buildAttributes(gl: WebGL2RenderingContext, layout: VertexLayout): VertexAttribute[] {
  const stride = layout.strideFloats * 4;
  return layout.attributes.map((attribute) => ({
    location: attribute.location,
    size: attribute.size,
    type: gl.FLOAT,
    stride,
    offset: attribute.offsetFloats * 4
  }));
}

export function createMesh(
  vertices: Float32Array,
  indices?: Uint32Array,
  vertexStrideFloats?: number
): number {
  const context = getContext();
  const gl = context.gl;

  const vertexData = ensureFloat32Array(vertices);
  const vertexBufferId = BufferManager.generateId();
  const vb = new GLBuffer(gl, vertexBufferId, gl.ARRAY_BUFFER);
  vb.setData(vertexData);
  BufferManager.register(vertexBufferId, vb);

  let ib: GLBuffer | undefined;
  let drawCount = 0;

  if (indices && indices.length > 0) {
    const indexData = ensureUint32Array(indices);
    const indexBufferId = BufferManager.generateId();
    ib = new GLBuffer(gl, indexBufferId, gl.ELEMENT_ARRAY_BUFFER);
    ib.setData(indexData);
    BufferManager.register(indexBufferId, ib);
    drawCount = indexData.length;
  }

  const meshId = MeshManager.generateId();
  const mesh = new GLMesh(gl, meshId, vb, ib);
  const effectiveStride = vertexStrideFloats && vertexStrideFloats > 0 ? vertexStrideFloats : 0;
  const layout = resolveLayout(vertexData.length, effectiveStride);
  if (!layout) {
    throw new Error(`Unsupported vertex layout (${vertexData.length} floats). Expected stride: 3, 6, 8, 9, 11, or 16`);
  }

  drawCount = indices ? drawCount : vertexData.length / layout.strideFloats;
  mesh.setAttributes(buildAttributes(gl, layout));
  mesh.setCount(drawCount);
  MeshManager.register(meshId, mesh);
  return meshId;
}

export function createParticleMesh(capacity: number): number {
  const context = getContext();
  if (capacity <= 0) throw new Error("createParticleMesh: capacity must be > 0");

  const gl = context.gl;
  const vertexData = new Float32Array(capacity * 8);
  const vertexBufferId = BufferManager.generateId();
  const vb = new GLBuffer(gl, vertexBufferId, gl.ARRAY_BUFFER);
  vb.setData(vertexData, gl.DYNAMIC_DRAW);
  BufferManager.register(vertexBufferId, vb);

  const meshId = MeshManager.generateId();
  const mesh = new GLMesh(gl, meshId, vb);
  mesh.setAttributes([
    { location: 0, size: 3, type: gl.FLOAT, stride: 32, offset: 0 },
    { location: 1, size: 1, type: gl.FLOAT, stride: 32, offset: 12 },
    { location: 2, size: 4, type: gl.FLOAT, stride: 32, offset: 16 }
  ]);
  mesh.setPrimitiveType(gl.POINTS);
  mesh.setCount(0);
  MeshManager.register(meshId, mesh);
  return meshId;
}

export function updateMeshVertices(meshId: number, vertices: Float32Array, vertexCount: number): void {
  const mesh = MeshManager.get(meshId);
  const vertexData = ensureFloat32Array(vertices);
  const drawCount = Math.max(0, vertexCount | 0);

  if (drawCount === 0) {
    mesh.setCount(0);
    return;
  }

  const required = drawCount * 8;
  const data = vertexData.length > required ? vertexData.subarray(0, required) : vertexData;
  mesh.updateVertexData(data, drawCount);
}
