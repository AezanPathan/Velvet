import { IBuffer } from "../../core/buffers/IBuffer";

/**
 * WebGL buffer wrapper.
 *
 * Manages GPU buffer lifecycle:
 * - creation
 * - data upload
 * - binding
 * - deletion
 *
 * Used for both vertex and index buffers.
 */
export class GLBuffer implements IBuffer {
  public readonly id: number;

  private handle: WebGLBuffer | null = null;
  private readonly target: GLenum;

  constructor(
    private gl: WebGL2RenderingContext,
    id: number,
    target: GLenum
  ) {
    this.id = id;
    this.target = target;

    const buf = this.gl.createBuffer();
    if (!buf) {
      throw new Error(`GLBuffer: Failed to create buffer (id=${id})`);
    }

    this.handle = buf;
  }

  public setData(data: Float32Array | Uint32Array, usage: GLenum = this.gl.STATIC_DRAW): void {
    if (!this.handle) {
      throw new Error(`GLBuffer: buffer deleted (id=${this.id})`);
    }

    this.gl.bindBuffer(this.target, this.handle);
    this.gl.bufferData(this.target, data, usage);
  }

  public bind(): void {
    if (this.handle) {
      this.gl.bindBuffer(this.target, this.handle);
    }
  }

  public unbind(): void {
    this.gl.bindBuffer(this.target, null);
  }

  public delete(): void {
    if (this.handle) {
      this.gl.deleteBuffer(this.handle);
      this.handle = null;
    }
  }

  public isValid(): boolean {
    return this.handle !== null;
  }

  public get raw(): WebGLBuffer | null {
    return this.handle;
  }
}
