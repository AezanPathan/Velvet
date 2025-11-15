using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Velvet.Core.Engine;
using Velvet.Blazor;

namespace Velvet.Demo.Blazor.Pages;

public partial class Index : ComponentBase
{
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        var app = new VelvetApp();
        app.UseWebGL(JSRuntime);
        app.Add(new DrawTriangle());
        await app.RunAsync();
    }
}
