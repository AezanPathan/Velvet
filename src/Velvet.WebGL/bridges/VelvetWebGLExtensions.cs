using Velvet.Core.Engine;

namespace Velvet.WebGL;

public static class VelvetWebGLExtensions
{
    /// <summary>
    /// Configure Velvet to use WebGL with a custom bridge implementation.
    /// </summary>
    public static void UseWebGL(this VelvetApp app, IWebGLBridge bridge, string canvasId = "velvetCanvas")
    {
        JsBridge.Configure(bridge);
        app.UseGraphics(new WebGLDevice(canvasId));
    }

    /// <summary>
    /// Configure Velvet to use the built-in static WebAssembly bridge (no JS runtime required).
    /// </summary>
    public static void UseWebGL(this VelvetApp app, string canvasId = "velvetCanvas")
    {
        JsBridge.Configure(new StaticWebGLBridge());
        app.UseGraphics(new WebGLDevice(canvasId));
    }
}
