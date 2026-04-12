using System;
using System.Collections.Generic;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Batching;
using Velvet.Core.Rendering.Meshes;
using Scene = Velvet.Core.Scene.Scene;

namespace Velvet.Examples;

/// <summary>
/// Reference sample showing batching usage patterns.
/// This file is intentionally outside runtime assemblies.
/// </summary>
public static class BatchRenderingExample
{
    public static void ManualBatching(Scene scene, IRenderProgram renderProgram)
    {
        var instances = new List<MeshInstance>();
        scene.CollectMeshes(instances);

        List<RenderBatch> batches = RenderBatcher.BuildBatches(scene, renderProgram);
        Console.WriteLine($"Scene has {instances.Count} instances grouped into {batches.Count} batches");
    }
}
