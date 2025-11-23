export class IndexBuffer {
    private readonly gl: WebGLRenderingContext;
    private readonly buffer: WebGLBuffer;

    constructor(gl: WebGLRenderingContext) {
        this.gl = gl;
        const buffer = gl.createBuffer();
        if (!buffer) {
            throw new Error('Failed to create index buffer');
        }
        this.buffer = buffer;
    }

    public upload(data: Uint16Array): void {
        this.gl.bindBuffer(this.gl.ELEMENT_ARRAY_BUFFER, this.buffer);
        this.gl.bufferData(this.gl.ELEMENT_ARRAY_BUFFER, data, this.gl.STATIC_DRAW);
    }

    public bind(): void {
        this.gl.bindBuffer(this.gl.ELEMENT_ARRAY_BUFFER, this.buffer);
    }

    public dispose(): void {
        this.gl.deleteBuffer(this.buffer);
    }
}
