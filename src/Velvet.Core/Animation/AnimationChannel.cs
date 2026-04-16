namespace Velvet.Core.Animation;

public sealed class AnimationChannel
{
    public AnimationChannel(AnimationSampler sampler, string targetNodeName, AnimationProperty path)
    {
        ArgumentNullException.ThrowIfNull(sampler);
        ArgumentNullException.ThrowIfNull(targetNodeName);
        if (string.IsNullOrWhiteSpace(targetNodeName))
            throw new ArgumentException("Target node name must not be whitespace.", nameof(targetNodeName));


        Sampler = sampler;
        TargetNodeName = targetNodeName;
        Path = path;
    }


    public AnimationSampler Sampler { get; }

    public string TargetNodeName { get; }

    public AnimationProperty Path { get; }
}
