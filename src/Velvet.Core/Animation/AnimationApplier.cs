using Velvet.Core.Engine;
using Velvet.Core.Math;

namespace Velvet.Core.Animation;

/// <summary>
/// Applies animation channel outputs to SceneNode local transforms.
/// Converts float arrays back into Vector3/Quaternion and builds updated local transforms.
/// 
/// This class bridges the generic sampler evaluation with semantic node property updates.
/// </summary>
internal static class AnimationApplier
{
    /// <summary>
    /// Applies an animation channel's evaluated output to a scene node's local transform.
    /// 
    /// Returns a new 4x4 local transform matrix reflecting the animation update.
    /// The input matrix is used as the base; only the relevant property (translation/rotation/scale)
    /// is updated based on the animation path and evaluated values.
    /// </summary>
    public static float[] ApplyChannel(
        AnimationChannel channel,
        float[] evaluatedValues,
        float[] currentLocalTransform)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(evaluatedValues);
        ArgumentNullException.ThrowIfNull(currentLocalTransform);

        if (currentLocalTransform.Length != 16)
        {
            throw new ArgumentException("Local transform must be a 4x4 matrix (16 floats).", nameof(currentLocalTransform));
        }

        return channel.Path switch
        {
            AnimationPath.Translation => ApplyTranslation(evaluatedValues, currentLocalTransform),
            AnimationPath.Rotation => ApplyRotation(evaluatedValues, currentLocalTransform),
            AnimationPath.Scale => ApplyScale(evaluatedValues, currentLocalTransform),
            AnimationPath.Weights => throw new NotSupportedException("Weights animation not yet supported."),
            _ => throw new InvalidOperationException($"Unknown animation path: {channel.Path}")
        };
    }

    /// <summary>
    /// Applies a translation update (3 floats) to the local transform.
    /// Replaces the translation component (right column) of the matrix.
    /// </summary>
    private static float[] ApplyTranslation(float[] translationValues, float[] currentTransform)
    {
        if (translationValues.Length != 3)
        {
            throw new ArgumentException($"Translation values must have exactly 3 components, got {translationValues.Length}.", nameof(translationValues));
        }

        float[] result = (float[])currentTransform.Clone();

        // Column-major layout: translation is at indices 12, 13, 14
        result[12] = translationValues[0];
        result[13] = translationValues[1];
        result[14] = translationValues[2];

        return result;
    }

    /// <summary>
    /// Applies a rotation update (4 floats as quaternion) to the local transform.
    /// Replaces the rotation portion of the matrix.
    /// </summary>
    private static float[] ApplyRotation(float[] rotationValues, float[] currentTransform)
    {
        if (rotationValues.Length != 4)
        {
            throw new ArgumentException($"Rotation values must have exactly 4 components (quaternion), got {rotationValues.Length}.", nameof(rotationValues));
        }

        // Create quaternion from evaluated values (x, y, z, w)
        var quat = new Quaternion(rotationValues[0], rotationValues[1], rotationValues[2], rotationValues[3]).Normalized();

        // Extract scale from current transform to preserve it
        var scale = ExtractScale(currentTransform);

        // Extract translation to preserve it
        float tx = currentTransform[12];
        float ty = currentTransform[13];
        float tz = currentTransform[14];

        // Build new transform: T * R * S (column-major)
        // First build rotation from quaternion (including scale)
        var rotMatrix = QuaternionToMatrix(quat, scale);

        // Apply translation
        rotMatrix[12] = tx;
        rotMatrix[13] = ty;
        rotMatrix[14] = tz;

        return rotMatrix;
    }

    /// <summary>
    /// Applies a scale update (3 floats) to the local transform.
    /// Replaces the scale component of the matrix.
    /// </summary>
    private static float[] ApplyScale(float[] scaleValues, float[] currentTransform)
    {
        if (scaleValues.Length != 3)
        {
            throw new ArgumentException($"Scale values must have exactly 3 components, got {scaleValues.Length}.", nameof(scaleValues));
        }

        // Extract current rotation and translation
        var rotation = ExtractRotation(currentTransform);
        float tx = currentTransform[12];
        float ty = currentTransform[13];
        float tz = currentTransform[14];

        // Build new transform with updated scale
        var result = QuaternionToMatrix(rotation, new Vector3(scaleValues[0], scaleValues[1], scaleValues[2]));
        result[12] = tx;
        result[13] = ty;
        result[14] = tz;

        return result;
    }

    /// <summary>
    /// Extracts scale factors from a 4x4 transformation matrix.
    /// Assumes the matrix is composed as T * R * S in column-major order.
    /// </summary>
    private static Vector3 ExtractScale(float[] matrix)
    {
        // Scale is the magnitude of each axis column (top-left 3x3)
        float sx = MathF.Sqrt(matrix[0] * matrix[0] + matrix[1] * matrix[1] + matrix[2] * matrix[2]);
        float sy = MathF.Sqrt(matrix[4] * matrix[4] + matrix[5] * matrix[5] + matrix[6] * matrix[6]);
        float sz = MathF.Sqrt(matrix[8] * matrix[8] + matrix[9] * matrix[9] + matrix[10] * matrix[10]);

        return new Vector3(sx, sy, sz);
    }

    /// <summary>
    /// Extracts rotation as a quaternion from a 4x4 matrix.
    /// Assumes the matrix is composed as T * R * S.
    /// </summary>
    private static Quaternion ExtractRotation(float[] matrix)
    {
        // Normalize the rotation part by removing scale
        var scale = ExtractScale(matrix);
        
        float sx = scale.X > 0 ? scale.X : 1f;
        float sy = scale.Y > 0 ? scale.Y : 1f;
        float sz = scale.Z > 0 ? scale.Z : 1f;

        // Extract the rotation matrix (remove scale)
        float m00 = matrix[0] / sx;
        float m10 = matrix[1] / sx;
        float m20 = matrix[2] / sx;

        float m01 = matrix[4] / sy;
        float m11 = matrix[5] / sy;
        float m21 = matrix[6] / sy;

        float m02 = matrix[8] / sz;
        float m12 = matrix[9] / sz;
        float m22 = matrix[10] / sz;

        // Convert rotation matrix to quaternion using Shepperd's method
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
        else if (m00 > m11 && m00 > m22)
        {
            float s = MathF.Sqrt(1f + m00 - m11 - m22) * 2;
            float w = (m21 - m12) / s;
            float x = 0.25f * s;
            float y = (m01 + m10) / s;
            float z = (m02 + m20) / s;
            return new Quaternion(x, y, z, w).Normalized();
        }
        else if (m11 > m22)
        {
            float s = MathF.Sqrt(1f + m11 - m00 - m22) * 2;
            float w = (m02 - m20) / s;
            float x = (m01 + m10) / s;
            float y = 0.25f * s;
            float z = (m12 + m21) / s;
            return new Quaternion(x, y, z, w).Normalized();
        }
        else
        {
            float s = MathF.Sqrt(1f + m22 - m00 - m11) * 2;
            float w = (m10 - m01) / s;
            float x = (m02 + m20) / s;
            float y = (m12 + m21) / s;
            float z = 0.25f * s;
            return new Quaternion(x, y, z, w).Normalized();
        }
    }

    /// <summary>
    /// Converts a quaternion and scale to a 4x4 rotation+scale matrix.
    /// Translation is left as identity (0, 0, 0); caller must set it.
    /// </summary>
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

        // Column-major layout with scale
        float[] matrix = new float[16];

        matrix[0] = (1f - (yy + zz)) * scale.X;
        matrix[1] = (xy + wz) * scale.X;
        matrix[2] = (xz - wy) * scale.X;
        matrix[3] = 0f;

        matrix[4] = (xy - wz) * scale.Y;
        matrix[5] = (1f - (xx + zz)) * scale.Y;
        matrix[6] = (yz + wx) * scale.Y;
        matrix[7] = 0f;

        matrix[8] = (xz + wy) * scale.Z;
        matrix[9] = (yz - wx) * scale.Z;
        matrix[10] = (1f - (xx + yy)) * scale.Z;
        matrix[11] = 0f;

        matrix[12] = 0f;
        matrix[13] = 0f;
        matrix[14] = 0f;
        matrix[15] = 1f;

        return matrix;
    }
}
