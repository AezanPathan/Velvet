using System.Text.Json;
using Velvet.Core.Geometry;
using Velvet.Core.Rendering.Meshes;

namespace Velvet.Core.Assets.Gltf;

internal static class GltfMeshReader
{
    internal static List<List<Mesh>> LoadMeshesByIndex(GltfContext context, string? baseUrl = null)
    {
        var meshesOut = new List<List<Mesh>>();

        var meshes = context.Root.GetProperty("meshes");

        foreach (var meshEl in meshes.EnumerateArray())
        {
            if (!meshEl.TryGetProperty("primitives", out var primitives) || primitives.ValueKind != JsonValueKind.Array)
            {
                meshesOut.Add([]);
                continue;
            }

            var meshPrimitives = new List<Mesh>();
            foreach (var prim in primitives.EnumerateArray())
            {
                var mesh = LoadPrimitive(prim, context, baseUrl);
                if (mesh != null)
                {
                    meshPrimitives.Add(mesh);
                }
            }

            meshesOut.Add(meshPrimitives);
        }

        return meshesOut;
    }


    internal static Mesh LoadPrimitive(JsonElement prim, GltfContext context, string? baseUrl = null)
    {
        if (!prim.TryGetProperty("attributes", out var attrs))
        {
            // Likely KHR_draco_mesh_compression or unsupported primitive
            return null!;
        }

        var positions = ReadPositions(attrs, context);
        var vertexCount = positions.Length / 3;
        var indices = ReadPrimitiveIndices(prim, context);
        var normals = ReadNormals(attrs, context, positions, indices);
        var uvs = ReadUvs(attrs, context, vertexCount);
        var skinning = TryReadSkinningData(attrs, context, vertexCount);

        var vertices = skinning.HasValue
            ? PackSkinnedVertices(positions, normals, uvs, skinning.Value.Joints, skinning.Value.Weights)
            : PackStandardVertices(positions, normals, uvs);

        var vertexLayout = skinning.HasValue
            ? VertexLayout.PositionNormalUVSkinnedJointsWeights
            : VertexLayout.PositionNormalUV;

        var geo = new LoadedGeometry(vertices, indices, vertexLayout);
        var mesh = new Mesh(geo);
        var materialIndex = prim.TryGetProperty("material", out var materialEl) ? materialEl.GetInt32() : (int?)null;
        mesh.Material = GltfMaterialReader.TryReadMaterial(context.Root, context.Bin, baseUrl, materialIndex);

        return mesh;
    }


    internal static float[] ReadPositions(JsonElement attrs, GltfContext context)
    {
        var positionAccessor = attrs.GetProperty("POSITION").GetInt32();
        return GltfAccessorReader.ReadAccessorFloatVec3(context, positionAccessor);
    }


    internal static uint[]? ReadPrimitiveIndices(JsonElement prim, GltfContext context)
    {
        return prim.TryGetProperty("indices", out var idx)
            ? GltfAccessorReader.ReadAccessorIndicesU32(context, idx.GetInt32())
            : null;
    }


    internal static float[] ReadNormals(
        JsonElement attrs,
        GltfContext context,
        float[] positions,
        uint[]? indices)
    {
        if (attrs.TryGetProperty("NORMAL", out var normalEl))
        {
            return GltfAccessorReader.ReadAccessorFloatVec3(context, normalEl.GetInt32());
        }

        // Fallback normals: compute from geometry when normals are missing (e.g., Fox.glb)
        return ComputeNormals(positions, indices);
    }


    internal static float[] ReadUvs(
        JsonElement attrs,
        GltfContext context,
        int vertexCount)
    {
        if (attrs.TryGetProperty("TEXCOORD_0", out var uvEl))
        {
            return GltfAccessorReader.ReadAccessorFloatVec2(context, uvEl.GetInt32());
        }

        return new float[vertexCount * 2];
    }


