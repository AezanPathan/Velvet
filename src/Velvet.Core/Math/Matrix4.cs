namespace Velvet.Core.Math;

/// <summary>
/// Immutable 4x4 matrix wrapper for the engine math layer.
/// Wraps a float[16] array in column-major layout, matching OpenGL/WebGL conventions.
/// Provides type safety for matrix operations while staying compatible with existing Matrix utilities.
/// </summary>
public readonly struct Matrix4
{
    private readonly float[] _data;

    /// <summary>
    /// Gets the underlying float array data in column-major order.
    /// </summary>
    public float[] Data => _data ?? throw new InvalidOperationException("Matrix4 has not been initialized.");

    /// <summary>
    /// Creates a new Matrix4 from a column-major float array.
    /// </summary>
    /// <param name="data">16-element float array in column-major order</param>
    /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
    /// <exception cref="ArgumentException">Thrown when data length is not 16</exception>
    public Matrix4(float[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length != 16)
            throw new ArgumentException("Matrix4 requires exactly 16 float elements.", nameof(data));

        _data = data;
    }

    /// <summary>
    /// Implicitly converts a float array to a Matrix4.
    /// </summary>
    public static implicit operator Matrix4(float[] data) => new Matrix4(data);

    /// <summary>
    /// Implicitly converts a Matrix4 to its underlying float array.
    /// </summary>
    public static implicit operator float[](Matrix4 matrix) => matrix.Data;

    /// <summary>
    /// Creates an identity matrix.
    /// </summary>
    public static Matrix4 Identity => new Matrix4(Matrix.Identity());

    /// <summary>
    /// Multiplies two 4x4 matrices and returns the result.
    /// </summary>
    public static Matrix4 Multiply(in Matrix4 a, in Matrix4 b) => new Matrix4(Matrix.Multiply(a.Data, b.Data));

    /// <summary>
    /// Multiplies two raw 4x4 matrices and returns a wrapped result.
    /// </summary>
    public static Matrix4 Multiply(float[] a, float[] b) => new Matrix4(Matrix.Multiply(a, b));

    /// <summary>
    /// Builds a TRS matrix (T * R * S).
    /// </summary>
    public static Matrix4 Trs(in Vector3 translation, in Quaternion rotation, in Vector3 scale)
        => new Matrix4(Matrix.Trs(translation, rotation, scale));

    /// <summary>
    /// Builds a right-handed view matrix.
    /// </summary>
    public static Matrix4 LookAt(in Vector3 eye, in Vector3 target, in Vector3 up)
        => new Matrix4(Matrix.LookAt(eye, target, up));

    /// <summary>
    /// Builds a right-handed perspective projection matrix.
    /// </summary>
    public static Matrix4 Perspective(float fovYRadians, float aspectRatio, float nearPlane, float farPlane)
        => new Matrix4(Matrix.Perspective(fovYRadians, aspectRatio, nearPlane, farPlane));

    /// <summary>
    /// Computes a normal matrix (inverse-transpose of upper-left 3x3).
    /// </summary>
    public static float[] NormalMatrix(in Matrix4 matrix)
        => Matrix.NormalMatrix(matrix.Data);
}
