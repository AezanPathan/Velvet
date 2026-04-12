using Velvet.Core.Geometry;

namespace Velvet.Core.Rendering;

/// <summary>
/// Identifies a unique rendering state for batching meshes together.
/// Meshes with the same BatchKey can be rendered sequentially with minimal state changes.
/// </summary>
public readonly struct BatchKey : System.IEquatable<BatchKey>
{
    public BatchKey(IRenderProgram renderProgram, Material material, VertexLayout vertexLayout)
    {
        RenderProgram = renderProgram ?? throw new System.ArgumentNullException(nameof(renderProgram));
        Material = material ?? throw new System.ArgumentNullException(nameof(material));
        VertexLayout = vertexLayout ?? throw new System.ArgumentNullException(nameof(vertexLayout));
    }

    /// <summary>
    /// Compatibility constructor for older object-based callers.
    /// </summary>
    [System.Obsolete("Use BatchKey(IRenderProgram, Material, VertexLayout) for type-safe batching contracts.")]
    public BatchKey(object shaderProgram, Material material, VertexLayout vertexLayout)
        : this(new ObjectRenderProgram(shaderProgram ?? throw new System.ArgumentNullException(nameof(shaderProgram))), material, vertexLayout)
    {
    }

    /// <summary>
    /// The backend render program used for this batch.
    /// </summary>
    public IRenderProgram RenderProgram { get; }

    /// <summary>
    /// Backward-compatible alias for older callers.
    /// </summary>
    [System.Obsolete("Use RenderProgram.")]
    public object ShaderProgram => RenderProgram;

    /// <summary>
    /// Material defining appearance (color, lighting properties).
    /// </summary>
    public Material Material { get; }

    /// <summary>
    /// Vertex layout describing buffer structure.
    /// </summary>
    public VertexLayout VertexLayout { get; }

    public bool Equals(BatchKey other)
        => RenderProgram.Equals(other.RenderProgram)
        && Material.Equals(other.Material)
        && VertexLayout.Equals(other.VertexLayout);

    public override bool Equals(object? obj)
        => obj is BatchKey other && Equals(other);

    public override int GetHashCode()
        => System.HashCode.Combine(RenderProgram, Material, VertexLayout);

    public static bool operator ==(BatchKey left, BatchKey right)
        => left.Equals(right);

    public static bool operator !=(BatchKey left, BatchKey right)
        => !left.Equals(right);
}

internal sealed class ObjectRenderProgram : IRenderProgram, System.IEquatable<ObjectRenderProgram>
{
    private readonly object _value;

    public ObjectRenderProgram(object value)
    {
        _value = value ?? throw new System.ArgumentNullException(nameof(value));
    }

    public bool Equals(ObjectRenderProgram? other)
        => other is not null && _value.Equals(other._value);

    public override bool Equals(object? obj)
        => obj is ObjectRenderProgram other && Equals(other);

    public override int GetHashCode()
        => _value.GetHashCode();
}
