using System;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering
{
    /// <summary>
    /// Simple engine-grade directional light.
    /// </summary>
    public sealed class DirectionalLight
    {
        public Vec3 Direction { get; }
        public Vec3 Color { get; }
        public float Intensity { get; }

        public DirectionalLight(in Vec3 direction, in Vec3 color, float intensity = 1.0f)
        {
            if (intensity < 0f) throw new ArgumentOutOfRangeException(nameof(intensity));
            Direction = direction.Normalized();
            Color = color;
            Intensity = intensity;
        }
    }
}
