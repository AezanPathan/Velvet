import { WebGLContext } from './core/WebGLContext';
import { Renderer } from './core/Renderer';
import { Mesh } from './core/Mesh';
import { Transform3D } from './core/Transform3D';

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

    const vertices = new Float32Array([
        0.0, 0.8, 1.0, 0.0, 0.0,
        -0.8, -0.8, 0.0, 1.0, 0.0,
        0.8, -0.8, 0.0, 0.0, 1.0,
    ]);

    const indices = new Uint16Array([0, 1, 2]);

    const mesh = new Mesh(gl, vertices, indices);
    mesh.upload();

    renderer.setMesh(mesh);

    // Create a Transform3D for animation
    const transform = new Transform3D();

    // Animation loop: Each frame, we update the rotation and compute the model matrix to transform the triangle,
    // demonstrating how the uModel uniform applies transformations to vertices in the vertex shader.
    const animate = () => {
        transform.rotation.z += 0.01; // Increment rotation around Z-axis for visible spinning
        renderer!.setModelMatrix(transform.getMatrix());
        renderer!.drawMesh();
        requestAnimationFrame(animate);
    };

    animate();
}

// Expose global API
window.Velvet = {
    init,
    ensureCanvas,
    drawTriangle,
};

// Export for module usage
export { init, ensureCanvas, drawTriangle };
