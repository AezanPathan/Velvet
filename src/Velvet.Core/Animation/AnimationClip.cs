namespace Velvet.Core.Animation;

/// <summary>
/// An animation clip is a named collection of channels that animate different node properties over time.
/// All channels in a clip can be played together.
/// 
/// This is analogous to Three.js AnimationClip or glTF 2.0 animation.
/// The clip is immutable; channels are provided at construction.
/// </summary>
public sealed class AnimationClip
{
    private readonly List<AnimationChannel> _channels;

    /// <summary>
    /// Creates a new animation clip with the given channels.
    /// </summary>
    /// <param name="name">Human-readable name for the clip (e.g., "Walk", "Run", "Idle").</param>
    /// <param name="channels">Channels animating different node properties.</param>
    public AnimationClip(string name, IEnumerable<AnimationChannel> channels)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(channels);
        
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Clip name must not be whitespace.", nameof(name));
        }

        Name = name;
        _channels = new List<AnimationChannel>(channels);
    }

    /// <summary>
    /// Human-readable name for this animation clip.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Channels in this clip. Each channel animates a specific node property.
    /// Immutable snapshot.
    /// </summary>
    public IReadOnlyList<AnimationChannel> Channels => _channels.AsReadOnly();

    /// <summary>
    /// Total duration of this clip (maximum duration of all contained channels' samplers).
    /// </summary>
    public float Duration
    {
        get
        {
            float maxDuration = 0f;
            foreach (var channel in _channels)
            {
                var samplerDuration = channel.Sampler.Duration;
                if (samplerDuration > maxDuration)
                {
                    maxDuration = samplerDuration;
                }
            }
            return maxDuration;
        }
    }
}
