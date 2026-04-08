using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Velvet.Core.Math;
using Velvet.Core.Rendering.Shaders;

namespace Velvet.Graphics.WebGL.Shaders;

/// <summary>
/// WebGL implementation of the IShader interface.
/// Uses an async queuing pattern to avoid blocking on WASM.
/// Uniform calls are enqueued and flushed asynchronously before rendering.
/// </summary>
public sealed class WebGLShader : IShader
{
    private readonly ShaderProgram _program;
    private readonly List<Task> _pendingUniformWrites;

    /// <summary>
    /// Creates a new WebGLShader that wraps the given ShaderProgram.
    /// </summary>
    /// <param name="program">The shader program to wrap</param>
    /// <exception cref="ArgumentNullException">Thrown when program is null</exception>
    public WebGLShader(ShaderProgram program)
    {
        _program = program ?? throw new ArgumentNullException(nameof(program));
        _pendingUniformWrites = new List<Task>(capacity: 16);
    }

    /// <summary>
    /// Activates this shader program for rendering.
    /// In WebGL, program activation is implicit when uniforms are set or draws are made.
    /// </summary>
    public void Use()
    {
        // Implicit activation via ShaderProgram design; no-op here.
    }

    /// <summary>
    /// Enqueues a float uniform write. Call FlushAsync() to apply all pending writes.
    /// </summary>
    /// <param name="name">The name of the uniform variable</param>
    /// <param name="value">The float value to set</param>
    public void SetFloat(string name, float value)
    {
        _pendingUniformWrites.Add(_program.SetUniform1fAsync(name, value));
    }

    /// <summary>
    /// Enqueues a Vector3 uniform write. Call FlushAsync() to apply all pending writes.
    /// </summary>
    /// <param name="name">The name of the uniform variable</param>
    /// <param name="value">The Vector3 value to set</param>
    public void SetVector3(string name, Vector3 value)
    {
        _pendingUniformWrites.Add(_program.SetUniform3fAsync(name, value.X, value.Y, value.Z));
    }

    /// <summary>
    /// Enqueues a Matrix4 uniform write. Call FlushAsync() to apply all pending writes.
    /// </summary>
    /// <param name="name">The name of the uniform variable</param>
    /// <param name="value">The Matrix4 value to set</param>
    public void SetMatrix4(string name, Matrix4 value)
    {
        _pendingUniformWrites.Add(_program.SetUniformMatrix4fvAsync(name, value.Data));
    }

    /// <summary>
    /// Awaits all pending uniform writes and clears the queue.
    /// Must be called before rendering to ensure uniforms are applied.
    /// </summary>
    public async Task FlushAsync()
    {
        if (_pendingUniformWrites.Count == 0)
        {
            return;
        }

        var writes = _pendingUniformWrites.ToArray();
        _pendingUniformWrites.Clear();
        await Task.WhenAll(writes).ConfigureAwait(false);
    }
}
