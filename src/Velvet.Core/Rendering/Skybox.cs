using System;
using Velvet.Core.Geometry;

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

    private Skybox(Mesh mesh)
    {
        Mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
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
