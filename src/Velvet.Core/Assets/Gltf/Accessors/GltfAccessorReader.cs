using System.Buffers.Binary;
using System.Text.Json;

namespace Velvet.Core.Assets.Gltf;

internal static class GltfAccessorReader
{
    internal static float[] ReadAccessorFloatScalar(GltfContext context, int accessorIndex, out int count)
    {
        var acc = context.Accessors[accessorIndex];

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
        var view = context.BufferViews[viewIndex];

        var viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
        var accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
        var byteOffset = viewOffset + accOffset;

        var stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 4;

        var result = new float[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = BitConverter.ToSingle(context.Bin, byteOffset + (i * stride));
        }

        return result;
    }


    internal static float[] ReadAccessorFloatArray(GltfContext context, int accessorIndex, int componentCount, out int count)
    {
        var acc = context.Accessors[accessorIndex];

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
        var view = context.BufferViews[viewIndex];

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
                result[i * componentCount + c] = BitConverter.ToSingle(context.Bin, baseByte + (c * 4));
            }
        }

        return result;
    }


    internal static float[] ReadAccessorFloatVec2(GltfContext context, int accessorIndex)
    {
        var acc = context.Accessors[accessorIndex];

        if (acc.GetProperty("componentType").GetInt32() != 5126)
            throw new NotSupportedException("Only FLOAT UVs supported.");

        if (acc.GetProperty("type").GetString() != "VEC2")
            throw new NotSupportedException("Only VEC2 UVs supported.");

        int count = acc.GetProperty("count").GetInt32();
        int viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = context.BufferViews[viewIndex];

        int viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
        int accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
        int byteOffset = viewOffset + accOffset;

        int stride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 8;

        var result = new float[count * 2];
        for (int i = 0; i < count; i++)
        {
            int baseByte = byteOffset + i * stride;
            result[i * 2 + 0] = BitConverter.ToSingle(context.Bin, baseByte);
            result[i * 2 + 1] = BitConverter.ToSingle(context.Bin, baseByte + 4);
        }

        return result;
    }


    internal static float[] ReadAccessorFloatVec3(GltfContext context, int accessorIndex)
    {
        var acc = context.Accessors[accessorIndex];

        var componentType = acc.GetProperty("componentType").GetInt32();
        if (componentType != 5126) throw new NotSupportedException("Only FLOAT accessors are supported for POSITION/NORMAL.");

        var type = acc.GetProperty("type").GetString();
        if (!string.Equals(type, "VEC3", StringComparison.Ordinal)) throw new NotSupportedException("Only VEC3 accessors are supported for POSITION/NORMAL.");

        var count = acc.GetProperty("count").GetInt32();
        var viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = context.BufferViews[viewIndex];

        var viewOffset = view.TryGetProperty("byteOffset", out var vo) ? vo.GetInt32() : 0;
        var accOffset = acc.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0;
        var byteOffset = viewOffset + accOffset;

        const int elementSize = 12; // 3 floats * 4 bytes
        var bufferStride = view.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 0;
        var stride = bufferStride > 0 ? bufferStride : elementSize;
        
        if (stride < elementSize) 
            throw new InvalidDataException("Invalid byteStride for VEC3 float.");

        var result = new float[count * 3];
        for (var i = 0; i < count; i++)
        {
            var baseByte = byteOffset + (i * stride);
            result[i * 3 + 0] = BitConverter.ToSingle(context.Bin, baseByte + 0);
            result[i * 3 + 1] = BitConverter.ToSingle(context.Bin, baseByte + 4);
            result[i * 3 + 2] = BitConverter.ToSingle(context.Bin, baseByte + 8);
        }

        return result;
    }


    internal static uint[] ReadAccessorIndicesU32(GltfContext context, int accessorIndex)
    {
        var acc = context.Accessors[accessorIndex];
        var type = acc.GetProperty("type").GetString();
        if (!string.Equals(type, "SCALAR", StringComparison.Ordinal)) throw new NotSupportedException("Only SCALAR accessors are supported for indices.");

        var count = acc.GetProperty("count").GetInt32();
        var viewIndex = acc.GetProperty("bufferView").GetInt32();
        var view = context.BufferViews[viewIndex];

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

        return componentType switch
        {
            5121 => ReadByteIndicesU32(context.Bin, byteOffset, count, stride),        // UNSIGNED_BYTE
            5123 => ReadUShortIndicesU32(context.Bin, byteOffset, count, stride),      // UNSIGNED_SHORT
            5125 => ReadUIntIndices(context.Bin, byteOffset, count, stride),           // UNSIGNED_INT
            _ => throw new NotSupportedException($"Unsupported index componentType: {componentType}")
        };
    }


    internal static uint[] ReadByteIndicesU32(byte[] bin, int byteOffset, int count, int stride)
    {
        var indices = new uint[count];
        for (var i = 0; i < count; i++)
        {
            indices[i] = bin[byteOffset + (i * stride)];
        }
        return indices;
    }


    internal static uint[] ReadUShortIndicesU32(byte[] bin, int byteOffset, int count, int stride)
    {
        var indices = new uint[count];
        for (var i = 0; i < count; i++)
        {
            indices[i] = BinaryPrimitives.ReadUInt16LittleEndian(bin.AsSpan(byteOffset + (i * stride), 2));
        }
        return indices;
    }


    internal static uint[] ReadUIntIndices(byte[] bin, int byteOffset, int count, int stride)
    {
        var indices = new uint[count];
        for (var i = 0; i < count; i++)
        {
            indices[i] = BinaryPrimitives.ReadUInt32LittleEndian(bin.AsSpan(byteOffset + (i * stride), 4));
        }
        return indices;
    }
}
