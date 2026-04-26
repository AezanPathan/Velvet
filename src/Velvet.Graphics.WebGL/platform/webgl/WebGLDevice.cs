namespace Velvet.Graphics.WebGL;

/// <summary>
/// Lightweight wrapper that provides a concrete WebGL device instance.
/// Accepts either an IWebGLBridge + canvas, or uses the global JsBridge configured instance.
/// </summary>
public sealed class WebGLDevice : WebGLGraphicsDevice
{
    #region Constructors

    public WebGLDevice(IWebGLBridge bridge, object canvas)
        : base(bridge, canvas)
    {
    }

    public WebGLDevice(object canvas)
        : base(JsBridge.Require(), canvas)
    {
    }

    #endregion
}
