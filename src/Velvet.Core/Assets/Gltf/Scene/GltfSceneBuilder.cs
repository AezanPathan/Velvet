using System.Text.Json;
using SceneModel = Velvet.Core.Scene.Scene;
using SceneNode = Velvet.Core.Scene.SceneNode;
using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering.Meshes;
using Velvet.Core.Rendering.Skinning;

namespace Velvet.Core.Assets.Gltf;

internal static class GltfSceneBuilder
{
    internal static SceneModel BuildScene(GltfContext context, List<List<Mesh>> meshesByIndex, Dictionary<int, Skin> skinsById)
    {
        if (!context.Root.TryGetProperty("nodes", out var nodesEl) || nodesEl.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("glTF has no nodes.");
        }

        var activeScene = 0;
        if (context.Root.TryGetProperty("scene", out var sceneEl))
        {
            activeScene = sceneEl.GetInt32();
        }

        var rootNodeIndices = new List<int>();
        if (context.Root.TryGetProperty("scenes", out var scenesEl) && scenesEl.ValueKind == JsonValueKind.Array && scenesEl.GetArrayLength() > activeScene)
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
            roots.Add(BuildNode(context, nodeIndex, nodesEl, meshesByIndex, skinsById));
        }

        return new SceneModel(roots);
    }


    internal static SceneNode BuildNode(GltfContext context, int nodeIndex, JsonElement nodesEl, List<List<Mesh>> meshesByIndex, Dictionary<int, Skin> skinsById)
    {
        var nodeEl = nodesEl[nodeIndex];

        var localTransform = ReadNodeTransform(nodeEl);
        var meshes = ResolveNodeMeshes(nodeEl, meshesByIndex);
        var children = ResolveNodeChildren(context, nodeEl, nodesEl, meshesByIndex, skinsById);
        var name = ResolveNodeName(nodeEl, nodeIndex);
        var skin = ResolveNodeSkin(nodeEl, skinsById);

        return new SceneNode(localTransform, meshes, children, name, skin, nodeIndex);
    }


    internal static Mesh[] ResolveNodeMeshes(JsonElement nodeEl, List<List<Mesh>> meshesByIndex)
    {
        if (!nodeEl.TryGetProperty("mesh", out var meshIndexEl))
        {
            return Array.Empty<Mesh>();
        }

        var meshIndex = meshIndexEl.GetInt32();
        if (meshIndex < 0 || meshIndex >= meshesByIndex.Count)
        {
            return Array.Empty<Mesh>();
        }

        return meshesByIndex[meshIndex].ToArray();
    }


    internal static List<SceneNode> ResolveNodeChildren(
        GltfContext context,
        JsonElement nodeEl,
        JsonElement nodesEl,
        List<List<Mesh>> meshesByIndex,
        Dictionary<int, Skin> skinsById)
    {
        var children = new List<SceneNode>();
        if (!nodeEl.TryGetProperty("children", out var childrenEl) || childrenEl.ValueKind != JsonValueKind.Array)
        {
            return children;
        }

        foreach (var childIndexEl in childrenEl.EnumerateArray())
        {
            children.Add(BuildNode(context, childIndexEl.GetInt32(), nodesEl, meshesByIndex, skinsById));
        }

        return children;
    }


    internal static string ResolveNodeName(JsonElement nodeEl, int nodeIndex)
    {
        var name = nodeEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
        return string.IsNullOrWhiteSpace(name) ? $"Node_{nodeIndex}" : name;
    }


    internal static Skin? ResolveNodeSkin(JsonElement nodeEl, Dictionary<int, Skin> skinsById)
    {
        if (!nodeEl.TryGetProperty("skin", out var skinIndexEl))
        {
            return null;
        }

        var skinIndex = skinIndexEl.GetInt32();
        return skinsById.TryGetValue(skinIndex, out var skin) ? skin : null;
    }


    internal static float[] ReadNodeTransform(JsonElement nodeEl)
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
}
