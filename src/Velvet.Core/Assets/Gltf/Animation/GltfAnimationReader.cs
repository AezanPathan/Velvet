using System.Text.Json;
using Velvet.Core.Animation;

namespace Velvet.Core.Assets.Gltf;

internal static class GltfAnimationReader
{
    internal static List<AnimationClip> LoadAnimations(GltfContext context)
    {
        var clips = new List<AnimationClip>();

        if (!context.Root.TryGetProperty("animations", out var animationsEl) || animationsEl.ValueKind != JsonValueKind.Array)
        {
            return clips;
        }

        if (!context.Root.TryGetProperty("nodes", out var nodesEl) || nodesEl.ValueKind != JsonValueKind.Array)
        {
            return clips;
        }

        var animationIndex = 0;
        foreach (var animationEl in animationsEl.EnumerateArray())
        {
            var clipName = animationEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(clipName))
            {
                clipName = $"Animation_{animationIndex}";
            }

            var channels = new List<AnimationChannel>();

            if (!animationEl.TryGetProperty("channels", out var channelsEl) || channelsEl.ValueKind != JsonValueKind.Array)
            {
                clips.Add(new AnimationClip(clipName!, channels));
                animationIndex++;
                continue;
            }

            if (!animationEl.TryGetProperty("samplers", out var samplersEl) || samplersEl.ValueKind != JsonValueKind.Array)
            {
                clips.Add(new AnimationClip(clipName!, channels));
                animationIndex++;
                continue;
            }

            foreach (var channelEl in channelsEl.EnumerateArray())
            {
                if (!channelEl.TryGetProperty("sampler", out var samplerIndexEl))
                {
                    continue;
                }

                var samplerIndex = samplerIndexEl.GetInt32();
                if (samplerIndex < 0 || samplerIndex >= samplersEl.GetArrayLength())
                {
                    continue;
                }

                if (!channelEl.TryGetProperty("target", out var targetEl) || targetEl.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (!targetEl.TryGetProperty("node", out var nodeIndexEl))
                {
                    continue;
                }

                var nodeIndex = nodeIndexEl.GetInt32();
                var nodeName = GetNodeName(nodesEl, nodeIndex);

                if (!targetEl.TryGetProperty("path", out var pathEl))
                {
                    continue;
                }

                var path = ParseAnimationProperty(pathEl.GetString());
                if (path == AnimationProperty.Weights)
                {
                    // Morph target weights are not supported in this node-based animation system.
                    continue;
                }

                var samplerEl = samplersEl[samplerIndex];
                var interpolation = ParseInterpolationMode(samplerEl);

                if (!samplerEl.TryGetProperty("input", out var inputEl) || !samplerEl.TryGetProperty("output", out var outputEl))
                {
                    continue;
                }

                var inputAccessor = inputEl.GetInt32();
                var outputAccessor = outputEl.GetInt32();

                var times = GltfAccessorReader.ReadAccessorFloatScalar(context, inputAccessor, out var timeCount);
                if (timeCount == 0)
                {
                    continue;
                }

                var expectedComponents = GetComponentCountForPath(path);
                var outputValues = GltfAccessorReader.ReadAccessorFloatArray(context, outputAccessor, expectedComponents, out var outputCount);

                var expectedOutputCount = interpolation == InterpolationMode.CubicSpline ? timeCount * 3 : timeCount;
                if (outputCount != expectedOutputCount)
                {
                    throw new InvalidDataException($"Animation sampler output count mismatch. Expected {expectedOutputCount}, got {outputCount}.");
                }

                var keyframes = new List<AnimationKeyframe>(timeCount);
                for (int i = 0; i < timeCount; i++)
                {
                    var values = ExtractKeyframeValues(outputValues, i, expectedComponents, interpolation);
                    keyframes.Add(new AnimationKeyframe(times[i], values));
                }

                var sampler = new AnimationSampler(keyframes, interpolation);
                channels.Add(new AnimationChannel(sampler, nodeName, path));
            }

            clips.Add(new AnimationClip(clipName!, channels));
            animationIndex++;
        }

        return clips;
    }


    internal static string GetNodeName(JsonElement nodesEl, int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= nodesEl.GetArrayLength())
        {
            return $"Node_{nodeIndex}";
        }

        var nodeEl = nodesEl[nodeIndex];
        var name = nodeEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"Node_{nodeIndex}";
        }

        return name!;
    }


    internal static AnimationProperty ParseAnimationProperty(string? path)
    {
        return path switch
        {
            "translation" => AnimationProperty.Translation,
            "rotation" => AnimationProperty.Rotation,
            "scale" => AnimationProperty.Scale,
            "weights" => AnimationProperty.Weights,
            _ => throw new NotSupportedException($"Unsupported animation path: {path}")
        };
    }


    internal static InterpolationMode ParseInterpolationMode(JsonElement samplerEl)
    {
        if (samplerEl.TryGetProperty("interpolation", out var interpEl))
        {
            var interp = interpEl.GetString();
            return interp switch
            {
                "STEP" => InterpolationMode.Step,
                "LINEAR" => InterpolationMode.Linear,
                "CUBICSPLINE" => InterpolationMode.CubicSpline,
                _ => throw new NotSupportedException($"Unsupported interpolation mode: {interp}")
            };
        }

        return InterpolationMode.Linear;
    }


    internal static int GetComponentCountForPath(AnimationProperty path)
    {
        return path switch
        {
            AnimationProperty.Translation => 3,
            AnimationProperty.Rotation => 4,
            AnimationProperty.Scale => 3,
            AnimationProperty.Weights => 1,
            _ => throw new NotSupportedException($"Unsupported animation path: {path}")
        };
    }


    internal static float[] ExtractKeyframeValues(float[] outputValues, int keyframeIndex, int componentCount, InterpolationMode interpolation)
    {
        var multiplier = interpolation == InterpolationMode.CubicSpline ? 3 : 1;
        var valuesPerKeyframe = componentCount * multiplier;
        var startIndex = keyframeIndex * valuesPerKeyframe;

        if (startIndex + valuesPerKeyframe > outputValues.Length)
        {
            throw new InvalidDataException("Animation output values are out of range for keyframe extraction.");
        }

        var values = new float[valuesPerKeyframe];
        Array.Copy(outputValues, startIndex, values, 0, valuesPerKeyframe);
        return values;
    }
}
