using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Velvet.Core.Engine;
using Velvet.Core.Math;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Lighting;
using Velvet.WebGL;

namespace Velvet.Blazor;

/// <summary>
/// Blazor-first engine entry point for Velvet.
/// Owns the WebGL renderer, shader program, uploaded meshes, and the update/render loop.
/// </summary>
public sealed class VelvetApp
{
    private readonly IWebGLBridge _bridge;
    private readonly IMeshUploader _meshUploader;
    private readonly int _rendererId;

    private readonly List<MeshInstance> _instances = new();
    private List<RenderBatch>? _batches;

    private ShaderProgram? _program;
    private Camera? _camera;
    private DirectionalLight? _directionalLight;
    private PointLight? _pointLight;
    private SpotLight? _spotLight;

    // For demo: ability to enable/disable lights
    private bool _directionalEnabled = true;
    private bool _pointEnabled = true;

    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    private VelvetApp(IWebGLBridge bridge, int rendererId)
    {
        _bridge = bridge;
        _meshUploader = new WebGLMeshUploader(bridge);
        _rendererId = rendererId;
    }

    /// <summary>
    /// Creates and initializes a Velvet application bound to a Blazor canvas.
    /// </summary>
    public static async Task<VelvetApp> CreateAsync(
        ElementReference canvas,
        IJSRuntime js,
        Func<IWebGLBridge, Task<ShaderProgram>>? programFactory = null)
    {
        ArgumentNullException.ThrowIfNull(js);

        var bridge = new BlazorWebGLBridge(js);
        var rendererId = await bridge.InitWithElementAsync(canvas).ConfigureAwait(false);

        var app = new VelvetApp(bridge, rendererId);

        if (programFactory is not null)
        {
            app._program = await programFactory(bridge).ConfigureAwait(false);
        }

        return app;
    }

    public ShaderProgram Program
        => _program ?? throw new InvalidOperationException("Shader program not configured. Provide a programFactory to CreateAsync(...). ");

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

    /// <summary>
    /// Registers a scene with the application. Upload occurs on <see cref="StartAsync"/>.
    /// </summary>
    public void Add(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ThrowIfRunning();

        foreach (var instance in scene.MeshInstances)
        {
            _instances.Add(instance);
        }
    }

    public Task StartAsync(Func<float, Task>? onFrame = null)
    {
        if (_program is null) throw new InvalidOperationException("Shader program not configured. Provide a programFactory to CreateAsync(...).");
        if (_instances.Count == 0) throw new InvalidOperationException("No meshes added. Call Add(scene) before StartAsync().");
        if (_loopTask is not null) return Task.CompletedTask;

        // Build batches from instances
        _batches = RenderBatcher.BuildBatches(_instances, _program);

        _loopCts = new CancellationTokenSource();
        _loopTask = RunLoopAsync(onFrame, beforeDrawMesh: null, _loopCts.Token);
        return Task.CompletedTask;
    }

    public Task StartAsync(Func<float, Task>? onFrame, Func<Mesh, Task>? beforeDrawMesh)
    {
        if (_program is null) throw new InvalidOperationException("Shader program not configured. Provide a programFactory to CreateAsync(...).");
        if (_instances.Count == 0) throw new InvalidOperationException("No meshes added. Call Add(scene) before StartAsync().");
        if (_loopTask is not null) return Task.CompletedTask;

        // Build batches from instances
        _batches = RenderBatcher.BuildBatches(_instances, _program);

        _loopCts = new CancellationTokenSource();
        _loopTask = RunLoopAsync(onFrame, beforeDrawMesh, _loopCts.Token);
        return Task.CompletedTask;
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
        }
    }

    private async Task RunLoopAsync(Func<float, Task>? onFrame, Func<Mesh, Task>? beforeDrawMesh, CancellationToken cancellationToken)
    {
        var program = _program ?? throw new InvalidOperationException("Shader program not configured.");
        var camera = _camera ?? throw new InvalidOperationException("Camera not configured. Assign a Camera before starting.");
        var batches = _batches ?? throw new InvalidOperationException("Batches not built.");

        // Ensure meshes are uploaded before we start drawing.
        foreach (var instance in _instances)
        {
            await instance.Mesh.UploadAsync(_meshUploader, cancellationToken).ConfigureAwait(false);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));

        var stopwatch = Stopwatch.StartNew();
        var lastSeconds = 0f;

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            var nowSeconds = (float)stopwatch.Elapsed.TotalSeconds;
            var deltaSeconds = nowSeconds - lastSeconds;
            lastSeconds = nowSeconds;

            if (onFrame is not null)
            {
                await onFrame(deltaSeconds).ConfigureAwait(false);
            }

            await _bridge.ClearAsync(_rendererId, 0.08f, 0.08f, 0.10f, 1.0f).ConfigureAwait(false);

            // Set per-frame matrices once (View and Projection are constant for all meshes in this frame)
            await program.SetUniformMatrix4fvAsync("uView", camera.ViewMatrix).ConfigureAwait(false);
            await program.SetUniformMatrix4fvAsync("uProjection", camera.ProjectionMatrix).ConfigureAwait(false);

            // Set per-frame lights
            if (_directionalLight is not null)
            {
                var dir = _directionalLight.Direction;
                var intensity = _directionalEnabled ? _directionalLight.Intensity : 0f;
                await program.SetUniform3fAsync("uLightDirection", dir.X, dir.Y, dir.Z).ConfigureAwait(false);
                await program.SetUniform3fAsync("uLightColor", _directionalLight.Color.X, _directionalLight.Color.Y, _directionalLight.Color.Z).ConfigureAwait(false);
                await program.SetUniform1fAsync("uLightIntensity", intensity).ConfigureAwait(false);
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

            // Render each batch
            foreach (var batch in batches)
            {
                // Set batch state once (Material is shared across all instances in this batch)
                await program.SetMaterialAsync(batch.Key.Material).ConfigureAwait(false);

                // Draw all instances in the batch
                foreach (var instance in batch.Instances)
                {
                    var mesh = instance.Mesh;
                    var meshId = mesh.Resources.VertexBufferId.Value;

                    if (beforeDrawMesh is not null)
                    {
                        await beforeDrawMesh(mesh).ConfigureAwait(false);
                    }

                    // Set per-mesh Model matrix and Normal matrix
                    await program.SetUniformMatrix4fvAsync("uModel", instance.ModelMatrix).ConfigureAwait(false);
                    await program.SetUniformMatrix3fvAsync("uNormalMatrix", instance.NormalMatrix).ConfigureAwait(false);

                    await program.DrawMeshAsync(meshId, _rendererId).ConfigureAwait(false);
                }
            }
        }
    }

    private void ThrowIfRunning()
    {
        if (_loopTask is not null)
        {
            throw new InvalidOperationException("Cannot modify VelvetApp while running. Call StopAsync() first.");
        }
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
