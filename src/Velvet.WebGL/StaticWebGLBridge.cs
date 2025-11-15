using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;

namespace Velvet.WebGL;

/// <summary>
/// IWebGLBridge implementation for plain WebAssembly hosts using the JS import/export APIs.
/// </summary>
public sealed partial class StaticWebGLBridge : IWebGLBridge
{
    public Task InitAsync(string canvasId)
    {
        VelvetEnsureCanvas(canvasId);
        VelvetInit(canvasId);
        return Task.CompletedTask;
    }

    public Task DrawTriangleAsync()
    {
        VelvetDrawTriangle();
        return Task.CompletedTask;
    }

    [JSImport("globalThis.Velvet.ensureCanvas")] private static partial void VelvetEnsureCanvas(string canvasId);
    [JSImport("globalThis.Velvet.init")] private static partial void VelvetInit(string canvasId);
    [JSImport("globalThis.Velvet.drawTriangle")] private static partial void VelvetDrawTriangle();
}
