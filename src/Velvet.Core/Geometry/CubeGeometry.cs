namespace Velvet.Core.Geometry;

/// <summary>
/// Unit cube centered at origin.
/// Non-indexed (36 vertices).
/// </summary>
public sealed class CubeGeometry : GeometryBase
{
    private static readonly (float r, float g, float b) White = (1f, 1f, 1f);

    public CubeGeometry()
        : base(CreateVertices(), null, VertexLayout.PositionColorNormal)
    {
    }

    private static float[] CreateVertices()
    {
        const float h = 0.5f;
        var data = new List<float>(36 * 9);

        // Front (+Z)
        GeometryBuilder.AddFacePositionColorNormal(data,
            (-h, -h, +h), (+h, -h, +h), (+h, +h, +h), (-h, +h, +h), White);

        // Back (-Z)
        GeometryBuilder.AddFacePositionColorNormal(data,
            (+h, -h, -h), (-h, -h, -h), (-h, +h, -h), (+h, +h, -h), White);

        // Top (+Y)
        GeometryBuilder.AddFacePositionColorNormal(data,
            (-h, +h, +h), (+h, +h, +h), (+h, +h, -h), (-h, +h, -h), White);

        // Bottom (-Y)
        GeometryBuilder.AddFacePositionColorNormal(data,
            (-h, -h, -h), (+h, -h, -h), (+h, -h, +h), (-h, -h, +h), White);

        // Right (+X)
        GeometryBuilder.AddFacePositionColorNormal(data,
            (+h, -h, +h), (+h, -h, -h), (+h, +h, -h), (+h, +h, +h), White);

        // Left (-X)
        GeometryBuilder.AddFacePositionColorNormal(data,
            (-h, -h, -h), (-h, -h, +h), (-h, +h, +h), (-h, +h, -h), White);

        return [.. data];
    }
}