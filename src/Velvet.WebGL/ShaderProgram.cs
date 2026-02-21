using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Velvet.Core.Rendering;

namespace Velvet.WebGL;

/// <summary>
/// A minimal shader program wrapper that hides program IDs and delegates work to an <see cref="IWebGLBridge"/>.
/// 
/// Supports both standard and skinned rendering pipelines.
/// </summary>
public sealed class ShaderProgram
{
    private readonly IWebGLBridge _bridge;
    private readonly int _programId;
    private readonly Dictionary<string, int> _textureCache = new();
    private bool _hasBonesSupport = false;

    private ShaderProgram(IWebGLBridge bridge, int programId, bool hasBonesSupport = false)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _programId = programId;
        _hasBonesSupport = hasBonesSupport;
    }

    public static async Task<ShaderProgram> CreateFromSourcesAsync(IWebGLBridge bridge, string vertexSource, string fragmentSource)
    {
        ArgumentNullException.ThrowIfNull(bridge);
        ArgumentNullException.ThrowIfNull(vertexSource);
        ArgumentNullException.ThrowIfNull(fragmentSource);

        var vsId = await bridge.CreateShaderAsync(vertexSource, "vertex").ConfigureAwait(false);
        var fsId = await bridge.CreateShaderAsync(fragmentSource, "fragment").ConfigureAwait(false);

        var programId = await bridge.CreateProgramAsync().ConfigureAwait(false);
        await bridge.AttachShaderAsync(programId, vsId).ConfigureAwait(false);
        await bridge.AttachShaderAsync(programId, fsId).ConfigureAwait(false);
        await bridge.LinkProgramAsync(programId).ConfigureAwait(false);

        // Detect if shader supports bones by checking for uBoneMatrices uniform
        bool hasBonesSupport = vertexSource.Contains("uBoneMatrices");
        return new ShaderProgram(bridge, programId, hasBonesSupport);
    }

    public static Task<ShaderProgram> CreateDefaultAsync(IWebGLBridge bridge)
        => CreateFromSourcesAsync(bridge, ShaderSources.StandardVertexShader, ShaderSources.StandardFragmentShader);

    public static Task<ShaderProgram> CreateSkinnedAsync(IWebGLBridge bridge)
        => CreateFromSourcesAsync(bridge, ShaderSources.SkinnedVertexShader, ShaderSources.StandardFragmentShader);

    public static Task<ShaderProgram> CreateParticlesAsync(IWebGLBridge bridge)
        => CreateFromSourcesAsync(bridge, ShaderSources.ParticleVertexShader, ShaderSources.ParticleFragmentShader);

    public Task SetUniformMatrix4fvAsync(string name, float[] matrix)
        => _bridge.SetUniformMatrix4fvAsync(_programId, name, matrix);

    public Task SetUniformMatrix3fvAsync(string name, float[] matrix)
        => _bridge.SetUniformMatrix3fvAsync(_programId, name, matrix);

    public Task SetUniform3fAsync(string name, float x, float y, float z)
        => _bridge.SetUniform3fAsync(_programId, name, x, y, z);

    public Task SetUniform1fAsync(string name, float value)
        => _bridge.SetUniform1fAsync(_programId, name, value);

    /// <summary>
    /// Sets an array of bone matrices for GPU skinning.
    /// This should be called each frame for skinned meshes.
    /// </summary>
    public async Task SetBoneMatricesAsync(float[] boneMatrices, int boneCount)
    {
        if (!_hasBonesSupport)
        {
            return; // Shader doesn't support skinning
        }

        ArgumentNullException.ThrowIfNull(boneMatrices);
        if (boneCount < 0 || boneCount > 64)
        {
            throw new ArgumentException("Bone count must be between 0 and 64.", nameof(boneCount));
        }

        if (boneMatrices.Length < boneCount * 16)
        {
            throw new ArgumentException($"Bone matrices array must contain at least {boneCount * 16} floats.", nameof(boneMatrices));
        }

        // Set individual bone matrices
        for (int i = 0; i < boneCount; i++)
        {
            var matrix = new float[16];
            Array.Copy(boneMatrices, i * 16, matrix, 0, 16);
            await SetUniformMatrix4fvAsync($"uBoneMatrices[{i}]", matrix).ConfigureAwait(false);
        }

        // Set bone count uniform
        await _bridge.SetUniform1iAsync(_programId, "uBoneCount", boneCount).ConfigureAwait(false);
    }

    public async Task SetMaterialAsync(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);

        var c = material.AlbedoColor;
        await SetUniform3fAsync("uBaseColor", c.X, c.Y, c.Z).ConfigureAwait(false);
        await SetUniform1fAsync("uAmbientStrength", material.AmbientStrength).ConfigureAwait(false);
        await SetUniform1fAsync("uMaterialUnlit", material.Unlit ? 1.0f : 0.0f).ConfigureAwait(false);

        // Minimal texture support: baseColor texture
        var hasTex = !string.IsNullOrWhiteSpace(material.BaseColorTextureUri);
        System.Diagnostics.Debug.WriteLine($"[Material] BaseColorTextureUri = '{material.BaseColorTextureUri}', hasTex = {hasTex}");
        
        await _bridge.SetUniform1bAsync(_programId, "uHasTexture", hasTex).ConfigureAwait(false);
        if (hasTex)
        {
            var uri = material.BaseColorTextureUri!;
            if (!_textureCache.TryGetValue(uri, out var texId))
            {
                texId = await _bridge.CreateTextureFromUrlAsync(uri).ConfigureAwait(false);
                _textureCache[uri] = texId;
            }
            // Bind to texture unit 0 and set sampler
            await _bridge.BindTextureAsync(_programId, "uBaseColorTex", texId, 0).ConfigureAwait(false);
        }
    }

    public Task DrawMeshAsync(int meshId, int rendererId)
        => _bridge.DrawMeshAsync(meshId, _programId, rendererId);
}