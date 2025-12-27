using System;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering
{
    /// <summary>
    /// Simple engine-grade spot light (single-instance friendly).
    /// Diffuse-only shading is expected (Lambert).
    /// </summary>
    public sealed class SpotLight
    {
        private Vec3 _direction;
        private float _intensity;
        private float _cutoff;
        private float _outerCutoff;

        private float _constant;
        private float _linear;
        private float _quadratic;

        public Vec3 Position { get; set; }

        /// <summary>
        /// Direction the spot light points toward (normalized).
        /// </summary>
        public Vec3 Direction
        {
            get => _direction;
            set
            {
                if (value.LengthSquared <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(value), "Direction must be non-zero.");

                _direction = value.Normalized();
            }
        }

        public Vec3 Color { get; set; }

        public float Intensity
        {
            get => _intensity;
            set
            {
                if (value < 0f) throw new ArgumentOutOfRangeException(nameof(value));
                _intensity = value;
            }
        }

        /// <summary>
        /// Inner cone angle in radians.
        /// </summary>
        public float Cutoff
        {
            get => _cutoff;
            set
            {
                if (value <= 0f) throw new ArgumentOutOfRangeException(nameof(value));
                if (_outerCutoff > 0f && value > _outerCutoff)
                    throw new ArgumentOutOfRangeException(nameof(value), "Cutoff must be <= OuterCutoff.");

                _cutoff = value;
            }
        }

        /// <summary>
        /// Outer cone angle in radians.
        /// </summary>
        public float OuterCutoff
        {
            get => _outerCutoff;
            set
            {
                if (value <= 0f) throw new ArgumentOutOfRangeException(nameof(value));
                if (_cutoff > 0f && value < _cutoff)
                    throw new ArgumentOutOfRangeException(nameof(value), "OuterCutoff must be >= Cutoff.");

                _outerCutoff = value;
            }
        }

        public float Constant
        {
            get => _constant;
            set
            {
                if (value <= 0f) throw new ArgumentOutOfRangeException(nameof(value));
                _constant = value;
            }
        }

        public float Linear
        {
            get => _linear;
            set
            {
                if (value < 0f) throw new ArgumentOutOfRangeException(nameof(value));
                _linear = value;
            }
        }

        public float Quadratic
        {
            get => _quadratic;
            set
            {
                if (value < 0f) throw new ArgumentOutOfRangeException(nameof(value));
                _quadratic = value;
            }
        }

        public SpotLight(
            in Vec3 position,
            in Vec3 direction,
            in Vec3 color,
            float intensity,
            float cutoff,
            float outerCutoff,
            float constant,
            float linear,
            float quadratic)
        {
            Position = position;
            Color = color;

            Direction = direction;
            OuterCutoff = outerCutoff;
            Cutoff = cutoff;

            Intensity = intensity;
            Constant = constant;
            Linear = linear;
            Quadratic = quadratic;
        }
    }
}
