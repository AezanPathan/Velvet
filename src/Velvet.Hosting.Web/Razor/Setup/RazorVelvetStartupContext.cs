using Microsoft.JSInterop;
using Velvet.Graphics.WebGL;

namespace Velvet.Hosting.Web.Razor.Setup;

public sealed class RazorVelvetStartupContext
{
    private RazorVelvetStartupContext(
        string canvasId,
        IServiceProvider services,
        IJSRuntime jsRuntime,
        IWebGLBridge bridge)
    {
        CanvasId = canvasId;
        Services = services;
        JsRuntime = jsRuntime;
        Bridge = bridge;
    }

    public string CanvasId { get; }

    public IServiceProvider Services { get; }

    public IJSRuntime JsRuntime { get; }

    public IWebGLBridge Bridge { get; }

    public static RazorVelvetStartupContext Create(
        string canvasId,
        IServiceProvider services,
        IJSRuntime jsRuntime,
        IWebGLBridge bridge)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(bridge);
        return new RazorVelvetStartupContext(canvasId, services, jsRuntime, bridge);
    }
}
