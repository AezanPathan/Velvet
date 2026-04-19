using Velvet.Core.Scene;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering.Skinning;

/// <summary>
/// Computes per-joint bone matrices for GPU skinning.
/// Each matrix transforms vertices from bind pose into current pose.
/// </summary>
public sealed class BoneMatrixCalculator
{
    /// <summary>
    /// Builds bone matrices for a skin.
    /// Result is a flat array (jointCount * 16) ready for GPU upload.
    /// </summary>
    public static float[] ComputeBoneMatrices(
        Skin skin,
        IReadOnlyList<SceneNode> roots,
        Dictionary<int, float[]>? worldTransforms = null)
    {
        ArgumentNullException.ThrowIfNull(skin);
        ArgumentNullException.ThrowIfNull(roots);

        var jointCount = skin.JointCount;
        var result = new float[jointCount * 16];

        if (worldTransforms == null)
        {
            worldTransforms = new Dictionary<int, float[]>();
            BuildWorldTransformMap(roots, Matrix4.Identity.Data, worldTransforms);
        }

        for (int i = 0; i < jointCount; i++)
        {
            var jointIndex = skin.JointNodeIndices[i];
            var inverseBind = skin.InverseBindMatrices[i];

            var world = worldTransforms.TryGetValue(jointIndex, out var m)
                ? m
                : Matrix4.Identity.Data;

            var bone = Matrix4.Multiply(world, inverseBind).Data;
            Array.Copy(bone, 0, result, i * 16, 16);
        }

        return result;
    }

    private static void BuildWorldTransformMap(
        IReadOnlyList<SceneNode> nodes,
        float[] parentWorld,
        Dictionary<int, float[]> output)
    {
        foreach (var node in nodes)
        {
            var world = Matrix4.Multiply(parentWorld, node.LocalTransform).Data;

            if (node.NodeIndex >= 0)
                output[node.NodeIndex] = world;

            BuildWorldTransformMap(node.Children, world, output);
        }
    }
}