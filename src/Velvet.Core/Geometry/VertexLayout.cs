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

        /// <summary>
        /// Layout: interleaved position (x,y,z) + color (r,g,b) + normal (nx,ny,nz) per vertex.
        /// Format: <c>[x, y, z, r, g, b, nx, ny, nz]</c>
        /// </summary>
        public static VertexLayout PositionColorNormal { get; } = new(
            strideFloats: 9,
            elements: new[]
            {
                new VertexElement(VertexElementSemantic.Position, OffsetFloats: 0, ComponentCount: 3),
                new VertexElement(VertexElementSemantic.Color, OffsetFloats: 3, ComponentCount: 3),
                new VertexElement(VertexElementSemantic.Normal, OffsetFloats: 6, ComponentCount: 3),
            });

        /// <summary>
        /// Layout: interleaved position (x,y,z) + color (r,g,b) + normal (nx,ny,nz) + uv (u,v) per vertex.
        /// Format: <c>[x, y, z, r, g, b, nx, ny, nz, u, v]</c>
        /// </summary>
        public static VertexLayout PositionColorNormalUV { get; } = new(
            strideFloats: 11,
            elements: new[]
            {
                new VertexElement(VertexElementSemantic.Position, OffsetFloats: 0, ComponentCount: 3),
                new VertexElement(VertexElementSemantic.Color, OffsetFloats: 3, ComponentCount: 3),
                new VertexElement(VertexElementSemantic.Normal, OffsetFloats: 6, ComponentCount: 3),
                new VertexElement(VertexElementSemantic.UV, OffsetFloats: 9, ComponentCount: 2),
            });

        /// <summary>
        /// Canonical layout for textured models: position (x,y,z) + normal (nx,ny,nz) + uv (u,v) per vertex.
        /// Format: <c>[x, y, z, nx, ny, nz, u, v]</c>
        /// Stride: 8 floats (32 bytes per vertex)
        /// </summary>
        public static VertexLayout PositionNormalUV { get; } = new(
            strideFloats: 8,
            elements: new[]
            {
                new VertexElement(VertexElementSemantic.Position, OffsetFloats: 0, ComponentCount: 3),
                new VertexElement(VertexElementSemantic.Normal, OffsetFloats: 3, ComponentCount: 3),
                new VertexElement(VertexElementSemantic.UV, OffsetFloats: 6, ComponentCount: 2),
            });
}

public enum VertexElementSemantic
{
    Position,
    Color,
    Normal,
    UV,
}

public readonly record struct VertexElement(
    VertexElementSemantic Semantic,
    int OffsetFloats,
    int ComponentCount
);
