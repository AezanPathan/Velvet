namespace Velvet.Core.Animation;

/// <summary>
/// An animation channel connects a sampler to a specific node's property.
/// Per glTF 2.0 spec, a channel specifies which node's which property to animate
/// and references a sampler that provides the keyframe data.
/// 
/// Multiple channels can reference the same sampler (output sharing).
/// A node may have multiple channels for different properties (e.g., translation + rotation).
/// </summary>
public sealed class AnimationChannel
{
    /// <summary>
    /// Creates a new animation channel.
    /// </summary>
    /// <param name="sampler">The sampler providing keyframe data.</param>
    /// <param name="targetNodeName">Name of the target node in the scene graph.</param>
    /// <param name="path">Which property of the node to animate (Translation, Rotation, Scale).</param>
    public AnimationChannel(AnimationSampler sampler, string targetNodeName, AnimationPath path)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        ArgumentNullException.ThrowIfNull(targetNodeName);
        if (string.IsNullOrWhiteSpace(targetNodeName))
        {
            throw new ArgumentException("Target node name must not be whitespace.", nameof(targetNodeName));
        }

        Sampler = sampler;
        TargetNodeName = targetNodeName;
        Path = path;
    }

    /// <summary>
    /// The sampler providing keyframe data for this channel.
    /// </summary>
    public AnimationSampler Sampler { get; }

    /// <summary>
    /// Name of the target node in the scene graph.
    /// The Animator will resolve this name to locate the actual node.
    /// </summary>
    public string TargetNodeName { get; }

    /// <summary>
    /// Which property of the target node is being animated.
    /// </summary>
    public AnimationPath Path { get; }
}
