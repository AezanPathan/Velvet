import { IBuffer } from "../core/buffers/IBuffer";

/**
 * GLBuffer
 * --------
 * Backend implementation of IBuffer for WebGL2.
 *
 * This class wraps creation, data upload, and binding of WebGL buffer objects.
 * It supports both vertex buffers (ARRAY_BUFFER) and index buffers (ELEMENT_ARRAY_BUFFER).
 *
 * The engine decides what target to use when instantiating this buffer.
 */
export class GLBuffer implements IBuffer {
  public readonly id: number;

  /** Native WebGL buffer handle */
  private handle: WebGLBuffer | null = null;

  /** ARRAY_BUFFER or ELEMENT_ARRAY_BUFFER */
  private readonly target: GLenum;

  constructor(
    private gl: WebGL2RenderingContext,
    id: number,
    target: GLenum // gl.ARRAY_BUFFER or gl.ELEMENT_ARRAY_BUFFER
  ) {
    this.id = id;
    this.target = target;

    const buf = this.gl.createBuffer();
    if (!buf) {
      throw new Error(`GLBuffer: Failed to create buffer (id=${id})`);
    }

    this.handle = buf;
  }

  /**
   * Uploads data to the buffer.
   *
   * @param data Float32Array or Uint16Array depending on vertex/index buffers
   */
  public setData(data: Float32Array | Uint16Array): void {
    if (!this.handle) {
      throw new Error(`GLBuffer.setData: buffer (id=${this.id}) is deleted`);
    }

    this.gl.bindBuffer(this.target, this.handle);
    this.gl.bufferData(this.target, data, this.gl.STATIC_DRAW);
  }

  /**
   * Bind this buffer to its target (ARRAY_BUFFER or ELEMENT_ARRAY_BUFFER).
   */
  public bind(): void {
    if (!this.handle) return;
    this.gl.bindBuffer(this.target, this.handle);
  }

  /**
   * Unbinds the current target buffer.
   * (Engine-side convenience method)
   */
  public unbind(): void {
    this.gl.bindBuffer(this.target, null);
  }

  /**
   * Deletes the buffer from GPU memory.
   */
  public delete(): void {
    if (this.handle) {
      this.gl.deleteBuffer(this.handle);
      this.handle = null;
    }
  }

  /**
   * Returns true if buffer is allocated.
   */
  public isValid(): boolean {
    return this.handle !== null;
  }

  /**
   * Returns the native WebGLBuffer handle (internal engine use only).
   */
  public get raw(): WebGLBuffer | null {
    return this.handle;
  }
}
