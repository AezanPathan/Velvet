using Microsoft.JSInterop;
using Velvet.Core.Rendering.Core;

namespace Velvet.Graphics.WebGL;

/// <summary>
/// A minimal shader program wrapper that hides program IDs and delegates work to an <see cref="IWebGLBridge"/>.
/// 
/// Supports both standard and skinned rendering pipelines.
/// </summary>
public sealed class ShaderProgram : IRenderProgram
{
    private readonly IWebGLBridge _bridge;
    private readonly int _programId;
    private readonly Dictionary<string, int> _textureCache = new();
    private readonly HashSet<string> _missingUniforms = new(StringComparer.Ordinal);
    private bool _hasBonesSupport = false;

    private ShaderProgram(IWebGLBridge bridge, int programId, bool hasBonesSupport = false)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _programId = programId;
        _hasBonesSupport = hasBonesSupport;
    }

    /// <summary>
    /// Gets the internal program ID for advanced operations.
    /// </summary>
    public int ProgramId => _programId;

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

    public static Task<ShaderProgram> CreateSkyboxAsync(IWebGLBridge bridge)
        => CreateFromSourcesAsync(bridge, ShaderSources.SkyboxVertexShader, ShaderSources.SkyboxFragmentShader);

    public Task SetUniformMatrix4fvAsync(string name, float[] matrix)
        => SetUniformSafeAsync(name, () => _bridge.SetUniformMatrix4fvAsync(_programId, name, matrix));

    public Task SetUniformMatrix3fvAsync(string name, float[] matrix)
        => SetUniformSafeAsync(name, () => _bridge.SetUniformMatrix3fvAsync(_programId, name, matrix));

    public Task SetUniform3fAsync(string name, float x, float y, float z)
        => SetUniformSafeAsync(name, () => _bridge.SetUniform3fAsync(_programId, name, x, y, z));

    public Task SetUniform1fAsync(string name, float value)
        => SetUniformSafeAsync(name, () => _bridge.SetUniform1fAsync(_programId, name, value));

    public Task SetUniform1iAsync(string name, int value)
        => SetUniformSafeAsync(name, () => _bridge.SetUniform1iAsync(_programId, name, value));

    public Task SetUniform1bAsync(string name, bool value)
        => SetUniformSafeAsync(name, () => _bridge.SetUniform1bAsync(_programId, name, value));

    public async Task BindTextureAsync(string samplerUniform, string textureUri, int textureUnit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(samplerUniform);
        ArgumentException.ThrowIfNullOrWhiteSpace(textureUri);
        if (textureUnit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(textureUnit), "Texture unit must be >= 0.");
        }

        if (!_textureCache.TryGetValue(textureUri, out var textureId))
        {
            textureId = await _bridge.CreateTextureFromUrlAsync(textureUri).ConfigureAwait(false);
            _textureCache[textureUri] = textureId;
        }

        await SetUniformSafeAsync(
            samplerUniform,
            () => _bridge.BindTextureAsync(_programId, samplerUniform, textureId, textureUnit)).ConfigureAwait(false);
    }

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
        await SetUniformSafeAsync("uBoneCount", () => _bridge.SetUniform1iAsync(_programId, "uBoneCount", boneCount))
            .ConfigureAwait(false);
    }

    public Task DrawMeshAsync(int meshId, int rendererId)
        => _bridge.DrawMeshAsync(meshId, _programId, rendererId);

    private async Task SetUniformSafeAsync(string uniformName, Func<Task> setter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uniformName);
        ArgumentNullException.ThrowIfNull(setter);

        if (_missingUniforms.Contains(uniformName))
        {
            return;
        }

        try
        {
            await setter().ConfigureAwait(false);
        }
        catch (JSException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            _missingUniforms.Add(uniformName);
        }
    }
}
