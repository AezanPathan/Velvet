using System;

namespace Velvet.Core.Math;

public static class Mat4
{
    public static float[] Identity() =>
    [
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
    ];

    public static float[] RotateX(float angle)
    {
        var c = (float)System.Math.Cos(angle);
        var s = (float)System.Math.Sin(angle);

        // Column-major
        return
        [
            1, 0, 0, 0,
            0, c, s, 0,
            0, -s, c, 0,
            0, 0, 0, 1
        ];
    }

    public static float[] RotateY(float angle)
    {
        var c = (float)System.Math.Cos(angle);
        var s = (float)System.Math.Sin(angle);

        // Column-major
        return
        [
            c, 0, -s, 0,
            0, 1, 0, 0,
            s, 0, c, 0,
            0, 0, 0, 1
        ];
    }

    public static float[] Multiply(float[] a, float[] b)
    {
        if (a.Length != 16) throw new ArgumentException("Expected 4x4 matrix", nameof(a));
        if (b.Length != 16) throw new ArgumentException("Expected 4x4 matrix", nameof(b));

        var r = new float[16];
        for (var col = 0; col < 4; col++)
        {
            for (var row = 0; row < 4; row++)
            {
                r[row + col * 4] =
                    a[row + 0 * 4] * b[0 + col * 4] +
                    a[row + 1 * 4] * b[1 + col * 4] +
                    a[row + 2 * 4] * b[2 + col * 4] +
                    a[row + 3 * 4] * b[3 + col * 4];
            }
        }

        return r;
    }

    /// <summary>
    /// Builds a right-handed view matrix (OpenGL/WebGL convention) using column-major storage.
    /// </summary>
    public static float[] LookAt(in Vec3 eye, in Vec3 target, in Vec3 up)
    {
        var f = (target - eye).Normalized();
        var s = Vec3.Cross(f, up).Normalized();
        var u = Vec3.Cross(s, f);

        // Column-major layout; matches GLSL multiplication: clip = P * V * M * position.
        return
        [
            s.X, u.X, -f.X, 0,
            s.Y, u.Y, -f.Y, 0,
            s.Z, u.Z, -f.Z, 0,
            -Vec3.Dot(s, eye), -Vec3.Dot(u, eye), Vec3.Dot(f, eye), 1
        ];
    }

    /// <summary>
    /// Builds a right-handed perspective projection matrix with NDC Z in [-1, +1] (WebGL/OpenGL).
    /// Inputs are in radians.
    /// </summary>
    public static float[] Perspective(float fovYRadians, float aspectRatio, float nearPlane, float farPlane)
    {
        if (fovYRadians <= 0f || fovYRadians >= System.MathF.PI)
            throw new ArgumentOutOfRangeException(nameof(fovYRadians), "FOV must be in (0, PI) radians.");
        if (aspectRatio <= 0f)
            throw new ArgumentOutOfRangeException(nameof(aspectRatio), "Aspect ratio must be > 0.");
        if (nearPlane <= 0f)
            throw new ArgumentOutOfRangeException(nameof(nearPlane), "Near plane must be > 0.");
        if (farPlane <= nearPlane)
            throw new ArgumentOutOfRangeException(nameof(farPlane), "Far plane must be > near plane.");

        var f = 1.0f / System.MathF.Tan(fovYRadians * 0.5f);
        var nf = 1.0f / (nearPlane - farPlane);

        return
        [
            f / aspectRatio, 0, 0, 0,
            0, f, 0, 0,
            0, 0, (farPlane + nearPlane) * nf, -1,
            0, 0, (2 * farPlane * nearPlane) * nf, 0
        ];
    }
}
