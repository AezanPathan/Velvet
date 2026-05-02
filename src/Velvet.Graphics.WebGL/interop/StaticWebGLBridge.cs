using Microsoft.JSInterop;

namespace Velvet.Graphics.WebGL;

/// <summary>
/// Static bridge implementation that calls Velvet API using IJSRuntime.
/// Intended for static HTML / non-Blazor WASM hosts that only have canvas IDs.
/// Uses IJSRuntime so the same code works for many hosting scenarios.
/// </summary>
public sealed class StaticWebGLBridge : JsRuntimeWebGLBridgeBase
{
    #region Constructor

    /// <summary>
    /// Create a new StaticWebGLBridge.
    /// </summary>
    /// <param name="js">IJSRuntime instance (available in static WASM or other hosts).</param>
    public StaticWebGLBridge(IJSRuntime js)
        : base(js)
    {
    }

    #endregion

    #region Initialization

    /// <inheritdoc />
    public override async Task<int> InitWithIdAsync(string canvasId)
    {
        if (string.IsNullOrWhiteSpace(canvasId)) throw new ArgumentNullException(nameof(canvasId));
        return await Js.InvokeAsync<int>("Velvet.initById", canvasId).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override Task<int> InitWithElementAsync(object canvasElement)
    {
        // Static bridge does not accept ElementReference; host should call InitWithIdAsync.
        throw new NotSupportedException("StaticWebGLBridge supports initialization by canvas ID only. Use InitWithIdAsync.");
    }

    #endregion

}
