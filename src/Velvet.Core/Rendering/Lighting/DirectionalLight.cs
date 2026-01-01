using System;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering.Lighting;

    /// <summary>
    /// Simple engine-grade directional light.
    /// </summary>
    public sealed class DirectionalLight
    {
        public Vector3 Direction { get; }
        public Vector3 Color { get; }
        public float Intensity { get; }

        public DirectionalLight(in Vector3 direction, in Vector3 color, float intensity = 1.0f)
        {
            if (intensity < 0f) throw new ArgumentOutOfRangeException(nameof(intensity));
            Direction = direction.Normalized();
            Color = color;
            Intensity = intensity;
        }
    }
