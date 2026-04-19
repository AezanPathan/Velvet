namespace Velvet.Core.Scene;

using Velvet.Core.Math;
using Velvet.Core.Rendering.Bounds;
using Velvet.Core.Rendering.Meshes;

public sealed class Scene
{
    private readonly List<SceneNode> _roots;

    public Scene(IEnumerable<SceneNode> roots)
    {
        ArgumentNullException.ThrowIfNull(roots);
        _roots = [.. roots];
    }

    public IReadOnlyList<SceneNode> Roots => _roots;

    public void CollectMeshes(List<MeshInstance> output)
    {
        ArgumentNullException.ThrowIfNull(output);

        foreach (var root in _roots)
        {
            root.CollectMeshes(output, Matrix4.Identity.Data);
        }
    }

    public BoundingBox ComputeBounds()
    {
        BoundingBox? bounds = null;

        foreach (var root in _roots)
        {
            var rootBounds = root.ComputeBoundsRecursive(Matrix4.Identity.Data);
            if (rootBounds.HasValue)
                SceneBoundsAccumulator.Expand(ref bounds, rootBounds.Value);
            
        }

        return bounds ?? new BoundingBox(Vector3.Zero, Vector3.Zero);
    }
}
