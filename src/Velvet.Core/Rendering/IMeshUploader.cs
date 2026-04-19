using Velvet.Core.Geometry;
using Velvet.Core.Rendering.Resources;

namespace Velvet.Core.Rendering;

/// <summary>
/// Uploads geometry to GPU and returns backend resource handles.
/// </summary>
public interface IMeshUploader
{
    ValueTask<MeshGpuResources> UploadAsync(GeometryBase geometry, CancellationToken cancellationToken = default);
}
