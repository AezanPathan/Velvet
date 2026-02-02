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
        "out vec3 vWorldPos;\n" +
        "out vec2 vUV;\n" +
        "\n" +
        "void main() {\n" +
        "    vNormal = normalize(uNormalMatrix * aNormal);\n" +
        "    vWorldPos = (uModel * vec4(aPosition, 1.0)).xyz;\n" +
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
        "out vec3 vWorldPos;\n" +
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
        "    vWorldPos = (uModel * vec4(finalPosition, 1.0)).xyz;\n" +
        "    vNormal = normalize(uNormalMatrix * finalNormal);\n" +
        "    vUV = aUV;\n" +
        "    gl_Position = uProjection * uView * uModel * vec4(finalPosition, 1.0);\n" +
        "}\n";

    /// <summary>
    /// Standard fragment shader (used by both skinned and non-skinned variants).
    /// 
    /// Handles:
    /// - Phong lighting (directional, point, spot lights)
    /// - Texture sampling
    /// - Material properties (color, ambient, diffuse, unlit)
    /// </summary>
    public const string StandardFragmentShader = "#version 300 es\n" +
        "precision highp float;\n" +
        "\n" +
        "in vec2 vUV;\n" +
        "in vec3 vNormal;\n" +
        "in vec3 vWorldPos;\n" +
        "out vec4 outColor;\n" +
        "\n" +
        "uniform vec3 uMaterialColor;\n" +
        "uniform float uMaterialAmbient;\n" +
        "uniform float uMaterialDiffuse;\n" +
        "uniform float uMaterialUnlit;\n" +
        "uniform sampler2D uBaseColorTex;\n" +
        "uniform bool uHasTexture;\n" +
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
        "    vec3 baseColor = uHasTexture ? texture(uBaseColorTex, vUV).rgb : uMaterialColor;\n" +
        "    if (uMaterialUnlit > 0.5) {\n" +
        "        outColor = vec4(baseColor, 1.0);\n" +
        "        return;\n" +
        "    }\n" +
        "    vec3 N = normalize(vNormal);\n" +
        "    vec3 L = normalize(-uLightDirection);\n" +
        "    float diffuse = max(dot(N, L), 0.0) * uMaterialDiffuse;\n" +
        "    vec3 result = baseColor * (uMaterialAmbient + diffuse * uLightColor * uLightIntensity);\n" +
        "    \n" +
        "    // Point light\n" +
        "    vec3 toPointLight = uPointLightPosition - vWorldPos;\n" +
        "    float dist = length(toPointLight);\n" +
        "    float attenuation = 1.0 / (uPointLightConstant + uPointLightLinear * dist + uPointLightQuadratic * dist * dist);\n" +
        "    vec3 pointL = normalize(toPointLight);\n" +
        "    float pointDiff = max(dot(N, pointL), 0.0) * uMaterialDiffuse;\n" +
        "    result += baseColor * pointDiff * uPointLightColor * uPointLightIntensity * attenuation;\n" +
        "    \n" +
        "    // Spot light\n" +
        "    vec3 toSpotLight = uSpotLightPosition - vWorldPos;\n" +
        "    dist = length(toSpotLight);\n" +
        "    vec3 spotL = normalize(toSpotLight);\n" +
        "    float theta = dot(spotL, normalize(-uSpotLightDirection));\n" +
        "    float epsilon = cos(uSpotLightCutoff) - cos(uSpotLightOuterCutoff);\n" +
        "    float intensity = clamp((theta - cos(uSpotLightOuterCutoff)) / epsilon, 0.0, 1.0);\n" +
        "    attenuation = 1.0 / (uSpotLightConstant + uSpotLightLinear * dist + uSpotLightQuadratic * dist * dist);\n" +
        "    float spotDiff = max(dot(N, spotL), 0.0) * uMaterialDiffuse;\n" +
        "    result += baseColor * spotDiff * uSpotLightColor * uSpotLightIntensity * intensity * attenuation;\n" +
        "    \n" +
        "    outColor = vec4(result, 1.0);\n" +
        "}\n";
}
