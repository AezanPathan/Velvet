using Velvet.Core.Math;

namespace Velvet.Core.Animation;

/// <summary>Applies evaluated channel values to node local transforms.</summary>
internal static class AnimationApplier
{
    public static float[] ApplyChannel(
        AnimationChannel channel,
        float[] evaluatedValues,
        float[] currentLocalTransform)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(evaluatedValues);
        ArgumentNullException.ThrowIfNull(currentLocalTransform);

        if (currentLocalTransform.Length != 16)
            throw new ArgumentException("Local transform must be a 4x4 matrix (16 floats).", nameof(currentLocalTransform));

        return channel.Path switch
        {
            AnimationProperty.Translation => ApplyTranslation(evaluatedValues, currentLocalTransform),
            AnimationProperty.Rotation => ApplyRotation(evaluatedValues, currentLocalTransform),
            AnimationProperty.Scale => ApplyScale(evaluatedValues, currentLocalTransform),
            AnimationProperty.Weights => throw new NotSupportedException("Weights animation not yet supported."),
            _ => throw new InvalidOperationException($"Unknown animation path: {channel.Path}")
        };
    }

    private static float[] ApplyTranslation(float[] translationValues, float[] currentTransform)
    {
        if (translationValues.Length != 3)
            throw new ArgumentException($"Translation values must have exactly 3 components, got {translationValues.Length}.", nameof(translationValues));

        float[] result = (float[])currentTransform.Clone();

        // Column-major matrix layout.
        result[12] = translationValues[0];
        result[13] = translationValues[1];
        result[14] = translationValues[2];

        return result;
    }

    private static float[] ApplyRotation(float[] rotationValues, float[] currentTransform)
    {
        if (rotationValues.Length != 4)
            throw new ArgumentException($"Rotation values must have exactly 4 components (quaternion), got {rotationValues.Length}.", nameof(rotationValues));

        var quat = new Quaternion(rotationValues[0], rotationValues[1], rotationValues[2], rotationValues[3]).Normalized();
        var scale = ExtractScale(currentTransform);
        float tx = currentTransform[12];
        float ty = currentTransform[13];
        float tz = currentTransform[14];

        var rotMatrix = QuaternionToMatrix(quat, scale);
        rotMatrix[12] = tx;
        rotMatrix[13] = ty;
        rotMatrix[14] = tz;

        return rotMatrix;
    }

    private static float[] ApplyScale(float[] scaleValues, float[] currentTransform)
    {
        if (scaleValues.Length != 3)
            throw new ArgumentException($"Scale values must have exactly 3 components, got {scaleValues.Length}.", nameof(scaleValues));

        var rotation = ExtractRotation(currentTransform);
        float tx = currentTransform[12];
        float ty = currentTransform[13];
        float tz = currentTransform[14];

        var result = QuaternionToMatrix(rotation, new Vector3(scaleValues[0], scaleValues[1], scaleValues[2]));
        result[12] = tx;
        result[13] = ty;
        result[14] = tz;

        return result;
    }

    private static Vector3 ExtractScale(float[] matrix)
    {
        float sx = MathF.Sqrt(matrix[0] * matrix[0] + matrix[1] * matrix[1] + matrix[2] * matrix[2]);
        float sy = MathF.Sqrt(matrix[4] * matrix[4] + matrix[5] * matrix[5] + matrix[6] * matrix[6]);
        float sz = MathF.Sqrt(matrix[8] * matrix[8] + matrix[9] * matrix[9] + matrix[10] * matrix[10]);

        return new Vector3(sx, sy, sz);
    }

    private static Quaternion ExtractRotation(float[] matrix)
    {
        var scale = ExtractScale(matrix);
        
        float sx = scale.X > 0 ? scale.X : 1f;
        float sy = scale.Y > 0 ? scale.Y : 1f;
        float sz = scale.Z > 0 ? scale.Z : 1f;

        float m00 = matrix[0] / sx;
        float m10 = matrix[1] / sx;
        float m20 = matrix[2] / sx;

        float m01 = matrix[4] / sy;
        float m11 = matrix[5] / sy;
        float m21 = matrix[6] / sy;

        float m02 = matrix[8] / sz;
        float m12 = matrix[9] / sz;
        float m22 = matrix[10] / sz;

        float trace = m00 + m11 + m22;

        if (trace > 0)
        {
            float s = MathF.Sqrt(trace + 1f) * 2;
            float w = 0.25f * s;
            float x = (m21 - m12) / s;
            float y = (m02 - m20) / s;
            float z = (m10 - m01) / s;
            return new Quaternion(x, y, z, w).Normalized();
        }
        if (m00 > m11 && m00 > m22)
        {
            float s = MathF.Sqrt(1f + m00 - m11 - m22) * 2;
            float w = (m21 - m12) / s;
            float x = 0.25f * s;
            float y = (m01 + m10) / s;
            float z = (m02 + m20) / s;
            return new Quaternion(x, y, z, w).Normalized();
        }

        if (m11 > m22)
        {
            float s = MathF.Sqrt(1f + m11 - m00 - m22) * 2;
            float w = (m02 - m20) / s;
            float x = (m01 + m10) / s;
            float y = 0.25f * s;
            float z = (m12 + m21) / s;
            return new Quaternion(x, y, z, w).Normalized();
        }
        float sLast = MathF.Sqrt(1f + m22 - m00 - m11) * 2;
        float wLast = (m10 - m01) / sLast;
        float xLast = (m02 + m20) / sLast;
        float yLast = (m12 + m21) / sLast;
        float zLast = 0.25f * sLast;
        return new Quaternion(xLast, yLast, zLast, wLast).Normalized();
    }

    /// <summary>Builds a 4x4 rotation-scale matrix from quaternion and scale.</summary>
    private static float[] QuaternionToMatrix(Quaternion q, Vector3 scale)
    {
        float x = q.X;
        float y = q.Y;
        float z = q.Z;
        float w = q.W;

        float x2 = x + x;
        float y2 = y + y;
        float z2 = z + z;

        float xx = x * x2;
        float xy = x * y2;
        float xz = x * z2;
        float yy = y * y2;
        float yz = y * z2;
        float zz = z * z2;
        float wx = w * x2;
        float wy = w * y2;
        float wz = w * z2;

        float[] matrix =
        [
            (1f - (yy + zz)) * scale.X,
            (xy + wz) * scale.X,
            (xz - wy) * scale.X,
            0f,
            (xy - wz) * scale.Y,
            (1f - (xx + zz)) * scale.Y,
            (yz + wx) * scale.Y,
            0f,
            (xz + wy) * scale.Z,
            (yz - wx) * scale.Z,
            (1f - (xx + yy)) * scale.Z,
            0f,
            0f,
            0f,
            0f,
            1f,
        ];
        return matrix;
    }
}
