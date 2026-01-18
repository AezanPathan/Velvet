namespace Velvet.Core.Engine;

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
