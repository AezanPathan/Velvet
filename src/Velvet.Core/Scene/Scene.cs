namespace Velvet.Core.Scene;

using System;
using System.Collections.Generic;
using Velvet.Core.Math;
using Velvet.Core.Rendering;

public sealed class Scene
{
	private readonly List<SceneNode> _roots;

	public Scene(IEnumerable<SceneNode> roots)
	{
		ArgumentNullException.ThrowIfNull(roots);

		_roots = new List<SceneNode>(roots);
	}

	public IReadOnlyList<SceneNode> Roots => _roots;

	public void CollectMeshes(List<MeshInstance> output)
	{
		ArgumentNullException.ThrowIfNull(output);

		foreach (var root in _roots)
		{
			root.CollectMeshes(output, Matrix.Identity());
		}
	}

	public BoundingBox ComputeBounds()
	{
		BoundingBox? bounds = null;

		foreach (var root in _roots)
		{
			var rootBounds = root.ComputeBoundsRecursive(Matrix.Identity());
			if (rootBounds.HasValue)
			{
				if (bounds == null)
					bounds = rootBounds;
				
				else
					bounds.Value.Expand(rootBounds.Value);
				
			}
		}

		return bounds ?? new BoundingBox(Vector3.Zero, Vector3.Zero);
	}
}
