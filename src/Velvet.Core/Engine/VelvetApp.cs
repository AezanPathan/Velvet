using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Velvet.Core.Rendering;

namespace Velvet.Core.Engine;

public sealed class VelvetApp
{
    private readonly List<IRenderable> _renderables = new();
    private IGraphicsDevice? _device;

    public void UseGraphics(IGraphicsDevice device)
    {
        _device = device;
    }

    public void Add(IRenderable renderable)
    {
        ArgumentNullException.ThrowIfNull(renderable);
        _renderables.Add(renderable);
    }

    public async Task RunAsync()
    {
        if (_device is null)
            throw new InvalidOperationException("No graphics device configured. Call UseGraphics(...) before RunAsync.");

        await _device.InitializeAsync().ConfigureAwait(false);

        foreach (var r in _renderables)
        {
            await r.RenderAsync(_device).ConfigureAwait(false);
        }
    }
}
