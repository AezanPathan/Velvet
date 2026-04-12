using System;
using System.Collections.Generic;
using Velvet.Core.Math;
using Velvet.Core.Rendering.Shaders;

namespace Velvet.Core.Rendering.Materials;

/// <summary>
/// Shader-driven material property bag.
/// Stores uniform values and applies them through an <see cref="IShader"/>.
/// </summary>
public class ShaderMaterial
{
    private readonly IShader _shader;
    private readonly Dictionary<string, object> _properties;

    public ShaderMaterial(IShader shader)
    {
        _shader = shader ?? throw new ArgumentNullException(nameof(shader));
        _properties = new Dictionary<string, object>();
    }

    public void Set(string name, object value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        _properties[name] = value;
    }

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
