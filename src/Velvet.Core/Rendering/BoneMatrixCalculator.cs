using System;
using System.Collections.Generic;
using Velvet.Core.Engine;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering;

/// <summary>
/// Computes bone matrices for GPU skinning.
/// 
/// Each bone matrix is computed as: jointWorldMatrix * inverseBindMatrix
/// 
/// This is called once per mesh per frame to update the bone data that gets uploaded to the GPU.
/// </summary>
public sealed class BoneMatrixCalculator
{
    /// <summary>
    /// Computes bone matrices for a skin given a node hierarchy.
    /// 
    /// Returns an array of bone matrices (flattened to floats for GPU upload).
    /// If a joint node is not found, its bone matrix defaults to identity.
    /// </summary>
    public static float[] ComputeBoneMatrices(
        Skin skin,
        IReadOnlyList<SceneNode> roots,
        Dictionary<int, float[]>? worldTransforms = null)
    {
        ArgumentNullException.ThrowIfNull(skin);
        ArgumentNullException.ThrowIfNull(roots);

        var jointCount = skin.JointCount;
        var result = new float[jointCount * 16]; // 16 floats per 4x4 matrix

        // Build a map of node name -> world transform if not provided
        if (worldTransforms == null)
        {
            worldTransforms = new Dictionary<int, float[]>();
            BuildWorldTransformMap(roots, Matrix.Identity(), worldTransforms);
        }

        // For each joint, compute: boneMatrix = jointWorldMatrix * inverseBindMatrix
        for (int i = 0; i < jointCount; i++)
        {
            var jointNodeIndex = skin.JointNodeIndices[i];
            var inverseBindMatrix = skin.InverseBindMatrices[i];

            // Look up the joint's world transform
            float[] jointWorldMatrix;
            if (worldTransforms.TryGetValue(jointNodeIndex, out var found))
            {
                jointWorldMatrix = found;
            }
            else
            {
                // Joint not found, use identity (should log a warning in real use)
                jointWorldMatrix = Matrix.Identity();
                System.Diagnostics.Debug.WriteLine($"[BONE] WARNING: Joint node index {jointNodeIndex} not found in world transforms. Using identity.");
            }

            // Compute bone matrix
            var boneMatrix = Matrix.Multiply(jointWorldMatrix, inverseBindMatrix);

            // Copy into result array
            Array.Copy(boneMatrix, 0, result, i * 16, 16);
            
            if (i < 3)
            {
                System.Diagnostics.Debug.WriteLine($"[BONE] Joint {i} (node {jointNodeIndex}): [{boneMatrix[0]:F3}, {boneMatrix[1]:F3}, {boneMatrix[2]:F3}, {boneMatrix[3]:F3}]");
            }
        }
        System.Diagnostics.Debug.WriteLine($"[BONE] Computed {jointCount} bone matrices");

        return result;
    }

    /// <summary>
    /// Recursively builds a map of node name -> world transform.
    /// </summary>
    private static void BuildWorldTransformMap(
        IReadOnlyList<SceneNode> nodes,
        float[] parentWorld,
        Dictionary<int, float[]> output)
    {
        foreach (var node in nodes)
        {
            var worldMatrix = Matrix.Multiply(parentWorld, node.LocalTransform);

            if (node.NodeIndex >= 0)
            {
                output[node.NodeIndex] = worldMatrix;
            }

            BuildWorldTransformMap(node.Children, worldMatrix, output);
        }
    }
}
