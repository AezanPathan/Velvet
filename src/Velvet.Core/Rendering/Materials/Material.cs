using System;
using System.Collections.Generic;
using Velvet.Core.Math;
using Velvet.Core.Rendering.Shaders;

namespace Velvet.Core.Rendering.Materials;

/// <summary>
/// Minimal material system that holds a shader reference and material properties.
/// Properties are stored in a dictionary and applied to the shader as uniforms when Apply() is called.
/// Supports float, Vector3, and Matrix4 property types.
/// </summary>
public sealed class Material
{
    private readonly IShader _shader;
    private readonly Dictionary<string, object> _properties;

    /// <summary>
    /// Creates a new material with the specified shader.
    /// </summary>
    /// <param name="shader">The shader to use for rendering this material</param>
    /// <exception cref="ArgumentNullException">Thrown when shader is null</exception>
    public Material(IShader shader)
    {
        _shader = shader ?? throw new ArgumentNullException(nameof(shader));
        _properties = new Dictionary<string, object>();
    }

    /// <summary>
    /// Sets a material property value.
    /// Supported types: float, Vector3, Matrix4.
    /// </summary>
    /// <param name="name">The name of the property (will be used as uniform name in shader)</param>
    /// <param name="value">The property value (must be float, Vector3, or Matrix4)</param>
    /// <exception cref="ArgumentNullException">Thrown when name or value is null</exception>
    public void Set(string name, object value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        
        _properties[name] = value;
    }

    /// <summary>
    /// Applies the material by activating its shader and setting all property uniforms.
    /// Properties are type-checked and appropriate shader methods are called.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a property has an unsupported type</exception>
    public void Apply()
    {
        _shader.Use();

        foreach (var kvp in _properties)
        {
            var name = kvp.Key;
            var value = kvp.Value;

            switch (value)
            {
                case float floatValue:
                    _shader.SetFloat(name, floatValue);
                    break;

                case Vector3 vector3Value:
                    _shader.SetVector3(name, vector3Value);
                    break;

                case Matrix4 matrix4Value:
                    _shader.SetMatrix4(name, matrix4Value);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported property type '{value.GetType().Name}' for property '{name}'. " +
                        "Supported types: float, Vector3, Matrix4.");
            }
        }
    }
}
