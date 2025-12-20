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

        // Create a single directional light and upload its uniforms (engine-grade, minimal).
        var light = new DirectionalLight(new Vec3(0.5f, -1.0f, -0.3f), new Vec3(1, 1, 1), 1.25f);
        await app.Program.SetUniform3fAsync("uLightDirection", light.Direction.X, light.Direction.Y, light.Direction.Z);
        await app.Program.SetUniform3fAsync("uLightColor", light.Color.X, light.Color.Y, light.Color.Z);
        await app.Program.SetUniform1fAsync("uLightIntensity", light.Intensity);

        await app.StartAsync(async dt =>
        {
            angle += dt * 1.2f;
            var model = Mat4.Multiply(Mat4.RotateY(angle), Mat4.RotateX(angle * 0.7f));
            await app.Program.SetUniformMatrix4fvAsync("uModel", model);
            // Compute and upload normal matrix (3x3 inverse-transpose of model's upper-left 3x3)
            var normalMat3 = Mat4.NormalMatrix(model);
            await app.Program.SetUniformMatrix3fvAsync("uNormalMatrix", normalMat3);
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
