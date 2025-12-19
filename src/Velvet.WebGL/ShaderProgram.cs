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

    public Task DrawMeshAsync(int meshId, int rendererId)
        => _bridge.DrawMeshAsync(meshId, _programId, rendererId);

    private const string DefaultVertexShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "layout(location = 0) in vec3 aPosition;\n" +
        "layout(location = 1) in vec3 aColor;\n" +
        "\n" +
        "uniform mat4 uModel;\n" +
        "uniform mat4 uView;\n" +
        "uniform mat4 uProjection;\n" +
        "\n" +
        "out vec3 vColor;\n" +
        "\n" +
        "void main() {\n" +
        "    vColor = aColor;\n" +
        "    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);\n" +
        "}\n";

    private const string DefaultFragmentShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "in vec3 vColor;\n" +
        "out vec4 outColor;\n" +
        "\n" +
        "void main() {\n" +
        "    outColor = vec4(vColor, 1.0);\n" +
        "}\n";
}
