namespace Velvet.Core.Math;

/// <summary>
/// Stateless 4x4 matrix utilities for the engine math layer.
/// Column-major layout matches OpenGL/WebGL conventions while staying backend-agnostic; suitable for any renderer expecting column-major data.
/// Static form keeps this code allocation-free and hot-path friendly for real-time rendering.
/// </summary>
public static class Matrix
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

    /// <summary>
    /// Column-major 4x4 multiply (a * b) matching OpenGL/WebGL memory order; returns column-major output suitable for GPU uniform uploads.
    /// </summary>
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
    /// Builds a column-major TRS matrix (T * R * S) using OpenGL/WebGL layout.
    /// Quaternion is interpreted as (x, y, z, w) in glTF convention.
    /// </summary>
    public static float[] Trs(in Vector3 translation, in Quaternion rotation, in Vector3 scale)
    {
        // Normalize to avoid accumulating scale into rotation.
        var q = rotation.Normalized();

        var xx = q.X * q.X; var yy = q.Y * q.Y; var zz = q.Z * q.Z;
        var xy = q.X * q.Y; var xz = q.X * q.Z; var yz = q.Y * q.Z;
        var wx = q.W * q.X; var wy = q.W * q.Y; var wz = q.W * q.Z;

        // Rotation matrix in row-major form (standard quaternion formula):
        // [ 1-2(yy+zz)  2(xy-wz)   2(xz+wy) ]
        // [ 2(xy+wz)    1-2(xx+zz) 2(yz-wx) ]
        // [ 2(xz-wy)    2(yz+wx)   1-2(xx+yy) ]
        //
        // Converted to column-major for GPU upload:
        var m00 = 1f - 2f * (yy + zz);
        var m10 = 2f * (xy + wz);
        var m20 = 2f * (xz - wy);

        var m01 = 2f * (xy - wz);
        var m11 = 1f - 2f * (xx + zz);
        var m21 = 2f * (yz + wx);

        var m02 = 2f * (xz + wy);
        var m12 = 2f * (yz - wx);
        var m22 = 1f - 2f * (xx + yy);

        // Apply scale to each basis column and append translation in the last column.
        return
        [
            m00 * scale.X, m10 * scale.X, m20 * scale.X, 0f,
            m01 * scale.Y, m11 * scale.Y, m21 * scale.Y, 0f,
            m02 * scale.Z, m12 * scale.Z, m22 * scale.Z, 0f,
            translation.X, translation.Y, translation.Z, 1f
        ];
    }

    /// <summary>
    /// Compute the 3x3 normal matrix (inverse-transpose of the upper-left 3x3 of the model matrix).
    /// Returns a float[9] in column-major order suitable for GLSL mat3 uniform upload.
    /// </summary>
    public static float[] NormalMatrix(float[] m)
    {
        if (m.Length != 16) throw new ArgumentException("Expected 4x4 matrix", nameof(m));

        // Extract upper-left 3x3
        var a00 = m[0]; var a01 = m[4]; var a02 = m[8];
        var a10 = m[1]; var a11 = m[5]; var a12 = m[9];
        var a20 = m[2]; var a21 = m[6]; var a22 = m[10];

        // Compute inverse of 3x3
        var b01 =  a22 * a11 - a12 * a21;
        var b11 = -a22 * a10 + a12 * a20;
        var b21 =  a21 * a10 - a11 * a20;

        var det = a00 * b01 + a01 * b11 + a02 * b21;
        if (det == 0f)
        {
            // Fallback to identity
            return new float[] { 1,0,0, 0,1,0, 0,0,1 };
        }

        var invDet = 1f / det;

        var c00 = b01 * invDet;
        var c01 = (-a22 * a01 + a02 * a21) * invDet;
        var c02 = (a12 * a01 - a02 * a11) * invDet;

        var c10 = b11 * invDet;
        var c11 = (a22 * a00 - a02 * a20) * invDet;
        var c12 = (-a12 * a00 + a02 * a10) * invDet;

        var c20 = b21 * invDet;
        var c21 = (-a21 * a00 + a01 * a20) * invDet;
        var c22 = (a11 * a00 - a01 * a10) * invDet;

        // Transpose (inverse transpose)
        return new float[] {
            c00, c10, c20,
            c01, c11, c21,
            c02, c12, c22
        };
    }

    /// <summary>
    /// Builds a right-handed view matrix (OpenGL/WebGL convention) using column-major storage.
    /// </summary>
    public static float[] LookAt(in Vector3 eye, in Vector3 target, in Vector3 up)
    {
        var f = (target - eye).Normalized();
        var s = Vector3.Cross(f, up).Normalized();
        var u = Vector3.Cross(s, f);

        // Column-major layout; matches GLSL multiplication: clip = P * V * M * position.
        return
        [
            s.X, u.X, -f.X, 0,
            s.Y, u.Y, -f.Y, 0,
            s.Z, u.Z, -f.Z, 0,
            -Vector3.Dot(s, eye), -Vector3.Dot(u, eye), Vector3.Dot(f, eye), 1
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
