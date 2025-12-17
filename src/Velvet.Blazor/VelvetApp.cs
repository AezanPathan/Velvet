using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Velvet.Core.Rendering;
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

    private readonly List<Mesh> _meshes = new();

    private int _programId = -1;

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
    public static async Task<VelvetApp> CreateAsync(ElementReference canvas, IJSRuntime js)
    {
        ArgumentNullException.ThrowIfNull(js);

        var bridge = new BlazorWebGLBridge(js);
        var rendererId = await bridge.InitWithElementAsync(canvas).ConfigureAwait(false);

        return new VelvetApp(bridge, rendererId);
    }

    /// <summary>
    /// Compiles and links the default shader program.
    /// No camera is used; view/projection default to identity.
    /// </summary>
    public async Task UseDefaultShaderAsync()
    {
        ThrowIfRunning();

        var vsId = await _bridge.CreateShaderAsync(DefaultVertexShader, "vertex").ConfigureAwait(false);
        var fsId = await _bridge.CreateShaderAsync(DefaultFragmentShader, "fragment").ConfigureAwait(false);

        _programId = await _bridge.CreateProgramAsync().ConfigureAwait(false);
        await _bridge.AttachShaderAsync(_programId, vsId).ConfigureAwait(false);
        await _bridge.AttachShaderAsync(_programId, fsId).ConfigureAwait(false);
        await _bridge.LinkProgramAsync(_programId).ConfigureAwait(false);

        // No camera yet: identity view/projection keeps the unit cube in clip space.
        var identity = Mat4.Identity();
        await _bridge.SetUniformMatrix4fvAsync(_programId, "uView", identity).ConfigureAwait(false);
        await _bridge.SetUniformMatrix4fvAsync(_programId, "uProjection", identity).ConfigureAwait(false);
    }

    /// <summary>
    /// Registers a mesh with the application. Upload occurs on <see cref="StartAsync"/>.
    /// </summary>
    public void Add(Mesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ThrowIfRunning();
        _meshes.Add(mesh);
    }

    public Task StartAsync()
    {
        if (_programId < 0) throw new InvalidOperationException("Shader program not configured. Call UseDefaultShaderAsync() first.");
        if (_meshes.Count == 0) throw new InvalidOperationException("No meshes added. Call Add(mesh) before StartAsync().");
        if (_loopTask is not null) return Task.CompletedTask;

        _loopCts = new CancellationTokenSource();
        _loopTask = RunLoopAsync(_loopCts.Token);
        return Task.CompletedTask;
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

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        // Ensure meshes are uploaded before we start drawing.
        foreach (var mesh in _meshes)
        {
            await mesh.UploadAsync(_meshUploader, cancellationToken).ConfigureAwait(false);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));

        var angle = 0f;
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            angle += 0.02f;

            var model = Mat4.Multiply(Mat4.RotateY(angle), Mat4.RotateX(angle * 0.7f));

            await _bridge.ClearAsync(_rendererId, 0.08f, 0.08f, 0.10f, 1.0f).ConfigureAwait(false);

            foreach (var mesh in _meshes)
            {
                var meshId = mesh.Resources.VertexBufferId.Value;

                await _bridge.SetUniformMatrix4fvAsync(_programId, "uModel", model).ConfigureAwait(false);
                await _bridge.DrawMeshAsync(meshId, _programId, _rendererId).ConfigureAwait(false);
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

    private const string DefaultVertexShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "layout(location = 0) in vec3 aPosition;\n" +
        "layout(location = 1) in vec3 aColor;\n" +
        "\n" +
        "uniform mat4 uModel;\n" +
        "uniform mat4 uView;\n" +
        "uniform mat4 uProjection;\n" +
        "\n" +
        "out vec3 vColor;\n" +
        "\n" +
        "void main() {\n" +
        "    vColor = aColor;\n" +
        "    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);\n" +
        "}\n";

    private const string DefaultFragmentShader = "#version 300 es\n" +
        "precision mediump float;\n" +
        "\n" +
        "in vec3 vColor;\n" +
        "out vec4 outColor;\n" +
        "\n" +
        "void main() {\n" +
        "    outColor = vec4(vColor, 1.0);\n" +
        "}\n";

    private static class Mat4
    {
        public static float[] Identity() =>
        [
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        ];

        public static float[] RotateX(float angle)
        {
            var c = (float)Math.Cos(angle);
            var s = (float)Math.Sin(angle);

            // Column-major
            return
            [
                1, 0, 0, 0,
                0, c, s, 0,
                0, -s, c, 0,
                0, 0, 0, 1
            ];
        }

        public static float[] RotateY(float angle)
        {
            var c = (float)Math.Cos(angle);
            var s = (float)Math.Sin(angle);

            // Column-major
            return
            [
                c, 0, -s, 0,
                0, 1, 0, 0,
                s, 0, c, 0,
                0, 0, 0, 1
            ];
        }

        public static float[] Multiply(float[] a, float[] b)
        {
            if (a.Length != 16) throw new ArgumentException("Expected 4x4 matrix", nameof(a));
            if (b.Length != 16) throw new ArgumentException("Expected 4x4 matrix", nameof(b));

            var r = new float[16];
            for (var col = 0; col < 4; col++)
            {
                for (var row = 0; row < 4; row++)
                {
                    r[row + col * 4] =
                        a[row + 0 * 4] * b[0 + col * 4] +
                        a[row + 1 * 4] * b[1 + col * 4] +
                        a[row + 2 * 4] * b[2 + col * 4] +
                        a[row + 3 * 4] * b[3 + col * 4];
                }
            }

            return r;
        }
    }
}
