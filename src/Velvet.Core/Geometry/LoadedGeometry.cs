namespace Velvet.Core.Geometry;

/// <summary>
/// Wraps runtime-loaded vertex data as engine geometry.
/// </summary>
public sealed class LoadedGeometry : GeometryBase
{
    public LoadedGeometry(float[] vertices, uint[]? indices, VertexLayout? layout = null)
        : base(vertices, indices, layout ?? VertexLayout.PositionNormalUV)
    {
    }
}