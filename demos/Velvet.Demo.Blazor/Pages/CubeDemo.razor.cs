using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering;
using Velvet.Blazor;
using Velvet.WebGL;

namespace Velvet.Demo.Blazor.Pages;

public partial class CubeDemo : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference canvasRef;

    private VelvetApp? app;
    private float angle;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        app = await VelvetApp.CreateAsync(canvasRef, JS, ShaderProgram.CreateDefaultAsync);

        var camera = new Camera(
            position: new Vec3(0, 0, 3.0f),
            target: new Vec3(0, 0, 0),
            up: Vec3.UnitY,
            fovYRadians: 60.0f * (System.MathF.PI / 180.0f),
            aspectRatio: 800.0f / 600.0f,
            nearPlane: 0.1f,
            farPlane: 100.0f);

        await app.Program.SetUniformMatrix4fvAsync("uView", camera.ViewMatrix);
        await app.Program.SetUniformMatrix4fvAsync("uProjection", camera.ProjectionMatrix);

        var cube = new Mesh(new CubeGeometry());
        app.Add(cube);

        await app.StartAsync(async dt =>
        {
            angle += dt * 1.2f;
            var model = Mat4.Multiply(Mat4.RotateY(angle), Mat4.RotateX(angle * 0.7f));
            await app.Program.SetUniformMatrix4fvAsync("uModel", model);
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.StopAsync();
        }
    }
}
