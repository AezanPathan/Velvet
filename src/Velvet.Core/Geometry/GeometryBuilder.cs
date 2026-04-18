namespace Velvet.Core.Geometry;

internal static class GeometryBuilder
{
    internal static (float x, float y, float z) CalculateNormal(
        (float x, float y, float z) a,
        (float x, float y, float z) b,
        (float x, float y, float z) c)
    {
        var ux = b.x - a.x; var uy = b.y - a.y; var uz = b.z - a.z;
        var vx = c.x - a.x; var vy = c.y - a.y; var vz = c.z - a.z;
        var nx = uy * vz - uz * vy;
        var ny = uz * vx - ux * vz;
        var nz = ux * vy - uy * vx;
        var len = MathF.Sqrt(nx * nx + ny * ny + nz * nz);
        if (len != 0f)
        {
            nx /= len;
            ny /= len;
            nz /= len;
        }

        return (nx, ny, nz);
    }

    internal static void AddFacePositionColorNormal(
        List<float> data,
        (float x, float y, float z) a,
        (float x, float y, float z) b,
        (float x, float y, float z) c,
        (float x, float y, float z) d,
        (float r, float g, float b) color)
    {
        var normal = CalculateNormal(a, b, c);
        AddFacePositionColorNormal(data, a, b, c, d, color, normal);
    }

    internal static void AddFacePositionColorNormal(
        List<float> data,
        (float x, float y, float z) a,
        (float x, float y, float z) b,
        (float x, float y, float z) c,
        (float x, float y, float z) d,
        (float r, float g, float b) color,
        (float x, float y, float z) normal)
    {
        AddVertexPositionColorNormal(data, a, color, normal);
        AddVertexPositionColorNormal(data, b, color, normal);
        AddVertexPositionColorNormal(data, c, color, normal);

        AddVertexPositionColorNormal(data, a, color, normal);
        AddVertexPositionColorNormal(data, c, color, normal);
        AddVertexPositionColorNormal(data, d, color, normal);
    }

    internal static void AddVertexPositionColorNormal(
        List<float> data,
        (float x, float y, float z) position,
        (float r, float g, float b) color,
        (float x, float y, float z) normal)
    {
        data.Add(position.x);
        data.Add(position.y);
        data.Add(position.z);
        data.Add(color.r);
        data.Add(color.g);
        data.Add(color.b);
        data.Add(normal.x);
        data.Add(normal.y);
        data.Add(normal.z);
    }

    internal static void AddFacePosition(
        List<float> data,
        (float x, float y, float z) a,
        (float x, float y, float z) b,
        (float x, float y, float z) c,
        (float x, float y, float z) d)
    {
        AddVertexPosition(data, a);
        AddVertexPosition(data, b);
        AddVertexPosition(data, c);

        AddVertexPosition(data, a);
        AddVertexPosition(data, c);
        AddVertexPosition(data, d);
    }

    internal static void AddVertexPosition(List<float> data, (float x, float y, float z) position)
    {
        data.Add(position.x);
        data.Add(position.y);
        data.Add(position.z);
    }
}
