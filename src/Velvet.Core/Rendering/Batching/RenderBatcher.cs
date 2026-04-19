using SceneModel = Velvet.Core.Scene.Scene;
using Velvet.Core.Rendering.Core;
using Velvet.Core.Rendering.Materials;
using Velvet.Core.Rendering.Meshes;
using DataMaterial = Velvet.Core.Rendering.Materials.Material;

namespace Velvet.Core.Rendering.Batching;

/// <summary>
/// Builds render batches from a scene's mesh instances.
/// </summary>
public static class RenderBatcher
{
    /// <summary>
    /// Creates batches from mesh instances.
    /// </summary>
    public static List<RenderBatch> BuildBatches( IReadOnlyList<MeshInstance> instances,IRenderProgram renderProgram)
    {
        var batchMap = new Dictionary<BatchKey, RenderBatch>();

        foreach (var instance in instances)
        {
            var material = instance.Mesh.Material ?? StandardMaterial.Default;
            var vertexLayout = instance.Mesh.Geometry.Layout;

            var key = new BatchKey(renderProgram, material, vertexLayout);

            if (!batchMap.TryGetValue(key, out var batch))
            {
                batch = new RenderBatch(key);
                batchMap[key] = batch;
            }

            batch.Add(instance);
        }

        return [.. batchMap.Values];
    }

    [Obsolete("Use BuildBatches(IReadOnlyList<MeshInstance>, IRenderProgram) for type-safe contracts.")]
    public static List<RenderBatch> BuildBatches(
        IReadOnlyList<MeshInstance> instances,
        object shaderProgram)
        => BuildBatches(instances, new ObjectRenderProgram(shaderProgram ?? throw new System.ArgumentNullException(nameof(shaderProgram))));

    /// <summary>
    /// Creates batches from a scene.
    /// </summary>
    public static List<RenderBatch> BuildBatches(SceneModel scene, IRenderProgram renderProgram)
    {
        System.ArgumentNullException.ThrowIfNull(scene);
        var instances = new List<MeshInstance>();
        scene.CollectMeshes(instances);
        return BuildBatches(instances, renderProgram);
    }
    
    [Obsolete("Use BuildBatches(Scene, IRenderProgram) for type-safe contracts.")]
    public static List<RenderBatch> BuildBatches(SceneModel scene, object shaderProgram)
        => BuildBatches(scene, new ObjectRenderProgram(shaderProgram ?? throw new System.ArgumentNullException(nameof(shaderProgram))));
}
