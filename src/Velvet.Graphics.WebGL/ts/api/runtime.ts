import { WebGLContext } from "../webgl/WebGLContext";
import { clearAllManagers } from "../core/resource/Managers";
import { GLProgram } from "../webgl/GLProgram";
import { ProgramManager } from "../core/resource/Managers";

let context: WebGLContext | null = null;

export function setContext(next: WebGLContext): void {
  context = next;

  // Reset stale IDs/resources on context transitions.
  next.onContextLost(() => clearAllManagers());
  next.onContextRestored(() => clearAllManagers());
}

export function getContext(): WebGLContext {
  if (!context) {
    throw new Error("Velvet not initialized");
  }

  return context;
}

type NumericArrayInput = ArrayLike<number> | ArrayBuffer;

export function ensureFloat32Array(data: unknown): Float32Array {
  if (data instanceof Float32Array) return data;
  if (Array.isArray(data)) return new Float32Array(data);
  return new Float32Array(data as NumericArrayInput);
}

export function ensureUint32Array(data: unknown): Uint32Array {
  if (data instanceof Uint32Array) return data;
  if (Array.isArray(data)) return new Uint32Array(data);
  return new Uint32Array(data as NumericArrayInput);
}

export function withOptionalUniformLocation(
  programId: number,
  name: string,
  apply: (gl: WebGL2RenderingContext, location: WebGLUniformLocation, program: GLProgram) => void
): void {
  const gl = getContext().gl;
  const program = ProgramManager.get(programId);
  const location = program.getUniformLocation(name);

  if (location === null) {
    console.warn(
      `Velvet.withOptionalUniformLocation: uniform '${name}' not found on program id=${programId}. ` +
      "This may happen when the uniform is optimized out or the name is incorrect."
    );
    return;
  }

  program.use();
  apply(gl, location, program);
}

export function getRequiredUniformLocation(
  programId: number,
  name: string,
  errorPrefix: string
): { gl: WebGL2RenderingContext; location: WebGLUniformLocation; program: GLProgram } {
  const gl = getContext().gl;
  const program = ProgramManager.get(programId);
  const location = program.getUniformLocation(name);
  if (location === null) {
    throw new Error(`${errorPrefix}: uniform ${name} not found`);
  }

  return { gl, location, program };
}
