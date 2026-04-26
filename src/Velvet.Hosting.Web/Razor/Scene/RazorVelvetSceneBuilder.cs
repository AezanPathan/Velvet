using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using Velvet.Graphics.WebGL;
using Velvet.Hosting.Web.Razor.Hosting;
using Velvet.Hosting.Web.Razor.Setup;

namespace Velvet.Hosting.Web.Razor.Scene;

public sealed class RazorVelvetSceneBuilder
{
    private readonly RazorVelvetStartupContext _startupContext;
    private Func<float, Task>? _onFrame;

    internal RazorVelvetSceneBuilder(RazorVelvetStartupContext startupContext)
    {
        _startupContext = startupContext;
    }

    public RazorVelvetHost Host
        => _host ?? throw new InvalidOperationException("Host not created. Call CreateHostAsync(...) first.");

    private RazorVelvetHost? _host;

    public async Task<RazorVelvetHost> CreateHostAsync(
        Func<IWebGLBridge, Task<ShaderProgram>> programFactory)
    {
        ArgumentNullException.ThrowIfNull(programFactory);

        if (_host is not null)
        {
            return _host;
        }

        _host = await RazorVelvetHost.CreateAsync(
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
        var hostRegistry = _startupContext.Services.GetRequiredService<RazorVelvetHostRegistry>();
        return StartCoreAsync(host, hostRegistry);
    }

    private async Task StartCoreAsync(RazorVelvetHost host, RazorVelvetHostRegistry hostRegistry)
    {
        await hostRegistry.ReplaceHostAsync(_startupContext.CanvasId, host).ConfigureAwait(false);
        await host.StartAsync(_onFrame).ConfigureAwait(false);
    }
}
