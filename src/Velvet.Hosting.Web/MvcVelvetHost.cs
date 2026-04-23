using System.Diagnostics;
using System.Threading;
using Microsoft.JSInterop;
using Velvet.Core.Geometry;
using Velvet.Core.Math;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Batching;
using Velvet.Core.Rendering.Bounds;
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
/// MVC/Razor Pages host for Velvet using canvas-id based initialization.
/// </summary>
public sealed class MvcVelvetHost
{
    private readonly IJSRuntime _js;
    private readonly IWebGLBridge _bridge;
    private readonly IMeshUploader _meshUploader;
    private readonly int _rendererId;
    private readonly ResizeController _resizeController = new();
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private int _isRunning;

    private readonly List<MeshInstance> _instances = new();
    private readonly List<BoundingBox> _instanceBounds = new();
    private readonly List<(Scene Scene, int Start, int Count)> _sceneInstanceRanges = new();
    private readonly Dictionary<Skin, float[]> _boneMatrixCache = new();
    private readonly Dictionary<Skin, long> _bonePreparedFrame = new();
    private readonly Dictionary<Scene, long> _scenePreparedFrame = new();
    private readonly Frustum _frustum = new();
    private List<RenderBatch>? _batches;
    private long _frameIndex;

    private ShaderProgram? _program;
    private ShaderProgram? _skyboxProgram;
    private Camera? _camera;
    private OrbitInputBinder? _orbitBinder;
    private Skybox? _skybox;

    private DotNetObjectReference<MvcVelvetHost>? _callbackRef;
    private string? _resizeBindingId;
    private string? _orbitInputBindingId;

    private DirectionalLight? _directionalLight;
    private PointLight? _pointLight;
    private SpotLight? _spotLight;
    private bool _directionalEnabled = true;
    private bool _pointEnabled = true;

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    private MvcVelvetHost(IJSRuntime js, IWebGLBridge bridge, int rendererId)
    {
        _js = js;
        _bridge = bridge;
        _meshUploader = new WebGLMeshUploader(bridge);
        _rendererId = rendererId;
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

        var app = new MvcVelvetHost(js, resolvedBridge, rendererId)
        {
            _program = await programFactory(resolvedBridge).ConfigureAwait(false)
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

    public bool EnableFrustumCulling { get; set; } = true;

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

    public DirectionalLight? DirectionalLight
    {
        get => _directionalLight;
        set
        {
            ThrowIfRunning();
            _directionalLight = value;
        }
    }

    public PointLight? PointLight
    {
        get => _pointLight;
        set
        {
            ThrowIfRunning();
            _pointLight = value;
        }
    }

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

    public async Task SetSkyboxAsync(Skybox skybox)
    {
        ArgumentNullException.ThrowIfNull(skybox);
        ThrowIfRunning();

        _skybox = skybox;
        _skyboxProgram ??= await ShaderProgram.CreateSkyboxAsync(_bridge).ConfigureAwait(false);
        await skybox.Mesh.UploadAsync(_meshUploader).ConfigureAwait(false);
    }

    public async Task SetCubemapSkyboxAsync(string px, string nx, string py, string ny, string pz, string nz)
    {
        ThrowIfRunning();

        var faceUrls = new[] { px, nx, py, ny, pz, nz };
        var textureId = await _bridge.CreateCubemapTextureAsync(faceUrls).ConfigureAwait(false);

        var geometry = new SkyboxGeometry();
        var mesh = new Mesh(geometry);
        var skybox = new Skybox(mesh, textureId);
        await SetSkyboxAsync(skybox).ConfigureAwait(false);
    }

    public void RequestResize(int width, int height, float dpr)
    {
        _resizeController.RequestResize(width, height, dpr);
    }

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
            _instanceBounds.Add(instance.BoundingBox);
        }

        _sceneInstanceRanges.Add((scene, start, sceneInstances.Count));
    }

