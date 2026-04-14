using System;
using System.Threading;
using System.Threading.Tasks;
using Velvet.Core.Geometry;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Resources;

namespace Velvet.Graphics.WebGL;

/// <summary>
/// WebGL-backed mesh uploader that delegates GPU upload to the Velvet JavaScript API via <see cref="IWebGLBridge"/>.
/// This type contains no JS/WebGL code; it only orchestrates calls through the bridge.
/// </summary>
public sealed class WebGLMeshUploader : IMeshUploader
{
    private readonly IWebGLBridge _bridge;

    public WebGLMeshUploader(IWebGLBridge bridge)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
    }

    public async ValueTask<MeshGpuResources> UploadAsync(GeometryBase geometry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        cancellationToken.ThrowIfCancellationRequested();

        // Velvet's current JS API returns a single mesh ID representing the uploaded GPU mesh resource.
        // This ID is backend-defined and opaque. In the WebGL backend it encapsulates vertex/index buffers + VAO.
        var meshId = await _bridge.CreateMeshAsync(geometry.Vertices, geometry.Indices, geometry.Layout.StrideFloats).ConfigureAwait(false);

        return new MeshGpuResources(
            VertexBufferId: new GpuBufferId(meshId),
            IndexBufferId: geometry.Indices is null ? null : new GpuBufferId(meshId)
        );
    }
}
