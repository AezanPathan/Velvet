namespace Velvet.Core.Rendering.Skinning;

/// <summary>Represents mesh skinning data (joints and inverse bind matrices).</summary>
public sealed class Skin
{
    /// <summary>Joint node indices, matching JOINTS_0 attribute indices.</summary>
    public IReadOnlyList<int> JointNodeIndices { get; }

    /// <summary>Joint names aligned with <see cref="JointNodeIndices"/>.</summary>
    public IReadOnlyList<string> JointNames { get; }

    /// <summary>Inverse bind matrices, one 4x4 matrix per joint.</summary>
    public IReadOnlyList<float[]> InverseBindMatrices { get; }

    /// <summary>Maximum joints per vertex.</summary>
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

    /// <summary>Gets the number of joints in this skin.</summary>
    public int JointCount => JointNodeIndices.Count;

    /// <summary>Gets the inverse bind matrix for a joint index.</summary>
    public float[] GetInverseBindMatrix(int index)
    {
        if (index < 0 || index >= InverseBindMatrices.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        return (float[])InverseBindMatrices[index].Clone();
    }
}
