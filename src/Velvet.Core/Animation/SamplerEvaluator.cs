namespace Velvet.Core.Animation;

using Velvet.Core.Math;

public static class SamplerEvaluator
{
    public static float[] Evaluate(AnimationSampler sampler, float time)
    {
        ArgumentNullException.ThrowIfNull(sampler);

        var keyframes = sampler.Keyframes;

        if (keyframes.Count == 0)
            throw new InvalidOperationException("Sampler has no keyframes.");

        float t = MathF.Max(0f, MathF.Min(time, sampler.Duration));

        int prevIndex = FindPreviousKeyframeIndex(keyframes, t);
        int nextIndex = prevIndex + 1;

        if (nextIndex >= keyframes.Count)
            return (float[])keyframes[prevIndex].Values.Clone();

        var prev = keyframes[prevIndex];
        var next = keyframes[nextIndex];

        return sampler.Interpolation switch
        {
            InterpolationMode.Step => EvaluateStep(prev, next, t),
            InterpolationMode.Linear => EvaluateLinear(prev, next, t),
            InterpolationMode.CubicSpline => EvaluateCubicSpline(prev, next, t),
            _ => throw new InvalidOperationException()
        };
    }

    private static int FindPreviousKeyframeIndex(IReadOnlyList<AnimationKeyframe> keyframes, float time)
    {
        int left = 0;
        int right = keyframes.Count - 1;

        while (left < right)
        {
            int mid = (left + right + 1) / 2;

            if (keyframes[mid].Time <= time)
                left = mid;
            else
                right = mid - 1;
        }

        return left;
    }

    private static float[] EvaluateStep(AnimationKeyframe prev, AnimationKeyframe next, float time)
    {
        return time < next.Time
            ? (float[])prev.Values.Clone()
            : (float[])next.Values.Clone();
    }

    private static float[] EvaluateLinear(AnimationKeyframe prev, AnimationKeyframe next, float time)
    {
        float t0 = prev.Time;
        float t1 = next.Time;
        float dt = t1 - t0;

        if (dt <= 0f)
            return (float[])next.Values.Clone();

        float u = (time - t0) / dt;

        var a = prev.Values;
        var b = next.Values;

        if (a.Length != b.Length)
            throw new InvalidOperationException("Keyframe value size mismatch.");

        float[] result = new float[a.Length];

        for (int i = 0; i < result.Length; i++)
            result[i] = a[i] * (1f - u) + b[i] * u;

        return result;
    }

    private static float[] EvaluateCubicSpline(AnimationKeyframe prev, AnimationKeyframe next, float time)
    {
        float t0 = prev.Time;
        float t1 = next.Time;
        float dt = t1 - t0;

        if (dt <= 0f)
            return (float[])next.Values.Clone();

        float u = (time - t0) / dt;

        var a = prev.Values;
        var b = next.Values;

        if (a.Length != b.Length || a.Length % 3 != 0)
            throw new InvalidOperationException("Invalid cubic spline data.");

        int size = a.Length / 3;

        float[] result = new float[size];

        float[] p0 = new float[size];
        float[] m0 = new float[size];
        float[] p1 = new float[size];
        float[] m1 = new float[size];

        Array.Copy(a, size, p0, 0, size);
        Array.Copy(a, size * 2, m0, 0, size);
        Array.Copy(b, size, p1, 0, size);
        Array.Copy(b, 0, m1, 0, size);

        float h00 = 2f * u * u * u - 3f * u * u + 1f;
        float h10 = u * u * u - 2f * u * u + u;
        float h01 = -2f * u * u * u + 3f * u * u;
        float h11 = u * u * u - u * u;

        for (int i = 0; i < size; i++)
        {
            result[i] =
                h00 * p0[i] +
                h10 * dt * m0[i] +
                h01 * p1[i] +
                h11 * dt * m1[i];
        }

        return result;
    }
}