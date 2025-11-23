export class Mesh {
    private gl: WebGLRenderingContext;
    private positions: Float32Array;
    private indices: Uint16Array;
    private vbo: WebGLBuffer | null = null;
    private ibo: WebGLBuffer | null = null;
    public indexCount: number;

    constructor(gl: WebGLRenderingContext, positions: Float32Array, indices: Uint16Array) {
        this.gl = gl;
        this.positions = positions;
        this.indices = indices;
        this.indexCount = indices.length;
    }

    upload(): void {
        this.vbo = this.gl.createBuffer();
        this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.vbo);
        this.gl.bufferData(this.gl.ARRAY_BUFFER, this.positions, this.gl.STATIC_DRAW);

        this.ibo = this.gl.createBuffer();
        this.gl.bindBuffer(this.gl.ELEMENT_ARRAY_BUFFER, this.ibo);
        this.gl.bufferData(this.gl.ELEMENT_ARRAY_BUFFER, this.indices, this.gl.STATIC_DRAW);
    }

    bind(): void {
        this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.vbo);
        this.gl.bindBuffer(this.gl.ELEMENT_ARRAY_BUFFER, this.ibo);
    }

    dispose(): void {
        if (this.vbo) {
            this.gl.deleteBuffer(this.vbo);
            this.vbo = null;
        }
        if (this.ibo) {
            this.gl.deleteBuffer(this.ibo);
            this.ibo = null;
        }
    }
}
