namespace Velvet.Core.Rendering.Shaders;

using Velvet.Core.Math;

/// <summary>
/// Minimal shader abstraction for setting uniforms and activating programs.
/// </summary>
public interface IShader
{
    void Use();

    void SetFloat(string name, float value);

    void SetVector3(string name, Vector3 value);

    void SetMatrix4(string name, Matrix4 value);
}