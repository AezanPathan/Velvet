using Velvet.Core.Math;

namespace Velvet.Core.Rendering.Lighting;

/// <summary>
/// Simple directional light.
/// </summary>
public sealed class DirectionalLight
{
    public Vector3 Color { get; set; }
    public float Intensity { get; set; }

    public Vector3 Direction
    {
        get;
        set => field = value.Normalized();
    }

    public DirectionalLight(in Vector3 direction, in Vector3 color, float intensity = 1.0f)
    {
        if (intensity < 0f) throw new ArgumentOutOfRangeException(nameof(intensity));
        Direction = direction.Normalized();
        Color = color;
        Intensity = intensity;
    }
}
