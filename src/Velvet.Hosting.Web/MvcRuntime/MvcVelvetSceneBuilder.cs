using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Velvet.Graphics.WebGL;

namespace Velvet.Hosting.Web.MvcRuntime;

public sealed class MvcVelvetSceneBuilder
{
    private readonly MvcVelvetStartupContext _startupContext;
    private Func<float, Task>? _onFrame;

    internal MvcVelvetSceneBuilder(MvcVelvetStartupContext startupContext)
    {
        _startupContext = startupContext;
    }

    public MvcVelvetHost Host
        => _host ?? throw new InvalidOperationException("Host not created. Call CreateHostAsync(...) first.");

    private MvcVelvetHost? _host;

    public async Task<MvcVelvetHost> CreateHostAsync(
        Func<IWebGLBridge, Task<ShaderProgram>> programFactory)
    {
        ArgumentNullException.ThrowIfNull(programFactory);

        if (_host is not null)
        {
            return _host;
        }

        _host = await MvcVelvetHost.CreateAsync(
            _startupContext.CanvasId,
            _startupContext.JsRuntime,
            programFactory,
            _startupContext.Bridge).ConfigureAwait(false);

        return _host;
    }

    public void OnFrame(Func<float, Task> onFrame)
    {
        ArgumentNullException.ThrowIfNull(onFrame);
        _onFrame = onFrame;
    }

    public Task StartAsync()
    {
        var host = Host;
        var hostRegistry = _startupContext.Services.GetRequiredService<MvcVelvetHostRegistry>();
        return StartCoreAsync(host, hostRegistry);
    }

    private async Task StartCoreAsync(MvcVelvetHost host, MvcVelvetHostRegistry hostRegistry)
    {
        await hostRegistry.ReplaceHostAsync(_startupContext.CanvasId, host).ConfigureAwait(false);
        await host.StartAsync(_onFrame).ConfigureAwait(false);
    }
}
