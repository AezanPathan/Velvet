namespace Velvet.Core.Animation;

/// <summary>
/// An animation sampler holds keyframe data and interpolation mode for a single track.
/// Per glTF 2.0 spec, a sampler is referenced by one or more channels (node + animation path pairs).
/// 
/// This class is immutable: keyframes are provided at construction and snapshot-cloned to prevent external mutation.
/// </summary>
public sealed class AnimationSampler
{
    private readonly List<AnimationKeyframe> _keyframes;

    /// <summary>
    /// Creates a new animation sampler with the given keyframes and interpolation mode.
    /// Keyframes must be sorted by time in ascending order.
    /// </summary>
    public AnimationSampler(IEnumerable<AnimationKeyframe> keyframes, InterpolationMode interpolation)
    {
        ArgumentNullException.ThrowIfNull(keyframes);

        _keyframes = new List<AnimationKeyframe>(keyframes);

        // Validate temporal ordering
        for (int i = 1; i < _keyframes.Count; i++)
        {
            if (_keyframes[i].Time < _keyframes[i - 1].Time)
            {
                throw new ArgumentException(
                    $"Keyframes must be sorted by time. Keyframe at index {i} (time={_keyframes[i].Time}) " +
                    $"is before keyframe at index {i - 1} (time={_keyframes[i - 1].Time}).",
                    nameof(keyframes));
            }
        }

        if (_keyframes.Count == 0)
        {
            throw new ArgumentException("Sampler must have at least one keyframe.", nameof(keyframes));
        }

        Interpolation = interpolation;
    }

    /// <summary>
    /// The interpolation mode between keyframes.
    /// </summary>
    public InterpolationMode Interpolation { get; }

    /// <summary>
    /// Keyframes in temporal order. Immutable snapshot.
    /// </summary>
    public IReadOnlyList<AnimationKeyframe> Keyframes => _keyframes.AsReadOnly();

    /// <summary>
    /// Total duration of this sampler (time of last keyframe).
    /// </summary>
    public float Duration => _keyframes.Count > 0 ? _keyframes[_keyframes.Count - 1].Time : 0f;
}
