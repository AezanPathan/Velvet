import { ResourceManager } from "./ResourceManager";
import { GLShader } from "../../platform/webgl/GLShader";
import { GLProgram } from "../../platform/webgl/GLProgram";
import { GLBuffer } from "../../platform/webgl/GLBuffer";
import { GLMesh } from "../../platform/webgl/GLMesh";
import { GLRenderer } from "../../platform/webgl/GLRenderer";

/**
 * Global engine resource registries.
 * Each manager maps ID → WebGL resource.
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
