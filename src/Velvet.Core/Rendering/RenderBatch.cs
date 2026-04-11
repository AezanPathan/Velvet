using System.Collections.Generic;
using Velvet.Core.Rendering.Meshes;

namespace Velvet.Core.Rendering;

/// <summary>
/// Groups mesh instances that share the same rendering state (shader, material, vertex layout).
/// Enables efficient rendering by setting state once per batch instead of per mesh.
/// </summary>
public sealed class RenderBatch
{
    private readonly List<MeshInstance> _instances = new();

    public RenderBatch(BatchKey key)
    {
        Key = key;
    }

    /// <summary>
    /// The shared rendering state for all meshes in this batch.
    /// </summary>
    public BatchKey Key { get; }

    /// <summary>
    /// All mesh instances in this batch.
    /// Each instance has its own Model matrix but shares shader/material/layout.
    /// </summary>
    public IReadOnlyList<MeshInstance> Instances => _instances;

    /// <summary>
    /// Adds a mesh instance to this batch.
    /// </summary>
    internal void Add(MeshInstance instance)
    {
        _instances.Add(instance);
    }
}
