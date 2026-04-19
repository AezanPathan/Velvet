using Velvet.Core.Geometry;
using Velvet.Core.Rendering.Core;
using DataMaterial = Velvet.Core.Rendering.Materials.Material;

namespace Velvet.Core.Rendering.Batching;

/// <summary>
/// <summary>
/// Key for grouping meshes that can be rendered together efficiently.
/// </summary>
public readonly struct BatchKey : IEquatable<BatchKey>
{
    public BatchKey(IRenderProgram renderProgram, DataMaterial material, VertexLayout vertexLayout)
    {
        RenderProgram = renderProgram ?? throw new System.ArgumentNullException(nameof(renderProgram));
        Material = material ?? throw new System.ArgumentNullException(nameof(material));
        VertexLayout = vertexLayout ?? throw new System.ArgumentNullException(nameof(vertexLayout));
    }

    /// <summary>
    /// Compatibility constructor for older object-based callers.
    /// </summary>
    [Obsolete("Use BatchKey(IRenderProgram, Velvet.Core.Rendering.Materials.Material, VertexLayout) for type-safe batching contracts.")]
    public BatchKey(object shaderProgram, DataMaterial material, VertexLayout vertexLayout)
        : this(new ObjectRenderProgram(shaderProgram ?? throw new System.ArgumentNullException(nameof(shaderProgram))), material, vertexLayout)
    {
    }

    public IRenderProgram RenderProgram { get; }

    [Obsolete("Use RenderProgram.")]
    public object ShaderProgram => RenderProgram;

    public DataMaterial Material { get; }

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

internal sealed class ObjectRenderProgram : IRenderProgram, IEquatable<ObjectRenderProgram>
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

    public Task SetUniformMatrix4fvAsync(string name, float[] matrix)
        => throw new NotSupportedException("ObjectRenderProgram does not support uniform operations.");

    public Task SetUniform3fAsync(string name, float x, float y, float z)
        => throw new NotSupportedException("ObjectRenderProgram does not support uniform operations.");

    public Task SetUniform1fAsync(string name, float value)
        => throw new NotSupportedException("ObjectRenderProgram does not support uniform operations.");

    public Task SetUniform1iAsync(string name, int value)
        => throw new NotSupportedException("ObjectRenderProgram does not support uniform operations.");

    public Task SetUniform1bAsync(string name, bool value)
        => throw new NotSupportedException("ObjectRenderProgram does not support uniform operations.");

    public Task BindTextureAsync(string samplerUniform, string textureUri, int textureUnit)
        => throw new NotSupportedException("ObjectRenderProgram does not support texture binding.");
}
