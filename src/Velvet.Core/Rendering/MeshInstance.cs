using System;
using Velvet.Core.Math;

namespace Velvet.Core.Rendering;

/// <summary>
/// A mesh with an immutable world transform.
/// </summary>
public readonly struct MeshInstance
{
    public MeshInstance(Mesh mesh, float[] modelMatrix)
        : this(mesh, modelMatrix, Matrix.NormalMatrix(modelMatrix))
    {
    }

    public MeshInstance(Mesh mesh, float[] modelMatrix, float[] normalMatrix)
    {
        Mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        ModelMatrix = (float[])(modelMatrix ?? throw new ArgumentNullException(nameof(modelMatrix))).Clone();
        NormalMatrix = (float[])(normalMatrix ?? throw new ArgumentNullException(nameof(normalMatrix))).Clone();

        if (ModelMatrix.Length != 16) throw new ArgumentException("Model matrix must be 4x4.", nameof(modelMatrix));
        if (NormalMatrix.Length != 9) throw new ArgumentException("Normal matrix must be 3x3.", nameof(normalMatrix));
    }

    public Mesh Mesh { get; }

    public float[] ModelMatrix { get; }

    public float[] NormalMatrix { get; }
}
