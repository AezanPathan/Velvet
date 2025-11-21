import { WebGLContext } from './core/WebGLContext';
import { Renderer } from './core/Renderer';

// Import shader sources as raw strings
import vertexShader from './shaders/basic.vert';
import fragmentShader from './shaders/basic.frag';

// Global API for C# interop
declare global {
    interface Window {
        Velvet: {
            init: (canvasId: string) => void;
            ensureCanvas: () => void;
            drawTriangle: () => void;
        };
    }
}

let renderer: Renderer | null = null;

function init(canvasId: string = 'velvetCanvas'): void {
    const context = WebGLContext.create(canvasId);
    renderer = new Renderer(context);
    renderer.initialize(vertexShader, fragmentShader);
}

function ensureCanvas(): void {
    if (!document.getElementById('velvetCanvas')) {
        const canvas = document.createElement('canvas');
        canvas.id = 'velvetCanvas';
        canvas.width = 800;
        canvas.height = 600;
        canvas.style.border = '1px solid black';
        document.body.appendChild(canvas);
    }
}

function drawTriangle(): void {
    if (!renderer) {
        throw new Error('Velvet not initialized. Call init() first.');
    }
    renderer.drawTriangle();
}

// Expose global API
window.Velvet = {
    init,
    ensureCanvas,
    drawTriangle,
};

// Export for module usage
export { init, ensureCanvas, drawTriangle };
