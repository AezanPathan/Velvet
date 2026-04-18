namespace Velvet.Core.Geometry;

/// <summary>
/// Base type for all geometry authored in C#.
/// </summary>
public abstract class GeometryBase
{
    public float[] Vertices { get; }
    public uint[]? Indices { get; }
    public VertexLayout Layout { get; }

    protected GeometryBase(float[] vertices, uint[]? indices, VertexLayout layout)
    {
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Indices = indices;
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));

        Validate();
    }

    public int VertexCount => Vertices.Length / Layout.StrideFloats;

    protected virtual void Validate()
    {
        if (Layout.StrideFloats <= 0)
            throw new InvalidOperationException("Vertex layout stride must be positive.");

        if (Vertices.Length == 0)
            throw new InvalidOperationException("Vertices cannot be empty.");

        if (Vertices.Length % Layout.StrideFloats != 0)
            throw new InvalidOperationException($"Vertices length ({Vertices.Length}) must be a multiple of stride ({Layout.StrideFloats}).");

        if (Indices is null) return;

        if (Indices.Length == 0)
            throw new InvalidOperationException("Indices cannot be an empty array. Use null for non-indexed geometry.");

        if (Indices.Length % 3 != 0)
            throw new InvalidOperationException(
                $"Index count ({Indices.Length}) must be a multiple of 3 (triangles).");

        var vertexCount = VertexCount;
        for (var i = 0; i < Indices.Length; i++)
        {
            if (Indices[i] >= vertexCount)
                throw new InvalidOperationException($"Index out of range at i={i}: {Indices[i]} >= vertexCount ({vertexCount}).");
        }
    }
}
