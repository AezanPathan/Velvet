import { IBuffer } from '../buffers/IBuffer';

export interface IMesh {
    id: number;
    vertexBuffer: IBuffer;
    indexBuffer?: IBuffer;
    draw(): void;
    delete(): void;
}
