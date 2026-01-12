using System;

namespace Velvet.Core.Geometry;

/// <summary>
/// Minimal concrete geometry wrapper for data loaded at runtime (e.g., glTF).
/// This keeps the existing Mesh pipeline unchanged.
/// </summary>
public sealed class LoadedGeometry : GeometryBase
{
    public LoadedGeometry(float[] vertices, uint[]? indices, VertexLayout? layout = null)
        : base(vertices, indices, layout ?? VertexLayout.PositionColorNormal)
    {
    }
}
