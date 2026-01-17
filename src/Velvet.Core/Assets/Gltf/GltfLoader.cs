using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Velvet.Core.Engine;
using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering;

namespace Velvet.Core.Assets.Gltf;

/// <summary>
/// Minimal CPU-side glTF 2.0 loader with scene graph support.
/// Prefers .glb but still understands .gltf with embedded base64 buffers.
/// </summary>
public static class GltfLoader
{
    public static Scene LoadScene(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var gltf = LoadGltfDocument(data);
        using (gltf.Doc)
        {
            var meshesByIndex = LoadMeshesByIndex(gltf.Doc.RootElement, gltf.Bin);
            return BuildScene(gltf.Doc.RootElement, meshesByIndex);
        }
    }

    public static List<Mesh> LoadMeshes(byte[] data)
    {
        var scene = LoadScene(data);
        var unique = new HashSet<Mesh>();
        var meshes = new List<Mesh>();

        foreach (var instance in scene.MeshInstances)
        {
            if (unique.Add(instance.Mesh))
            {
                meshes.Add(instance.Mesh);
            }
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

    private static List<List<Mesh>> LoadMeshesByIndex(JsonElement root, byte[] bin)
    {
        var meshesOut = new List<List<Mesh>>();

        var meshes = root.GetProperty("meshes");
        var accessors = root.GetProperty("accessors");
        var bufferViews = root.GetProperty("bufferViews");

        foreach (var meshEl in meshes.EnumerateArray())
        {
            if (!meshEl.TryGetProperty("primitives", out var primitives) || primitives.ValueKind != JsonValueKind.Array)
            {
                meshesOut.Add(new List<Mesh>());
                continue;
            }

            var meshPrimitives = new List<Mesh>();
            foreach (var prim in primitives.EnumerateArray())
            {
                var mesh = LoadPrimitive(prim, accessors, bufferViews, bin, root);
                if (mesh != null)
                {
                    meshPrimitives.Add(mesh);
                }
            }

            meshesOut.Add(meshPrimitives);
        }

        return meshesOut;
    }

    private static Scene BuildScene(JsonElement root, List<List<Mesh>> meshesByIndex)
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
            roots.Add(BuildNode(nodeIndex, nodesEl, meshesByIndex));
        }

        return new Scene(roots);
    }

