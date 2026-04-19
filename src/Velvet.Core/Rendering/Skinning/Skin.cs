namespace Velvet.Core.Rendering.Skinning;

/// <summary>
/// Holds skeletal data for a skinned mesh.
/// </summary>
public sealed class Skin
{
    /// <summary>Node indices that act as joints.</summary>
    public IReadOnlyList<int> JointNodeIndices { get; }

    /// <summary>Optional joint names (for debugging/tools).</summary>
    public IReadOnlyList<string> JointNames { get; }

    /// <summary>Inverse bind matrices (one per joint).</summary>
    public IReadOnlyList<float[]> InverseBindMatrices { get; }

    /// <summary>Maximum joints influencing a vertex (usually 4).</summary>
    public int MaxJointsPerVertex { get; } = 4;

    public Skin(
        IReadOnlyList<int> jointNodeIndices,
        IReadOnlyList<string> jointNames,
        IReadOnlyList<float[]> inverseBindMatrices)
    {
        ArgumentNullException.ThrowIfNull(jointNodeIndices);
        ArgumentNullException.ThrowIfNull(inverseBindMatrices);

        if (jointNodeIndices.Count == 0)
            throw new ArgumentException("Skin must contain joints.");

        if (jointNodeIndices.Count != inverseBindMatrices.Count)
            throw new ArgumentException("Joint count must match inverse bind matrices.");

        foreach (var m in inverseBindMatrices)
        {
            if (m?.Length != 16)
                throw new ArgumentException("Inverse bind matrix must be 4x4.");
        }

        JointNodeIndices = jointNodeIndices;
        JointNames = jointNames;
        InverseBindMatrices = inverseBindMatrices;
    }

    public int JointCount => JointNodeIndices.Count;

    public float[] GetInverseBindMatrix(int index)
    {
        if (index < 0 || index >= JointCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return (float[])InverseBindMatrices[index].Clone();
    }
}