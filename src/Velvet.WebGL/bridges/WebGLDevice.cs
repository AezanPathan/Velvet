namespace Velvet.WebGL;

public sealed class WebGLDevice : WebGLGraphicsDevice
{
    public WebGLDevice(string canvasId = "velvetCanvas")
        : base(JsBridge.Require(), canvasId)
    {
    }

    public WebGLDevice(IWebGLBridge bridge, string canvasId = "velvetCanvas")
        : base(bridge, canvasId)
    {
    }
}
