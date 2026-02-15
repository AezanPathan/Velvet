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

public partial class WaterFlowDemo : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference canvasRef;

    private IWebGLBridge? bridge;
    private int rendererId;
    private Camera? camera;
    private ParticleEmitter? flowEmitter;
    private ParticleSystem? flowSystem;
    private ParticleRenderer? flowRenderer;

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

        flowEmitter = new ParticleEmitter
        {
            Shape = ParticleEmitterShape.Box,
            Position = new Vector3(-0.9f, 0.0f, 0.6f),
            BoxExtents = new Vector3(1.2f, 0.12f, 0.5f),
            SpawnRate = 120f,
            InitialVelocity = new Vector3(0.7f, 0.02f, 0.0f)
        };

        flowSystem = new ParticleSystem(capacity: 2048, flowEmitter)
        {
            ParticleLifetime = 4.5f,
            StartSize = 4.0f,
            EndSize = 1.5f,
            StartColor = new Vector4(0.3f, 0.7f, 1.0f, 0.55f),
            EndColor = new Vector4(0.4f, 0.85f, 1.0f, 0.0f),
            BlendMode = ParticleBlendMode.Alpha
        };

        flowRenderer = new ParticleRenderer(flowSystem, bridge);
        await flowRenderer.InitializeAsync();

        loopCts = new CancellationTokenSource();
        loopTask = RunLoopAsync(loopCts.Token);
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        if (bridge is null || camera is null || flowSystem is null || flowEmitter is null || flowRenderer is null)
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

            flowEmitter.InitialVelocity = FlowVelocity(nowSeconds);
            flowSystem.Update(deltaSeconds);

            await bridge.ClearAsync(rendererId, 0.02f, 0.05f, 0.09f, 1.0f).ConfigureAwait(false);

            await flowRenderer.UploadAsync().ConfigureAwait(false);
            await flowRenderer.RenderAsync(rendererId, camera).ConfigureAwait(false);
        }
    }

    private static Vector3 FlowVelocity(float timeSeconds)
    {
        var baseSpeed = 0.7f;
        var sway = MathF.Sin(timeSeconds * 0.7f) * 0.06f;
        var lift = MathF.Sin(timeSeconds * 0.9f + 1.1f) * 0.03f;
        var depth = MathF.Cos(timeSeconds * 0.6f) * 0.03f;
        return new Vector3(baseSpeed + sway, lift, depth);
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
