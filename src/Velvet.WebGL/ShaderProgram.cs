using System;
using System.Threading.Tasks;
using Velvet.Core.Rendering;

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

    public Task SetMaterialAsync(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);

        var c = material.AlbedoColor;
        return Task.WhenAll(
            SetUniform3fAsync("uMaterialColor", c.X, c.Y, c.Z),
            SetUniform1fAsync("uMaterialAmbient", material.AmbientStrength),
            SetUniform1fAsync("uMaterialDiffuse", material.DiffuseStrength),
            SetUniform1fAsync("uMaterialUnlit", material.Unlit ? 1.0f : 0.0f));
    }

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
        "out vec3 vWorldPos;\n" +
        "\n" +
        "void main() {\n" +
        "    vColor = aColor;\n" +
        "    vNormal = normalize(uNormalMatrix * aNormal);\n" +
        "    vWorldPos = (uModel * vec4(aPosition, 1.0)).xyz;\n" +
        "    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);\n" +
        "}\n";

    private const string DefaultFragmentShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "in vec3 vColor;\n" +
        "in vec3 vNormal;\n" +
        "in vec3 vWorldPos;\n" +
        "out vec4 outColor;\n" +
        "\n" +
        "uniform vec3 uMaterialColor;\n" +
        "uniform float uMaterialAmbient;\n" +
        "uniform float uMaterialDiffuse;\n" +
        "uniform float uMaterialUnlit;\n" +
        "\n" +
        "uniform vec3 uLightDirection;\n" +
        "uniform vec3 uLightColor;\n" +
        "uniform float uLightIntensity;\n" +
        "\n" +
        "uniform vec3 uPointLightPosition;\n" +
        "uniform vec3 uPointLightColor;\n" +
        "uniform float uPointLightIntensity;\n" +
        "uniform float uPointLightConstant;\n" +
        "uniform float uPointLightLinear;\n" +
        "uniform float uPointLightQuadratic;\n" +
        "\n" +
        "uniform vec3 uSpotLightPosition;\n" +
        "uniform vec3 uSpotLightDirection;\n" +
        "uniform vec3 uSpotLightColor;\n" +
        "uniform float uSpotLightIntensity;\n" +
        "uniform float uSpotLightCutoff;\n" +
        "uniform float uSpotLightOuterCutoff;\n" +
        "uniform float uSpotLightConstant;\n" +
        "uniform float uSpotLightLinear;\n" +
        "uniform float uSpotLightQuadratic;\n" +
        "\n" +
        "void main() {\n" +
        "    if (uMaterialUnlit > 0.5) {\n" +
        "        outColor = vec4(uMaterialColor, 1.0);\n" +
        "        return;\n" +
        "    }\n" +
        "\n" +
        "    vec3 N = normalize(vNormal);\n" +
        "    vec3 L = normalize(-uLightDirection);\n" +
        "    float diff = max(dot(N, L), 0.0);\n" +
        "    vec3 diffuse = uMaterialColor * uLightColor * diff * uLightIntensity * uMaterialDiffuse;\n" +
        "\n" +
        "    vec3 toPoint = uPointLightPosition - vWorldPos;\n" +
        "    float dist = length(toPoint);\n" +
        "    vec3 Lp = (dist > 0.0001) ? (toPoint / dist) : vec3(0.0, 0.0, 0.0);\n" +
        "    float diffP = max(dot(N, Lp), 0.0);\n" +
        "    float attenuation = 1.0 / (uPointLightConstant + uPointLightLinear * dist + uPointLightQuadratic * dist * dist);\n" +
        "    vec3 pointDiffuse = uMaterialColor * uPointLightColor * diffP * uPointLightIntensity * attenuation * uMaterialDiffuse;\n" +
        "\n" +
        "    vec3 toSpot = uSpotLightPosition - vWorldPos;\n" +
        "    float distS = length(toSpot);\n" +
        "    vec3 Ls = (distS > 0.0001) ? (toSpot / distS) : vec3(0.0, 0.0, 0.0);\n" +
        "    float diffS = max(dot(N, Ls), 0.0);\n" +
        "    float attenuationS = 1.0 / (uSpotLightConstant + uSpotLightLinear * distS + uSpotLightQuadratic * distS * distS);\n" +
        "\n" +
        "    vec3 spotDir = normalize(uSpotLightDirection);\n" +
        "    vec3 fromLight = (distS > 0.0001) ? normalize(vWorldPos - uSpotLightPosition) : vec3(0.0, 0.0, 0.0);\n" +
        "    float theta = dot(fromLight, spotDir);\n" +
        "    float innerCos = cos(uSpotLightCutoff);\n" +
        "    float outerCos = cos(uSpotLightOuterCutoff);\n" +
        "    float cone = smoothstep(outerCos, innerCos, theta);\n" +
        "\n" +
        "    vec3 spotDiffuse = uMaterialColor * uSpotLightColor * diffS * uSpotLightIntensity * attenuationS * cone * uMaterialDiffuse;\n" +
        "\n" +
        "    vec3 ambient = uMaterialAmbient * uMaterialColor;\n" +
        "    outColor = vec4(ambient + diffuse + pointDiffuse + spotDiffuse, 1.0);\n" +
        "}\n";
}

