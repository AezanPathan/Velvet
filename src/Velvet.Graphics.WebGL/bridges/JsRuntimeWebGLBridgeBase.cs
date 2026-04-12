using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Velvet.Graphics.WebGL;

/// <summary>
/// Shared IJSRuntime-based IWebGLBridge implementation for common WebGL API forwarding.
/// Host-specific initialization is implemented by derived bridge types.
/// </summary>
public abstract class JsRuntimeWebGLBridgeBase : IWebGLBridge
{
    protected JsRuntimeWebGLBridgeBase(IJSRuntime js)
    {
        Js = js ?? throw new ArgumentNullException(nameof(js));
    }

    protected IJSRuntime Js { get; }

    public abstract Task<int> InitWithElementAsync(object canvasElement);
    public abstract Task<int> InitWithIdAsync(string canvasId);

    public Task<int> CreateShaderAsync(string source, string type)
        => Js.InvokeAsync<int>("Velvet.createShader", source, type).AsTask();

    public Task<int> CreateProgramAsync()
        => Js.InvokeAsync<int>("Velvet.createProgram").AsTask();

    public Task AttachShaderAsync(int programId, int shaderId)
        => Js.InvokeVoidAsync("Velvet.attachShader", programId, shaderId).AsTask();

    public Task LinkProgramAsync(int programId)
        => Js.InvokeVoidAsync("Velvet.linkProgram", programId).AsTask();

    public Task<int> CreateMeshAsync(float[] vertices, uint[]? indices = null, int vertexStrideFloats = 0)
        => Js.InvokeAsync<int>("Velvet.createMesh", vertices, indices, vertexStrideFloats).AsTask();

    public Task<int> CreateParticleMeshAsync(int capacity)
        => Js.InvokeAsync<int>("Velvet.createParticleMesh", capacity).AsTask();

    public Task UpdateMeshVerticesAsync(int meshId, float[] vertices, int vertexCount)
        => Js.InvokeVoidAsync("Velvet.updateMeshVertices", meshId, vertices, vertexCount).AsTask();

    public Task DrawMeshAsync(int meshId, int programId, int rendererId)
        => Js.InvokeVoidAsync("Velvet.drawMesh", meshId, programId, rendererId).AsTask();

    public Task ClearAsync(int rendererId, float r, float g, float b, float a)
        => Js.InvokeVoidAsync("Velvet.clear", rendererId, r, g, b, a).AsTask();

    public Task SetBlendModeAsync(int rendererId, string mode)
        => Js.InvokeVoidAsync("Velvet.setBlendMode", rendererId, mode).AsTask();

    public Task SetUniformMatrix4fvAsync(int programId, string name, float[] matrix)
        => Js.InvokeVoidAsync("Velvet.setUniformMatrix4fv", programId, name, matrix).AsTask();

    public Task SetUniform3fAsync(int programId, string name, float x, float y, float z)
        => Js.InvokeVoidAsync("Velvet.setUniform3f", programId, name, x, y, z).AsTask();

    public Task SetUniform1fAsync(int programId, string name, float value)
        => Js.InvokeVoidAsync("Velvet.setUniform1f", programId, name, value).AsTask();

    public Task SetUniformMatrix3fvAsync(int programId, string name, float[] matrix)
        => Js.InvokeVoidAsync("Velvet.setUniformMatrix3fv", programId, name, matrix).AsTask();

    public Task SetUniform1iAsync(int programId, string name, int value)
        => Js.InvokeVoidAsync("Velvet.setUniform1i", programId, name, value).AsTask();

    public Task SetUniform1bAsync(int programId, string name, bool value)
        => Js.InvokeVoidAsync("Velvet.setUniform1b", programId, name, value).AsTask();

    public Task<int> CreateTextureFromUrlAsync(string url)
        => Js.InvokeAsync<int>("Velvet.createTextureFromUrl", url).AsTask();

    public Task<int> CreateCubemapTextureAsync(string[] faceUrls)
        => Js.InvokeAsync<int>("Velvet.createCubemapTexture", (object)faceUrls).AsTask();

    public Task BindTextureAsync(int programId, string samplerName, int textureId, int textureUnit)
        => Js.InvokeVoidAsync("Velvet.bindTextureById", programId, samplerName, textureId, textureUnit).AsTask();

    public Task BindCubemapTextureAsync(int programId, string samplerName, int textureId, int textureUnit)
        => Js.InvokeVoidAsync("Velvet.bindCubemapTextureById", programId, samplerName, textureId, textureUnit).AsTask();

    public Task ResizeAsync(int width, int height)
        => Js.InvokeVoidAsync("Velvet.resize", width, height).AsTask();

    public Task SetDepthMaskAsync(int rendererId, bool enabled)
        => Js.InvokeVoidAsync("Velvet.setDepthMask", rendererId, enabled).AsTask();
}
