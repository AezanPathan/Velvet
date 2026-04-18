namespace Velvet.Core.Geometry;

/// <summary>
/// Describes how vertex data is structured within a float array.
/// Offsets and stride are expressed in floats.
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

    /// <summary>Position only: [x, y, z]</summary>
    public static VertexLayout Position { get; } = new(
        3,
        [
            new VertexElement(VertexElementSemantic.Position, 0, 3),
        ]);

    /// <summary>Position + color + normal</summary>
    public static VertexLayout PositionColorNormal { get; } = new(
        9,
        [
            new VertexElement(VertexElementSemantic.Position, 0, 3),
            new VertexElement(VertexElementSemantic.Color, 3, 3),
            new VertexElement(VertexElementSemantic.Normal, 6, 3),
        ]);

    /// <summary>Position + normal + UV</summary>
    public static VertexLayout PositionNormalUV { get; } = new(
        8,
        [
            new VertexElement(VertexElementSemantic.Position, 0, 3),
            new VertexElement(VertexElementSemantic.Normal, 3, 3),
            new VertexElement(VertexElementSemantic.UV, 6, 2),
        ]);

    /// <summary>Position + normal + UV + joints + weights</summary>
    public static VertexLayout PositionNormalUVSkinnedJointsWeights { get; } = new(
        16,
        [
            new VertexElement(VertexElementSemantic.Position, 0, 3),
            new VertexElement(VertexElementSemantic.Normal, 3, 3),
            new VertexElement(VertexElementSemantic.UV, 6, 2),
            new VertexElement(VertexElementSemantic.Joints, 8, 4),
            new VertexElement(VertexElementSemantic.Weights, 12, 4),
        ]);
}

public enum VertexElementSemantic
{
    Position,
    Color,
    Normal,
    UV,
    Joints,
    Weights,
}

public readonly record struct VertexElement(
    VertexElementSemantic Semantic,
    int OffsetFloats,
    int ComponentCount
);