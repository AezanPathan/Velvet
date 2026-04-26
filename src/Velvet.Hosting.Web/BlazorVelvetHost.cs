using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Velvet.Core.Particles;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Batching;
using Velvet.Core.Rendering.Cameras;
using Velvet.Core.Rendering.Controllers;
using Velvet.Core.Rendering.Core;
using Velvet.Core.Rendering.Environment;
using Velvet.Core.Rendering.Input;
using Velvet.Core.Rendering.Lighting;
using Velvet.Core.Rendering.Meshes;
using Velvet.Core.Scene;
using Velvet.Graphics.WebGL;
using Velvet.Hosting.Web.Core;

namespace Velvet.Hosting.Web;

/// <summary>
/// Blazor-first engine entry point for Velvet.
/// Thin wrapper around VelvetHostCore with timer-based rendering loop.
/// </summary>
public sealed class BlazorVelvetHost : VelvetHostCore
{
    private readonly IJSRuntime _js;
    private readonly ResizeController _resizeController = new();

    // Particle system support
    private readonly List<Velvet.Core.Particles.ParticleSystem> _particleSystems = new();
    private readonly List<ParticleRenderer> _particleRenderers = new();

    private OrbitInputBinder? _orbitBinder;

    private DotNetObjectReference<BlazorVelvetHost>? _resizeCallbackRef;
    private string? _resizeBindingId;
    private string? _orbitInputBindingId;

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    private BlazorVelvetHost(IJSRuntime js, IWebGLBridge bridge, int rendererId)
        : base(bridge, rendererId)
    {
        _js = js;
    }

    /// <summary>
    /// Creates and initializes a Velvet application bound to a Blazor canvas.
    /// </summary>
    public static async Task<BlazorVelvetHost> CreateAsync(
        ElementReference canvas,
        IJSRuntime js,
        Func<IWebGLBridge, Task<ShaderProgram>>? programFactory = null)
    {
        ArgumentNullException.ThrowIfNull(js);

        var bridge = new BlazorWebGLBridge(js);
        var rendererId = await bridge.InitWithElementAsync(canvas).ConfigureAwait(false);

        var app = new BlazorVelvetHost(js, bridge, rendererId);
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
            app.Program = await programFactory(bridge).ConfigureAwait(false);
        }

