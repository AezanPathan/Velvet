namespace Velvet.Core.Math;

/// <summary>
/// Lightweight wrapper over a column-major 4x4 matrix.
/// </summary>
public readonly struct Matrix4
{
    private readonly float[] _data;

    public float[] Data => _data ?? throw new InvalidOperationException("Matrix not initialized.");

    public Matrix4(float[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length != 16)
            throw new ArgumentException("Matrix4 requires 16 elements.", nameof(data));

        _data = data;
    }

    public static implicit operator Matrix4(float[] data) => new(data);
    public static implicit operator float[](Matrix4 m) => m.Data;

    public static Matrix4 Identity => new(Matrix.Identity());

    public static Matrix4 Multiply(in Matrix4 a, in Matrix4 b)
        => new(Matrix.Multiply(a.Data, b.Data));

    public static Matrix4 Trs(in Vector3 t, in Quaternion r, in Vector3 s)
        => new(Matrix.Trs(t, r, s));

    public static Matrix4 LookAt(in Vector3 eye, in Vector3 target, in Vector3 up)
        => new(Matrix.LookAt(eye, target, up));

    public static Matrix4 Perspective(float fov, float aspect, float near, float far)
        => new(Matrix.Perspective(fov, aspect, near, far));

    public static float[] NormalMatrix(in Matrix4 m)
        => Matrix.NormalMatrix(m.Data);
}