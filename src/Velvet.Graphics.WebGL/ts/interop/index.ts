import * as API from "./VelvetAPI";

/**
 * Global Velvet API exposed for:
 * - Blazor interop
 * - Direct JS usage
 */
(window as any).Velvet = {
    // Initialization
    init: API.init,
    initById: API.initById,

    // Shader / Program
    createShader: API.createShader,
    createProgram: API.createProgram,
    attachShader: API.attachShader,
    linkProgram: API.linkProgram,

    // Mesh
    createMesh: API.createMesh,
    createParticleMesh: API.createParticleMesh,
    updateMeshVertices: API.updateMeshVertices,
    drawMesh: API.drawMesh,

    // Uniforms
    setUniformMatrix4fv: API.setUniformMatrix4fv,
    setUniformMatrix3fv: API.setUniformMatrix3fv,
    setUniform3f: API.setUniform3f,
    setUniform1f: API.setUniform1f,
    setUniform1i: API.setUniform1i,
    setUniform1b: API.setUniform1b,

    // Textures
    createTextureFromUrl: API.createTextureFromUrl,
    createCubemapTexture: API.createCubemapTexture,
    bindTextureById: API.bindTextureById,
    bindCubemapTextureById: API.bindCubemapTextureById,

    // Render state
    setBlendMode: API.setBlendMode,
    setDepthMask: API.setDepthMask,

    // Frame
    clear: API.clear,
    resize: API.resize
};

window.addEventListener("load", () => {
    const velvet = (window as any).Velvet;
    if (!velvet || typeof velvet.start !== "function") {
        return;
    }

    const canvas = document.querySelector("canvas") as HTMLCanvasElement | null;
    if (!canvas || !canvas.id) {
        return;
    }

    if (canvas.dataset.velvetStarted === "true") {
        return;
    }

    canvas.dataset.velvetStarted = "true";
    velvet.start(canvas.id);
});

export const Velvet = (window as any).Velvet;