import { WebGLContext } from './core/WebGLContext';
import { Renderer } from './core/Renderer';
import { Mesh } from './core/Mesh';

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
    if (!renderer) throw new Error("Velvet not initialized");

    const gl = renderer.getContext().getContext();

    const positions = new Float32Array([
        0.0, 0.8,
        -0.8, -0.8,
        0.8, -0.8,
    ]);

    const indices = new Uint16Array([0, 1, 2]);

    const mesh = new Mesh(gl, positions, indices);
    mesh.upload();

    renderer.setMesh(mesh);
    renderer.drawMesh();
}

// Expose global API
window.Velvet = {
    init,
    ensureCanvas,
    drawTriangle,
};

// Export for module usage
export { init, ensureCanvas, drawTriangle };
