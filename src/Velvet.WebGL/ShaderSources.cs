using System;

namespace Velvet.WebGL;

/// <summary>
/// Built-in shader sources for Velvet rendering.
/// 
/// These shaders support:
/// - Standard rendering with materials and textures
/// - GPU skinning (skeletal animation) via bone matrices
/// 
/// The skinned variant dynamically determines vertex interpolation based on joint weights.
/// </summary>
public static class ShaderSources
{
    /// <summary>
    /// Standard vertex shader (non-skinned models).
    /// 
    /// Inputs: position, normal, uv
    /// Uniforms: view, projection, model matrices + material/lighting
    /// </summary>
    public const string StandardVertexShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "layout(location = 0) in vec3 aPosition;\n" +
        "layout(location = 1) in vec3 aNormal;\n" +
        "layout(location = 2) in vec2 aUV;\n" +
        "\n" +
        "uniform mat4 uModel;\n" +
        "uniform mat4 uView;\n" +
        "uniform mat4 uProjection;\n" +
        "uniform mat3 uNormalMatrix;\n" +
        "\n" +
        "out vec3 vNormal;\n" +
        "out vec3 vPosition;\n" +
        "out vec2 vUV;\n" +
        "\n" +
        "void main() {\n" +
        "    vNormal = normalize(uNormalMatrix * aNormal);\n" +
        "    vPosition = (uModel * vec4(aPosition, 1.0)).xyz;\n" +
        "    vUV = aUV;\n" +
        "    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);\n" +
        "}\n";

    /// <summary>
    /// Skinned vertex shader for skeletal animation.
    /// 
    /// Additional inputs: joints (4 uint8), weights (4 floats)
    /// Additional uniforms: bone matrices array (up to 64 matrices)
    /// 
    /// Skinning formula: skinned_position = sum(boneMatrix[jointIndex[i]] * localPosition * weight[i])
    /// The vertex is transformed by a weighted combination of bone matrices based on joint indices and weights.
    /// </summary>
    public const string SkinnedVertexShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "layout(location = 0) in vec3 aPosition;\n" +
        "layout(location = 1) in vec3 aNormal;\n" +
        "layout(location = 2) in vec2 aUV;\n" +
        "layout(location = 3) in vec4 aJoints;  // 4 joint indices stored as floats (will cast to int)\n" +
        "layout(location = 4) in vec4 aWeights; // 4 float weights (should sum to 1.0)\n" +
        "\n" +
        "uniform mat4 uModel;\n" +
        "uniform mat4 uView;\n" +
        "uniform mat4 uProjection;\n" +
        "uniform mat3 uNormalMatrix;\n" +
        "uniform mat4 uBoneMatrices[64]; // Up to 64 bones\n" +
        "uniform int uBoneCount;\n" +
        "\n" +
        "out vec3 vNormal;\n" +
        "out vec3 vPosition;\n" +
        "out vec2 vUV;\n" +
        "\n" +
        "void main() {\n" +
        "    // Cast joint indices from float to int (data is uint8 stored as float)\n" +
        "    ivec4 joints = ivec4(aJoints);\n" +
        "    vec4 weights = aWeights;\n" +
        "    float totalWeight = weights.x + weights.y + weights.z + weights.w;\n" +
        "\n" +
        "    // Clamp joint indices to valid range\n" +
        "    joints = clamp(joints, 0, max(0, uBoneCount - 1));\n" +
        "\n" +
        "    vec3 finalPosition;\n" +
        "    vec3 finalNormal;\n" +
        "\n" +
        "    // If no skinning (totalWeight near zero), use original vertex data\n" +
        "    if (totalWeight < 0.01) {\n" +
        "        finalPosition = aPosition;\n" +
        "        finalNormal = aNormal;\n" +
        "    } else {\n" +
        "        // Compute skinned position: weighted sum of bone-transformed positions\n" +
        "        vec3 skinnedPos = vec3(0.0);\n" +
        "        vec3 skinnedNormal = vec3(0.0);\n" +
        "\n" +
        "        for (int i = 0; i < 4; i++) {\n" +
        "            if (weights[i] > 0.0) {\n" +
        "                int jointIdx = joints[i];\n" +
        "                mat4 boneMatrix = uBoneMatrices[jointIdx];\n" +
        "                vec4 skinnedPosH = boneMatrix * vec4(aPosition, 1.0);\n" +
        "                skinnedPos += weights[i] * skinnedPosH.xyz;\n" +
        "                // For normals, use only the 3x3 rotation part (upper-left of bone matrix)\n" +
        "                mat3 boneRotation = mat3(boneMatrix);\n" +
        "                skinnedNormal += weights[i] * boneRotation * aNormal;\n" +
        "            }\n" +
        "        }\n" +
        "        finalPosition = skinnedPos;\n" +
        "        finalNormal = skinnedNormal;\n" +
        "    }\n" +
        "\n" +
        "    // Transform to world space\n" +
        "    vPosition = (uModel * vec4(finalPosition, 1.0)).xyz;\n" +
        "    vNormal = normalize(uNormalMatrix * finalNormal);\n" +
        "    vUV = aUV;\n" +
        "    gl_Position = uProjection * uView * uModel * vec4(finalPosition, 1.0);\n" +
        "}\n";

