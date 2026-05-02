using Velvet.Core.Graphics;

namespace Velvet.Graphics.WebGL;

public class WebGLGraphicsDevice : IGraphicsDevice
{
    protected readonly IWebGLBridge Bridge;
    protected readonly object Canvas;
    protected int RendererId = -1;

    public WebGLGraphicsDevice(IWebGLBridge bridge, object canvas)
    {
        Bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        Canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
    }

    public virtual async Task<int> InitializeAsync()
    {
        RendererId = Canvas is string id
            ? await Bridge.InitWithIdAsync(id)
            : await Bridge.InitWithElementAsync(Canvas);

        return RendererId;
    }
}