using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Velvet.Graphics.WebGL;

/// <summary>
/// Blazor bridge implementation that talks to the Velvet JavaScript API via IJSRuntime.
/// This implementation intentionally avoids a compile-time dependency on Blazor types
/// (e.g. ElementReference) so the Velvet.WebGL project remains host-agnostic.
/// 
/// At runtime, Blazor should pass an ElementReference object; IJSRuntime will marshal it.
/// </summary>
public sealed class BlazorWebGLBridge : IWebGLBridge
{
    #region Fields

    private readonly IJSRuntime _js;

    #endregion

    #region Constructor

    public BlazorWebGLBridge(IJSRuntime js)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
    }

    #endregion

    #region Initialization

    /// <inheritdoc />
    /// <remarks>
    /// The parameter is declared as <see cref="object"/> to avoid a compile-time
    /// dependency on Microsoft.AspNetCore.Components. When calling from a Razor
    /// component, pass the ElementReference (e.g. `await Bridge.InitWithElementAsync(canvasRef)`).
    /// </remarks>
    public Task<int> InitWithElementAsync(object canvasElement)
    {
        if (canvasElement is null) throw new ArgumentNullException(nameof(canvasElement));

        // We intentionally do not type-check for ElementReference at compile-time.
        // If you want compile-time checks, add a reference to Microsoft.AspNetCore.Components and
        // change the parameter type to ElementReference.
        return _js.InvokeAsync<int>("Velvet.init", canvasElement).AsTask();
    }

    /// <inheritdoc />
    public Task<int> InitWithIdAsync(string canvasId)
    {
        // Blazor bridge intentionally does not support string id init.
        throw new NotSupportedException("BlazorWebGLBridge does not support InitWithIdAsync. Use InitWithElementAsync with an ElementReference.");
    }

    #endregion

    #region Resource creation / management

    public Task<int> CreateShaderAsync(string source, string type)
        => _js.InvokeAsync<int>("Velvet.createShader", source, type).AsTask();

    public Task<int> CreateProgramAsync()
        => _js.InvokeAsync<int>("Velvet.createProgram").AsTask();

    public Task AttachShaderAsync(int programId, int shaderId)
        => _js.InvokeVoidAsync("Velvet.attachShader", programId, shaderId).AsTask();

    public Task LinkProgramAsync(int programId)
        => _js.InvokeVoidAsync("Velvet.linkProgram", programId).AsTask();

    public Task<int> CreateMeshAsync(float[] vertices, uint[]? indices = null, int vertexStrideFloats = 0)
        => _js.InvokeAsync<int>("Velvet.createMesh", vertices, indices, vertexStrideFloats).AsTask();

    public Task<int> CreateParticleMeshAsync(int capacity)
        => _js.InvokeAsync<int>("Velvet.createParticleMesh", capacity).AsTask();

    public Task UpdateMeshVerticesAsync(int meshId, float[] vertices, int vertexCount)
        => _js.InvokeVoidAsync("Velvet.updateMeshVertices", meshId, vertices, vertexCount).AsTask();

    #endregion

    #region Rendering / state

    public Task DrawMeshAsync(int meshId, int programId, int rendererId)
        => _js.InvokeVoidAsync("Velvet.drawMesh", meshId, programId, rendererId).AsTask();

    public Task ClearAsync(int rendererId, float r, float g, float b, float a)
        => _js.InvokeVoidAsync("Velvet.clear", rendererId, r, g, b, a).AsTask();

    public Task SetBlendModeAsync(int rendererId, string mode)
        => _js.InvokeVoidAsync("Velvet.setBlendMode", rendererId, mode).AsTask();

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

    public Task<int> CreateCubemapTextureAsync(string[] faceUrls)
        => _js.InvokeAsync<int>("Velvet.createCubemapTexture", (object)faceUrls).AsTask();

    public Task BindTextureAsync(int programId, string samplerName, int textureId, int textureUnit)
        => _js.InvokeVoidAsync("Velvet.bindTextureById", programId, samplerName, textureId, textureUnit).AsTask();

    public Task BindCubemapTextureAsync(int programId, string samplerName, int textureId, int textureUnit)
        => _js.InvokeVoidAsync("Velvet.bindCubemapTextureById", programId, samplerName, textureId, textureUnit).AsTask();

    public Task ResizeAsync(int width, int height)
        => _js.InvokeVoidAsync("Velvet.resize", width, height).AsTask();

    public Task SetDepthMaskAsync(int rendererId, bool enabled)
        => _js.InvokeVoidAsync("Velvet.setDepthMask", rendererId, enabled).AsTask();

    #endregion
}
