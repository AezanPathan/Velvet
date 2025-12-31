using System;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering;

/// <summary>
/// Minimal, data-only material used to decouple geometry from appearance.
/// Authored in C# and sent to shaders via uniforms.
/// </summary>
public sealed class Material
{
    public Material(Vec3 albedoColor, float ambientStrength, float diffuseStrength, bool unlit = false)
    {
        AlbedoColor = albedoColor;
        AmbientStrength = ambientStrength;
        DiffuseStrength = diffuseStrength;
        Unlit = unlit;
    }

    public Vec3 AlbedoColor { get; set; }

    public float AmbientStrength { get; set; }

    public float DiffuseStrength { get; set; }

    public bool Unlit { get; set; }

    public static Material Default { get; } = new(
        albedoColor: new Vec3(1.0f, 1.0f, 1.0f),
        ambientStrength: 0.05f,
        diffuseStrength: 1.0f,
        unlit: false);
}
