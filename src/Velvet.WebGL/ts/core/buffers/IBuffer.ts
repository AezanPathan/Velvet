export interface IBuffer {
    id: number;
    setData(data: Float32Array | Uint16Array): void;
    bind(): void;
    unbind(): void;
    delete(): void;
}
