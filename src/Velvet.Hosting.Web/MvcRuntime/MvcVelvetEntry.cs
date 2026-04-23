using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Velvet.Graphics.WebGL;
using System.Collections.Concurrent;
using System.Threading;

namespace Velvet.Hosting.Web.MvcRuntime;

public static class MvcVelvetEntry
{
    private static IServiceProvider? _services;
    private static readonly ConcurrentDictionary<string, Lazy<Task>> _startupTasks = new(StringComparer.Ordinal);

    internal static void Configure(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    [JSInvokable]
    public static async Task Start(string canvasId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);

        var lazyTask = _startupTasks.GetOrAdd(
            canvasId,
            static id => new Lazy<Task>(() => StartCoreAsync(id), LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            await lazyTask.Value.ConfigureAwait(false);
        }
        catch
        {
            throw;
        }
        finally
        {
            // A Razor view can tear down and recreate the canvas (same id) during subsequent renders.
            // Keep Start(...) re-entrant across those lifecycles by allowing a fresh startup attempt.
            _startupTasks.TryRemove(canvasId, out _);
        }
    }

    private static async Task StartCoreAsync(string canvasId)
    {
        var services = _services ?? throw new InvalidOperationException(
            "MVC Velvet runtime is not configured. Call app.UseMvcVelvetRuntime() at startup.");

        var options = services.GetRequiredService<MvcVelvetStartupOptions>();
        var runtimeAccessor = services.GetRequiredService<MvcVelvetRuntimeAccessor>();
        var sceneRegistry = services.GetRequiredService<MvcVelvetSceneRegistry>();

        IJSRuntime? js = null;
        for (var i = 0; i < 100; i++)
        {
            js = runtimeAccessor.Current;
            if (js is not null)
            {
                break;
            }

            await Task.Delay(50).ConfigureAwait(false);
        }

        if (js is null)
        {
            throw new InvalidOperationException(
                "No active Blazor circuit runtime yet. Retry start after connection is established.");
        }

        var bridge = services.GetRequiredService<IWebGLBridge>();
        var context = MvcVelvetStartupContext.Create(canvasId, services, js, bridge);

        if (sceneRegistry.TryGet(canvasId, out var sceneStartup) && sceneStartup is not null)
        {
            await sceneStartup(context).ConfigureAwait(false);
            return;
        }

        await options.Startup(context).ConfigureAwait(false);
    }
}
