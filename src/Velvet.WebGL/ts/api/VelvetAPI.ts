import { WebGLContext } from '../webgl/WebGLContext';
import { WebGLRenderer } from '../webgl/WebGLRenderer';
import { WebGLMesh } from '../webgl/WebGLMesh';
import { Transform3D } from '../core/math/Transform3D';
import vertexShader from '../../wwwroot/shaders/basic.vert';
import fragmentShader from '../../wwwroot/shaders/basic.frag';

let renderer: WebGLRenderer | null = null;

export function init(canvasId: string = 'velvetCanvas'): void {
    const context = WebGLContext.create(canvasId);
    renderer = new WebGLRenderer(context);
    renderer.initialize(vertexShader, fragmentShader);
}

export function ensureCanvas(): void {
    if (!document.getElementById('velvetCanvas')) {
        const canvas = document.createElement('canvas');
        canvas.id = 'velvetCanvas';
        canvas.width = 800;
        canvas.height = 600;
        canvas.style.border = '1px solid black';
        document.body.appendChild(canvas);
    }
}

export function drawTriangle(): void {
    drawCube();
}

export function drawCube(): void {
    if (!renderer) throw new Error("Velvet not initialized");

    const gl = renderer.getContext().getContext();

    const vertices = new Float32Array([
        // x, y, z, r, g, b
        -1, -1,  1, 1,0,0,
         1, -1,  1, 0,1,0,
         1,  1,  1, 0,0,1,
        -1,  1,  1, 1,1,0,

        -1, -1, -1, 1,0,1,
         1, -1, -1, 0,1,1,
         1,  1, -1, 1,1,1,
        -1,  1, -1, 0,0,0,
    ]);

    const indices = new Uint16Array([
        // Front
        0, 1, 2, 0, 2, 3,
        // Back
        4, 6, 5, 4, 7, 6,
        // Left
        4, 0, 3, 4, 3, 7,
        // Right
        1, 5, 6, 1, 6, 2,
        // Top
        3, 2, 6, 3, 6, 7,
        // Bottom
        4, 5, 1, 4, 1, 0
    ]);

    const mesh = new WebGLMesh(gl, vertices, indices);
    mesh.upload();

    renderer.setMesh(mesh);

    const transform = new Transform3D();
    transform.scale = { x: 0.5, y: 0.5, z: 0.5 };

    const animate = () => {
        transform.rotation.x += 0.01;
        transform.rotation.y += 0.01;
        transform.rotation.z += 0.01;
        
        renderer!.setModelMatrix(transform.getMatrix());
        renderer!.drawMesh();
        requestAnimationFrame(animate);
    };

    animate();
}
