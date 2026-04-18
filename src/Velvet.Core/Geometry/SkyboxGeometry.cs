namespace Velvet.Core.Geometry;

/// <summary>
/// Unit cube for skybox rendering.
/// Vertices are ordered with inverted winding so the cube is visible from inside.
/// Uses position-only vertex layout (xyz per vertex).
/// </summary>
public sealed class SkyboxGeometry : GeometryBase
{
    public SkyboxGeometry()
        : base(vertices: CreateVertices(), indices: null, layout: VertexLayout.Position)
    {
    }

    private static float[] CreateVertices()
    {
        const float h = 1.0f;

        var data = new List<float>(capacity: 36 * 3);

        GeometryBuilder.AddFacePosition(data,
            a: (-h, +h, +h), b: (+h, +h, +h), c: (+h, -h, +h), d: (-h, -h, +h));

        GeometryBuilder.AddFacePosition(data,
            a: (+h, +h, -h), b: (-h, +h, -h), c: (-h, -h, -h), d: (+h, -h, -h));

        GeometryBuilder.AddFacePosition(data,
            a: (-h, +h, -h), b: (+h, +h, -h), c: (+h, +h, +h), d: (-h, +h, +h));

        GeometryBuilder.AddFacePosition(data,
            a: (-h, -h, +h), b: (+h, -h, +h), c: (+h, -h, -h), d: (-h, -h, -h));

        GeometryBuilder.AddFacePosition(data,
            a: (+h, +h, +h), b: (+h, +h, -h), c: (+h, -h, -h), d: (+h, -h, +h));

        GeometryBuilder.AddFacePosition(data,
            a: (-h, +h, -h), b: (-h, +h, +h), c: (-h, -h, +h), d: (-h, -h, -h));

        return [.. data];
    }
}
