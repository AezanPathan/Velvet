import { WebGLContext } from './WebGLContext';
import { Program } from './Program';
import { Mesh } from './Mesh';

export class Renderer {
    private readonly context: WebGLContext;
    private program: Program | null = null;
    private mesh: Mesh | null = null;

    constructor(context: WebGLContext) {
        this.context = context;
    }

    public getContext(): WebGLContext {
        return this.context;
    }

    public initialize(vertexSource: string, fragmentSource: string): void {
        const gl = this.context.getContext();
        
        this.program = new Program(gl, vertexSource, fragmentSource);
    }

    public setMesh(mesh: Mesh): void {
        this.mesh = mesh;
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

        // Enable the position attribute
        const positionLocation = this.program.getAttribLocation('position');
        gl.enableVertexAttribArray(positionLocation);
        gl.vertexAttribPointer(positionLocation, 2, gl.FLOAT, false, 5 * 4, 0);

        // Enable the color attribute
        const colorLocation = this.program.getAttribLocation('color');
        gl.enableVertexAttribArray(colorLocation);
        gl.vertexAttribPointer(colorLocation, 3, gl.FLOAT, false, 5 * 4, 2 * 4);

        // Draw
        gl.drawElements(gl.TRIANGLES, this.mesh.indexCount, gl.UNSIGNED_SHORT, 0);
    }
}
