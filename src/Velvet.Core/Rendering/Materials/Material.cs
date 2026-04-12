using Velvet.Core.Rendering.Shaders;

namespace Velvet.Core.Rendering.Materials;

/// <summary>
/// Backward-compatible alias for the previous shader-driven Material type.
/// Use <see cref="ShaderMaterial"/> for all new code.
/// </summary>
[Obsolete("Use ShaderMaterial instead. This alias remains for compatibility during migration.")]
public sealed class Material
{
    private readonly ShaderMaterial _inner;

    public Material(IShader shader)
    {
        _inner = new ShaderMaterial(shader);
    }

    public void Set(string name, object value) => _inner.Set(name, value);

    public void Apply() => _inner.Apply();
}
