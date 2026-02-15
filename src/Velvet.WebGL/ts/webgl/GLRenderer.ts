// import { WebGLContext } from './WebGLContext';
// import { Program } from './WebGLProgram';
// import { WebGLMesh } from './WebGLMesh';

// export class WebGLRenderer {
//     private readonly context: WebGLContext;
//     private program: Program | null = null;
//     private mesh: WebGLMesh | null = null;
//     private uModelLocation: WebGLUniformLocation | null = null;
//     private currentModelMatrix: Float32Array = new Float32Array([
//         1, 0, 0, 0,
//         0, 1, 0, 0,
//         0, 0, 1, 0,
//         0, 0, 0, 1
//     ]);

//     constructor(context: WebGLContext) {
//         this.context = context;
//     }

//     public getContext(): WebGLContext {
//         return this.context;
//     }

//     public initialize(vertexSource: string, fragmentSource: string): void {
//         const gl = this.context.getContext();
        
//         this.program = new Program(gl, vertexSource, fragmentSource);
//         this.uModelLocation = this.program.getUniformLocation("uModel");
//     }

//     public setMesh(mesh: WebGLMesh): void {
//         this.mesh = mesh;
//     }

//     public setModelMatrix(matrix: Float32Array): void {
//         this.currentModelMatrix = matrix;
//     }

//     public drawMesh(): void {
//         if (!this.program || !this.mesh) {
//             throw new Error('Renderer not initialized or mesh not set');
//         }

//         const gl = this.context.getContext();
//         const canvas = this.context.getCanvas();

//         // Setup viewport and clear
//         gl.viewport(0, 0, canvas.width, canvas.height);
//         gl.clearColor(0.1, 0.1, 0.1, 1.0);
//         gl.clear(gl.COLOR_BUFFER_BIT);

//         // Bind the mesh
//         this.mesh.bind();

//         // Bind the shader program
//         this.program.use();

//         // Set model matrix uniform
//         gl.uniformMatrix4fv(this.uModelLocation, false, this.currentModelMatrix);

//         // Enable the position attribute
//         const positionLocation = this.program.getAttribLocation('position');
//         gl.enableVertexAttribArray(positionLocation);
//         gl.vertexAttribPointer(positionLocation, 3, gl.FLOAT, false, 24, 0);

//         // Enable the color attribute
//         const colorLocation = this.program.getAttribLocation('color');
//         gl.enableVertexAttribArray(colorLocation);
//         gl.vertexAttribPointer(colorLocation, 3, gl.FLOAT, false, 24, 12);

//         // Draw
//         gl.drawElements(gl.TRIANGLES, this.mesh.indexCount, gl.UNSIGNED_SHORT, 0);
//     }
// }
import { BlendMode, IRenderer } from "../core/renderer/IRenderer";
import { IMesh } from "../core/mesh/IMesh";
import { IProgram } from "../core/program/IProgram";

/**
 * GLRenderer
 * ----------
 * Backend implementation of IRenderer for WebGL2.
 *
 * Responsibilities:
 *  - Manage the WebGL2RenderingContext
 *  - Clear screen
 *  - Resize viewport
 *  - Bind a program and draw meshes
 *
 * This is the central component used by VelvetAPI for all draw operations.
 */
export class GLRenderer implements IRenderer {
  public readonly gl: WebGL2RenderingContext;

  /** Unique Velvet renderer ID (not used yet, for future resource management) */
  public readonly id: number;

  constructor(gl: WebGL2RenderingContext, id: number) {
    this.gl = gl;
    this.id = id;

    // Enable commonly used WebGL states
    this.gl.enable(this.gl.DEPTH_TEST);
    this.gl.depthFunc(this.gl.LEQUAL);

    this.gl.enable(this.gl.CULL_FACE);
    this.gl.cullFace(this.gl.BACK);
  }

  /**
   * Clears the framebuffer with a given color.
   */
  public clear(r: number, g: number, b: number, a: number): void {
    const gl = this.gl;
    gl.clearColor(r, g, b, a);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
  }

  /**
   * Resizes the viewport to match the canvas.
   */
  public resize(width: number, height: number): void {
    this.gl.viewport(0, 0, width, height);
  }

  public setBlendMode(mode: BlendMode): void {
    const gl = this.gl;

    if (mode === "off") {
      gl.disable(gl.BLEND);
      return;
    }

    gl.enable(gl.BLEND);
    if (mode === "additive") {
      gl.blendFunc(gl.SRC_ALPHA, gl.ONE);
    } else {
      gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
    }
  }

  /**
   * Draws a mesh with a specific program.
   */
  public drawMesh(mesh: IMesh, program: IProgram): void {
    const gl = this.gl;

    // Ensure program is ready
    if (!program.isLinked()) {
      throw new Error(`GLRenderer.drawMesh: program (id=${program.id}) is not linked`);
    }

    // Activate program
    program.use();

    // Mesh handles buffer binding + attribute setup internally
    mesh.draw();
  }
}
