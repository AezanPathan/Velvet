namespace Velvet.Graphics.WebGL;

/// <summary>
/// Simple global bridge registry so hosts can wire in their preferred JS transport.
/// </summary>
public static class JsBridge
{
    private static IWebGLBridge? _bridge;

    public static void Configure(IWebGLBridge bridge)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
    }

    public static IWebGLBridge Require()
    {
        if (_bridge is null)
        {
            throw new InvalidOperationException("No WebGL bridge configured. Call JsBridge.Configure(...) or provide a WebGLDevice with an explicit bridge.");
        }

        return _bridge;
    }
}
