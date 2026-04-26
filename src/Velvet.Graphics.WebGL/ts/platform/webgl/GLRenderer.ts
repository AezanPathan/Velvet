import { BlendMode, IRenderer } from "../../core/renderer/IRenderer";
import { IMesh } from "../../core/mesh/IMesh";
import { IProgram } from "../../core/program/IProgram";

/**
 * Core renderer.
 *
 * Responsible for:
 * - GPU state setup
 * - clearing
 * - draw execution
 *
 * Does NOT manage scene logic.
 */

export class GLRenderer implements IRenderer {
  public readonly gl: WebGL2RenderingContext;

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

  public clear(r: number, g: number, b: number, a: number): void {
    const gl = this.gl;
    gl.enable(gl.DEPTH_TEST);
    gl.depthFunc(gl.LEQUAL);
    gl.depthMask(true);
    gl.clearColor(r, g, b, a);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
  }

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

  public setDepthMask(enabled: boolean): void {
    this.gl.depthMask(enabled);
  }

  public drawMesh(mesh: IMesh, program: IProgram): void {
    const gl = this.gl;

    if (!program.isLinked()) {
      throw new Error(`GLRenderer.drawMesh: program (id=${program.id}) is not linked`);
    }

    program.use();
    mesh.draw();
  }
}
