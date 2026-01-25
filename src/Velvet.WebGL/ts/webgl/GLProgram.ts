import { IProgram } from "../core/program/IProgram";
import { IShader } from "../core/shaders/IShader";

/**
 * GLProgram
 * ---------
 * WebGL implementation of IProgram.
 *
 * - Creates a WebGL program
 * - Accepts IShader instances (expects GLShader implementation to expose a `raw` getter)
 * - Links, validates and exposes `use()` and lifecycle methods
 */
export class GLProgram implements IProgram {
  public readonly id: number;
  private programHandle: WebGLProgram | null = null;
  private linked = false;

  // Default, engine-wide attribute bindings.
  // These are applied before linking so meshes that use hard-coded locations render reliably.
  private static readonly defaultAttribBindings: ReadonlyArray<{
    location: number;
    name: string;
  }> = [
    { location: 0, name: "aPosition" },
    { location: 1, name: "aNormal" },
    { location: 2, name: "aUV" }
  ];

  constructor(private gl: WebGL2RenderingContext, id: number) {
    this.id = id;
    const program = this.gl.createProgram();
    if (!program) {
      throw new Error("WebGL: Failed to create program");
    }
    this.programHandle = program;
  }

  /**
   * Attach a backend shader (IShader). This method expects that WebGL-specific
   * shader implementations expose a `raw` property containing the native WebGLShader.
   */
  public attachShader(shader: IShader): void {
    if (!this.programHandle) {
      throw new Error("GLProgram: Program handle is already deleted");
    }

    // runtime-bridge: we expect GLShader to expose a `raw` DOM WebGLShader.
    const anyShader = shader as any;
    const rawShader: WebGLShader | null | undefined = anyShader.raw;

    if (!rawShader) {
      throw new Error(
        `GLProgram.attachShader: provided shader (id=${shader.id}) does not expose a native WebGL shader handle (raw).`
      );
    }

    try {
      this.gl.attachShader(this.programHandle, rawShader);
      // If the program was previously linked, attaching a new shader invalidates it.
      this.linked = false;
    } catch (err) {
      throw new Error(
        `WebGLProgram.attachShader: attach failed: ${(err as Error).message}`
      );
    }
  }

  /**
   * Link the attached shaders into a program.
   * Throws on failure with a detailed info log.
   */
  public link(): void {
    if (!this.programHandle) {
      throw new Error("GLProgram.link: program handle is null");
    }

    // Attribute locations are NOT guaranteed unless explicitly bound before linking.
    // Velvet's default mesh layout uses:
    //  - location 0: aPosition
    //  - location 1: aColor
    for (const binding of GLProgram.defaultAttribBindings) {
      this.gl.bindAttribLocation(this.programHandle, binding.location, binding.name);
    }

    this.gl.linkProgram(this.programHandle);

    const success = this.gl.getProgramParameter(
      this.programHandle,
      this.gl.LINK_STATUS
    );
    if (!success) {
      const info = this.getProgramInfoLog();
      // Clean up the program since linking failed
      this.gl.deleteProgram(this.programHandle);
      this.programHandle = null;
      this.linked = false;
      throw new Error(
        `WebGLProgram.link: linking failed:\n${info || "no info log"}`
      );
    }

    this.linked = true;
  }

  /**
   * Use this program on the GL context.
   */
  public use(): void {
    if (!this.programHandle) {
      throw new Error("GLProgram.use: program is deleted or not created");
    }
    if (!this.linked) {
      throw new Error("GLProgram.use: program is not linked");
    }
    this.gl.useProgram(this.programHandle);
  }

  /**
   * Returns whether the program is linked and usable.
   */
  public isLinked(): boolean {
    return this.linked && this.programHandle !== null;
  }

  /**
   * Deletes the underlying WebGLProgram and marks this instance as disposed.
   */
  public delete(): void {
    if (this.programHandle) {
      this.gl.deleteProgram(this.programHandle);
      this.programHandle = null;
      this.linked = false;
    }
  }

  /**
   * Helper: retrieve the program info log for errors/debugging.
   */
  private getProgramInfoLog(): string | null {
    if (!this.programHandle) return null;
    return this.gl.getProgramInfoLog(this.programHandle);
  }

  /**
   * Optional helper to query attribute/uniform locations.
   * These are convenience helpers — keep core behaviour in IProgram minimal.
   */
  public getAttribLocation(name: string): number {
    if (!this.programHandle)
      throw new Error("GLProgram.getAttribLocation: program not available");
    return this.gl.getAttribLocation(this.programHandle, name);
  }

  public getUniformLocation(name: string): WebGLUniformLocation | null {
    if (!this.programHandle)
      throw new Error("GLProgram.getUniformLocation: program not available");
    return this.gl.getUniformLocation(this.programHandle, name);
  }
}
