export interface IShader {
    id: number;
    compile(source: string, type: 'vertex' | 'fragment'): void;
    isValid(): boolean;
    delete(): void;
}
