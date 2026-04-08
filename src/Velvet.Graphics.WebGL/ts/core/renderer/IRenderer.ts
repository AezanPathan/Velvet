import { IMesh } from '../mesh/IMesh';
import { IProgram } from '../program/IProgram';

export type BlendMode = "off" | "alpha" | "additive";

export interface IRenderer {
    clear(r: number, g: number, b: number, a: number): void;
    drawMesh(mesh: IMesh, program: IProgram): void;
    resize(width: number, height: number): void;
    setBlendMode(mode: BlendMode): void;
}
