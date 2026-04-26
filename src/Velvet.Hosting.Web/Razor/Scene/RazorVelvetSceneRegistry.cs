using System.Collections.Concurrent;
using Velvet.Hosting.Web.Razor.Setup;

namespace Velvet.Hosting.Web.Razor.Scene;

public sealed class RazorVelvetSceneRegistry
{
    private readonly ConcurrentDictionary<string, Func<RazorVelvetStartupContext, Task>> _sceneStarters =
        new(StringComparer.Ordinal);

    public void RegisterOrReplace(string canvasId, Func<RazorVelvetStartupContext, Task> startup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        ArgumentNullException.ThrowIfNull(startup);

        _sceneStarters[canvasId] = startup;
    }

    public bool TryGet(string canvasId, out Func<RazorVelvetStartupContext, Task>? startup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        return _sceneStarters.TryGetValue(canvasId, out startup);
    }
}
