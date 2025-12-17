using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Velvet.Core.Geometry;
using Velvet.Core.Rendering;
using Velvet.Blazor;

namespace Velvet.Demo.Blazor.Pages;

public partial class CubeDemo : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference canvasRef;

    private VelvetApp? app;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        app = await VelvetApp.CreateAsync(canvasRef, JS);
        await app.UseDefaultShaderAsync();

        var cube = new Mesh(new CubeGeometry());
        app.Add(cube);

        await app.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.StopAsync();
        }
    }
}
