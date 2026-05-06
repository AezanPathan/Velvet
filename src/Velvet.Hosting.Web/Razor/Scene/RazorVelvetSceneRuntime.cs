namespace Velvet.Hosting.Web.Razor.Scene;

public sealed class RazorVelvetSceneRuntime
{
    private readonly RazorVelvetSceneRegistry _sceneRegistry;

    public RazorVelvetSceneRuntime(RazorVelvetSceneRegistry sceneRegistry)
    {
        _sceneRegistry = sceneRegistry;
    }

    /// <summary>
    /// Registers a scene startup pipeline for a specific canvas id.
    /// The pipeline is executed by the engine runtime when JS invokes Start(canvasId).
    /// </summary>
    public Task StartScene(string canvasId, Func<RazorVelvetSceneBuilder, Task> build)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        ArgumentNullException.ThrowIfNull(build);

        _sceneRegistry.RegisterOrReplace(canvasId, async startupContext =>
        {
            var builder = new RazorVelvetSceneBuilder(startupContext);
            await build(builder).ConfigureAwait(false);
            await builder.StartAsync().ConfigureAwait(false);
        });

        return Task.CompletedTask;
    }
}
