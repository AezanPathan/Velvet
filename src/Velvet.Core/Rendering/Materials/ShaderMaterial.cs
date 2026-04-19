using Velvet.Core.Math;
using Velvet.Core.Rendering.Core;

namespace Velvet.Core.Rendering.Materials;

/// <summary>
/// Shader-driven material property bag.
/// Stores uniform values and applies them through an <see cref="IRenderProgram"/>.
/// </summary>
public sealed class ShaderMaterial : Material
{
    private readonly Dictionary<string, object> _properties = new();

    public ShaderMaterial(bool unlit = false)
    {
        Unlit = unlit;
    }

    public void Set(string name, object value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        _properties[name] = value;
    }

    public override async Task ApplyAsync(IRenderProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);

        await ApplyBaseAsync(program).ConfigureAwait(false);

        foreach (var kvp in _properties)
        {
            var name = kvp.Key;
            var value = kvp.Value;

            switch (value)
            {
                case float floatValue:
                    await program.SetUniform1fAsync(name, floatValue).ConfigureAwait(false);
                    break;

                case Vector3 vector3Value:
                    await program.SetUniform3fAsync(name, vector3Value.X, vector3Value.Y, vector3Value.Z).ConfigureAwait(false);
                    break;

                case Matrix4 matrix4Value:
                    await program.SetUniformMatrix4fvAsync(name, matrix4Value.Data).ConfigureAwait(false);
                    break;

                case bool boolValue:
                    await program.SetUniform1bAsync(name, boolValue).ConfigureAwait(false);
                    break;

                case int intValue:
                    await program.SetUniform1iAsync(name, intValue).ConfigureAwait(false);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported property type '{value.GetType().Name}' for property '{name}'. " +
                        "Supported types: float, Vector3, Matrix4, bool, int.");
            }
        }
    }
}
