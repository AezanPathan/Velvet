using Velvet.Core.Math;

namespace Velvet.Core.Rendering.Shaders;

/// <summary>
/// Defines the interface for shader program operations.
/// Provides methods to activate a shader and set uniform values.
/// Implementations should handle shader compilation, linking, and uniform location caching.
/// </summary>
public interface IShader
{
    /// <summary>
    /// Activates this shader program for rendering.
    /// Subsequent draw calls will use this shader until another shader is activated.
    /// </summary>
    void Use();

    /// <summary>
    /// Sets a float uniform value in the shader.
    /// </summary>
    /// <param name="name">The name of the uniform variable in the shader</param>
    /// <param name="value">The float value to set</param>
    void SetFloat(string name, float value);

    /// <summary>
    /// Sets a Vector3 uniform value in the shader.
    /// </summary>
    /// <param name="name">The name of the uniform variable in the shader</param>
    /// <param name="value">The Vector3 value to set</param>
    void SetVector3(string name, Vector3 value);

    /// <summary>
    /// Sets a Matrix4 uniform value in the shader.
    /// </summary>
    /// <param name="name">The name of the uniform variable in the shader</param>
    /// <param name="value">The Matrix4 value to set</param>
    void SetMatrix4(string name, Matrix4 value);
}
