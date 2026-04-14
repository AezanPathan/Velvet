namespace Velvet.Core.Rendering.Resources;

/// <summary>
/// GPU resource identifiers associated with a mesh.
/// </summary>
public readonly record struct MeshGpuResources(
    GpuBufferId VertexBufferId,
    GpuBufferId? IndexBufferId
);
