using System;
using System.Collections.Generic;

namespace Velvet.Core.Rendering.Skinning;

/// <summary>
/// Represents a glTF skin (skeletal deformation) for a mesh.
/// 
/// Skinning applies bone matrices to vertices based on joint indices and weights.
/// This supports GPU skinning where bone matrices are computed per-frame and uploaded to the GPU.
/// 
/// Architecture:
/// - Skin holds joint node references and inverse bind matrices
/// - MeshInstance holds per-mesh skin reference
/// - Renderer computes bone matrices each frame: jointWorldMatrix * inverseBindMatrix
/// - Vertex shader applies skinning: skinnedPosition = sum(boneMatrix[i] * localPosition * weight[i])
/// </summary>
public sealed class Skin
{
    /// <summary>
    /// List of joint node indices. Index in this list matches JOINTS_0 attribute indices.
    /// </summary>
    public IReadOnlyList<int> JointNodeIndices { get; }

    /// <summary>
    /// List of bone node names (for debugging only). Index matches <see cref="JointNodeIndices"/>.
    /// </summary>
    public IReadOnlyList<string> JointNames { get; }

    /// <summary>
    /// Inverse bind matrices (one per joint), stored as column-major 4x4 matrices.
    /// Each matrix is 16 floats: [m00, m10, m20, m30, m01, m11, ...]
    /// </summary>
    public IReadOnlyList<float[]> InverseBindMatrices { get; }

    /// <summary>
    /// Maximum number of joints that can influence a single vertex (hardware dependent, typically 4).
    /// </summary>
    public int MaxJointsPerVertex { get; } = 4;

    public Skin(IReadOnlyList<int> jointNodeIndices, IReadOnlyList<string> jointNames, IReadOnlyList<float[]> inverseBindMatrices)
    {
        ArgumentNullException.ThrowIfNull(jointNodeIndices);
        ArgumentNullException.ThrowIfNull(jointNames);
        ArgumentNullException.ThrowIfNull(inverseBindMatrices);

        if (jointNodeIndices.Count != inverseBindMatrices.Count)
        {
            throw new ArgumentException(
                $"Joint node indices count ({jointNodeIndices.Count}) must match inverse bind matrices count ({inverseBindMatrices.Count}).",
                nameof(inverseBindMatrices));
        }

        if (jointNodeIndices.Count == 0)
        {
            throw new ArgumentException("Skin must have at least one joint.", nameof(jointNodeIndices));
        }

        foreach (var matrix in inverseBindMatrices)
        {
            if (matrix?.Length != 16)
            {
                throw new ArgumentException("Each inverse bind matrix must be 4x4 (16 floats).", nameof(inverseBindMatrices));
            }
        }

        JointNodeIndices = jointNodeIndices;
        JointNames = jointNames;
        InverseBindMatrices = inverseBindMatrices;
    }

    /// <summary>
    /// Gets the number of joints in this skin.
    /// </summary>
    public int JointCount => JointNodeIndices.Count;

    /// <summary>
    /// Gets the inverse bind matrix for a joint by index.
    /// </summary>
    public float[] GetInverseBindMatrix(int index)
    {
        if (index < 0 || index >= InverseBindMatrices.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        return (float[])InverseBindMatrices[index].Clone();
    }
}