    public void Render(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        PrepareSceneForFrame(scene, _frameIndex);
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
            var program = _program ?? throw new InvalidOperationException("Shader program not configured.");
            if (_instances.Count == 0)
            {
                throw new InvalidOperationException("No meshes added. Call Add(scene) before StartAsync().");
            }

            if (_loopTask is not null || Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            {
                return;
            }

            _batches = RenderBatcher.BuildBatches(_instances, program);
            _loopCts = new CancellationTokenSource();
            _loopTask = RunLoopAsync(onFrame, _loopCts.Token);
            _ = _loopTask.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception is not null)
                {
                    Console.WriteLine($"[Velvet MVC] Render loop faulted: {t.Exception}");
                }
            }, TaskScheduler.Default);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? cts;
        Task? task;

        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
        try
        {
            cts = _loopCts;
            task = _loopTask;
            _loopCts = null;
            _loopTask = null;
            Volatile.Write(ref _isRunning, 0);
        }
        finally
        {
            _lifecycleGate.Release();
        }

        if (cts is not null && task is not null)
        {
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
            }
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

    public void SetDirectionalEnabled(bool enabled) => _directionalEnabled = enabled;

    public void SetPointEnabled(bool enabled) => _pointEnabled = enabled;

    private async Task RunLoopAsync(Func<float, Task>? onFrame, CancellationToken cancellationToken)
    {
        try
        {
            var program = _program ?? throw new InvalidOperationException("Shader program not configured.");
            var camera = _camera ?? throw new InvalidOperationException("Camera not configured. Assign a Camera before StartAsync().");
            _ = _batches ?? throw new InvalidOperationException("Batches not built.");

            foreach (var instance in _instances)
            {
                await instance.Mesh.UploadAsync(_meshUploader, cancellationToken).ConfigureAwait(false);
            }

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));
            var stopwatch = Stopwatch.StartNew();
            var lastSeconds = 0f;

            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await ApplyPendingResizeAsync(camera, cancellationToken).ConfigureAwait(false);

                var nowSeconds = (float)stopwatch.Elapsed.TotalSeconds;
                var deltaSeconds = nowSeconds - lastSeconds;
                lastSeconds = nowSeconds;
                _frameIndex++;
                var frameIndex = _frameIndex;

                if (onFrame is not null)
                {
                    await onFrame(deltaSeconds).ConfigureAwait(false);
                }

                _orbitBinder?.Update();

                PrepareAllScenesForFrame(frameIndex);
                var batches = _batches ?? throw new InvalidOperationException("Batches not built.");

                await _bridge.SetBlendModeAsync(_rendererId, "off").ConfigureAwait(false);
                await _bridge.SetDepthMaskAsync(_rendererId, true).ConfigureAwait(false);

                await _bridge.ClearAsync(_rendererId, 0.08f, 0.08f, 0.10f, 1.0f).ConfigureAwait(false);
                await RenderSkyboxAsync(camera).ConfigureAwait(false);
                await SetFrameUniformsAsync(program, camera).ConfigureAwait(false);

                if (EnableFrustumCulling)
                {
                    _frustum.UpdateFromMatrix(camera.ViewProjectionMatrix);
                }

                await RenderBatchesAsync(program, batches).ConfigureAwait(false);
            }
        }
        finally
        {
            Volatile.Write(ref _isRunning, 0);
        }
    }

    private async Task ApplyPendingResizeAsync(Camera camera, CancellationToken cancellationToken)
    {
        if (!_resizeController.HasPendingResize)
        {
            return;
        }

        await _resizeController
            .ApplyResizeAsync((pixelWidth, pixelHeight) => _bridge.ResizeAsync(pixelWidth, pixelHeight), camera, cancellationToken)
            .ConfigureAwait(false);
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
            var normalizedDir = dir.LengthSquared > 0.000001f ? dir.Normalized() : new Vector3(0f, -1f, 0f);
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

    private async Task RenderBatchesAsync(ShaderProgram program, List<RenderBatch> batches)
    {
        foreach (var batch in batches)
        {
            await batch.Key.Material.ApplyAsync(program).ConfigureAwait(false);

            foreach (var instanceIndex in batch.InstanceIndices)
            {
                if ((uint)instanceIndex >= (uint)_instances.Count)
                {
                    continue;
                }

                if (EnableFrustumCulling && !_frustum.Intersects(_instanceBounds[instanceIndex]))
                {
                    continue;
                }

                var instance = _instances[instanceIndex];
                var mesh = instance.Mesh;
                var meshId = mesh.Resources.VertexBufferId.Value;
                mesh.Skin = instance.Skin;

                if (mesh.Skin is not null && _boneMatrixCache.TryGetValue(mesh.Skin, out var boneMatrices))
                {
                    await program.SetBoneMatricesAsync(boneMatrices, mesh.Skin.JointCount).ConfigureAwait(false);
                }

                await program.SetUniformMatrix4fvAsync("uModel", instance.ModelMatrix).ConfigureAwait(false);
                await program.SetUniformMatrix3fvAsync("uNormalMatrix", instance.NormalMatrix).ConfigureAwait(false);
                await program.DrawMeshAsync(meshId, _rendererId).ConfigureAwait(false);
            }
        }
    }

    private void PrepareAllScenesForFrame(long frameIndex)
    {
        foreach (var range in _sceneInstanceRanges)
        {
            PrepareSceneForFrame(range.Scene, frameIndex);
        }
    }

    private void PrepareSceneForFrame(Scene scene, long frameIndex)
    {
        if (_scenePreparedFrame.TryGetValue(scene, out var preparedFrame) && preparedFrame == frameIndex)
        {
            return;
        }

        foreach (var range in _sceneInstanceRanges)
        {
            if (!ReferenceEquals(range.Scene, scene))
            {
                continue;
            }

            UpdateSceneInstances(scene, range.Start, range.Count);
            UpdateBoneMatrices(scene, range.Start, range.Count, frameIndex);
        }

        _scenePreparedFrame[scene] = frameIndex;
    }

    private void UpdateSceneInstances(Scene scene, int startIndex, int instanceCount)
    {
        var nextIndex = startIndex;
        var endIndex = startIndex + instanceCount;

        foreach (var root in scene.Roots)
        {
            UpdateSceneNode(root, Matrix4.Identity.Data, ref nextIndex, endIndex);
        }

        if (nextIndex != endIndex)
        {
            throw new InvalidOperationException("Mesh instance count mismatch while updating transforms.");
        }
    }

    private void UpdateSceneNode(
        SceneNode node,
        float[] parentWorld,
        ref int nextIndex,
        int endIndex)
    {
        var world = Matrix4.Multiply(parentWorld, node.LocalTransform).Data;

        foreach (var mesh in node.Meshes)
        {
            if (nextIndex >= endIndex)
            {
                throw new InvalidOperationException("Mesh instance count mismatch while updating transforms.");
            }

            ApplyInstanceTransform(nextIndex, mesh, world);
            nextIndex++;
        }

        foreach (var child in node.Children)
        {
            UpdateSceneNode(child, world, ref nextIndex, endIndex);
        }
    }

    private void UpdateBoneMatrices(Scene scene, int startIndex, int instanceCount, long frameIndex)
    {
        var endIndex = startIndex + instanceCount;
        var preparedSkins = new HashSet<Skin>();

        for (var i = startIndex; i < endIndex; i++)
        {
            var skin = _instances[i].Skin;
            if (skin is null)
            {
                continue;
            }

            if (!preparedSkins.Add(skin))
            {
                continue;
            }

            if (_bonePreparedFrame.TryGetValue(skin, out var preparedFrame) && preparedFrame == frameIndex)
            {
                continue;
            }

            _boneMatrixCache[skin] = BoneMatrixCalculator.ComputeBoneMatrices(skin, scene.Roots);
            _bonePreparedFrame[skin] = frameIndex;
        }
    }

    private void ApplyInstanceTransform(int index, Mesh mesh, float[] world)
    {
        var instance = _instances[index];

        Array.Copy(world, instance.ModelMatrix, instance.ModelMatrix.Length);
        var normalMatrix = Matrix.NormalMatrix(world);
        Array.Copy(normalMatrix, instance.NormalMatrix, instance.NormalMatrix.Length);

        _instanceBounds[index] = ComputeBounds(mesh.LocalBounds, world);
    }

    private static BoundingBox ComputeBounds(in BoundingBox localBounds, float[] worldMatrix)
    {
        var min = localBounds.Min;
        var max = localBounds.Max;

        var center = new Vector3(
            (min.X + max.X) * 0.5f,
            (min.Y + max.Y) * 0.5f,
            (min.Z + max.Z) * 0.5f);

        var extents = new Vector3(
            (max.X - min.X) * 0.5f,
            (max.Y - min.Y) * 0.5f,
            (max.Z - min.Z) * 0.5f);

        var worldCenter = TransformPoint(worldMatrix, center);

        var ex = MathF.Abs(worldMatrix[0]) * extents.X
               + MathF.Abs(worldMatrix[4]) * extents.Y
               + MathF.Abs(worldMatrix[8]) * extents.Z;
        var ey = MathF.Abs(worldMatrix[1]) * extents.X
               + MathF.Abs(worldMatrix[5]) * extents.Y
               + MathF.Abs(worldMatrix[9]) * extents.Z;
        var ez = MathF.Abs(worldMatrix[2]) * extents.X
               + MathF.Abs(worldMatrix[6]) * extents.Y
               + MathF.Abs(worldMatrix[10]) * extents.Z;

        var worldExtents = new Vector3(ex, ey, ez);
        return new BoundingBox(worldCenter - worldExtents, worldCenter + worldExtents);
    }

    private static Vector3 TransformPoint(float[] matrix, Vector3 point)
    {
        var x = point.X;
        var y = point.Y;
        var z = point.Z;
        var w = 1.0f;

        var resultX = matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12] * w;
        var resultY = matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13] * w;
        var resultZ = matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14] * w;
        var resultW = matrix[3] * x + matrix[7] * y + matrix[11] * z + matrix[15] * w;

        if (MathF.Abs(resultW - 1.0f) > float.Epsilon && resultW != 0f)
        {
            resultX /= resultW;
            resultY /= resultW;
            resultZ /= resultW;
        }

        return new Vector3(resultX, resultY, resultZ);
    }

    private void ThrowIfRunning()
    {
        if (_loopTask is not null)
        {
            throw new InvalidOperationException("Cannot modify host while running. Call StopAsync() first.");
        }
    }

    private async Task CleanupInteropAsync()
    {
        var resizeBindingId = _resizeBindingId;
        _resizeBindingId = null;
        var orbitBindingId = _orbitInputBindingId;
        _orbitInputBindingId = null;

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

        _callbackRef?.Dispose();
        _callbackRef = null;
    }
}
