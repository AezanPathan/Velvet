using System;
using System.Collections.Generic;

namespace Velvet.Core.Geometry;

/// <summary>
/// Simple UV sphere centered at origin.
/// Indexed triangles with correct per-vertex normals.
/// </summary>
public sealed class SphereGeometry : GeometryBase
{
    public SphereGeometry(int latitudeSegments = 12, int longitudeSegments = 16, float radius = 0.5f)
        : base(
            vertices: CreateVertices(latitudeSegments, longitudeSegments, radius),
            indices: CreateIndices(latitudeSegments, longitudeSegments),
            layout: VertexLayout.PositionColorNormal)
    {
        if (latitudeSegments < 3) throw new ArgumentOutOfRangeException(nameof(latitudeSegments), "latitudeSegments must be >= 3.");
        if (longitudeSegments < 3) throw new ArgumentOutOfRangeException(nameof(longitudeSegments), "longitudeSegments must be >= 3.");
        if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius), "radius must be > 0.");
    }

    private static float[] CreateVertices(int latSegments, int lonSegments, float radius)
    {
        // Vertices are laid out as a (latSegments+1) x (lonSegments+1) grid.
        // We duplicate the seam column (lon = 0 and lon = lonSegments) for correct interpolation.
        var vertexCount = (latSegments + 1) * (lonSegments + 1);
        var data = new List<float>(capacity: vertexCount * 9);

        for (var lat = 0; lat <= latSegments; lat++)
        {
            var v = lat / (float)latSegments;          // 0..1
            var phi = v * System.MathF.PI;             // 0..PI

            var sinPhi = System.MathF.Sin(phi);
            var cosPhi = System.MathF.Cos(phi);

            for (var lon = 0; lon <= lonSegments; lon++)
            {
                var u = lon / (float)lonSegments;      // 0..1
                var theta = u * (System.MathF.PI * 2); // 0..2PI

                var sinTheta = System.MathF.Sin(theta);
                var cosTheta = System.MathF.Cos(theta);

                // Unit normal
                var nx = sinPhi * cosTheta;
                var ny = cosPhi;
                var nz = sinPhi * sinTheta;

                // Position
                var x = nx * radius;
                var y = ny * radius;
                var z = nz * radius;

                // Vertex color is currently unused by the default shader when materials are enabled,
                // but keep it authored for completeness and future flexibility.
                const float r = 1f, g = 1f, b = 1f;

                data.Add(x);
                data.Add(y);
                data.Add(z);
                data.Add(r);
                data.Add(g);
                data.Add(b);
                data.Add(nx);
                data.Add(ny);
                data.Add(nz);
            }
        }

        return data.ToArray();
    }

    private static uint[] CreateIndices(int latSegments, int lonSegments)
    {
        // Two triangles per quad.
        // Grid has (latSegments) rows of quads and (lonSegments) columns of quads.
        var indexCount = latSegments * lonSegments * 6;
        var indices = new uint[indexCount];

        var stride = lonSegments + 1;
        var idx = 0;

        for (var lat = 0; lat < latSegments; lat++)
        {
            for (var lon = 0; lon < lonSegments; lon++)
            {
                var a = (uint)((lat * stride) + lon);
                var b = (uint)(((lat + 1) * stride) + lon);
                var c = (uint)(((lat + 1) * stride) + (lon + 1));
                var d = (uint)((lat * stride) + (lon + 1));

                // CCW winding when viewed from outside.
                indices[idx++] = a;
                indices[idx++] = b;
                indices[idx++] = d;

                indices[idx++] = b;
                indices[idx++] = c;
                indices[idx++] = d;
            }
        }

        return indices;
    }
}
