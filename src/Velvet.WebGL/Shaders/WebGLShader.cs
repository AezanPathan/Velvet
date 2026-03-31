using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Velvet.Core.Math;
using Velvet.Core.Rendering.Shaders;

namespace Velvet.WebGL.Shaders;

/// <summary>
/// WebGL implementation of the IShader interface.
/// Compiles vertex and fragment shaders, links them into a program, and provides
/// methods to set uniform values. Uniform locations are cached for performance.
/// </summary>
public sealed class WebGLShader : IShader
{
    private readonly IWebGLBridge _bridge;
    private readonly int _programId;
    private readonly Dictionary<string, int> _uniformLocationCache;

    private WebGLShader(IWebGLBridge bridge, int programId)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _programId = programId;
        _uniformLocationCache = new Dictionary<string, int>();
    }

    /// <summary>
    /// Creates and compiles a new WebGL shader from vertex and fragment shader sources.
    /// </summary>
    /// <param name="bridge">The WebGL bridge for communicating with the GPU</param>
    /// <param name="vertexSource">GLSL source code for the vertex shader</param>
    /// <param name="fragmentSource">GLSL source code for the fragment shader</param>
    /// <returns>A new WebGLShader instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
    public static async Task<WebGLShader> CreateAsync(IWebGLBridge bridge, string vertexSource, string fragmentSource)
    {
        ArgumentNullException.ThrowIfNull(bridge);
        ArgumentNullException.ThrowIfNull(vertexSource);
        ArgumentNullException.ThrowIfNull(fragmentSource);

        var vsId = await bridge.CreateShaderAsync(vertexSource, "vertex").ConfigureAwait(false);
        var fsId = await bridge.CreateShaderAsync(fragmentSource, "fragment").ConfigureAwait(false);

        var programId = await bridge.CreateProgramAsync().ConfigureAwait(false);
        await bridge.AttachShaderAsync(programId, vsId).ConfigureAwait(false);
        await bridge.AttachShaderAsync(programId, fsId).ConfigureAwait(false);
        await bridge.LinkProgramAsync(programId).ConfigureAwait(false);

        return new WebGLShader(bridge, programId);
    }

    /// <summary>
    /// Activates this shader program for rendering.
    /// Note: WebGL bridge methods automatically use the program ID, so this is a no-op.
    /// The program is implicitly activated when uniforms are set or draw calls are made.
    /// </summary>
    public void Use()
    {
        // WebGL bridge design: program is activated implicitly when uniforms are set
        // or when DrawMeshAsync is called with this programId.
        // No explicit "use program" call is needed at this layer.
    }

    /// <summary>
    /// Sets a float uniform value in the shader.
    /// </summary>
    /// <param name="name">The name of the uniform variable</param>
    /// <param name="value">The float value to set</param>
    public void SetFloat(string name, float value)
    {
        _bridge.SetUniform1fAsync(_programId, name, value).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Sets a Vector3 uniform value in the shader.
    /// </summary>
    /// <param name="name">The name of the uniform variable</param>
    /// <param name="value">The Vector3 value to set</param>
    public void SetVector3(string name, Vector3 value)
    {
        _bridge.SetUniform3fAsync(_programId, name, value.X, value.Y, value.Z).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Sets a Matrix4 uniform value in the shader.
    /// </summary>
    /// <param name="name">The name of the uniform variable</param>
    /// <param name="value">The Matrix4 value to set</param>
    public void SetMatrix4(string name, Matrix4 value)
    {
        _bridge.SetUniformMatrix4fvAsync(_programId, name, value.Data).GetAwaiter().GetResult();
    }
}
