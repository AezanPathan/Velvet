using System.Threading.Tasks;

namespace Velvet.WebGL;

/// <summary>
/// Minimal bridge that backends use to call into JavaScript.
/// Implementations can use Blazor IJSRuntime or any other mechanism.
/// </summary>
public interface IWebGLBridge
{
    Task InitAsync(string canvasId);
    Task DrawTriangleAsync();
}
