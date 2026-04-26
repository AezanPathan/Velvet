import { IMesh } from "../../core/mesh/IMesh";
import { IBuffer } from "../../core/buffers/IBuffer";

/**
 * WebGL mesh implementation.
 *
 * Encapsulates:
 * - vertex/index buffers
 * - vertex layout (attributes)
 * - VAO configuration
 * - draw call
 *
 * VAO is created lazily and cached.
 */

export interface VertexAttribute {
  location: number;
  size: number;
  type: GLenum;
  stride: number;
  offset: number;
  isInteger?: boolean;
}


export class GLMesh implements IMesh {
  public readonly id: number;

  public vertexBuffer: IBuffer;
  public indexBuffer?: IBuffer;

  private attributes: VertexAttribute[] = [];
  private vao: WebGLVertexArrayObject | null = null;
  private count: number = 0;
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

  public setAttributes(attributes: VertexAttribute[]): void {
    this.attributes = attributes;
    this.invalidateVao();
  }

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

    this.gl.bindVertexArray(this.vao);

    this.vertexBuffer.bind();

    for (const attr of this.attributes) {
      this.gl.enableVertexAttribArray(attr.location);
      if (attr.isInteger) {
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

    if (this.indexBuffer) {
      this.indexBuffer.bind();
    }

    this.gl.bindVertexArray(null);
  }

  public draw(): void {
    const gl = this.gl;

    this.ensureVaoConfigured();
    if (!this.vao) {
      throw new Error(`GLMesh.draw: VAO is not available (mesh id=${this.id})`);
    }

    gl.bindVertexArray(this.vao);

    if (this.count > 0) {
      if (this.indexBuffer) {
        gl.drawElements(this.primitiveType, this.count, gl.UNSIGNED_INT, 0);
      } else {
        gl.drawArrays(this.primitiveType, 0, this.count);
      }
    }

    gl.bindVertexArray(null);
  }

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

