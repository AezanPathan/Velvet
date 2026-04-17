using System.Buffers.Binary;
using System.Text.Json;
using Velvet.Core.Math;
using Velvet.Core.Rendering.Skinning;

namespace Velvet.Core.Assets.Gltf;

internal static class GltfSkinReader
{

    /// <summary>
    /// Loads all skins from the glTF document.
    /// Returns a dictionary mapping skin index to Skin object.
    /// </summary>
    internal static Dictionary<int, Skin> LoadSkins(GltfContext context)
    {
        var result = new Dictionary<int, Skin>();

        if (!context.Root.TryGetProperty("skins", out var skinsEl) || skinsEl.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        if (!context.Root.TryGetProperty("nodes", out var nodesEl) || nodesEl.ValueKind != JsonValueKind.Array)
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
                var name = GltfAnimationReader.GetNodeName(nodesEl, nodeIndex);
                jointNames.Add(name);
            }

            // Load inverse bind matrices
            float[] inverseBindMatrices = null!;
            if (skinEl.TryGetProperty("inverseBindMatrices", out var ibmEl))
            {
                var accessorIndex = ibmEl.GetInt32();
                inverseBindMatrices = ReadAccessorFloatMat4(context, accessorIndex);
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
            skinIndex++;
        }

        return result;
    }

    /// <summary>
    /// Reads a MAT4 accessor and returns flattened 4x4 matrices.
    /// Each matrix is 16 floats in column-major order.
    /// </summary>



    /// <summary>
    /// Reads a MAT4 accessor and returns flattened 4x4 matrices.
    /// Each matrix is 16 floats in column-major order.
    /// </summary>
    internal static float[] ReadAccessorFloatMat4(GltfContext context, int accessorIndex)
    {
        var acc = context.Accessors[accessorIndex];

        var componentType = acc.GetProperty("componentType").GetInt32();
        if (componentType != 5126)
            throw new NotSupportedException("Only FLOAT accessors are supported for inverse bind matrices.");

        var type = acc.GetProperty("type").GetString();
        if (!string.Equals(type, "MAT4", StringComparison.Ordinal))
            throw new NotSupportedException("Only MAT4 accessors are supported for inverse bind matrices.");

        var count = acc.GetProperty("count").GetInt32();
        var viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = context.BufferViews[viewIndex];

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
                result[i * 16 + j] = BitConverter.ToSingle(context.Bin, baseByte + j * 4);
            }
        }

        return result;
    }

    /// <summary>
    /// Reads JOINTS_0 accessor (uint8 joint indices).
    /// Returns an array of 4 bytes per vertex.
    /// </summary>



    /// <summary>
    /// Reads JOINTS_0 accessor (uint8 joint indices).
    /// Returns an array of 4 bytes per vertex.
    /// </summary>
    internal static byte[] ReadAccessorJointsU8(GltfContext context, int accessorIndex, int expectedVertexCount)
    {
        var acc = context.Accessors[accessorIndex];

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
        var view = context.BufferViews[viewIndex];

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
                result[i * 4 + 0] = context.Bin[baseByte + 0];
                result[i * 4 + 1] = context.Bin[baseByte + 1];
                result[i * 4 + 2] = context.Bin[baseByte + 2];
                result[i * 4 + 3] = context.Bin[baseByte + 3];
            }
        }
        else if (componentType == 5123) // UNSIGNED_SHORT
        {
            var stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 8;
            for (int i = 0; i < count; i++)
            {
                int baseByte = byteOffset + i * stride;
                result[i * 4 + 0] = (byte)BinaryPrimitives.ReadUInt16LittleEndian(context.Bin.AsSpan(baseByte, 2));
                result[i * 4 + 1] = (byte)BinaryPrimitives.ReadUInt16LittleEndian(context.Bin.AsSpan(baseByte + 2, 2));
                result[i * 4 + 2] = (byte)BinaryPrimitives.ReadUInt16LittleEndian(context.Bin.AsSpan(baseByte + 4, 2));
                result[i * 4 + 3] = (byte)BinaryPrimitives.ReadUInt16LittleEndian(context.Bin.AsSpan(baseByte + 6, 2));
            }
        }
        else // UNSIGNED_INT
        {
            var stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 16;
            for (int i = 0; i < count; i++)
            {
                int baseByte = byteOffset + i * stride;
                result[i * 4 + 0] = (byte)BinaryPrimitives.ReadUInt32LittleEndian(context.Bin.AsSpan(baseByte, 4));
                result[i * 4 + 1] = (byte)BinaryPrimitives.ReadUInt32LittleEndian(context.Bin.AsSpan(baseByte + 4, 4));
                result[i * 4 + 2] = (byte)BinaryPrimitives.ReadUInt32LittleEndian(context.Bin.AsSpan(baseByte + 8, 4));
                result[i * 4 + 3] = (byte)BinaryPrimitives.ReadUInt32LittleEndian(context.Bin.AsSpan(baseByte + 12, 4));
            }
        }

        return result;
    }

    /// <summary>
    /// Reads WEIGHTS_0 accessor (float weights summing to 1.0).
    /// Returns an array of 4 floats per vertex.
    /// </summary>



    /// <summary>
    /// Reads WEIGHTS_0 accessor (float weights summing to 1.0).
    /// Returns an array of 4 floats per vertex.
    /// </summary>
    internal static float[] ReadAccessorFloatVec4(GltfContext context, int accessorIndex)
    {
        var acc = context.Accessors[accessorIndex];

        var componentType = acc.GetProperty("componentType").GetInt32();
        if (componentType != 5126)
            throw new NotSupportedException("Only FLOAT accessors are supported for WEIGHTS_0.");

        var type = acc.GetProperty("type").GetString();
        if (!string.Equals(type, "VEC4", StringComparison.Ordinal))
            throw new NotSupportedException("Only VEC4 accessors are supported for WEIGHTS_0.");

        var count = acc.GetProperty("count").GetInt32();
        var viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = context.BufferViews[viewIndex];

        var viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
        var accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
        var byteOffset = viewOffset + accOffset;

        var stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 16; // 4 floats * 4 bytes

        var result = new float[count * 4];
        for (int i = 0; i < count; i++)
        {
            int baseByte = byteOffset + i * stride;
            result[i * 4 + 0] = BitConverter.ToSingle(context.Bin, baseByte + 0);
            result[i * 4 + 1] = BitConverter.ToSingle(context.Bin, baseByte + 4);
            result[i * 4 + 2] = BitConverter.ToSingle(context.Bin, baseByte + 8);
            result[i * 4 + 3] = BitConverter.ToSingle(context.Bin, baseByte + 12);
        }

        return result;
    }
}