    internal static (byte[] Joints, float[] Weights)? TryReadSkinningData(
        JsonElement attrs,
        GltfContext context,
        int vertexCount)
    {
        if (!attrs.TryGetProperty("JOINTS_0", out var jointsEl) || !attrs.TryGetProperty("WEIGHTS_0", out var weightsEl))
        {
            return null;
        }

        var joints = GltfSkinReader.ReadAccessorJointsU8(context, jointsEl.GetInt32(), vertexCount);
        var weights = GltfSkinReader.ReadAccessorFloatVec4(context, weightsEl.GetInt32());
        NormalizeWeightsInPlace(weights, vertexCount);
        return (joints, weights);
    }


    internal static void NormalizeWeightsInPlace(float[] weights, int vertexCount)
    {
        // JOINTS_0 validation happens at runtime with the attached skin.
        for (var i = 0; i < vertexCount; i++)
        {
            var weightOffset = i * 4;
            var weightSum = weights[weightOffset + 0] + weights[weightOffset + 1] + weights[weightOffset + 2] + weights[weightOffset + 3];
            if (System.Math.Abs(weightSum - 1.0f) <= 0.01f)
            {
                continue;
            }

            if (weightSum > 0.0001f)
            {
                weights[weightOffset + 0] /= weightSum;
                weights[weightOffset + 1] /= weightSum;
                weights[weightOffset + 2] /= weightSum;
                weights[weightOffset + 3] /= weightSum;
            }
            else
            {
                weights[weightOffset + 0] = 1.0f;
            }
        }
    }


    internal static float[] PackSkinnedVertices(float[] positions, float[] normals, float[] uvs, byte[] joints, float[] weights)
    {
        // POSITION(3) + NORMAL(3) + UV(2) + JOINTS(4 as floats) + WEIGHTS(4)
        var vertexCount = positions.Length / 3;
        var vertices = new float[vertexCount * 16];

        for (var i = 0; i < vertexCount; i++)
        {
            var positionOffset = i * 3;
            var vertexOffset = i * 16;
            var uvOffset = i * 2;
            var weightOffset = i * 4;

            vertices[vertexOffset + 0] = positions[positionOffset + 0];
            vertices[vertexOffset + 1] = positions[positionOffset + 1];
            vertices[vertexOffset + 2] = positions[positionOffset + 2];

            vertices[vertexOffset + 3] = normals[positionOffset + 0];
            vertices[vertexOffset + 4] = normals[positionOffset + 1];
            vertices[vertexOffset + 5] = normals[positionOffset + 2];

            vertices[vertexOffset + 6] = uvs[uvOffset + 0];
            vertices[vertexOffset + 7] = uvs[uvOffset + 1];

            vertices[vertexOffset + 8] = joints[weightOffset + 0];
            vertices[vertexOffset + 9] = joints[weightOffset + 1];
            vertices[vertexOffset + 10] = joints[weightOffset + 2];
            vertices[vertexOffset + 11] = joints[weightOffset + 3];

            vertices[vertexOffset + 12] = weights[weightOffset + 0];
            vertices[vertexOffset + 13] = weights[weightOffset + 1];
            vertices[vertexOffset + 14] = weights[weightOffset + 2];
            vertices[vertexOffset + 15] = weights[weightOffset + 3];
        }

        return vertices;
    }


    internal static float[] PackStandardVertices(float[] positions, float[] normals, float[] uvs)
    {
        // POSITION(3) + NORMAL(3) + UV(2)
        var vertexCount = positions.Length / 3;
        var vertices = new float[vertexCount * 8];

        for (var i = 0; i < vertexCount; i++)
        {
            var positionOffset = i * 3;
            var vertexOffset = i * 8;
            var uvOffset = i * 2;

            vertices[vertexOffset + 0] = positions[positionOffset + 0];
            vertices[vertexOffset + 1] = positions[positionOffset + 1];
            vertices[vertexOffset + 2] = positions[positionOffset + 2];

            vertices[vertexOffset + 3] = normals[positionOffset + 0];
            vertices[vertexOffset + 4] = normals[positionOffset + 1];
            vertices[vertexOffset + 5] = normals[positionOffset + 2];

            vertices[vertexOffset + 6] = uvs[uvOffset + 0];
            vertices[vertexOffset + 7] = uvs[uvOffset + 1];
        }

        return vertices;
    }


    internal static float[] ComputeNormals(float[] positions, uint[]? indices)
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
}
