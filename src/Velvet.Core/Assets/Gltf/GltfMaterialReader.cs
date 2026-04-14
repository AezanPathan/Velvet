using System;
using System.Text.Json;
using Velvet.Core.Math;
using DataMaterial = Velvet.Core.Rendering.Materials.Material;

namespace Velvet.Core.Assets.Gltf;

internal static class GltfMaterialReader
{
    internal static DataMaterial? TryReadMaterial(JsonElement root, byte[] bin, string? baseUrl = null, int? materialIndex = null)
    {
        if (!root.TryGetProperty("materials", out var materials) || materials.GetArrayLength() < 1) return null;

        var index = materialIndex ?? 0;
        if (index < 0 || index >= materials.GetArrayLength())
            index = 0;

        var m = materials[index];

        var unlit = false;
        if (m.TryGetProperty("extensions", out var ext) && ext.ValueKind == JsonValueKind.Object)
        {
            if (ext.TryGetProperty("KHR_materials_unlit", out _))
                unlit = true;
        }

        Vector3 color = new(1, 1, 1);
        if (m.TryGetProperty("pbrMetallicRoughness", out var pbr) && pbr.ValueKind == JsonValueKind.Object)
        {
            if (pbr.TryGetProperty("baseColorFactor", out var f) && f.ValueKind == JsonValueKind.Array && f.GetArrayLength() >= 3)
            {
                color = new Vector3(
                    (float)f[0].GetDouble(),
                    (float)f[1].GetDouble(),
                    (float)f[2].GetDouble());
            }
        }

        var material = new DataMaterial(
            albedoColor: color,
            ambientStrength: 0.05f,
            diffuseStrength: 1.0f,
            unlit: unlit);

        var textureUri = TryReadBaseColorImage(root, m, bin);
        if (textureUri != null)
        {
            if (!string.IsNullOrWhiteSpace(baseUrl) && !textureUri.StartsWith("data:") && !textureUri.StartsWith("/"))
            {
                if (!baseUrl.EndsWith("/"))
                    baseUrl += "/";

                textureUri = baseUrl + textureUri;
            }

            material.BaseColorTextureUri = textureUri;
        }

        return material;
    }

    private static string? TryReadBaseColorImage(JsonElement root, JsonElement material, byte[] bin)
    {
        if (!material.TryGetProperty("pbrMetallicRoughness", out var pbr) ||
            !pbr.TryGetProperty("baseColorTexture", out var tex) ||
            !tex.TryGetProperty("index", out var indexEl))
        {
            return null;
        }

        var textureIndex = indexEl.GetInt32();
        if (!root.TryGetProperty("textures", out var texturesEl) ||
            texturesEl.ValueKind != JsonValueKind.Array ||
            textureIndex < 0 ||
            textureIndex >= texturesEl.GetArrayLength())
        {
            return null;
        }

        var texture = texturesEl[textureIndex];
        if (!texture.TryGetProperty("source", out var sourceEl))
        {
            return null;
        }

        var imageIndex = sourceEl.GetInt32();
        if (!root.TryGetProperty("images", out var imagesEl) ||
            imagesEl.ValueKind != JsonValueKind.Array ||
            imageIndex < 0 ||
            imageIndex >= imagesEl.GetArrayLength())
        {
            return null;
        }

        var image = imagesEl[imageIndex];
        if (image.TryGetProperty("uri", out var uriEl))
        {
            return uriEl.GetString();
        }

        if (!image.TryGetProperty("bufferView", out var bvEl))
        {
            return null;
        }

        var viewIndex = bvEl.GetInt32();
        if (!root.TryGetProperty("bufferViews", out var bufferViews) ||
            bufferViews.ValueKind != JsonValueKind.Array ||
            viewIndex < 0 ||
            viewIndex >= bufferViews.GetArrayLength())
        {
            return null;
        }

        var view = bufferViews[viewIndex];
        var byteOffset = view.TryGetProperty("byteOffset", out var bo) ? bo.GetInt32() : 0;
        var byteLength = view.TryGetProperty("byteLength", out var bl) ? bl.GetInt32() : 0;
        if (byteLength <= 0)
        {
            return null;
        }

        var mimeType = image.TryGetProperty("mimeType", out var mtEl) ? (mtEl.GetString() ?? "image/png") : "image/png";
        var bytes = new byte[byteLength];
        Buffer.BlockCopy(bin, byteOffset, bytes, 0, byteLength);
        var base64 = Convert.ToBase64String(bytes);
        return $"data:{mimeType};base64,{base64}";
    }
}
