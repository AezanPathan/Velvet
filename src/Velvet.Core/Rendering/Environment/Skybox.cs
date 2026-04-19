using Velvet.Core.Geometry;
using Velvet.Core.Rendering.Meshes;

namespace Velvet.Core.Rendering.Environment;

/// <summary>
/// Skybox for rendering an environment background.
/// </summary>
public sealed class Skybox
{
    public Mesh Mesh { get; }

    public int? CubemapTextureId { get; }

    public Skybox(Mesh mesh, int? cubemapTextureId = null)
    {
        Mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        CubemapTextureId = cubemapTextureId;
    }

    public static Skybox CreateDefault()
    {
        var geometry = new SkyboxGeometry();
        var mesh = new Mesh(geometry);
        return new Skybox(mesh);
    }
}
