#version 300 es
precision mediump float;

in vec3 vColor;
in vec3 vNormal;
in vec3 vWorldPos;

out vec4 outColor;

uniform vec3 uMaterialColor;
uniform float uMaterialAmbient;
uniform float uMaterialDiffuse;
uniform float uMaterialUnlit;

uniform vec3 uLightDirection;
uniform vec3 uLightColor;
uniform float uLightIntensity;

uniform vec3 uPointLightPosition;
uniform vec3 uPointLightColor;
uniform float uPointLightIntensity;
uniform float uPointLightConstant;
uniform float uPointLightLinear;
uniform float uPointLightQuadratic;

uniform vec3 uSpotLightPosition;
uniform vec3 uSpotLightDirection;
uniform vec3 uSpotLightColor;
uniform float uSpotLightIntensity;
uniform float uSpotLightCutoff;
uniform float uSpotLightOuterCutoff;
uniform float uSpotLightConstant;
uniform float uSpotLightLinear;
uniform float uSpotLightQuadratic;

void main() {
    if (uMaterialUnlit > 0.5) {
        outColor = vec4(uMaterialColor, 1.0);
        return;
    }

    vec3 N = normalize(vNormal);
    vec3 L = normalize(-uLightDirection);
    float diff = max(dot(N, L), 0.0);
    vec3 diffuse = uMaterialColor * uLightColor * diff * uLightIntensity * uMaterialDiffuse;

    vec3 toPoint = uPointLightPosition - vWorldPos;
    float dist = length(toPoint);
    vec3 Lp = (dist > 0.0001) ? (toPoint / dist) : vec3(0.0, 0.0, 0.0);
    float diffP = max(dot(N, Lp), 0.0);
    float attenuation = 1.0 / (uPointLightConstant + uPointLightLinear * dist + uPointLightQuadratic * dist * dist);
    vec3 pointDiffuse = uMaterialColor * uPointLightColor * diffP * uPointLightIntensity * attenuation * uMaterialDiffuse;

    vec3 toSpot = uSpotLightPosition - vWorldPos;
    float distS = length(toSpot);
    vec3 Ls = (distS > 0.0001) ? (toSpot / distS) : vec3(0.0, 0.0, 0.0);
    float diffS = max(dot(N, Ls), 0.0);
    float attenuationS = 1.0 / (uSpotLightConstant + uSpotLightLinear * distS + uSpotLightQuadratic * distS * distS);

    vec3 spotDir = normalize(uSpotLightDirection);
    vec3 fromLight = (distS > 0.0001) ? normalize(vWorldPos - uSpotLightPosition) : vec3(0.0, 0.0, 0.0);
    float theta = dot(fromLight, spotDir);
    float innerCos = cos(uSpotLightCutoff);
    float outerCos = cos(uSpotLightOuterCutoff);
    float cone = smoothstep(outerCos, innerCos, theta);

    vec3 spotDiffuse = uMaterialColor * uSpotLightColor * diffS * uSpotLightIntensity * attenuationS * cone * uMaterialDiffuse;

    vec3 ambient = uMaterialAmbient * uMaterialColor;
    outColor = vec4(ambient + diffuse + pointDiffuse + spotDiffuse, 1.0);
}
