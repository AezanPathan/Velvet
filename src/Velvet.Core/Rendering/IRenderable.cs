using System.Threading.Tasks;
using Velvet.Core.Graphics;

namespace Velvet.Core.Rendering;

public interface IRenderable
{
    Task RenderAsync(IGraphicsDevice device);
}
