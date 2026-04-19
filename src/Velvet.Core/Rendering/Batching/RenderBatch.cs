using Velvet.Core.Rendering.Meshes;

namespace Velvet.Core.Rendering.Batching;

/// <summary>
/// Enables efficient rendering by setting state once per batch instead of per mesh.
/// </summary>
public sealed class RenderBatch
{
    private readonly List<MeshInstance> _instances = [];

    public RenderBatch(BatchKey key)
    {
        Key = key;
    }

    public BatchKey Key { get; }

    public IReadOnlyList<MeshInstance> Instances => _instances;

    internal void Add(MeshInstance instance)
    {
        _instances.Add(instance);
    }
}
