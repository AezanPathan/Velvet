using System;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering
{
    /// <summary>
    /// Simple engine-grade point light (single-instance friendly).
    /// Diffuse-only shading is expected (Lambert).
    /// </summary>
    public sealed class PointLight
    {
        public Vec3 Position { get; }
        public Vec3 Color { get; }
        public float Intensity { get; }

        public float Constant { get; }
        public float Linear { get; }
        public float Quadratic { get; }

        public PointLight(
            in Vec3 position,
            in Vec3 color,
            float intensity = 1.0f,
            float constant = 1.0f,
            float linear = 0.09f,
            float quadratic = 0.032f)
        {
            if (intensity < 0f) throw new ArgumentOutOfRangeException(nameof(intensity));
            if (constant <= 0f) throw new ArgumentOutOfRangeException(nameof(constant));
            if (linear < 0f) throw new ArgumentOutOfRangeException(nameof(linear));
            if (quadratic < 0f) throw new ArgumentOutOfRangeException(nameof(quadratic));

            Position = position;
            Color = color;
            Intensity = intensity;
            Constant = constant;
            Linear = linear;
            Quadratic = quadratic;
        }
    }
}
