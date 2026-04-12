import { WebGLContext } from "../webgl/WebGLContext";
import { GLRenderer } from "../webgl/GLRenderer";
import { GLShader } from "../webgl/GLShader";
import { GLProgram } from "../webgl/GLProgram";
import { ProgramManager, RendererManager, ShaderManager } from "../core/resource/Managers";
import { getContext, setContext } from "./runtime";
import {
  setUniformMatrix4fv,
  setUniformMatrix3fv,
  setUniform3f,
  setUniform1f,
  setUniform1i,
  setUniform1b
} from "./uniforms";
import { createMesh, createParticleMesh, updateMeshVertices } from "./meshes";
import {
  loadTexture,
  createTextureFromUrl,
  bindTextureById,
  createCubemapTexture,
  bindCubemapTextureById,
  bindTexture
} from "./textures";
import { drawMesh, setBlendMode, clear, resize, setDepthMask } from "./rendererState";

export { setUniformMatrix4fv, setUniformMatrix3fv, setUniform3f, setUniform1f, setUniform1i, setUniform1b };
export { createMesh, createParticleMesh, updateMeshVertices };
export { loadTexture, createTextureFromUrl, bindTextureById, createCubemapTexture, bindCubemapTextureById, bindTexture };
export { drawMesh, setBlendMode, clear, resize, setDepthMask };

export function init(canvas: string | HTMLCanvasElement): number {
  let canvasElement: HTMLCanvasElement;

  if (typeof canvas === "string") {
    const element = document.getElementById(canvas);
    if (!element) {
      throw new Error(`Velvet.init: canvas '${canvas}' not found`);
    }
    if (!(element instanceof HTMLCanvasElement)) {
      throw new Error(`Velvet.init: element '${canvas}' is not a canvas (found ${element.tagName})`);
    }
    canvasElement = element;
  } else {
    if (!(canvas instanceof HTMLCanvasElement)) {
      throw new Error("Velvet.init: provided element is not an HTMLCanvasElement");
    }
    canvasElement = canvas;
  }

  const context = new WebGLContext(canvasElement);
  setContext(context);

  const renderer = new GLRenderer(context.gl, RendererManager.generateId());
  return RendererManager.add(renderer);
}

export function createShader(source: string, type: "vertex" | "fragment"): number {
  const gl = getContext().gl;
  const shader = new GLShader(gl, ShaderManager.generateId());
  shader.compile(source, type);

  return ShaderManager.add(shader);
}

export function createProgram(): number {
  const gl = getContext().gl;
  const program = new GLProgram(gl, ProgramManager.generateId());
  return ProgramManager.add(program);
}

export function attachShader(programId: number, shaderId: number): void {
  const program = ProgramManager.get(programId);
  const shader = ShaderManager.get(shaderId);
  program.attachShader(shader);
}

export function linkProgram(programId: number): void {
  const program = ProgramManager.get(programId);
  program.link();
}
