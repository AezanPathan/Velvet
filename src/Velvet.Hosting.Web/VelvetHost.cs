using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Particles;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Batching;
using Velvet.Core.Rendering.Cameras;
using Velvet.Core.Rendering.Controllers;
using Velvet.Core.Rendering.Core;
using Velvet.Core.Rendering.Culling;
using Velvet.Core.Rendering.Environment;
using Velvet.Core.Rendering.Input;
using Velvet.Core.Rendering.Lighting;
using Velvet.Core.Rendering.Meshes;
using Velvet.Core.Rendering.Skinning;
using Velvet.Core.Scene;
using Velvet.Graphics.WebGL;

namespace Velvet.Hosting.Web;

/// <summary>
/// Blazor-first engine entry point for Velvet.
/// Owns the WebGL renderer, shader program, uploaded meshes, and the update/render loop.
/// </summary>
public sealed class VelvetHost
{
    private readonly IJSRuntime _js;
    private readonly IWebGLBridge _bridge;
    private readonly IMeshUploader _meshUploader;
    private readonly int _rendererId;
    private readonly ResizeController _resizeController = new();

    private readonly List<MeshInstance> _instances = new();
    private readonly List<(Scene Scene, int Start, int Count)> _sceneInstanceRanges = new();
    private List<RenderBatch>? _batches;
    private readonly Dictionary<Skin, float[]> _boneMatrixCache = new();
    private readonly Frustum _frustum = new();
    private int _lastFrameTotalMeshes;
    private int _lastFrameCulledMeshes;
    private int _lastFrameRenderedMeshes;
    
    // Particle system support
    private readonly List<Velvet.Core.Particles.ParticleSystem> _particleSystems = new();
    private readonly List<ParticleRenderer> _particleRenderers = new();

    private ShaderProgram? _program;
    private ShaderProgram? _skyboxProgram;
    private Camera? _camera;
    private DirectionalLight? _directionalLight;
    private PointLight? _pointLight;
    private SpotLight? _spotLight;
    private OrbitInputBinder? _orbitBinder;
    private Skybox? _skybox;

    private DotNetObjectReference<VelvetHost>? _resizeCallbackRef;
    private string? _resizeBindingId;
    private string? _orbitInputBindingId;

    // For demo: ability to enable/disable lights
    private bool _directionalEnabled = true;
    private bool _pointEnabled = true;

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    private VelvetHost(IJSRuntime js, IWebGLBridge bridge, int rendererId)
    {
        _js = js;
        _bridge = bridge;
        _meshUploader = new WebGLMeshUploader(bridge);
        _rendererId = rendererId;
    }

    /// <summary>
    /// Creates and initializes a Velvet application bound to a Blazor canvas.
    /// </summary>
    public static async Task<VelvetHost> CreateAsync(
        ElementReference canvas,
        IJSRuntime js,
        Func<IWebGLBridge, Task<ShaderProgram>>? programFactory = null)
    {
        ArgumentNullException.ThrowIfNull(js);

        var bridge = new BlazorWebGLBridge(js);
        var rendererId = await bridge.InitWithElementAsync(canvas).ConfigureAwait(false);

        var app = new VelvetHost(js, bridge, rendererId);
        app._resizeCallbackRef = DotNetObjectReference.Create(app);
        app._resizeBindingId = await js.InvokeAsync<string>(
            "CanvasHelpers.bindResizeTracking",
            canvas,
            app._resizeCallbackRef,
            nameof(OnResizeFromJs)).AsTask().ConfigureAwait(false);
        app._orbitInputBindingId = await js.InvokeAsync<string>(
            "CanvasHelpers.bindOrbitInput",
            canvas,
            app._resizeCallbackRef,
            nameof(OnOrbitMouseDownFromJs),
            nameof(OnOrbitMouseMoveFromJs),
            nameof(OnOrbitMouseUpFromJs),
            nameof(OnOrbitWheelFromJs)).AsTask().ConfigureAwait(false);

        if (programFactory is not null)
        {
            app._program = await programFactory(bridge).ConfigureAwait(false);
        }

        return app;
    }

