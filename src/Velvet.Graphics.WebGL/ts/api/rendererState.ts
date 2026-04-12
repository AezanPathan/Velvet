import { MeshManager, ProgramManager, RendererManager } from "../core/resource/Managers";
import { getContext } from "./runtime";

export function drawMesh(meshId: number, programId: number, rendererId: number): void {
  const mesh = MeshManager.get(meshId);
  const program = ProgramManager.get(programId);
  const renderer = RendererManager.get(rendererId);
  renderer.drawMesh(mesh, program);
}

export function setBlendMode(rendererId: number, mode: "off" | "alpha" | "additive"): void {
  const renderer = RendererManager.get(rendererId);
  renderer.setBlendMode(mode);
}

export function clear(rendererId: number, r: number, g: number, b: number, a: number): void {
  const renderer = RendererManager.get(rendererId);
  renderer.clear(r, g, b, a);
}

export function resize(width: number, height: number): void {
  const context = getContext();
  context.resize(width, height);
}

export function setDepthMask(rendererId: number, enabled: boolean): void {
  const renderer = RendererManager.get(rendererId);
  renderer.setDepthMask(enabled);
}
