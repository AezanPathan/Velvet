import { WebGLContext } from './WebGLContext';
import { Program } from './WebGLProgram';
import { WebGLMesh } from './WebGLMesh';

export class WebGLRenderer {
    private readonly context: WebGLContext;
    private program: Program | null = null;
    private mesh: WebGLMesh | null = null;
    private uModelLocation: WebGLUniformLocation | null = null;
    private currentModelMatrix: Float32Array = new Float32Array([
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
    ]);

    constructor(context: WebGLContext) {
        this.context = context;
    }

    public getContext(): WebGLContext {
        return this.context;
    }

    public initialize(vertexSource: string, fragmentSource: string): void {
        const gl = this.context.getContext();
        
        this.program = new Program(gl, vertexSource, fragmentSource);
        this.uModelLocation = this.program.getUniformLocation("uModel");
    }

    public setMesh(mesh: WebGLMesh): void {
        this.mesh = mesh;
    }

    public setModelMatrix(matrix: Float32Array): void {
        this.currentModelMatrix = matrix;
    }

    public drawMesh(): void {
        if (!this.program || !this.mesh) {
            throw new Error('Renderer not initialized or mesh not set');
        }

        const gl = this.context.getContext();
        const canvas = this.context.getCanvas();

        // Setup viewport and clear
        gl.viewport(0, 0, canvas.width, canvas.height);
        gl.clearColor(0.1, 0.1, 0.1, 1.0);
        gl.clear(gl.COLOR_BUFFER_BIT);

        // Bind the mesh
        this.mesh.bind();

        // Bind the shader program
        this.program.use();

        // Set model matrix uniform
        gl.uniformMatrix4fv(this.uModelLocation, false, this.currentModelMatrix);

        // Enable the position attribute
        const positionLocation = this.program.getAttribLocation('position');
        gl.enableVertexAttribArray(positionLocation);
        gl.vertexAttribPointer(positionLocation, 3, gl.FLOAT, false, 24, 0);

        // Enable the color attribute
        const colorLocation = this.program.getAttribLocation('color');
        gl.enableVertexAttribArray(colorLocation);
        gl.vertexAttribPointer(colorLocation, 3, gl.FLOAT, false, 24, 12);

        // Draw
        gl.drawElements(gl.TRIANGLES, this.mesh.indexCount, gl.UNSIGNED_SHORT, 0);
    }
}
