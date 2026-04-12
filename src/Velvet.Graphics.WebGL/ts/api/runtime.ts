import { WebGLContext } from "../webgl/WebGLContext";
import { clearAllManagers } from "../core/resource/Managers";

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

export function ensureFloat32Array(data: unknown): Float32Array {
  if (data instanceof Float32Array) return data;
  if (Array.isArray(data)) return new Float32Array(data);
  return new Float32Array(data as any);
}

export function ensureUint32Array(data: unknown): Uint32Array {
  if (data instanceof Uint32Array) return data;
  if (Array.isArray(data)) return new Uint32Array(data);
  return new Uint32Array(data as any);
}
