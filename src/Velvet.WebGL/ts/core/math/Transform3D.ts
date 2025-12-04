export class Transform3D {
    public position: { x: number; y: number; z: number } = { x: 0, y: 0, z: 0 };
    public rotation: { x: number; y: number; z: number } = { x: 0, y: 0, z: 0 };
    public scale: { x: number; y: number; z: number } = { x: 1, y: 1, z: 1 };

    public getMatrix(): Float32Array {
        // Helper function to multiply two 4x4 matrices
        const multiply = (a: Float32Array, b: Float32Array): Float32Array => {
            const result = new Float32Array(16);
            for (let i = 0; i < 4; i++) {
                for (let j = 0; j < 4; j++) {
                    result[i * 4 + j] = 0;
                    for (let k = 0; k < 4; k++) {
                        result[i * 4 + j] += a[i * 4 + k] * b[k * 4 + j];
                    }
                }
            }
            return result;
        };

        // Start with scale matrix
        let matrix = new Float32Array([
            this.scale.x, 0, 0, 0,
            0, this.scale.y, 0, 0,
            0, 0, this.scale.z, 0,
            0, 0, 0, 1
        ]);

        // Apply rotation X
        const rx = new Float32Array([
            1, 0, 0, 0,
            0, Math.cos(this.rotation.x), -Math.sin(this.rotation.x), 0,
            0, Math.sin(this.rotation.x), Math.cos(this.rotation.x), 0,
            0, 0, 0, 1
        ]);
        // @ts-ignore
        matrix = multiply(rx, matrix);

        // Apply rotation Y
        const ry = new Float32Array([
            Math.cos(this.rotation.y), 0, Math.sin(this.rotation.y), 0,
            0, 1, 0, 0,
            -Math.sin(this.rotation.y), 0, Math.cos(this.rotation.y), 0,
            0, 0, 0, 1
        ]);
        // @ts-ignore
        matrix = multiply(ry, matrix);

        // Apply rotation Z
        const rz = new Float32Array([
            Math.cos(this.rotation.z), -Math.sin(this.rotation.z), 0, 0,
            Math.sin(this.rotation.z), Math.cos(this.rotation.z), 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        ]);
        // @ts-ignore
        matrix = multiply(rz, matrix);

        // Apply translation
        const t = new Float32Array([
            1, 0, 0, this.position.x,
            0, 1, 0, this.position.y,
            0, 0, 1, this.position.z,
            0, 0, 0, 1
        ]);
        // @ts-ignore
        matrix = multiply(t, matrix);

        return matrix;
    }
}