using System.Threading;
using System.Threading.Tasks;
using Velvet.Core.Geometry;
using Velvet.Core.Rendering.Resources;

namespace Velvet.Core.Rendering;

/// <summary>
/// Backend abstraction for uploading mesh geometry to the GPU.
/// Implementations may use WebGL/JS interop, native APIs, etc.
/// </summary>
public interface IMeshUploader
{
    /// <summary>
    /// Uploads geometry and returns backend-specific GPU buffer identifiers.
    /// </summary>
    ValueTask<MeshGpuResources> UploadAsync(GeometryBase geometry, CancellationToken cancellationToken = default);
}
