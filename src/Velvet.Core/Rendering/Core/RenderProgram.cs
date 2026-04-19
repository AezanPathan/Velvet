namespace Velvet.Core.Rendering.Core;

/// <summary>
/// Marker interface for backend-specific render program handles used by core batching.
/// Implementations live in backend projects (for example WebGL).
/// </summary>
public interface IRenderProgram
{
	Task SetUniformMatrix4fvAsync(string name, float[] matrix);

	Task SetUniform3fAsync(string name, float x, float y, float z);

	Task SetUniform1fAsync(string name, float value);

	Task SetUniform1iAsync(string name, int value);

	Task SetUniform1bAsync(string name, bool value);

	Task BindTextureAsync(string samplerUniform, string textureUri, int textureUnit);
}
