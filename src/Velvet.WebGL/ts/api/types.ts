export interface VelvetGlobal {
    init: (canvasId: string) => void;
    ensureCanvas: () => void;
    drawTriangle: () => void;
    drawCube: () => void;
}

declare global {
    interface Window {
        Velvet: VelvetGlobal;
    }
}
