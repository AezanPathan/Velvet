namespace Velvet.Core.Animation;

/// <summary>
/// Identifies which property of a node is being animated.
/// Per glTF 2.0 spec: translation, rotation, scale, or weights (for morph targets).
/// We focus on TRS for now.
/// </summary>
public enum AnimationPath
{
    /// <summary>Node's local translation (Vec3)</summary>
    Translation,

    /// <summary>Node's local rotation (Quaternion)</summary>
    Rotation,

    /// <summary>Node's local scale (Vec3)</summary>
    Scale,

    /// <summary>Morph target weights (not implemented)</summary>
    Weights
}
