using Velvet.Core.Math;
using Velvet.Core.Rendering.Core;

namespace Velvet.Core.Rendering.Materials;

public sealed class StandardMaterial : Material
{
    public Vector3 AlbedoColor { get; set; }

    public float AmbientStrength { get; set; }

    public float DiffuseStrength { get; set; }

    public string? BaseColorTextureUri { get; set; }

    public StandardMaterial(Vector3 albedoColor, float ambientStrength, float diffuseStrength, bool unlit = false)
    {
        AlbedoColor = albedoColor;
        AmbientStrength = ambientStrength;
        DiffuseStrength = diffuseStrength;
        Unlit = unlit;
    }

    public static StandardMaterial Default { get; } = new(
        albedoColor: new Vector3(1.0f, 1.0f, 1.0f),
        ambientStrength: 0.05f,
        diffuseStrength: 1.0f,
        unlit: false);

    public override async Task ApplyAsync(IRenderProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);

        await ApplyBaseAsync(program).ConfigureAwait(false);

        var c = AlbedoColor;
        await program.SetUniform3fAsync("uBaseColor", c.X, c.Y, c.Z).ConfigureAwait(false);
        await program.SetUniform1fAsync("uAmbientStrength", AmbientStrength).ConfigureAwait(false);
        await program.SetUniform1fAsync("uDiffuseStrength", DiffuseStrength).ConfigureAwait(false);

        var hasTex = !string.IsNullOrWhiteSpace(BaseColorTextureUri);
        await program.SetUniform1bAsync("uHasTexture", hasTex).ConfigureAwait(false);

        if (!hasTex)
        {
            return;
        }

        await program.BindTextureAsync("uBaseColorTex", BaseColorTextureUri!, 0).ConfigureAwait(false);
    }
}