    public ShaderProgram Program
        => _program ?? throw new InvalidOperationException("Shader program not configured. Provide a programFactory to CreateAsync(...). ");

    public bool EnableFrustumCulling { get; set; } = true;

    public int LastFrameTotalMeshes => _lastFrameTotalMeshes;

    public int LastFrameCulledMeshes => _lastFrameCulledMeshes;

    public int LastFrameRenderedMeshes => _lastFrameRenderedMeshes;

    /// <summary>
    /// Camera used for View and Projection matrices in the render loop.
    /// </summary>
    public Camera? Camera
    {
        get => _camera;
        set
        {
            ThrowIfRunning();
            _camera = value;
            _resizeController.ApplyViewportToCamera(_camera);
        }
    }

    /// <summary>
    /// Directional light for the scene.
    /// </summary>
    public DirectionalLight? DirectionalLight
    {
        get => _directionalLight;
        set
        {
            ThrowIfRunning();
            _directionalLight = value;
        }
    }

    /// <summary>
    /// Point light for the scene.
    /// </summary>
    public PointLight? PointLight
    {
        get => _pointLight;
        set
        {
            ThrowIfRunning();
            _pointLight = value;
        }
    }

    /// <summary>
    /// Spot light for the scene.
    /// </summary>
    public SpotLight? SpotLight
    {
        get => _spotLight;
        set
        {
            ThrowIfRunning();
            _spotLight = value;
        }
    }

    public void SetController(OrbitController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ThrowIfRunning();

        if (_camera is null)
        {
            throw new InvalidOperationException("Camera must be set before controller.");
        }

        _orbitBinder = new OrbitInputBinder(controller, _camera);
    }

