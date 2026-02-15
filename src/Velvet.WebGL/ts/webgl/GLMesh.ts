import { IMesh } from "../core/mesh/IMesh";
import { IBuffer } from "../core/buffers/IBuffer";

/**
 * Attribute description
 * ---------------------
 * Describes how a vertex attribute is stored inside a buffer.
 *
 * Example:
 *  - location: 0 (shader attribute location)
 *  - size: 3 (vec3)
 *  - type: gl.FLOAT
 *  - stride: vertexSize * 4
 *  - offset: 0
 */
export interface VertexAttribute {
  location: number;
  size: number;      // number of components (1-4)
  type: GLenum;      // gl.FLOAT, gl.UNSIGNED_INT, gl.UNSIGNED_BYTE, etc.
  stride: number;    // total size of a vertex in bytes
  offset: number;    // byte offset of this attribute
  isInteger?: boolean; // true for integer attributes (use vertexAttribIPointer)
}

/**
 * GLMesh
 * ------
 * WebGL implementation of IMesh.
 *
 * A mesh contains:
 *  - a vertex buffer (positions, later normals/uvs)
 *  - an optional index buffer (triangles)
 *  - attribute descriptions (how to interpret vertex data)
 *
 * The renderer will call draw() to draw this mesh.
 */
export class GLMesh implements IMesh {
  public readonly id: number;

  public vertexBuffer: IBuffer;
  public indexBuffer?: IBuffer;

  /** Attribute layout for vertex buffer */
  private attributes: VertexAttribute[] = [];

  /** WebGL2 requires vertex attribute state to be stored in a VAO */
  private vao: WebGLVertexArrayObject | null = null;

  /** Number of indices or vertices to draw */
  private count: number = 0;

  /** Primitive type used for drawing (TRIANGLES by default) */
  private primitiveType: GLenum;

  constructor(
    private gl: WebGL2RenderingContext,
    id: number,
    vertexBuffer: IBuffer,
    indexBuffer?: IBuffer
  ) {
    this.id = id;
    this.vertexBuffer = vertexBuffer;
    this.indexBuffer = indexBuffer;
    this.primitiveType = this.gl.TRIANGLES;
  }

  /**
   * Specifies vertex attribute layout for this mesh.
   *
   * Example:
   * mesh.setAttributes([
   *   { location: 0, size: 3, type: gl.FLOAT, stride: 12, offset: 0 }
   * ]);
   */
  public setAttributes(attributes: VertexAttribute[]): void {
    this.attributes = attributes;
    this.invalidateVao();
  }

  /**
   * Sets the number of vertices or indices to draw.
   */
  public setCount(count: number): void {
    this.count = count;
  }

  public setPrimitiveType(primitive: GLenum): void {
    this.primitiveType = primitive;
  }

  private invalidateVao(): void {
    if (!this.vao) return;
    this.gl.deleteVertexArray(this.vao);
    this.vao = null;
  }

  private ensureVaoConfigured(): void {
    if (this.vao) return;

    const vao = this.gl.createVertexArray();
    if (!vao) {
      throw new Error(`GLMesh: Failed to create VAO (mesh id=${this.id})`);
    }

    this.vao = vao;

    // Configure VAO once: bind buffers + define vertex attributes.
    this.gl.bindVertexArray(this.vao);

    // Bind vertex buffer so vertexAttribPointer captures it into VAO state
    this.vertexBuffer.bind();

    for (const attr of this.attributes) {
      this.gl.enableVertexAttribArray(attr.location);
      if (attr.isInteger) {
        // Use vertexAttribIPointer for integer attributes (e.g., joint indices)
        this.gl.vertexAttribIPointer(
          attr.location,
          attr.size,
          attr.type,
          attr.stride,
          attr.offset
        );
      } else {
        this.gl.vertexAttribPointer(
          attr.location,
          attr.size,
          attr.type,
          false,
          attr.stride,
          attr.offset
        );
      }
    }

    // Bind index buffer (ELEMENT_ARRAY_BUFFER binding is also stored in VAO)
    if (this.indexBuffer) {
      this.indexBuffer.bind();
    }

    // Unbind VAO to avoid relying on implicit global state
    this.gl.bindVertexArray(null);
  }

  /**
   * Bind buffers & attributes, then issue draw call.
   */
  public draw(): void {
    const gl = this.gl;

    this.ensureVaoConfigured();
    if (!this.vao) {
      throw new Error(`GLMesh.draw: VAO is not available (mesh id=${this.id})`);
    }

    gl.bindVertexArray(this.vao);

    // Only draw if we have a valid count
    if (this.count > 0) {
      // If using indices, must have index buffer
      if (this.indexBuffer) {
        gl.drawElements(this.primitiveType, this.count, gl.UNSIGNED_INT, 0);
      } else {
        // Non-indexed draw with vertex count
        gl.drawArrays(this.primitiveType, 0, this.count);
      }
    }

    gl.bindVertexArray(null);
  }

  /**
   * Deletes associated buffers.
   */
  public delete(): void {
    if (this.vao) {
      this.gl.deleteVertexArray(this.vao);
      this.vao = null;
    }
    this.vertexBuffer.delete();
    if (this.indexBuffer) {
      this.indexBuffer.delete();
    }
  }

  public updateVertexData(data: Float32Array, count: number): void {
    this.vertexBuffer.bind();
    this.gl.bufferSubData(this.gl.ARRAY_BUFFER, 0, data);
    this.count = count;
  }
}

