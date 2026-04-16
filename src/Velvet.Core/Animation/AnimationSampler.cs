namespace Velvet.Core.Animation;

public sealed class AnimationSampler
{
    private readonly List<AnimationKeyframe> _keyframes;

    public AnimationSampler(IEnumerable<AnimationKeyframe> keyframes, InterpolationMode interpolation)
    {
        ArgumentNullException.ThrowIfNull(keyframes);

        _keyframes = [.. keyframes];

        if (_keyframes.Count == 0)
            throw new ArgumentException("Sampler must have at least one keyframe.", nameof(keyframes));

        for (int i = 1; i < _keyframes.Count; i++)
        {
            if (_keyframes[i].Time < _keyframes[i - 1].Time)
                throw new ArgumentException("Keyframes must be sorted by time.", nameof(keyframes));
        }

        Interpolation = interpolation;
    }

    public InterpolationMode Interpolation { get; }

    public IReadOnlyList<AnimationKeyframe> Keyframes => _keyframes;

    public float Duration => _keyframes[^1].Time;
}