import { IMesh } from '../mesh/IMesh';
import { IProgram } from '../program/IProgram';

export interface IRenderer {
    clear(r: number, g: number, b: number, a: number): void;
    drawMesh(mesh: IMesh, program: IProgram): void;
    resize(width: number, height: number): void;
}
