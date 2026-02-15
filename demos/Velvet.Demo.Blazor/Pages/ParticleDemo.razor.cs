using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Velvet.Core.Math;
using Velvet.Core.Particles;
using Velvet.Core.Rendering;
using Velvet.WebGL;

namespace Velvet.Demo.Blazor.Pages;

public partial class ParticleDemo : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference canvasRef;

    private IWebGLBridge? bridge;
    private int rendererId;
    private Camera? camera;
    private ParticleEmitter? fireEmitter;
    private ParticleEmitter? sparkleEmitter;
    private ParticleEmitter? dustEmitter;

    private ParticleSystem? fireSystem;
    private ParticleSystem? sparkleSystem;
    private ParticleSystem? dustSystem;

    private ParticleRenderer? fireRenderer;
    private ParticleRenderer? sparkleRenderer;
    private ParticleRenderer? dustRenderer;

    private readonly Random rng = new();

    private CancellationTokenSource? loopCts;
    private Task? loopTask;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        bridge = new BlazorWebGLBridge(JS);
        rendererId = await bridge.InitWithElementAsync(canvasRef);

        camera = new Camera(
            position: new Vector3(0, 0, 3.0f),
            target: new Vector3(0, 0, 0),
            up: Vector3.UnitY,
            fovYRadians: 60.0f * (MathF.PI / 180.0f),
            aspectRatio: 800.0f / 600.0f,
            nearPlane: 0.1f,
            farPlane: 100.0f);

        fireEmitter = new ParticleEmitter
        {
            Shape = ParticleEmitterShape.Point,
            Position = new Vector3(0.0f, 0.0f, 0.8f),
            SpawnRate = 42f,
            InitialVelocity = new Vector3(0.0f, 0.6f, 0.0f)
        };

        fireSystem = new ParticleSystem(capacity: 512, fireEmitter)
        {
            ParticleLifetime = 2.6f,
            StartSize = 12f,
            EndSize = 0f,
            StartColor = new Vector4(1.0f, 0.65f, 0.2f, 1.0f),
            EndColor = new Vector4(1.0f, 0.65f, 0.2f, 0.0f),
            BlendMode = ParticleBlendMode.Additive
        };

        sparkleEmitter = new ParticleEmitter
        {
            Shape = ParticleEmitterShape.Box,
            Position = new Vector3(0.0f, 0.35f, 0.7f),
            BoxExtents = new Vector3(0.25f, 0.2f, 0.2f),
            SpawnRate = 10f,
            InitialVelocity = Vector3.Zero
        };

        sparkleSystem = new ParticleSystem(capacity: 256, sparkleEmitter)
        {
            ParticleLifetime = 3.6f,
            StartSize = 5f,
            EndSize = 0f,
            StartColor = new Vector4(0.8f, 0.75f, 1.0f, 1.0f),
            EndColor = new Vector4(0.8f, 0.75f, 1.0f, 0.0f),
            BlendMode = ParticleBlendMode.Additive
        };

        dustEmitter = new ParticleEmitter
        {
            Shape = ParticleEmitterShape.Box,
            Position = new Vector3(0.0f, 0.0f, 1.5f),
            BoxExtents = new Vector3(1.8f, 1.2f, 1.0f),
            SpawnRate = 6f,
            InitialVelocity = Vector3.Zero
        };

        dustSystem = new ParticleSystem(capacity: 384, dustEmitter)
        {
            ParticleLifetime = 6.5f,
            StartSize = 3.5f,
            EndSize = 1.5f,
            StartColor = new Vector4(0.9f, 0.9f, 0.9f, 0.4f),
            EndColor = new Vector4(0.9f, 0.9f, 0.9f, 0.0f),
            BlendMode = ParticleBlendMode.Alpha
        };

        fireRenderer = new ParticleRenderer(fireSystem, bridge);
        sparkleRenderer = new ParticleRenderer(sparkleSystem, bridge);
        dustRenderer = new ParticleRenderer(dustSystem, bridge);

        await fireRenderer.InitializeAsync();
        await sparkleRenderer.InitializeAsync();
        await dustRenderer.InitializeAsync();

        loopCts = new CancellationTokenSource();
        loopTask = RunLoopAsync(loopCts.Token);
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        if (bridge is null || camera is null || fireSystem is null || sparkleSystem is null || dustSystem is null ||
            fireEmitter is null || sparkleEmitter is null || dustEmitter is null ||
            fireRenderer is null || sparkleRenderer is null || dustRenderer is null)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));
        var stopwatch = Stopwatch.StartNew();
        var lastSeconds = 0f;

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            var nowSeconds = (float)stopwatch.Elapsed.TotalSeconds;
            var deltaSeconds = nowSeconds - lastSeconds;
            lastSeconds = nowSeconds;

            sparkleEmitter.InitialVelocity = RandomDrift(0.1f, 0.15f);
            dustEmitter.InitialVelocity = RandomDrift(0.03f, 0.05f);

            fireSystem.Update(deltaSeconds);
            sparkleSystem.Update(deltaSeconds);
            dustSystem.Update(deltaSeconds);

            await bridge.ClearAsync(rendererId, 0.04f, 0.04f, 0.06f, 1.0f).ConfigureAwait(false);

            await fireRenderer.UploadAsync().ConfigureAwait(false);
            await fireRenderer.RenderAsync(rendererId, camera).ConfigureAwait(false);

            await sparkleRenderer.UploadAsync().ConfigureAwait(false);
            await sparkleRenderer.RenderAsync(rendererId, camera).ConfigureAwait(false);

            await dustRenderer.UploadAsync().ConfigureAwait(false);
            await dustRenderer.RenderAsync(rendererId, camera).ConfigureAwait(false);
        }
    }

    private Vector3 RandomDrift(float minSpeed, float maxSpeed)
    {
        var x = (float)(rng.NextDouble() * 2.0 - 1.0);
        var y = (float)(rng.NextDouble() * 2.0 - 1.0);
        var z = (float)(rng.NextDouble() * 2.0 - 1.0);
        var dir = new Vector3(x, y, z);
        var lenSq = dir.LengthSquared;
        if (lenSq <= 0.0001f)
        {
            return new Vector3(0.0f, minSpeed, 0.0f);
        }

        var speed = (float)(minSpeed + rng.NextDouble() * (maxSpeed - minSpeed));
        return dir.Normalized() * speed;
    }

    public async ValueTask DisposeAsync()
    {
        if (loopCts is not null)
        {
            loopCts.Cancel();
        }

        if (loopTask is not null)
        {
            try
            {
                await loopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        loopCts?.Dispose();
    }
}
