export interface VelvetGlobal {
    init: (canvas: HTMLCanvasElement) => number;

    createShader: (source: string, type: "vertex" | "fragment") => number;
    createProgram: () => number;
    linkProgram: (programId: number) => void;

    createMesh: (vertices: Float32Array, indices?: Uint32Array) => number;
    drawMesh: (meshId: number, programId: number, rendererId: number) => void;

    clear: (r: number, g: number, b: number, a: number) => void;
    resize: (width: number, height: number) => void;
}

declare global {
    interface Window {
        Velvet: VelvetGlobal;
    }
}
