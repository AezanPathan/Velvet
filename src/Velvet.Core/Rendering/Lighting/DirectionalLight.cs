using Velvet.Core.Math;

namespace Velvet.Core.Rendering.Lighting;

/// <summary>
/// Simple engine-grade directional light.
/// </summary>
public sealed class DirectionalLight
{

    #region Fields

    private Vector3 _direction;

    public Vector3 Direction
    {
        get => _direction;
        set => _direction = value.Normalized();
    }

    public Vector3 Color { get; set; }
    public float Intensity { get; set; }


    #endregion

    #region  Ctor

    public DirectionalLight(in Vector3 direction, in Vector3 color, float intensity = 1.0f)
    {
        if (intensity < 0f) throw new ArgumentOutOfRangeException(nameof(intensity));
        Direction = direction.Normalized();
        Color = color;
        Intensity = intensity;
    }

    #endregion

    #region Methods

    // Methods will go here 

    #endregion


}
