using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Velvet.WebGL;

public sealed class BlazorWebGLBridge : IWebGLBridge
{
    private readonly IJSRuntime _js;

    public BlazorWebGLBridge(IJSRuntime js)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
    }

    public async Task InitAsync(string canvasId)
    {
        await _js.InvokeVoidAsync("Velvet.ensureCanvas", canvasId);
        await _js.InvokeVoidAsync("Velvet.init", canvasId);
    }

    public Task DrawTriangleAsync()
    {
        return _js.InvokeVoidAsync("Velvet.drawTriangle").AsTask();
    }
}
