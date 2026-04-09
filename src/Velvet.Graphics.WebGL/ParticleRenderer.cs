using System;
using System.Threading.Tasks;
using Velvet.Core.Particles;
using Velvet.Core.Rendering;
using Velvet.Core.Rendering.Cameras;

namespace Velvet.Graphics.WebGL;

/// <summary>
/// GPU renderer for <see cref="ParticleSystem"/> using WebGL point rendering.
/// </summary>
public sealed class ParticleRenderer
{
    private readonly ParticleSystem _system;
    private readonly IWebGLBridge _bridge;
    private ShaderProgram? _program;
    private int _meshId = -1;
    private float[]? _vertexBuffer;

    public ShaderProgram? Program => _program;

    public ParticleRenderer(ParticleSystem system, IWebGLBridge bridge)
    {
        _system = system ?? throw new ArgumentNullException(nameof(system));
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
    }

    public async Task InitializeAsync()
    {
        if (_program != null)
            return;

        _program = await ShaderProgram.CreateParticlesAsync(_bridge).ConfigureAwait(false);
        _meshId = await _bridge.CreateParticleMeshAsync(_system.Capacity).ConfigureAwait(false);
        _vertexBuffer = new float[_system.Capacity * 8];
    }

    /// <summary>
    /// Uploads particle render data to the GPU.
    /// </summary>
    public async Task UploadAsync()
    {
        if (_program == null || _vertexBuffer == null || _meshId < 0)
            throw new InvalidOperationException("ParticleRenderer is not initialized.");

        int count = _system.WriteRenderBuffer(_vertexBuffer);
        await _bridge.UpdateMeshVerticesAsync(_meshId, _vertexBuffer, count).ConfigureAwait(false);
    }

    /// <summary>
    /// Renders particles as GPU points.
    /// </summary>
    public async Task RenderAsync(int rendererId, Camera camera)
    {
        if (_program == null || _meshId < 0)
            throw new InvalidOperationException("ParticleRenderer is not initialized.");

        ArgumentNullException.ThrowIfNull(camera);

        await _bridge.SetBlendModeAsync(rendererId, BlendModeToString(_system.BlendMode)).ConfigureAwait(false);
        await _program.SetUniformMatrix4fvAsync("uView", camera.ViewMatrix).ConfigureAwait(false);
        await _program.SetUniformMatrix4fvAsync("uProjection", camera.ProjectionMatrix).ConfigureAwait(false);
        await _program.DrawMeshAsync(_meshId, rendererId).ConfigureAwait(false);
    }

    private static string BlendModeToString(ParticleBlendMode mode)
        => mode switch
        {
            ParticleBlendMode.Additive => "additive",
            ParticleBlendMode.Off => "off",
            _ => "alpha"
        };
}
