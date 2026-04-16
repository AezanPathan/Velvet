using Velvet.Core.Math;

namespace Velvet.Core.Rendering.Lighting;

/// <summary>Simple spot light.</summary>
public sealed class SpotLight
{
    private Vector3 _direction;
    private float _intensity;
    private float _cutoff;
    private float _outerCutoff;
    private float _constant;
    private float _linear;
    private float _quadratic;

    public Vector3 Position { get; set; }

    public Vector3 Direction
    {
        get => _direction;
        set
        {
            if (value.LengthSquared <= 0f)
                throw new ArgumentOutOfRangeException(nameof(value), "Direction must be non-zero.");

            _direction = value.Normalized();
        }
    }

    public Vector3 Color { get; set; }

    public float Intensity
    {
        get => _intensity;
        set
        {
            if (value < 0f) throw new ArgumentOutOfRangeException(nameof(value));
            _intensity = value;
        }
    }

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
        in Vector3 position,
        in Vector3 direction,
        in Vector3 color,
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
