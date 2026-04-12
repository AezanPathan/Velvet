import { ResourceManager } from "./ResourceManager";
import { GLShader } from "../../webgl/GLShader";
import { GLProgram } from "../../webgl/GLProgram";
import { GLBuffer } from "../../webgl/GLBuffer";
import { GLMesh } from "../../webgl/GLMesh";
import { GLRenderer } from "../../webgl/GLRenderer";

/**
 * Global resource managers for the engine backend.
 * These handle ID → Resource mapping.
 */

export const ShaderManager = new ResourceManager<GLShader>();
export const ProgramManager = new ResourceManager<GLProgram>();
export const BufferManager = new ResourceManager<GLBuffer>();
export const MeshManager = new ResourceManager<GLMesh>();
export const RendererManager = new ResourceManager<GLRenderer>();
export const TextureManager = new ResourceManager<WebGLTexture>();

export function clearAllManagers(): void {
    ShaderManager.clear();
    ProgramManager.clear();
    BufferManager.clear();
    MeshManager.clear();
    RendererManager.clear();
    TextureManager.clear();
}
