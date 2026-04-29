using SceneModel = Velvet.Core.Scene.Scene;
using Velvet.Core.Animation;
using Velvet.Core.Rendering.Meshes;

namespace Velvet.Core.Assets.Gltf;

public static class GltfLoader
{
    public static async Task<(SceneModel Scene, List<AnimationClip> Animations)> LoadFromUrl(
    HttpClient http,
    string url)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var bytes = await http.GetByteArrayAsync(url);

        // Extract base path for textures
        var baseUrl = Path.GetDirectoryName(url)?.Replace("\\", "/");

        return await LoadSceneWithAnimations(bytes, baseUrl);
    }

    public static async Task<SceneModel> LoadScene(byte[] data, string? baseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Yield immediately so browser regains control
        await Task.Yield();

        var gltf = GltfDocumentLoader.LoadGltfDocument(data);
        using (gltf.Doc)
        {
            // Yield between heavy steps
            await Task.Yield();
            var root = gltf.Doc.RootElement;
            var context = new GltfContext(
                root,
                root.GetProperty("accessors"),
                root.GetProperty("bufferViews"),
                gltf.Bin);
            var skinsById = GltfSkinReader.LoadSkins(context);
            var meshesByIndex = GltfMeshReader.LoadMeshesByIndex(context, baseUrl);
            await Task.Yield();
            return GltfSceneBuilder.BuildScene(context, meshesByIndex, skinsById);
        }
    }

    public static async Task<(SceneModel Scene, List<AnimationClip> Animations)> LoadSceneWithAnimations(byte[] data, string? baseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Yield immediately so browser regains control
        await Task.Yield();

        var gltf = GltfDocumentLoader.LoadGltfDocument(data);
        using (gltf.Doc)
        {
            // Yield between heavy steps
            await Task.Yield();
            var root = gltf.Doc.RootElement;
            var context = new GltfContext(
                root,
                root.GetProperty("accessors"),
                root.GetProperty("bufferViews"),
                gltf.Bin);
            var skinsById = GltfSkinReader.LoadSkins(context);
            var meshesByIndex = GltfMeshReader.LoadMeshesByIndex(context, baseUrl);
            await Task.Yield();

            var scene = GltfSceneBuilder.BuildScene(context, meshesByIndex, skinsById);
            var animations = GltfAnimationReader.LoadAnimations(context);

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
}
