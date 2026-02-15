using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Velvet.WebGL;

/// <summary>
/// Static bridge implementation that calls Velvet API using IJSRuntime.
/// Intended for static HTML / non-Blazor WASM hosts that only have canvas IDs.
/// Uses IJSRuntime so the same code works for many hosting scenarios.
/// </summary>
public sealed class StaticWebGLBridge : IWebGLBridge
{
    #region Fields

    private readonly IJSRuntime _js;

    #endregion

    #region Constructor

    /// <summary>
    /// Create a new StaticWebGLBridge.
    /// </summary>
    /// <param name="js">IJSRuntime instance (available in static WASM or other hosts).</param>
    public StaticWebGLBridge(IJSRuntime js)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
    }

    #endregion

    #region Initialization

    /// <inheritdoc />
    public async Task<int> InitWithIdAsync(string canvasId)
    {
        if (string.IsNullOrWhiteSpace(canvasId)) throw new ArgumentNullException(nameof(canvasId));
        // Call the Velvet.init(canvasId) convention in the demo (JS should accept either ID or element).
        return await _js.InvokeAsync<int>("Velvet.init", canvasId).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<int> InitWithElementAsync(object canvasElement)
    {
        // Static bridge does not accept ElementReference; host should call InitWithIdAsync.
        throw new NotSupportedException("StaticWebGLBridge supports initialization by canvas ID only. Use InitWithIdAsync.");
    }

    #endregion

    #region Resource creation / management

    /// <inheritdoc />
    public Task<int> CreateShaderAsync(string source, string type)
        => _js.InvokeAsync<int>("Velvet.createShader", source, type).AsTask();

    /// <inheritdoc />
    public Task<int> CreateProgramAsync()
        => _js.InvokeAsync<int>("Velvet.createProgram").AsTask();

    /// <inheritdoc />
    public Task AttachShaderAsync(int programId, int shaderId)
        => _js.InvokeVoidAsync("Velvet.attachShader", programId, shaderId).AsTask();

    /// <inheritdoc />
    public Task LinkProgramAsync(int programId)
        => _js.InvokeVoidAsync("Velvet.linkProgram", programId).AsTask();

    /// <inheritdoc />
    public Task<int> CreateMeshAsync(float[] vertices, uint[]? indices = null, int vertexStrideFloats = 0)
        => _js.InvokeAsync<int>("Velvet.createMesh", vertices, indices, vertexStrideFloats).AsTask();

    public Task<int> CreateParticleMeshAsync(int capacity)
        => _js.InvokeAsync<int>("Velvet.createParticleMesh", capacity).AsTask();

    public Task UpdateMeshVerticesAsync(int meshId, float[] vertices, int vertexCount)
        => _js.InvokeVoidAsync("Velvet.updateMeshVertices", meshId, vertices, vertexCount).AsTask();

    #endregion

    #region Rendering / state

    /// <inheritdoc />
    public Task DrawMeshAsync(int meshId, int programId, int rendererId)
        => _js.InvokeVoidAsync("Velvet.drawMesh", meshId, programId, rendererId).AsTask();

    /// <inheritdoc />
    public Task ClearAsync(int rendererId, float r, float g, float b, float a)
        => _js.InvokeVoidAsync("Velvet.clear", rendererId, r, g, b, a).AsTask();

    public Task SetBlendModeAsync(int rendererId, string mode)
        => _js.InvokeVoidAsync("Velvet.setBlendMode", rendererId, mode).AsTask();

    /// <inheritdoc />
    public Task SetUniformMatrix4fvAsync(int programId, string name, float[] matrix)
        => _js.InvokeVoidAsync("Velvet.setUniformMatrix4fv", programId, name, matrix).AsTask();

    public Task SetUniform3fAsync(int programId, string name, float x, float y, float z)
        => _js.InvokeVoidAsync("Velvet.setUniform3f", programId, name, x, y, z).AsTask();

    public Task SetUniform1fAsync(int programId, string name, float value)
        => _js.InvokeVoidAsync("Velvet.setUniform1f", programId, name, value).AsTask();

    public Task SetUniformMatrix3fvAsync(int programId, string name, float[] matrix)
        => _js.InvokeVoidAsync("Velvet.setUniformMatrix3fv", programId, name, matrix).AsTask();

    public Task SetUniform1iAsync(int programId, string name, int value)
        => _js.InvokeVoidAsync("Velvet.setUniform1i", programId, name, value).AsTask();

    public Task SetUniform1bAsync(int programId, string name, bool value)
        => _js.InvokeVoidAsync("Velvet.setUniform1b", programId, name, value).AsTask();

    public Task<int> CreateTextureFromUrlAsync(string url)
        => _js.InvokeAsync<int>("Velvet.createTextureFromUrl", url).AsTask();

    public Task BindTextureAsync(int programId, string samplerName, int textureId, int textureUnit)
        => _js.InvokeVoidAsync("Velvet.bindTextureById", programId, samplerName, textureId, textureUnit).AsTask();

    /// <inheritdoc />
    public Task ResizeAsync(int width, int height)
        => _js.InvokeVoidAsync("Velvet.resize", width, height).AsTask();

    #endregion
}
