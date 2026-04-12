using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using SceneModel = Velvet.Core.Scene.Scene;
using SceneNode = Velvet.Core.Scene.SceneNode;
using Velvet.Core.Animation;
using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Meshes;

namespace Velvet.Core.Assets.Gltf;

/// <summary>
/// Minimal CPU-side glTF 2.0 loader with scene graph support.
/// Prefers .glb but still understands .gltf with embedded base64 buffers.
/// </summary>
public static class GltfLoader
{
    public static async Task<SceneModel> LoadScene(byte[] data, string? baseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Yield immediately so browser regains control
        await Task.Yield();

        var gltf = LoadGltfDocument(data);
        using (gltf.Doc)
        {
            // Yield between heavy steps
            await Task.Yield();

            var accessors = gltf.Doc.RootElement.GetProperty("accessors");
            var bufferViews = gltf.Doc.RootElement.GetProperty("bufferViews");
            var skinsById = LoadSkins(gltf.Doc.RootElement, accessors, bufferViews, gltf.Bin);
            var meshesByIndex = LoadMeshesByIndex(gltf.Doc.RootElement, gltf.Bin, baseUrl);
            await Task.Yield();
            return BuildScene(gltf.Doc.RootElement, meshesByIndex, skinsById);
        }
    }

    public static async Task<(SceneModel Scene, List<AnimationClip> Animations)> LoadSceneWithAnimations(byte[] data, string? baseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Yield immediately so browser regains control
        await Task.Yield();

        var gltf = LoadGltfDocument(data);
        using (gltf.Doc)
        {
            // Yield between heavy steps
            await Task.Yield();

            var accessors = gltf.Doc.RootElement.GetProperty("accessors");
            var bufferViews = gltf.Doc.RootElement.GetProperty("bufferViews");
            var skinsById = LoadSkins(gltf.Doc.RootElement, accessors, bufferViews, gltf.Bin);
            var meshesByIndex = LoadMeshesByIndex(gltf.Doc.RootElement, gltf.Bin, baseUrl);
            await Task.Yield();

            var scene = BuildScene(gltf.Doc.RootElement, meshesByIndex, skinsById);
            var animations = LoadAnimations(gltf.Doc.RootElement, gltf.Bin);

            return (scene, animations);
        }
    }

    public static async Task<List<Mesh>> LoadMeshes(byte[] data)
    {
        var scene = await LoadScene(data);
        var unique = new HashSet<Mesh>();
        var meshes = new List<Mesh>();
        var instances = new List<MeshInstance>();

        scene.CollectMeshes(instances);

        foreach (var instance in instances)
        {
            if (unique.Add(instance.Mesh))
                meshes.Add(instance.Mesh);

        }

        return meshes;
    }

    private static (JsonDocument Doc, byte[] Bin) LoadGltfDocument(byte[] data)
    {
        if (IsGlb(data))
        {
            return LoadFromGlb(data);
        }

        // Demo fallback: embedded-base64 .gltf
        return LoadFromGltfJson(data);
    }

    private static (JsonDocument Doc, byte[] Bin) LoadFromGlb(byte[] glb)
    {
        var version = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(4, 4));
        if (version != 2)
            throw new NotSupportedException("Only glTF 2.0 supported.");

        int offset = 12;

