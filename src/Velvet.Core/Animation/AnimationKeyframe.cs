namespace Velvet.Core.Animation;

public sealed class AnimationKeyframe
{
    public AnimationKeyframe(float time, float[] values)
    {
        if (time < 0f)
            throw new ArgumentOutOfRangeException(nameof(time));

        ArgumentNullException.ThrowIfNull(values);

        if (values.Length == 0)
            throw new ArgumentException("Values cannot be empty.", nameof(values));

        Time = time;
        Values = (float[])values.Clone();
    }

    public float Time { get; }

    public float[] Values { get; }
}