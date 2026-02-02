using System.Net.Http;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Velvet.Blazor;
using Velvet.Core.Animation;
using Velvet.Core.Assets.Gltf;
using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Lighting;
using Velvet.Core.Engine;
using Velvet.Demo.Blazor.Debug;
using Velvet.WebGL;
using BlazorApp = Velvet.Blazor.VelvetApp;
using EngineScene = Velvet.Core.Engine.Scene;
using EngineNode = Velvet.Core.Engine.SceneNode;

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
    private DirectionalLight? directional;
    private PointLight? point;
    private SpotLight? spot;
    private Mesh? cube;
    private Animator? animator;
    private List<AnimationClip>? animationClips;

    // Mock states for debug UI compatibility
    private DirectionalLightState? directionalState;
    private PointLightState? pointState;

    // Mouse input tracking
    private bool isMouseDown = false;
    private int lastMouseX = 0;
    private int lastMouseY = 0;

    private DotNetObjectReference<VelvetDebugInterop>? debugRef;
    private VelvetDebugInterop? debugInterop;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        // Use skinned shader by default so we can handle both skinned and non-skinned meshes
        app = await BlazorApp.CreateAsync(canvasRef, JS, ShaderProgram.CreateSkinnedAsync);

        camera = new Camera(
            position: new Vector3(0, 20f, 2.6f),
            target: new Vector3(0, 0, 0),
            up: Vector3.UnitY,
            fovYRadians: 60.0f * (System.MathF.PI / 180.0f),
            aspectRatio: 800.0f / 600.0f,
            nearPlane: 0.1f,
            farPlane: 100.0f);

        directional = new DirectionalLight(
            direction: new Vector3(0.4f, -1.0f, -0.25f),
            color: new Vector3(1, 1, 1),
            intensity: 1.1f);

        point = new PointLight(
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

        // Create state objects for debug UI
        directionalState = new DirectionalLightState(
            enabled: true,
            direction: directional.Direction,
            color: directional.Color,
            intensity: directional.Intensity);

        pointState = new PointLightState(
            enabled: true,
            position: point.Position,
            color: point.Color,
            intensity: point.Intensity,
            constant: point.Constant,
            linear: point.Linear,
            quadratic: point.Quadratic);

        // Load a single demo model from wwwroot.
       // var bytes = await Http.GetByteArrayAsync("models/DragonAttenuation.glb");
        var bytes = await Http.GetByteArrayAsync("models/Fox.glb");
        var loadResult = await GltfLoader.LoadSceneWithAnimations(bytes, "models");
        scene = loadResult.Scene;
        animationClips = loadResult.Animations;

        animator = new Animator(scene);
        if (animationClips.Count > 0)
        {
            animator.PlayClip(animationClips[0]);
        }
        //scene = await GltfLoader.LoadFromUrlAsync("models/DragonAttenuation.glb");

        // Detect if any meshes are skinned
        var skinnedMeshCount = 0;
        var totalBones = 0;
        foreach (var instance in scene.MeshInstances)
        {
            if (instance.Skin != null)
            {
                skinnedMeshCount++;
                totalBones += instance.Skin.JointCount;
                System.Diagnostics.Debug.WriteLine($"[Skinning] Detected skinned mesh with {instance.Skin.JointCount} bones");
            }
        }
        
        if (skinnedMeshCount > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Skinning] Found {skinnedMeshCount} skinned meshes with {totalBones} total bones");
        }
        
        // Create a cube geometry and add it to the scene
        cube = new Mesh(new CubeGeometry());
        var cubeNode = new EngineNode(Matrix.Identity(), new[] { cube }, Array.Empty<EngineNode>());
        var cubeScene = new EngineScene(new[] { cubeNode });
        app.Add(scene);
        app.Add(cubeScene);

        // Auto-frame the camera to fit the loaded model.
        var bounds = scene.ComputeBounds();
        camera.Frame(bounds, frameMultiplier: 1.3f);

        // Assign camera to the application
        app.Camera = camera;

        // Assign lights to the application
        app.DirectionalLight = directional;
        app.PointLight = point;
        app.SpotLight = spot;
        app.SetDirectionalEnabled(true);
        app.SetPointEnabled(true);

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
            getDirectional: () => directionalState,
            getPoint: () => pointState,
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
            setDirectionalEnabled: enabled =>
            {
                directionalState!.Enabled = enabled;
                app?.SetDirectionalEnabled(enabled);
            },
            setDirectionalDirection: v =>
            {
                directionalState!.Direction = v;
                if (directional is not null) directional.Direction = v;
            },
            setDirectionalColor: v =>
            {
                directionalState!.Color = v;
                if (directional is not null) directional.Color = v;
            },
            setDirectionalIntensity: intensity =>
            {
                directionalState!.Intensity = intensity;
                if (directional is not null) directional.Intensity = intensity;
            },
            setPointEnabled: enabled =>
            {
                pointState!.Enabled = enabled;
                app?.SetPointEnabled(enabled);
            },
            setPointPosition: v =>
            {
                pointState!.Position = v;
                if (point is not null) point.Position = v;
            },
            setPointColor: v =>
            {
                pointState!.Color = v;
                if (point is not null) point.Color = v;
            },
            setPointIntensity: intensity =>
            {
                pointState!.Intensity = intensity;
                if (point is not null) point.Intensity = intensity;
            },
            setPointAttenuation: (constant, linear, quadratic) =>
            {
                constant = constant <= 0f ? 0.0001f : constant;
                linear = linear < 0f ? 0f : linear;
                quadratic = quadratic < 0f ? 0f : quadratic;
                
                pointState!.Constant = constant;
                pointState.Linear = linear;
                pointState.Quadratic = quadratic;
                
                if (point is not null)
                {
                    point.Constant = constant;
                    point.Linear = linear;
                    point.Quadratic = quadratic;
                }
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
        var yawDelta = -deltaX * 0.005f;
        var pitchDelta = deltaY * 0.005f;

        orbitController.ApplyYaw(yawDelta);
        orbitController.ApplyPitch(pitchDelta);

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
        // Scroll up (negative DeltaY) zooms in, scroll down (positive DeltaY) zooms out.
        var zoomMultiplier = 1.0f + (float)e.DeltaY * 0.001f;
        orbitController.ApplyZoomMultiplier(zoomMultiplier);
    }

    private async Task OnFrameAsync(float dt)
    {
        if (app is null || camera is null || orbitController is null) return;

        // Update camera from orbit controller if available.
        orbitController.UpdateCamera(camera);

        if (scene is not null && animator is not null)
        {
            // Explicit animation update (no implicit time in renderer)
            animator.Update(dt);

            // Render uses current animated transforms and pure skinning
            app.Render(scene);
        }

        await Task.CompletedTask;
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