        // JSON chunk
        uint jsonLength = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(offset, 4));
        uint jsonType = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(offset + 4, 4));
        offset += 8;

        if (jsonType != 0x4E4F534A)
            throw new InvalidDataException("Missing JSON chunk.");

        byte[] json = glb.AsSpan(offset, (int)jsonLength).ToArray();
        offset += (int)jsonLength;

        // BIN chunk
        uint binLength = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(offset, 4));
        uint binType = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(offset + 4, 4));
        offset += 8;

        if (binType != 0x004E4942)
            throw new InvalidDataException("Missing BIN chunk.");

        byte[] bin = glb.AsSpan(offset, (int)binLength).ToArray();

        return (JsonDocument.Parse(json), bin);
    }

    private static (JsonDocument Doc, byte[] Bin) LoadFromGltfJson(byte[] gltfJsonUtf8)
    {
        gltfJsonUtf8 = StripUtf8Bom(gltfJsonUtf8);
        var doc = JsonDocument.Parse(gltfJsonUtf8);
        var root = doc.RootElement;

        if (!root.TryGetProperty("asset", out var asset) || !asset.TryGetProperty("version", out var ver) || ver.GetString() != "2.0")
        {
            throw new NotSupportedException("Only glTF 2.0 is supported.");
        }

        var buffers = root.GetProperty("buffers");
        if (buffers.GetArrayLength() < 1) throw new InvalidDataException("glTF has no buffers.");

        var buffer0 = buffers[0];
        if (!buffer0.TryGetProperty("uri", out var uriEl))
        {
            throw new NotSupportedException(".gltf without embedded buffer URI is not supported in this minimal loader.");
        }

        var uri = uriEl.GetString() ?? string.Empty;
        const string prefix = "data:application/octet-stream;base64,";
        if (!uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("Only embedded base64 buffers are supported for .gltf in this demo loader.");
        }

        var base64 = uri.Substring(prefix.Length);
        var binBytes = Convert.FromBase64String(base64);

        return (doc, binBytes);
    }

    private static List<List<Mesh>> LoadMeshesByIndex(JsonElement root, byte[] bin, string? baseUrl = null)
    {
        var meshesOut = new List<List<Mesh>>();

        var meshes = root.GetProperty("meshes");
        var accessors = root.GetProperty("accessors");
        var bufferViews = root.GetProperty("bufferViews");

        int meshIdx = 0;
        foreach (var meshEl in meshes.EnumerateArray())
        {
            if (!meshEl.TryGetProperty("primitives", out var primitives) || primitives.ValueKind != JsonValueKind.Array)
            {
                meshesOut.Add(new List<Mesh>());
                meshIdx++;
                continue;
            }

            var meshPrimitives = new List<Mesh>();
            foreach (var prim in primitives.EnumerateArray())
            {
                var mesh = LoadPrimitive(prim, accessors, bufferViews, bin, root, baseUrl);
                if (mesh != null)
                {
                    meshPrimitives.Add(mesh);
                }
            }

            meshesOut.Add(meshPrimitives);
            meshIdx++;
        }

        return meshesOut;
    }

    private static SceneModel BuildScene(JsonElement root, List<List<Mesh>> meshesByIndex, Dictionary<int, Skin> skinsById)
    {
        if (!root.TryGetProperty("nodes", out var nodesEl) || nodesEl.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("glTF has no nodes.");
        }

        var activeScene = 0;
        if (root.TryGetProperty("scene", out var sceneEl))
        {
            activeScene = sceneEl.GetInt32();
        }

        var rootNodeIndices = new List<int>();
        if (root.TryGetProperty("scenes", out var scenesEl) && scenesEl.ValueKind == JsonValueKind.Array && scenesEl.GetArrayLength() > activeScene)
        {
            var sceneObj = scenesEl[activeScene];
            if (sceneObj.TryGetProperty("nodes", out var nodes) && nodes.ValueKind == JsonValueKind.Array)
            {
                foreach (var nodeIndexEl in nodes.EnumerateArray())
                {
                    rootNodeIndices.Add(nodeIndexEl.GetInt32());
                }
            }
        }

        // Fallback if scenes array is missing or empty.
        if (rootNodeIndices.Count == 0)
        {
            for (var i = 0; i < nodesEl.GetArrayLength(); i++)
            {
                rootNodeIndices.Add(i);
            }
        }

        var roots = new List<SceneNode>(rootNodeIndices.Count);
        foreach (var nodeIndex in rootNodeIndices)
        {
            roots.Add(BuildNode(nodeIndex, nodesEl, meshesByIndex, skinsById));
        }

        return new SceneModel(roots);
    }

    private static SceneNode BuildNode(int nodeIndex, JsonElement nodesEl, List<List<Mesh>> meshesByIndex, Dictionary<int, Skin> skinsById)
    {
        var nodeEl = nodesEl[nodeIndex];

        var localTransform = ReadNodeTransform(nodeEl);

        var meshes = Array.Empty<Mesh>();
        if (nodeEl.TryGetProperty("mesh", out var meshIndexEl))
        {
            var meshIndex = meshIndexEl.GetInt32();
            if (meshIndex >= 0 && meshIndex < meshesByIndex.Count)
            {
                meshes = meshesByIndex[meshIndex].ToArray();
            }
        }

        var children = new List<SceneNode>();
        if (nodeEl.TryGetProperty("children", out var childrenEl) && childrenEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var childIndexEl in childrenEl.EnumerateArray())
            {
                children.Add(BuildNode(childIndexEl.GetInt32(), nodesEl, meshesByIndex, skinsById));
            }
        }

        var name = nodeEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"Node_{nodeIndex}";
        }

        Skin? skin = null;
        if (nodeEl.TryGetProperty("skin", out var skinIndexEl))
        {
            var skinIndex = skinIndexEl.GetInt32();
            if (skinsById.TryGetValue(skinIndex, out var foundSkin))
            {
                skin = foundSkin;
                System.Diagnostics.Debug.WriteLine($"[LOADER] Attached skin {skinIndex} to node {nodeIndex} ({name})");
            }
        }

        return new SceneNode(localTransform, meshes, children, name, skin, nodeIndex);
    }

    private static List<AnimationClip> LoadAnimations(JsonElement root, byte[] bin)
    {
        var clips = new List<AnimationClip>();

        if (!root.TryGetProperty("animations", out var animationsEl) || animationsEl.ValueKind != JsonValueKind.Array)
        {
            return clips;
        }

        if (!root.TryGetProperty("nodes", out var nodesEl) || nodesEl.ValueKind != JsonValueKind.Array)
        {
            return clips;
        }

        var accessors = root.GetProperty("accessors");
        var bufferViews = root.GetProperty("bufferViews");

        var animationIndex = 0;
        foreach (var animationEl in animationsEl.EnumerateArray())
        {
            var clipName = animationEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(clipName))
            {
                clipName = $"Animation_{animationIndex}";
            }

            var channels = new List<AnimationChannel>();

            if (!animationEl.TryGetProperty("channels", out var channelsEl) || channelsEl.ValueKind != JsonValueKind.Array)
            {
                clips.Add(new AnimationClip(clipName!, channels));
                animationIndex++;
                continue;
            }

            if (!animationEl.TryGetProperty("samplers", out var samplersEl) || samplersEl.ValueKind != JsonValueKind.Array)
            {
                clips.Add(new AnimationClip(clipName!, channels));
                animationIndex++;
                continue;
            }

            foreach (var channelEl in channelsEl.EnumerateArray())
            {
                if (!channelEl.TryGetProperty("sampler", out var samplerIndexEl))
                {
                    continue;
                }

                var samplerIndex = samplerIndexEl.GetInt32();
                if (samplerIndex < 0 || samplerIndex >= samplersEl.GetArrayLength())
                {
                    continue;
                }

                if (!channelEl.TryGetProperty("target", out var targetEl) || targetEl.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (!targetEl.TryGetProperty("node", out var nodeIndexEl))
                {
                    continue;
                }

                var nodeIndex = nodeIndexEl.GetInt32();
                var nodeName = GetNodeName(nodesEl, nodeIndex);

                if (!targetEl.TryGetProperty("path", out var pathEl))
                {
                    continue;
                }

                var path = ParseAnimationPath(pathEl.GetString());
                if (path == AnimationPath.Weights)
                {
                    // Morph target weights are not supported in this node-based animation system.
                    continue;
                }

                var samplerEl = samplersEl[samplerIndex];
                var interpolation = ParseInterpolationMode(samplerEl);

                if (!samplerEl.TryGetProperty("input", out var inputEl) || !samplerEl.TryGetProperty("output", out var outputEl))
                {
                    continue;
                }

                var inputAccessor = inputEl.GetInt32();
                var outputAccessor = outputEl.GetInt32();

                var times = ReadAccessorFloatScalar(accessors, bufferViews, bin, inputAccessor, out var timeCount);
                if (timeCount == 0)
                {
                    continue;
                }

                var expectedComponents = GetComponentCountForPath(path);
                var outputValues = ReadAccessorFloatArray(accessors, bufferViews, bin, outputAccessor, expectedComponents, out var outputCount);

                var expectedOutputCount = interpolation == InterpolationMode.CubicSpline ? timeCount * 3 : timeCount;
                if (outputCount != expectedOutputCount)
                {
                    throw new InvalidDataException($"Animation sampler output count mismatch. Expected {expectedOutputCount}, got {outputCount}.");
                }

                var keyframes = new List<AnimationKeyframe>(timeCount);
                for (int i = 0; i < timeCount; i++)
                {
                    var values = ExtractKeyframeValues(outputValues, i, expectedComponents, interpolation);
                    keyframes.Add(new AnimationKeyframe(times[i], values));
                }

                var sampler = new AnimationSampler(keyframes, interpolation);
                channels.Add(new AnimationChannel(sampler, nodeName, path));
            }

            clips.Add(new AnimationClip(clipName!, channels));
            animationIndex++;
        }

        return clips;
    }

    private static string GetNodeName(JsonElement nodesEl, int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= nodesEl.GetArrayLength())
        {
            return $"Node_{nodeIndex}";
        }

        var nodeEl = nodesEl[nodeIndex];
        var name = nodeEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"Node_{nodeIndex}";
        }

        return name!;
    }

    private static AnimationPath ParseAnimationPath(string? path)
    {
        return path switch
        {
            "translation" => AnimationPath.Translation,
            "rotation" => AnimationPath.Rotation,
            "scale" => AnimationPath.Scale,
            "weights" => AnimationPath.Weights,
            _ => throw new NotSupportedException($"Unsupported animation path: {path}")
        };
    }

    private static InterpolationMode ParseInterpolationMode(JsonElement samplerEl)
    {
        if (samplerEl.TryGetProperty("interpolation", out var interpEl))
        {
            var interp = interpEl.GetString();
            return interp switch
            {
                "STEP" => InterpolationMode.Step,
                "LINEAR" => InterpolationMode.Linear,
                "CUBICSPLINE" => InterpolationMode.CubicSpline,
                _ => throw new NotSupportedException($"Unsupported interpolation mode: {interp}")
            };
        }

        return InterpolationMode.Linear;
    }

    private static int GetComponentCountForPath(AnimationPath path)
    {
        return path switch
        {
            AnimationPath.Translation => 3,
            AnimationPath.Rotation => 4,
            AnimationPath.Scale => 3,
            AnimationPath.Weights => 1,
            _ => throw new NotSupportedException($"Unsupported animation path: {path}")
        };
    }

    private static float[] ReadAccessorFloatScalar(JsonElement accessors, JsonElement bufferViews, byte[] bin, int accessorIndex, out int count)
    {
        var acc = accessors[accessorIndex];

        var componentType = acc.GetProperty("componentType").GetInt32();
        if (componentType != 5126)
        {
            throw new NotSupportedException("Only FLOAT accessors are supported for animation times.");
        }

        var type = acc.GetProperty("type").GetString();
        if (!string.Equals(type, "SCALAR", StringComparison.Ordinal))
        {
            throw new NotSupportedException("Only SCALAR accessors are supported for animation times.");
        }

        count = acc.GetProperty("count").GetInt32();

        var viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = bufferViews[viewIndex];

        var viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
        var accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
        var byteOffset = viewOffset + accOffset;

        var stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 4;

        var result = new float[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = BitConverter.ToSingle(bin, byteOffset + (i * stride));
        }

        return result;
    }

    private static float[] ReadAccessorFloatArray(JsonElement accessors, JsonElement bufferViews, byte[] bin, int accessorIndex, int componentCount, out int count)
    {
        var acc = accessors[accessorIndex];

        var componentType = acc.GetProperty("componentType").GetInt32();
        if (componentType != 5126)
        {
            throw new NotSupportedException("Only FLOAT accessors are supported for animation outputs.");
        }

        var type = acc.GetProperty("type").GetString();
        var expectedType = componentCount switch
        {
            1 => "SCALAR",
            2 => "VEC2",
            3 => "VEC3",
            4 => "VEC4",
            _ => throw new NotSupportedException($"Unsupported component count: {componentCount}")
        };

        if (!string.Equals(type, expectedType, StringComparison.Ordinal))
        {
            throw new NotSupportedException($"Expected accessor type {expectedType} but got {type}.");
        }

        count = acc.GetProperty("count").GetInt32();

        var viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = bufferViews[viewIndex];

        var viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
        var accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
        var byteOffset = viewOffset + accOffset;

        var stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : componentCount * 4;

        var result = new float[count * componentCount];
        for (var i = 0; i < count; i++)
        {
            var baseByte = byteOffset + (i * stride);
            for (var c = 0; c < componentCount; c++)
            {
                result[i * componentCount + c] = BitConverter.ToSingle(bin, baseByte + (c * 4));
            }
        }

        return result;
    }

    private static float[] ExtractKeyframeValues(float[] outputValues, int keyframeIndex, int componentCount, InterpolationMode interpolation)
    {
        var multiplier = interpolation == InterpolationMode.CubicSpline ? 3 : 1;
        var valuesPerKeyframe = componentCount * multiplier;
        var startIndex = keyframeIndex * valuesPerKeyframe;

        if (startIndex + valuesPerKeyframe > outputValues.Length)
        {
            throw new InvalidDataException("Animation output values are out of range for keyframe extraction.");
        }

        var values = new float[valuesPerKeyframe];
        Array.Copy(outputValues, startIndex, values, 0, valuesPerKeyframe);
        return values;
    }

    private static float[] ReadNodeTransform(JsonElement nodeEl)
    {
        // glTF 2.0 spec: node transformations can be provided as either:
        // 1. A 4x4 matrix (column-major, same layout as GPU matrices)
        // 2. Separate translation, rotation (quaternion), and scale (TRS)
        // If a matrix is present, it takes precedence and TRS is ignored.
        if (nodeEl.TryGetProperty("matrix", out var mEl) && mEl.ValueKind == JsonValueKind.Array && mEl.GetArrayLength() == 16)
        {
            // Read column-major matrix directly from glTF JSON.
            var m = new float[16];
            for (var i = 0; i < 16; i++)
            {
                m[i] = (float)mEl[i].GetDouble();
            }
            return m;
        }

        // Default identity values if TRS components are missing.
        var translation = Vector3.Zero;
        if (nodeEl.TryGetProperty("translation", out var tEl) && tEl.ValueKind == JsonValueKind.Array && tEl.GetArrayLength() == 3)
        {
            translation = new Vector3(
                (float)tEl[0].GetDouble(),
                (float)tEl[1].GetDouble(),
                (float)tEl[2].GetDouble());
        }

        var scale = new Vector3(1f, 1f, 1f);
        if (nodeEl.TryGetProperty("scale", out var sEl) && sEl.ValueKind == JsonValueKind.Array && sEl.GetArrayLength() == 3)
        {
            scale = new Vector3(
                (float)sEl[0].GetDouble(),
                (float)sEl[1].GetDouble(),
                (float)sEl[2].GetDouble());
        }

        // glTF rotation is a unit quaternion (x, y, z, w).
        var rotation = Quaternion.Identity;
        if (nodeEl.TryGetProperty("rotation", out var rEl) && rEl.ValueKind == JsonValueKind.Array && rEl.GetArrayLength() == 4)
        {
            rotation = new Quaternion(
                (float)rEl[0].GetDouble(),
                (float)rEl[1].GetDouble(),
                (float)rEl[2].GetDouble(),
                (float)rEl[3].GetDouble());
        }

        // Compose TRS into a single 4x4 column-major matrix.
        return Matrix4.Trs(translation, rotation, scale).Data;
    }

    private static bool IsGlb(byte[] data)
    {
        return data.Length >= 4 &&
               data[0] == (byte)'g' &&
               data[1] == (byte)'l' &&
               data[2] == (byte)'T' &&
               data[3] == (byte)'F';
    }

    private static Mesh LoadPrimitive(JsonElement prim, JsonElement accessors, JsonElement bufferViews, byte[] bin, JsonElement root, string? baseUrl = null)
    {
        if (!prim.TryGetProperty("attributes", out var attrs))
        {
            // Likely KHR_draco_mesh_compression or unsupported primitive
            return null!;
        }

        int posAcc = attrs.GetProperty("POSITION").GetInt32();

        int? norAcc = null;
        if (attrs.TryGetProperty("NORMAL", out var normalEl))
        {
            norAcc = normalEl.GetInt32();
        }

        float[] positions = ReadAccessorFloatVec3(accessors, bufferViews, bin, posAcc);
        float[] normals;

        // Texture UV coordinates
        int? uvAcc = null;
        if (attrs.TryGetProperty("TEXCOORD_0", out var uvEl))
            uvAcc = uvEl.GetInt32();

        float[] uvs = uvAcc.HasValue
                  ? ReadAccessorFloatVec2(accessors, bufferViews, bin, uvAcc.Value)
                  : new float[(positions.Length / 3) * 2];

        uint[]? indices = null;
        int indexCount = 0;
        if (prim.TryGetProperty("indices", out var idx))
        {
            indices = ReadAccessorIndicesU32(
                accessors,
                bufferViews,
                bin,
                idx.GetInt32());
            indexCount = indices.Length;
        }

        if (norAcc.HasValue)
        {
            normals = ReadAccessorFloatVec3(accessors, bufferViews, bin, norAcc.Value);
        }
        else
        {
            // Fallback normals: compute from geometry when normals are missing (e.g., Fox.glb)
            normals = ComputeNormals(positions, indices);
        }

        int vertexCount = positions.Length / 3;
        System.Diagnostics.Debug.WriteLine($"[glTF] Loaded primitive: {vertexCount} vertices, {indexCount} indices");

        // Check for skinning data (JOINTS_0 and WEIGHTS_0)
        byte[]? joints = null;
        float[]? weights = null;
        bool hasSkinning = false;

        if (attrs.TryGetProperty("JOINTS_0", out var jointsEl) && attrs.TryGetProperty("WEIGHTS_0", out var weightsEl))
        {
            joints = ReadAccessorJointsU8(accessors, bufferViews, bin, jointsEl.GetInt32(), vertexCount);
            weights = ReadAccessorFloatVec4(accessors, bufferViews, bin, weightsEl.GetInt32());
            hasSkinning = true;
        }

        // Determine vertex layout and pack vertices
        float[] vertices;
        var vertexLayout = VertexLayout.PositionNormalUV;

        if (hasSkinning && joints != null && weights != null)
        {
            // Normalize weights (JOINTS_0 validation happens at runtime with the attached skin)
            for (int i = 0; i < vertexCount; i++)
            {
                int j = i * 4;
                float weightSum = weights[j + 0] + weights[j + 1] + weights[j + 2] + weights[j + 3];
                if (System.Math.Abs(weightSum - 1.0f) > 0.01f)
                {
                    if (weightSum > 0.0001f)
                    {
                        weights[j + 0] /= weightSum;
                        weights[j + 1] /= weightSum;
                        weights[j + 2] /= weightSum;
                        weights[j + 3] /= weightSum;
                    }
                    else
                    {
                        weights[j + 0] = 1.0f;
                    }
                }
            }
            
            // Pack vertices: POSITION(3) + NORMAL(3) + UV(2) + JOINTS(4 as floats) + WEIGHTS(4) = 16 floats per vertex
            vertexLayout = VertexLayout.PositionNormalUVSkinnedJointsWeights;
            vertices = new float[vertexCount * 16];

            for (int i = 0; i < vertexCount; i++)
            {
                int p = i * 3;
                int v = i * 16;
                int uv = i * 2;
                int j = i * 4;

                // Position
                vertices[v + 0] = positions[p + 0];
                vertices[v + 1] = positions[p + 1];
                vertices[v + 2] = positions[p + 2];

                // Normal
                vertices[v + 3] = normals[p + 0];
                vertices[v + 4] = normals[p + 1];
                vertices[v + 5] = normals[p + 2];

                // UV
                vertices[v + 6] = uvs[uv + 0];
                vertices[v + 7] = uvs[uv + 1];

                // Joints (stored as floats but represent uint8 indices, will be unpacked in shader)
                vertices[v + 8] = (float)joints[j + 0];
                vertices[v + 9] = (float)joints[j + 1];
                vertices[v + 10] = (float)joints[j + 2];
                vertices[v + 11] = (float)joints[j + 3];

                // Weights
                vertices[v + 12] = weights[j + 0];
                vertices[v + 13] = weights[j + 1];
                vertices[v + 14] = weights[j + 2];
                vertices[v + 15] = weights[j + 3];
            }
        }
        else
        {
            // Standard layout: POSITION(3) + NORMAL(3) + UV(2) = 8 floats per vertex
            vertices = new float[vertexCount * 8];

            for (int i = 0; i < vertexCount; i++)
            {
                int p = i * 3;
                int v = i * 8;
                int uv = i * 2;

                // Position
                vertices[v + 0] = positions[p + 0];
                vertices[v + 1] = positions[p + 1];
                vertices[v + 2] = positions[p + 2];

                // Normal
                vertices[v + 3] = normals[p + 0];
                vertices[v + 4] = normals[p + 1];
                vertices[v + 5] = normals[p + 2];

                // UV
                vertices[v + 6] = uvs[uv + 0];
                vertices[v + 7] = uvs[uv + 1];
            }
        }

        var geo = new LoadedGeometry(vertices, indices, vertexLayout);
        var mesh = new Mesh(geo);
        var materialIndex = prim.TryGetProperty("material", out var materialEl) ? materialEl.GetInt32() : (int?)null;
        mesh.Material = GltfMaterialReader.TryReadMaterial(root, bin, baseUrl, materialIndex);

        return mesh;
    }


    private static float[] ReadAccessorFloatVec3(JsonElement accessors, JsonElement bufferViews, byte[] bin, int accessorIndex)
    {
        var acc = accessors[accessorIndex];

        var componentType = acc.GetProperty("componentType").GetInt32();
        if (componentType != 5126) throw new NotSupportedException("Only FLOAT accessors are supported for POSITION/NORMAL.");

        var type = acc.GetProperty("type").GetString();
        if (!string.Equals(type, "VEC3", StringComparison.Ordinal)) throw new NotSupportedException("Only VEC3 accessors are supported for POSITION/NORMAL.");

        var count = acc.GetProperty("count").GetInt32();
        var viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = bufferViews[viewIndex];

        var viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
        var accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
        var byteOffset = viewOffset + accOffset;

        const int elementSize = 12; // 3 floats * 4 bytes
        var bufferStride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 0;
        var stride = bufferStride > 0 ? bufferStride : elementSize;
        
        if (stride < elementSize) 
            throw new InvalidDataException("Invalid byteStride for VEC3 float.");

        Console.WriteLine($"[glTF] ReadAccessorFloatVec3:");
        Console.WriteLine($"  accessor.count={count}");
        Console.WriteLine($"  bufferView.byteStride={bufferStride}");
        Console.WriteLine($"  elementSize={elementSize}");
        Console.WriteLine($"  stride used={stride}");
        Console.WriteLine($"  final byteOffset={byteOffset} (viewOffset={viewOffset} + accOffset={accOffset})");

        var result = new float[count * 3];
        for (var i = 0; i < count; i++)
        {
            var baseByte = byteOffset + (i * stride);
            result[i * 3 + 0] = BitConverter.ToSingle(bin, baseByte + 0);
            result[i * 3 + 1] = BitConverter.ToSingle(bin, baseByte + 4);
            result[i * 3 + 2] = BitConverter.ToSingle(bin, baseByte + 8);
        }

        // Log first 3 vertices for diagnostics
        if (count > 0)
        {
            Console.WriteLine($"  First 3 vertices:");
            for (int i = 0; i < System.Math.Min(3, count); i++)
            {
                Console.WriteLine($"    [{i}] x={result[i * 3 + 0]:F4} y={result[i * 3 + 1]:F4} z={result[i * 3 + 2]:F4}");
            }
        }

        return result;
    }

    private static float[] ComputeNormals(float[] positions, uint[]? indices)
    {
        ArgumentNullException.ThrowIfNull(positions);

        var vertexCount = positions.Length / 3;
        var normals = new float[vertexCount * 3];

        if (indices is { Length: >= 3 })
        {
            for (var i = 0; i + 2 < indices.Length; i += 3)
            {
                var i0 = (int)indices[i + 0];
                var i1 = (int)indices[i + 1];
                var i2 = (int)indices[i + 2];

                if (i0 < 0 || i1 < 0 || i2 < 0 || i0 >= vertexCount || i1 >= vertexCount || i2 >= vertexCount)
                {
                    continue;
                }

                var p0 = i0 * 3;
                var p1 = i1 * 3;
                var p2 = i2 * 3;

                var ux = positions[p1 + 0] - positions[p0 + 0];
                var uy = positions[p1 + 1] - positions[p0 + 1];
                var uz = positions[p1 + 2] - positions[p0 + 2];

                var vx = positions[p2 + 0] - positions[p0 + 0];
                var vy = positions[p2 + 1] - positions[p0 + 1];
                var vz = positions[p2 + 2] - positions[p0 + 2];

                var nx = uy * vz - uz * vy;
                var ny = uz * vx - ux * vz;
                var nz = ux * vy - uy * vx;

                normals[p0 + 0] += nx; normals[p0 + 1] += ny; normals[p0 + 2] += nz;
                normals[p1 + 0] += nx; normals[p1 + 1] += ny; normals[p1 + 2] += nz;
                normals[p2 + 0] += nx; normals[p2 + 1] += ny; normals[p2 + 2] += nz;
            }
        }
        else
        {
            // Non-indexed geometry: assume triangles in order.
            for (var i = 0; i + 8 < positions.Length; i += 9)
            {
                var ux = positions[i + 3] - positions[i + 0];
                var uy = positions[i + 4] - positions[i + 1];
                var uz = positions[i + 5] - positions[i + 2];

                var vx = positions[i + 6] - positions[i + 0];
                var vy = positions[i + 7] - positions[i + 1];
                var vz = positions[i + 8] - positions[i + 2];

                var nx = uy * vz - uz * vy;
                var ny = uz * vx - ux * vz;
                var nz = ux * vy - uy * vx;

                normals[i + 0] = nx; normals[i + 1] = ny; normals[i + 2] = nz;
                normals[i + 3] = nx; normals[i + 4] = ny; normals[i + 5] = nz;
                normals[i + 6] = nx; normals[i + 7] = ny; normals[i + 8] = nz;
            }
        }

        // Normalize normals
        for (var i = 0; i < normals.Length; i += 3)
        {
            var nx = normals[i + 0];
            var ny = normals[i + 1];
            var nz = normals[i + 2];
            var len = System.MathF.Sqrt(nx * nx + ny * ny + nz * nz);
            if (len > 0.000001f)
            {
                normals[i + 0] = nx / len;
                normals[i + 1] = ny / len;
                normals[i + 2] = nz / len;
            }
            else
            {
                normals[i + 0] = 0f;
                normals[i + 1] = 1f;
                normals[i + 2] = 0f;
            }
        }

        return normals;
    }

    private static byte[] StripUtf8Bom(byte[] bytes)
    {
        // Some tooling (notably Windows PowerShell Set-Content -Encoding UTF8) writes a UTF-8 BOM.
        // System.Text.Json expects JSON to begin with '{' or '['; strip BOM to be resilient.
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            var trimmed = new byte[bytes.Length - 3];
            Buffer.BlockCopy(bytes, 3, trimmed, 0, trimmed.Length);
            return trimmed;
        }

        return bytes;
    }

    /// <summary>
    /// Loads all skins from the glTF document.
    /// Returns a dictionary mapping skin index to Skin object.
    /// </summary>
    private static Dictionary<int, Skin> LoadSkins(JsonElement root, JsonElement accessors, JsonElement bufferViews, byte[] bin)
    {
        var result = new Dictionary<int, Skin>();

        if (!root.TryGetProperty("skins", out var skinsEl) || skinsEl.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        if (!root.TryGetProperty("nodes", out var nodesEl) || nodesEl.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        int skinIndex = 0;
        foreach (var skinEl in skinsEl.EnumerateArray())
        {
            if (!skinEl.TryGetProperty("joints", out var jointsEl) || jointsEl.ValueKind != JsonValueKind.Array)
            {
                skinIndex++;
                continue;
            }

            var jointNodeIndices = new List<int>();
            foreach (var jointIndexEl in jointsEl.EnumerateArray())
            {
                jointNodeIndices.Add(jointIndexEl.GetInt32());
            }

            if (jointNodeIndices.Count == 0)
            {
                skinIndex++;
                continue;
            }

            // Get joint names from node indices
            var jointNames = new List<string>(jointNodeIndices.Count);
            foreach (var nodeIndex in jointNodeIndices)
            {
                var name = GetNodeName(nodesEl, nodeIndex);
                jointNames.Add(name);
            }

            // Load inverse bind matrices
            float[] inverseBindMatrices = null!;
            if (skinEl.TryGetProperty("inverseBindMatrices", out var ibmEl))
            {
                var accessorIndex = ibmEl.GetInt32();
                inverseBindMatrices = ReadAccessorFloatMat4(accessors, bufferViews, bin, accessorIndex);
            }
            else
            {
                // No inverse bind matrices; default to identity matrices
                inverseBindMatrices = new float[jointNodeIndices.Count * 16];
                for (int i = 0; i < jointNodeIndices.Count; i++)
                {
                    var identity = Matrix.Identity();
                    Array.Copy(identity, 0, inverseBindMatrices, i * 16, 16);
                }
            }

            // Convert flat array to list of 4x4 matrices
            var matrices = new List<float[]>(jointNodeIndices.Count);
            for (int i = 0; i < jointNodeIndices.Count; i++)
            {
                var matrix = new float[16];
                Array.Copy(inverseBindMatrices, i * 16, matrix, 0, 16);
                matrices.Add(matrix);
            }

            var skin = new Skin(jointNodeIndices, jointNames, matrices);
            result[skinIndex] = skin;
            System.Diagnostics.Debug.WriteLine($"[LOADER] Loaded skin {skinIndex} with {skin.JointCount} joints: {string.Join(", ", jointNames)}");
            System.Diagnostics.Debug.WriteLine($"[LOADER]   Joint node indices: {string.Join(", ", jointNodeIndices)}");
            skinIndex++;
        }

        return result;
    }

    /// <summary>
    /// Reads a MAT4 accessor and returns flattened 4x4 matrices.
    /// Each matrix is 16 floats in column-major order.
    /// </summary>
    private static float[] ReadAccessorFloatMat4(JsonElement accessors, JsonElement bufferViews, byte[] bin, int accessorIndex)
    {
        var acc = accessors[accessorIndex];

        var componentType = acc.GetProperty("componentType").GetInt32();
        if (componentType != 5126)
            throw new NotSupportedException("Only FLOAT accessors are supported for inverse bind matrices.");

        var type = acc.GetProperty("type").GetString();
        if (!string.Equals(type, "MAT4", StringComparison.Ordinal))
            throw new NotSupportedException("Only MAT4 accessors are supported for inverse bind matrices.");

        var count = acc.GetProperty("count").GetInt32();
        var viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = bufferViews[viewIndex];

        var viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
        var accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
        var byteOffset = viewOffset + accOffset;

        var stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 64; // 16 floats * 4 bytes

        var result = new float[count * 16];
        for (int i = 0; i < count; i++)
        {
            int baseByte = byteOffset + i * stride;
            for (int j = 0; j < 16; j++)
            {
                result[i * 16 + j] = BitConverter.ToSingle(bin, baseByte + j * 4);
            }
        }

        return result;
    }

    /// <summary>
    /// Reads JOINTS_0 accessor (uint8 joint indices).
    /// Returns an array of 4 bytes per vertex.
    /// </summary>
    private static byte[] ReadAccessorJointsU8(JsonElement accessors, JsonElement bufferViews, byte[] bin, int accessorIndex, int expectedVertexCount)
    {
        var acc = accessors[accessorIndex];

        var componentType = acc.GetProperty("componentType").GetInt32();
        // Component type 5125 = unsigned int, 5123 = unsigned short, 5121 = unsigned byte
        if (componentType != 5121 && componentType != 5123 && componentType != 5125)
            throw new NotSupportedException($"Unsupported component type {componentType} for JOINTS_0.");

        var type = acc.GetProperty("type").GetString();
        if (!string.Equals(type, "VEC4", StringComparison.Ordinal))
            throw new NotSupportedException("Only VEC4 accessors are supported for JOINTS_0.");

        var count = acc.GetProperty("count").GetInt32();
        if (count != expectedVertexCount)
            throw new InvalidDataException($"Joint count ({count}) must match vertex count ({expectedVertexCount}).");

        var viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = bufferViews[viewIndex];

        var viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
        var accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
        var byteOffset = viewOffset + accOffset;

        var result = new byte[count * 4];
        
        if (componentType == 5121) // UNSIGNED_BYTE
        {
            var stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 4;
            for (int i = 0; i < count; i++)
            {
                int baseByte = byteOffset + i * stride;
                result[i * 4 + 0] = bin[baseByte + 0];
                result[i * 4 + 1] = bin[baseByte + 1];
                result[i * 4 + 2] = bin[baseByte + 2];
                result[i * 4 + 3] = bin[baseByte + 3];
            }
        }
        else if (componentType == 5123) // UNSIGNED_SHORT
        {
            var stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 8;
            for (int i = 0; i < count; i++)
            {
                int baseByte = byteOffset + i * stride;
                result[i * 4 + 0] = (byte)BinaryPrimitives.ReadUInt16LittleEndian(bin.AsSpan(baseByte, 2));
                result[i * 4 + 1] = (byte)BinaryPrimitives.ReadUInt16LittleEndian(bin.AsSpan(baseByte + 2, 2));
                result[i * 4 + 2] = (byte)BinaryPrimitives.ReadUInt16LittleEndian(bin.AsSpan(baseByte + 4, 2));
                result[i * 4 + 3] = (byte)BinaryPrimitives.ReadUInt16LittleEndian(bin.AsSpan(baseByte + 6, 2));
            }
        }
        else // UNSIGNED_INT
        {
            var stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 16;
            for (int i = 0; i < count; i++)
            {
                int baseByte = byteOffset + i * stride;
                result[i * 4 + 0] = (byte)BinaryPrimitives.ReadUInt32LittleEndian(bin.AsSpan(baseByte, 4));
                result[i * 4 + 1] = (byte)BinaryPrimitives.ReadUInt32LittleEndian(bin.AsSpan(baseByte + 4, 4));
                result[i * 4 + 2] = (byte)BinaryPrimitives.ReadUInt32LittleEndian(bin.AsSpan(baseByte + 8, 4));
                result[i * 4 + 3] = (byte)BinaryPrimitives.ReadUInt32LittleEndian(bin.AsSpan(baseByte + 12, 4));
            }
        }

        return result;
    }

    /// <summary>
    /// Reads WEIGHTS_0 accessor (float weights summing to 1.0).
    /// Returns an array of 4 floats per vertex.
    /// </summary>
    private static float[] ReadAccessorFloatVec4(JsonElement accessors, JsonElement bufferViews, byte[] bin, int accessorIndex)
    {
        var acc = accessors[accessorIndex];

        var componentType = acc.GetProperty("componentType").GetInt32();
        if (componentType != 5126)
            throw new NotSupportedException("Only FLOAT accessors are supported for WEIGHTS_0.");

        var type = acc.GetProperty("type").GetString();
        if (!string.Equals(type, "VEC4", StringComparison.Ordinal))
            throw new NotSupportedException("Only VEC4 accessors are supported for WEIGHTS_0.");

        var count = acc.GetProperty("count").GetInt32();
        var viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = bufferViews[viewIndex];

        var viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
        var accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
        var byteOffset = viewOffset + accOffset;

        var stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 16; // 4 floats * 4 bytes

        System.Diagnostics.Debug.WriteLine($"[glTF] Reading {count} VEC4 weights, byteOffset={byteOffset}, stride={stride}");

        var result = new float[count * 4];
        for (int i = 0; i < count; i++)
        {
            int baseByte = byteOffset + i * stride;
            result[i * 4 + 0] = BitConverter.ToSingle(bin, baseByte + 0);
            result[i * 4 + 1] = BitConverter.ToSingle(bin, baseByte + 4);
            result[i * 4 + 2] = BitConverter.ToSingle(bin, baseByte + 8);
            result[i * 4 + 3] = BitConverter.ToSingle(bin, baseByte + 12);
        }

        return result;
    }


    private static uint[] ReadAccessorIndicesU32(JsonElement accessors, JsonElement bufferViews, byte[] bin, int accessorIndex)
    {
        var acc = accessors[accessorIndex];
        var type = acc.GetProperty("type").GetString();
        if (!string.Equals(type, "SCALAR", StringComparison.Ordinal)) throw new NotSupportedException("Only SCALAR accessors are supported for indices.");

        var count = acc.GetProperty("count").GetInt32();
        var viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = bufferViews[viewIndex];

        var viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
        var accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
        var byteOffset = viewOffset + accOffset;

        var componentType = acc.GetProperty("componentType").GetInt32();

        // CRITICAL: Respect byteStride if present (may be larger than element size for interleaved data)
        int stride = componentType switch
        {
            5121 => 1,      // UNSIGNED_BYTE
            5123 => 2,      // UNSIGNED_SHORT
            5125 => 4,      // UNSIGNED_INT
            _ => throw new NotSupportedException($"Unsupported index componentType: {componentType}")
        };
        
        // If byteStride is specified and larger than element size, use it
        if (view.TryGetProperty("byteStride", out var bs))
        {
            var bufferStride = bs.GetInt32();
            if (bufferStride > stride)
            {
                stride = bufferStride;
            }
        }

        System.Diagnostics.Debug.WriteLine($"[glTF] Reading {count} indices, componentType={componentType}, byteOffset={byteOffset}, stride={stride}");

        return componentType switch
        {
            5121 => ReadByteIndicesU32(bin, byteOffset, count, stride),        // UNSIGNED_BYTE
            5123 => ReadUShortIndicesU32(bin, byteOffset, count, stride),      // UNSIGNED_SHORT
            5125 => ReadUIntIndices(bin, byteOffset, count, stride),           // UNSIGNED_INT
            _ => throw new NotSupportedException($"Unsupported index componentType: {componentType}")
        };
    }

    private static uint[] ReadByteIndicesU32(byte[] bin, int byteOffset, int count, int stride)
    {
        var indices = new uint[count];
        for (var i = 0; i < count; i++)
        {
            indices[i] = bin[byteOffset + (i * stride)];
        }
        return indices;
    }

    private static uint[] ReadUShortIndicesU32(byte[] bin, int byteOffset, int count, int stride)
    {
        var indices = new uint[count];
        for (var i = 0; i < count; i++)
        {
            indices[i] = BinaryPrimitives.ReadUInt16LittleEndian(bin.AsSpan(byteOffset + (i * stride), 2));
        }
        return indices;
    }

    private static uint[] ReadUIntIndices(byte[] bin, int byteOffset, int count, int stride)
    {
        var indices = new uint[count];
        for (var i = 0; i < count; i++)
        {
            indices[i] = BinaryPrimitives.ReadUInt32LittleEndian(bin.AsSpan(byteOffset + (i * stride), 4));
        }
        return indices;
    }


    private static float[] ReadAccessorFloatVec2(JsonElement accessors, JsonElement bufferViews, byte[] bin, int accessorIndex)
    {
        var acc = accessors[accessorIndex];

        if (acc.GetProperty("componentType").GetInt32() != 5126)
            throw new NotSupportedException("Only FLOAT UVs supported.");

        if (acc.GetProperty("type").GetString() != "VEC2")
            throw new NotSupportedException("Only VEC2 UVs supported.");

        int count = acc.GetProperty("count").GetInt32();
        int viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = bufferViews[viewIndex];

        int viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
        int accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
        int byteOffset = viewOffset + accOffset;

        int stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 8;

        System.Diagnostics.Debug.WriteLine($"[glTF] Reading {count} VEC2 UVs, byteOffset={byteOffset}, stride={stride}");

        var result = new float[count * 2];
        for (int i = 0; i < count; i++)
        {
            int baseByte = byteOffset + i * stride;
            result[i * 2 + 0] = BitConverter.ToSingle(bin, baseByte);
            result[i * 2 + 1] = BitConverter.ToSingle(bin, baseByte + 4);
        }

        return result;
    }

}
