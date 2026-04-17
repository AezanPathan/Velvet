using System.Buffers.Binary;
using System.Text.Json;

namespace Velvet.Core.Assets.Gltf;

internal static class GltfDocumentLoader
{
    internal static (JsonDocument Doc, byte[] Bin) LoadGltfDocument(byte[] data)
    {
        if (IsGlb(data))
            return LoadFromGlb(data);

        // embedded-base64 .gltf
        return LoadFromGltfJson(data);
    }

    internal static (JsonDocument Doc, byte[] Bin) LoadFromGlb(byte[] glb)
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

        var binBytes = glb.AsSpan(offset, (int)binLength).ToArray();
        var doc = JsonDocument.Parse(json);

        return (doc, binBytes);
    }

    internal static (JsonDocument Doc, byte[] Bin) LoadFromGltfJson(byte[] gltfJsonUtf8)
    {
        gltfJsonUtf8 = StripUtf8Bom(gltfJsonUtf8);
        var doc = JsonDocument.Parse(gltfJsonUtf8);
        var root = doc.RootElement;

        if (!root.TryGetProperty("asset", out var asset) || !asset.TryGetProperty("version", out var ver) || ver.GetString() != "2.0")
            throw new NotSupportedException("Only glTF 2.0 is supported.");

        var buffers = root.GetProperty("buffers");
        if (buffers.GetArrayLength() < 1) throw new InvalidDataException("glTF has no buffers.");

        var buffer0 = buffers[0];
        if (!buffer0.TryGetProperty("uri", out var uriEl))
            throw new NotSupportedException(".gltf without embedded buffer URI is not supported in this minimal loader.");

        var uri = uriEl.GetString() ?? string.Empty;
        const string prefix = "data:application/octet-stream;base64,";
        if (!uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Only embedded base64 buffers are supported for .gltf in this demo loader.");

        var base64 = uri[prefix.Length..];
        var binBytes = Convert.FromBase64String(base64);

        return (doc, binBytes);
    }

    internal static bool IsGlb(byte[] data)
    {
        return data.Length >= 4 &&
               data[0] == (byte)'g' &&
               data[1] == (byte)'l' &&
               data[2] == (byte)'T' &&
               data[3] == (byte)'F';
    }

    internal static byte[] StripUtf8Bom(byte[] bytes)
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
}
