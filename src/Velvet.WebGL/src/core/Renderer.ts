import { WebGLContext } from './WebGLContext';
import { Program } from './Program';

export class Renderer {
    private readonly context: WebGLContext;
    private program: Program | null = null;
    private vertexBuffer: WebGLBuffer | null = null;

    constructor(context: WebGLContext) {
        this.context = context;
    }

    public initialize(vertexSource: string, fragmentSource: string): void {
        const gl = this.context.getContext();
        
        this.program = new Program(gl, vertexSource, fragmentSource);
        
        const buffer = gl.createBuffer();
        if (!buffer) {
            throw new Error('Failed to create vertex buffer');
        }
        this.vertexBuffer = buffer;
    }

    public drawTriangle(): void {
        if (!this.program || !this.vertexBuffer) {
            throw new Error('Renderer not initialized');
        }

        const gl = this.context.getContext();
        const canvas = this.context.getCanvas();

        // Setup viewport and clear
        gl.viewport(0, 0, canvas.width, canvas.height);
        gl.clearColor(0.1, 0.1, 0.1, 1.0);
        gl.clear(gl.COLOR_BUFFER_BIT);

        // Triangle vertices
        const vertices = new Float32Array([
            0.0, 0.8,
            -0.8, -0.8,
            0.8, -0.8,
        ]);

        // Upload vertices
        gl.bindBuffer(gl.ARRAY_BUFFER, this.vertexBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, vertices, gl.STATIC_DRAW);

        // Use program and setup attributes
        this.program.use();

        const positionLocation = this.program.getAttribLocation('position');
        gl.enableVertexAttribArray(positionLocation);
        gl.vertexAttribPointer(positionLocation, 2, gl.FLOAT, false, 0, 0);

        // Draw
        gl.drawArrays(gl.TRIANGLES, 0, 3);
    }
}
