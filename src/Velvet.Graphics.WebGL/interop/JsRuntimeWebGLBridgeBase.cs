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
    public virtual Task<int> InitWithIdAsync(string canvasId)
    {
        ValidateRequiredText(canvasId, nameof(canvasId), "InitWithIdAsync requires a non-empty canvas id.");
        return Js.InvokeAsync<int>("Velvet.initById", canvasId).AsTask();
    }

    public Task<int> CreateShaderAsync(string source, string type)
    {
        ValidateRequiredText(source, nameof(source), "CreateShaderAsync requires non-empty shader source.");
        ValidateRequiredText(type, nameof(type), "CreateShaderAsync requires shader type 'vertex' or 'fragment'.");
        if (!string.Equals(type, "vertex", StringComparison.Ordinal) &&
            !string.Equals(type, "fragment", StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "CreateShaderAsync only supports 'vertex' or 'fragment'.");
        }

        return Js.InvokeAsync<int>("Velvet.createShader", source, type).AsTask();
    }

    public Task<int> CreateProgramAsync()
        => Js.InvokeAsync<int>("Velvet.createProgram").AsTask();

    public Task AttachShaderAsync(int programId, int shaderId)
        => Js.InvokeVoidAsync("Velvet.attachShader", programId, shaderId).AsTask();

    public Task LinkProgramAsync(int programId)
        => Js.InvokeVoidAsync("Velvet.linkProgram", programId).AsTask();

    public Task<int> CreateMeshAsync(float[] vertices, uint[]? indices = null, int vertexStrideFloats = 0)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        if (vertices.Length == 0)
        {
            throw new ArgumentException("CreateMeshAsync requires at least one vertex float.", nameof(vertices));
        }

        if (vertexStrideFloats < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(vertexStrideFloats), vertexStrideFloats, "CreateMeshAsync requires vertexStrideFloats >= 0.");
        }

        return Js.InvokeAsync<int>("Velvet.createMesh", vertices, indices, vertexStrideFloats).AsTask();
    }

    public Task<int> CreateParticleMeshAsync(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "CreateParticleMeshAsync requires capacity > 0.");
        }

        return Js.InvokeAsync<int>("Velvet.createParticleMesh", capacity).AsTask();
    }

    public Task UpdateMeshVerticesAsync(int meshId, float[] vertices, int vertexCount)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        if (vertexCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(vertexCount), vertexCount, "UpdateMeshVerticesAsync requires vertexCount >= 0.");
        }

        return Js.InvokeVoidAsync("Velvet.updateMeshVertices", meshId, vertices, vertexCount).AsTask();
    }

    public Task DrawMeshAsync(int meshId, int programId, int rendererId)
        => Js.InvokeVoidAsync("Velvet.drawMesh", meshId, programId, rendererId).AsTask();

    public Task ClearAsync(int rendererId, float r, float g, float b, float a)
        => Js.InvokeVoidAsync("Velvet.clear", rendererId, r, g, b, a).AsTask();

    public Task SetBlendModeAsync(int rendererId, string mode)
    {
        ValidateRequiredText(mode, nameof(mode), "SetBlendModeAsync requires a blend mode: 'off', 'alpha', or 'additive'.");
        if (!string.Equals(mode, "off", StringComparison.Ordinal) &&
            !string.Equals(mode, "alpha", StringComparison.Ordinal) &&
            !string.Equals(mode, "additive", StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "SetBlendModeAsync only supports 'off', 'alpha', or 'additive'.");
        }

        return Js.InvokeVoidAsync("Velvet.setBlendMode", rendererId, mode).AsTask();
    }

    public Task SetUniformMatrix4fvAsync(int programId, string name, float[] matrix)
    {
        ValidateUniformName(name, nameof(name));
        ValidateMatrixLength(matrix, nameof(matrix), expectedLength: 16, operation: "SetUniformMatrix4fvAsync");
        return Js.InvokeVoidAsync("Velvet.setUniformMatrix4fv", programId, name, matrix).AsTask();
    }

    public Task SetUniform3fAsync(int programId, string name, float x, float y, float z)
    {
        ValidateUniformName(name, nameof(name));
        return Js.InvokeVoidAsync("Velvet.setUniform3f", programId, name, x, y, z).AsTask();
    }

    public Task SetUniform1fAsync(int programId, string name, float value)
    {
        ValidateUniformName(name, nameof(name));
        return Js.InvokeVoidAsync("Velvet.setUniform1f", programId, name, value).AsTask();
    }

    public Task SetUniformMatrix3fvAsync(int programId, string name, float[] matrix)
    {
        ValidateUniformName(name, nameof(name));
        ValidateMatrixLength(matrix, nameof(matrix), expectedLength: 9, operation: "SetUniformMatrix3fvAsync");
        return Js.InvokeVoidAsync("Velvet.setUniformMatrix3fv", programId, name, matrix).AsTask();
    }

    public Task SetUniform1iAsync(int programId, string name, int value)
    {
        ValidateUniformName(name, nameof(name));
        return Js.InvokeVoidAsync("Velvet.setUniform1i", programId, name, value).AsTask();
    }

    public Task SetUniform1bAsync(int programId, string name, bool value)
    {
        ValidateUniformName(name, nameof(name));
        return Js.InvokeVoidAsync("Velvet.setUniform1b", programId, name, value).AsTask();
    }

    public Task<int> CreateTextureFromUrlAsync(string url)
    {
        ValidateRequiredText(url, nameof(url), "CreateTextureFromUrlAsync requires a non-empty URL.");
        return Js.InvokeAsync<int>("Velvet.createTextureFromUrl", url).AsTask();
    }

    public Task<int> CreateCubemapTextureAsync(string[] faceUrls)
    {
        ArgumentNullException.ThrowIfNull(faceUrls);
        if (faceUrls.Length != 6)
        {
            throw new ArgumentException("CreateCubemapTextureAsync requires exactly 6 face URLs.", nameof(faceUrls));
        }

        for (var i = 0; i < faceUrls.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(faceUrls[i]))
            {
                throw new ArgumentException($"CreateCubemapTextureAsync face URL at index {i} is null/empty.", nameof(faceUrls));
            }
        }

        return Js.InvokeAsync<int>("Velvet.createCubemapTexture", (object)faceUrls).AsTask();
    }

    public Task BindTextureAsync(int programId, string samplerName, int textureId, int textureUnit)
    {
        ValidateRequiredText(samplerName, nameof(samplerName), "BindTextureAsync requires a non-empty sampler uniform name.");
        if (textureUnit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(textureUnit), textureUnit, "BindTextureAsync requires textureUnit >= 0.");
        }

        return Js.InvokeVoidAsync("Velvet.bindTextureById", programId, samplerName, textureId, textureUnit).AsTask();
    }

    public Task BindCubemapTextureAsync(int programId, string samplerName, int textureId, int textureUnit)
    {
        ValidateRequiredText(samplerName, nameof(samplerName), "BindCubemapTextureAsync requires a non-empty sampler uniform name.");
        if (textureUnit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(textureUnit), textureUnit, "BindCubemapTextureAsync requires textureUnit >= 0.");
        }

        return Js.InvokeVoidAsync("Velvet.bindCubemapTextureById", programId, samplerName, textureId, textureUnit).AsTask();
    }

    public Task ResizeAsync(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "ResizeAsync requires width > 0.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "ResizeAsync requires height > 0.");
        }

        return Js.InvokeVoidAsync("Velvet.resize", width, height).AsTask();
    }

    public Task SetDepthMaskAsync(int rendererId, bool enabled)
        => Js.InvokeVoidAsync("Velvet.setDepthMask", rendererId, enabled).AsTask();

    private static void ValidateRequiredText(string value, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, paramName);
        }
    }

    private static void ValidateUniformName(string value, string paramName)
        => ValidateRequiredText(value, paramName, "Uniform name cannot be null, empty, or whitespace.");

    private static void ValidateMatrixLength(float[] matrix, string paramName, int expectedLength, string operation)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        if (matrix.Length != expectedLength)
        {
            throw new ArgumentException($"{operation} requires exactly {expectedLength} float values, received {matrix.Length}.", paramName);
        }
    }
}
