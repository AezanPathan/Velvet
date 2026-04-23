namespace Velvet.Hosting.Web.MvcRuntime;

public sealed class MvcVelvetSceneRuntime
{
    private readonly MvcVelvetSceneRegistry _sceneRegistry;

    public MvcVelvetSceneRuntime(MvcVelvetSceneRegistry sceneRegistry)
    {
        _sceneRegistry = sceneRegistry;
    }

    /// <summary>
    /// Registers a scene startup pipeline for a specific canvas id.
    /// The pipeline is executed by the engine runtime when JS invokes Start(canvasId).
    /// </summary>
    public Task StartScene(string canvasId, Func<MvcVelvetSceneBuilder, Task> build)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        ArgumentNullException.ThrowIfNull(build);

        _sceneRegistry.RegisterOrReplace(canvasId, async startupContext =>
        {
            var builder = new MvcVelvetSceneBuilder(startupContext);
            await build(builder).ConfigureAwait(false);
            await builder.StartAsync().ConfigureAwait(false);
        });

        return Task.CompletedTask;
    }
}