    private static SceneNode BuildNode(int nodeIndex, JsonElement nodesEl, List<List<Mesh>> meshesByIndex)
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
                children.Add(BuildNode(childIndexEl.GetInt32(), nodesEl, meshesByIndex));
            }
        }

        var name = nodeEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;

        return new SceneNode(localTransform, meshes, children, name);
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
        return Matrix.Trs(translation, rotation, scale);
    }

    private static bool IsGlb(byte[] data)
    {
        return data.Length >= 4 &&
               data[0] == (byte)'g' &&
               data[1] == (byte)'l' &&
               data[2] == (byte)'T' &&
               data[3] == (byte)'F';
    }

    private static Mesh LoadPrimitive(JsonElement prim, JsonElement accessors, JsonElement bufferViews, byte[] bin, JsonElement root)
    {
        //var attrs = prim.GetProperty("attributes");

        if (!prim.TryGetProperty("attributes", out var attrs))
        {
            // Likely KHR_draco_mesh_compression or unsupported primitive
            return null!;
        }



        int posAcc = attrs.GetProperty("POSITION").GetInt32();
        ///int? norAcc = attrs.TryGetProperty("NORMAL", out var n) ? n.GetInt32() : null;

        int? norAcc = null;
        if (attrs.TryGetProperty("NORMAL", out var normalEl))
        {
            norAcc = normalEl.GetInt32();
        }


        float[] positions = ReadAccessorFloatVec3(accessors, bufferViews, bin, posAcc);
        // float[] normals = norAcc.HasValue
        //     ? ReadAccessorFloatVec3(accessors, bufferViews, bin, norAcc.Value)
        //     : new float[positions.Length];
        float[] normals;

        if (norAcc.HasValue)
        {
            normals = ReadAccessorFloatVec3(accessors, bufferViews, bin, norAcc.Value);
        }
        else
        {
            // Fallback normals (valid & REQUIRED)
            normals = new float[positions.Length];
        }

        uint[]? indices = null;
        if (prim.TryGetProperty("indices", out var idx))
        {
            indices = ReadAccessorIndicesU32(
                accessors,
                bufferViews,
                bin,
                idx.GetInt32());
        }


        int vertexCount = positions.Length / 3;
        float[] vertices = new float[vertexCount * 9];

        for (int i = 0; i < vertexCount; i++)
        {
            int p = i * 3;
            int v = i * 9;

            vertices[v + 0] = positions[p + 0];
            vertices[v + 1] = positions[p + 1];
            vertices[v + 2] = positions[p + 2];

            vertices[v + 3] = 1f;
            vertices[v + 4] = 1f;
            vertices[v + 5] = 1f;

            vertices[v + 6] = normals[p + 0];
            vertices[v + 7] = normals[p + 1];
            vertices[v + 8] = normals[p + 2];
        }

        var geo = new LoadedGeometry(vertices, indices, VertexLayout.PositionColorNormal);
        var mesh = new Mesh(geo);
        mesh.Material = TryReadMaterial(root);

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

        var stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 12;
        if (stride < 12) throw new InvalidDataException("Invalid byteStride for VEC3 float.");

        var result = new float[count * 3];
        for (var i = 0; i < count; i++)
        {
            var baseByte = byteOffset + (i * stride);
            result[i * 3 + 0] = BitConverter.ToSingle(bin, baseByte + 0);
            result[i * 3 + 1] = BitConverter.ToSingle(bin, baseByte + 4);
            result[i * 3 + 2] = BitConverter.ToSingle(bin, baseByte + 8);
        }

        return result;
    }

    private static Material? TryReadMaterial(JsonElement root)
    {
        if (!root.TryGetProperty("materials", out var materials) || materials.GetArrayLength() < 1)
        {
            return null;
        }

        var m = materials[0];

        // Unlit extension (optional)
        var unlit = false;
        if (m.TryGetProperty("extensions", out var ext) && ext.ValueKind == JsonValueKind.Object)
        {
            if (ext.TryGetProperty("KHR_materials_unlit", out _))
            {
                unlit = true;
            }
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

        return new Material(
            albedoColor: color,
            ambientStrength: 0.05f,
            diffuseStrength: 1.0f,
            unlit: unlit);
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



    // private static uint[] ReadAccessorIndicesU162(JsonElement accessors, JsonElement bufferViews, byte[] bin, int accessorIndex)
    // {
    //     var acc = accessors[accessorIndex];
    //     var type = acc.GetProperty("type").GetString();
    //     if (!string.Equals(type, "SCALAR", StringComparison.Ordinal)) throw new NotSupportedException("Only SCALAR accessors are supported for indices.");

    //     var count = acc.GetProperty("count").GetInt32();
    //     var viewIndex = acc.GetProperty("bufferView").GetInt32();
    //     var view = bufferViews[viewIndex];

    //     var viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
    //     var accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
    //     var byteOffset = viewOffset + accOffset;

    //     var componentType = acc.GetProperty("componentType").GetInt32();
    //     return componentType switch
    //     {
    //         5123 => readu8(bin, byteOffset, count),      // UNSIGNED_SHORT
    //         5121 => ReadByteIndices(bin, byteOffset, count),        // UNSIGNED_BYTE
    //         5125 => ReadUIntIndicesAsUShort(bin, byteOffset, count),// UNSIGNED_INT
    //         _ => throw new NotSupportedException($"Unsupported index componentType: {componentType}")
    //     };
    // }

    // Obsolete code below - kept for reference only

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
            return componentType switch
            {
                5121 => ReadByteIndicesU32(bin, byteOffset, count),        // UNSIGNED_BYTE
                5123 => ReadUShortIndicesU32(bin, byteOffset, count),      // UNSIGNED_SHORT
                5125 => ReadUIntIndices(bin, byteOffset, count),           // UNSIGNED_INT
                _ => throw new NotSupportedException($"Unsupported index componentType: {componentType}")
            };
        }

        private static uint[] ReadByteIndicesU32(byte[] bin, int byteOffset, int count)
        {
            var indices = new uint[count];
            for (var i = 0; i < count; i++)
            {
                indices[i] = bin[byteOffset + i];
            }
            return indices;
        }

        private static uint[] ReadUShortIndicesU32(byte[] bin, int byteOffset, int count)
        {
            var indices = new uint[count];
            for (var i = 0; i < count; i++)
            {
                indices[i] = BinaryPrimitives.ReadUInt16LittleEndian(bin.AsSpan(byteOffset + (i * 2), 2));
            }
            return indices;
        }

        private static uint[] ReadUIntIndices(byte[] bin, int byteOffset, int count)
        {
            var indices = new uint[count];
            for (var i = 0; i < count; i++)
            {
                indices[i] = BinaryPrimitives.ReadUInt32LittleEndian(bin.AsSpan(byteOffset + (i * 4), 4));
            }
            return indices;
        }

    /*
        public static Mesh LoadSingleMesh(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (data.Length >= 12 && data[0] == (byte)'g' && data[1] == (byte)'l' && data[2] == (byte)'T' && data[3] == (byte)'F')
            {
                return LoadFromGlb(data);
            }

            // Treat as UTF-8 JSON .gltf (demo assets).
            return LoadFromGltfJson(data);
        }

        private static Mesh LoadFromGlb(byte[] glb)
        {
            // GLB header
            // 0..3 magic 'glTF'
            // 4..7 version (uint32)
            // 8..11 length (uint32)
            if (glb.Length < 12) throw new InvalidDataException("Invalid GLB: too small.");

            var version = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(4, 4));
            if (version != 2) throw new NotSupportedException($"Only glTF 2.0 is supported (version={version}).");

            var totalLength = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(8, 4));
            if (totalLength != glb.Length) throw new InvalidDataException("Invalid GLB: length mismatch.");

            var offset = 12;

            // Chunk 0: JSON
            if (offset + 8 > glb.Length) throw new InvalidDataException("Invalid GLB: missing JSON chunk header.");
            var jsonLen = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(offset, 4));
            var jsonType = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(offset + 4, 4));
            offset += 8;
            if (jsonType != 0x4E4F534A) throw new InvalidDataException("Invalid GLB: first chunk is not JSON.");
            if (offset + jsonLen > glb.Length) throw new InvalidDataException("Invalid GLB: JSON chunk overruns file.");
            var jsonBytes = glb.AsSpan(offset, checked((int)jsonLen)).ToArray();
            offset += checked((int)jsonLen);

            // Chunk 1: BIN (optional but required for our loader)
            if (offset + 8 > glb.Length) throw new InvalidDataException("Invalid GLB: missing BIN chunk.");
            var binLen = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(offset, 4));
            var binType = BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(offset + 4, 4));
            offset += 8;
            if (binType != 0x004E4942) throw new InvalidDataException("Invalid GLB: second chunk is not BIN.");
            if (offset + binLen > glb.Length) throw new InvalidDataException("Invalid GLB: BIN chunk overruns file.");
            var binBytes = glb.AsSpan(offset, checked((int)binLen)).ToArray();

            return ParseGltf(jsonBytes, binBytes);
        }

        private static Mesh LoadFromGltfJson(byte[] gltfJsonUtf8)
        {
            gltfJsonUtf8 = StripUtf8Bom(gltfJsonUtf8);
            using var doc = JsonDocument.Parse(gltfJsonUtf8);
            var root = doc.RootElement;

            if (!root.TryGetProperty("asset", out var asset) || !asset.TryGetProperty("version", out var ver) || ver.GetString() != "2.0")
            {
                throw new NotSupportedException("Only glTF 2.0 is supported.");
            }

            // Minimal support: first buffer must be an embedded base64 blob.
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

            return ParseGltf(gltfJsonUtf8, binBytes);
        }

        private static Mesh ParseGltf(byte[] jsonUtf8, byte[] binBytes)
        {
            jsonUtf8 = StripUtf8Bom(jsonUtf8);
            using var doc = JsonDocument.Parse(jsonUtf8);
            var root = doc.RootElement;

            var accessors = root.GetProperty("accessors");
            var bufferViews = root.GetProperty("bufferViews");

            // Single mesh primitive (mesh[0].primitives[0])
            var meshes = root.GetProperty("meshes");
            if (meshes.GetArrayLength() < 1) throw new InvalidDataException("glTF has no meshes.");
            var prims = meshes[0].GetProperty("primitives");
            if (prims.GetArrayLength() < 1) throw new InvalidDataException("glTF mesh has no primitives.");
            var prim = prims[0];

            var attrs = prim.GetProperty("attributes");
            if (!attrs.TryGetProperty("POSITION", out var posAccEl)) throw new NotSupportedException("glTF primitive missing POSITION.");
            var posAccessorIndex = posAccEl.GetInt32();

            int? normalAccessorIndex = null;
            if (attrs.TryGetProperty("NORMAL", out var nAccEl)) normalAccessorIndex = nAccEl.GetInt32();

            int? indicesAccessorIndex = null;
            if (prim.TryGetProperty("indices", out var idxEl)) indicesAccessorIndex = idxEl.GetInt32();

            var positions = ReadAccessorFloatVec3(accessors, bufferViews, binBytes, posAccessorIndex);
            var normals = normalAccessorIndex.HasValue
                ? ReadAccessorFloatVec3(accessors, bufferViews, binBytes, normalAccessorIndex.Value)
                : null;

            if (normals is null)
            {
                // Minimal fallback: if normals aren't provided, create placeholder normals.
                // (Still allows rendering; lighting will be incorrect but avoids hard failure.)
                normals = new float[positions.Length];
            }

            if (positions.Length != normals.Length)
            {
                throw new InvalidDataException("POSITION and NORMAL accessor sizes do not match.");
            }

            uint[]? indices = null;
            if (indicesAccessorIndex.HasValue)
            {
                indices = ReadAccessorIndicesU32(accessors, bufferViews, binBytes, indicesAccessorIndex.Value);
            }

            // Build interleaved vertices: [pos.xyz, color.rgb, normal.xyz]
            var vertexCount = positions.Length / 3;
            var vertices = new float[vertexCount * 9];
            for (var i = 0; i < vertexCount; i++)
            {
                var p = i * 3;
                var v = i * 9;

                vertices[v + 0] = positions[p + 0];
                vertices[v + 1] = positions[p + 1];
                vertices[v + 2] = positions[p + 2];

                // Default per-vertex color (kept for legacy shader attribute; materials drive appearance).
                vertices[v + 3] = 1f;
                vertices[v + 4] = 1f;
                vertices[v + 5] = 1f;

                vertices[v + 6] = normals[p + 0];
                vertices[v + 7] = normals[p + 1];
                vertices[v + 8] = normals[p + 2];
            }

            var geometry = new LoadedGeometry(vertices, indices, VertexLayout.PositionColorNormal);
            var mesh = new Mesh(geometry);

            // Material mapping (very minimal): pbrMetallicRoughness.baseColorFactor -> Material.AlbedoColor
            mesh.Material = TryReadMaterial(root);

            return mesh;
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
            return componentType switch
            {
                5121 => ReadByteIndicesU32(bin, byteOffset, count),        // UNSIGNED_BYTE
                5123 => ReadUShortIndicesU32(bin, byteOffset, count),      // UNSIGNED_SHORT
                5125 => ReadUIntIndices(bin, byteOffset, count),           // UNSIGNED_INT
                _ => throw new NotSupportedException($"Unsupported index componentType: {componentType}")
            };
        }

        private static uint[] ReadByteIndicesU32(byte[] bin, int byteOffset, int count)
        {
            var indices = new uint[count];
            for (var i = 0; i < count; i++)
            {
                indices[i] = bin[byteOffset + i];
            }
            return indices;
        }

        private static uint[] ReadUShortIndicesU32(byte[] bin, int byteOffset, int count)
        {
            var indices = new uint[count];
            for (var i = 0; i < count; i++)
            {
                indices[i] = BinaryPrimitives.ReadUInt16LittleEndian(bin.AsSpan(byteOffset + (i * 2), 2));
            }
            return indices;
        }

        private static uint[] ReadUIntIndices(byte[] bin, int byteOffset, int count)
        {
            var indices = new uint[count];
            for (var i = 0; i < count; i++)
            {
                indices[i] = BinaryPrimitives.ReadUInt32LittleEndian(bin.AsSpan(byteOffset + (i * 4), 4));
            }
            return indices;
        }
    */



}
