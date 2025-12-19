import * as API from "./api/VelvetAPI";
import "./api/types";

/**
 * Expose Velvet globally for Blazor / JS / HTML usage.
 */
(window as any).Velvet = {
    init: API.init,
    createShader: API.createShader,
    createProgram: API.createProgram,
    attachShader: API.attachShader,
    linkProgram: API.linkProgram,
    createMesh: API.createMesh,
    drawMesh: API.drawMesh,
    setUniformMatrix4fv: API.setUniformMatrix4fv,
    clear: API.clear,
    resize: API.resize
};

export const Velvet = (window as any).Velvet;