using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering;
using Velvet.Blazor;
using Velvet.WebGL;
using Velvet.Demo.Blazor.Debug;

namespace Velvet.Demo.Blazor.Pages;

public partial class CubeDemo : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference canvasRef;

    private VelvetApp? app;
    private Mesh? cube;
    private Camera? camera;
    private DirectionalLightState? directional;
    private PointLightState? point;

    private DotNetObjectReference<VelvetDebugInterop>? debugRef;
    private VelvetDebugInterop? debugInterop;

    private float angle;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        app = await VelvetApp.CreateAsync(canvasRef, JS, ShaderProgram.CreateDefaultAsync);

        camera = new Camera(
            position: new Vec3(0, 0, 3.0f),
            target: new Vec3(0, 0, 0),
            up: Vec3.UnitY,
            fovYRadians: 60.0f * (System.MathF.PI / 180.0f),
            aspectRatio: 800.0f / 600.0f,
            nearPlane: 0.1f,
            farPlane: 100.0f);

        cube = new Mesh(new CubeGeometry());
        app.Add(cube);

        directional = new DirectionalLightState(
            enabled: true,
            direction: new Vec3(0.5f, -1.0f, -0.3f),
            color: new Vec3(1, 1, 1),
            intensity: 1.25f);

        point = new PointLightState(
            enabled: true,
            position: new Vec3(1.5f, 1.2f, 1.5f),
            color: new Vec3(1.0f, 0.9f, 0.8f),
            intensity: 2.0f,
            constant: 1.0f,
            linear: 0.14f,
            quadratic: 0.07f);

        debugInterop = new VelvetDebugInterop(
            getCamera: () => camera,
            getDirectional: () => directional,
            getPoint: () => point,
            setCameraPosition: v => camera.Position = v,
            setCameraTarget: v => camera.Target = v,
            setCameraPerspective: (fovYRadians, nearPlane, farPlane) => camera.SetPerspective(fovYRadians, camera.AspectRatio, nearPlane, farPlane),
            setDirectionalEnabled: enabled => directional.Enabled = enabled,
            setDirectionalDirection: v => directional.Direction = v,
            setDirectionalColor: v => directional.Color = v,
            setDirectionalIntensity: intensity => directional.Intensity = intensity,
            setPointEnabled: enabled => point.Enabled = enabled,
            setPointPosition: v => point.Position = v,
            setPointColor: v => point.Color = v,
            setPointIntensity: intensity => point.Intensity = intensity,
            setPointAttenuation: (constant, linear, quadratic) =>
            {
                // Match PointLight ctor constraints (constant > 0, linear/quadratic >= 0)
                point.Constant = constant <= 0f ? 0.0001f : constant;
                point.Linear = linear < 0f ? 0f : linear;
                point.Quadratic = quadratic < 0f ? 0f : quadratic;
            },
            pause: async () =>
            {
                if (app is not null) await app.StopAsync();
            },
            resume: async () =>
            {
                if (app is not null)
                {
                    await app.StartAsync(OnFrameAsync);
                }
            });

        debugRef = DotNetObjectReference.Create(debugInterop);

        await app.StartAsync(OnFrameAsync);

        await TryInitDebugUiAsync();
    }

    private async Task OnFrameAsync(float dt)
    {
        if (app is null || camera is null || directional is null || point is null) return;

        angle += dt * 1.2f;
        var model = Mat4.Multiply(Mat4.RotateY(angle), Mat4.RotateX(angle * 0.7f));
        await app.Program.SetUniformMatrix4fvAsync("uModel", model);

        var normalMat3 = Mat4.NormalMatrix(model);
        await app.Program.SetUniformMatrix3fvAsync("uNormalMatrix", normalMat3);

        await app.Program.SetUniformMatrix4fvAsync("uView", camera.ViewMatrix);
        await app.Program.SetUniformMatrix4fvAsync("uProjection", camera.ProjectionMatrix);

        // Directional light
        var dir = directional.Direction;
        if (dir.LengthSquared > 0f)
        {
            dir = dir.Normalized();
        }
        await app.Program.SetUniform3fAsync("uLightDirection", dir.X, dir.Y, dir.Z);
        await app.Program.SetUniform3fAsync("uLightColor", directional.Color.X, directional.Color.Y, directional.Color.Z);
        await app.Program.SetUniform1fAsync("uLightIntensity", directional.Enabled ? directional.Intensity : 0f);

        // Point light
        await app.Program.SetUniform3fAsync("uPointLightPosition", point.Position.X, point.Position.Y, point.Position.Z);
        await app.Program.SetUniform3fAsync("uPointLightColor", point.Color.X, point.Color.Y, point.Color.Z);
        await app.Program.SetUniform1fAsync("uPointLightIntensity", point.Enabled ? point.Intensity : 0f);
        await app.Program.SetUniform1fAsync("uPointLightConstant", point.Constant);
        await app.Program.SetUniform1fAsync("uPointLightLinear", point.Linear);
        await app.Program.SetUniform1fAsync("uPointLightQuadratic", point.Quadratic);
    }

    private async Task TryInitDebugUiAsync()
    {
        if (debugRef is null) return;

        // Optional: if Tweakpane or velvet-debug-ui.js isn't loaded, this should not break the demo.
        try
        {
            await JS.InvokeVoidAsync("VelvetDebugUI.init", new
            {
                title = "Velvet Debug",
                pollMs = 500,
                camera = new
                {
                    dotnet = debugRef,
                    getState = nameof(VelvetDebugInterop.GetState),
                    setPosition = nameof(VelvetDebugInterop.SetCameraPosition),
                    setTarget = nameof(VelvetDebugInterop.SetCameraTarget),
                    setPerspective = nameof(VelvetDebugInterop.SetCameraPerspective),
                },
                directionalLight = new
                {
                    dotnet = debugRef,
                    setEnabled = nameof(VelvetDebugInterop.SetDirectionalEnabled),
                    setDirection = nameof(VelvetDebugInterop.SetDirectionalDirection),
                    setColor = nameof(VelvetDebugInterop.SetDirectionalColor),
                    setIntensity = nameof(VelvetDebugInterop.SetDirectionalIntensity),
                },
                pointLight = new
                {
                    dotnet = debugRef,
                    setEnabled = nameof(VelvetDebugInterop.SetPointEnabled),
                    setPosition = nameof(VelvetDebugInterop.SetPointPosition),
                    setColor = nameof(VelvetDebugInterop.SetPointColor),
                    setIntensity = nameof(VelvetDebugInterop.SetPointIntensity),
                    setAttenuation = nameof(VelvetDebugInterop.SetPointAttenuation),
                },
                renderer = new
                {
                    dotnet = debugRef,
                    pause = nameof(VelvetDebugInterop.PauseAsync),
                    resume = nameof(VelvetDebugInterop.ResumeAsync),
                },
            });
        }
        catch (JSException ex)
        {
            // Intentionally swallow: debug UI is developer-only and optional.
            // But log a hint so missing host scripts are obvious.
            System.Console.WriteLine($"[VelvetDebugUI] Init skipped: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        debugRef?.Dispose();
        if (app is not null)
        {
            await app.StopAsync();
        }
    }
}
