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
        Task<int> CreateMeshAsync(float[] vertices, ushort[]? indices = null);

        #endregion

        #region Rendering / state

        /// <summary>
        /// Draw a mesh using programId on the renderer identified by rendererId.
        /// This is intentionally minimal: the JS side will resolve IDs to real objects.
        /// </summary>
        Task DrawMeshAsync(int meshId, int programId, int rendererId);

        /// <summary>
        /// Clear the framebuffer with the given color.
        /// </summary>
        Task ClearAsync(float r, float g, float b, float a);

        /// <summary>
        /// Resize the renderer's viewport (and canvas if necessary).
        /// </summary>
        Task ResizeAsync(int width, int height);

        #endregion
    }
}
