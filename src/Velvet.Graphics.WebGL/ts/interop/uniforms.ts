import { ensureFloat32Array, withOptionalUniformLocation } from "./runtime";

/**
 * Uniform helpers.
 * Automatically resolves uniform location and applies value.
 */

export function setUniformMatrix4fv(programId: number, name: string, matrix: Float32Array): void {
  withOptionalUniformLocation(programId, name, (gl, location) => {
    gl.uniformMatrix4fv(location, false, ensureFloat32Array(matrix));
  });
}

export function setUniformMatrix3fv(programId: number, name: string, matrix: Float32Array): void {
  withOptionalUniformLocation(programId, name, (gl, location) => {
    gl.uniformMatrix3fv(location, false, ensureFloat32Array(matrix));
  });
}

export function setUniform3f(programId: number, name: string, x: number, y: number, z: number): void {
  withOptionalUniformLocation(programId, name, (gl, location) => {
    gl.uniform3f(location, x, y, z);
  });
}

export function setUniform1f(programId: number, name: string, value: number): void {
  withOptionalUniformLocation(programId, name, (gl, location) => {
    gl.uniform1f(location, value);
  });
}

export function setUniform1i(programId: number, name: string, value: number): void {
  withOptionalUniformLocation(programId, name, (gl, location) => {
    gl.uniform1i(location, value);
  });
}

export function setUniform1b(programId: number, name: string, value: boolean): void {
  withOptionalUniformLocation(programId, name, (gl, location) => {
    gl.uniform1i(location, value ? 1 : 0);
  });
}
