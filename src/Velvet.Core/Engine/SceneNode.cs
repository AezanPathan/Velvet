using System;
using System.Collections.Generic;
using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering;

namespace Velvet.Core.Engine;

/// <summary>
/// Node in the scene graph containing an optional mesh list and child nodes.
/// </summary>
public sealed class SceneNode
{
    private float[] _localTransform;

    public SceneNode(float[] localTransform, IReadOnlyList<Mesh> meshes, IReadOnlyList<SceneNode> children, string? name = null, Skin? skin = null, int nodeIndex = -1)
    {
        ArgumentNullException.ThrowIfNull(localTransform);
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentNullException.ThrowIfNull(children);
        if (localTransform.Length != 16) throw new ArgumentException("Local transform must be a 4x4 matrix.", nameof(localTransform));

        _localTransform = (float[])localTransform.Clone();
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

    public float[] LocalTransform => (float[])_localTransform.Clone();

    internal void SetLocalTransform(float[] localTransform)
    {
        ArgumentNullException.ThrowIfNull(localTransform);
        if (localTransform.Length != 16)
        {
            throw new ArgumentException("Local transform must be a 4x4 matrix.", nameof(localTransform));
        }

        _localTransform = (float[])localTransform.Clone();
    }

    /// <summary>
    /// Recursively flatten this node and children into mesh instances using inherited transforms.
    /// 
    /// Transform order (for column-major matrices in OpenGL/WebGL convention):
    /// world_transform = parent_world * node_local
    /// 
    /// This follows the standard scene graph hierarchy where child nodes inherit parent transforms.
    /// In GLSL, vertex positions are transformed as: gl_Position = projection * view * world * local_position
    /// </summary>
    internal void CollectMeshes(List<MeshInstance> output, float[] parentWorld)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(parentWorld);

        // Compute this node's world transform by multiplying parent world with local transform.
        // Matrix.Multiply(A, B) returns A * B in column-major form.
        var world = Matrix.Multiply(parentWorld, _localTransform);

        foreach (var mesh in Meshes)
        {
            output.Add(new MeshInstance(mesh, world, Skin));
        }

        foreach (var child in Children)
        {
            // Child inherits the world transform computed above.
            child.CollectMeshes(output, world);
        }
    }

    /// <summary>
    /// Recursively computes the world-space bounding box of this node and all its children.
    /// Transforms each vertex of each mesh by the accumulated world matrix.
    /// </summary>
    /// <param name="parentWorld">The parent node's world transformation matrix (column-major).</param>
    /// <returns>The world-space bounding box enclosing all geometry under this node, or null if no geometry exists.</returns>
    internal BoundingBox? ComputeLocalBounds(float[] parentWorld)
    {
        ArgumentNullException.ThrowIfNull(parentWorld);

        // Compute this node's world transform.
        var world = Matrix.Multiply(parentWorld, _localTransform);

        BoundingBox? bounds = null;

        // Compute bounds from all meshes at this node.
        foreach (var mesh in Meshes)
        {
            var meshBounds = ComputeMeshBounds(mesh, world);
            if (bounds == null)
            {
                bounds = meshBounds;
            }
            else
            {
                bounds.Value.Expand(meshBounds);
            }
        }

        // Recursively compute bounds from all children.
        foreach (var child in Children)
        {
            var childBounds = child.ComputeLocalBounds(world);
            if (childBounds.HasValue)
            {
                if (bounds == null)
                {
                    bounds = childBounds.Value;
                }
                else
                {
                    bounds.Value.Expand(childBounds.Value);
                }
            }
        }

        return bounds;
    }

    /// <summary>
    /// Computes the world-space bounding box for a single mesh given a world transformation.
    /// Transforms all vertex positions using the matrix and expands the bounding box.
    /// </summary>
    private static BoundingBox ComputeMeshBounds(Mesh mesh, float[] worldMatrix)
    {
        var geometry = mesh.Geometry;
        var vertices = geometry.Vertices;
        var layout = geometry.Layout;
        var stride = layout.StrideFloats;

        // Find the position element in the vertex layout.
        var positionElement = FindPositionElement(layout);
        if (positionElement == null)
        {
            // No position data; return empty bounds.
            return new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        var positionOffset = positionElement.Value.OffsetFloats;

        // Initialize bounds with the first vertex.
        BoundingBox bounds = new();
        bool firstVertex = true;

        for (int i = 0; i < vertices.Length; i += stride)
        {
            // Extract position from interleaved data.
            var px = vertices[i + positionOffset];
            var py = vertices[i + positionOffset + 1];
            var pz = vertices[i + positionOffset + 2];

            // Transform position by world matrix.
            var worldPos = TransformPoint(worldMatrix, new Vector3(px, py, pz));

            // Expand bounding box.
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

    /// <summary>
    /// Finds the position element in a vertex layout.
    /// </summary>
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

    /// <summary>
    /// Transforms a 3D point by a 4x4 column-major matrix (homogeneous division).
    /// </summary>
    private static Vector3 TransformPoint(float[] matrix, Vector3 point)
    {
        // For a column-major 4x4 matrix:
        // [m00 m10 m20 m30]   [x]     [m00*x + m10*y + m20*z + m30*w]
        // [m01 m11 m21 m31] * [y]  =  [m01*x + m11*y + m21*z + m31*w]
        // [m02 m12 m22 m32]   [z]     [m02*x + m12*y + m22*z + m32*w]
        // [m03 m13 m23 m33]   [w]     [m03*x + m13*y + m23*z + m33*w]
        //
        // Column-major indexing: matrix[row + col * 4]
        var x = point.X;
        var y = point.Y;
        var z = point.Z;
        var w = 1.0f;

        var resultX = matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12] * w;
        var resultY = matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13] * w;
        var resultZ = matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14] * w;
        var resultW = matrix[3] * x + matrix[7] * y + matrix[11] * z + matrix[15] * w;

        // Homogeneous division (only necessary if w != 1, but we include it for generality).
        if (System.MathF.Abs(resultW - 1.0f) > float.Epsilon)
        {
            resultX /= resultW;
            resultY /= resultW;
            resultZ /= resultW;
        }

        return new Vector3(resultX, resultY, resultZ);
    }}