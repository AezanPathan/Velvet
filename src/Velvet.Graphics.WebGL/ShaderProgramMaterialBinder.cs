using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Velvet.Core.Rendering;

namespace Velvet.Graphics.WebGL;

/// <summary>
/// Encapsulates material + texture binding behavior for a shader program.
/// Keeps texture cache and uniform mapping separate from program orchestration.
/// </summary>
internal sealed class ShaderProgramMaterialBinder
{
    private readonly IWebGLBridge _bridge;
    private readonly int _programId;
    private readonly Dictionary<string, int> _textureCache = new();

    public ShaderProgramMaterialBinder(IWebGLBridge bridge, int programId)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _programId = programId;
    }

    public async Task SetMaterialAsync(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);

        var c = material.AlbedoColor;
        await _bridge.SetUniform3fAsync(_programId, "uBaseColor", c.X, c.Y, c.Z).ConfigureAwait(false);
        await _bridge.SetUniform1fAsync(_programId, "uAmbientStrength", material.AmbientStrength).ConfigureAwait(false);
        await _bridge.SetUniform1fAsync(_programId, "uMaterialUnlit", material.Unlit ? 1.0f : 0.0f).ConfigureAwait(false);

        var hasTex = !string.IsNullOrWhiteSpace(material.BaseColorTextureUri);
        await _bridge.SetUniform1bAsync(_programId, "uHasTexture", hasTex).ConfigureAwait(false);
        if (!hasTex)
        {
            return;
        }

        var uri = material.BaseColorTextureUri!;
        if (!_textureCache.TryGetValue(uri, out var texId))
        {
            texId = await _bridge.CreateTextureFromUrlAsync(uri).ConfigureAwait(false);
            _textureCache[uri] = texId;
        }

        await _bridge.BindTextureAsync(_programId, "uBaseColorTex", texId, 0).ConfigureAwait(false);
    }
}