    /// <summary>
    /// Sets the skybox for the scene.
    /// The skybox will be rendered as an infinitely distant background.
    /// </summary>
    public async Task SetSkybox(Skybox skybox)
    {
        ArgumentNullException.ThrowIfNull(skybox);
        ThrowIfRunning();

        _skybox = skybox;

        // Create skybox shader program if not already created
        if (_skyboxProgram is null)
        {
            _skyboxProgram = await ShaderProgram.CreateSkyboxAsync(_bridge).ConfigureAwait(false);
        }

        // Upload skybox mesh
        await skybox.Mesh.UploadAsync(_meshUploader).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates and sets a cubemap skybox from 6 face images.
    /// Face order: +X, -X, +Y, -Y, +Z, -Z
    /// </summary>
    public async Task SetCubemapSkybox(string px, string nx, string py, string ny, string pz, string nz)
    {
        ThrowIfRunning();
        
        // Create cubemap texture
        var faceUrls = new[] { px, nx, py, ny, pz, nz };
        var textureId = await _bridge.CreateCubemapTextureAsync(faceUrls).ConfigureAwait(false);
        
        // Create skybox with cubemap
        var geometry = new SkyboxGeometry();
        var mesh = new Mesh(geometry);
        var skybox = new Skybox(mesh, textureId);
        
        await SetSkybox(skybox).ConfigureAwait(false);
    }

    /// <summary>
    /// Queues a resize request to be applied at the start of the next render frame.
    /// </summary>
    public void RequestResize(int width, int height, float dpr)
    {
        _resizeController.RequestResize(width, height, dpr);
    }

    /// <summary>
    /// Registers a scene with the application. Upload occurs on <see cref="StartAsync"/>.
    /// </summary>
    public void Add(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ThrowIfRunning();

        var sceneInstances = new List<MeshInstance>();
        scene.CollectMeshes(sceneInstances);

        var start = _instances.Count;
        foreach (var instance in sceneInstances)
        {
            _instances.Add(instance);
        }

        _sceneInstanceRanges.Add((scene, start, sceneInstances.Count));
    }

    /// <summary>
    /// Registers a particle system with the application. 
    /// Particle renderer initialization occurs on <see cref="StartAsync"/>.
    /// </summary>
    public void Add(ParticleSystem particleSystem)
    {
        ArgumentNullException.ThrowIfNull(particleSystem);
        ThrowIfRunning();

        _particleSystems.Add(particleSystem);
    }

    /// <summary>
    /// Prepares a scene for rendering using its current node transforms.
    /// This updates mesh instance matrices and computes bone matrices for skinned meshes.
    /// Animation time is not advanced here; call Animator.Update(dt) explicitly before Render(...).
    /// </summary>
    public void Render(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        var currentInstances = new List<MeshInstance>();
        scene.CollectMeshes(currentInstances);

        foreach (var range in _sceneInstanceRanges)
        {
            if (!ReferenceEquals(range.Scene, scene))
            {
                continue;
            }

            if (currentInstances.Count != range.Count)
            {
                throw new InvalidOperationException("Mesh instance count mismatch while updating transforms.");
            }

            for (var i = 0; i < range.Count; i++)
            {
                var source = currentInstances[i];
                _instances[range.Start + i] = source;
            }
        }

        foreach (var instance in currentInstances)
        {
            var skin = instance.Skin;
            if (skin is null)
            {
                continue;
            }

            if (!_boneMatrixCache.ContainsKey(skin))
            {
                var boneMatrices = BoneMatrixCalculator.ComputeBoneMatrices(skin, scene.Roots);
                _boneMatrixCache[skin] = boneMatrices;
            }
        }
    }

    public Task StartAsync(Func<float, Task>? onFrame = null)
    {
        return StartAsyncCore(new FrameCallbacks(onFrame, BeforeDrawMesh: null));
    }

    public Task StartAsync(Func<float, Task>? onFrame, Func<Mesh, Task>? beforeDrawMesh)
    {
        return StartAsyncCore(new FrameCallbacks(onFrame, beforeDrawMesh));
    }

    public Task StartAsync(Action<float> onFrame)
    {
        ArgumentNullException.ThrowIfNull(onFrame);
        return StartAsync(dt =>
        {
            onFrame(dt);
            return Task.CompletedTask;
        });
    }

    public async Task StopAsync()
    {
        var cts = _loopCts;
        var task = _loopTask;

        if (cts is null || task is null)
        {
            await CleanupResizeInteropAsync().ConfigureAwait(false);
            return;
        }

        _loopCts = null;
        _loopTask = null;

        cts.Cancel();
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cts.Dispose();
            await CleanupResizeInteropAsync().ConfigureAwait(false);
        }
    }

