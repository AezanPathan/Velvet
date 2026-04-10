namespace Velvet.Core.Scene;

using System;
using System.Collections.Generic;
using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Meshes;

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

    #region Properties

    public string? Name { get; }

    public IReadOnlyList<Mesh> Meshes { get; }

    public Skin? Skin { get; }

    public int NodeIndex { get; }

    public IReadOnlyList<SceneNode> Children { get; }

    public float[] LocalTransform => _localTransform;

    #endregion

    internal void SetLocalTransform(float[] localTransform)
    {
        ArgumentNullException.ThrowIfNull(localTransform);
        if (localTransform.Length != 16)
            throw new ArgumentException("Local transform must be a 4x4 matrix.", nameof(localTransform));
        
        _localTransform = localTransform;
    }

    internal void CollectMeshes(List<MeshInstance> output, float[] parentWorld)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(parentWorld);

        var world = Matrix.Multiply(parentWorld, _localTransform);

        foreach (var mesh in Meshes)
            output.Add(new MeshInstance(mesh, world, Skin));
        

        foreach (var child in Children)
            child.CollectMeshes(output, world);
        
    }

    internal BoundingBox? ComputeBoundsRecursive(float[] parentWorld)
    {
        ArgumentNullException.ThrowIfNull(parentWorld);

        var world = Matrix.Multiply(parentWorld, _localTransform);

        BoundingBox? bounds = null;

        foreach (var mesh in Meshes)
        {
            var meshBounds = ComputeMeshBounds(mesh, world);

            if (bounds == null)
                bounds = meshBounds;
            
            else
                bounds.Value.Expand(meshBounds);
            
        }

        foreach (var child in Children)
        {
            var childBounds = child.ComputeBoundsRecursive(world);
            if (childBounds.HasValue)
            {
                if (bounds == null)
                    bounds = childBounds.Value;
                
                else
                    bounds.Value.Expand(childBounds.Value);
                
            }
        }

        return bounds;
    }

    private static BoundingBox ComputeMeshBounds(Mesh mesh, float[] worldMatrix)
    {
        var geometry = mesh.Geometry;
        var vertices = geometry.Vertices;
        var layout = geometry.Layout;
        var stride = layout.StrideFloats;

        var positionElement = FindPositionElement(layout);
        if (positionElement == null)
        {
            return new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        var positionOffset = positionElement.Value.OffsetFloats;

        BoundingBox bounds = new();
        bool firstVertex = true;

        for (int i = 0; i < vertices.Length; i += stride)
        {
            var px = vertices[i + positionOffset];
            var py = vertices[i + positionOffset + 1];
            var pz = vertices[i + positionOffset + 2];

            var worldPos = TransformPoint(worldMatrix, new Vector3(px, py, pz));

            if (firstVertex)
            {
                bounds = BoundingBox.FromPoint(worldPos);
                firstVertex = false;
            }
            else
            {
                bounds.Expand(worldPos);
            }
        }

        return bounds;
    }

    private static VertexElement? FindPositionElement(VertexLayout layout)
    {
        foreach (var element in layout.Elements)
        {
            if (element.Semantic == VertexElementSemantic.Position)
            {
                return element;
            }
        }
        return null;
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
        if (System.MathF.Abs(resultW - 1.0f) > float.Epsilon)
        {
            resultX /= resultW;
            resultY /= resultW;
            resultZ /= resultW;
        }

        return new Vector3(resultX, resultY, resultZ);
    }}