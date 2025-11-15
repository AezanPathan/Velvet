using System.Threading.Tasks;

namespace Velvet.Core.Rendering;

/// <summary>
/// Abstraction for a graphics device. Engine code depends on this only.
/// Backends implement this to perform real rendering.
/// </summary>
public interface IGraphicsDevice
{
    Task InitializeAsync();
    Task DrawTriangleAsync();
}
