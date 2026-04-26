using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.JSInterop;
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
using Velvet.Hosting.Web.Core;

namespace Velvet.Hosting.Web;

/// <summary>
/// MVC/Razor Pages host for Velvet using canvas-id based initialization.
/// Delegates rendering to VelvetHostCore with animation frame-based loop.
/// </summary>
public sealed class MvcVelvetHost : VelvetHostCore
{
    private static int s_nextHostId;

    private readonly IJSRuntime _js;
    private readonly int _hostId;
    private readonly string _canvasId;
    private readonly ResizeController _resizeController = new();
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private int _isRunning;

    private readonly List<(Scene Scene, long PreparedFrame)> _scenePreparedFrames = new();
    private long _frameIndex;

    private OrbitInputBinder? _orbitBinder;

    private DotNetObjectReference<MvcVelvetHost>? _callbackRef;
    private string? _resizeBindingId;
    private string? _orbitInputBindingId;
    private string? _animationLoopBindingId;

    private readonly SemaphoreSlim _frameGate = new(1, 1);
    private Func<float, Task>? _onFrame;
    private double _lastFrameTimestampMs = -1;
    private TaskCompletionSource<object?>? _loopTcs;
    private Task? _loopTask;

    public bool EnableDebugOverlay { get; set; } = true;

    private MvcVelvetHost(IJSRuntime js, IWebGLBridge bridge, int rendererId, string canvasId)
        : base(bridge, rendererId)
    {
        _js = js;
        _hostId = Interlocked.Increment(ref s_nextHostId);
        _canvasId = canvasId;
    }

    public static async Task<MvcVelvetHost> CreateAsync(
        string canvasId,
        IJSRuntime js,
        Func<IWebGLBridge, Task<ShaderProgram>> programFactory,
        IWebGLBridge? bridge = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        ArgumentNullException.ThrowIfNull(js);
        ArgumentNullException.ThrowIfNull(programFactory);

        var resolvedBridge = bridge ?? new StaticWebGLBridge(js);
        var rendererId = await resolvedBridge.InitWithIdAsync(canvasId).ConfigureAwait(false);

        var app = new MvcVelvetHost(js, resolvedBridge, rendererId, canvasId)
        {
            Program = await programFactory(resolvedBridge).ConfigureAwait(false)
        };

        app._callbackRef = DotNetObjectReference.Create(app);
        app._resizeBindingId = await js.InvokeAsync<string>(
            "CanvasHelpers.bindResizeTrackingById",
            canvasId,
            app._callbackRef,
            nameof(OnResizeFromJs)).AsTask().ConfigureAwait(false);

        app._orbitInputBindingId = await js.InvokeAsync<string>(
            "CanvasHelpers.bindOrbitInputById",
            canvasId,
            app._callbackRef,
            nameof(OnOrbitMouseDownFromJs),
            nameof(OnOrbitMouseMoveFromJs),
            nameof(OnOrbitMouseUpFromJs),
            nameof(OnOrbitWheelFromJs)).AsTask().ConfigureAwait(false);

        return app;
    }

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

    public async Task SetSkyboxAsync(Skybox skybox)
    {
        ArgumentNullException.ThrowIfNull(skybox);
        ThrowIfRunning();
        await SetSkyboxImplAsync(skybox).ConfigureAwait(false);
    }

    public async Task SetCubemapSkyboxAsync(string px, string nx, string py, string ny, string pz, string nz)
    {
        ThrowIfRunning();
        await SetCubemapSkyboxImplAsync(px, nx, py, ny, pz, nz).ConfigureAwait(false);
    }

    public void RequestResize(int width, int height, float dpr)
    {
        _resizeController.RequestResize(width, height, dpr);
    }

