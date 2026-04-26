/**
 * WebGL context wrapper.
 *
 * Responsibilities:
 * - initialize WebGL (2 → fallback 1)
 * - expose GPU capabilities
 * - handle context loss/restore
 * - manage canvas + viewport
 */

export class WebGLContext {
  public readonly canvas: HTMLCanvasElement;
  public readonly gl: WebGL2RenderingContext;

  public readonly isWebGL2: boolean = true;
  public readonly caps: {
    maxTextures: number;
    maxVertexAttribs: number;
    maxTextureSize: number;
  };

  private readonly contextLostHandlers: Array<() => void> = [];
  private readonly contextRestoredHandlers: Array<() => void> = [];

  constructor(canvas: HTMLCanvasElement) {
    this.canvas = canvas;

    const gl2 = canvas.getContext("webgl2", {
      alpha: false,
      antialias: true,
      depth: true,
      stencil: false,
      premultipliedAlpha: false,
      preserveDrawingBuffer: false
    }) as WebGL2RenderingContext | null;

    if (gl2) {
      this.gl = gl2;
      this.isWebGL2 = true;
    } else {
      const gl1 = canvas.getContext("webgl", {
        alpha: false,
        antialias: true,
        depth: true
      }) as WebGL2RenderingContext | null;

      if (!gl1) {
        throw new Error("Velvet: WebGL is not supported by this browser.");
      }

      this.gl = gl1;
      this.isWebGL2 = false;
    }

    this.caps = {
      maxTextures: this.gl.getParameter(this.gl.MAX_TEXTURE_IMAGE_UNITS),
      maxVertexAttribs: this.gl.getParameter(this.gl.MAX_VERTEX_ATTRIBS),
      maxTextureSize: this.gl.getParameter(this.gl.MAX_TEXTURE_SIZE)
    };

    // Context loss handling
    this.canvas.addEventListener("webglcontextlost", (e) => {
      e.preventDefault();
      for (const handler of this.contextLostHandlers) {
        handler();
      }
    });

    this.canvas.addEventListener("webglcontextrestored", () => {
      for (const handler of this.contextRestoredHandlers) {
        handler();
      }
    });
  }

  public onContextLost(handler: () => void): void {
    this.contextLostHandlers.push(handler);
  }

  public onContextRestored(handler: () => void): void {
    this.contextRestoredHandlers.push(handler);
  }

  /**
   * Resize canvas AND viewport.
   */
  public resize(width: number, height: number): void {
    this.canvas.width = width;
    this.canvas.height = height;
    this.gl.viewport(0, 0, width, height);
  }
}