    /// <summary>
    /// Standard fragment shader (used by both skinned and non-skinned variants).
    /// 
    /// Handles:
    /// - Minimal Lambert-style lighting (ambient + directional diffuse)
    /// - Optional base color texture sampling
    /// - Material base color and unlit toggle
    /// </summary>
    public const string StandardFragmentShader = "#version 300 es\n" +
        "precision highp float;\n" +
        "\n" +
        "in vec2 vUV;\n" +
        "in vec3 vNormal;\n" +
        "in vec3 vPosition;\n" +
        "out vec4 outColor;\n" +
        "\n" +
        "uniform vec3 uBaseColor;\n" +
        "uniform float uAmbientStrength;\n" +
        "uniform float uMaterialUnlit;\n" +
        "uniform sampler2D uBaseColorTex;\n" +
        "uniform bool uHasTexture;\n" +
        "\n" +
        "uniform vec3 uLightDirection;\n" +
        "uniform vec3 uLightColor;\n" +
        "\n" +
        "void main() {\n" +
        "    vec3 textureColor = uHasTexture ? texture(uBaseColorTex, vUV).rgb : vec3(1.0);\n" +
        "    vec3 baseColor = uBaseColor * textureColor;\n" +
        "    if (uMaterialUnlit > 0.5) {\n" +
        "        outColor = vec4(baseColor, 1.0);\n" +
        "        return;\n" +
        "    }\n" +
        "    vec3 N = normalize(vNormal);\n" +
        "    vec3 lightDir = -uLightDirection;\n" +
        "    float lightLen = length(lightDir);\n" +
        "    vec3 L = lightLen > 0.0001 ? (lightDir / lightLen) : vec3(0.0, 1.0, 0.0);\n" +
        "    float NdotL = max(dot(N, L), 0.0);\n" +
        "    vec3 ambient = baseColor * uAmbientStrength;\n" +
        "    vec3 diffuse = baseColor * NdotL * uLightColor;\n" +
        "    vec3 result = ambient + diffuse;\n" +
        "    outColor = vec4(result, 1.0);\n" +
        "}\n";

    /// <summary>
    /// Particle vertex shader.
    /// Inputs: position (vec3), size (float), color (vec4)
    /// Uniforms: view, projection matrices
    /// </summary>
    public const string ParticleVertexShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "layout(location = 0) in vec3 aPosition;\n" +
        "layout(location = 1) in float aSize;\n" +
        "layout(location = 2) in vec4 aColor;\n" +
        "\n" +
        "uniform mat4 uView;\n" +
        "uniform mat4 uProjection;\n" +
        "\n" +
        "out vec4 vColor;\n" +
        "\n" +
        "void main() {\n" +
        "    vColor = aColor;\n" +
        "    gl_PointSize = aSize;\n" +
        "    gl_Position = uProjection * uView * vec4(aPosition, 1.0);\n" +
        "}\n";

    /// <summary>
    /// Particle fragment shader.
    /// </summary>
    public const string ParticleFragmentShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "in vec4 vColor;\n" +
        "out vec4 outColor;\n" +
        "\n" +
        "void main() {\n" +
        "    outColor = vColor;\n" +
        "}\n";

    /// <summary>
    /// Skybox vertex shader.
    /// Removes translation from view matrix so skybox appears infinitely distant.
    /// Passes position as direction vector to fragment shader.
    /// </summary>
    public const string SkyboxVertexShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "layout(location = 0) in vec3 aPosition;\n" +
        "\n" +
        "uniform mat4 uView;\n" +
        "uniform mat4 uProjection;\n" +
        "\n" +
        "out vec3 vDirection;\n" +
        "\n" +
        "void main() {\n" +
        "    // Remove translation from view matrix by extracting rotation only\n" +
        "    mat4 viewRotation = mat4(mat3(uView));\n" +
        "    vec4 pos = uProjection * viewRotation * vec4(aPosition, 1.0);\n" +
        "    // Set depth to maximum (z = w) so skybox is always behind everything\n" +
        "    gl_Position = pos.xyww;\n" +
        "    vDirection = aPosition;\n" +
        "}\n";

    /// <summary>
    /// Skybox fragment shader.
    /// Supports both cubemap textures and gradient fallback.
    /// </summary>
    public const string SkyboxFragmentShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "in vec3 vDirection;\n" +
        "out vec4 outColor;\n" +
        "\n" +
        "uniform samplerCube u_Skybox;\n" +
        "uniform bool u_HasCubemap;\n" +
        "\n" +
        "void main() {\n" +
        "    if (u_HasCubemap) {\n" +
        "        // Sample cubemap texture\n" +
        "        outColor = texture(u_Skybox, vDirection);\n" +
        "    } else {\n" +
        "        // Simple gradient: blend between horizon and zenith colors based on y\n" +
        "        vec3 dir = normalize(vDirection);\n" +
        "        float t = dir.y * 0.5 + 0.5; // Map -1..1 to 0..1\n" +
        "        \n" +
        "        // Horizon color (bottom): light blue-gray\n" +
        "        vec3 horizonColor = vec3(0.5, 0.7, 0.9);\n" +
        "        // Zenith color (top): deeper blue\n" +
        "        vec3 zenithColor = vec3(0.2, 0.4, 0.8);\n" +
        "        \n" +
        "        vec3 color = mix(horizonColor, zenithColor, t);\n" +
        "        outColor = vec4(color, 1.0);\n" +
        "    }\n" +
        "}\n";
}
