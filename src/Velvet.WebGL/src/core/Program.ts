import { Shader } from './Shader';

export class Program {
    private readonly gl: WebGLRenderingContext;
    private readonly program: WebGLProgram;

    constructor(gl: WebGLRenderingContext, vertexSource: string, fragmentSource: string) {
        this.gl = gl;

        const vertexShader = new Shader(gl, gl.VERTEX_SHADER, vertexSource);
        const fragmentShader = new Shader(gl, gl.FRAGMENT_SHADER, fragmentSource);

        const program = gl.createProgram();
        if (!program) {
            vertexShader.dispose();
            fragmentShader.dispose();
            throw new Error('Failed to create program');
        }

        gl.attachShader(program, vertexShader.getHandle());
        gl.attachShader(program, fragmentShader.getHandle());
        gl.linkProgram(program);

        if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
            const info = gl.getProgramInfoLog(program);
            gl.deleteProgram(program);
            vertexShader.dispose();
            fragmentShader.dispose();
            throw new Error(`Program linking failed: ${info || 'unknown error'}`);
        }

        // Clean up shaders after linking
        vertexShader.dispose();
        fragmentShader.dispose();

        this.program = program;
    }

    public use(): void {
        this.gl.useProgram(this.program);
    }

    public getAttribLocation(name: string): number {
        return this.gl.getAttribLocation(this.program, name);
    }
}
