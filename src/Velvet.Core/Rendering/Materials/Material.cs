using Velvet.Core.Math;

namespace Velvet.Core.Rendering.Materials;

/// <summary>
/// Minimal, data-only material used to decouple geometry from appearance.
/// Authored in C# and sent to shaders via uniforms.
/// </summary>
public sealed class Material
{
    /// <summary>
    /// Base surface color
    /// </summary>
    public Vector3 AlbedoColor { get; set; }
    /// <summary>
    /// AmbientStrength
    /// </summary>
    public float AmbientStrength { get; set; }

    /// <summary>
    /// DiffuseStrength 
    /// </summary>
    public float DiffuseStrength { get; set; }

    /// <summary>
    /// If true, lighting is bypassed and the material renders as a flat color.
    /// </summary>
    public bool Unlit { get; set; }

    /// <summary>
    /// Optional base color texture URI (relative or data: URL).
    /// If provided, the renderer will load and sample this texture.
    /// </summary>
    public string? BaseColorTextureUri { get; set; }

    public Material(Vector3 albedoColor, float ambientStrength, float diffuseStrength, bool unlit = false)
    {
        AlbedoColor = albedoColor;
        AmbientStrength = ambientStrength;
        DiffuseStrength = diffuseStrength;
        Unlit = unlit;
    }

    // Default material color 
    public static Material Default { get; } = new(
        albedoColor: new Vector3(1.0f, 1.0f, 1.0f),
        ambientStrength: 0.05f,
        diffuseStrength: 1.0f,
        unlit: false);
}
