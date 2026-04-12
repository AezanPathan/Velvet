/**
 * WebGLContext
 * ------------
 * A modern engine-level wrapper around WebGL2RenderingContext.
 *
 * Responsibilities:
 *  - Initialize WebGL2 (fallback to WebGL1)
 *  - Handle context loss + restore
 *  - Store canvas reference
 *  - Load extensions
 *  - Expose capabilities
 *  - Provide the renderer with a stable GPU context
 */
export class WebGLContext {
  public readonly canvas: HTMLCanvasElement;
  public readonly gl: WebGL2RenderingContext;

  /** True if running WebGL2, false if fallback WebGL1 is used */
  public readonly isWebGL2: boolean = true;

  /** GPU Capabilities (max textures, max attributes, etc.) */
  public readonly caps: {
    maxTextures: number;
    maxVertexAttribs: number;
    maxTextureSize: number;
  };

  private readonly contextLostHandlers: Array<() => void> = [];
  private readonly contextRestoredHandlers: Array<() => void> = [];

  constructor(canvas: HTMLCanvasElement) {
    this.canvas = canvas;

    // Try WebGL2 first
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
      // Fallback WebGL1 ONLY if absolutely needed
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

    // Load capabilities
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
