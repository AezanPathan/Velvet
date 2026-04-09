namespace Velvet.Core.Scene;

using System;
using System.Collections.Generic;
using Velvet.Core.Math;
using Velvet.Core.Rendering;

/// <summary>
/// Lightweight scene graph: a set of root nodes with precomputed mesh instances.
/// </summary>
public sealed class Scene
{
	private readonly List<SceneNode> _roots;
	private readonly List<MeshInstance> _meshInstances;

	public Scene(IEnumerable<SceneNode> roots)
	{
		ArgumentNullException.ThrowIfNull(roots);

		_roots = new List<SceneNode>(roots);
		_meshInstances = BuildInstances(_roots);
	}

	/// <summary>
	/// Root nodes of the active scene.
	/// </summary>
	public IReadOnlyList<SceneNode> Roots => _roots;

	/// <summary>
	/// Flattened mesh instances with baked world and normal matrices.
	/// </summary>
	public IReadOnlyList<MeshInstance> MeshInstances => _meshInstances;

	/// <summary>
	/// Computes the world-space axis-aligned bounding box of the entire scene.
	/// Recursively traverses the scene graph, accumulating world matrices and transforming vertex positions.
	/// Returns a bounding box that encloses all geometry in all meshes of all nodes.
	/// </summary>
	public BoundingBox ComputeBounds()
	{
		BoundingBox? bounds = null;

		foreach (var root in _roots)
		{
			var rootBounds = root.ComputeLocalBounds(Matrix.Identity());
			if (rootBounds.HasValue)
			{
				if (bounds == null)
				{
					bounds = rootBounds;
				}
				else
				{
					bounds.Value.Expand(rootBounds.Value);
				}
			}
		}

		// Return an empty bounding box at origin if the scene has no geometry.
		return bounds ?? new BoundingBox(Vector3.Zero, Vector3.Zero);
	}

	/// <summary>
	/// Updates mesh instance model/normal matrices in-place using a local-transform override.
	/// This is intended for animation: provide updated local transforms per node and the scene
	/// will recompute world matrices before rendering.
	/// </summary>
	/// <param name="localTransformOverride">
	/// Callback that returns an updated local transform for a node, or null to use the node's original local transform.
	/// </param>
	public void UpdateMeshInstances(Func<SceneNode, float[]?> localTransformOverride)
	{
		ArgumentNullException.ThrowIfNull(localTransformOverride);

		var instanceIndex = 0;
		foreach (var root in _roots)
		{
			UpdateNodeMeshes(root, Matrix.Identity(), localTransformOverride, _meshInstances, ref instanceIndex);
		}
	}

	private static void UpdateNodeMeshes(
		SceneNode node,
		float[] parentWorld,
		Func<SceneNode, float[]?> localTransformOverride,
		List<MeshInstance> instances,
		ref int instanceIndex)
	{
		var localTransform = localTransformOverride(node) ?? node.LocalTransform;
		var world = Matrix.Multiply(parentWorld, localTransform);

		foreach (var mesh in node.Meshes)
		{
			if (instanceIndex >= instances.Count)
			{
				throw new InvalidOperationException("Mesh instance count mismatch while updating transforms.");
			}

			var instance = instances[instanceIndex];
			CopyMatrix(world, instance.ModelMatrix);
			CopyMatrix3(Matrix.NormalMatrix(world), instance.NormalMatrix);
			instanceIndex++;
		}

		foreach (var child in node.Children)
		{
			UpdateNodeMeshes(child, world, localTransformOverride, instances, ref instanceIndex);
		}
	}

	private static void CopyMatrix(float[] source, float[] destination)
	{
		if (destination.Length != 16 || source.Length != 16)
		{
			throw new ArgumentException("Expected 4x4 matrices (16 floats).", nameof(destination));
		}

		for (int i = 0; i < 16; i++)
		{
			destination[i] = source[i];
		}
	}

	private static void CopyMatrix3(float[] source, float[] destination)
	{
		if (destination.Length != 9 || source.Length != 9)
		{
			throw new ArgumentException("Expected 3x3 matrices (9 floats).", nameof(destination));
		}

		for (int i = 0; i < 9; i++)
		{
			destination[i] = source[i];
		}
	}

	private static List<MeshInstance> BuildInstances(IEnumerable<SceneNode> roots)
	{
		var instances = new List<MeshInstance>();
		foreach (var root in roots)
		{
			root.CollectMeshes(instances, Matrix.Identity());
		}

		return instances;
	}
}