    [JSInvokable]
    public Task OnResizeFromJs(int width, int height, double dpr)
    {
        RequestResize(width, height, (float)dpr);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnOrbitMouseDownFromJs(int x, int y)
    {
        _orbitBinder?.OnMouseDown(x, y);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnOrbitMouseMoveFromJs(int x, int y)
    {
        _orbitBinder?.OnMouseMove(x, y);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnOrbitMouseUpFromJs()
    {
        _orbitBinder?.OnMouseUp();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnOrbitWheelFromJs(double delta)
    {
        _orbitBinder?.OnWheel((float)delta);
        return Task.CompletedTask;
    }

    private Task StartAsyncCore(FrameCallbacks callbacks)
    {
        var program = _program ?? throw new InvalidOperationException("Shader program not configured. Provide a programFactory to CreateAsync(...).");
        if (_instances.Count == 0 && _particleSystems.Count == 0)
        {
            throw new InvalidOperationException("No meshes or particle systems added. Call Add(scene) or Add(particleSystem) before StartAsync().");
        }

        if (_loopTask is not null)
        {
            return Task.CompletedTask;
        }

        _batches = RenderBatcher.BuildBatches(_instances, program);
        _loopCts = new CancellationTokenSource();
        _loopTask = RunLoopAsync(callbacks, _loopCts.Token);
        return Task.CompletedTask;
    }

    private async Task RunLoopAsync(FrameCallbacks callbacks, CancellationToken cancellationToken)
    {
        var program = _program ?? throw new InvalidOperationException("Shader program not configured.");
        var camera = _camera ?? throw new InvalidOperationException("Camera not configured. Assign a Camera before starting.");
        var batches = _batches ?? throw new InvalidOperationException("Batches not built.");

        await UploadSceneMeshesAsync(cancellationToken).ConfigureAwait(false);
        await InitializeParticleRenderersAsync().ConfigureAwait(false);

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));

        var stopwatch = Stopwatch.StartNew();
        var lastSeconds = 0f;

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            await ApplyPendingResizeAsync(camera, cancellationToken).ConfigureAwait(false);

            var nowSeconds = (float)stopwatch.Elapsed.TotalSeconds;
            var deltaSeconds = nowSeconds - lastSeconds;
            lastSeconds = nowSeconds;

            await ExecuteFrameAsync(program, camera, batches, callbacks, deltaSeconds).ConfigureAwait(false);
        }
    }

    private async Task UploadSceneMeshesAsync(CancellationToken cancellationToken)
    {
        foreach (var instance in _instances)
        {
            await instance.Mesh.UploadAsync(_meshUploader, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task InitializeParticleRenderersAsync()
    {
        foreach (var particleSystem in _particleSystems)
        {
            var particleRenderer = new ParticleRenderer(particleSystem, _bridge);
            await particleRenderer.InitializeAsync().ConfigureAwait(false);
            _particleRenderers.Add(particleRenderer);
        }
    }

    private async Task ApplyPendingResizeAsync(Camera camera, CancellationToken cancellationToken)
    {
        if (_resizeController.HasPendingResize)
        {
            await _resizeController
                .ApplyResizeAsync((pixelWidth, pixelHeight) => _bridge.ResizeAsync(pixelWidth, pixelHeight), camera, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task ExecuteFrameAsync(
        ShaderProgram program,
        Camera camera,
        List<RenderBatch> batches,
        FrameCallbacks callbacks,
        float deltaSeconds)
    {
        _orbitBinder?.Update();
        _boneMatrixCache.Clear();

        foreach (var particleSystem in _particleSystems)
        {
            particleSystem.Update(deltaSeconds);
        }

        if (callbacks.OnFrame is not null)
        {
            await callbacks.OnFrame(deltaSeconds).ConfigureAwait(false);
        }

        await _bridge.ClearAsync(_rendererId, 0.08f, 0.08f, 0.10f, 1.0f).ConfigureAwait(false);
        await RenderSkyboxAsync(camera).ConfigureAwait(false);
        await SetFrameUniformsAsync(program, camera).ConfigureAwait(false);

        _lastFrameTotalMeshes = 0;
        _lastFrameCulledMeshes = 0;
        _lastFrameRenderedMeshes = 0;

        if (EnableFrustumCulling)
        {
            _frustum.UpdateFromMatrix(camera.ViewProjectionMatrix);
        }

        await RenderBatchesAsync(program, batches, callbacks.BeforeDrawMesh, EnableFrustumCulling).ConfigureAwait(false);
        await RenderParticlesAsync(camera).ConfigureAwait(false);
    }

    private async Task RenderSkyboxAsync(Camera camera)
    {
        if (_skybox is null || _skyboxProgram is null)
        {
            return;
        }

        await _bridge.SetDepthMaskAsync(_rendererId, false).ConfigureAwait(false);
        await _skyboxProgram.SetUniformMatrix4fvAsync("uView", camera.ViewMatrix).ConfigureAwait(false);
        await _skyboxProgram.SetUniformMatrix4fvAsync("uProjection", camera.ProjectionMatrix).ConfigureAwait(false);

        if (_skybox.CubemapTextureId.HasValue)
        {
            await _bridge.BindCubemapTextureAsync(
                _skyboxProgram.ProgramId,
                "u_Skybox",
                _skybox.CubemapTextureId.Value,
                0).ConfigureAwait(false);
            await _skyboxProgram.SetUniform1bAsync("u_HasCubemap", true).ConfigureAwait(false);
        }
        else
        {
            await _skyboxProgram.SetUniform1bAsync("u_HasCubemap", false).ConfigureAwait(false);
        }

        var skyboxMeshId = _skybox.Mesh.Resources.VertexBufferId.Value;
        await _skyboxProgram.DrawMeshAsync(skyboxMeshId, _rendererId).ConfigureAwait(false);
        await _bridge.SetDepthMaskAsync(_rendererId, true).ConfigureAwait(false);
    }

    private async Task SetFrameUniformsAsync(ShaderProgram program, Camera camera)
    {
        await program.SetUniformMatrix4fvAsync("uView", camera.ViewMatrix).ConfigureAwait(false);
        await program.SetUniformMatrix4fvAsync("uProjection", camera.ProjectionMatrix).ConfigureAwait(false);
        await SetFrameLightsAsync(program).ConfigureAwait(false);
    }

    private async Task SetFrameLightsAsync(ShaderProgram program)
    {
        if (_directionalLight is not null)
        {
            var dir = _directionalLight.Direction;
            var dirLenSq = dir.LengthSquared;
            var normalizedDir = dirLenSq > 0.000001f ? dir.Normalized() : new Vector3(0f, -1f, 0f);
            var directionalColor = _directionalEnabled
                ? _directionalLight.Color * _directionalLight.Intensity
                : Vector3.Zero;
            await program.SetUniform3fAsync("uLightDirection", normalizedDir.X, normalizedDir.Y, normalizedDir.Z).ConfigureAwait(false);
            await program.SetUniform3fAsync("uLightColor", directionalColor.X, directionalColor.Y, directionalColor.Z).ConfigureAwait(false);
        }

        if (_pointLight is not null)
        {
            var intensity = _pointEnabled ? _pointLight.Intensity : 0f;
            await program.SetUniform3fAsync("uPointLightPosition", _pointLight.Position.X, _pointLight.Position.Y, _pointLight.Position.Z).ConfigureAwait(false);
            await program.SetUniform3fAsync("uPointLightColor", _pointLight.Color.X, _pointLight.Color.Y, _pointLight.Color.Z).ConfigureAwait(false);
            await program.SetUniform1fAsync("uPointLightIntensity", intensity).ConfigureAwait(false);
            await program.SetUniform1fAsync("uPointLightConstant", _pointLight.Constant).ConfigureAwait(false);
            await program.SetUniform1fAsync("uPointLightLinear", _pointLight.Linear).ConfigureAwait(false);
            await program.SetUniform1fAsync("uPointLightQuadratic", _pointLight.Quadratic).ConfigureAwait(false);
        }

        if (_spotLight is not null)
        {
            await program.SetUniform3fAsync("uSpotLightPosition", _spotLight.Position.X, _spotLight.Position.Y, _spotLight.Position.Z).ConfigureAwait(false);
            await program.SetUniform3fAsync("uSpotLightDirection", _spotLight.Direction.X, _spotLight.Direction.Y, _spotLight.Direction.Z).ConfigureAwait(false);
            await program.SetUniform3fAsync("uSpotLightColor", _spotLight.Color.X, _spotLight.Color.Y, _spotLight.Color.Z).ConfigureAwait(false);
            await program.SetUniform1fAsync("uSpotLightIntensity", _spotLight.Intensity).ConfigureAwait(false);
            await program.SetUniform1fAsync("uSpotLightCutoff", _spotLight.Cutoff).ConfigureAwait(false);
            await program.SetUniform1fAsync("uSpotLightOuterCutoff", _spotLight.OuterCutoff).ConfigureAwait(false);
            await program.SetUniform1fAsync("uSpotLightConstant", _spotLight.Constant).ConfigureAwait(false);
            await program.SetUniform1fAsync("uSpotLightLinear", _spotLight.Linear).ConfigureAwait(false);
            await program.SetUniform1fAsync("uSpotLightQuadratic", _spotLight.Quadratic).ConfigureAwait(false);
        }
    }

    private async Task RenderBatchesAsync(ShaderProgram program, List<RenderBatch> batches, Func<Mesh, Task>? beforeDrawMesh, bool applyFrustumCulling)
    {
        foreach (var batch in batches)
        {
            await batch.Key.Material.ApplyAsync(program).ConfigureAwait(false);

            foreach (var instance in batch.Instances)
            {
                _lastFrameTotalMeshes++;

                if (applyFrustumCulling && !_frustum.Intersects(instance.BoundingBox))
                {
                    _lastFrameCulledMeshes++;
                    continue;
                }

                var mesh = instance.Mesh;
                var meshId = mesh.Resources.VertexBufferId.Value;
                mesh.Skin = instance.Skin;

                if (beforeDrawMesh is not null)
                {
                    await beforeDrawMesh(mesh).ConfigureAwait(false);
                }

                if (mesh.Skin is not null && _boneMatrixCache.TryGetValue(mesh.Skin, out var boneMatrices))
                {
                    await program.SetBoneMatricesAsync(boneMatrices, mesh.Skin.JointCount).ConfigureAwait(false);
                }

                await program.SetUniformMatrix4fvAsync("uModel", instance.ModelMatrix).ConfigureAwait(false);
                await program.SetUniformMatrix3fvAsync("uNormalMatrix", instance.NormalMatrix).ConfigureAwait(false);
                await program.DrawMeshAsync(meshId, _rendererId).ConfigureAwait(false);
                _lastFrameRenderedMeshes++;
            }
        }
    }

    private async Task RenderParticlesAsync(Camera camera)
    {
        foreach (var particleRenderer in _particleRenderers)
        {
            await particleRenderer.UploadAsync().ConfigureAwait(false);
            await particleRenderer.RenderAsync(_rendererId, camera).ConfigureAwait(false);
        }
    }

    private readonly record struct FrameCallbacks(
        Func<float, Task>? OnFrame,
        Func<Mesh, Task>? BeforeDrawMesh);

    private void ThrowIfRunning()
    {
        if (_loopTask is not null)
        {
            throw new InvalidOperationException("Cannot modify VelvetHost while running. Call StopAsync() first.");
        }
    }

    private async Task CleanupResizeInteropAsync()
    {
        var bindingId = _resizeBindingId;
        _resizeBindingId = null;
        var orbitBindingId = _orbitInputBindingId;
        _orbitInputBindingId = null;

        if (!string.IsNullOrWhiteSpace(bindingId))
        {
            try
            {
                await _js.InvokeVoidAsync("CanvasHelpers.unbindResizeTracking", bindingId).AsTask().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        if (!string.IsNullOrWhiteSpace(orbitBindingId))
        {
            try
            {
                await _js.InvokeVoidAsync("CanvasHelpers.unbindOrbitInput", orbitBindingId).AsTask().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        _resizeCallbackRef?.Dispose();
        _resizeCallbackRef = null;
    }

    /// <summary>
    /// Enable or disable the directional light.
    /// Can be called while the app is running.
    /// </summary>
    public void SetDirectionalEnabled(bool enabled)
    {
        _directionalEnabled = enabled;
    }

    /// <summary>
    /// Enable or disable the point light.
    /// Can be called while the app is running.
    /// </summary>
    public void SetPointEnabled(bool enabled)
    {
        _pointEnabled = enabled;
    }
}
