import { IShader } from "../core/shaders/IShader";

/**
 * GLShader
 * --------
 * A backend implementation of the IShader interface for WebGL2.
 *
 * This class is a thin, safe wrapper around WebGL shader creation and compilation.
 * All WebGL-specific behavior stays inside this file, while the core engine
 * communicates through the IShader interface.
 */
export class GLShader implements IShader {
  /** Unique engine-side ID (not the WebGL shader handle) */
  public readonly id: number;

  /** The actual WebGL shader handle */
  private handle: WebGLShader | null = null;

  constructor(private gl: WebGL2RenderingContext, id: number) {
    this.id = id;
  }

  /**
   * Compiles the shader source for a given shader type.
   *
   * @param source GLSL shader code
   * @param type "vertex" | "fragment"
   */
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

  /**
   * Returns true if the shader is compiled and ready.
   */
  public isValid(): boolean {
    return this.handle !== null;
  }

  /**
   * Returns the raw WebGL shader handle.
   * Used internally by GLProgram only.
   */
  public get raw(): WebGLShader | null {
    return this.handle;
  }

  /**
   * Deletes the shader from GPU memory.
   */
  public delete(): void {
    if (this.handle) {
      this.gl.deleteShader(this.handle);
      this.handle = null;
    }
  }
}
