export class Shader {
    private readonly gl: WebGLRenderingContext;
    private readonly handle: WebGLShader;

    constructor(gl: WebGLRenderingContext, type: number, source: string) {
        this.gl = gl;

        const shader = gl.createShader(type);
        if (!shader) {
            throw new Error('Failed to create shader');
        }

        gl.shaderSource(shader, source);
        gl.compileShader(shader);

        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
            const info = gl.getShaderInfoLog(shader);
            gl.deleteShader(shader);
            throw new Error(`Shader compilation failed: ${info || 'unknown error'}`);
        }

        this.handle = shader;
    }

    public getHandle(): WebGLShader {
        return this.handle;
    }

    public dispose(): void {
        this.gl.deleteShader(this.handle);
    }
}
