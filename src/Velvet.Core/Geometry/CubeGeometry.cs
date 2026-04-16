namespace Velvet.Core.Geometry;

/// <summary>
/// Unit cube centered at origin.
/// Non-indexed, 6 faces × 2 triangles × 3 vertices = 36 vertices.
/// </summary>
public sealed class CubeGeometry : GeometryBase
{
    public CubeGeometry()
        : base(vertices: CreateVertices(), indices: null, layout: VertexLayout.PositionColorNormal)
    {
    }

    private static float[] CreateVertices()
    {
        const float h = 0.5f;

        // Per-face colors (RGB, 0..1)
        var red = (r: 1f, g: 0f, b: 0f);
        var green = (r: 0f, g: 1f, b: 0f);
        var blue = (r: 0f, g: 0f, b: 1f);
        var yellow = (r: 1f, g: 1f, b: 0f);
        var magenta = (r: 1f, g: 0f, b: 1f);
        var cyan = (r: 0f, g: 1f, b: 1f);

        // 36 vertices * 9 floats = 324 floats.
        var data = new List<float>(capacity: 36 * 9);

        // Faces are authored with CCW winding when looking at the outside of the cube.
        // Front (+Z)
        AddFace(
            data,
            a: (-h, -h, +h),
            b: (+h, -h, +h),
            c: (+h, +h, +h),
            d: (-h, +h, +h),
            color: red);

        // Back (-Z)
        AddFace(
            data,
            a: (+h, -h, -h),
            b: (-h, -h, -h),
            c: (-h, +h, -h),
            d: (+h, +h, -h),
            color: green);

        // Top (+Y)
        AddFace(
            data,
            a: (-h, +h, +h),
            b: (+h, +h, +h),
            c: (+h, +h, -h),
            d: (-h, +h, -h),
            color: blue);

        // Bottom (-Y)
        AddFace(
            data,
            a: (-h, -h, -h),
            b: (+h, -h, -h),
            c: (+h, -h, +h),
            d: (-h, -h, +h),
            color: yellow);

        // Right (+X)
        AddFace(
            data,
            a: (+h, -h, +h),
            b: (+h, -h, -h),
            c: (+h, +h, -h),
            d: (+h, +h, +h),
            color: magenta);

        // Left (-X)
        AddFace(
            data,
            a: (-h, -h, -h),
            b: (-h, -h, +h),
            c: (-h, +h, +h),
            d: (-h, +h, -h),
            color: cyan);

        if (data.Count != 36 * 9)
            throw new InvalidOperationException($"CubeGeometry authoring error: expected {36 * 9} floats, got {data.Count}.");

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
        // Compute face normal using cross product of edges (b - a) × (c - a)
        var ux = b.x - a.x; var uy = b.y - a.y; var uz = b.z - a.z;
        var vx = c.x - a.x; var vy = c.y - a.y; var vz = c.z - a.z;
        var nx = uy * vz - uz * vy;
        var ny = uz * vx - ux * vz;
        var nz = ux * vy - uy * vx;
        // Normalize
        var len = (float)System.Math.Sqrt(nx * nx + ny * ny + nz * nz);
        if (len != 0f)
        {
            nx /= len;
            ny /= len;
            nz /= len;
        }

        // Two triangles: (a,b,c) and (a,c,d)
        AddVertex(data, a, color, (nx, ny, nz));
        AddVertex(data, b, color, (nx, ny, nz));
        AddVertex(data, c, color, (nx, ny, nz));

        AddVertex(data, a, color, (nx, ny, nz));
        AddVertex(data, c, color, (nx, ny, nz));
        AddVertex(data, d, color, (nx, ny, nz));
    }

    private static void AddVertex(List<float> data, (float x, float y, float z) p, (float r, float g, float b) c, (float x, float y, float z) n)
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
