export class Transform2D {
    public position: { x: number; y: number } = { x: 0, y: 0 };
    public rotation: number = 0;
    public scale: { x: number; y: number } = { x: 1, y: 1 };

    public getMatrix(): Float32Array {
        const cos = Math.cos(this.rotation);
        const sin = Math.sin(this.rotation);

        // Translation * Rotation * Scale
        const matrix = new Float32Array(16);
        matrix[0] = cos * this.scale.x; // m00
        matrix[1] = sin * this.scale.x; // m01
        matrix[4] = -sin * this.scale.y; // m10
        matrix[5] = cos * this.scale.y; // m11
        matrix[12] = this.position.x; // m30
        matrix[13] = this.position.y; // m31
        matrix[15] = 1; // m33

        return matrix;
    }
}