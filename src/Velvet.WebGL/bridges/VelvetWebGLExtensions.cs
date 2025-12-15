namespace Velvet.WebGL;

/// <summary>
/// Host-side extension helpers to configure Velvet WebGL integration.
/// </summary>
public static class VelvetWebGLExtensions
{
    /// <summary>
    /// Configure Velvet to use WebGL with an explicit bridge instance and canvas parameter.
    /// Canvas can be ElementReference (Blazor) or string id (static).
    /// </summary>
    public static void UseWebGL(this Velvet.Core.Engine.VelvetApp app, IWebGLBridge bridge, object canvas)
    {
        JsBridge.Configure(bridge);
        app.UseGraphics(new WebGLDevice(bridge, canvas));
    }

    /// <summary>
    /// Configure Velvet to use WebGL with the globally configured bridge
    /// and a canvas parameter (string or ElementReference).
    /// </summary>
    public static void UseWebGL(this Velvet.Core.Engine.VelvetApp app, object canvas)
    {
        JsBridge.Configure(JsBridge.Require());
        app.UseGraphics(new WebGLDevice(canvas));
    }
}
