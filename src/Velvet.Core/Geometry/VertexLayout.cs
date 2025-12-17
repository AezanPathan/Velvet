using System;
using System.Collections.Generic;

namespace Velvet.Core.Geometry;

/// <summary>
/// Describes how vertex data is laid out in the <see cref="GeometryBase.Vertices"/> array.
/// Offsets and stride are expressed in floats (not bytes) to match the engine's data-only intent.
/// </summary>
public sealed class VertexLayout
{
    public int StrideFloats { get; }

    public IReadOnlyList<VertexElement> Elements { get; }

    private VertexLayout(int strideFloats, IReadOnlyList<VertexElement> elements)
    {
        if (strideFloats <= 0) throw new ArgumentOutOfRangeException(nameof(strideFloats));
        StrideFloats = strideFloats;
        Elements = elements ?? throw new ArgumentNullException(nameof(elements));
    }

    /// <summary>
    /// Layout: interleaved position (x,y,z) + color (r,g,b) per vertex.
    /// Format: <c>[x, y, z, r, g, b]</c>
    /// </summary>
    public static VertexLayout PositionColor { get; } = new(
        strideFloats: 6,
        elements: new[]
        {
            new VertexElement(VertexElementSemantic.Position, OffsetFloats: 0, ComponentCount: 3),
            new VertexElement(VertexElementSemantic.Color, OffsetFloats: 3, ComponentCount: 3),
        });
}

public enum VertexElementSemantic
{
    Position,
    Color,
}

public readonly record struct VertexElement(
    VertexElementSemantic Semantic,
    int OffsetFloats,
    int ComponentCount
);
