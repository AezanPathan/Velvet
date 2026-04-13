using System;
using System.Threading;
using System.Threading.Tasks;
using Velvet.Core.Geometry;
using RenderingMaterial = Velvet.Core.Rendering.Material;

namespace Velvet.Core.Rendering.Meshes;

/// <summary>
/// Represents geometry uploaded to a GPU backend.
/// This type owns its <see cref="Geometry"/> and tracks GPU resource identifiers.
/// </summary>
public sealed class Mesh
{
    private readonly object _gate = new();

    private MeshGpuResources? _resources;

    public Mesh(GeometryBase geometry)
    {
        Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
    }

    /// <summary>
    /// Optional material assigned to this mesh.
    /// If null, renderers should use <see cref="RenderingMaterial.Default"/>.
    /// </summary>
    public RenderingMaterial? Material { get; set; }

    /// <summary>
    /// Optional skin for skeletal deformation (skinning).
    /// If set, the mesh has JOINTS_0 and WEIGHTS_0 vertex attributes.
    /// </summary>
    public Skin? Skin { get; set; }

    /// <summary>
    /// Source geometry (data-only, reusable across hosts).
    /// </summary>
    public GeometryBase Geometry { get; }

    /// <summary>
    /// True if this mesh has been uploaded by a backend.
    /// </summary>
    public bool IsUploaded
    {
        get
        {
            lock (_gate)
            {
                return _resources.HasValue;
            }
        }
    }

    /// <summary>
    /// GPU resource identifiers assigned by the backend after upload.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the mesh has not been uploaded yet.</exception>
    public MeshGpuResources Resources
    {
        get
        {
            lock (_gate)
            {
                return _resources ?? throw new InvalidOperationException("Mesh is not uploaded. Call UploadAsync(...) first.");
            }
        }
    }

    /// <summary>
    /// Uploads the mesh to the GPU using the provided uploader.
    /// Mesh does not contain any backend-specific code; it delegates to <see cref="IMeshUploader"/>.
    /// </summary>
    /// <remarks>
    /// If called multiple times, subsequent calls are no-ops once uploaded.
    /// </remarks>
    public async ValueTask UploadAsync(IMeshUploader uploader, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uploader);

        lock (_gate)
        {
            if (_resources.HasValue)
            {
                return;
            }
        }

        var resources = await uploader.UploadAsync(Geometry, cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            _resources ??= resources;
        }
    }
}
