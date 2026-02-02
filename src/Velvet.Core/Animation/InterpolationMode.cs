namespace Velvet.Core.Animation;

/// <summary>
/// Interpolation mode between keyframe values.
/// Per glTF 2.0 spec, these are the only three supported modes.
/// </summary>
public enum InterpolationMode
{
    /// <summary>
    /// Step function: hold the input tangent value until the next keyframe.
    /// </summary>
    Step,

    /// <summary>
    /// Linear interpolation: linear blend between successive output values.
    /// </summary>
    Linear,

    /// <summary>
    /// Cubic spline interpolation with in/out tangents.
    /// Each output value has associated in-tangent and out-tangent for smooth curves.
    /// </summary>
    CubicSpline
}
