import { IProgram } from "../../core/program/IProgram";
import { IShader } from "../../core/shaders/IShader";

/**
 * WebGL shader program.
 *
 * Handles:
 * - shader attachment
 * - linking
 * - uniform lookup (cached)
 * - program usage
 *
 * Enforces fixed attribute locations for engine consistency.
 */

export class GLProgram implements IProgram {
  public readonly id: number;
  private programHandle: WebGLProgram | null = null;
  private linked = false;
  private readonly uniformLocationCache = new Map<string, WebGLUniformLocation | null>();

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

  public attachShader(shader: IShader): void {
    if (!this.programHandle) {
      throw new Error("GLProgram: Program handle is already deleted");
    }

    const shaderWithRawHandle = shader as any;
    const rawShader: WebGLShader | null | undefined = shaderWithRawHandle.raw;

    if (!rawShader) {
      throw new Error(
        `GLProgram.attachShader: provided shader (id=${shader.id}) does not expose a native WebGL shader handle (raw).`
      );
    }

    try {
      this.gl.attachShader(this.programHandle, rawShader);
      this.linked = false;
    } catch (err) {
      throw new Error(
        `WebGLProgram.attachShader: attach failed: ${(err as Error).message}`
      );
    }
  }

  public link(): void {
    if (!this.programHandle) {
      throw new Error("GLProgram.link: program handle is null");
    }

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
    this.uniformLocationCache.clear();
  }

  public use(): void {
    if (!this.programHandle) {
      throw new Error("GLProgram.use: program is deleted or not created");
    }
    if (!this.linked) {
      throw new Error("GLProgram.use: program is not linked");
    }
    this.gl.useProgram(this.programHandle);
  }

  public isLinked(): boolean {
    return this.linked && this.programHandle !== null;
  }

  public delete(): void {
    if (this.programHandle) {
      this.gl.deleteProgram(this.programHandle);
      this.programHandle = null;
      this.linked = false;
      this.uniformLocationCache.clear();
    }
  }

  private getProgramInfoLog(): string | null {
    if (!this.programHandle) return null;
    return this.gl.getProgramInfoLog(this.programHandle);
  }

  public getAttribLocation(name: string): number {
    if (!this.programHandle)
      throw new Error("GLProgram.getAttribLocation: program not available");
    return this.gl.getAttribLocation(this.programHandle, name);
  }

  public getUniformLocation(name: string): WebGLUniformLocation | null {
    if (!this.programHandle)
      throw new Error("GLProgram.getUniformLocation: program not available");

    if (this.uniformLocationCache.has(name)) {
      return this.uniformLocationCache.get(name) ?? null;
    }

    const location = this.gl.getUniformLocation(this.programHandle, name);
    this.uniformLocationCache.set(name, location);
    return location;
  }
}
