using System;
using Velvet.Core.Geometry;
using Velvet.Core.Rendering.Meshes;

namespace Velvet.Core.Rendering;

/// <summary>
/// Skybox for rendering an environment background.
/// The skybox is rendered as a cube surrounding the camera, appearing infinitely distant.
/// </summary>
public sealed class Skybox
{
    /// <summary>
    /// The mesh representing the skybox cube.
    /// </summary>
    public Mesh Mesh { get; }

    /// <summary>
    /// Optional cubemap texture ID for image-based skybox.
    /// If null, the skybox will use a gradient.
    /// </summary>
    public int? CubemapTextureId { get; }

    public Skybox(Mesh mesh, int? cubemapTextureId = null)
    {
        Mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        CubemapTextureId = cubemapTextureId;
    }

    /// <summary>
    /// Creates a default skybox with a gradient background.
    /// </summary>
    public static Skybox CreateDefault()
    {
        var geometry = new SkyboxGeometry();
        var mesh = new Mesh(geometry);
        return new Skybox(mesh);
    }
}
