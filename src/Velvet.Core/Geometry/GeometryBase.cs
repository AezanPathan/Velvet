using System;

namespace Velvet.Core.Geometry;

/// <summary>
/// Base type for all geometry authored in C#.
/// Geometry is data-only: it contains vertex/index arrays and a vertex layout.
/// </summary>
public abstract class GeometryBase
{
    protected GeometryBase(float[] vertices, ushort[]? indices, VertexLayout layout)
    {
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Indices = indices;
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));

        Validate();
    }

    /// <summary>
    /// Interleaved vertex data. For <see cref="VertexLayout.PositionColor"/> the format is:
    /// <c>[x, y, z, r, g, b]</c> per vertex.
    /// </summary>
    public float[] Vertices { get; }

    /// <summary>
    /// Optional triangle index buffer.
    /// When null, the geometry is drawn non-indexed (typically triangles).
    /// </summary>
    public ushort[]? Indices { get; }

    /// <summary>
    /// Describes the meaning of the interleaved vertex data.
    /// </summary>
    public VertexLayout Layout { get; }

    /// <summary>
    /// Number of vertices in <see cref="Vertices"/>, derived from <see cref="Layout"/>.
    /// </summary>
    public int VertexCount => Vertices.Length / Layout.StrideFloats;

    /// <summary>
    /// True when <see cref="Indices"/> is provided.
    /// </summary>
    public bool IsIndexed => Indices is { Length: > 0 };

    protected virtual void Validate()
    {
        if (Layout.StrideFloats <= 0)
        {
            throw new InvalidOperationException("Vertex layout stride must be positive.");
        }

        if (Vertices.Length == 0)
        {
            throw new InvalidOperationException("Vertices cannot be empty.");
        }

        if (Vertices.Length % Layout.StrideFloats != 0)
        {
            throw new InvalidOperationException(
                $"Vertices length ({Vertices.Length}) must be a multiple of stride ({Layout.StrideFloats}).");
        }

        if (Indices is null)
        {
            return;
        }

        if (Indices.Length == 0)
        {
            throw new InvalidOperationException("Indices cannot be an empty array. Use null for non-indexed geometry.");
        }

        if (Indices.Length % 3 != 0)
        {
            throw new InvalidOperationException(
                $"Index count ({Indices.Length}) must be a multiple of 3 (triangles).");
        }

        var vertexCount = VertexCount;
        for (var i = 0; i < Indices.Length; i++)
        {
            if (Indices[i] >= vertexCount)
            {
                throw new InvalidOperationException(
                    $"Index out of range at i={i}: {Indices[i]} >= vertexCount ({vertexCount}).");
            }
        }
    }
}
