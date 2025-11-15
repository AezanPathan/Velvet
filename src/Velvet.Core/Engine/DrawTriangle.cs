using System;
using System.Threading.Tasks;
using Velvet.Core.Rendering;

namespace Velvet.Core.Engine;

public sealed class DrawTriangle : IRenderable
{
    public async Task RenderAsync(IGraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        await device.DrawTriangleAsync().ConfigureAwait(false);
    }
}
