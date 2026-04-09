using System.Collections.Generic;
using SceneModel = Velvet.Core.Scene.Scene;

namespace Velvet.Core.Rendering.Batching;

/// <summary>
/// Builds render batches from a scene's mesh instances.
/// Groups instances by (ShaderProgram, Material, VertexLayout) to minimize state changes.
/// </summary>
public static class RenderBatcher
{
    /// <summary>
    /// Creates batches from mesh instances.
    /// </summary>
    /// <param name="instances">All mesh instances to batch.</param>
    /// <param name="shaderProgram">The shader program to use (passed as object to avoid coupling).</param>
    /// <returns>List of batches, each containing instances with matching rendering state.</returns>
    public static List<RenderBatch> BuildBatches(
        IReadOnlyList<MeshInstance> instances,
        object shaderProgram)
    {
        var batchMap = new Dictionary<BatchKey, RenderBatch>();

        foreach (var instance in instances)
        {
            var material = instance.Mesh.Material ?? Material.Default;
            var vertexLayout = instance.Mesh.Geometry.Layout;

            var key = new BatchKey(shaderProgram, material, vertexLayout);

            if (!batchMap.TryGetValue(key, out var batch))
            {
                batch = new RenderBatch(key);
                batchMap[key] = batch;
            }

            batch.Add(instance);
        }

        return new List<RenderBatch>(batchMap.Values);
    }

    /// <summary>
    /// Creates batches from a scene.
    /// </summary>
    public static List<RenderBatch> BuildBatches(SceneModel scene, object shaderProgram)
    {
        System.ArgumentNullException.ThrowIfNull(scene);
        return BuildBatches(scene.MeshInstances, shaderProgram);
    }
}
