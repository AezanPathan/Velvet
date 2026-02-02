namespace Velvet.Core.Animation;

/// <summary>
/// A keyframe in an animation sampler's timeline.
/// Stores time input and corresponding output value(s).
/// 
/// For cubic spline interpolation, the output structure is:
/// [in_tangent, value, out_tangent] where each is a scalar or vector
/// depending on the animation path (Translation/Scale = Vec3, Rotation = Vec4).
/// 
/// For step and linear, only the value is used.
/// </summary>
public sealed class AnimationKeyframe
{
    /// <summary>
    /// Time in seconds at which this keyframe occurs.
    /// Must be >= 0 and monotonically increasing within a sampler.
    /// </summary>
    public float Time { get; }

    /// <summary>
    /// Raw output values associated with this keyframe.
    /// Length depends on interpolation mode and animation path:
    /// 
    /// Step/Linear:
    ///   - Translation/Scale: 3 floats (Vec3)
    ///   - Rotation: 4 floats (Quaternion)
    /// 
    /// CubicSpline:
    ///   - Translation/Scale: 9 floats (in_tangent[3] + value[3] + out_tangent[3])
    ///   - Rotation: 12 floats (in_tangent[4] + value[4] + out_tangent[4])
    /// </summary>
    public float[] Values { get; }

    public AnimationKeyframe(float time, float[] values)
    {
        if (time < 0f) throw new ArgumentException("Keyframe time must be non-negative.", nameof(time));
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0) throw new ArgumentException("Keyframe values must not be empty.", nameof(values));

        Time = time;
        Values = (float[])values.Clone();
    }
}
