namespace Velvet.Core.Rendering.Batching;

/// <summary>
/// Enables efficient rendering by setting state once per batch instead of per mesh.
/// </summary>
public sealed class RenderBatch
{
    private readonly List<int> _instanceIndices = [];

    public RenderBatch(BatchKey key)
    {
        Key = key;
    }

    public BatchKey Key { get; }

    public IReadOnlyList<int> InstanceIndices => _instanceIndices;

    internal void AddInstanceIndex(int instanceIndex)
    {
        _instanceIndices.Add(instanceIndex);
    }
}
