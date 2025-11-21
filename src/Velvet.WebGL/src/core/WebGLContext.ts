export class WebGLContext {
    private readonly gl: WebGLRenderingContext;
    private readonly canvas: HTMLCanvasElement;

    private constructor(canvas: HTMLCanvasElement, gl: WebGLRenderingContext) {
        this.canvas = canvas;
        this.gl = gl;
    }

    public static create(canvasId: string): WebGLContext {
        const canvas = document.getElementById(canvasId) as HTMLCanvasElement | null;
        if (!canvas) {
            throw new Error(`Canvas with id '${canvasId}' not found`);
        }

        const gl = canvas.getContext('webgl');
        if (!gl) {
            throw new Error('WebGL not supported in this browser');
        }

        return new WebGLContext(canvas, gl);
    }

    public getContext(): WebGLRenderingContext {
        return this.gl;
    }

    public getCanvas(): HTMLCanvasElement {
        return this.canvas;
    }
}
