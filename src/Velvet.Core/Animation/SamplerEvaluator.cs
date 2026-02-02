using Velvet.Core.Math;

namespace Velvet.Core.Animation;

/// <summary>
/// Evaluates animation samplers at a given time, producing interpolated output values.
/// Handles all three interpolation modes: Step, Linear, and CubicSpline.
/// 
/// Per glTF 2.0 spec:
/// - Step: hold the value of the first keyframe whose time is less than or equal to the input time
/// - Linear: linear interpolation between keyframes
/// - CubicSpline: cubic Hermite spline using in/out tangents from keyframe data
/// </summary>
public static class SamplerEvaluator
{
    /// <summary>
    /// Evaluates a sampler at the given time, returning the interpolated output.
    /// Returns a new float array containing the interpolated value(s).
    /// 
    /// Time is typically wrapped/clamped externally by the Animator.
    /// </summary>
    public static float[] Evaluate(AnimationSampler sampler, float time)
    {
        ArgumentNullException.ThrowIfNull(sampler);

        var keyframes = sampler.Keyframes;
        if (keyframes.Count == 0)
        {
            throw new InvalidOperationException("Cannot evaluate a sampler with no keyframes.");
        }

        // Clamp time to sampler duration
        float clampedTime = System.MathF.Max(0f, System.MathF.Min(time, sampler.Duration));

        // Find the two keyframes bracketing the time
        int prevIndex = FindPreviousKeyframeIndex(keyframes, clampedTime);
        int nextIndex = prevIndex + 1;

        // If time is exactly at a keyframe or before the first one
        if (nextIndex >= keyframes.Count)
        {
            return (float[])keyframes[prevIndex].Values.Clone();
        }

        var prevKeyframe = keyframes[prevIndex];
        var nextKeyframe = keyframes[nextIndex];

        return sampler.Interpolation switch
        {
            InterpolationMode.Step => EvaluateStep(prevKeyframe, nextKeyframe, clampedTime),
            InterpolationMode.Linear => EvaluateLinear(prevKeyframe, nextKeyframe, clampedTime),
            InterpolationMode.CubicSpline => EvaluateCubicSpline(prevKeyframe, nextKeyframe, clampedTime),
            _ => throw new InvalidOperationException($"Unknown interpolation mode: {sampler.Interpolation}")
        };
    }

    /// <summary>
    /// Finds the index of the keyframe with the highest time that is <= the query time.
    /// Returns 0 if time is before the first keyframe.
    /// </summary>
    private static int FindPreviousKeyframeIndex(IReadOnlyList<AnimationKeyframe> keyframes, float time)
    {
        // Binary search for efficiency on large keyframe sets
        int left = 0;
        int right = keyframes.Count - 1;

        while (left < right)
        {
            int mid = (left + right + 1) / 2;
            if (keyframes[mid].Time <= time)
            {
                left = mid;
            }
            else
            {
                right = mid - 1;
            }
        }

        return left;
    }

    /// <summary>
    /// Step function: returns the value of the previous keyframe.
    /// </summary>
    private static float[] EvaluateStep(
        AnimationKeyframe prevKeyframe,
        AnimationKeyframe nextKeyframe,
        float time)
    {
        // In Step mode, we hold the previous value until the next keyframe time is reached
        if (time < nextKeyframe.Time)
        {
            return (float[])prevKeyframe.Values.Clone();
        }
        else
        {
            return (float[])nextKeyframe.Values.Clone();
        }
    }

    /// <summary>
    /// Linear interpolation between two keyframes.
    /// </summary>
    private static float[] EvaluateLinear(
        AnimationKeyframe prevKeyframe,
        AnimationKeyframe nextKeyframe,
        float time)
    {
        float t0 = prevKeyframe.Time;
        float t1 = nextKeyframe.Time;
        float dt = t1 - t0;

        // Handle edge case where keyframes have the same time
        if (dt <= 0f)
        {
            return (float[])nextKeyframe.Values.Clone();
        }

        float u = (time - t0) / dt;

        var prevValues = prevKeyframe.Values;
        var nextValues = nextKeyframe.Values;

        // Both should have the same length
        if (prevValues.Length != nextValues.Length)
        {
            throw new InvalidOperationException(
                $"Keyframe values length mismatch: prev={prevValues.Length}, next={nextValues.Length}");
        }

        float[] result = new float[prevValues.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = prevValues[i] * (1f - u) + nextValues[i] * u;
        }

        return result;
    }

    /// <summary>
    /// Cubic spline interpolation using Hermite curves.
    /// Per glTF spec, the keyframe values are structured as:
    /// [in_tangent, value, out_tangent]
    /// where each section has the same size as the output value.
    /// </summary>
    private static float[] EvaluateCubicSpline(
        AnimationKeyframe prevKeyframe,
        AnimationKeyframe nextKeyframe,
        float time)
    {
        float t0 = prevKeyframe.Time;
        float t1 = nextKeyframe.Time;
        float dt = t1 - t0;

        if (dt <= 0f)
        {
            // Degenerate case: return next value
            return (float[])nextKeyframe.Values.Clone();
        }

        float u = (time - t0) / dt;

        // Parse the cubic spline data structure
        var prevData = prevKeyframe.Values;
        var nextData = nextKeyframe.Values;

        // For cubic spline, the structure is [in_tangent, value, out_tangent]
        // So the value size is 1/3 of the total data
        int valueSize = prevData.Length / 3;

        if (prevData.Length != nextData.Length || prevData.Length % 3 != 0)
        {
            throw new InvalidOperationException(
                $"Invalid cubic spline keyframe data: length={prevData.Length} (should be divisible by 3)");
        }

        float[] result = new float[valueSize];

        // Extract values and tangents
        float[] p0 = new float[valueSize];           // prev value
        float[] m0 = new float[valueSize];           // prev out_tangent
        float[] p1 = new float[valueSize];           // next value
        float[] m1 = new float[valueSize];           // next in_tangent

        Array.Copy(prevData, valueSize, p0, 0, valueSize);         // prev value at [1/3 : 2/3]
        Array.Copy(prevData, valueSize * 2, m0, 0, valueSize);     // prev out_tangent at [2/3 : 1]
        Array.Copy(nextData, valueSize, p1, 0, valueSize);         // next value at [1/3 : 2/3]
        Array.Copy(nextData, 0, m1, 0, valueSize);                 // next in_tangent at [0 : 1/3]

        // Hermite basis functions
        float h00 = 2f * u * u * u - 3f * u * u + 1f;
        float h10 = u * u * u - 2f * u * u + u;
        float h01 = -2f * u * u * u + 3f * u * u;
        float h11 = u * u * u - u * u;

        // Interpolate each component
        for (int i = 0; i < valueSize; i++)
        {
            result[i] = h00 * p0[i] + h10 * dt * m0[i] + h01 * p1[i] + h11 * dt * m1[i];
        }

        return result;
    }
}
