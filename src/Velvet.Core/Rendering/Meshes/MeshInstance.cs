namespace Velvet.Core.Rendering.Meshes;

using Velvet.Core.Math;
using Velvet.Core.Rendering.Bounds;
using Velvet.Core.Rendering.Skinning;

/// <summary>
/// A mesh with an immutable world transform.
/// </summary>
public readonly struct MeshInstance
{
    public MeshInstance(Mesh mesh, float[] modelMatrix, Skin? skin = null)
        : this(mesh, modelMatrix, Matrix.NormalMatrix(modelMatrix), skin)
    {
    }

    public MeshInstance(Mesh mesh, float[] modelMatrix, float[] normalMatrix, Skin? skin = null)
    {
        Mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        ModelMatrix = (float[])(modelMatrix ?? throw new ArgumentNullException(nameof(modelMatrix))).Clone();
        NormalMatrix = (float[])(normalMatrix ?? throw new ArgumentNullException(nameof(normalMatrix))).Clone();
        Skin = skin;

        if (ModelMatrix.Length != 16) throw new ArgumentException("Model matrix must be 4x4.", nameof(modelMatrix));
        if (NormalMatrix.Length != 9) throw new ArgumentException("Normal matrix must be 3x3.", nameof(normalMatrix));

        BoundingBox = TransformBounds(mesh.LocalBounds, ModelMatrix);
    }

    public Mesh Mesh { get; }

    public Skin? Skin { get; }

    public float[] ModelMatrix { get; }

    public float[] NormalMatrix { get; }

    public BoundingBox BoundingBox { get; }

    private static BoundingBox TransformBounds(in BoundingBox localBounds, float[] modelMatrix)
    {
        var min = localBounds.Min;
        var max = localBounds.Max;

        var center = new Vector3(
            (min.X + max.X) * 0.5f,
            (min.Y + max.Y) * 0.5f,
            (min.Z + max.Z) * 0.5f);

        var extents = new Vector3(
            (max.X - min.X) * 0.5f,
            (max.Y - min.Y) * 0.5f,
            (max.Z - min.Z) * 0.5f);

        var worldCenter = TransformPoint(modelMatrix, center);

        var ex = MathF.Abs(modelMatrix[0]) * extents.X
               + MathF.Abs(modelMatrix[4]) * extents.Y
               + MathF.Abs(modelMatrix[8]) * extents.Z;
        var ey = MathF.Abs(modelMatrix[1]) * extents.X
               + MathF.Abs(modelMatrix[5]) * extents.Y
               + MathF.Abs(modelMatrix[9]) * extents.Z;
        var ez = MathF.Abs(modelMatrix[2]) * extents.X
               + MathF.Abs(modelMatrix[6]) * extents.Y
               + MathF.Abs(modelMatrix[10]) * extents.Z;

        var worldExtents = new Vector3(ex, ey, ez);

        return new BoundingBox(worldCenter - worldExtents, worldCenter + worldExtents);
    }

    private static Vector3 TransformPoint(float[] matrix, in Vector3 point)
    {
        var x = point.X;
        var y = point.Y;
        var z = point.Z;
        var w = 1f;

        var tx = matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12] * w;
        var ty = matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13] * w;
        var tz = matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14] * w;
        var tw = matrix[3] * x + matrix[7] * y + matrix[11] * z + matrix[15] * w;

        if (MathF.Abs(tw - 1f) > float.Epsilon && tw != 0f)
        {
            tx /= tw;
            ty /= tw;
            tz /= tw;
        }

        return new Vector3(tx, ty, tz);
    }
}