        return app;
    }

    public new int LastFrameTotalMeshes => base.LastFrameTotalMeshes;

    public new int LastFrameCulledMeshes => base.LastFrameCulledMeshes;

    public new int LastFrameRenderedMeshes => base.LastFrameRenderedMeshes;

    public void SetCamera(Camera? camera)
    {
        ThrowIfRunning();
        Camera = camera;
        _resizeController.ApplyViewportToCamera(Camera);
    }

    public void SetDirectionalLight(DirectionalLight? light)
    {
        ThrowIfRunning();
        DirectionalLight = light;
    }

    public void SetPointLight(PointLight? light)
    {
        ThrowIfRunning();
        PointLight = light;
    }

    public void SetSpotLight(SpotLight? light)
    {
        ThrowIfRunning();
        SpotLight = light;
    }

    public void SetController(OrbitController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ThrowIfRunning();

        if (Camera is null)
        {
            throw new InvalidOperationException("Camera must be set before controller.");
        }

        _orbitBinder = new OrbitInputBinder(controller, Camera);
    }

    /// <summary>
    /// Sets the skybox for the scene.
    /// The skybox will be rendered as an infinitely distant background.
    /// </summary>
    public async Task SetSkybox(Skybox skybox)
    {
        ArgumentNullException.ThrowIfNull(skybox);
        ThrowIfRunning();
        await SetSkyboxImplAsync(skybox).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates and sets a cubemap skybox from 6 face images.
    /// Face order: +X, -X, +Y, -Y, +Z, -Z
    /// </summary>
    public async Task SetCubemapSkybox(string px, string nx, string py, string ny, string pz, string nz)
    {
        ThrowIfRunning();
        await SetCubemapSkyboxImplAsync(px, nx, py, ny, pz, nz).ConfigureAwait(false);
    }

    /// <summary>
    /// Queues a resize request to be applied at the start of the next render frame.
    /// </summary>
    public void RequestResize(int width, int height, float dpr)
    {
        _resizeController.RequestResize(width, height, dpr);
    }

    /// <summary>
    /// Registers a particle system with the application.
    /// Particle renderer initialization occurs on StartAsync.
    /// </summary>
    public void Add(ParticleSystem particleSystem)
    {
        ArgumentNullException.ThrowIfNull(particleSystem);
        ThrowIfRunning();

        _particleSystems.Add(particleSystem);
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

    /// <summary>
    /// Enable or disable the directional light.
    /// Can be called while the app is running.
    /// </summary>
    public void SetDirectionalEnabled(bool enabled)
    {
        DirectionalEnabled = enabled;
    }

    /// <summary>
    /// Enable or disable the point light.
    /// Can be called while the app is running.
    /// </summary>
    public void SetPointEnabled(bool enabled)
    {
        PointEnabled = enabled;
    }

    private Task StartAsyncCore(FrameCallbacks callbacks)
    {
        var program = Program!;
        if (Instances.Count == 0 && _particleSystems.Count == 0)
        {
            throw new InvalidOperationException("No meshes or particle systems added. Call Add(scene) or Add(particleSystem) before StartAsync().");
        }

        if (_loopTask is not null)
        {
            return Task.CompletedTask;
        }

        Batches = RenderBatcher.BuildBatches(Instances, program);
        _loopCts = new CancellationTokenSource();
        _loopTask = RunLoopAsync(callbacks, _loopCts.Token);
        return Task.CompletedTask;
    }

    private async Task RunLoopAsync(FrameCallbacks callbacks, CancellationToken cancellationToken)
    {
        var program = Program!;
        var camera = Camera ?? throw new InvalidOperationException("Camera not configured. Assign a Camera before starting.");
        var batches = Batches ?? throw new InvalidOperationException("Batches not built.");

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

    private async Task InitializeParticleRenderersAsync()
    {
        foreach (var particleSystem in _particleSystems)
        {
            var particleRenderer = new ParticleRenderer(particleSystem, Bridge);
            await particleRenderer.InitializeAsync().ConfigureAwait(false);
            _particleRenderers.Add(particleRenderer);
        }
    }

    private async Task ApplyPendingResizeAsync(Camera camera, CancellationToken cancellationToken)
    {
        if (_resizeController.HasPendingResize)
        {
            await _resizeController
                .ApplyResizeAsync((pixelWidth, pixelHeight) => Bridge.ResizeAsync(pixelWidth, pixelHeight), camera, cancellationToken)
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
        BoneMatrixCache.Clear();

        foreach (var particleSystem in _particleSystems)
        {
            particleSystem.Update(deltaSeconds);
        }

        if (callbacks.OnFrame is not null)
        {
            await callbacks.OnFrame(deltaSeconds).ConfigureAwait(false);
        }

        await Bridge.ClearAsync(RendererId, 0.08f, 0.08f, 0.10f, 1.0f).ConfigureAwait(false);
        await RenderSkyboxAsync(camera).ConfigureAwait(false);
        await SetFrameUniformsAsync(program, camera).ConfigureAwait(false);

        if (EnableFrustumCulling)
        {
            Frustum.UpdateFromMatrix(camera.ViewProjectionMatrix);
        }

        await RenderBatchesAsync(program, batches, callbacks.BeforeDrawMesh).ConfigureAwait(false);
        await RenderParticlesAsync(camera).ConfigureAwait(false);
    }

    private async Task RenderParticlesAsync(Camera camera)
    {
        foreach (var particleRenderer in _particleRenderers)
        {
            await particleRenderer.UploadAsync().ConfigureAwait(false);
            await particleRenderer.RenderAsync(RendererId, camera).ConfigureAwait(false);
        }
    }

    private readonly record struct FrameCallbacks(
        Func<float, Task>? OnFrame,
        Func<Mesh, Task>? BeforeDrawMesh);

    protected override void ThrowIfRunning()
    {
        if (_loopTask is not null)
        {
            throw new InvalidOperationException("Cannot modify BlazorVelvetHost while running. Call StopAsync() first.");
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
}
