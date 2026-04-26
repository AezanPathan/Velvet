import { IShader } from "../../core/shaders/IShader";

/**
 * WebGL shader wrapper.
 *
 * Handles compilation and lifecycle of a single shader.
 * Used internally by GLProgram.
 */

export class GLShader implements IShader {
  public readonly id: number;
  private handle: WebGLShader | null = null;

  constructor(private gl: WebGL2RenderingContext, id: number) {
    this.id = id;
  }

  public compile(source: string, type: "vertex" | "fragment"): void {
    const gl = this.gl;

    const shaderType =
      type === "vertex" ? gl.VERTEX_SHADER : gl.FRAGMENT_SHADER;

    const shader = gl.createShader(shaderType);

    if (!shader) {
      throw new Error(`WebGL: Failed to create shader (type: ${type})`);
    }

    gl.shaderSource(shader, source);
    gl.compileShader(shader);

    const success = gl.getShaderParameter(shader, gl.COMPILE_STATUS);

    if (!success) {
      const infoLog = gl.getShaderInfoLog(shader);
      gl.deleteShader(shader);

      throw new Error(
        `WebGL shader compilation failed (type: ${type}):\n${infoLog}`
      );
    }

    this.handle = shader;
  }

  public isValid(): boolean {
    return this.handle !== null;
  }

  public get raw(): WebGLShader | null {
    return this.handle;
  }

  public delete(): void {
    if (this.handle) {
      this.gl.deleteShader(this.handle);
      this.handle = null;
    }
  }
}
