namespace Velvet.Core.Math;

public readonly struct Vec3
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;

    public Vec3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vec3 Zero => new(0, 0, 0);
    public static Vec3 UnitY => new(0, 1, 0);

    public float LengthSquared => X * X + Y * Y + Z * Z;
    public float Length => System.MathF.Sqrt(LengthSquared);

    public Vec3 Normalized()
    {
        var lenSq = LengthSquared;
        if (lenSq <= 0f) throw new System.InvalidOperationException("Cannot normalize a zero-length vector.");

        var invLen = 1.0f / System.MathF.Sqrt(lenSq);
        return new Vec3(X * invLen, Y * invLen, Z * invLen);
    }

    public static float Dot(in Vec3 a, in Vec3 b)
        => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vec3 Cross(in Vec3 a, in Vec3 b)
        => new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

    public static Vec3 operator +(in Vec3 a, in Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(in Vec3 a, in Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator *(in Vec3 v, float s) => new(v.X * s, v.Y * s, v.Z * s);
    public static Vec3 operator *(float s, in Vec3 v) => v * s;
}