    public async Task StartAsync(Func<float, Task>? onFrame = null)
    {
        if (Volatile.Read(ref _isRunning) == 1)
        {
            return;
        }

        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var program = Program ?? throw new InvalidOperationException("Shader program not configured.");
            if (Instances.Count == 0)
            {
                throw new InvalidOperationException("No meshes added. Call Add(scene) before StartAsync().");
            }

            if (_loopTask is not null || Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            {
                return;
            }

            foreach (var instance in Instances)
            {
                await instance.Mesh.UploadAsync(MeshUploader).ConfigureAwait(false);
            }

            Batches = RenderBatcher.BuildBatches(Instances, program);
            _onFrame = onFrame;
            _lastFrameTimestampMs = -1;
            var loopTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _loopTcs = loopTcs;
            _loopTask = loopTcs.Task;

            try
            {
                _animationLoopBindingId = await _js.InvokeAsync<string>(
                    "CanvasHelpers.startAnimationLoopById",
                    _canvasId,
                    _callbackRef,
                    nameof(OnAnimationFrameFromJs)).AsTask().ConfigureAwait(false);
            }
            catch
            {
                _loopTask = null;
                _loopTcs = null;
                _onFrame = null;
                Volatile.Write(ref _isRunning, 0);
                throw;
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync()
    {
        TaskCompletionSource<object?>? loopTcs;
        Task? task;
        string? animationLoopBindingId;

        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
        try
        {
            task = _loopTask;
            loopTcs = _loopTcs;
            animationLoopBindingId = _animationLoopBindingId;
            _animationLoopBindingId = null;
            _loopTask = null;
            _loopTcs = null;
            _onFrame = null;
            Volatile.Write(ref _isRunning, 0);
        }
        finally
        {
            _lifecycleGate.Release();
        }

        if (!string.IsNullOrWhiteSpace(animationLoopBindingId))
        {
            try
            {
                await _js.InvokeVoidAsync("CanvasHelpers.stopAnimationLoop", animationLoopBindingId).AsTask().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        await _frameGate.WaitAsync().ConfigureAwait(false);
        _frameGate.Release();

        loopTcs?.TrySetResult(null);
        if (task is not null)
        {
            await task.ConfigureAwait(false);
        }

        await CleanupInteropAsync().ConfigureAwait(false);
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

    [JSInvokable]
    public Task OnAnimationFrameFromJs(double timestampMs)
    {
        return RenderFrameAsync(timestampMs);
    }

    public void SetDirectionalEnabled(bool enabled) => DirectionalEnabled = enabled;

    public void SetPointEnabled(bool enabled) => PointEnabled = enabled;

    private async Task RenderFrameAsync(double timestampMs)
    {
        if (Volatile.Read(ref _isRunning) == 0)
        {
            return;
        }

        if (!await _frameGate.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            var program = Program ?? throw new InvalidOperationException("Shader program not configured.");
            var camera = Camera ?? throw new InvalidOperationException("Camera not configured. Assign a Camera before StartAsync().");
            var batches = Batches ?? throw new InvalidOperationException("Batches not built.");

            if (Volatile.Read(ref _isRunning) == 0)
            {
                return;
            }

            await ApplyPendingResizeAsync(camera).ConfigureAwait(false);

            float deltaSeconds;
            if (_lastFrameTimestampMs < 0)
            {
                deltaSeconds = 0f;
            }
            else
            {
                var deltaMs = timestampMs - _lastFrameTimestampMs;
                if (deltaMs < 0)
                {
                    deltaMs = 0;
                }

                deltaSeconds = (float)(deltaMs / 1000.0);
                if (deltaSeconds > 0.25f)
                {
                    deltaSeconds = 0.25f;
                }
            }

            _lastFrameTimestampMs = timestampMs;
            _frameIndex++;
            var frameIndex = _frameIndex;

            var onFrame = _onFrame;
            if (onFrame is not null)
            {
                await onFrame(deltaSeconds).ConfigureAwait(false);
            }

            _orbitBinder?.Update();

            PrepareAllScenesForFrame(frameIndex);
            await Bridge.SetBlendModeAsync(RendererId, "off");
            await Bridge.SetDepthMaskAsync(RendererId, true);
            await Bridge.ClearAsync(RendererId, 0.08f, 0.08f, 0.10f, 1.0f).ConfigureAwait(false);
            await RenderSkyboxAsync(camera).ConfigureAwait(false);
            await SetFrameUniformsAsync(program, camera).ConfigureAwait(false);

            if (EnableFrustumCulling)
            {
                Frustum.UpdateFromMatrix(camera.ViewProjectionMatrix);
            }

            await RenderBatchesAsync(program, batches).ConfigureAwait(false);
            await UpdateDebugOverlayAsync(frameIndex, deltaSeconds, batches.Count).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Volatile.Read(ref _isRunning) == 1)
            {
                Volatile.Write(ref _isRunning, 0);
                _loopTcs?.TrySetException(ex);
                Console.WriteLine($"[Velvet MVC] Frame render faulted: {ex}");
            }
        }
        finally
        {
            _frameGate.Release();
        }
    }

    private async Task ApplyPendingResizeAsync(Camera camera)
    {
        if (!_resizeController.HasPendingResize)
        {
            return;
        }

        await _resizeController
            .ApplyResizeAsync((pixelWidth, pixelHeight) => Bridge.ResizeAsync(pixelWidth, pixelHeight), camera, CancellationToken.None)
            .ConfigureAwait(false);
    }

    private async Task UpdateDebugOverlayAsync(long frameIndex, float deltaSeconds, int batchCount)
    {
        if (!EnableDebugOverlay)
        {
            return;
        }

        if (frameIndex % 8 != 0)
        {
            return;
        }

        var text =
            $"Velvet MVC Debug\n" +
            $"host={_hostId} renderer={RendererId}\n" +
            $"frame={frameIndex} dt={deltaSeconds * 1000f:F1}ms\n" +
            $"batches={batchCount} instances={Instances.Count}\n" +
            $"rendered={LastFrameRenderedMeshes} culled={LastFrameCulledMeshes}\n" +
            $"boneCache={BoneMatrixCache.Count} scenes={SceneInstanceRanges.Count}";

        try
        {
            await _js.InvokeVoidAsync("CanvasHelpers.setDebugOverlayById", _canvasId, text).AsTask().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void PrepareAllScenesForFrame(long frameIndex)
    {
        foreach (var range in SceneInstanceRanges)
        {
            PrepareSceneForFrame(range.Scene, frameIndex);
        }
    }

    private void PrepareSceneForFrame(Scene scene, long frameIndex)
    {
        var preparedFrame = _scenePreparedFrames.FirstOrDefault(s => ReferenceEquals(s.Scene, scene)).PreparedFrame;
        if (preparedFrame == frameIndex)
        {
            return;
        }

        foreach (var range in SceneInstanceRanges)
        {
            if (!ReferenceEquals(range.Scene, scene))
            {
                continue;
            }

            UpdateSceneInstances(scene, range.Start, range.Count);
            UpdateBoneMatrices(scene, range.Start, range.Count);
        }

        // Update or add frame tracker
        var index = _scenePreparedFrames.FindIndex(s => ReferenceEquals(s.Scene, scene));
        if (index >= 0)
        {
            _scenePreparedFrames[index] = (scene, frameIndex);
        }
        else
        {
            _scenePreparedFrames.Add((scene, frameIndex));
        }
    }

    protected override void ThrowIfRunning()
    {
        if (_loopTask is not null)
        {
            throw new InvalidOperationException("Cannot modify host while running. Call StopAsync() first.");
        }
    }

    private async Task CleanupInteropAsync()
    {
        var animationLoopBindingId = _animationLoopBindingId;
        _animationLoopBindingId = null;
        var resizeBindingId = _resizeBindingId;
        _resizeBindingId = null;
        var orbitBindingId = _orbitInputBindingId;
        _orbitInputBindingId = null;

        if (!string.IsNullOrWhiteSpace(animationLoopBindingId))
        {
            try
            {
                await _js.InvokeVoidAsync("CanvasHelpers.stopAnimationLoop", animationLoopBindingId).AsTask().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        if (!string.IsNullOrWhiteSpace(resizeBindingId))
        {
            try
            {
                await _js.InvokeVoidAsync("CanvasHelpers.unbindResizeTracking", resizeBindingId).AsTask().ConfigureAwait(false);
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

        if (!string.IsNullOrWhiteSpace(_canvasId))
        {
            try
            {
                await _js.InvokeVoidAsync("CanvasHelpers.clearDebugOverlayById", _canvasId).AsTask().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        _callbackRef?.Dispose();
        _callbackRef = null;
    }
}
