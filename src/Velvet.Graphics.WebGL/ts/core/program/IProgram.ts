import { IShader } from '../shaders/IShader';

export interface IProgram {
    id: number;
    attachShader(shader: IShader): void;
    link(): void;
    use(): void;
    isLinked(): boolean;
    delete(): void;
}
