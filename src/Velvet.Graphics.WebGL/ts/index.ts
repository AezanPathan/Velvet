import * as API from "./interop/VelvetAPI";
import "./interop/types";

/**
 * Expose Velvet globally for Blazor / JS / HTML usage.
 */
(window as any).Velvet = {
    init: API.init,
    initById: API.initById,
    createShader: API.createShader,
    createProgram: API.createProgram,
    attachShader: API.attachShader,
    linkProgram: API.linkProgram,
    createMesh: API.createMesh,
    createParticleMesh: API.createParticleMesh,
    updateMeshVertices: API.updateMeshVertices,
    drawMesh: API.drawMesh,
    setUniformMatrix4fv: API.setUniformMatrix4fv,
    setUniformMatrix3fv: API.setUniformMatrix3fv,
    setUniform3f: API.setUniform3f,
    setUniform1f: API.setUniform1f,
    setUniform1i: API.setUniform1i,
    setUniform1b: API.setUniform1b,
    createTextureFromUrl: API.createTextureFromUrl,
    createCubemapTexture: API.createCubemapTexture,
    bindTextureById: API.bindTextureById,
    bindCubemapTextureById: API.bindCubemapTextureById,
    setBlendMode: API.setBlendMode,
    clear: API.clear,
    resize: API.resize,
    setDepthMask: API.setDepthMask
};

export const Velvet = (window as any).Velvet;
