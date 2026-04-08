export interface VelvetGlobal {
    init: (canvas: HTMLCanvasElement) => number;

    createShader: (source: string, type: "vertex" | "fragment") => number;
    createProgram: () => number;
    linkProgram: (programId: number) => void;

    createMesh: (vertices: Float32Array, indices?: Uint32Array) => number;
    createParticleMesh: (capacity: number) => number;
    updateMeshVertices: (meshId: number, vertices: Float32Array, vertexCount: number) => void;
    drawMesh: (meshId: number, programId: number, rendererId: number) => void;

    setBlendMode: (rendererId: number, mode: "off" | "alpha" | "additive") => void;

    clear: (r: number, g: number, b: number, a: number) => void;
    resize: (width: number, height: number) => void;
    setDepthMask: (rendererId: number, enabled: boolean) => void;
}

declare global {
    interface Window {
        Velvet: VelvetGlobal;
    }
}
