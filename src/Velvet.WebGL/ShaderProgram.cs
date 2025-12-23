using System;
using System.Threading.Tasks;

namespace Velvet.WebGL;

/// <summary>
/// A minimal shader program wrapper that hides program IDs and delegates work to an <see cref="IWebGLBridge"/>.
/// </summary>
public sealed class ShaderProgram
{
    private readonly IWebGLBridge _bridge;
    private readonly int _programId;

    private ShaderProgram(IWebGLBridge bridge, int programId)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _programId = programId;
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

        return new ShaderProgram(bridge, programId);
    }

    public static Task<ShaderProgram> CreateDefaultAsync(IWebGLBridge bridge)
        => CreateFromSourcesAsync(bridge, DefaultVertexShader, DefaultFragmentShader);

    public Task SetUniformMatrix4fvAsync(string name, float[] matrix)
        => _bridge.SetUniformMatrix4fvAsync(_programId, name, matrix);

    public Task SetUniformMatrix3fvAsync(string name, float[] matrix)
        => _bridge.SetUniformMatrix3fvAsync(_programId, name, matrix);

    public Task SetUniform3fAsync(string name, float x, float y, float z)
        => _bridge.SetUniform3fAsync(_programId, name, x, y, z);

    public Task SetUniform1fAsync(string name, float value)
        => _bridge.SetUniform1fAsync(_programId, name, value);

    public Task DrawMeshAsync(int meshId, int rendererId)
        => _bridge.DrawMeshAsync(meshId, _programId, rendererId);

    private const string DefaultVertexShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "layout(location = 0) in vec3 aPosition;\n" +
        "layout(location = 1) in vec3 aColor;\n" +
        "layout(location = 2) in vec3 aNormal;\n" +
        "\n" +
        "uniform mat4 uModel;\n" +
        "uniform mat4 uView;\n" +
        "uniform mat4 uProjection;\n" +
        "uniform mat3 uNormalMatrix;\n" +
        "\n" +
        "out vec3 vColor;\n" +
        "out vec3 vNormal;\n" +
        "\n" +
        "void main() {\n" +
        "    vColor = aColor;\n" +
        "    vNormal = normalize(uNormalMatrix * aNormal);\n" +
        "    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);\n" +
        "}\n";

    private const string DefaultFragmentShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "in vec3 vColor;\n" +
        "in vec3 vNormal;\n" +
        "out vec4 outColor;\n" +
        "\n" +
        "uniform vec3 uLightDirection;\n" +
        "uniform vec3 uLightColor;\n" +
        "uniform float uLightIntensity;\n" +
        "\n" +
        "void main() {\n" +
        "    vec3 N = normalize(vNormal);\n" +
        "    vec3 L = normalize(-uLightDirection);\n" +
        "    float diff = max(dot(N, L), 0.0);\n" +
        "    vec3 diffuse = vColor * uLightColor * diff * uLightIntensity;\n" +
        "    // Small ambient to avoid fully black faces\n" +
        "    vec3 ambient = 0.05 * vColor;\n" +
        "    outColor = vec4(ambient + diffuse, 1.0);\n" +
        "}\n";
}
