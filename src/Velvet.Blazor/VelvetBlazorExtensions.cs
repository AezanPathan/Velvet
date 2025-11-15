using Microsoft.JSInterop;
using Velvet.Core.Engine;
using Velvet.WebGL;

namespace Velvet.Blazor;

public static class VelvetBlazorExtensions
{
    /// <summary>
    /// Convenience: configure the app to use the WebGL backend using Blazor's IJSRuntime.
    /// </summary>
    public static void UseWebGL(this VelvetApp app, IJSRuntime jsRuntime, string canvasId = "velvetCanvas")
    {
        var bridge = new BlazorWebGLBridge(jsRuntime);
        JsBridge.Configure(bridge);
        app.UseGraphics(new WebGLDevice(canvasId));
    }
}
