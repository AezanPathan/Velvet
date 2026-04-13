using System;
using System.ComponentModel;
using Velvet.Core.Rendering.Shaders;

namespace Velvet.Core.Rendering.Materials;

/// <summary>
/// Backward-compatible alias for the legacy shader-driven material type.
/// Use <see cref="ShaderMaterial"/> for shader property bags and
/// <see cref="Velvet.Core.Rendering.Material"/> for data-only mesh materials.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("Use ShaderMaterial or Velvet.Core.Rendering.Material. This alias remains for compatibility during migration.")]
public sealed class Material : ShaderMaterial
{
    public Material(IShader shader)
        : base(shader)
    {
    }
}
