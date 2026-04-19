using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering.Bounds;
using Velvet.Core.Rendering.Resources;
using Velvet.Core.Rendering.Skinning;
using RenderingMaterial = Velvet.Core.Rendering.Materials.Material;

namespace Velvet.Core.Rendering.Meshes;

/// <summary>
/// Represents geometry uploaded to a GPU backend.
/// </summary>
public sealed class Mesh
{
    private readonly object _gate = new();

    private MeshGpuResources? _resources;
    private BoundingBox _localBounds;
    private bool _localBoundsComputed;

    public RenderingMaterial? Material { get; set; }

    public Skin? Skin { get; set; }

    public GeometryBase Geometry { get; }

    public Mesh(GeometryBase geometry)
    {
        Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
    }

    public BoundingBox LocalBounds
    {
        get
        {
            lock (_gate)
            {
                if (!_localBoundsComputed)
                {
                    _localBounds = ComputeLocalBounds(Geometry);
                    _localBoundsComputed = true;
                }

                return _localBounds;
            }
        }
    }


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

    public async ValueTask UploadAsync(IMeshUploader uploader, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uploader);

        lock (_gate)
        {
            if (_resources.HasValue) return;
        }

        var resources = await uploader.UploadAsync(Geometry, cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            _resources ??= resources;
        }
    }

    private static BoundingBox ComputeLocalBounds(GeometryBase geometry)
    {
        var vertices = geometry.Vertices;
        var layout = geometry.Layout;
        var stride = layout.StrideFloats;

        var positionOffset = -1;
        foreach (var element in layout.Elements)
        {
            if (element.Semantic == VertexElementSemantic.Position)
            {
                positionOffset = element.OffsetFloats;
                break;
            }
        }

        if (positionOffset < 0)
        {
            return new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        var minX = 0f;
        var minY = 0f;
        var minZ = 0f;
        var maxX = 0f;
        var maxY = 0f;
        var maxZ = 0f;
        var hasVertex = false;

        for (var i = 0; i < vertices.Length; i += stride)
        {
            var x = vertices[i + positionOffset];
            var y = vertices[i + positionOffset + 1];
            var z = vertices[i + positionOffset + 2];

            if (!hasVertex)
            {
                minX = maxX = x;
                minY = maxY = y;
                minZ = maxZ = z;
                hasVertex = true;
                continue;
            }

            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (z < minZ) minZ = z;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
            if (z > maxZ) maxZ = z;
        }

        if (!hasVertex)
        {
            return new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        return new BoundingBox(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
    }
}
