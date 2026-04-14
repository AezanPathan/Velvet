namespace Velvet.Core.Math;

/// <summary>
/// Minimal quaternion for node rotations.
/// </summary>
public readonly struct Quaternion
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;
    public readonly float W;

    public Quaternion(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static Quaternion Identity => new(0f, 0f, 0f, 1f);

    public Quaternion Normalized()
    {
        var lenSq = X * X + Y * Y + Z * Z + W * W;
        if (lenSq <= 0f) return Identity;
        

        var invLen = 1f / MathF.Sqrt(lenSq);
        return new Quaternion(X * invLen, Y * invLen, Z * invLen, W * invLen);
    }
}
