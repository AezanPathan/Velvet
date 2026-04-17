using Velvet.Core.Geometry;

public sealed class PlaneGeometry : GeometryBase
{
    public PlaneGeometry()
        : base(CreateVertices(), null, VertexLayout.PositionColorNormal)
    {
    }

    private static float[] CreateVertices()
    {
        const float h = 0.5f;

        var color = (r: 1f, g: 1f, b: 1f);

        var data = new List<float>(6 * 9);

        AddFace(
            data,
            (-h, 0f, -h),
            (+h, 0f, -h),
            (+h, 0f, +h),
            (-h, 0f, +h),
            color
        );

        return data.ToArray();
    }

    private static void AddFace(
        List<float> data,
        (float x, float y, float z) a,
        (float x, float y, float z) b,
        (float x, float y, float z) c,
        (float x, float y, float z) d,
        (float r, float g, float b) color)
    {
        // normal (up)
        var normal = (0f, 1f, 0f);

        AddVertex(data, a, color, normal);
        AddVertex(data, b, color, normal);
        AddVertex(data, c, color, normal);

        AddVertex(data, a, color, normal);
        AddVertex(data, c, color, normal);
        AddVertex(data, d, color, normal);
    }

    private static void AddVertex(
        List<float> data,
        (float x, float y, float z) p,
        (float r, float g, float b) c,
        (float x, float y, float z) n)
    {
        data.Add(p.x);
        data.Add(p.y);
        data.Add(p.z);

        data.Add(c.r);
        data.Add(c.g);
        data.Add(c.b);

        data.Add(n.x);
        data.Add(n.y);
        data.Add(n.z);
    }
}