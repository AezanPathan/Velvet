namespace Velvet.Core.Math;

public readonly struct Vector3
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3 Zero => new(0, 0, 0);
    public static Vector3 UnitY => new(0, 1, 0);

    public float LengthSquared => X * X + Y * Y + Z * Z;
    public float Length => System.MathF.Sqrt(LengthSquared);

    public Vector3 Normalized()
    {
        var lenSq = LengthSquared;
        if (lenSq <= 0f) throw new System.InvalidOperationException("Cannot normalize a zero-length vector.");

        var invLen = 1.0f / System.MathF.Sqrt(lenSq);
        return new Vector3(X * invLen, Y * invLen, Z * invLen);
    }

    public static float Dot(in Vector3 a, in Vector3 b)
        => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vector3 Cross(in Vector3 a, in Vector3 b)
        => new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

    public static Vector3 operator +(in Vector3 a, in Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(in Vector3 a, in Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator *(in Vector3 v, float s) => new(v.X * s, v.Y * s, v.Z * s);
    public static Vector3 operator *(float s, in Vector3 v) => v * s;
}
