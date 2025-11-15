using System.Threading.Tasks;
using Velvet.Core.Rendering;

namespace Velvet.WebGL;

public class WebGLGraphicsDevice : IGraphicsDevice
{
    private readonly IWebGLBridge _bridge;
    private readonly string _canvasId;

    protected WebGLGraphicsDevice(IWebGLBridge bridge, string canvasId)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _canvasId = canvasId;
    }

    public virtual Task InitializeAsync()
    {
        return _bridge.InitAsync(_canvasId);
    }

    public Task DrawTriangleAsync()
    {
        return _bridge.DrawTriangleAsync();
    }
}
