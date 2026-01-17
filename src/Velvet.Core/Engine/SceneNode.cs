using System;
using System.Collections.Generic;
using Velvet.Core.Math;
using Velvet.Core.Rendering;

namespace Velvet.Core.Engine;

/// <summary>
/// Node in the scene graph containing an optional mesh list and child nodes.
/// </summary>
public sealed class SceneNode
{
    private readonly float[] _localTransform;

    public SceneNode(float[] localTransform, IReadOnlyList<Mesh> meshes, IReadOnlyList<SceneNode> children, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(localTransform);
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentNullException.ThrowIfNull(children);
        if (localTransform.Length != 16) throw new ArgumentException("Local transform must be a 4x4 matrix.", nameof(localTransform));

        _localTransform = (float[])localTransform.Clone();
        Meshes = meshes;
        Children = children;
        Name = name;
    }

    public string? Name { get; }

    public IReadOnlyList<Mesh> Meshes { get; }

    public IReadOnlyList<SceneNode> Children { get; }

    public float[] LocalTransform => (float[])_localTransform.Clone();

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
            output.Add(new MeshInstance(mesh, world));
        }

        foreach (var child in Children)
        {
            // Child inherits the world transform computed above.
            child.CollectMeshes(output, world);
        }
    }
}
