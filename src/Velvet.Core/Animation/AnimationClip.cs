namespace Velvet.Core.Animation;

public sealed class AnimationClip
{
    private readonly List<AnimationChannel> _channels;

    public AnimationClip(string name, IEnumerable<AnimationChannel> channels)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(channels);
        
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Clip name must not be whitespace.", nameof(name));
        }

        Name = name;
        _channels = [.. channels];
    }

    public string Name { get; }

    public IReadOnlyList<AnimationChannel> Channels => _channels;

    public float Duration
    {
        get
        {
            float maxDuration = 0f;
            foreach (var channel in _channels)
            {
                var samplerDuration = channel.Sampler.Duration;
                if (samplerDuration > maxDuration)
                    maxDuration = samplerDuration;
                
            }
            return maxDuration;
        }
    }
}
