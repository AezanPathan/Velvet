using Velvet.Core.Rendering.Core;

namespace Velvet.Core.Rendering.Materials;

/// <summary>
/// Base material contract for all renderable materials.
/// </summary>
public abstract class Material
{
    public bool Unlit { get; set; }

    protected Task ApplyBaseAsync(IRenderProgram program)
        => program.SetUniform1fAsync("uMaterialUnlit", Unlit ? 1f : 0f);

    public abstract Task ApplyAsync(IRenderProgram program);
}
