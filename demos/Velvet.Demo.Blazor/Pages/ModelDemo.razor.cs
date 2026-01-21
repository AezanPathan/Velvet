using System.Net.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Velvet.Blazor;
using Velvet.Core.Assets.Gltf;
using Velvet.Core.Math;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Lighting;
using Velvet.Demo.Blazor.Debug;
using Velvet.WebGL;
using BlazorApp = Velvet.Blazor.VelvetApp;
using EngineScene = Velvet.Core.Engine.Scene;

namespace Velvet.Demo.Blazor.Pages;

public partial class ModelDemo : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;

    private ElementReference canvasRef;

    private BlazorApp? app;
    private EngineScene? scene;
    private Camera? camera;
    private OrbitController? orbitController;
    private DirectionalLightState? directional;
    private PointLightState? point;
    private SpotLight? spot;

    // Mouse input tracking
    private bool isMouseDown = false;
    private int lastMouseX = 0;
    private int lastMouseY = 0;

    private DotNetObjectReference<VelvetDebugInterop>? debugRef;
    private VelvetDebugInterop? debugInterop;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        app = await BlazorApp.CreateAsync(canvasRef, JS, ShaderProgram.CreateDefaultAsync);

        camera = new Camera(
            position: new Vector3(0, 20f, 2.6f),
            target: new Vector3(0, 0, 0),
            up: Vector3.UnitY,
            fovYRadians: 60.0f * (System.MathF.PI / 180.0f),
            aspectRatio: 800.0f / 600.0f,
            nearPlane: 0.1f,
            farPlane: 100.0f);

        directional = new DirectionalLightState(
            enabled: true,
            direction: new Vector3(0.4f, -1.0f, -0.25f),
            color: new Vector3(1, 1, 1),
            intensity: 1.1f);

        point = new PointLightState(
            enabled: true,
            position: new Vector3(1.5f, 1.1f, 1.6f),
            color: new Vector3(1.0f, 0.95f, 0.9f),
            intensity: 2.0f,
            constant: 1.0f,
            linear: 0.14f,
            quadratic: 0.07f);

        // Keep spotlight uniforms valid; disable by setting intensity to 0.
        spot = new SpotLight(
            position: new Vector3(0.0f, 2.2f, 2.2f),
            direction: new Vector3(0.0f, -1.0f, -1.0f),
            color: new Vector3(1.0f, 1.0f, 1.0f),
            intensity: 0.0f,
            cutoff: 12.0f * (System.MathF.PI / 180.0f),
            outerCutoff: 20.0f * (System.MathF.PI / 180.0f),
            constant: 1.0f,
            linear: 0.09f,
            quadratic: 0.032f);

        // Load a single demo model from wwwroot.
        var bytes = await Http.GetByteArrayAsync("models/DragonAttenuation.glb");
        scene = await GltfLoader.LoadScene(bytes);
        app.Add(scene);

        // Auto-frame the camera to fit the loaded model.
        var bounds = scene.ComputeBounds();
        camera.Frame(bounds, frameMultiplier: 1.3f);

        // Assign camera to the application
        app.Camera = camera;

        // Initialize orbit controller around the model's center.
        orbitController = new OrbitController(
            target: bounds.Center,
            yaw: 0f,
            pitch: 0.3f,  // Slight upward angle
            distance: (bounds.Center - camera.Position).Length,
            minDistance: bounds.Radius * 0.5f,
            maxDistance: bounds.Radius * 10f);


        debugInterop = new VelvetDebugInterop(
            getCamera: () => camera,
            getDirectional: () => directional,
            getPoint: () => point,
            getMaterial: () =>
            {
                if (scene is null) return Material.Default;
                foreach (var inst in scene.MeshInstances)
                {
                    return inst.Mesh.Material ?? Material.Default;
                }
                return Material.Default;
            },
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
                point.Constant = constant <= 0f ? 0.0001f : constant;
                point.Linear = linear < 0f ? 0f : linear;
                point.Quadratic = quadratic < 0f ? 0f : quadratic;
            },
            // Material controls intentionally omitted from this demo; keep interop no-op.
            setMaterialColor: _ => { },
            setMaterialAmbient: _ => { },
            setMaterialDiffuse: _ => { },
            setMaterialUnlit: _ => { },
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

    private void OnCanvasMouseDown(MouseEventArgs e)
    {
        isMouseDown = true;
        lastMouseX = (int)e.ClientX;
        lastMouseY = (int)e.ClientY;
    }

    private void OnCanvasMouseMove(MouseEventArgs e)
    {
        if (!isMouseDown || orbitController is null) return;

        var deltaX = (int)e.ClientX - lastMouseX;
        var deltaY = (int)e.ClientY - lastMouseY;

        // Convert pixel movement to radians.
        // Sensitivity: ~0.005 radians per pixel (~0.3° per pixel)
        var yawDelta = deltaX * 0.005f;
        var pitchDelta = deltaY * 0.005f;

        orbitController.ApplyYaw(yawDelta);
        orbitController.ApplyPitch(-pitchDelta);  // Invert Y for intuitive up/down

        lastMouseX = (int)e.ClientX;
        lastMouseY = (int)e.ClientY;
    }

    private void OnCanvasMouseUp(MouseEventArgs e)
    {
        isMouseDown = false;
    }

    private void OnCanvasMouseLeave(MouseEventArgs e)
    {
        isMouseDown = false;
    }

    private void OnCanvasWheel(WheelEventArgs e)
    {
        if (orbitController is null) return;

        // Wheel delta is typically ±120 per notch.
        // Convert to zoom multiplier: 0.9 zooms in, 1.1 zooms out.
        var zoomMultiplier = 1.0f - (float)e.DeltaY * 0.001f;
        orbitController.ApplyZoomMultiplier(zoomMultiplier);
    }

    private async Task OnFrameAsync(float dt)
    {
        if (app is null || camera is null || directional is null || point is null || spot is null) return;

        // Update camera from orbit controller if available.
        if (orbitController is not null)
        {
            orbitController.UpdateCamera(camera);
        }

        // Single static model at origin.
        var modelMat = Matrix.Identity();
        await app.Program.SetUniformMatrix4fvAsync("uModel", modelMat);
        await app.Program.SetUniformMatrix3fvAsync("uNormalMatrix", Matrix.NormalMatrix(modelMat));

        await app.Program.SetUniformMatrix4fvAsync("uView", camera.ViewMatrix);
        await app.Program.SetUniformMatrix4fvAsync("uProjection", camera.ProjectionMatrix);

        // Directional light
        var dir = directional.Direction;
        if (dir.LengthSquared > 0f) dir = dir.Normalized();
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

        // Spot light (disabled, but keep uniforms valid)
        await app.Program.SetUniform3fAsync("uSpotLightPosition", spot.Position.X, spot.Position.Y, spot.Position.Z);
        await app.Program.SetUniform3fAsync("uSpotLightDirection", spot.Direction.X, spot.Direction.Y, spot.Direction.Z);
        await app.Program.SetUniform3fAsync("uSpotLightColor", spot.Color.X, spot.Color.Y, spot.Color.Z);
        await app.Program.SetUniform1fAsync("uSpotLightIntensity", spot.Intensity);
        await app.Program.SetUniform1fAsync("uSpotLightCutoff", spot.Cutoff);
        await app.Program.SetUniform1fAsync("uSpotLightOuterCutoff", spot.OuterCutoff);
        await app.Program.SetUniform1fAsync("uSpotLightConstant", spot.Constant);
        await app.Program.SetUniform1fAsync("uSpotLightLinear", spot.Linear);
        await app.Program.SetUniform1fAsync("uSpotLightQuadratic", spot.Quadratic);
    }

    private async Task TryInitDebugUiAsync()
    {
        if (debugRef is null) return;

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
