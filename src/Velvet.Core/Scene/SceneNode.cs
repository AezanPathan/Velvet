namespace Velvet.Core.Scene;

using Velvet.Core.Math;
using Velvet.Core.Rendering.Bounds;
using Velvet.Core.Rendering.Meshes;
using Velvet.Core.Rendering.Skinning;

public sealed class SceneNode
{
    private float[] _localTransform;

    public SceneNode(float[] localTransform, IReadOnlyList<Mesh> meshes, IReadOnlyList<SceneNode> children, string? name = null, Skin? skin = null, int nodeIndex = -1)
    {
        ArgumentNullException.ThrowIfNull(localTransform);
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentNullException.ThrowIfNull(children);
        if (localTransform.Length != 16) throw new ArgumentException("Local transform must be a 4x4 matrix.", nameof(localTransform));

        _localTransform = localTransform;
        Meshes = meshes;
        Children = children;
        Name = name;
        Skin = skin;
        NodeIndex = nodeIndex;
    }

    public string? Name { get; }

    public IReadOnlyList<Mesh> Meshes { get; }

    public Skin? Skin { get; }

    public int NodeIndex { get; }

    public IReadOnlyList<SceneNode> Children { get; }

    public float[] LocalTransform => _localTransform;

    internal void SetLocalTransform(float[] localTransform)
    {
        ArgumentNullException.ThrowIfNull(localTransform);

        if (localTransform.Length != 16)
        {
            throw new ArgumentException("Local transform must be a 4x4 matrix.", nameof(localTransform));
        }

        _localTransform = localTransform;
    }

    internal void CollectMeshes(List<MeshInstance> output, float[] parentWorld)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(parentWorld);

        var world = Matrix4.Multiply(parentWorld, _localTransform).Data;
        var normalMatrix = Matrix.NormalMatrix(world);

        foreach (var mesh in Meshes)
            output.Add(MeshInstance.CreateOwned(mesh, world, normalMatrix, Skin));

        foreach (var child in Children)
            child.CollectMeshes(output, world);
    }

    internal BoundingBox? ComputeBoundsRecursive(float[] parentWorld)
    {
        ArgumentNullException.ThrowIfNull(parentWorld);

        var world = Matrix4.Multiply(parentWorld, _localTransform).Data;

        BoundingBox? bounds = null;

        foreach (var mesh in Meshes)
        {
            var meshBounds = TransformBounds(mesh.LocalBounds, world);
            SceneBoundsAccumulator.Expand(ref bounds, meshBounds);
        }

        foreach (var child in Children)
        {
            var childBounds = child.ComputeBoundsRecursive(world);
            if (childBounds.HasValue)
            {
                SceneBoundsAccumulator.Expand(ref bounds, childBounds.Value);
            }
        }

        return bounds;
    }

    private static BoundingBox TransformBounds(in BoundingBox localBounds, float[] worldMatrix)
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

        var worldCenter = TransformPoint(worldMatrix, center);

        var ex = MathF.Abs(worldMatrix[0]) * extents.X
               + MathF.Abs(worldMatrix[4]) * extents.Y
               + MathF.Abs(worldMatrix[8]) * extents.Z;
        var ey = MathF.Abs(worldMatrix[1]) * extents.X
               + MathF.Abs(worldMatrix[5]) * extents.Y
               + MathF.Abs(worldMatrix[9]) * extents.Z;
        var ez = MathF.Abs(worldMatrix[2]) * extents.X
               + MathF.Abs(worldMatrix[6]) * extents.Y
               + MathF.Abs(worldMatrix[10]) * extents.Z;

        var worldExtents = new Vector3(ex, ey, ez);

        return new BoundingBox(worldCenter - worldExtents, worldCenter + worldExtents);
    }

    private static Vector3 TransformPoint(float[] matrix, Vector3 point)
    {
        var x = point.X;
        var y = point.Y;
        var z = point.Z;
        var w = 1.0f;

        var resultX = matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12] * w;
        var resultY = matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13] * w;
        var resultZ = matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14] * w;
        var resultW = matrix[3] * x + matrix[7] * y + matrix[11] * z + matrix[15] * w;

        // Apply homogeneous divide when transformed w differs from 1.
        if (MathF.Abs(resultW - 1.0f) > float.Epsilon)
        {
            resultX /= resultW;
            resultY /= resultW;
            resultZ /= resultW;
        }

        return new Vector3(resultX, resultY, resultZ);
    }
}
