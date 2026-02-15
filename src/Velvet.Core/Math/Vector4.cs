namespace Velvet.Core.Math;

/// <summary>
/// Immutable 4D vector used for colors (RGBA) and general math.
/// </summary>
public readonly struct Vector4
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;
    public readonly float W;

    public Vector4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static Vector4 Zero => new(0, 0, 0, 0);
    public static Vector4 One => new(1, 1, 1, 1);

    public static Vector4 operator +(in Vector4 a, in Vector4 b)
        => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);

    public static Vector4 operator -(in Vector4 a, in Vector4 b)
        => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);

    public static Vector4 operator *(in Vector4 v, float s)
        => new(v.X * s, v.Y * s, v.Z * s, v.W * s);

    public static Vector4 operator *(float s, in Vector4 v) => v * s;
}
