export interface IBuffer {
    id: number;
    setData(data: Float32Array | Uint32Array): void;
    bind(): void;
    unbind(): void;
    delete(): void;
}
