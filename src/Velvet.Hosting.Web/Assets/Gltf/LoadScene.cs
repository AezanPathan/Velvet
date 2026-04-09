using Velvet.Core.Assets.Gltf;
using Velvet.Core.Scene;

namespace Velvet.Hosting.Web.Assets.Gltf;

public static class LoadScene
{
    public static async Task<Scene> LoadFromUrlAsync(HttpClient http, string url)
    {
        var bytes = await http.GetByteArrayAsync(url);
        return await GltfLoader.LoadScene(bytes);
    }
}