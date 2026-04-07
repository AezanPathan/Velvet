using System.Threading.Tasks;

namespace Velvet.WebGL
{
    /// <summary>
    /// Minimal JS bridge interface for Velvet's WebGL backend.
    /// 
    /// Implementations of this interface provide the tiny "glue" layer
    /// between the .NET world and the JavaScript VelvetAPI.
    /// 
    /// Design goals:
    /// - Keep the bridge surface small and stable (don't mirror every JS method).
    /// - Support Blazor (ElementReference) and static WASM (canvas ID string).
    /// - Allow implementations to use IJSRuntime, JSImport, or any other transport.
    /// </summary>
    public interface IWebGLBridge
    {
        #region Initialization

        /// <summary>
        /// Initialize the Velvet renderer by passing a *canvas element* from the host.
        /// 
        /// Blazor implementation: pass an <see cref="Microsoft.AspNetCore.Components.ElementReference"/>
        /// (use <c>object</c> to avoid a compile-time dependency on Blazor in this project).
        /// The JavaScript side will receive the actual DOM element.
        /// 
        /// Returns a renderer ID that the host / engine can use for subsequent draw calls.
        /// </summary>
        /// <param name="canvasElement">
        /// Host-specific canvas element (ElementReference for Blazor, or any host object).</param>
        /// <returns>Renderer ID assigned by the JS engine.</returns>
        Task<int> InitWithElementAsync(object canvasElement);

        /// <summary>
        /// Initialize the Velvet renderer by canvas identifier (static WASM or simple HTML apps).
        /// Use this for hosts that only have a string ID for the canvas.
        /// Returns a renderer ID.
        /// </summary>
        /// <param name="canvasId">DOM id of the canvas element (e.g. "velvetCanvas").</param>
        /// <returns>Renderer ID assigned by the JS engine.</returns>
        Task<int> InitWithIdAsync(string canvasId);

        #endregion

        #region Resource creation / management

        /// <summary>
        /// Compile a shader from source on the JS side.
        /// <para>Type is "vertex" or "fragment". Returns a shader resource ID.</para>
        /// </summary>
        Task<int> CreateShaderAsync(string source, string type);

        /// <summary>
        /// Create an empty GPU program and return its ID.
        /// </summary>
        Task<int> CreateProgramAsync();

        /// <summary>
        /// Attach a compiled shader (shaderId) to a program (programId).
        /// </summary>
        Task AttachShaderAsync(int programId, int shaderId);

        /// <summary>
        /// Link the program identified by programId. Throws/returns an error on link failure.
        /// </summary>
        Task LinkProgramAsync(int programId);

        /// <summary>
        /// Create a mesh resource (returns meshId).
        /// Vertices should be an array of floats (e.g. interleaved attributes).
        /// Indices may be null for non-indexed geometry.
        /// </summary>
        Task<int> CreateMeshAsync(float[] vertices, uint[]? indices = null, int vertexStrideFloats = 0);

        /// <summary>
        /// Create a GPU mesh for particle rendering (points).
        /// Allocates a fixed-size vertex buffer for the given capacity.
        /// </summary>
        Task<int> CreateParticleMeshAsync(int capacity);

        /// <summary>
        /// Update mesh vertex buffer data and vertex count.
        /// </summary>
        Task UpdateMeshVerticesAsync(int meshId, float[] vertices, int vertexCount);

        Task SetUniform3fAsync(int programId, string name, float x, float y, float z);
        Task SetUniform1fAsync(int programId, string name, float value);
        Task SetUniformMatrix3fvAsync(int programId, string name, float[] matrix);

        Task SetUniform1iAsync(int programId, string name, int value);
        Task SetUniform1bAsync(int programId, string name, bool value);

        /// <summary>
        /// Create a texture from a URL on the JS side and return a texture ID.
        /// </summary>
        Task<int> CreateTextureFromUrlAsync(string url);

        /// <summary>
        /// Create a cubemap texture from 6 face URLs and return a texture ID.
        /// Face order: +X, -X, +Y, -Y, +Z, -Z
        /// </summary>
        Task<int> CreateCubemapTextureAsync(string[] faceUrls);

        /// <summary>
        /// Bind a texture by ID to the given sampler uniform on a program.
        /// </summary>
        Task BindTextureAsync(int programId, string samplerName, int textureId, int textureUnit);

        /// <summary>
        /// Bind a cubemap texture by ID to the given sampler uniform on a program.
        /// </summary>
        Task BindCubemapTextureAsync(int programId, string samplerName, int textureId, int textureUnit);

        #endregion

        #region Rendering / state

        /// <summary>
        /// Draw a mesh using programId on the renderer identified by rendererId.
        /// This is intentionally minimal: the JS side will resolve IDs to real objects.
        /// </summary>
        Task DrawMeshAsync(int meshId, int programId, int rendererId);

        /// <summary>
        /// Clear the framebuffer with the given color on the specified renderer.
        /// </summary>
        Task ClearAsync(int rendererId, float r, float g, float b, float a);

        /// <summary>
        /// Configure GPU blend mode for the specified renderer.
        /// Supported values: "off", "alpha", "additive".
        /// </summary>
        Task SetBlendModeAsync(int rendererId, string mode);

        /// <summary>
        /// Set a mat4 uniform on the given program.
        /// </summary>
        Task SetUniformMatrix4fvAsync(int programId, string name, float[] matrix);

        /// <summary>
        /// Resize the renderer's viewport (and canvas if necessary).
        /// </summary>
        Task ResizeAsync(int width, int height);

        /// <summary>
        /// Enable or disable depth buffer writes.
        /// When disabled, fragments are still depth tested but don't update the depth buffer.
        /// </summary>
        Task SetDepthMaskAsync(int rendererId, bool enabled);

        #endregion
    }
}
