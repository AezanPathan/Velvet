import { ProgramManager } from "../core/resource/Managers";
import { GLProgram } from "../webgl/GLProgram";
import { ensureFloat32Array, getContext } from "./runtime";

export function setUniformMatrix4fv(programId: number, name: string, matrix: Float32Array): void {
  const context = getContext();
  const program = ProgramManager.get(programId) as GLProgram;
  const location = program.getUniformLocation(name);

  if (location) {
    program.use();
    context.gl.uniformMatrix4fv(location, false, ensureFloat32Array(matrix));
  }
}

export function setUniformMatrix3fv(programId: number, name: string, matrix: Float32Array): void {
  const context = getContext();
  const program = ProgramManager.get(programId) as GLProgram;
  const location = program.getUniformLocation(name);

  if (location) {
    program.use();
    context.gl.uniformMatrix3fv(location, false, ensureFloat32Array(matrix));
  }
}

export function setUniform3f(programId: number, name: string, x: number, y: number, z: number): void {
  const context = getContext();
  const program = ProgramManager.get(programId) as GLProgram;
  const location = program.getUniformLocation(name);

  if (location) {
    program.use();
    context.gl.uniform3f(location, x, y, z);
  }
}

export function setUniform1f(programId: number, name: string, value: number): void {
  const context = getContext();
  const program = ProgramManager.get(programId) as GLProgram;
  const location = program.getUniformLocation(name);

  if (location) {
    program.use();
    context.gl.uniform1f(location, value);
  }
}

export function setUniform1i(programId: number, name: string, value: number): void {
  const context = getContext();
  const program = ProgramManager.get(programId) as GLProgram;
  const location = program.getUniformLocation(name);

  if (location) {
    program.use();
    context.gl.uniform1i(location, value);
  }
}

export function setUniform1b(programId: number, name: string, value: boolean): void {
  const context = getContext();
  const program = ProgramManager.get(programId) as GLProgram;
  const location = program.getUniformLocation(name);

  if (location) {
    program.use();
    context.gl.uniform1i(location, value ? 1 : 0);
  }
}
