using System.Text.Json;

namespace Velvet.Core.Assets.Gltf;

internal sealed class GltfContext(JsonElement root, JsonElement accessors, JsonElement bufferViews, byte[] bin)
{
    public JsonElement Root => root;
    public JsonElement Accessors => accessors;
    public JsonElement BufferViews => bufferViews;
    public byte[] Bin => bin;
}