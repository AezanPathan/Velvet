using Velvet.Core.Geometry;

public sealed class PlaneGeometry : GeometryBase
{
    private static readonly (float r, float g, float b) White = (1f, 1f, 1f);

    public PlaneGeometry()
        : base(CreateVertices(), null, VertexLayout.PositionColorNormal)
    {
    }

    private static float[] CreateVertices()
    {
        const float h = 0.5f;
        var data = new List<float>(6 * 9);

        GeometryBuilder.AddFacePositionColorNormal(
            data,
            (-h, 0f, -h),
            (+h, 0f, -h),
            (+h, 0f, +h),
            (-h, 0f, +h),
            White,
            normal: (0f, 1f, 0f)
        );

        return data.ToArray();
    }
}
