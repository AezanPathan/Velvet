using Microsoft.JSInterop;
using Velvet.Graphics.WebGL;

namespace Velvet.Hosting.Web.MvcRuntime;

public sealed class MvcVelvetStartupContext
{
    private MvcVelvetStartupContext(
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

    public static MvcVelvetStartupContext Create(
        string canvasId,
        IServiceProvider services,
        IJSRuntime jsRuntime,
        IWebGLBridge bridge)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(jsRuntime);
        ArgumentNullException.ThrowIfNull(bridge);
        return new MvcVelvetStartupContext(canvasId, services, jsRuntime, bridge);
    }
}
