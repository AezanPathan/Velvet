using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering;
using Velvet.Blazor;
using Velvet.WebGL;
using Velvet.Demo.Blazor.Debug;
using Velvet.Core.Rendering.Lighting;

namespace Velvet.Demo.Blazor.Pages;

public partial class CubeDemo : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference canvasRef;

    private VelvetApp? app;
    private Mesh? cube;
    private Mesh? sphere;
    private Material? sphereMaterial;
    private Camera? camera;
    private DirectionalLightState? directional;
    private PointLightState? point;
    private SpotLight? spot;

    private DotNetObjectReference<VelvetDebugInterop>? debugRef;
    private VelvetDebugInterop? debugInterop;

    private float angle;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        app = await VelvetApp.CreateAsync(canvasRef, JS, ShaderProgram.CreateDefaultAsync);

        camera = new Camera(
            position: new Vector3(0, 0, 3.0f),
            target: new Vector3(0, 0, 0),
            up: Vector3.UnitY,
            fovYRadians: 60.0f * (System.MathF.PI / 180.0f),
            aspectRatio: 800.0f / 600.0f,
            nearPlane: 0.1f,
            farPlane: 100.0f);

        // Rotating cube uses the engine default material (intent: geometry without explicit appearance).
        cube = new Mesh(new CubeGeometry());

        // Sphere is the material showcase target.
        sphereMaterial = new Material(
            albedoColor: new Vector3(0.20f, 0.65f, 1.0f),
            ambientStrength: 0.08f,
            diffuseStrength: 1.0f,
            unlit: false);

        sphere = new Mesh(new SphereGeometry(latitudeSegments: 14, longitudeSegments: 20, radius: 0.55f))
        {
            Material = sphereMaterial,
        };

        app.Add(cube);
        app.Add(sphere);

        directional = new DirectionalLightState(
            enabled: true,
            direction: new Vector3(0.5f, -1.0f, -0.3f),
            color: new Vector3(1, 1, 1),
            intensity: 1.25f);

        point = new PointLightState(
            enabled: true,
            position: new Vector3(1.5f, 1.2f, 1.5f),
            color: new Vector3(1.0f, 0.9f, 0.8f),
            intensity: 2.0f,
            constant: 1.0f,
            linear: 0.14f,
            quadratic: 0.07f);

        // Spotlight (animated direction).
        // Keep it explicit and single-instance: no managers, no arrays.
        spot = new SpotLight(
            position: new Vector3(0.0f, 2.2f, 2.2f),
            direction: new Vector3(0.0f, -1.0f, -1.0f),
            color: new Vector3(1.0f, 1.0f, 1.0f),
            intensity: 6.0f,
            cutoff: 12.0f * (System.MathF.PI / 180.0f),
            outerCutoff: 20.0f * (System.MathF.PI / 180.0f),
            constant: 1.0f,
            linear: 0.09f,
            quadratic: 0.032f);

        debugInterop = new VelvetDebugInterop(
            getCamera: () => camera,
            getDirectional: () => directional,
            getPoint: () => point,
            getMaterial: () => sphereMaterial ?? Material.Default,
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
            setMaterialColor: v =>
            {
                if (sphereMaterial is not null) sphereMaterial.AlbedoColor = v;
            },
            setMaterialAmbient: a =>
            {
                if (sphereMaterial is not null) sphereMaterial.AmbientStrength = a < 0f ? 0f : a;
            },
            setMaterialDiffuse: d =>
            {
                if (sphereMaterial is not null) sphereMaterial.DiffuseStrength = d < 0f ? 0f : d;
            },
            setMaterialUnlit: unlit =>
            {
                if (sphereMaterial is not null) sphereMaterial.Unlit = unlit;
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

        await app.StartAsync(OnFrameAsync, BeforeDrawMeshAsync);

        await TryInitDebugUiAsync();
    }

    private async Task OnFrameAsync(float dt)
    {
        if (app is null || camera is null || directional is null || point is null || spot is null) return;

        angle += dt * 1.2f;

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

        // Spot light
        // Sweep the cone across the cube by orbiting the target point around the origin.
        var sweep = angle * 0.9f;
        var target = new Vector3(System.MathF.Sin(sweep) * 0.9f, 0.0f, System.MathF.Cos(sweep) * 0.9f);
        var spotDir = (target - spot.Position).Normalized();
        spot.Direction = spotDir;

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

    private async Task BeforeDrawMeshAsync(Mesh mesh)
    {
        if (app is null || cube is null || sphere is null) return;

        // Minimal per-mesh model setup (no scene graph): cube rotates, sphere stays static.
        float[] model;
        if (ReferenceEquals(mesh, cube))
        {
            var rotation = Matrix.Multiply(Matrix.RotateY(angle), Matrix.RotateX(angle * 0.7f));
            model = Matrix.Multiply(Translate(-1.05f, 0.0f, 0.0f), rotation);
        }
        else if (ReferenceEquals(mesh, sphere))
        {
            model = Translate(1.05f, -0.05f, 0.0f);
        }
        else
        {
            model = Matrix.Identity();
        }

        await app.Program.SetUniformMatrix4fvAsync("uModel", model);

        var normalMat3 = Matrix.NormalMatrix(model);
        await app.Program.SetUniformMatrix3fvAsync("uNormalMatrix", normalMat3);
    }

    private static float[] Translate(float x, float y, float z)
        =>
        [
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            x, y, z, 1
        ];

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
                material = new
                {
                    dotnet = debugRef,
                    setColor = nameof(VelvetDebugInterop.SetMaterialColor),
                    setAmbient = nameof(VelvetDebugInterop.SetMaterialAmbient),
                    setDiffuse = nameof(VelvetDebugInterop.SetMaterialDiffuse),
                    setUnlit = nameof(VelvetDebugInterop.SetMaterialUnlit),
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
