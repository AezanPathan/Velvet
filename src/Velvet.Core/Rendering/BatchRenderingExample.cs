using System;
using System.Collections.Generic;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Batching;
using Scene = Velvet.Core.Scene.Scene;

namespace Velvet.Examples;

/// <summary>
/// Example demonstrating batch rendering usage patterns.
/// NOTE: This file contains documentation examples and is not meant to be executed.
/// </summary>
public static class BatchRenderingExample
{
    /// <summary>
    /// Example 1: Automatic batching (recommended for most use cases).
    /// VelvetHost handles batching internally when you call StartAsync().
    /// </summary>
    /// <remarks>
    /// In your real code:
    /// <code>
    /// var host = await VelvetHost.CreateAsync(canvasRef, JS, ShaderProgram.CreateDefaultAsync);
    /// var scene = await GltfLoader.LoadScene(bytes);
    /// host.Add(scene);
    /// await host.StartAsync(OnFrameAsync);  // Batches built automatically
    /// </code>
    /// </remarks>
    public static void AutomaticBatchingExample()
    {
        // This is a documentation example showing the pattern.
        // VelvetHost now groups meshes by (Shader, Material, VertexLayout)
        // and renders them efficiently with minimal state changes.
        
        // See your existing ModelDemo.razor.cs for a working example.
    }

    /// <summary>
    /// Example 2: Manual batching for custom rendering pipelines.
    /// </summary>
    public static void ManualBatching(Scene scene, object shaderProgram)
    {
        // Build batches from the scene
        List<RenderBatch> batches = RenderBatcher.BuildBatches(scene, shaderProgram);

        Console.WriteLine($"Scene has {scene.MeshInstances.Count} instances grouped into {batches.Count} batches");

        // Inspect batch composition
        foreach (var batch in batches)
        {
            Console.WriteLine($"Batch: {batch.Instances.Count} instances, Material: {batch.Key.Material.AlbedoColor}");
        }
    }

    /// <summary>
    /// Example 3: Batch rendering pattern (pseudo-code for documentation).
    /// </summary>
    /// <remarks>
    /// This shows the conceptual pattern. In practice, VelvetHost handles this automatically.
    /// </remarks>
    public static void CustomRenderLoopPattern()
    {
        // Conceptual pattern (not executable code):
        //
        // foreach (var batch in batches)
        // {
        //     // Set batch-level state once per batch (major optimization!)
        //     await program.SetMaterialAsync(batch.Key.Material);
        //
        //     // Draw all instances in the batch
        //     foreach (var instance in batch.Instances)
        //     {
        //         // Set per-instance transforms
        //         await program.SetUniformMatrix4fvAsync("uModel", instance.ModelMatrix);
        //         await program.SetUniformMatrix3fvAsync("uNormalMatrix", instance.NormalMatrix);
        //
        //         // Draw
        //         var meshId = instance.Mesh.Resources.VertexBufferId.Value;
        //         await program.DrawMeshAsync(meshId, rendererId);
        //     }
        // }
    }

    /// <summary>
    /// Example 4: Understanding batch keys and grouping.
    /// </summary>
    public static void UnderstandingBatchKeys()
    {
        // Meshes are batched when they share the same:
        // 1. ShaderProgram (typically one per app)
        // 2. Material (color, lighting properties)
        // 3. VertexLayout (buffer structure: PositionColorNormal, etc.)

        // Example scene composition:
        //
        // 10 red cubes   → Batch 1 (same shader + red material + PositionColorNormal)
        // 5  red spheres → Batch 1 (merged! same shader + material + layout)
        // 8  blue cubes  → Batch 2 (different material)
        // 3  red cubes (PositionColor only) → Batch 3 (different layout)
        //
        // Result: 26 meshes rendered in 3 batches
        // Material state changes: 3 (instead of 26!)
    }
}
