using Velvet.Core.Geometry;

namespace Velvet.Core.Rendering;

/// <summary>
/// Identifies a unique rendering state for batching meshes together.
/// Meshes with the same BatchKey can be rendered sequentially with minimal state changes.
/// </summary>
public readonly struct BatchKey : System.IEquatable<BatchKey>
{
    public BatchKey(object shaderProgram, Material material, VertexLayout vertexLayout)
    {
        ShaderProgram = shaderProgram ?? throw new System.ArgumentNullException(nameof(shaderProgram));
        Material = material ?? throw new System.ArgumentNullException(nameof(material));
        VertexLayout = vertexLayout ?? throw new System.ArgumentNullException(nameof(vertexLayout));
    }

    /// <summary>
    /// The shader program used for rendering.
    /// Stored as object to avoid coupling to WebGL-specific types.
    /// </summary>
    public object ShaderProgram { get; }

    /// <summary>
    /// Material defining appearance (color, lighting properties).
    /// </summary>
    public Material Material { get; }

    /// <summary>
    /// Vertex layout describing buffer structure.
    /// </summary>
    public VertexLayout VertexLayout { get; }

    public bool Equals(BatchKey other)
        => ShaderProgram.Equals(other.ShaderProgram)
        && Material.Equals(other.Material)
        && VertexLayout.Equals(other.VertexLayout);

    public override bool Equals(object? obj)
        => obj is BatchKey other && Equals(other);

    public override int GetHashCode()
        => System.HashCode.Combine(ShaderProgram, Material, VertexLayout);

    public static bool operator ==(BatchKey left, BatchKey right)
        => left.Equals(right);

    public static bool operator !=(BatchKey left, BatchKey right)
        => !left.Equals(right);
}
