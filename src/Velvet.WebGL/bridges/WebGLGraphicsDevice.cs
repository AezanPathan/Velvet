using System;
using System.Threading.Tasks;
using Velvet.Core.Engine;
using Velvet.Core.Rendering;

namespace Velvet.WebGL;

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
        if (Canvas is string id)
        {
            RendererId = await Bridge.InitWithIdAsync(id);
        }
        else
        {
            RendererId = await Bridge.InitWithElementAsync(Canvas);
        }

        return RendererId;
    }
}
