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
