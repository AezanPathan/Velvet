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
        const float h = 1.0f; // Use size 1.0 for skybox

        var data = new List<float>(capacity: 36 * 3); 
        
        // Front (+Z) - viewed from inside
        AddFace(data,
            a: (-h, +h, +h),
            b: (+h, +h, +h),
            c: (+h, -h, +h),
            d: (-h, -h, +h));

        // Back (-Z)
        AddFace(data,
            a: (+h, +h, -h),
            b: (-h, +h, -h),
            c: (-h, -h, -h),
            d: (+h, -h, -h));

        // Top (+Y)
        AddFace(data,
            a: (-h, +h, -h),
            b: (+h, +h, -h),
            c: (+h, +h, +h),
            d: (-h, +h, +h));

        // Bottom (-Y)
        AddFace(data,
            a: (-h, -h, +h),
            b: (+h, -h, +h),
            c: (+h, -h, -h),
            d: (-h, -h, -h));

        // Right (+X)
        AddFace(data,
            a: (+h, +h, +h),
            b: (+h, +h, -h),
            c: (+h, -h, -h),
            d: (+h, -h, +h));

        // Left (-X)
        AddFace(data,
            a: (-h, +h, -h),
            b: (-h, +h, +h),
            c: (-h, -h, +h),
            d: (-h, -h, -h));

        if (data.Count != 36 * 3)
        {
            throw new InvalidOperationException($"SkyboxGeometry error: expected {36 * 3} floats, got {data.Count}.");
        }

        return data.ToArray();
    }

    private static void AddFace(
        List<float> data,
        (float x, float y, float z) a,
        (float x, float y, float z) b,
        (float x, float y, float z) c,
        (float x, float y, float z) d)
    {
        // Two triangles: (a,b,c) and (a,c,d)
        AddVertex(data, a);
        AddVertex(data, b);
        AddVertex(data, c);

        AddVertex(data, a);
        AddVertex(data, c);
        AddVertex(data, d);
    }

    private static void AddVertex(List<float> data, (float x, float y, float z) p)
    {
        data.Add(p.x);
        data.Add(p.y);
        data.Add(p.z);
    }
}
