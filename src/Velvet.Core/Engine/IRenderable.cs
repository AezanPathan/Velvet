using System.Threading.Tasks;
using Velvet.Core.Rendering;

namespace Velvet.Core.Engine;

public interface IRenderable
{
    Task RenderAsync(IGraphicsDevice device);
}
