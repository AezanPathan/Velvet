using Velvet.Core.Geometry;
using Velvet.Core.Rendering.Meshes;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering.Environment;

/// <summary>
/// Skybox for rendering an environment background.
/// </summary>
public sealed class Skybox
{
    public Mesh Mesh { get; }

    public int? CubemapTextureId { get; }
    
    /// <summary>
    /// Horizon color for gradient skybox (used when no cubemap is set).
    /// Default is light blue-gray (0.5, 0.7, 0.9).
    /// </summary>
    public Vector3 HorizonColor { get; set; }
    
    /// <summary>
    /// Zenith (top) color for gradient skybox (used when no cubemap is set).
    /// Default is deeper blue (0.2, 0.4, 0.8).
    /// </summary>
    public Vector3 ZenithColor { get; set; }

    public Skybox(Mesh mesh, int? cubemapTextureId = null, Vector3? horizonColor = null, Vector3? zenithColor = null)
    {
        Mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        CubemapTextureId = cubemapTextureId;
        HorizonColor = horizonColor ?? new Vector3(0.5f, 0.7f, 0.9f);
        ZenithColor = zenithColor ?? new Vector3(0.2f, 0.4f, 0.8f);
    }

    public static Skybox CreateDefault()
    {
        var geometry = new SkyboxGeometry();
        var mesh = new Mesh(geometry);
        return new Skybox(mesh);
    }
    
    /// <summary>
    /// Creates a skybox with custom gradient colors.
    /// </summary>
    public static Skybox CreateWithGradient(Vector3 horizonColor, Vector3 zenithColor)
    {
        var geometry = new SkyboxGeometry();
        var mesh = new Mesh(geometry);
        return new Skybox(mesh, cubemapTextureId: null, horizonColor, zenithColor);
    }
}
