using System.Collections.Concurrent;

namespace Velvet.Hosting.Web.MvcRuntime;

public sealed class MvcVelvetSceneRegistry
{
    private readonly ConcurrentDictionary<string, Func<MvcVelvetStartupContext, Task>> _sceneStarters =
        new(StringComparer.Ordinal);

    public void RegisterOrReplace(string canvasId, Func<MvcVelvetStartupContext, Task> startup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        ArgumentNullException.ThrowIfNull(startup);

        _sceneStarters[canvasId] = startup;
    }

    public bool TryGet(string canvasId, out Func<MvcVelvetStartupContext, Task>? startup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        return _sceneStarters.TryGetValue(canvasId, out startup);
    }
}
